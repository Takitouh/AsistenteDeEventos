using AsistenteEventos.Application.DTOs;
using AsistenteEventos.Domain.Entities;
using AsistenteEventos.Infrastructure.Data;
using AsistenteEventos.Security.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AsistenteEventos.Application.Services;

public interface IPresupuestoService
{
    Task<PresupuestoResumenDto> GetResumenPresupuestoAsync(int eventoId);
    Task<List<RubroPresupuestalDto>> UpdateRubrosAsync(int eventoId, RubrosBulkUpdateDto dto);
}

public class PresupuestoService : IPresupuestoService
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PresupuestoService> _logger;

    public PresupuestoService(AppDbContext context, ITenantContext tenantContext, ILogger<PresupuestoService> logger)
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

    public async Task<PresupuestoResumenDto> GetResumenPresupuestoAsync(int eventoId)
    {
        var pymeId = EnsureTenantId();

        var evento = await _context.Eventos
            .Include(e => e.Rubros)
            .Include(e => e.Gastos)
            .Where(e => e.Id == eventoId && e.PymeId == pymeId)
            .FirstOrDefaultAsync();

        if (evento == null)
            throw new KeyNotFoundException($"El evento con ID {eventoId} no fue encontrado.");

        var totalGastos = evento.Gastos.Sum(g => g.Valor);
        var saldoDisponible = evento.PresupuestoBase - totalGastos;
        var porcentajeEjecucion = evento.PresupuestoBase > 0
            ? Math.Round((totalGastos / evento.PresupuestoBase) * 100m, 2)
            : 0m;

        var superaPresupuesto = totalGastos > evento.PresupuestoBase;
        string alertaMensaje = string.Empty;

        if (superaPresupuesto)
        {
            var sobrecosto = totalGastos - evento.PresupuestoBase;
            alertaMensaje = $"¡Atención! Los gastos reales han superado el presupuesto asignado en ${sobrecosto:N0} COP ({porcentajeEjecucion}% ejecutado).";
        }
        else if (porcentajeEjecucion >= 90m)
        {
            alertaMensaje = $"Alerta: Se ha ejecutado el {porcentajeEjecucion}% del presupuesto asignado. Saldo restante: ${saldoDisponible:N0} COP.";
        }

        // Agrupar gastos por categoría para comparar con rubros estimados
        var gastosPorCategoria = evento.Gastos
            .GroupBy(g => g.Categoria)
            .ToDictionary(k => k.Key, v => v.Sum(g => g.Valor), StringComparer.OrdinalIgnoreCase);

        var rubrosDtos = evento.Rubros.Select(r => new RubroPresupuestalDto
        {
            Id = r.Id,
            EventoId = r.EventoId,
            Categoria = r.Categoria,
            Porcentaje = r.Porcentaje,
            ValorEstimado = r.ValorEstimado,
            TotalGastadoEnCategoria = gastosPorCategoria.TryGetValue(r.Categoria, out var gastoCat) ? gastoCat : 0m
        }).ToList();

        var ultimosGastos = evento.Gastos
            .OrderByDescending(g => g.Fecha)
            .Take(5)
            .Select(g => new GastoDto
            {
                Id = g.Id,
                EventoId = g.EventoId,
                Descripcion = g.Descripcion,
                Categoria = g.Categoria,
                Valor = g.Valor,
                Fecha = g.Fecha
            }).ToList();

        return new PresupuestoResumenDto
        {
            EventoId = evento.Id,
            NombreEvento = evento.Nombre,
            PresupuestoAsignadoCOP = evento.PresupuestoBase,
            GastosRealesTotalesCOP = totalGastos,
            SaldoDisponibleCOP = saldoDisponible,
            PorcentajeEjecucion = porcentajeEjecucion,
            SuperaPresupuesto = superaPresupuesto,
            AlertaMensaje = alertaMensaje,
            Rubros = rubrosDtos,
            UltimosGastos = ultimosGastos
        };
    }

    public async Task<List<RubroPresupuestalDto>> UpdateRubrosAsync(int eventoId, RubrosBulkUpdateDto dto)
    {
        var pymeId = EnsureTenantId();

        var evento = await _context.Eventos
            .Include(e => e.Rubros)
            .Where(e => e.Id == eventoId && e.PymeId == pymeId)
            .FirstOrDefaultAsync();

        if (evento == null)
            throw new KeyNotFoundException($"El evento con ID {eventoId} no fue encontrado.");

        // Eliminar rubros anteriores y reemplazar con los nuevos
        _context.RubrosPresupuestales.RemoveRange(evento.Rubros);

        foreach (var item in dto.Rubros)
        {
            var rubro = new RubroPresupuestal
            {
                PymeId = pymeId,
                EventoId = eventoId,
                Categoria = item.Categoria.Trim(),
                Porcentaje = item.Porcentaje,
                ValorEstimado = item.ValorEstimado > 0 
                    ? item.ValorEstimado 
                    : (evento.PresupuestoBase * (item.Porcentaje / 100m))
            };
            _context.RubrosPresupuestales.Add(rubro);
        }

        await _context.SaveChangesAsync();

        return await _context.RubrosPresupuestales
            .Where(r => r.EventoId == eventoId && r.PymeId == pymeId)
            .Select(r => new RubroPresupuestalDto
            {
                Id = r.Id,
                EventoId = r.EventoId,
                Categoria = r.Categoria,
                Porcentaje = r.Porcentaje,
                ValorEstimado = r.ValorEstimado,
                TotalGastadoEnCategoria = 0m
            })
            .ToListAsync();
    }
}
