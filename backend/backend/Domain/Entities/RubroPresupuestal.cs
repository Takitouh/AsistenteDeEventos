using AsistenteEventos.Domain.Common;

namespace AsistenteEventos.Domain.Entities;

public class RubroPresupuestal : BaseEntity, ITenantEntity
{
    public int PymeId { get; set; }
    public Pyme? Pyme { get; set; }

    public int EventoId { get; set; }
    public Evento? Evento { get; set; }

    public string Categoria { get; set; } = string.Empty; // "Logística", "Insumos/Comida", "Publicidad", "Imprevistos"
    public decimal Porcentaje { get; set; } // Ejemplo: 30.00
    public decimal ValorEstimado { get; set; } // En Pesos Colombianos (COP)
}
