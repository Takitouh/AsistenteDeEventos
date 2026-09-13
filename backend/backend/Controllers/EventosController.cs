using AsistenteEventos.Application.DTOs;
using AsistenteEventos.Application.Services;
using AsistenteEventos.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsistenteEventos.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventosController : ControllerBase
{
    private readonly IEventoService _eventoService;

    public EventosController(IEventoService eventoService)
    {
        _eventoService = eventoService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<EventoDto>>>> GetAll()
    {
        var eventos = await _eventoService.GetAllAsync();
        return Ok(ApiResponse<List<EventoDto>>.Ok(eventos));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<EventoDto>>> GetById(int id)
    {
        try
        {
            var evento = await _eventoService.GetByIdAsync(id);
            return Ok(ApiResponse<EventoDto>.Ok(evento));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<EventoDto>.Fail(ex.Message));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EventoDto>>> Create([FromBody] EventoCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest(ApiResponse<EventoDto>.Fail("El nombre del evento es obligatorio."));

        var evento = await _eventoService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = evento.Id }, ApiResponse<EventoDto>.Ok(evento, "Evento creado exitosamente."));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<EventoDto>>> Update(int id, [FromBody] EventoUpdateDto dto)
    {
        try
        {
            var evento = await _eventoService.UpdateAsync(id, dto);
            return Ok(ApiResponse<EventoDto>.Ok(evento, "Evento actualizado exitosamente."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<EventoDto>.Fail(ex.Message));
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        try
        {
            await _eventoService.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Evento eliminado exitosamente."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
    }
}
