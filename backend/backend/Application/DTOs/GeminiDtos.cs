using System.Text.Json.Serialization;

namespace AsistenteEventos.Application.DTOs;

public record GeminiChatRequestDto
{
    public string Mensaje { get; init; } = string.Empty;
    public int? EventoId { get; init; }
}

public record GeminiChatResponseDto
{
    public string Respuesta { get; init; } = string.Empty;
    public bool EsPropuestaEstructurada { get; init; }
    public List<PropuestaEventoDto>? Propuestas { get; init; }
    public string DisclaimerLegal { get; init; } = "Las estimaciones económicas generadas por IA son únicamente referenciales y no constituyen cotizaciones contractuales.";
}

public record GeminiPlanRequestDto
{
    public int? EventoId { get; init; }
    public string? NombreEvento { get; init; }
    public string ObjetivoComercial { get; init; } = string.Empty;
    public string TipoEvento { get; init; } = "Lanzamiento / Comercial";
    public int Aforo { get; init; } = 50;
    public decimal PresupuestoMaximoCOP { get; init; } = 5_000_000m;
    public string Ciudad { get; init; } = string.Empty;
    public string? Restricciones { get; init; }
}

public class PropuestaRubroDto
{
    [JsonPropertyName("categoria")]
    public string Categoria { get; set; } = string.Empty;

    [JsonPropertyName("porcentaje")]
    public decimal Porcentaje { get; set; }

    [JsonPropertyName("valorEstimado")]
    public decimal ValorEstimado { get; set; }
}

public class PropuestaTareaDto
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("responsable")]
    public string Responsable { get; set; } = "Coordinador";

    [JsonPropertyName("diasAntes")]
    public int DiasAntes { get; set; } = 7;
}

public class PropuestaEventoDto
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("objetivo")]
    public string Objetivo { get; set; } = string.Empty;

    [JsonPropertyName("aforo")]
    public int Aforo { get; set; }

    [JsonPropertyName("presupuestoEstimadoCOP")]
    public decimal PresupuestoEstimadoCOP { get; set; }

    [JsonPropertyName("rubros")]
    public List<PropuestaRubroDto> Rubros { get; set; } = new();

    [JsonPropertyName("tareas")]
    public List<PropuestaTareaDto> Tareas { get; set; } = new();
}

public class PropuestasContainerDto
{
    [JsonPropertyName("propuestas")]
    public List<PropuestaEventoDto> Propuestas { get; set; } = new();
}

public record FormalizarPropuestaDto
{
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public DateTime FechaEvento { get; init; }
    public string Ubicacion { get; init; } = string.Empty;
    public int Aforo { get; init; }
    public decimal PresupuestoBase { get; init; }
    public List<PropuestaRubroDto> Rubros { get; init; } = new();
    public List<PropuestaTareaDto> Tareas { get; init; } = new();
}
