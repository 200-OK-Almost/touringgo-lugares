using System.Text;
using System.Text.Json;
using Lugares.Application.DTOs;
using Lugares.Application.Interfaces;
using Lugares.Domain.Entities;
using Lugares.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

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

        // 2. Realizar petición HTTP a Google Places API (New Text Search)
        const string url = "https://places.googleapis.com/v1/places:searchText";
        var queryCompleta = $"{busqueda} {barrio} Buenos Aires";

        var requestBody = new
        {
            textQuery = queryCompleta,
            languageCode = "es"
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        requestMessage.Headers.Add("X-Goog-Api-Key", apiKey);
        requestMessage.Headers.Add("X-Goog-FieldMask", "places.id,places.displayName,places.formattedAddress,places.location,places.rating,places.userRatingCount,places.priceLevel,places.types,places.photos");

        try
        {
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Error al consultar Google Places API (New): {StatusCode} - {Error}", response.StatusCode, errorBody);
                return Array.Empty<LugarResumenDto>();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var placesData = JsonSerializer.Deserialize<GooglePlacesNewResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Registrar llamada consumida en el contador diario
            await _controlCuotaService.RegistrarLlamadaAsync(cancellationToken);

            if (placesData?.Places == null || placesData.Places.Count == 0)
            {
                return Array.Empty<LugarResumenDto>();
            }

            // Catálogo oficial de categorías de TouringGO para mapeo automático
            var categoriasDb = await _lugarRepository.ObtenerCategoriasAsync(cancellationToken);

            var lugaresProcesados = new List<LugarResumenDto>();

            // 3. Estrategia Write-Through: Cachear / Persistir en PostgreSQL
            foreach (var item in placesData.Places.Take(10)) // Tomamos los 10 mejores resultados
            {
                var placeId = item.Id;
                var nombre = item.DisplayName?.Text;

                if (string.IsNullOrWhiteSpace(placeId) || string.IsNullOrWhiteSpace(nombre) || item.Location == null)
                {
                    continue;
                }

                // Verificar si ya existe en la base por id_google_place (evitar duplicados)
                var existente = await _lugarRepository.ObtenerPorGooglePlaceIdAsync(placeId, cancellationToken);
                if (existente != null)
                {
                    existente.FechaUltimaSincronizacion = DateTime.UtcNow;
                    existente.Puntuacion = item.Rating;
                    existente.CantidadResenas = item.UserRatingCount ?? existente.CantidadResenas;
                    await _lugarRepository.ActualizarAsync(existente, cancellationToken);

                    lugaresProcesados.Add(MapearAResumen(existente));
                    continue;
                }

                var lat = item.Location.Latitude;
                var lng = item.Location.Longitude;

                var nuevoLugar = new Lugar
                {
                    Id = Guid.NewGuid(),
                    Origen = OrigenLugar.GooglePlaces,
                    IdGooglePlace = placeId,
                    FechaUltimaSincronizacion = DateTime.UtcNow,
                    TipoLugar = TipoLugar.PuntoInteres,
                    Nombre = nombre,
                    Barrio = barrio,
                    Direccion = item.FormattedAddress ?? $"{barrio}, Buenos Aires",
                    Latitud = lat,
                    Longitud = lng,
                    Ubicacion = new Point((double)lng, (double)lat) { SRID = 4326 },
                    DuracionEstimadaMinutos = 60,
                    NivelPrecio = MapearNivelPrecio(item.PriceLevel),
                    Puntuacion = item.Rating,
                    CantidadResenas = item.UserRatingCount ?? 0,
                    EsBajoTecho = true,
                    EsAlAireLibre = false,
                    AptoNinos = true,
                    AccesibleMovilidadReducida = false,
                    OfreceMenuInfantil = false,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow,
                    FechaActualizacion = DateTime.UtcNow
                };

                // Mapeo automático de categorías oficiales de TouringGO
                var codigoCat = DeterminarCodigoCategoria(item.Types, nombre);
                var categoriaMatch = categoriasDb.FirstOrDefault(c => c.Codigo == codigoCat) ?? categoriasDb.FirstOrDefault();
                if (categoriaMatch != null)
                {
                    nuevoLugar.Categorias.Add(new LugarCategoria
                    {
                        LugarId = nuevoLugar.Id,
                        CategoriaId = categoriaMatch.Id,
                        Categoria = categoriaMatch,
                        EsPrincipal = true
                    });
                }

                // Fotos optimizadas para web (maxHeightPx=480, maxWidthPx=480 para evitar 4K y sobrecargar ancho de banda)
                string? photoUrl = null;
                if (item.Photos != null && item.Photos.Count > 0)
                {
                    var photoName = item.Photos[0].Name;
                    if (!string.IsNullOrWhiteSpace(photoName))
                    {
                        photoUrl = $"https://places.googleapis.com/v1/{photoName}/media?maxHeightPx=480&maxWidthPx=480&key={apiKey}";
                    }
                }

                // Fallback liviano y optimizado (480px) por categoría si Google omite fotos (ej. sin cuenta de facturación vinculada)
                if (string.IsNullOrWhiteSpace(photoUrl))
                {
                    photoUrl = ObtenerFotoFallbackPorCategoria(codigoCat);
                }

                if (!string.IsNullOrWhiteSpace(photoUrl))
                {
                    nuevoLugar.Fotos.Add(new FotoLugar
                    {
                        Id = Guid.NewGuid(),
                        LugarId = nuevoLugar.Id,
                        Url = photoUrl,
                        Orden = 1,
                        EsPortada = true
                    });
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

    private static short MapearNivelPrecio(string? priceLevel)
    {
        return priceLevel switch
        {
            "PRICE_LEVEL_INEXPENSIVE" => 1,
            "PRICE_LEVEL_MODERATE" => 2,
            "PRICE_LEVEL_EXPENSIVE" => 3,
            "PRICE_LEVEL_VERY_EXPENSIVE" => 3,
            _ => 1
        };
    }

    private static string DeterminarCodigoCategoria(List<string>? types, string nombre)
    {
        var nombreLower = nombre.ToLowerInvariant();
        var typeSet = new HashSet<string>(types ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        if (typeSet.Contains("cafe") || typeSet.Contains("coffee_shop") || typeSet.Contains("bakery") || nombreLower.Contains("café") || nombreLower.Contains("cafe"))
            return "cafe_especialidad";

        if (typeSet.Contains("cocktail_bar") || typeSet.Contains("night_club") || nombreLower.Contains("cocktail") || nombreLower.Contains("coctel"))
            return "cocteleria_autor";

        if (typeSet.Contains("bar") || typeSet.Contains("pub") || typeSet.Contains("wine_bar") || nombreLower.Contains("bar") || nombreLower.Contains("vermú") || nombreLower.Contains("cerveza"))
            return "bares_vermu";

        if (typeSet.Contains("museum") || typeSet.Contains("art_gallery") || typeSet.Contains("cultural_center") || nombreLower.Contains("museo") || nombreLower.Contains("galería"))
            return "museos_galerias";

        if (typeSet.Contains("park") || typeSet.Contains("tourist_attraction") || typeSet.Contains("campground") || typeSet.Contains("plaza") || nombreLower.Contains("plaza") || nombreLower.Contains("parque") || nombreLower.Contains("jardín"))
            return "parques_aire_libre";

        if (typeSet.Contains("book_store") || typeSet.Contains("library") || nombreLower.Contains("librería") || nombreLower.Contains("libros"))
            return "librerias_diseno";

        return "gastronomia_clasica";
    }

    private static string ObtenerFotoFallbackPorCategoria(string codigoCategoria)
    {
        return codigoCategoria switch
        {
            "cafe_especialidad" => "https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb?w=480&q=80",
            "museos_galerias" => "https://images.unsplash.com/photo-1565008447742-97f6f38c985c?w=480&q=80",
            "parques_aire_libre" => "https://images.unsplash.com/photo-1519331379826-f10be5486c6f?w=480&q=80",
            "librerias_diseno" => "https://images.unsplash.com/photo-1507842229451-7f01be7fe88d?w=480&q=80",
            "bares_vermu" => "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=480&q=80",
            "cocteleria_autor" => "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=480&q=80",
            _ => "https://images.unsplash.com/photo-1544025162-d76694265947?w=480&q=80"
        };
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
            Categorias = l.Categorias
                .Where(c => c.Categoria != null)
                .Select(c => c.Categoria.Codigo)
                .ToList()
        };
    }

    private class GooglePlacesNewResponse
    {
        public List<GooglePlaceItem>? Places { get; set; }
    }

    private class GooglePlaceItem
    {
        public string? Id { get; set; }
        public GoogleDisplayName? DisplayName { get; set; }
        public string? FormattedAddress { get; set; }
        public GoogleLocation? Location { get; set; }
        public decimal? Rating { get; set; }
        public int? UserRatingCount { get; set; }
        public string? PriceLevel { get; set; }
        public List<string>? Types { get; set; }
        public List<GooglePhotoItem>? Photos { get; set; }
    }

    private class GoogleDisplayName
    {
        public string? Text { get; set; }
    }

    private class GoogleLocation
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }

    private class GooglePhotoItem
    {
        public string? Name { get; set; }
    }
}
