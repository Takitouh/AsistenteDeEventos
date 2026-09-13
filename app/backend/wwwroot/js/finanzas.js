// =============================================================================
// MÓDULO FINANZAS: PRESUPUESTO Y GASTOS EN COP (RF05)
// =============================================================================

const Finanzas = {
  currentEventoId: null,

  init() {
    this.bindEvents();
  },

  bindEvents() {
    const btnNuevoGasto = document.getElementById('btn-nuevo-gasto');
    if (btnNuevoGasto) {
      btnNuevoGasto.addEventListener('click', () => this.abrirModalGasto());
    }

    const formGasto = document.getElementById('form-gasto-modal');
    if (formGasto) {
      formGasto.addEventListener('submit', (e) => this.handleGastoSubmit(e));
    }
  },

  async load(eventoId = null) {
    const id = eventoId || App.getSelectedEventoId();
    if (!id) {
      document.getElementById('finanzas-empty').classList.remove('d-none');
      document.getElementById('finanzas-content').classList.add('d-none');
      return;
    }

    this.currentEventoId = id;
    document.getElementById('finanzas-empty').classList.add('d-none');
    document.getElementById('finanzas-content').classList.remove('d-none');

    try {
      const res = await API.get(`/eventos/${id}/presupuesto`);
      const data = res.data;
      this.renderPresupuesto(data);
      this.loadGastos(id);
    } catch (err) {
      console.error('Error al cargar finanzas:', err);
    }
  },

  renderPresupuesto(d) {
    document.getElementById('fin-nombre-evento').textContent = d.nombreEvento;
    document.getElementById('fin-presupuesto-base').textContent = API.formatCOP(d.presupuestoAsignadoCOP);
    document.getElementById('fin-gastos-totales').textContent = API.formatCOP(d.gastosRealesTotalesCOP);
    document.getElementById('fin-saldo-disponible').textContent = API.formatCOP(d.saldoDisponibleCOP);
    document.getElementById('fin-porcentaje-ejecucion').textContent = `${d.porcentajeEjecucion}%`;

    const alertaBox = document.getElementById('fin-alerta-box');
    if (d.superaPresupuesto) {
      alertaBox.className = 'alert alert-danger mb-4';
      alertaBox.innerHTML = `<i class="bi bi-exclamation-triangle-fill me-2"></i>${d.alertaMensaje}`;
      alertaBox.classList.remove('d-none');
    } else if (d.alertaMensaje) {
      alertaBox.className = 'alert alert-warning mb-4';
      alertaBox.innerHTML = `<i class="bi bi-exclamation-circle-fill me-2"></i>${d.alertaMensaje}`;
      alertaBox.classList.remove('d-none');
    } else {
      alertaBox.classList.add('d-none');
    }

    // Renderizar Rubros
    const containerRubros = document.getElementById('fin-rubros-container');
    containerRubros.innerHTML = '';

    if (!d.rubros || d.rubros.length === 0) {
      containerRubros.innerHTML = '<p class="text-muted small">No hay rubros presupuestales definidos aún.</p>';
      return;
    }

    d.rubros.forEach(r => {
      const pctGastado = r.valorEstimado > 0 
        ? Math.round((r.totalGastadoEnCategoria / r.valorEstimado) * 100) 
        : 0;

      const col = document.createElement('div');
      col.className = 'col-md-6 mb-3';
      col.innerHTML = `
        <div class="card p-3 border">
          <div class="d-flex justify-content-between align-items-center mb-1">
            <span class="fw-bold text-dark">${r.categoria}</span>
            <span class="badge bg-secondary">${r.porcentaje}% Asignado</span>
          </div>
          <div class="d-flex justify-content-between small text-muted mb-2">
            <span>Presupuestado: <strong>${API.formatCOP(r.valorEstimado)}</strong></span>
            <span>Gastado: <strong class="${pctGastado > 100 ? 'text-danger' : 'text-primary'}">${API.formatCOP(r.totalGastadoEnCategoria)}</strong></span>
          </div>
          <div class="progress" style="height: 8px;">
            <div class="progress-bar ${pctGastado > 100 ? 'bg-danger' : 'bg-primary'}" style="width: ${Math.min(pctGastado, 100)}%;"></div>
          </div>
          <div class="text-end mt-1">
            <span class="badge ${pctGastado > 100 ? 'bg-danger' : 'bg-light text-muted border'}">${pctGastado}% ejecutado</span>
          </div>
        </div>
      `;
      containerRubros.appendChild(col);
    });
  },

  async loadGastos(eventoId) {
    const tbody = document.getElementById('tabla-gastos-body');
    tbody.innerHTML = '<tr><td colspan="5" class="text-center py-3"><span class="spinner-border spinner-border-sm"></span> Cargando gastos...</td></tr>';

    try {
      const res = await API.get(`/eventos/${eventoId}/gastos`);
      const gastos = res.data || [];
      tbody.innerHTML = '';

      if (gastos.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-3">No hay gastos reales registrados aún para este evento.</td></tr>';
        return;
      }

      gastos.forEach(g => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
          <td>${API.formatDate(g.fecha)}</td>
          <td class="fw-semibold">${g.descripcion}</td>
          <td><span class="badge bg-light text-dark border">${g.categoria}</span></td>
          <td class="fw-bold text-danger">${API.formatCOP(g.valor)}</td>
          <td class="text-end">
            <button class="btn btn-sm btn-outline-danger" onclick="Finanzas.eliminarGasto(${g.id})" title="Eliminar gasto">
              <i class="bi bi-trash"></i>
            </button>
          </td>
        `;
        tbody.appendChild(tr);
      });
    } catch (err) {
      tbody.innerHTML = `<tr><td colspan="5" class="text-center text-danger py-3">${err.message}</td></tr>`;
    }
  },

  abrirModalGasto() {
    if (!this.currentEventoId) {
      alert('Por favor selecciona un evento primero.');
      return;
    }

    document.getElementById('form-gasto-desc').value = '';
    document.getElementById('form-gasto-categoria').value = 'Logística';
    document.getElementById('form-gasto-valor').value = '';
    document.getElementById('form-gasto-fecha').value = new Date().toISOString().slice(0, 10);

    const modal = new bootstrap.Modal(document.getElementById('modal-gasto'));
    modal.show();
  },

  async handleGastoSubmit(e) {
    e.preventDefault();
    const modalEl = document.getElementById('modal-gasto');
    const modal = bootstrap.Modal.getInstance(modalEl);

    const payload = {
      descripcion: document.getElementById('form-gasto-desc').value.trim(),
      categoria: document.getElementById('form-gasto-categoria').value,
      valor: parseFloat(document.getElementById('form-gasto-valor').value) || 0,
      fecha: new Date(document.getElementById('form-gasto-fecha').value).toISOString()
    };

    try {
      await API.post(`/eventos/${this.currentEventoId}/gastos`, payload);
      modal.hide();
      await this.load(this.currentEventoId);
      // Actualizar también el contador de notificaciones si hubo alerta presupuestal
      Notificaciones.load();
    } catch (err) {
      alert('Error al registrar gasto: ' + err.message);
    }
  },

  async eliminarGasto(id) {
    if (!confirm('¿Deseas eliminar este registro de gasto? El saldo disponible del evento se recalculará automáticamente.')) {
      return;
    }

    try {
      await API.delete(`/gastos/${id}`);
      await this.load(this.currentEventoId);
    } catch (err) {
      alert('Error al eliminar gasto: ' + err.message);
    }
  }
};
