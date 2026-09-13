namespace AsistenteEventos.Domain.Common;

/// <summary>
/// Contrato para todas las entidades pertenecientes a una PYME.
/// Garantiza el aislamiento estricto multi-tenant a nivel de dominio y base de datos.
/// </summary>
public interface ITenantEntity
{
    int PymeId { get; set; }
}

public abstract class BaseEntity
{
    public int Id { get; set; }
}
