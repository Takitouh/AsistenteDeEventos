using AsistenteEventos.Domain.Common;

namespace AsistenteEventos.Domain.Entities;

public class Notificacion : BaseEntity, ITenantEntity
{
    public int PymeId { get; set; }
    public Pyme? Pyme { get; set; }

    public int? EventoId { get; set; }
    public Evento? Evento { get; set; }

    public int? TareaId { get; set; }
    public Tarea? Tarea { get; set; }

    public string Mensaje { get; set; } = string.Empty;
    public string Tipo { get; set; } = "General"; // "TareaVencimiento", "PresupuestoExcedido", "EventoProximo", "General"
    public bool Leida { get; set; } = false;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
