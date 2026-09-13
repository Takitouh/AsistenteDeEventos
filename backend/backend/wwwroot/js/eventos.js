// =============================================================================
// MÓDULO GESTIÓN DE EVENTOS (RF04)
// =============================================================================

const Eventos = {
  eventosList: [],

  init() {
    this.bindEvents();
  },

  bindEvents() {
    const btnNuevo = document.getElementById('btn-nuevo-evento');
    if (btnNuevo) {
      btnNuevo.addEventListener('click', () => this.abrirModalCrear());
    }

    const formEvento = document.getElementById('form-evento-modal');
    if (formEvento) {
      formEvento.addEventListener('submit', (e) => this.handleSubmit(e));
    }
  },

  async load() {
    const tbody = document.getElementById('tabla-eventos-body');
    tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4"><span class="spinner-border spinner-border-sm text-primary"></span> Cargando eventos...</td></tr>';

    try {
      const res = await API.get('/eventos');
      this.eventosList = res.data || [];
      this.render();
    } catch (err) {
      tbody.innerHTML = `<tr><td colspan="7" class="text-center text-danger py-4">${err.message}</td></tr>`;
    }
  },

  render() {
    const tbody = document.getElementById('tabla-eventos-body');
    tbody.innerHTML = '';

    if (this.eventosList.length === 0) {
      tbody.innerHTML = '<tr><td colspan="7" class="text-center text-muted py-4"><i class="bi bi-calendar-x me-1"></i>No tienes eventos registrados aún. ¡Crea el primero o pide una propuesta en el Chat!</td></tr>';
      return;
    }

    this.eventosList.forEach(ev => {
      const tr = document.createElement('tr');

      let badgeClass = 'badge-estado-planificacion';
      if (ev.estado === 'En Proceso') badgeClass = 'badge-estado-proceso';
      if (ev.estado === 'Completado') badgeClass = 'badge-estado-completado';

      tr.innerHTML = `
        <td class="fw-bold text-dark">${ev.nombre}</td>
        <td>${API.formatDate(ev.fechaEvento)}</td>
        <td><i class="bi bi-geo-alt text-muted me-1"></i>${ev.ubicacion || 'Por definir'}</td>
        <td><i class="bi bi-people text-muted me-1"></i>${ev.aforoEstimado}</td>
        <td class="fw-semibold text-primary">${API.formatCOP(ev.presupuestoBase)}</td>
        <td><span class="badge ${badgeClass} px-2 py-1">${ev.estado}</span></td>
        <td class="text-end">
          <button class="btn btn-sm btn-outline-primary me-1" onclick="Eventos.seleccionarYVerDashboard(${ev.id})" title="Ver Dashboard">
            <i class="bi bi-speedometer2"></i>
          </button>
          <button class="btn btn-sm btn-outline-secondary me-1" onclick="Eventos.abrirModalEditar(${ev.id})" title="Editar">
            <i class="bi bi-pencil"></i>
          </button>
          <button class="btn btn-sm btn-outline-danger" onclick="Eventos.eliminar(${ev.id}, '${this.escapeHtml(ev.nombre)}')" title="Eliminar">
            <i class="bi bi-trash"></i>
          </button>
        </td>
      `;
      tbody.appendChild(tr);
    });
  },

  abrirModalCrear() {
    document.getElementById('modal-evento-titulo').textContent = 'Crear Nuevo Evento';
    document.getElementById('form-evento-id').value = '';
    document.getElementById('form-evento-nombre').value = '';
    document.getElementById('form-evento-desc').value = '';
    document.getElementById('form-evento-fecha').value = new Date(Date.now() + 15 * 24 * 60 * 60 * 1000).toISOString().slice(0, 16);
    document.getElementById('form-evento-ubicacion').value = '';
    document.getElementById('form-evento-aforo').value = '50';
    document.getElementById('form-evento-presupuesto').value = '5000000';
    document.getElementById('form-evento-estado').value = 'En Planificación';

    const modal = new bootstrap.Modal(document.getElementById('modal-evento'));
    modal.show();
  },

  abrirModalEditar(id) {
    const ev = this.eventosList.find(e => e.id === id);
    if (!ev) return;

    document.getElementById('modal-evento-titulo').textContent = 'Editar Evento';
    document.getElementById('form-evento-id').value = ev.id;
    document.getElementById('form-evento-nombre').value = ev.nombre;
    document.getElementById('form-evento-desc').value = ev.descripcion || '';
    document.getElementById('form-evento-fecha').value = new Date(ev.fechaEvento).toISOString().slice(0, 16);
    document.getElementById('form-evento-ubicacion').value = ev.ubicacion || '';
    document.getElementById('form-evento-aforo').value = ev.aforoEstimado;
    document.getElementById('form-evento-presupuesto').value = ev.presupuestoBase;
    document.getElementById('form-evento-estado').value = ev.estado;

    const modal = new bootstrap.Modal(document.getElementById('modal-evento'));
    modal.show();
  },

  async handleSubmit(e) {
    e.preventDefault();
    const id = document.getElementById('form-evento-id').value;
    const modalEl = document.getElementById('modal-evento');
    const modal = bootstrap.Modal.getInstance(modalEl);

    const payload = {
      nombre: document.getElementById('form-evento-nombre').value.trim(),
      descripcion: document.getElementById('form-evento-desc').value.trim(),
      fechaEvento: new Date(document.getElementById('form-evento-fecha').value).toISOString(),
      ubicacion: document.getElementById('form-evento-ubicacion').value.trim(),
      aforoEstimado: parseInt(document.getElementById('form-evento-aforo').value) || 0,
      presupuestoBase: parseFloat(document.getElementById('form-evento-presupuesto').value) || 0,
      estado: document.getElementById('form-evento-estado').value
    };

    try {
      if (id) {
        await API.put(`/eventos/${id}`, payload);
      } else {
        await API.post('/eventos', payload);
      }
      modal.hide();
      await App.loadEventosList();
      this.load();
    } catch (err) {
      alert('Error: ' + err.message);
    }
  },

  async eliminar(id, nombre) {
    if (!confirm(`¿Estás seguro de eliminar el evento '${nombre}'? Esta acción eliminará sus rubros, tareas y gastos asociados.`)) {
      return;
    }

    try {
      await API.delete(`/eventos/${id}`);
      await App.loadEventosList();
      this.load();
    } catch (err) {
      alert('Error al eliminar evento: ' + err.message);
    }
  },

  seleccionarYVerDashboard(id) {
    App.setSelectedEventoId(id);
    App.switchTab('dashboard');
  },

  escapeHtml(str) {
    return (str || '').replace(/'/g, "\\'");
  }
};
