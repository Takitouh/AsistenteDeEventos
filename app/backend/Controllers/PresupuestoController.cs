using AsistenteEventos.Application.DTOs;
using AsistenteEventos.Application.Services;
using AsistenteEventos.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsistenteEventos.Controllers;

[ApiController]
[Authorize]
public class PresupuestoController : ControllerBase
{
    private readonly IPresupuestoService _presupuestoService;

    public PresupuestoController(IPresupuestoService presupuestoService)
    {
        _presupuestoService = presupuestoService;
    }

    [HttpGet("api/eventos/{eventoId:int}/presupuesto")]
    public async Task<ActionResult<ApiResponse<PresupuestoResumenDto>>> GetResumenPresupuesto(int eventoId)
    {
        try
        {
            var resumen = await _presupuestoService.GetResumenPresupuestoAsync(eventoId);
            return Ok(ApiResponse<PresupuestoResumenDto>.Ok(resumen));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<PresupuestoResumenDto>.Fail(ex.Message));
        }
    }

    [HttpPut("api/eventos/{eventoId:int}/presupuesto")]
    public async Task<ActionResult<ApiResponse<List<RubroPresupuestalDto>>>> UpdateRubros(int eventoId, [FromBody] RubrosBulkUpdateDto dto)
    {
        try
        {
            var rubros = await _presupuestoService.UpdateRubrosAsync(eventoId, dto);
            return Ok(ApiResponse<List<RubroPresupuestalDto>>.Ok(rubros, "Rubros presupuestales actualizados exitosamente."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<List<RubroPresupuestalDto>>.Fail(ex.Message));
        }
    }
}
