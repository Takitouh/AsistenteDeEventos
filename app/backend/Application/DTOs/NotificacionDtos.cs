namespace AsistenteEventos.Application.DTOs;

public record NotificacionDto
{
    public int Id { get; init; }
    public int? EventoId { get; init; }
    public string? NombreEvento { get; init; }
    public int? TareaId { get; init; }
    public string Mensaje { get; init; } = string.Empty;
    public string Tipo { get; init; } = "General"; // "TareaVencimiento", "PresupuestoExcedido", "EventoProximo", "General"
    public bool Leida { get; init; }
    public DateTime FechaCreacion { get; init; }
}
