using AsistenteEventos.Domain.Common;
using AsistenteEventos.Domain.Entities;
using AsistenteEventos.Security.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AsistenteEventos.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Pyme> Pymes => Set<Pyme>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<Tarea> Tareas => Set<Tarea>();
    public DbSet<RubroPresupuestal> RubrosPresupuestales => Set<RubroPresupuestal>();
    public DbSet<Gasto> Gastos => Set<Gasto>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<ConversacionMensaje> ConversacionMensajes => Set<ConversacionMensaje>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de Pyme
        modelBuilder.Entity<Pyme>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NIT).IsUnique();
            entity.Property(e => e.RazonSocial).IsRequired().HasMaxLength(200);
            entity.Property(e => e.NIT).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Ciudad).HasMaxLength(100);
            entity.Property(e => e.Departamento).HasMaxLength(100);
            entity.Property(e => e.Municipio).HasMaxLength(100);
            entity.Property(e => e.Sector).HasMaxLength(100);
        });

        // Configuración de Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Correo).IsUnique();
            entity.HasIndex(e => e.PymeId);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Correo).IsRequired().HasMaxLength(150);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Rol).IsRequired().HasMaxLength(50);

            entity.HasOne(e => e.Pyme)
                  .WithMany(p => p.Usuarios)
                  .HasForeignKey(e => e.PymeId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Global Query Filter para Multi-Tenancy
            entity.HasQueryFilter(e => !_tenantContext.IsAuthenticated || e.PymeId == _tenantContext.PymeId);
        });

        // Configuración de Evento
        modelBuilder.Entity<Evento>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PymeId);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Ubicacion).HasMaxLength(200);
            entity.Property(e => e.PresupuestoBase).HasPrecision(18, 2);
            entity.Property(e => e.Estado).IsRequired().HasMaxLength(50);

            entity.HasOne(e => e.Pyme)
                  .WithMany(p => p.Eventos)
                  .HasForeignKey(e => e.PymeId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Global Query Filter para Multi-Tenancy
            entity.HasQueryFilter(e => !_tenantContext.IsAuthenticated || e.PymeId == _tenantContext.PymeId);
        });

        // Configuración de Tarea
        modelBuilder.Entity<Tarea>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PymeId);
            entity.HasIndex(e => e.EventoId);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Responsable).HasMaxLength(150);
            entity.Property(e => e.Estado).IsRequired().HasMaxLength(50);

            entity.HasOne(e => e.Evento)
                  .WithMany(ev => ev.Tareas)
                  .HasForeignKey(e => e.EventoId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Global Query Filter
            entity.HasQueryFilter(e => !_tenantContext.IsAuthenticated || e.PymeId == _tenantContext.PymeId);
        });

        // Configuración de RubroPresupuestal
        modelBuilder.Entity<RubroPresupuestal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PymeId);
            entity.HasIndex(e => e.EventoId);
            entity.Property(e => e.Categoria).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Porcentaje).HasPrecision(5, 2);
            entity.Property(e => e.ValorEstimado).HasPrecision(18, 2);

            entity.HasOne(e => e.Evento)
                  .WithMany(ev => ev.Rubros)
                  .HasForeignKey(e => e.EventoId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Global Query Filter
            entity.HasQueryFilter(e => !_tenantContext.IsAuthenticated || e.PymeId == _tenantContext.PymeId);
        });

        // Configuración de Gasto
        modelBuilder.Entity<Gasto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PymeId);
            entity.HasIndex(e => e.EventoId);
            entity.Property(e => e.Descripcion).IsRequired().HasMaxLength(250);
            entity.Property(e => e.Categoria).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Valor).HasPrecision(18, 2);

            entity.HasOne(e => e.Evento)
                  .WithMany(ev => ev.Gastos)
                  .HasForeignKey(e => e.EventoId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Global Query Filter
            entity.HasQueryFilter(e => !_tenantContext.IsAuthenticated || e.PymeId == _tenantContext.PymeId);
        });

        // Configuración de Notificacion
        modelBuilder.Entity<Notificacion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PymeId);
            entity.Property(e => e.Mensaje).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Tipo).IsRequired().HasMaxLength(50);

            entity.HasOne(e => e.Evento)
                  .WithMany(ev => ev.Notificaciones)
                  .HasForeignKey(e => e.EventoId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Global Query Filter
            entity.HasQueryFilter(e => !_tenantContext.IsAuthenticated || e.PymeId == _tenantContext.PymeId);
        });

        // Configuración de ConversacionMensaje
        modelBuilder.Entity<ConversacionMensaje>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PymeId);
            entity.HasIndex(e => e.EventoId);
            entity.Property(e => e.Rol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Contenido).IsRequired();

            entity.HasOne(e => e.Evento)
                  .WithMany(ev => ev.Mensajes)
                  .HasForeignKey(e => e.EventoId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Global Query Filter
            entity.HasQueryFilter(e => !_tenantContext.IsAuthenticated || e.PymeId == _tenantContext.PymeId);
        });
    }

    /// <summary>
    /// Sobrescribe SaveChangesAsync para garantizar que cualquier entidad agregada
    /// reciba obligatoriamente el PymeId del TenantContext autenticado.
    /// Esto ignora cualquier PymeId manipulado enviado por el cliente.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_tenantContext.IsAuthenticated && _tenantContext.PymeId.HasValue)
        {
            var tenantEntities = ChangeTracker.Entries<ITenantEntity>()
                .Where(e => e.State == EntityState.Added);

            foreach (var entry in tenantEntities)
            {
                // Forzar PymeId del contexto seguro
                entry.Entity.PymeId = _tenantContext.PymeId.Value;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
