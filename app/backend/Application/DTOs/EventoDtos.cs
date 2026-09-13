namespace AsistenteEventos.Application.DTOs;

public record EventoDto
{
    public int Id { get; init; }
    public int PymeId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public DateTime FechaEvento { get; init; }
    public string Ubicacion { get; init; } = string.Empty;
    public int AforoEstimado { get; init; }
    public decimal PresupuestoBase { get; init; } // En COP
    public string Estado { get; init; } = "En Planificación";
    public DateTime FechaCreacion { get; init; }
}

public record EventoCreateDto
{
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public DateTime FechaEvento { get; init; }
    public string Ubicacion { get; init; } = string.Empty;
    public int AforoEstimado { get; init; }
    public decimal PresupuestoBase { get; init; } // En COP
    public string Estado { get; init; } = "En Planificación";
}

public record EventoUpdateDto
{
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public DateTime FechaEvento { get; init; }
    public string Ubicacion { get; init; } = string.Empty;
    public int AforoEstimado { get; init; }
    public decimal PresupuestoBase { get; init; } // En COP
    public string Estado { get; init; } = "En Planificación";
}
