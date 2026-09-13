using AsistenteEventos.Application.DTOs;
using AsistenteEventos.Application.Services;
using AsistenteEventos.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsistenteEventos.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificacionesController : ControllerBase
{
    private readonly INotificacionService _notificacionService;

    public NotificacionesController(INotificacionService notificacionService)
    {
        _notificacionService = notificacionService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<NotificacionDto>>>> GetAll([FromQuery] bool soloNoLeidas = false)
    {
        var notificaciones = await _notificacionService.GetAllAsync(soloNoLeidas);
        return Ok(ApiResponse<List<NotificacionDto>>.Ok(notificaciones));
    }

    [HttpPatch("{id:int}/leer")]
    public async Task<ActionResult<ApiResponse>> MarcarComoLeida(int id)
    {
        try
        {
            await _notificacionService.MarcarComoLeidaAsync(id);
            return Ok(ApiResponse.Ok("Notificación marcada como leída."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("generar")]
    public async Task<ActionResult<ApiResponse<int>>> GenerarRecordatorios()
    {
        var totalGeneradas = await _notificacionService.GenerarRecordatoriosAutomaticosAsync();
        return Ok(ApiResponse<int>.Ok(totalGeneradas, $"Se han sincronizado {totalGeneradas} recordatorio(s)."));
    }
}
