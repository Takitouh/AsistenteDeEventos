using AsistenteEventos.Application.DTOs;
using AsistenteEventos.Application.Services;
using AsistenteEventos.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsistenteEventos.Controllers;

[ApiController]
[Authorize]
public class TareasController : ControllerBase
{
    private readonly ITareaService _tareaService;

    public TareasController(ITareaService tareaService)
    {
        _tareaService = tareaService;
    }

    [HttpGet("api/eventos/{eventoId:int}/tareas")]
    public async Task<ActionResult<ApiResponse<List<TareaDto>>>> GetByEventoId(int eventoId)
    {
        try
        {
            var tareas = await _tareaService.GetByEventoIdAsync(eventoId);
            return Ok(ApiResponse<List<TareaDto>>.Ok(tareas));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<List<TareaDto>>.Fail(ex.Message));
        }
    }

    [HttpPost("api/eventos/{eventoId:int}/tareas")]
    public async Task<ActionResult<ApiResponse<TareaDto>>> Create(int eventoId, [FromBody] TareaCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest(ApiResponse<TareaDto>.Fail("El nombre de la tarea es obligatorio."));

        try
        {
            var tarea = await _tareaService.CreateAsync(eventoId, dto);
            return Ok(ApiResponse<TareaDto>.Ok(tarea, "Tarea creada exitosamente."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<TareaDto>.Fail(ex.Message));
        }
    }

    [HttpGet("api/tareas/{id:int}")]
    public async Task<ActionResult<ApiResponse<TareaDto>>> GetById(int id)
    {
        try
        {
            var tarea = await _tareaService.GetByIdAsync(id);
            return Ok(ApiResponse<TareaDto>.Ok(tarea));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<TareaDto>.Fail(ex.Message));
        }
    }

    [HttpPut("api/tareas/{id:int}")]
    public async Task<ActionResult<ApiResponse<TareaDto>>> Update(int id, [FromBody] TareaUpdateDto dto)
    {
        try
        {
            var tarea = await _tareaService.UpdateAsync(id, dto);
            return Ok(ApiResponse<TareaDto>.Ok(tarea, "Tarea actualizada exitosamente."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<TareaDto>.Fail(ex.Message));
        }
    }

    [HttpPatch("api/tareas/{id:int}/estado")]
    public async Task<ActionResult<ApiResponse<TareaDto>>> UpdateEstado(int id, [FromBody] TareaEstadoUpdateDto dto)
    {
        try
        {
            var tarea = await _tareaService.UpdateEstadoAsync(id, dto.Estado);
            return Ok(ApiResponse<TareaDto>.Ok(tarea, "Estado de la tarea actualizado exitosamente."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<TareaDto>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<TareaDto>.Fail(ex.Message));
        }
    }

    [HttpDelete("api/tareas/{id:int}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        try
        {
            await _tareaService.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Tarea eliminada exitosamente."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
    }
}
