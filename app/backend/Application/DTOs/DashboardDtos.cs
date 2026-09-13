namespace AsistenteEventos.Application.DTOs;

public record TareaProximaVencerDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Responsable { get; init; } = string.Empty;
    public DateTime FechaVencimiento { get; init; }
    public string Estado { get; init; } = string.Empty;
    public int DiasRestantes { get; init; }
    public bool Vencida { get; init; }
}

public record DashboardResumenDto
{
    public int EventoId { get; init; }
    public string NombreEvento { get; init; } = string.Empty;
    public string EstadoEvento { get; init; } = "En Planificación";
    public DateTime FechaEvento { get; init; }
    public string Ubicacion { get; init; } = string.Empty;
    public int AforoEstimado { get; init; }
    public int DiasRestantesParaEvento { get; init; }

    // Progreso de Tareas
    public int TotalTareas { get; init; }
    public int TareasCompletadas { get; init; }
    public int TareasEnProceso { get; init; }
    public int TareasPendientes { get; init; }
    public decimal PorcentajeAvanceTareas { get; init; }

    // Métricas Financieras (COP)
    public decimal PresupuestoAsignadoCOP { get; init; }
    public decimal GastosTotalesCOP { get; init; }
    public decimal SaldoDisponibleCOP { get; init; }
    public decimal PorcentajeEjecucionPresupuesto { get; init; }
    public bool AlertaPresupuestoSuperado { get; init; }

    // Tareas críticas
    public List<TareaProximaVencerDto> TareasProximasVencer { get; init; } = new();

    // Mensaje de descargo legal
    public string DisclaimerLegal { get; init; } = "Las estimaciones económicas generadas por IA son únicamente referenciales y no constituyen cotizaciones contractuales.";
}
