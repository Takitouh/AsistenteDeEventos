using AsistenteEventos.Domain.Common;

namespace AsistenteEventos.Domain.Entities;

public class Evento : BaseEntity, ITenantEntity
{
    public int PymeId { get; set; }
    public Pyme? Pyme { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaEvento { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public int AforoEstimado { get; set; }
    public decimal PresupuestoBase { get; set; } // En Pesos Colombianos (COP)
    public string Estado { get; set; } = "En Planificación"; // "En Planificación", "En Proceso", "Completado"
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Relaciones de navegación
    public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
    public ICollection<RubroPresupuestal> Rubros { get; set; } = new List<RubroPresupuestal>();
    public ICollection<Gasto> Gastos { get; set; } = new List<Gasto>();
    public ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();
    public ICollection<ConversacionMensaje> Mensajes { get; set; } = new List<ConversacionMensaje>();
}
