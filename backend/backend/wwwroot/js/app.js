// =============================================================================
// COORDINADOR PRINCIPAL DE LA APLICACIÓN WEB
// =============================================================================

const App = {
  currentTab: 'dashboard',
  selectedEventoId: null,

  init() {
    Auth.init();
    Chat.init();
    Canvas.init();
    Eventos.init();
    Finanzas.init();
    Tareas.init();
    Notificaciones.init();

    this.bindNavigation();
  },

  bindNavigation() {
    document.querySelectorAll('.nav-link-custom').forEach(link => {
      link.addEventListener('click', (e) => {
        e.preventDefault();
        const tab = link.getAttribute('data-tab');
        if (tab) this.switchTab(tab);
      });
    });

    const selector = document.getElementById('select-evento-activo');
    if (selector) {
      selector.addEventListener('change', (e) => {
        const val = e.target.value;
        this.selectedEventoId = val ? parseInt(val) : null;
        this.refreshCurrentTab();
      });
    }
  },

  async onLoginSuccess() {
    await this.loadEventosList();
    this.switchTab('dashboard');
    Notificaciones.load();
  },

  async loadEventosList() {
    try {
      const res = await API.get('/eventos');
      const eventos = res.data || [];
      const selector = document.getElementById('select-evento-activo');
      if (!selector) return;

      selector.innerHTML = '<option value="">-- Todos / Resumen General --</option>';
      eventos.forEach(ev => {
        const opt = document.createElement('option');
        opt.value = ev.id;
        opt.textContent = `${ev.nombre} (${ev.estado})`;
        selector.appendChild(opt);
      });

      if (eventos.length > 0 && !this.selectedEventoId) {
        // Seleccionar por defecto el primero
        this.selectedEventoId = eventos[0].id;
        selector.value = eventos[0].id;
      }
    } catch (err) {
      console.error('Error al cargar lista de eventos:', err);
    }
  },

  getSelectedEventoId() {
    return this.selectedEventoId;
  },

  setSelectedEventoId(id) {
    this.selectedEventoId = id;
    const selector = document.getElementById('select-evento-activo');
    if (selector) selector.value = id;
  },

  switchTab(tabName) {
    this.currentTab = tabName;

    // Actualizar clases activas en la barra de navegación
    document.querySelectorAll('.nav-link-custom').forEach(link => {
      if (link.getAttribute('data-tab') === tabName) {
        link.classList.add('active');
      } else {
        link.classList.remove('active');
      }
    });

    // Ocultar todos los paneles de vistas
    document.querySelectorAll('.tab-view').forEach(view => {
      view.classList.add('d-none');
    });

    // Mostrar el panel de la vista activa
    const targetView = document.getElementById(`view-${tabName}`);
    if (targetView) {
      targetView.classList.remove('d-none');
    }

    this.refreshCurrentTab();
  },

  refreshCurrentTab() {
    const id = this.selectedEventoId;
    switch (this.currentTab) {
      case 'dashboard':
        Dashboard.load(id);
        break;
      case 'eventos':
        Eventos.load();
        break;
      case 'finanzas':
        Finanzas.load(id);
        break;
      case 'tareas':
        Tareas.load(id);
        break;
      case 'notificaciones':
        Notificaciones.load();
        break;
      case 'canvas':
        Canvas.render();
        break;
    }
  }
};

// Inicialización cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', () => {
  App.init();
});
