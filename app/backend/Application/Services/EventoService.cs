using AsistenteEventos.Application.DTOs;
using AsistenteEventos.Domain.Entities;
using AsistenteEventos.Infrastructure.Data;
using AsistenteEventos.Security.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AsistenteEventos.Application.Services;

public interface IEventoService
{
    Task<List<EventoDto>> GetAllAsync();
    Task<EventoDto> GetByIdAsync(int id);
    Task<EventoDto> CreateAsync(EventoCreateDto dto);
    Task<EventoDto> UpdateAsync(int id, EventoUpdateDto dto);
    Task<bool> DeleteAsync(int id);
}

public class EventoService : IEventoService
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<EventoService> _logger;

    public EventoService(AppDbContext context, ITenantContext tenantContext, ILogger<EventoService> logger)
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

    public async Task<List<EventoDto>> GetAllAsync()
    {
        var pymeId = EnsureTenantId();

        // Filtro explícito de tenencia además del Global Query Filter (defensa en profundidad)
        return await _context.Eventos
            .Where(e => e.PymeId == pymeId)
            .OrderByDescending(e => e.FechaCreacion)
            .Select(e => new EventoDto
            {
                Id = e.Id,
                PymeId = e.PymeId,
                Nombre = e.Nombre,
                Descripcion = e.Descripcion,
                FechaEvento = e.FechaEvento,
                Ubicacion = e.Ubicacion,
                AforoEstimado = e.AforoEstimado,
                PresupuestoBase = e.PresupuestoBase,
                Estado = e.Estado,
                FechaCreacion = e.FechaCreacion
            })
            .ToListAsync();
    }

    public async Task<EventoDto> GetByIdAsync(int id)
    {
        var pymeId = EnsureTenantId();

        var evento = await _context.Eventos
            .Where(e => e.Id == id && e.PymeId == pymeId)
            .FirstOrDefaultAsync();

        if (evento == null)
        {
            // No revelar si existe o no en otra PYME
            throw new KeyNotFoundException($"El evento con ID {id} no fue encontrado.");
        }

        return new EventoDto
        {
            Id = evento.Id,
            PymeId = evento.PymeId,
            Nombre = evento.Nombre,
            Descripcion = evento.Descripcion,
            FechaEvento = evento.FechaEvento,
            Ubicacion = evento.Ubicacion,
            AforoEstimado = evento.AforoEstimado,
            PresupuestoBase = evento.PresupuestoBase,
            Estado = evento.Estado,
            FechaCreacion = evento.FechaCreacion
        };
    }

    public async Task<EventoDto> CreateAsync(EventoCreateDto dto)
    {
        var pymeId = EnsureTenantId();

        var evento = new Evento
        {
            PymeId = pymeId,
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion.Trim(),
            FechaEvento = dto.FechaEvento,
            Ubicacion = dto.Ubicacion.Trim(),
            AforoEstimado = dto.AforoEstimado,
            PresupuestoBase = dto.PresupuestoBase,
            Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "En Planificación" : dto.Estado.Trim(),
            FechaCreacion = DateTime.UtcNow
        };

        _context.Eventos.Add(evento);
        await _context.SaveChangesAsync();

        // Inicializar rubros presupuestales base estándar para PYME colombiana (Logística 30%, Insumos/Comida 40%, Publicidad 20%, Imprevistos 10%)
        if (dto.PresupuestoBase > 0)
        {
            _context.RubrosPresupuestales.AddRange(
                new RubroPresupuestal { PymeId = pymeId, EventoId = evento.Id, Categoria = "Logística", Porcentaje = 30m, ValorEstimado = dto.PresupuestoBase * 0.30m },
                new RubroPresupuestal { PymeId = pymeId, EventoId = evento.Id, Categoria = "Insumos/Comida", Porcentaje = 40m, ValorEstimado = dto.PresupuestoBase * 0.40m },
                new RubroPresupuestal { PymeId = pymeId, EventoId = evento.Id, Categoria = "Publicidad", Porcentaje = 20m, ValorEstimado = dto.PresupuestoBase * 0.20m },
                new RubroPresupuestal { PymeId = pymeId, EventoId = evento.Id, Categoria = "Imprevistos", Porcentaje = 10m, ValorEstimado = dto.PresupuestoBase * 0.10m }
            );
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Evento creado ID {Id} para PYME {PymeId}", evento.Id, pymeId);

        return new EventoDto
        {
            Id = evento.Id,
            PymeId = evento.PymeId,
            Nombre = evento.Nombre,
            Descripcion = evento.Descripcion,
            FechaEvento = evento.FechaEvento,
            Ubicacion = evento.Ubicacion,
            AforoEstimado = evento.AforoEstimado,
            PresupuestoBase = evento.PresupuestoBase,
            Estado = evento.Estado,
            FechaCreacion = evento.FechaCreacion
        };
    }

    public async Task<EventoDto> UpdateAsync(int id, EventoUpdateDto dto)
    {
        var pymeId = EnsureTenantId();

        var evento = await _context.Eventos
            .Where(e => e.Id == id && e.PymeId == pymeId)
            .FirstOrDefaultAsync();

        if (evento == null)
            throw new KeyNotFoundException($"El evento con ID {id} no fue encontrado.");

        evento.Nombre = dto.Nombre.Trim();
        evento.Descripcion = dto.Descripcion.Trim();
        evento.FechaEvento = dto.FechaEvento;
        evento.Ubicacion = dto.Ubicacion.Trim();
        evento.AforoEstimado = dto.AforoEstimado;
        evento.PresupuestoBase = dto.PresupuestoBase;
        evento.Estado = dto.Estado.Trim();

        await _context.SaveChangesAsync();

        return new EventoDto
        {
            Id = evento.Id,
            PymeId = evento.PymeId,
            Nombre = evento.Nombre,
            Descripcion = evento.Descripcion,
            FechaEvento = evento.FechaEvento,
            Ubicacion = evento.Ubicacion,
            AforoEstimado = evento.AforoEstimado,
            PresupuestoBase = evento.PresupuestoBase,
            Estado = evento.Estado,
            FechaCreacion = evento.FechaCreacion
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var pymeId = EnsureTenantId();

        var evento = await _context.Eventos
            .Where(e => e.Id == id && e.PymeId == pymeId)
            .FirstOrDefaultAsync();

        if (evento == null)
            throw new KeyNotFoundException($"El evento con ID {id} no fue encontrado.");

        _context.Eventos.Remove(evento);
        await _context.SaveChangesAsync();
        return true;
    }
}
