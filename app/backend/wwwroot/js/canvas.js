// =============================================================================
// MÓDULO CANVAS DE PLANIFICACIÓN (RF03)
// =============================================================================

const Canvas = {
  propuestasActuales: [],

  init() {
    this.bindEvents();
  },

  bindEvents() {
    const btnGenerarCanvas = document.getElementById('btn-generar-canvas');
    if (btnGenerarCanvas) {
      btnGenerarCanvas.addEventListener('click', () => this.abrirModalGenerar());
    }

    const formGenerar = document.getElementById('form-generar-propuestas');
    if (formGenerar) {
      formGenerar.addEventListener('submit', (e) => this.handleGenerarPropuestas(e));
    }
  },

  setPropuestasTemporales(propuestas) {
    this.propuestasActuales = propuestas;
    this.render();
  },

  abrirModalGenerar() {
    const modalEl = document.getElementById('modal-generar-propuestas');
    const modal = new bootstrap.Modal(modalEl);
    modal.show();
  },

  async handleGenerarPropuestas(e) {
    e.preventDefault();
    const btnSubmit = document.getElementById('btn-submit-generar');
    const modalEl = document.getElementById('modal-generar-propuestas');
    const modal = bootstrap.Modal.getInstance(modalEl);

    const payload = {
      objetivoComercial: document.getElementById('gen-objetivo').value.trim(),
      tipoEvento: document.getElementById('gen-tipo').value.trim(),
      aforo: parseInt(document.getElementById('gen-aforo').value) || 50,
      presupuestoMaximoCOP: parseFloat(document.getElementById('gen-presupuesto').value) || 5000000,
      ciudad: document.getElementById('gen-ciudad').value.trim(),
      restricciones: document.getElementById('gen-restricciones').value.trim()
    };

    btnSubmit.disabled = true;
    btnSubmit.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Gemini generando escenarios en COP...';

    try {
      const res = await API.post('/gemini/planificar', payload);
      this.propuestasActuales = res.data.propuestas || [];
      this.render();
      if (modal) modal.hide();
    } catch (err) {
      alert('Error al generar propuestas: ' + err.message);
    } finally {
      btnSubmit.disabled = false;
      btnSubmit.innerHTML = '<i class="bi bi-magic me-1"></i>Generar Propuestas con IA';
    }
  },

  render() {
    const container = document.getElementById('canvas-cards-container');
    const emptyState = document.getElementById('canvas-empty-state');

    if (!this.propuestasActuales || this.propuestasActuales.length === 0) {
      container.classList.add('d-none');
      emptyState.classList.remove('d-none');
      return;
    }

    emptyState.classList.add('d-none');
    container.classList.remove('d-none');
    container.innerHTML = '';

    this.propuestasActuales.forEach((p, index) => {
      const col = document.createElement('div');
      col.className = 'col-lg-6 mb-4';

      let rubrosHtml = '';
      if (p.rubros && p.rubros.length > 0) {
        rubrosHtml = p.rubros.map(r => `
          <div class="rubro-row">
            <span class="fw-semibold text-secondary">${r.categoria} (${r.porcentaje}%):</span>
            <span class="fw-bold text-dark">${API.formatCOP(r.valorEstimado)}</span>
          </div>
        `).join('');
      }

      let tareasHtml = '';
      if (p.tareas && p.tareas.length > 0) {
        tareasHtml = p.tareas.map(t => `
          <li class="small mb-1">
            <strong>${t.nombre}</strong> <span class="text-muted">(${t.responsable || 'Coordinador'} - ${t.diasAntes || 7} días antes)</span>
          </li>
        `).join('');
      }

      col.innerHTML = `
        <div class="propuesta-card h-100">
          <div class="d-flex justify-content-between align-items-start mb-2">
            <span class="badge bg-primary-subtle text-primary fw-bold px-2 py-1">Propuesta ${index + 1}</span>
            <span class="badge bg-dark text-white px-2 py-1"><i class="bi bi-people me-1"></i>Aforo: ${p.aforo} pers.</span>
          </div>
          <h4 class="h5 fw-bold text-dark">${p.nombre}</h4>
          <p class="text-muted small mb-3">${p.descripcion}</p>

          <div class="card bg-light border-0 p-3 mb-3">
            <div class="d-flex justify-content-between align-items-center mb-2">
              <span class="small text-uppercase fw-bold text-muted">Presupuesto Estimado</span>
              <span class="h5 fw-bold text-primary mb-0">${API.formatCOP(p.presupuestoEstimadoCOP)}</span>
            </div>
            <div class="rubros-container">
              ${rubrosHtml}
            </div>
          </div>

          <div class="mb-3">
            <h6 class="small fw-bold text-uppercase text-secondary mb-2"><i class="bi bi-check2-square me-1"></i>Cronograma de Tareas Clave:</h6>
            <ul class="ps-3 mb-0">
              ${tareasHtml}
            </ul>
          </div>

          <div class="disclaimer-ia mb-3">
            <i class="bi bi-info-circle"></i> Estimación económica referencial (COP), no contractual.
          </div>

          <div class="mt-auto pt-2 border-top">
            <button class="btn btn-success w-100 fw-semibold" onclick="Canvas.formalizarPropuesta(${index})">
              <i class="bi bi-check-circle me-1"></i>Formalizar esta Propuesta en mi Planificación
            </button>
          </div>
        </div>
      `;

      container.appendChild(col);
    });
  },

  async formalizarPropuesta(index) {
    const propuesta = this.propuestasActuales[index];
    if (!propuesta) return;

    if (!confirm(`¿Deseas formalizar el evento '${propuesta.nombre}'? Se registrará automáticamente en tu base de datos con sus rubros y tareas.`)) {
      return;
    }

    const payload = {
      nombre: propuesta.nombre,
      descripcion: propuesta.descripcion,
      fechaEvento: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(), // 30 días a futuro
      ubicacion: 'Locación en ciudad de la PYME (Por definir)',
      aforo: propuesta.aforo,
      presupuestoBase: propuesta.presupuestoEstimadoCOP,
      rubros: propuesta.rubros || [],
      tareas: propuesta.tareas || []
    };

    try {
      const res = await API.post('/gemini/formalizar', payload);
      alert(`¡Evento formalizado con éxito! Se ha creado el evento con ID ${res.data.id}.`);
      
      // Recargar lista de eventos y cambiar al Dashboard
      await App.loadEventosList();
      App.setSelectedEventoId(res.data.id);
      App.switchTab('dashboard');
    } catch (err) {
      alert('Error al formalizar la propuesta: ' + err.message);
    }
  }
};
