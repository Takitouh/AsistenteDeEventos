using AsistenteEventos.Application.DTOs;
using AsistenteEventos.Infrastructure.Data;
using AsistenteEventos.Security.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AsistenteEventos.Application.Services;

public interface IDashboardService
{
    Task<DashboardResumenDto?> GetDashboardByEventoIdAsync(int eventoId);
    Task<DashboardResumenDto?> GetDashboardDefaultAsync();
}

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;

    public DashboardService(AppDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    private int EnsureTenantId()
    {
        if (!_tenantContext.PymeId.HasValue)
            throw new UnauthorizedAccessException("Acceso denegado: Identidad de PYME no disponible.");
        return _tenantContext.PymeId.Value;
    }

    public async Task<DashboardResumenDto?> GetDashboardDefaultAsync()
    {
        var pymeId = EnsureTenantId();

        // Buscar el evento activo más relevante (En Proceso o En Planificación, o el más reciente)
        var evento = await _context.Eventos
            .Where(e => e.PymeId == pymeId)
            .OrderByDescending(e => e.Estado == "En Proceso")
            .ThenByDescending(e => e.Estado == "En Planificación")
            .ThenByDescending(e => e.FechaEvento)
            .FirstOrDefaultAsync();

        if (evento == null)
            return null;

        return await GetDashboardByEventoIdAsync(evento.Id);
    }

    public async Task<DashboardResumenDto?> GetDashboardByEventoIdAsync(int eventoId)
    {
        var pymeId = EnsureTenantId();

        var evento = await _context.Eventos
            .Include(e => e.Tareas)
            .Include(e => e.Gastos)
            .Where(e => e.Id == eventoId && e.PymeId == pymeId)
            .FirstOrDefaultAsync();

        if (evento == null)
            throw new KeyNotFoundException($"El evento con ID {eventoId} no fue encontrado.");

        var hoy = DateTime.UtcNow.Date;
        var diasRestantes = (evento.FechaEvento.Date - hoy).Days;

        // Cálculos de tareas
        var totalTareas = evento.Tareas.Count;
        var tareasCompletadas = evento.Tareas.Count(t => t.Estado == "Completada");
        var tareasEnProceso = evento.Tareas.Count(t => t.Estado == "En Proceso");
        var tareasPendientes = evento.Tareas.Count(t => t.Estado == "Pendiente");
        var porcentajeAvance = totalTareas > 0
            ? Math.Round(((decimal)tareasCompletadas / totalTareas) * 100m, 1)
            : 0m;

        // Cálculos financieros (COP)
        var totalGastos = evento.Gastos.Sum(g => g.Valor);
        var saldoDisponible = evento.PresupuestoBase - totalGastos;
        var porcentajeEjecucion = evento.PresupuestoBase > 0
            ? Math.Round((totalGastos / evento.PresupuestoBase) * 100m, 1)
            : 0m;
        var alertaPresupuesto = totalGastos > evento.PresupuestoBase;

        // Tareas próximas a vencer o pendientes ordenadas por urgencia
        var tareasCriticas = evento.Tareas
            .Where(t => t.Estado != "Completada")
            .OrderBy(t => t.FechaVencimiento)
            .Take(5)
            .Select(t =>
            {
                var dias = (t.FechaVencimiento.Date - hoy).Days;
                return new TareaProximaVencerDto
                {
                    Id = t.Id,
                    Nombre = t.Nombre,
                    Responsable = t.Responsable,
                    FechaVencimiento = t.FechaVencimiento,
                    Estado = t.Estado,
                    DiasRestantes = dias,
                    Vencida = dias < 0
                };
            })
            .ToList();

        return new DashboardResumenDto
        {
            EventoId = evento.Id,
            NombreEvento = evento.Nombre,
            EstadoEvento = evento.Estado,
            FechaEvento = evento.FechaEvento,
            Ubicacion = evento.Ubicacion,
            AforoEstimado = evento.AforoEstimado,
            DiasRestantesParaEvento = diasRestantes,
            TotalTareas = totalTareas,
            TareasCompletadas = tareasCompletadas,
            TareasEnProceso = tareasEnProceso,
            TareasPendientes = tareasPendientes,
            PorcentajeAvanceTareas = porcentajeAvance,
            PresupuestoAsignadoCOP = evento.PresupuestoBase,
            GastosTotalesCOP = totalGastos,
            SaldoDisponibleCOP = saldoDisponible,
            PorcentajeEjecucionPresupuesto = porcentajeEjecucion,
            AlertaPresupuestoSuperado = alertaPresupuesto,
            TareasProximasVencer = tareasCriticas
        };
    }
}
