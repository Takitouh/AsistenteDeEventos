namespace AsistenteEventos.Application.DTOs;

public record RubroPresupuestalDto
{
    public int Id { get; init; }
    public int EventoId { get; init; }
    public string Categoria { get; init; } = string.Empty; // "Logística", "Insumos/Comida", "Publicidad", "Imprevistos"
    public decimal Porcentaje { get; init; }
    public decimal ValorEstimado { get; init; } // En COP
    public decimal TotalGastadoEnCategoria { get; init; } // Calculado en base a gastos registrados
}

public record RubroItemUpdateDto
{
    public string Categoria { get; init; } = string.Empty;
    public decimal Porcentaje { get; init; }
    public decimal ValorEstimado { get; init; }
}

public record RubrosBulkUpdateDto
{
    public List<RubroItemUpdateDto> Rubros { get; init; } = new();
}

public record GastoDto
{
    public int Id { get; init; }
    public int EventoId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public string Categoria { get; init; } = string.Empty;
    public decimal Valor { get; init; } // En COP
    public DateTime Fecha { get; init; }
}

public record GastoCreateDto
{
    public string Descripcion { get; init; } = string.Empty;
    public string Categoria { get; init; } = string.Empty;
    public decimal Valor { get; init; } // En COP
    public DateTime? Fecha { get; init; }
}

public record GastoUpdateDto
{
    public string Descripcion { get; init; } = string.Empty;
    public string Categoria { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public DateTime Fecha { get; init; }
}

public record PresupuestoResumenDto
{
    public int EventoId { get; init; }
    public string NombreEvento { get; init; } = string.Empty;
    public decimal PresupuestoAsignadoCOP { get; init; }
    public decimal GastosRealesTotalesCOP { get; init; }
    public decimal SaldoDisponibleCOP { get; init; }
    public decimal PorcentajeEjecucion { get; init; }
    public bool SuperaPresupuesto { get; init; }
    public string AlertaMensaje { get; init; } = string.Empty;
    public string DisclaimerLegal { get; init; } = "Las estimaciones económicas generadas por IA son únicamente referenciales y no constituyen cotizaciones contractuales.";
    public List<RubroPresupuestalDto> Rubros { get; init; } = new();
    public List<GastoDto> UltimosGastos { get; init; } = new();
}
