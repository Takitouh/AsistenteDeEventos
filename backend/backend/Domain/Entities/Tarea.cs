using AsistenteEventos.Domain.Common;

namespace AsistenteEventos.Domain.Entities;

public class Tarea : BaseEntity, ITenantEntity
{
    public int PymeId { get; set; }
    public Pyme? Pyme { get; set; }

    public int EventoId { get; set; }
    public Evento? Evento { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public DateTime FechaVencimiento { get; set; }
    public string Estado { get; set; } = "Pendiente"; // "Pendiente", "En Proceso", "Completada"
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
