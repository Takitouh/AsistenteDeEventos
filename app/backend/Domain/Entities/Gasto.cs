using AsistenteEventos.Domain.Common;

namespace AsistenteEventos.Domain.Entities;

public class Gasto : BaseEntity, ITenantEntity
{
    public int PymeId { get; set; }
    public Pyme? Pyme { get; set; }

    public int EventoId { get; set; }
    public Evento? Evento { get; set; }

    public string Descripcion { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal Valor { get; set; } // En Pesos Colombianos (COP)
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
