namespace AsistenteEventos.Application.DTOs;

public record TareaDto
{
    public int Id { get; init; }
    public int PymeId { get; init; }
    public int EventoId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public string Responsable { get; init; } = string.Empty;
    public DateTime FechaVencimiento { get; init; }
    public string Estado { get; init; } = "Pendiente"; // "Pendiente", "En Proceso", "Completada"
    public DateTime FechaCreacion { get; init; }
}

public record TareaCreateDto
{
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public string Responsable { get; init; } = string.Empty;
    public DateTime FechaVencimiento { get; init; }
    public string Estado { get; init; } = "Pendiente";
}

public record TareaUpdateDto
{
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public string Responsable { get; init; } = string.Empty;
    public DateTime FechaVencimiento { get; init; }
    public string Estado { get; init; } = "Pendiente";
}

public record TareaEstadoUpdateDto
{
    public string Estado { get; init; } = "Pendiente"; // "Pendiente", "En Proceso", "Completada"
}
