using Lugares.Application.DTOs;
using Lugares.Application.Interfaces;
using Lugares.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Lugares.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LugaresController : ControllerBase
{
    private readonly ILugarService _lugarService;
    private readonly IGooglePlacesService _googlePlacesService;
    private readonly IControlCuotaService _controlCuotaService;

    public LugaresController(
        ILugarService lugarService,
        IGooglePlacesService googlePlacesService,
        IControlCuotaService controlCuotaService)
    {
        _lugarService = lugarService;
        _googlePlacesService = googlePlacesService;
        _controlCuotaService = controlCuotaService;
    }

    /// <summary>
    /// Consulta y filtra lugares del catálogo según los criterios especificados.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LugarResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LugarResumenDto>>> Buscar(
        [FromQuery] LugarFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var lugares = await _lugarService.BuscarLugaresAsync(filtro, cancellationToken);
        return Ok(lugares);
    }

    /// <summary>
    /// Obtiene la ficha de detalle de un lugar por su identificador único.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LugarDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LugarDetalleDto>> ObtenerPorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        var lugar = await _lugarService.ObtenerDetalleLugarAsync(id, cancellationToken);
        if (lugar == null)
        {
            return NotFound(new { mensaje = $"No se encontró el lugar con ID {id}" });
        }

        return Ok(lugar);
    }

    /// <summary>
    /// Obtiene el catálogo de categorías disponibles.
    /// </summary>
    [HttpGet("categorias")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoriaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoriaDto>>> ObtenerCategorias(
        CancellationToken cancellationToken)
    {
        var categorias = await _lugarService.ObtenerCategoriasAsync(cancellationToken);
        return Ok(categorias);
    }

    /// <summary>
    /// Busca lugares en Google Places y los guarda automáticamente en PostgreSQL (caché write-through).
    /// </summary>
    [HttpPost("google/buscar")]
    [ProducesResponseType(typeof(IReadOnlyList<LugarResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LugarResumenDto>>> BuscarEnGooglePlaces(
        [FromQuery] string busqueda,
        [FromQuery] BarrioCaba barrio,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(busqueda))
        {
            return BadRequest(new { mensaje = "El parámetro de búsqueda es requerido." });
        }

        var lugares = await _googlePlacesService.BuscarYCachearAsync(busqueda, barrio, cancellationToken);
        return Ok(lugares);
    }

    /// <summary>
    /// Obtiene el estado actual de la cuota diaria de llamadas a Google Places y Circuit Breaker.
    /// </summary>
    [HttpGet("google/cuota")]
    [ProducesResponseType(typeof(ControlCuotaGoogleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ControlCuotaGoogleDto>> ObtenerEstadoCuota(
        CancellationToken cancellationToken)
    {
        var cuota = await _controlCuotaService.ObtenerEstadoHoyAsync(cancellationToken);
        return Ok(cuota);
    }
}
