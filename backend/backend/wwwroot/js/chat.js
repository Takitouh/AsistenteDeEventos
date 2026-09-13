// =============================================================================
// MÓDULO CHAT ASISTENTE CON GEMINI (RF02)
// =============================================================================

const Chat = {
  init() {
    this.bindEvents();
  },

  bindEvents() {
    const form = document.getElementById('chat-form');
    if (form) {
      form.addEventListener('submit', (e) => this.sendMessage(e));
    }

    // Quick prompt pills
    document.querySelectorAll('.prompt-pill').forEach(pill => {
      pill.addEventListener('click', () => {
        const input = document.getElementById('chat-input');
        input.value = pill.getAttribute('data-prompt');
        input.focus();
      });
    });
  },

  async sendMessage(e) {
    e.preventDefault();
    const input = document.getElementById('chat-input');
    const mensaje = input.value.trim();
    if (!mensaje) return;

    input.value = '';
    this.appendMessage('user', mensaje);
    this.showTypingIndicator();

    try {
      const eventoId = App.getSelectedEventoId();
      const res = await API.post('/gemini/chat', { mensaje, eventoId });
      this.hideTypingIndicator();

      const data = res.data;
      this.appendMessage('model', data.respuesta);

      // Si retornó propuestas estructuradas, renderizar tarjeta interactiva en el chat con botón para ir al Canvas
      if (data.esPropuestaEstructurada && data.propuestas && data.propuestas.length > 0) {
        Canvas.setPropuestasTemporales(data.propuestas);
        this.appendPropuestaActionBubble(data.propuestas);
      }
    } catch (err) {
      this.hideTypingIndicator();
      this.appendMessage('model', 'El asistente no está disponible temporalmente. Tus datos guardados están seguros y no se han perdido.');
    }
  },

  appendMessage(role, text) {
    const container = document.getElementById('chat-messages');
    const bubble = document.createElement('div');
    bubble.className = `message-bubble ${role}`;

    const icon = role === 'user' ? '<i class="bi bi-person-fill me-2"></i>' : '<i class="bi bi-robot text-primary me-2"></i>';
    bubble.innerHTML = `<div><strong>${role === 'user' ? 'Tú' : 'Asistente IA'}:</strong></div><div class="mt-1">${this.escapeHtml(text)}</div>`;

    container.appendChild(bubble);
    container.scrollTop = container.scrollHeight;
  },

  appendPropuestaActionBubble(propuestas) {
    const container = document.getElementById('chat-messages');
    const bubble = document.createElement('div');
    bubble.className = 'message-bubble model border-primary';

    bubble.innerHTML = `
      <div class="d-flex align-items-center justify-content-between">
        <div>
          <span class="badge bg-primary me-2"><i class="bi bi-stars"></i> IA Gemini</span>
          <strong>¡Propuestas Listas para el Canvas!</strong>
          <p class="small text-muted mb-0 mt-1">Se han generado ${propuestas.length} propuestas completas con presupuestos en COP y tareas.</p>
        </div>
        <button class="btn btn-sm btn-primary ms-3" onclick="App.switchTab('canvas')">
          <i class="bi bi-kanban me-1"></i>Ver en Canvas
        </button>
      </div>
    `;

    container.appendChild(bubble);
    container.scrollTop = container.scrollHeight;
  },

  showTypingIndicator() {
    const container = document.getElementById('chat-messages');
    const indicator = document.createElement('div');
    indicator.id = 'chat-typing-indicator';
    indicator.className = 'typing-indicator my-2';
    indicator.innerHTML = `
      <span class="text-muted small me-2"><i class="bi bi-cpu"></i> Gemini analizando en COP...</span>
      <div class="typing-dot"></div>
      <div class="typing-dot"></div>
      <div class="typing-dot"></div>
    `;
    container.appendChild(indicator);
    container.scrollTop = container.scrollHeight;
  },

  hideTypingIndicator() {
    const indicator = document.getElementById('chat-typing-indicator');
    if (indicator) indicator.remove();
  },

  escapeHtml(unsafe) {
    return unsafe
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;')
      .replace(/\n/g, '<br>');
  }
};
