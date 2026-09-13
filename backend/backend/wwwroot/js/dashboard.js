// =============================================================================
// MÓDULO DASHBOARD EJECUTIVO (RF08)
// =============================================================================

const Dashboard = {
  currentEventoId: null,

  async load(eventoId = null) {
    const container = document.getElementById('dashboard-content');
    const emptyState = document.getElementById('dashboard-empty');

    try {
      const endpoint = eventoId ? `/dashboard/${eventoId}` : '/dashboard';
      const res = await API.get(endpoint);
      const data = res.data;

      if (!data) {
        container.classList.add('d-none');
        emptyState.classList.remove('d-none');
        return;
      }

      emptyState.classList.add('d-none');
      container.classList.remove('d-none');
      this.currentEventoId = data.eventoId;

      this.renderKPIs(data);
      this.renderTareasCriticas(data.tareasProximasVencer);
    } catch (err) {
      console.error('Error al cargar dashboard:', err);
    }
  },

  renderKPIs(d) {
    // Título y Estado
    document.getElementById('dash-evento-nombre').textContent = d.nombreEvento;
    document.getElementById('dash-evento-ubicacion').textContent = d.ubicacion || 'Ubicación por definir';
    document.getElementById('dash-evento-fecha').textContent = API.formatDate(d.fechaEvento);
    document.getElementById('dash-evento-aforo').textContent = `${d.aforoEstimado} personas`;
    document.getElementById('dash-evento-dias').textContent = d.diasRestantesParaEvento >= 0 
      ? `Faltan ${d.diasRestantesParaEvento} días` 
      : `Finalizó hace ${Math.abs(d.diasRestantesParaEvento)} días`;

    const badgeEstado = document.getElementById('dash-evento-estado');
    badgeEstado.textContent = d.estadoEvento;
    badgeEstado.className = 'badge px-3 py-2 rounded-pill ';
    if (d.estadoEvento === 'En Planificación') badgeEstado.classList.add('badge-estado-planificacion');
    else if (d.estadoEvento === 'En Proceso') badgeEstado.classList.add('badge-estado-proceso');
    else badgeEstado.classList.add('badge-estado-completado');

    // Finanzas COP
    document.getElementById('dash-presupuesto-base').textContent = API.formatCOP(d.presupuestoAsignadoCOP);
    document.getElementById('dash-gastos-totales').textContent = API.formatCOP(d.gastosTotalesCOP);
    document.getElementById('dash-saldo-disponible').textContent = API.formatCOP(d.saldoDisponibleCOP);
    document.getElementById('dash-porcentaje-ejecucion').textContent = `${d.porcentajeEjecucionPresupuesto}%`;

    // Barra de progreso financiero
    const barEjecucion = document.getElementById('dash-bar-ejecucion');
    barEjecucion.style.width = `${Math.min(d.porcentajeEjecucionPresupuesto, 100)}%`;
    barEjecucion.className = 'progress-bar ';
    if (d.alertaPresupuestoSuperado) {
      barEjecucion.classList.add('bg-danger');
      document.getElementById('dash-alerta-box').classList.remove('d-none');
      document.getElementById('dash-alerta-box').textContent = `¡Sobrecosto Detectado! Los gastos superan el presupuesto asignado.`;
    } else if (d.porcentajeEjecucionPresupuesto >= 90) {
      barEjecucion.classList.add('bg-warning');
      document.getElementById('dash-alerta-box').classList.remove('d-none');
      document.getElementById('dash-alerta-box').textContent = `Precaución: Has alcanzado el ${d.porcentajeEjecucionPresupuesto}% del presupuesto.`;
    } else {
      barEjecucion.classList.add('bg-primary');
      document.getElementById('dash-alerta-box').classList.add('d-none');
    }

    // Tareas
    document.getElementById('dash-tareas-progreso-texto').textContent = `${d.tareasCompletadas} de ${d.totalTareas} completadas`;
    document.getElementById('dash-tareas-porcentaje').textContent = `${d.porcentajeAvanceTareas}%`;
    document.getElementById('dash-bar-tareas').style.width = `${d.porcentajeAvanceTareas}%`;
  },

  renderTareasCriticas(tareas) {
    const tbody = document.getElementById('dash-tabla-tareas-body');
    tbody.innerHTML = '';

    if (!tareas || tareas.length === 0) {
      tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-3"><i class="bi bi-check-all me-1"></i>No hay tareas pendientes próximas a vencer</td></tr>';
      return;
    }

    tareas.forEach(t => {
      const tr = document.createElement('tr');
      let diasBadge = t.vencida 
        ? `<span class="badge bg-danger">Venció hace ${Math.abs(t.diasRestantes)}d</span>`
        : (t.diasRestantes === 0 
          ? `<span class="badge bg-warning text-dark">Vence Hoy</span>`
          : `<span class="badge bg-info text-dark">En ${t.diasRestantes} días</span>`);

      tr.innerHTML = `
        <td class="fw-semibold">${t.nombre}</td>
        <td><span class="badge bg-light text-dark border">${t.responsable || 'Sin asignar'}</span></td>
        <td>${API.formatDate(t.fechaVencimiento)} ${diasBadge}</td>
        <td><span class="badge bg-secondary">${t.estado}</span></td>
      `;
      tbody.appendChild(tr);
    });
  }
};
