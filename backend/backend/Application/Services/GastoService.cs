using AsistenteEventos.Application.DTOs;
using AsistenteEventos.Domain.Entities;
using AsistenteEventos.Infrastructure.Data;
using AsistenteEventos.Security.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AsistenteEventos.Application.Services;

public interface IGastoService
{
    Task<List<GastoDto>> GetByEventoIdAsync(int eventoId);
    Task<GastoDto> GetByIdAsync(int id);
    Task<GastoDto> CreateAsync(int eventoId, GastoCreateDto dto);
    Task<GastoDto> UpdateAsync(int id, GastoUpdateDto dto);
    Task<bool> DeleteAsync(int id);
}

public class GastoService : IGastoService
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GastoService> _logger;

    public GastoService(AppDbContext context, ITenantContext tenantContext, ILogger<GastoService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    private int EnsureTenantId()
    {
        if (!_tenantContext.PymeId.HasValue)
            throw new UnauthorizedAccessException("Acceso denegado: Identidad de PYME no disponible.");
        return _tenantContext.PymeId.Value;
    }

    public async Task<List<GastoDto>> GetByEventoIdAsync(int eventoId)
    {
        var pymeId = EnsureTenantId();

        var eventoExiste = await _context.Eventos
            .AnyAsync(e => e.Id == eventoId && e.PymeId == pymeId);

        if (!eventoExiste)
            throw new KeyNotFoundException($"El evento con ID {eventoId} no fue encontrado.");

        return await _context.Gastos
            .Where(g => g.EventoId == eventoId && g.PymeId == pymeId)
            .OrderByDescending(g => g.Fecha)
            .Select(g => new GastoDto
            {
                Id = g.Id,
                EventoId = g.EventoId,
                Descripcion = g.Descripcion,
                Categoria = g.Categoria,
                Valor = g.Valor,
                Fecha = g.Fecha
            })
            .ToListAsync();
    }

    public async Task<GastoDto> GetByIdAsync(int id)
    {
        var pymeId = EnsureTenantId();

        var gasto = await _context.Gastos
            .Where(g => g.Id == id && g.PymeId == pymeId)
            .FirstOrDefaultAsync();

        if (gasto == null)
            throw new KeyNotFoundException($"El gasto con ID {id} no fue encontrado.");

        return new GastoDto
        {
            Id = gasto.Id,
            EventoId = gasto.EventoId,
            Descripcion = gasto.Descripcion,
            Categoria = gasto.Categoria,
            Valor = gasto.Valor,
            Fecha = gasto.Fecha
        };
    }

    public async Task<GastoDto> CreateAsync(int eventoId, GastoCreateDto dto)
    {
        var pymeId = EnsureTenantId();

        var evento = await _context.Eventos
            .Include(e => e.Gastos)
            .Where(e => e.Id == eventoId && e.PymeId == pymeId)
            .FirstOrDefaultAsync();

        if (evento == null)
            throw new KeyNotFoundException($"El evento con ID {eventoId} no fue encontrado.");

        var gasto = new Gasto
        {
            PymeId = pymeId,
            EventoId = eventoId,
            Descripcion = dto.Descripcion.Trim(),
            Categoria = string.IsNullOrWhiteSpace(dto.Categoria) ? "Logística" : dto.Categoria.Trim(),
            Valor = dto.Valor,
            Fecha = dto.Fecha ?? DateTime.UtcNow
        };

        _context.Gastos.Add(gasto);
        await _context.SaveChangesAsync();

        // Verificar si los gastos superan el presupuesto y generar notificación
        var nuevoTotalGastos = evento.Gastos.Sum(g => g.Valor) + dto.Valor;
        if (nuevoTotalGastos > evento.PresupuestoBase && evento.PresupuestoBase > 0)
        {
            var alerta = new Notificacion
            {
                PymeId = pymeId,
                EventoId = eventoId,
                Mensaje = $"Alerta Presupuestal: El evento '{evento.Nombre}' ha superado su presupuesto base (${evento.PresupuestoBase:N0} COP) al registrar un gasto de ${dto.Valor:N0} COP.",
                Tipo = "PresupuestoExcedido",
                Leida = false,
                FechaCreacion = DateTime.UtcNow
            };
            _context.Notificaciones.Add(alerta);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Gasto registrado ID {Id} para Evento {EventoId} por ${Valor} COP", gasto.Id, eventoId, gasto.Valor);

        return new GastoDto
        {
            Id = gasto.Id,
            EventoId = gasto.EventoId,
            Descripcion = gasto.Descripcion,
            Categoria = gasto.Categoria,
            Valor = gasto.Valor,
            Fecha = gasto.Fecha
        };
    }

    public async Task<GastoDto> UpdateAsync(int id, GastoUpdateDto dto)
    {
        var pymeId = EnsureTenantId();

        var gasto = await _context.Gastos
            .Where(g => g.Id == id && g.PymeId == pymeId)
            .FirstOrDefaultAsync();

        if (gasto == null)
            throw new KeyNotFoundException($"El gasto con ID {id} no fue encontrado.");

        gasto.Descripcion = dto.Descripcion.Trim();
        gasto.Categoria = dto.Categoria.Trim();
        gasto.Valor = dto.Valor;
        gasto.Fecha = dto.Fecha;

        await _context.SaveChangesAsync();

        return new GastoDto
        {
            Id = gasto.Id,
            EventoId = gasto.EventoId,
            Descripcion = gasto.Descripcion,
            Categoria = gasto.Categoria,
            Valor = gasto.Valor,
            Fecha = gasto.Fecha
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var pymeId = EnsureTenantId();

        var gasto = await _context.Gastos
            .Where(g => g.Id == id && g.PymeId == pymeId)
            .FirstOrDefaultAsync();

        if (gasto == null)
            throw new KeyNotFoundException($"El gasto con ID {id} no fue encontrado.");

        _context.Gastos.Remove(gasto);
        await _context.SaveChangesAsync();
        return true;
    }
}
