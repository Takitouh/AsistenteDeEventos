using AsistenteEventos.Domain.Common;

namespace AsistenteEventos.Domain.Entities;

public class Pyme : BaseEntity
{
    public string RazonSocial { get; set; } = string.Empty;
    public string NIT { get; set; } = string.Empty;
    public string Pais { get; set; } = "Colombia";
    public string Departamento { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    // Relaciones de navegación
    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    public ICollection<Evento> Eventos { get; set; } = new List<Evento>();
}
