using AsistenteEventos.Application.DTOs;
using AsistenteEventos.Application.Services;
using AsistenteEventos.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsistenteEventos.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<DashboardResumenDto>>> GetDefault()
    {
        var dashboard = await _dashboardService.GetDashboardDefaultAsync();
        if (dashboard == null)
            return Ok(ApiResponse<DashboardResumenDto?>.Ok(null, "No hay eventos registrados para mostrar en el dashboard."));

        return Ok(ApiResponse<DashboardResumenDto>.Ok(dashboard));
    }

    [HttpGet("{eventoId:int}")]
    public async Task<ActionResult<ApiResponse<DashboardResumenDto>>> GetByEventoId(int eventoId)
    {
        try
        {
            var dashboard = await _dashboardService.GetDashboardByEventoIdAsync(eventoId);
            if (dashboard == null)
                return NotFound(ApiResponse<DashboardResumenDto>.Fail("No se encontró información del evento."));

            return Ok(ApiResponse<DashboardResumenDto>.Ok(dashboard));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<DashboardResumenDto>.Fail(ex.Message));
        }
    }
}
