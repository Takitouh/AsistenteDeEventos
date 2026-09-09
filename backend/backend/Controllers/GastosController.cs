using AsistenteEventos.Application.DTOs;
using AsistenteEventos.Application.Services;
using AsistenteEventos.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsistenteEventos.Controllers;

[ApiController]
[Authorize]
public class GastosController : ControllerBase
{
    private readonly IGastoService _gastoService;

    public GastosController(IGastoService gastoService)
    {
        _gastoService = gastoService;
    }

    [HttpGet("api/eventos/{eventoId:int}/gastos")]
    public async Task<ActionResult<ApiResponse<List<GastoDto>>>> GetByEventoId(int eventoId)
    {
        try
        {
            var gastos = await _gastoService.GetByEventoIdAsync(eventoId);
            return Ok(ApiResponse<List<GastoDto>>.Ok(gastos));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<List<GastoDto>>.Fail(ex.Message));
        }
    }

    [HttpPost("api/eventos/{eventoId:int}/gastos")]
    public async Task<ActionResult<ApiResponse<GastoDto>>> Create(int eventoId, [FromBody] GastoCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            return BadRequest(ApiResponse<GastoDto>.Fail("La descripción del gasto es obligatoria."));

        if (dto.Valor <= 0)
            return BadRequest(ApiResponse<GastoDto>.Fail("El valor del gasto en COP debe ser mayor a cero."));

        try
        {
            var gasto = await _gastoService.CreateAsync(eventoId, dto);
            return Ok(ApiResponse<GastoDto>.Ok(gasto, "Gasto registrado exitosamente."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<GastoDto>.Fail(ex.Message));
        }
    }

    [HttpGet("api/gastos/{id:int}")]
    public async Task<ActionResult<ApiResponse<GastoDto>>> GetById(int id)
    {
        try
        {
            var gasto = await _gastoService.GetByIdAsync(id);
            return Ok(ApiResponse<GastoDto>.Ok(gasto));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<GastoDto>.Fail(ex.Message));
        }
    }

    [HttpPut("api/gastos/{id:int}")]
    public async Task<ActionResult<ApiResponse<GastoDto>>> Update(int id, [FromBody] GastoUpdateDto dto)
    {
        try
        {
            var gasto = await _gastoService.UpdateAsync(id, dto);
            return Ok(ApiResponse<GastoDto>.Ok(gasto, "Gasto actualizado exitosamente."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<GastoDto>.Fail(ex.Message));
        }
    }

    [HttpDelete("api/gastos/{id:int}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        try
        {
            await _gastoService.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Gasto eliminado exitosamente."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
    }
}
