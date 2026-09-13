// =============================================================================
// MÓDULO DE AUTENTICACIÓN Y GESTIÓN DE SESIÓN
// =============================================================================

const Auth = {
  init() {
    this.bindEvents();
    this.checkSession();
  },

  bindEvents() {
    // Formularios
    const loginForm = document.getElementById('form-login');
    if (loginForm) {
      loginForm.addEventListener('submit', (e) => this.handleLogin(e));
    }

    const registerForm = document.getElementById('form-register');
    if (registerForm) {
      registerForm.addEventListener('submit', (e) => this.handleRegister(e));
    }

    const btnLogout = document.getElementById('btn-logout');
    if (btnLogout) {
      btnLogout.addEventListener('click', () => this.logout());
    }

    // Botones de acceso rápido para pruebas multi-tenant
    const btnDemoA = document.getElementById('btn-demo-pyme-a');
    if (btnDemoA) {
      btnDemoA.addEventListener('click', () => {
        document.getElementById('login-email').value = 'admin@innovatech.com.co';
        document.getElementById('login-password').value = 'Password123*';
      });
    }

    const btnDemoB = document.getElementById('btn-demo-pyme-b');
    if (btnDemoB) {
      btnDemoB.addEventListener('click', () => {
        document.getElementById('login-email').value = 'gerencia@cafedelvalle.co';
        document.getElementById('login-password').value = 'Password123*';
      });
    }

    window.addEventListener('auth:expired', () => {
      this.showAuthView();
      alert('Tu sesión ha expirado. Por favor inicia sesión nuevamente.');
    });
  },

  checkSession() {
    if (API.isAuthenticated()) {
      const user = API.getUser();
      this.showAppView(user);
    } else {
      this.showAuthView();
    }
  },

  async handleLogin(e) {
    e.preventDefault();
    const btnSubmit = document.getElementById('btn-submit-login');
    const alertBox = document.getElementById('alert-login');
    alertBox.classList.add('d-none');

    const email = document.getElementById('login-email').value.trim();
    const password = document.getElementById('login-password').value;

    btnSubmit.disabled = true;
    btnSubmit.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Iniciando sesión...';

    try {
      const res = await API.post('/auth/login', { correo: email, password });
      API.setToken(res.data.token);
      API.setUser(res.data);
      this.showAppView(res.data);
    } catch (err) {
      alertBox.textContent = err.message || 'Error al iniciar sesión';
      alertBox.classList.remove('d-none');
    } finally {
      btnSubmit.disabled = false;
      btnSubmit.innerHTML = '<i class="bi bi-box-arrow-in-right me-2"></i>Iniciar Sesión';
    }
  },

  async handleRegister(e) {
    e.preventDefault();
    const btnSubmit = document.getElementById('btn-submit-register');
    const alertBox = document.getElementById('alert-register');
    alertBox.classList.add('d-none');

    const payload = {
      razonSocial: document.getElementById('reg-razon-social').value.trim(),
      nit: document.getElementById('reg-nit').value.trim(),
      pais: 'Colombia',
      departamento: document.getElementById('reg-departamento').value.trim(),
      municipio: document.getElementById('reg-ciudad').value.trim(),
      ciudad: document.getElementById('reg-ciudad').value.trim(),
      sector: document.getElementById('reg-sector').value.trim(),
      nombre: document.getElementById('reg-nombre').value.trim(),
      correo: document.getElementById('reg-email').value.trim(),
      password: document.getElementById('reg-password').value
    };

    btnSubmit.disabled = true;
    btnSubmit.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Registrando PYME...';

    try {
      const res = await API.post('/auth/register', payload);
      API.setToken(res.data.token);
      API.setUser(res.data);
      this.showAppView(res.data);
    } catch (err) {
      alertBox.textContent = err.message || 'Error al registrar la PYME';
      alertBox.classList.remove('d-none');
    } finally {
      btnSubmit.disabled = false;
      btnSubmit.innerHTML = '<i class="bi bi-person-check me-2"></i>Crear Cuenta PYME';
    }
  },

  logout() {
    API.removeToken();
    this.showAuthView();
  },

  showAuthView() {
    document.getElementById('auth-container').classList.remove('d-none');
    document.getElementById('app-container').classList.add('d-none');
  },

  showAppView(user) {
    document.getElementById('auth-container').classList.add('d-none');
    document.getElementById('app-container').classList.remove('d-none');

    // Actualizar datos del usuario y empresa en la interfaz
    if (user) {
      const elNombre = document.getElementById('nav-user-nombre');
      const elPyme = document.getElementById('nav-user-pyme');
      if (elNombre) elNombre.textContent = user.nombre;
      if (elPyme) elPyme.textContent = `${user.razonSocial} (${user.ciudad || 'Colombia'})`;

      // Cargar vista de perfil
      const profPyme = document.getElementById('prof-pyme');
      const profNit = document.getElementById('prof-nit');
      const profCiudad = document.getElementById('prof-ciudad');
      const profUsuario = document.getElementById('prof-usuario');
      const profCorreo = document.getElementById('prof-correo');

      if (profPyme) profPyme.textContent = user.razonSocial;
      if (profNit) profNit.textContent = user.nit;
      if (profCiudad) profCiudad.textContent = user.ciudad || 'Colombia';
      if (profUsuario) profUsuario.textContent = user.nombre;
      if (profCorreo) profCorreo.textContent = user.correo;
    }

    // Inicializar los módulos de la aplicación
    App.onLoginSuccess();
  }
};
