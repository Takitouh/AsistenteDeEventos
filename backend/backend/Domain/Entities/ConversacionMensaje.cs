using AsistenteEventos.Domain.Common;

namespace AsistenteEventos.Domain.Entities;

public class ConversacionMensaje : BaseEntity, ITenantEntity
{
    public int PymeId { get; set; }
    public Pyme? Pyme { get; set; }

    public int? EventoId { get; set; }
    public Evento? Evento { get; set; }

    public string Rol { get; set; } = "user"; // "user", "model", "system"
    public string Contenido { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
