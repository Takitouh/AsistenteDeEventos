// =============================================================================
// MÓDULO GESTIÓN DE TAREAS (RF06)
// =============================================================================

const Tareas = {
  currentEventoId: null,
  tareasList: [],

  init() {
    this.bindEvents();
  },

  bindEvents() {
    const btnNuevaTarea = document.getElementById('btn-nueva-tarea');
    if (btnNuevaTarea) {
      btnNuevaTarea.addEventListener('click', () => this.abrirModalCrear());
    }

    const formTarea = document.getElementById('form-tarea-modal');
    if (formTarea) {
      formTarea.addEventListener('submit', (e) => this.handleSubmit(e));
    }
  },

  async load(eventoId = null) {
    const id = eventoId || App.getSelectedEventoId();
    if (!id) {
      document.getElementById('tareas-empty').classList.remove('d-none');
      document.getElementById('tareas-content').classList.add('d-none');
      return;
    }

    this.currentEventoId = id;
    document.getElementById('tareas-empty').classList.add('d-none');
    document.getElementById('tareas-content').classList.remove('d-none');

    try {
      const res = await API.get(`/eventos/${id}/tareas`);
      this.tareasList = res.data || [];
      this.render();
    } catch (err) {
      console.error('Error al cargar tareas:', err);
    }
  },

  render() {
    const colPendiente = document.getElementById('kanban-col-pendiente');
    const colProceso = document.getElementById('kanban-col-proceso');
    const colCompletada = document.getElementById('kanban-col-completada');

    colPendiente.innerHTML = '';
    colProceso.innerHTML = '';
    colCompletada.innerHTML = '';

    const pendientes = this.tareasList.filter(t => t.estado === 'Pendiente');
    const enProceso = this.tareasList.filter(t => t.estado === 'En Proceso');
    const completadas = this.tareasList.filter(t => t.estado === 'Completada');

    document.getElementById('count-pendiente').textContent = pendientes.length;
    document.getElementById('count-proceso').textContent = enProceso.length;
    document.getElementById('count-completada').textContent = completadas.length;

    pendientes.forEach(t => colPendiente.appendChild(this.crearTarjetaTarea(t)));
    enProceso.forEach(t => colProceso.appendChild(this.crearTarjetaTarea(t)));
    completadas.forEach(t => colCompletada.appendChild(this.crearTarjetaTarea(t)));
  },

  crearTarjetaTarea(t) {
    const card = document.createElement('div');
    card.className = 'card mb-3 p-3 shadow-sm border';

    const fecha = API.formatDate(t.fechaVencimiento);
    const vencida = new Date(t.fechaVencimiento) < new Date() && t.estado !== 'Completada';

    card.innerHTML = `
      <div class="d-flex justify-content-between align-items-start mb-2">
        <h6 class="fw-bold mb-0 text-dark">${t.nombre}</h6>
        <div class="dropdown">
          <button class="btn btn-sm btn-link text-muted p-0" data-bs-toggle="dropdown">
            <i class="bi bi-three-dots-vertical"></i>
          </button>
          <ul class="dropdown-menu dropdown-menu-end">
            <li><a class="dropdown-item" href="javascript:void(0)" onclick="Tareas.cambiarEstado(${t.id}, 'Pendiente')">Mover a Pendiente</a></li>
            <li><a class="dropdown-item" href="javascript:void(0)" onclick="Tareas.cambiarEstado(${t.id}, 'En Proceso')">Mover a En Proceso</a></li>
            <li><a class="dropdown-item" href="javascript:void(0)" onclick="Tareas.cambiarEstado(${t.id}, 'Completada')">Mover a Completada</a></li>
            <li><hr class="dropdown-divider"></li>
            <li><a class="dropdown-item text-danger" href="javascript:void(0)" onclick="Tareas.eliminar(${t.id})">Eliminar</a></li>
          </ul>
        </div>
      </div>
      <p class="small text-muted mb-2">${t.descripcion || 'Sin descripción adicional'}</p>
      <div class="d-flex justify-content-between align-items-center mt-2 pt-2 border-top">
        <span class="badge bg-light text-dark border"><i class="bi bi-person me-1"></i>${t.responsable || 'Sin asignar'}</span>
        <span class="small ${vencida ? 'text-danger fw-bold' : 'text-muted'}"><i class="bi bi-calendar3 me-1"></i>${fecha}</span>
      </div>
    `;

    return card;
  },

  abrirModalCrear() {
    if (!this.currentEventoId) {
      alert('Por favor selecciona un evento primero.');
      return;
    }

    document.getElementById('modal-tarea-titulo').textContent = 'Crear Nueva Tarea';
    document.getElementById('form-tarea-id').value = '';
    document.getElementById('form-tarea-nombre').value = '';
    document.getElementById('form-tarea-desc').value = '';
    document.getElementById('form-tarea-responsable').value = '';
    document.getElementById('form-tarea-fecha').value = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10);
    document.getElementById('form-tarea-estado').value = 'Pendiente';

    const modal = new bootstrap.Modal(document.getElementById('modal-tarea'));
    modal.show();
  },

  async handleSubmit(e) {
    e.preventDefault();
    const id = document.getElementById('form-tarea-id').value;
    const modalEl = document.getElementById('modal-tarea');
    const modal = bootstrap.Modal.getInstance(modalEl);

    const payload = {
      nombre: document.getElementById('form-tarea-nombre').value.trim(),
      descripcion: document.getElementById('form-tarea-desc').value.trim(),
      responsable: document.getElementById('form-tarea-responsable').value.trim(),
      fechaVencimiento: new Date(document.getElementById('form-tarea-fecha').value).toISOString(),
      estado: document.getElementById('form-tarea-estado').value
    };

    try {
      if (id) {
        await API.put(`/tareas/${id}`, payload);
      } else {
        await API.post(`/eventos/${this.currentEventoId}/tareas`, payload);
      }
      modal.hide();
      this.load(this.currentEventoId);
    } catch (err) {
      alert('Error: ' + err.message);
    }
  },

  async cambiarEstado(id, nuevoEstado) {
    try {
      await API.patch(`/tareas/${id}/estado`, { estado: nuevoEstado });
      this.load(this.currentEventoId);
    } catch (err) {
      alert('Error al cambiar estado: ' + err.message);
    }
  },

  async eliminar(id) {
    if (!confirm('¿Deseas eliminar esta tarea?')) return;

    try {
      await API.delete(`/tareas/${id}`);
      this.load(this.currentEventoId);
    } catch (err) {
      alert('Error al eliminar tarea: ' + err.message);
    }
  }
};
