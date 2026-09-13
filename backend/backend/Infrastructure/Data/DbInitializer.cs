using AsistenteEventos.Domain.Entities;
using AsistenteEventos.Security.Authentication;
using Microsoft.EntityFrameworkCore;

namespace AsistenteEventos.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context, IPasswordHasher passwordHasher)
    {
        // Asegurar creación de base de datos
        await context.Database.EnsureCreatedAsync();

        // Verificar si ya existen PYMEs usando IgnoreQueryFilters para ver la tabla completa
        if (await context.Pymes.IgnoreQueryFilters().AnyAsync())
        {
            return; // Ya fue sembrada
        }

        var defaultPasswordHash = passwordHasher.HashPassword("Password123*");

        // -------------------------------------------------------------
        // PYME A: Innovatech Colombia S.A.S. (Bogotá)
        // -------------------------------------------------------------
        var pymeA = new Pyme
        {
            RazonSocial = "Innovatech Colombia S.A.S.",
            NIT = "901.234.567-1",
            Pais = "Colombia",
            Departamento = "Cundinamarca",
            Municipio = "Bogotá D.C.",
            Ciudad = "Bogotá",
            Sector = "Tecnología y Software",
            FechaRegistro = DateTime.UtcNow.AddMonths(-3)
        };
        context.Pymes.Add(pymeA);
        await context.SaveChangesAsync();

        var usuarioA = new Usuario
        {
            PymeId = pymeA.Id,
            Nombre = "Carlos Mendoza",
            Correo = "admin@innovatech.com.co",
            PasswordHash = defaultPasswordHash,
            Rol = "Administrador",
            Activo = true,
            FechaRegistro = DateTime.UtcNow.AddMonths(-3)
        };
        context.Usuarios.Add(usuarioA);

        var eventoA = new Evento
        {
            PymeId = pymeA.Id,
            Nombre = "Lanzamiento App Móvil B2B 2026",
            Descripcion = "Evento de presentación oficial de la nueva plataforma de gestión para clientes empresariales en Bogotá.",
            FechaEvento = DateTime.UtcNow.AddDays(20),
            Ubicacion = "Centro de Convenciones Ágora Bogotá, Salón 3",
            AforoEstimado = 120,
            PresupuestoBase = 12_000_000m, // 12 Millones COP
            Estado = "En Proceso",
            FechaCreacion = DateTime.UtcNow.AddDays(-10)
        };
        context.Eventos.Add(eventoA);
        await context.SaveChangesAsync();

        // Rubros presupuestales de Evento A
        context.RubrosPresupuestales.AddRange(
            new RubroPresupuestal { PymeId = pymeA.Id, EventoId = eventoA.Id, Categoria = "Logística", Porcentaje = 30m, ValorEstimado = 3_600_000m },
            new RubroPresupuestal { PymeId = pymeA.Id, EventoId = eventoA.Id, Categoria = "Insumos/Comida", Porcentaje = 40m, ValorEstimado = 4_800_000m },
            new RubroPresupuestal { PymeId = pymeA.Id, EventoId = eventoA.Id, Categoria = "Publicidad", Porcentaje = 20m, ValorEstimado = 2_400_000m },
            new RubroPresupuestal { PymeId = pymeA.Id, EventoId = eventoA.Id, Categoria = "Imprevistos", Porcentaje = 10m, ValorEstimado = 1_200_000m }
        );

        // Gastos reales de Evento A
        context.Gastos.AddRange(
            new Gasto { PymeId = pymeA.Id, EventoId = eventoA.Id, Descripcion = "Anticipo Alquiler Salón Ágora", Categoria = "Logística", Valor = 3_200_000m, Fecha = DateTime.UtcNow.AddDays(-5) },
            new Gasto { PymeId = pymeA.Id, EventoId = eventoA.Id, Descripcion = "Reserva Catering gourmet de bienvenida", Categoria = "Insumos/Comida", Valor = 2_100_000m, Fecha = DateTime.UtcNow.AddDays(-3) }
        );

        // Tareas de Evento A
        var tareaA1 = new Tarea { PymeId = pymeA.Id, EventoId = eventoA.Id, Nombre = "Confirmar agenda con conferencistas clave", Descripcion = "Validar temas y horarios de presentaciones", Responsable = "Carlos Mendoza", FechaVencimiento = DateTime.UtcNow.AddDays(-1), Estado = "Completada" };
        var tareaA2 = new Tarea { PymeId = pymeA.Id, EventoId = eventoA.Id, Nombre = "Diseñar material publicitario y pendones", Descripcion = "Branding con identidad visual de la PYME", Responsable = "Agencia Creativa", FechaVencimiento = DateTime.UtcNow.AddDays(3), Estado = "En Proceso" };
        var tareaA3 = new Tarea { PymeId = pymeA.Id, EventoId = eventoA.Id, Nombre = "Pruebas de sonido y streaming", Descripcion = "Verificar conectividad de internet y microfonía", Responsable = "Técnico Audiovisual", FechaVencimiento = DateTime.UtcNow.AddDays(18), Estado = "Pendiente" };
        context.Tareas.AddRange(tareaA1, tareaA2, tareaA3);

        // Notificación Evento A
        context.Notificaciones.Add(new Notificacion
        {
            PymeId = pymeA.Id,
            EventoId = eventoA.Id,
            Mensaje = "Atención: La tarea 'Diseñar material publicitario' vence en 3 días.",
            Tipo = "TareaVencimiento",
            Leida = false,
            FechaCreacion = DateTime.UtcNow
        });

        // -------------------------------------------------------------
        // PYME B: Café Del Valle S.A. (Medellín)
        // -------------------------------------------------------------
        var pymeB = new Pyme
        {
            RazonSocial = "Café Del Valle S.A.",
            NIT = "800.987.654-3",
            Pais = "Colombia",
            Departamento = "Antioquia",
            Municipio = "Medellín",
            Ciudad = "Medellín",
            Sector = "Alimentos y Bebidas",
            FechaRegistro = DateTime.UtcNow.AddMonths(-2)
        };
        context.Pymes.Add(pymeB);
        await context.SaveChangesAsync();

        var usuarioB = new Usuario
        {
            PymeId = pymeB.Id,
            Nombre = "Mariana Restrepo",
            Correo = "gerencia@cafedelvalle.co",
            PasswordHash = defaultPasswordHash,
            Rol = "Administrador",
            Activo = true,
            FechaRegistro = DateTime.UtcNow.AddMonths(-2)
        };
        context.Usuarios.Add(usuarioB);

        var eventoB = new Evento
        {
            PymeId = pymeB.Id,
            Nombre = "Feria Barista y Degustación de Café Especial",
            Descripcion = "Exposición de métodos de filtrado y cata de café de origen antioqueño.",
            FechaEvento = DateTime.UtcNow.AddDays(35),
            Ubicacion = "Plaza Mayor Medellín, Pabellón Verde",
            AforoEstimado = 80,
            PresupuestoBase = 6_500_000m, // 6.5 Millones COP
            Estado = "En Planificación",
            FechaCreacion = DateTime.UtcNow.AddDays(-4)
        };
        context.Eventos.Add(eventoB);
        await context.SaveChangesAsync();

        // Rubros presupuestales de Evento B
        context.RubrosPresupuestales.AddRange(
            new RubroPresupuestal { PymeId = pymeB.Id, EventoId = eventoB.Id, Categoria = "Logística", Porcentaje = 25m, ValorEstimado = 1_625_000m },
            new RubroPresupuestal { PymeId = pymeB.Id, EventoId = eventoB.Id, Categoria = "Insumos/Comida", Porcentaje = 45m, ValorEstimado = 2_925_000m },
            new RubroPresupuestal { PymeId = pymeB.Id, EventoId = eventoB.Id, Categoria = "Publicidad", Porcentaje = 20m, ValorEstimado = 1_300_000m },
            new RubroPresupuestal { PymeId = pymeB.Id, EventoId = eventoB.Id, Categoria = "Imprevistos", Porcentaje = 10m, ValorEstimado = 650_000m }
        );

        // Gastos reales de Evento B
        context.Gastos.Add(new Gasto
        {
            PymeId = pymeB.Id,
            EventoId = eventoB.Id,
            Descripcion = "Compra de microlotes de café para catas",
            Categoria = "Insumos/Comida",
            Valor = 1_450_000m,
            Fecha = DateTime.UtcNow.AddDays(-2)
        });

        // Tareas de Evento B
        context.Tareas.AddRange(
            new Tarea { PymeId = pymeB.Id, EventoId = eventoB.Id, Nombre = "Cotizar stands modulares en Plaza Mayor", Descripcion = "Comparar proveedores locales", Responsable = "Mariana Restrepo", FechaVencimiento = DateTime.UtcNow.AddDays(5), Estado = "En Proceso" },
            new Tarea { PymeId = pymeB.Id, EventoId = eventoB.Id, Nombre = "Invitar a baristas certificados de la región", Descripcion = "Contactar asociación de barismo", Responsable = "Coordinador Eventos", FechaVencimiento = DateTime.UtcNow.AddDays(15), Estado = "Pendiente" }
        );

        // Notificación Evento B
        context.Notificaciones.Add(new Notificacion
        {
            PymeId = pymeB.Id,
            EventoId = eventoB.Id,
            Mensaje = "Recordatorio: Se inició la planificación de la Feria Barista 2026.",
            Tipo = "General",
            Leida = false,
            FechaCreacion = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }
}
