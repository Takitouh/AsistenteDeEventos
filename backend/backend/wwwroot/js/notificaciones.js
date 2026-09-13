// =============================================================================
// MÓDULO NOTIFICACIONES Y RECORDATORIOS (RF07)
// =============================================================================

const Notificaciones = {
  notificacionesList: [],

  init() {
    this.bindEvents();
  },

  bindEvents() {
    const btnSync = document.getElementById('btn-sync-notificaciones');
    if (btnSync) {
      btnSync.addEventListener('click', () => this.sincronizarRecordatorios());
    }
  },

  async load() {
    const container = document.getElementById('notificaciones-list');
    const badgeNav = document.getElementById('nav-badge-notif');

    try {
      const res = await API.get('/notificaciones');
      this.notificacionesList = res.data || [];

      const noLeidas = this.notificacionesList.filter(n => !n.leida).length;
      if (badgeNav) {
        if (noLeidas > 0) {
          badgeNav.textContent = noLeidas;
          badgeNav.classList.remove('d-none');
        } else {
          badgeNav.classList.add('d-none');
        }
      }

      this.render();
    } catch (err) {
      console.error('Error al cargar notificaciones:', err);
    }
  },

  render() {
    const container = document.getElementById('notificaciones-list');
    if (!container) return;

    container.innerHTML = '';

    if (this.notificacionesList.length === 0) {
      container.innerHTML = `
        <div class="text-center py-5 text-muted">
          <i class="bi bi-bell-slash fs-1 d-block mb-2"></i>
          <p>No tienes notificaciones pendientes en este momento.</p>
        </div>
      `;
      return;
    }

    this.notificacionesList.forEach(n => {
      const item = document.createElement('div');
      item.className = `p-3 mb-2 rounded border ${n.leida ? 'bg-light text-muted' : 'bg-white border-primary shadow-sm'}`;

      let icon = '<i class="bi bi-info-circle text-primary fs-4 me-3"></i>';
      if (n.tipo === 'PresupuestoExcedido') icon = '<i class="bi bi-exclamation-octagon-fill text-danger fs-4 me-3"></i>';
      if (n.tipo === 'TareaVencimiento') icon = '<i class="bi bi-clock-history text-warning fs-4 me-3"></i>';
      if (n.tipo === 'EventoProximo') icon = '<i class="bi bi-calendar-event text-success fs-4 me-3"></i>';

      item.innerHTML = `
        <div class="d-flex align-items-center justify-content-between">
          <div class="d-flex align-items-center">
            ${icon}
            <div>
              <div class="fw-semibold ${n.leida ? '' : 'text-dark'}">${n.mensaje}</div>
              <small class="text-muted">${API.formatDateTime(n.fechaCreacion)} ${n.nombreEvento ? '• Evento: ' + n.nombreEvento : ''}</small>
            </div>
          </div>
          <div>
            ${!n.leida ? `
              <button class="btn btn-sm btn-outline-primary" onclick="Notificaciones.marcarLeida(${n.id})">
                <i class="bi bi-check2"></i> Marcar leída
              </button>
            ` : '<span class="badge bg-secondary">Leída</span>'}
          </div>
        </div>
      `;

      container.appendChild(item);
    });
  },

  async marcarLeida(id) {
    try {
      await API.patch(`/notificaciones/${id}/leer`);
      await this.load();
    } catch (err) {
      alert('Error: ' + err.message);
    }
  },

  async sincronizarRecordatorios() {
    const btn = document.getElementById('btn-sync-notificaciones');
    if (btn) btn.disabled = true;

    try {
      const res = await API.post('/notificaciones/generar');
      alert(res.message || 'Recordatorios sincronizados.');
      await this.load();
    } catch (err) {
      alert('Error al sincronizar: ' + err.message);
    } finally {
      if (btn) btn.disabled = false;
    }
  }
};
