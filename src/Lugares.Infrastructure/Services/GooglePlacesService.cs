using System.Text.Json;
using Lugares.Application.DTOs;
using Lugares.Application.Interfaces;
using Lugares.Domain.Entities;
using Lugares.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lugares.Infrastructure.Services;

public class GooglePlacesService : IGooglePlacesService
{
    private readonly HttpClient _httpClient;
    private readonly ILugarRepository _lugarRepository;
    private readonly IControlCuotaService _controlCuotaService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GooglePlacesService> _logger;

    public GooglePlacesService(
        HttpClient httpClient,
        ILugarRepository lugarRepository,
        IControlCuotaService controlCuotaService,
        IConfiguration configuration,
        ILogger<GooglePlacesService> logger)
    {
        _httpClient = httpClient;
        _lugarRepository = lugarRepository;
        _controlCuotaService = controlCuotaService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LugarResumenDto>> BuscarYCachearAsync(
        string busqueda,
        BarrioCaba barrio,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["GooglePlaces:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("TU_API_KEY"))
        {
            _logger.LogWarning("Búsqueda en Google Places omitida: no hay ApiKey configurada.");
            return Array.Empty<LugarResumenDto>();
        }

        // 1. Validar Circuit Breaker / Control de Cuota
        var puedeLlamar = await _controlCuotaService.PuedeRealizarLlamadaAsync(cancellationToken);
        if (!puedeLlamar)
        {
            _logger.LogWarning("Búsqueda en Google Places omitida por Circuit Breaker (límite diario alcanzado).");
            return Array.Empty<LugarResumenDto>();
        }

        // 2. Realizar petición HTTP a Google Places API (Text Search)
        var queryCompleta = Uri.EscapeDataString($"{busqueda} {barrio} Buenos Aires");
        var url = $"https://maps.googleapis.com/maps/api/place/textsearch/json?query={queryCompleta}&key={apiKey}";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error al consultar Google Places API: {StatusCode}", response.StatusCode);
                return Array.Empty<LugarResumenDto>();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var placesData = JsonSerializer.Deserialize<GooglePlacesResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Registrar llamada consumida en el contador diario
            await _controlCuotaService.RegistrarLlamadaAsync(cancellationToken);

            if (placesData?.Results == null || placesData.Results.Count == 0)
            {
                return Array.Empty<LugarResumenDto>();
            }

            var lugaresProcesados = new List<LugarResumenDto>();

            // 3. Estrategia Write-Through: Cachear / Persistir en PostgreSQL
            foreach (var item in placesData.Results.Take(10)) // Tomamos los 10 mejores resultados
            {
                if (string.IsNullOrWhiteSpace(item.PlaceId) || string.IsNullOrWhiteSpace(item.Name) || item.Geometry?.Location == null)
                {
                    continue;
                }

                // Verificar si ya existe en la base por id_google_place (evitar duplicados)
                var existente = await _lugarRepository.ObtenerPorGooglePlaceIdAsync(item.PlaceId, cancellationToken);
                if (existente != null)
                {
                    existente.FechaUltimaSincronizacion = DateTime.UtcNow;
                    existente.Puntuacion = item.Rating;
                    existente.CantidadResenas = item.UserRatingsTotal ?? existente.CantidadResenas;
                    await _lugarRepository.ActualizarAsync(existente, cancellationToken);

                    lugaresProcesados.Add(MapearAResumen(existente));
                    continue;
                }

                var nuevoLugar = new Lugar
                {
                    Id = Guid.NewGuid(),
                    Origen = OrigenLugar.GooglePlaces,
                    IdGooglePlace = item.PlaceId,
                    FechaUltimaSincronizacion = DateTime.UtcNow,
                    TipoLugar = TipoLugar.PuntoInteres,
                    Nombre = item.Name,
                    Barrio = barrio,
                    Direccion = item.FormattedAddress ?? $"{barrio}, Buenos Aires",
                    Latitud = item.Geometry.Location.Lat,
                    Longitud = item.Geometry.Location.Lng,
                    DuracionEstimadaMinutos = 60,
                    NivelPrecio = (short)Math.Clamp(item.PriceLevel ?? 1, 1, 3),
                    Puntuacion = item.Rating,
                    CantidadResenas = item.UserRatingsTotal ?? 0,
                    EsBajoTecho = true,
                    EsAlAireLibre = false,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = false,
                    OfreceMenuInfantil = false,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow,
                    FechaActualizacion = DateTime.UtcNow
                };

                if (item.Photos != null && item.Photos.Count > 0)
                {
                    var photoRef = item.Photos[0].PhotoReference;
                    if (!string.IsNullOrWhiteSpace(photoRef))
                    {
                        nuevoLugar.Fotos.Add(new FotoLugar
                        {
                            Id = Guid.NewGuid(),
                            LugarId = nuevoLugar.Id,
                            Url = $"https://maps.googleapis.com/maps/api/place/photo?maxwidth=800&photo_reference={photoRef}&key={apiKey}",
                            Orden = 1,
                            EsPortada = true
                        });
                    }
                }

                await _lugarRepository.AgregarAsync(nuevoLugar, cancellationToken);
                lugaresProcesados.Add(MapearAResumen(nuevoLugar));
            }

            return lugaresProcesados;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción inesperada al procesar búsqueda de Google Places.");
            return Array.Empty<LugarResumenDto>();
        }
    }

    private static LugarResumenDto MapearAResumen(Lugar l)
    {
        return new LugarResumenDto
        {
            Id = l.Id,
            Nombre = l.Nombre,
            TipoLugar = l.TipoLugar,
            Barrio = l.Barrio,
            Subzona = l.Subzona,
            Direccion = l.Direccion,
            Latitud = l.Latitud,
            Longitud = l.Longitud,
            DuracionEstimadaMinutos = l.DuracionEstimadaMinutos,
            NivelPrecio = l.NivelPrecio,
            Puntuacion = l.Puntuacion,
            CantidadResenas = l.CantidadResenas,
            EsBajoTecho = l.EsBajoTecho,
            EsAlAireLibre = l.EsAlAireLibre,
            AptoNinos = l.AptoNinos,
            AccesibleMovilidadReducida = l.AccesibleMovilidadReducida,
            FotoPortadaUrl = l.Fotos.FirstOrDefault(f => f.EsPortada)?.Url ?? l.Fotos.FirstOrDefault()?.Url,
            Categorias = l.Categorias.Select(c => c.Categoria.Nombre).ToList()
        };
    }

    private class GooglePlacesResponse
    {
        public string? Status { get; set; }
        public List<GooglePlaceItem>? Results { get; set; }
    }

    private class GooglePlaceItem
    {
        public string? PlaceId { get; set; }
        public string? Name { get; set; }
        public string? FormattedAddress { get; set; }
        public GoogleGeometry? Geometry { get; set; }
        public decimal? Rating { get; set; }
        public int? UserRatingsTotal { get; set; }
        public int? PriceLevel { get; set; }
        public List<GooglePhotoItem>? Photos { get; set; }
    }

    private class GoogleGeometry
    {
        public GoogleLocation? Location { get; set; }
    }

    private class GoogleLocation
    {
        public decimal Lat { get; set; }
        public decimal Lng { get; set; }
    }

    private class GooglePhotoItem
    {
        public string? PhotoReference { get; set; }
    }
}
