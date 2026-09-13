using AsistenteEventos.Application.DTOs;
using AsistenteEventos.Application.Services;
using AsistenteEventos.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsistenteEventos.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GeminiController : ControllerBase
{
    private readonly IGeminiService _geminiService;

    public GeminiController(IGeminiService geminiService)
    {
        _geminiService = geminiService;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ApiResponse<GeminiChatResponseDto>>> Chat([FromBody] GeminiChatRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Mensaje))
            return BadRequest(ApiResponse<GeminiChatResponseDto>.Fail("El mensaje no puede estar vacío."));

        var response = await _geminiService.ChatAsync(request);
        return Ok(ApiResponse<GeminiChatResponseDto>.Ok(response));
    }

    [HttpPost("planificar")]
    public async Task<ActionResult<ApiResponse<PropuestasContainerDto>>> Planificar([FromBody] GeminiPlanRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ObjetivoComercial))
            return BadRequest(ApiResponse<PropuestasContainerDto>.Fail("El objetivo comercial del evento es obligatorio."));

        var propuestas = await _geminiService.GenerarPropuestasAsync(request);
        return Ok(ApiResponse<PropuestasContainerDto>.Ok(propuestas, "Propuestas estructuradas generadas exitosamente."));
    }

    [HttpPost("formalizar")]
    public async Task<ActionResult<ApiResponse<EventoDto>>> Formalizar([FromBody] FormalizarPropuestaDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return BadRequest(ApiResponse<EventoDto>.Fail("El nombre del evento es obligatorio para formalizarlo."));

        var eventoCreado = await _geminiService.FormalizarPropuestaAsync(request);
        return Ok(ApiResponse<EventoDto>.Ok(eventoCreado, "¡Evento formalizado exitosamente en tu planificación!"));
    }
}
