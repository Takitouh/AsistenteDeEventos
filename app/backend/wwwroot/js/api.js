// =============================================================================
// CLIENTE API REST CENTRALIZADO CON AISLAMIENTO MULTI-TENANT
// =============================================================================

const API = {
  baseUrl: '/api',

  getToken() {
    return localStorage.getItem('asistente_token');
  },

  setToken(token) {
    localStorage.setItem('asistente_token', token);
  },

  removeToken() {
    localStorage.removeItem('asistente_token');
    localStorage.removeItem('asistente_user');
  },

  getUser() {
    const userStr = localStorage.getItem('asistente_user');
    return userStr ? JSON.parse(userStr) : null;
  },

  setUser(user) {
    localStorage.setItem('asistente_user', JSON.stringify(user));
  },

  isAuthenticated() {
    return !!this.getToken();
  },

  async request(endpoint, options = {}) {
    const url = `${this.baseUrl}${endpoint}`;
    const token = this.getToken();

    const headers = {
      'Content-Type': 'application/json',
      ...options.headers,
    };

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    try {
      const response = await fetch(url, {
        ...options,
        headers,
      });

      if (response.status === 401) {
        // Sesión expirada o no autorizada
        this.removeToken();
        window.dispatchEvent(new CustomEvent('auth:expired'));
        throw new Error('Sesión expirada o no autorizada. Por favor inicia sesión.');
      }

      const data = await response.json();

      if (!response.ok || (data && data.success === false)) {
        throw new Error(data.message || `Error en la solicitud (${response.status})`);
      }

      return data;
    } catch (error) {
      console.error(`Error en API [${endpoint}]:`, error);
      throw error;
    }
  },

  get(endpoint) {
    return this.request(endpoint, { method: 'GET' });
  },

  post(endpoint, body) {
    return this.request(endpoint, {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },

  put(endpoint, body) {
    return this.request(endpoint, {
      method: 'PUT',
      body: JSON.stringify(body),
    });
  },

  patch(endpoint, body = {}) {
    return this.request(endpoint, {
      method: 'PATCH',
      body: JSON.stringify(body),
    });
  },

  delete(endpoint) {
    return this.request(endpoint, { method: 'DELETE' });
  },

  // Formateador estándar de moneda: Pesos Colombianos (COP)
  formatCOP(value) {
    const num = Number(value) || 0;
    return new Intl.NumberFormat('es-CO', {
      style: 'currency',
      currency: 'COP',
      maximumFractionDigits: 0,
    }).format(num);
  },

  formatDate(dateString) {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('es-CO', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  },

  formatDateTime(dateString) {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('es-CO', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }
};
