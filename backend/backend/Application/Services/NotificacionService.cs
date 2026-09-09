using AsistenteEventos.Application.DTOs;
using AsistenteEventos.Domain.Entities;
using AsistenteEventos.Infrastructure.Data;
using AsistenteEventos.Security.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AsistenteEventos.Application.Services;

public interface INotificacionService
{
    Task<List<NotificacionDto>> GetAllAsync(bool soloNoLeidas = false);
    Task<bool> MarcarComoLeidaAsync(int id);
    Task<int> GenerarRecordatoriosAutomaticosAsync();
}

public class NotificacionService : INotificacionService
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<NotificacionService> _logger;

    public NotificacionService(AppDbContext context, ITenantContext tenantContext, ILogger<NotificacionService> logger)
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

    public async Task<List<NotificacionDto>> GetAllAsync(bool soloNoLeidas = false)
    {
        var pymeId = EnsureTenantId();

        var query = _context.Notificaciones
            .Include(n => n.Evento)
            .Where(n => n.PymeId == pymeId);

        if (soloNoLeidas)
            query = query.Where(n => !n.Leida);

        return await query
            .OrderByDescending(n => n.FechaCreacion)
            .Select(n => new NotificacionDto
            {
                Id = n.Id,
                EventoId = n.EventoId,
                NombreEvento = n.Evento != null ? n.Evento.Nombre : null,
                TareaId = n.TareaId,
                Mensaje = n.Mensaje,
                Tipo = n.Tipo,
                Leida = n.Leida,
                FechaCreacion = n.FechaCreacion
            })
            .ToListAsync();
    }

    public async Task<bool> MarcarComoLeidaAsync(int id)
    {
        var pymeId = EnsureTenantId();

        var notif = await _context.Notificaciones
            .Where(n => n.Id == id && n.PymeId == pymeId)
            .FirstOrDefaultAsync();

        if (notif == null)
            throw new KeyNotFoundException($"La notificación con ID {id} no fue encontrada.");

        notif.Leida = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GenerarRecordatoriosAutomaticosAsync()
    {
        var pymeId = EnsureTenantId();
        var hoy = DateTime.UtcNow.Date;
        var limite3Dias = hoy.AddDays(3);
        int generadas = 0;

        // 1. Tareas próximas a vencer o vencidas sin completar
        var tareasCriticas = await _context.Tareas
            .Include(t => t.Evento)
            .Where(t => t.PymeId == pymeId && t.Estado != "Completada" && t.FechaVencimiento.Date <= limite3Dias)
            .ToListAsync();

        foreach (var tarea in tareasCriticas)
        {
            var yaNotificada = await _context.Notificaciones
                .AnyAsync(n => n.PymeId == pymeId && n.TareaId == tarea.Id && !n.Leida);

            if (!yaNotificada)
            {
                var dias = (tarea.FechaVencimiento.Date - hoy).Days;
                string texto = dias < 0
                    ? $"URGENTE: La tarea '{tarea.Nombre}' del evento '{tarea.Evento?.Nombre}' venció hace {Math.Abs(dias)} días."
                    : (dias == 0
                        ? $"HOY: La tarea '{tarea.Nombre}' del evento '{tarea.Evento?.Nombre}' vence hoy."
                        : $"RECORDATORIO: La tarea '{tarea.Nombre}' del evento '{tarea.Evento?.Nombre}' vence en {dias} días.");

                _context.Notificaciones.Add(new Notificacion
                {
                    PymeId = pymeId,
                    EventoId = tarea.EventoId,
                    TareaId = tarea.Id,
                    Mensaje = texto,
                    Tipo = "TareaVencimiento",
                    Leida = false,
                    FechaCreacion = DateTime.UtcNow
                });
                generadas++;
            }
        }

        // 2. Eventos próximos (en menos de 7 días)
        var eventosProximos = await _context.Eventos
            .Where(e => e.PymeId == pymeId && e.Estado != "Completado" && e.FechaEvento.Date >= hoy && e.FechaEvento.Date <= hoy.AddDays(7))
            .ToListAsync();

        foreach (var evento in eventosProximos)
        {
            var yaNotificado = await _context.Notificaciones
                .AnyAsync(n => n.PymeId == pymeId && n.EventoId == evento.Id && n.Tipo == "EventoProximo" && !n.Leida);

            if (!yaNotificado)
            {
                var dias = (evento.FechaEvento.Date - hoy).Days;
                _context.Notificaciones.Add(new Notificacion
                {
                    PymeId = pymeId,
                    EventoId = evento.Id,
                    Mensaje = $"¡Faltan solo {dias} días para tu evento '{evento.Nombre}'!",
                    Tipo = "EventoProximo",
                    Leida = false,
                    FechaCreacion = DateTime.UtcNow
                });
                generadas++;
            }
        }

        if (generadas > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Se generaron {Count} notificaciones automáticas para la PYME {PymeId}", generadas, pymeId);
        }

        return generadas;
    }
}
