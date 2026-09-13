using AsistenteEventos.Application.DTOs;
using AsistenteEventos.Domain.Entities;
using AsistenteEventos.Infrastructure.Data;
using AsistenteEventos.Security.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AsistenteEventos.Application.Services;

public interface ITareaService
{
    Task<List<TareaDto>> GetByEventoIdAsync(int eventoId);
    Task<TareaDto> GetByIdAsync(int id);
    Task<TareaDto> CreateAsync(int eventoId, TareaCreateDto dto);
    Task<TareaDto> UpdateAsync(int id, TareaUpdateDto dto);
    Task<TareaDto> UpdateEstadoAsync(int id, string nuevoEstado);
    Task<bool> DeleteAsync(int id);
}

public class TareaService : ITareaService
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TareaService> _logger;

    public TareaService(AppDbContext context, ITenantContext tenantContext, ILogger<TareaService> logger)
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

    public async Task<List<TareaDto>> GetByEventoIdAsync(int eventoId)
    {
        var pymeId = EnsureTenantId();

        // Validar que el evento pertenezca a la PYME
        var eventoExiste = await _context.Eventos
            .AnyAsync(e => e.Id == eventoId && e.PymeId == pymeId);

        if (!eventoExiste)
            throw new KeyNotFoundException($"El evento con ID {eventoId} no fue encontrado.");

        return await _context.Tareas
            .Where(t => t.EventoId == eventoId && t.PymeId == pymeId)
            .OrderBy(t => t.FechaVencimiento)
            .Select(t => new TareaDto
            {
                Id = t.Id,
                PymeId = t.PymeId,
                EventoId = t.EventoId,
                Nombre = t.Nombre,
                Descripcion = t.Descripcion,
                Responsable = t.Responsable,
                FechaVencimiento = t.FechaVencimiento,
                Estado = t.Estado,
                FechaCreacion = t.FechaCreacion
            })
            .ToListAsync();
    }

    public async Task<TareaDto> GetByIdAsync(int id)
    {
        var pymeId = EnsureTenantId();

        var tarea = await _context.Tareas
            .Where(t => t.Id == id && t.PymeId == pymeId)
            .FirstOrDefaultAsync();

        if (tarea == null)
            throw new KeyNotFoundException($"La tarea con ID {id} no fue encontrada.");

        return new TareaDto
        {
            Id = tarea.Id,
            PymeId = tarea.PymeId,
            EventoId = tarea.EventoId,
            Nombre = tarea.Nombre,
            Descripcion = tarea.Descripcion,
            Responsable = tarea.Responsable,
            FechaVencimiento = tarea.FechaVencimiento,
            Estado = tarea.Estado,
            FechaCreacion = tarea.FechaCreacion
        };
    }

    public async Task<TareaDto> CreateAsync(int eventoId, TareaCreateDto dto)
    {
        var pymeId = EnsureTenantId();

        var evento = await _context.Eventos
            .Where(e => e.Id == eventoId && e.PymeId == pymeId)
            .FirstOrDefaultAsync();

        if (evento == null)
            throw new KeyNotFoundException($"El evento con ID {eventoId} no fue encontrado.");

        var estado = string.IsNullOrWhiteSpace(dto.Estado) ? "Pendiente" : dto.Estado.Trim();
        if (estado != "Pendiente" && estado != "En Proceso" && estado != "Completada")
            estado = "Pendiente";

        var tarea = new Tarea
        {
            PymeId = pymeId,
            EventoId = eventoId,
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion.Trim(),
            Responsable = string.IsNullOrWhiteSpace(dto.Responsable) ? "Por Asignar" : dto.Responsable.Trim(),
            FechaVencimiento = dto.FechaVencimiento,
            Estado = estado,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Tareas.Add(tarea);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tarea creada ID {Id} para Evento {EventoId}", tarea.Id, eventoId);

        return new TareaDto
        {
            Id = tarea.Id,
            PymeId = tarea.PymeId,
            EventoId = tarea.EventoId,
            Nombre = tarea.Nombre,
            Descripcion = tarea.Descripcion,
            Responsable = tarea.Responsable,
            FechaVencimiento = tarea.FechaVencimiento,
            Estado = tarea.Estado,
            FechaCreacion = tarea.FechaCreacion
        };
    }

    public async Task<TareaDto> UpdateAsync(int id, TareaUpdateDto dto)
    {
        var pymeId = EnsureTenantId();

        var tarea = await _context.Tareas
            .Where(t => t.Id == id && t.PymeId == pymeId)
            .FirstOrDefaultAsync();

        if (tarea == null)
            throw new KeyNotFoundException($"La tarea con ID {id} no fue encontrada.");

        var estado = dto.Estado.Trim();
        if (estado != "Pendiente" && estado != "En Proceso" && estado != "Completada")
            estado = tarea.Estado;

        tarea.Nombre = dto.Nombre.Trim();
        tarea.Descripcion = dto.Descripcion.Trim();
        tarea.Responsable = dto.Responsable.Trim();
        tarea.FechaVencimiento = dto.FechaVencimiento;
        tarea.Estado = estado;

        await _context.SaveChangesAsync();

        return new TareaDto
        {
            Id = tarea.Id,
            PymeId = tarea.PymeId,
            EventoId = tarea.EventoId,
            Nombre = tarea.Nombre,
            Descripcion = tarea.Descripcion,
            Responsable = tarea.Responsable,
            FechaVencimiento = tarea.FechaVencimiento,
            Estado = tarea.Estado,
            FechaCreacion = tarea.FechaCreacion
        };
    }

    public async Task<TareaDto> UpdateEstadoAsync(int id, string nuevoEstado)
    {
        var pymeId = EnsureTenantId();

        var tarea = await _context.Tareas
            .Where(t => t.Id == id && t.PymeId == pymeId)
            .FirstOrDefaultAsync();

        if (tarea == null)
            throw new KeyNotFoundException($"La tarea con ID {id} no fue encontrada.");

        var est = nuevoEstado.Trim();
        if (est != "Pendiente" && est != "En Proceso" && est != "Completada")
            throw new ArgumentException("El estado debe ser: 'Pendiente', 'En Proceso' o 'Completada'.");

        tarea.Estado = est;
        await _context.SaveChangesAsync();

        return new TareaDto
        {
            Id = tarea.Id,
            PymeId = tarea.PymeId,
            EventoId = tarea.EventoId,
            Nombre = tarea.Nombre,
            Descripcion = tarea.Descripcion,
            Responsable = tarea.Responsable,
            FechaVencimiento = tarea.FechaVencimiento,
            Estado = tarea.Estado,
            FechaCreacion = tarea.FechaCreacion
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var pymeId = EnsureTenantId();

        var tarea = await _context.Tareas
            .Where(t => t.Id == id && t.PymeId == pymeId)
            .FirstOrDefaultAsync();

        if (tarea == null)
            throw new KeyNotFoundException($"La tarea con ID {id} no fue encontrada.");

        _context.Tareas.Remove(tarea);
        await _context.SaveChangesAsync();
        return true;
    }
}
