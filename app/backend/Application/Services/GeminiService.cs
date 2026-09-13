using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AsistenteEventos.Application.DTOs;
using AsistenteEventos.Domain.Entities;
using AsistenteEventos.Infrastructure.Data;
using AsistenteEventos.Security.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AsistenteEventos.Application.Services;

public interface IGeminiService
{
    Task<GeminiChatResponseDto> ChatAsync(GeminiChatRequestDto request);
    Task<PropuestasContainerDto> GenerarPropuestasAsync(GeminiPlanRequestDto request);
    Task<EventoDto> FormalizarPropuestaAsync(FormalizarPropuestaDto dto);
}

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(
        HttpClient httpClient,
        IConfiguration configuration,
        AppDbContext context,
        ITenantContext tenantContext,
        ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    private int EnsureTenantId()
    {
        if (!_tenantContext.PymeId.HasValue)
            throw new UnauthorizedAccessException("Acceso denegado: Identidad de PYME no disponible.");
        return _tenantContext.PymeId.Value;
    }

    public async Task<GeminiChatResponseDto> ChatAsync(GeminiChatRequestDto request)
    {
        var pymeId = EnsureTenantId();

        var pyme = await _context.Pymes
            .Where(p => p.Id == pymeId)
            .FirstOrDefaultAsync();

        if (pyme == null)
            throw new KeyNotFoundException("No se encontró la información de la PYME autenticada.");

        Evento? evento = null;
        if (request.EventoId.HasValue)
        {
            evento = await _context.Eventos
                .Where(e => e.Id == request.EventoId.Value && e.PymeId == pymeId)
                .FirstOrDefaultAsync();
        }

        // Guardar mensaje del usuario en el historial
        var mensajeUsuario = new ConversacionMensaje
        {
            PymeId = pymeId,
            EventoId = request.EventoId,
            Rol = "user",
            Contenido = request.Mensaje.Trim(),
            FechaCreacion = DateTime.UtcNow
        };
        _context.ConversacionMensajes.Add(mensajeUsuario);
        await _context.SaveChangesAsync();

        // Detectar si la intención es idear o planificar un evento con presupuesto/aforo
        bool esSolicitudPlanificacion = DetectarIntencionPlanificacion(request.Mensaje);

        string respuestaTexto;
        List<PropuestaEventoDto>? propuestas = null;

        try
        {
            if (esSolicitudPlanificacion)
            {
                // Generar propuestas estructuradas mediante Gemini
                var planReq = new GeminiPlanRequestDto
                {
                    EventoId = request.EventoId,
                    ObjetivoComercial = request.Mensaje,
                    TipoEvento = "Comercial / Promocional",
                    Aforo = ExtraerNumero(request.Mensaje, @"(\d+)\s*(personas|asistentes|invitados)", 60),
                    PresupuestoMaximoCOP = ExtraerMontoCOP(request.Mensaje, 5_000_000m),
                    Ciudad = pyme.Ciudad
                };

                var container = await GenerarPropuestasAsync(planReq);
                propuestas = container.Propuestas;
                respuestaTexto = $"He estructurado {propuestas.Count} propuesta(s) de evento para {pyme.RazonSocial} en {pyme.Ciudad}, optimizadas para tu presupuesto en pesos colombianos (COP). Puedes revisarlas en detalle y formalizarlas en tu Canvas de Planificación.";
            }
            else
            {
                // Conversación general de asesoría de eventos
                respuestaTexto = await ConsultarGeminiConversacionalAsync(pyme, evento, request.Mensaje);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al invocar Google Gemini API: {Message}", ex.Message);
            // Fallback seguro (RNF-F1): Nunca fallar bruscamente ni perder datos
            respuestaTexto = GenerarRespuestaFallbackConversacional(pyme, request.Mensaje);
        }

        // Guardar respuesta del asistente en el historial
        var mensajeModelo = new ConversacionMensaje
        {
            PymeId = pymeId,
            EventoId = request.EventoId,
            Rol = "model",
            Contenido = respuestaTexto,
            FechaCreacion = DateTime.UtcNow
        };
        _context.ConversacionMensajes.Add(mensajeModelo);
        await _context.SaveChangesAsync();

        return new GeminiChatResponseDto
        {
            Respuesta = respuestaTexto,
            EsPropuestaEstructurada = esSolicitudPlanificacion && propuestas != null && propuestas.Any(),
            Propuestas = propuestas
        };
    }

    public async Task<PropuestasContainerDto> GenerarPropuestasAsync(GeminiPlanRequestDto request)
    {
        var pymeId = EnsureTenantId();

        var pyme = await _context.Pymes
            .Where(p => p.Id == pymeId)
            .FirstOrDefaultAsync();

        if (pyme == null)
            throw new KeyNotFoundException("PYME no encontrada.");

        var apiKey = _configuration["Gemini:ApiKey"] 
                     ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        // Si no hay API Key configurada o se produce timeout, usar motor generativo heurístico local para Colombia
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Gemini API Key no configurada. Generando propuestas mediante motor heurístico colombiano.");
            return GenerarPropuestasHeuristicasLocales(pyme, request);
        }

        var systemPrompt = $@"Eres un experto senior en producción, logística y finanzas de eventos corporativos y publicitarios para PYMES en Colombia.
Tu tarea es idear y planificar propuestas de eventos altamente realistas y adaptadas al mercado colombiano.

INFORMACIÓN DE LA PYME:
- Razón Social: {pyme.RazonSocial}
- Sector: {pyme.Sector}
- Ubicación: {pyme.Ciudad}, {pyme.Departamento}, Colombia.

PARÁMETROS DEL EVENTO SOLICITADO:
- Objetivo Comercial: {request.ObjetivoComercial}
- Aforo Estimado: {request.Aforo} personas
- Presupuesto Máximo en Pesos Colombianos (COP): ${request.PresupuestoMaximoCOP:N0} COP
- Restricciones: {request.Restricciones ?? "Ninguna"}

REGLAS OBLIGATORIAS:
1. Todos los valores monetarios deben estar expresados en Pesos Colombianos (COP).
2. Debes incluir exactamente los 4 rubros obligatorios de presupuesto: 'Logística', 'Insumos/Comida', 'Publicidad', 'Imprevistos'.
3. La suma de los porcentajes de los rubros debe ser exactamente 100%.
4. La suma de los 'valorEstimado' de los rubros debe ser igual o menor a presupuestoEstimadoCOP.
5. Cada propuesta debe incluir entre 3 y 5 tareas operativas clave con responsable y días de anticipación (diasAntes).
6. IMPORTANTE: Responde ÚNICAMENTE con un objeto JSON válido con la siguiente estructura, sin comentarios ni explicaciones adicionales:

{{
  ""propuestas"": [
    {{
      ""nombre"": ""Nombre atractivo del evento"",
      ""descripcion"": ""Descripción concisa y persuasiva"",
      ""objetivo"": ""Objetivo comercial específico"",
      ""aforo"": {request.Aforo},
      ""presupuestoEstimadoCOP"": {request.PresupuestoMaximoCOP},
      ""rubros"": [
        {{ ""categoria"": ""Logística"", ""porcentaje"": 30, ""valorEstimado"": {request.PresupuestoMaximoCOP * 0.30m} }},
        {{ ""categoria"": ""Insumos/Comida"", ""porcentaje"": 40, ""valorEstimado"": {request.PresupuestoMaximoCOP * 0.40m} }},
        {{ ""categoria"": ""Publicidad"", ""porcentaje"": 20, ""valorEstimado"": {request.PresupuestoMaximoCOP * 0.20m} }},
        {{ ""categoria"": ""Imprevistos"", ""porcentaje"": 10, ""valorEstimado"": {request.PresupuestoMaximoCOP * 0.10m} }}
      ],
      ""tareas"": [
        {{ ""nombre"": ""Reservar locación o salón"", ""descripcion"": ""Cotizar y separar espacio"", ""responsable"": ""Coordinador"", ""diasAntes"": 20 }},
        {{ ""nombre"": ""Diseñar piezas de difusión digital"", ""descripcion"": ""Crear flyers y pauta en redes"", ""responsable"": ""Marketing"", ""diasAntes"": 15 }},
        {{ ""nombre"": ""Contratar servicio de catering/degustación"", ""descripcion"": ""Definir menú según aforo"", ""responsable"": ""Logística"", ""diasAntes"": 10 }}
      ]
    }}
  ]
}}";

        try
        {
            var rawJson = await LlamarGeminiApiAsync(apiKey, systemPrompt);
            var parsed = ParsearYValidarPropuestas(rawJson, request.PresupuestoMaximoCOP, request.Aforo);
            if (parsed != null && parsed.Propuestas.Any())
            {
                return parsed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallo al consultar Gemini API. Activando contingencia heurística para Colombia.");
        }

        // Fallback robusto garantizado
        return GenerarPropuestasHeuristicasLocales(pyme, request);
    }

    public async Task<EventoDto> FormalizarPropuestaAsync(FormalizarPropuestaDto dto)
    {
        var pymeId = EnsureTenantId();

        // 1. Crear el Evento
        var evento = new Evento
        {
            PymeId = pymeId,
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion.Trim(),
            FechaEvento = dto.FechaEvento > DateTime.UtcNow ? dto.FechaEvento : DateTime.UtcNow.AddDays(30),
            Ubicacion = string.IsNullOrWhiteSpace(dto.Ubicacion) ? "Ubicación por definir" : dto.Ubicacion.Trim(),
            AforoEstimado = dto.Aforo > 0 ? dto.Aforo : 50,
            PresupuestoBase = dto.PresupuestoBase > 0 ? dto.PresupuestoBase : 5_000_000m,
            Estado = "En Planificación",
            FechaCreacion = DateTime.UtcNow
        };

        _context.Eventos.Add(evento);
        await _context.SaveChangesAsync();

        // 2. Crear los Rubros Presupuestales
        if (dto.Rubros != null && dto.Rubros.Any())
        {
            foreach (var r in dto.Rubros)
            {
                _context.RubrosPresupuestales.Add(new RubroPresupuestal
                {
                    PymeId = pymeId,
                    EventoId = evento.Id,
                    Categoria = r.Categoria,
                    Porcentaje = r.Porcentaje,
                    ValorEstimado = r.ValorEstimado > 0 ? r.ValorEstimado : (evento.PresupuestoBase * (r.Porcentaje / 100m))
                });
            }
        }
        else
        {
            // Rubros por defecto
            _context.RubrosPresupuestales.AddRange(
                new RubroPresupuestal { PymeId = pymeId, EventoId = evento.Id, Categoria = "Logística", Porcentaje = 30m, ValorEstimado = evento.PresupuestoBase * 0.30m },
                new RubroPresupuestal { PymeId = pymeId, EventoId = evento.Id, Categoria = "Insumos/Comida", Porcentaje = 40m, ValorEstimado = evento.PresupuestoBase * 0.40m },
                new RubroPresupuestal { PymeId = pymeId, EventoId = evento.Id, Categoria = "Publicidad", Porcentaje = 20m, ValorEstimado = evento.PresupuestoBase * 0.20m },
                new RubroPresupuestal { PymeId = pymeId, EventoId = evento.Id, Categoria = "Imprevistos", Porcentaje = 10m, ValorEstimado = evento.PresupuestoBase * 0.10m }
            );
        }

        // 3. Crear las Tareas sugeridas automáticamente
        if (dto.Tareas != null && dto.Tareas.Any())
        {
            foreach (var t in dto.Tareas)
            {
                var fechaVencimiento = evento.FechaEvento.AddDays(-Math.Max(t.DiasAntes, 1));
                if (fechaVencimiento < DateTime.UtcNow) fechaVencimiento = DateTime.UtcNow.AddDays(2);

                _context.Tareas.Add(new Tarea
                {
                    PymeId = pymeId,
                    EventoId = evento.Id,
                    Nombre = t.Nombre.Trim(),
                    Descripcion = t.Descripcion.Trim(),
                    Responsable = string.IsNullOrWhiteSpace(t.Responsable) ? "Coordinador del Evento" : t.Responsable.Trim(),
                    FechaVencimiento = fechaVencimiento,
                    Estado = "Pendiente",
                    FechaCreacion = DateTime.UtcNow
                });
            }
        }

        // 4. Crear Notificación de bienvenida a la planificación
        _context.Notificaciones.Add(new Notificacion
        {
            PymeId = pymeId,
            EventoId = evento.Id,
            Mensaje = $"¡Excelente! El evento '{evento.Nombre}' ha sido formalizado en tu sistema con presupuesto asignado de ${evento.PresupuestoBase:N0} COP.",
            Tipo = "General",
            Leida = false,
            FechaCreacion = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Propuesta formalizada como Evento ID {Id} para PYME {PymeId}", evento.Id, pymeId);

        return new EventoDto
        {
            Id = evento.Id,
            PymeId = evento.PymeId,
            Nombre = evento.Nombre,
            Descripcion = evento.Descripcion,
            FechaEvento = evento.FechaEvento,
            Ubicacion = evento.Ubicacion,
            AforoEstimado = evento.AforoEstimado,
            PresupuestoBase = evento.PresupuestoBase,
            Estado = evento.Estado,
            FechaCreacion = evento.FechaCreacion
        };
    }

    private async Task<string> LlamarGeminiApiAsync(string apiKey, string prompt)
    {
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.4,
                maxOutputTokens = 2048,
                responseMimeType = "application/json"
            }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(11)); // RNF-R2: <= 12 segundos
        var response = await _httpClient.PostAsync(endpoint, jsonContent, cts.Token);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Gemini API error ({response.StatusCode}): {err}");
        }

        var responseString = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseString);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? string.Empty;
    }

    private async Task<string> ConsultarGeminiConversacionalAsync(Pyme pyme, Evento? evento, string mensajeUsuario)
    {
        var apiKey = _configuration["Gemini:ApiKey"] 
                     ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return GenerarRespuestaFallbackConversacional(pyme, mensajeUsuario);
        }

        var sbContext = new StringBuilder();
        sbContext.AppendLine("Eres el 'Asistente de Eventos de Negocio' con IA para PYMES en Colombia.");
        sbContext.AppendLine($"PYME actual: {pyme.RazonSocial}, Sector: {pyme.Sector}, Ciudad: {pyme.Ciudad}, Colombia.");
        if (evento != null)
        {
            sbContext.AppendLine($"Evento en contexto: '{evento.Nombre}', Aforo: {evento.AforoEstimado}, Presupuesto Base: ${evento.PresupuestoBase:N0} COP, Estado: {evento.Estado}.");
        }
        sbContext.AppendLine("Instrucciones: Asesora de manera amigable, ejecutiva y práctica. Usa siempre Pesos Colombianos (COP). Recuerda que las estimaciones son referenciales.");
        sbContext.AppendLine($"Consulta del usuario: {mensajeUsuario}");

        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = sbContext.ToString() } } }
            },
            generationConfig = new
            {
                temperature = 0.6,
                maxOutputTokens = 1000
            }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4)); // RNF-R1: <= 4 segundos
        var response = await _httpClient.PostAsync(endpoint, jsonContent, cts.Token);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Gemini error {response.StatusCode}");

        var resJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(resJson);
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? GenerarRespuestaFallbackConversacional(pyme, mensajeUsuario);
    }

    private PropuestasContainerDto? ParsearYValidarPropuestas(string rawText, decimal presupuestoMaximo, int aforo)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return null;

        // Limpieza de posibles delimitadores markdown ```json ... ```
        var cleanJson = Regex.Replace(rawText, @"^```(json)?\s*", "", RegexOptions.Multiline);
        cleanJson = Regex.Replace(cleanJson, @"\s*```$", "", RegexOptions.Multiline).Trim();

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var container = JsonSerializer.Deserialize<PropuestasContainerDto>(cleanJson, options);

            if (container != null && container.Propuestas.Any())
            {
                // Validar y normalizar cada propuesta recibida
                foreach (var prop in container.Propuestas)
                {
                    if (prop.Aforo <= 0) prop.Aforo = aforo > 0 ? aforo : 50;
                    if (prop.PresupuestoEstimadoCOP <= 0) prop.PresupuestoEstimadoCOP = presupuestoMaximo > 0 ? presupuestoMaximo : 5_000_000m;

                    // Garantizar los 4 rubros requeridos
                    var categoriasRequeridas = new[] { "Logística", "Insumos/Comida", "Publicidad", "Imprevistos" };
                    if (prop.Rubros == null || !prop.Rubros.Any())
                    {
                        prop.Rubros = new List<PropuestaRubroDto>
                        {
                            new() { Categoria = "Logística", Porcentaje = 30, ValorEstimado = prop.PresupuestoEstimadoCOP * 0.30m },
                            new() { Categoria = "Insumos/Comida", Porcentaje = 40, ValorEstimado = prop.PresupuestoEstimadoCOP * 0.40m },
                            new() { Categoria = "Publicidad", Porcentaje = 20, ValorEstimado = prop.PresupuestoEstimadoCOP * 0.20m },
                            new() { Categoria = "Imprevistos", Porcentaje = 10, ValorEstimado = prop.PresupuestoEstimadoCOP * 0.10m }
                        };
                    }
                    else
                    {
                        foreach (var cat in categoriasRequeridas)
                        {
                            if (!prop.Rubros.Any(r => r.Categoria.Equals(cat, StringComparison.OrdinalIgnoreCase)))
                            {
                                prop.Rubros.Add(new PropuestaRubroDto
                                {
                                    Categoria = cat,
                                    Porcentaje = 10,
                                    ValorEstimado = prop.PresupuestoEstimadoCOP * 0.10m
                                });
                            }
                        }
                    }

                    // Validar tareas mínimas
                    if (prop.Tareas == null || !prop.Tareas.Any())
                    {
                        prop.Tareas = new List<PropuestaTareaDto>
                        {
                            new() { Nombre = "Cotizar y reservar locación", Descripcion = "Verificar disponibilidad según aforo", Responsable = "Coordinador", DiasAntes = 20 },
                            new() { Nombre = "Campaña de difusión y convocatorias", Descripcion = "Pauta digital y envío de invitaciones", Responsable = "Marketing", DiasAntes = 15 },
                            new() { Nombre = "Coordinación de insumos y pasabocas", Descripcion = "Catering adaptado a la ciudad", Responsable = "Logística", DiasAntes = 7 }
                        };
                    }
                }

                return container;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "No fue posible parsear JSON retornado por Gemini: {CleanJson}", cleanJson);
        }

        return null;
    }

    private PropuestasContainerDto GenerarPropuestasHeuristicasLocales(Pyme pyme, GeminiPlanRequestDto request)
    {
        var presupuesto = request.PresupuestoMaximoCOP > 0 ? request.PresupuestoMaximoCOP : 5_000_000m;
        var aforo = request.Aforo > 0 ? request.Aforo : 60;
        var ciudad = string.IsNullOrWhiteSpace(request.Ciudad) ? pyme.Ciudad : request.Ciudad;

        return new PropuestasContainerDto
        {
            Propuestas = new List<PropuestaEventoDto>
            {
                new()
                {
                    Nombre = $"Experiencia de Marca y Networking: {pyme.RazonSocial}",
                    Descripcion = $"Evento presencial exclusivo en {ciudad} diseñado para fidelizar clientes y generar nuevas alianzas estratégicas para {pyme.RazonSocial}.",
                    Objetivo = $"Posicionamiento comercial y captación de clientes en {ciudad}",
                    Aforo = aforo,
                    PresupuestoEstimadoCOP = presupuesto,
                    Rubros = new List<PropuestaRubroDto>
                    {
                        new() { Categoria = "Logística", Porcentaje = 30m, ValorEstimado = presupuesto * 0.30m },
                        new() { Categoria = "Insumos/Comida", Porcentaje = 40m, ValorEstimado = presupuesto * 0.40m },
                        new() { Categoria = "Publicidad", Porcentaje = 20m, ValorEstimado = presupuesto * 0.20m },
                        new() { Categoria = "Imprevistos", Porcentaje = 10m, ValorEstimado = presupuesto * 0.10m }
                    },
                    Tareas = new List<PropuestaTareaDto>
                    {
                        new() { Nombre = $"Separar salón en centro empresarial de {ciudad}", Descripcion = $"Capacidad para {aforo} personas con ayudas audiovisuales", Responsable = "Coordinador de Operaciones", DiasAntes = 25 },
                        new() { Nombre = "Diseño de tarjetas de invitación VIP y piezas de difusión", Descripcion = "Diseño gráfico adaptado a la identidad corporativa de la PYME", Responsable = "Equipo de Marketing", DiasAntes = 18 },
                        new() { Nombre = "Cotización de catering gourmet con café de origen", Descripcion = "Degustación de pasabocas y bebidas típicas colombianas", Responsable = "Logística", DiasAntes = 12 },
                        new() { Nombre = "Envío de recordatorios y confirmación de asistencia", Descripcion = "Llamadas y confirmación por WhatsApp empresarial", Responsable = "Servicio al Cliente", DiasAntes = 4 }
                    }
                },
                new()
                {
                    Nombre = $"Jornada de Demostración y Lanzamiento Comercial",
                    Descripcion = $"Showcase dinámico y presentación en vivo de los productos y servicios líderes de {pyme.RazonSocial} en {ciudad}.",
                    Objetivo = $"Demostración de valor y cierre de ventas inmediatas",
                    Aforo = Math.Max(aforo - 10, 30),
                    PresupuestoEstimadoCOP = Math.Round(presupuesto * 0.85m),
                    Rubros = new List<PropuestaRubroDto>
                    {
                        new() { Categoria = "Logística", Porcentaje = 35m, ValorEstimado = Math.Round(presupuesto * 0.85m * 0.35m) },
                        new() { Categoria = "Insumos/Comida", Porcentaje = 35m, ValorEstimado = Math.Round(presupuesto * 0.85m * 0.35m) },
                        new() { Categoria = "Publicidad", Porcentaje = 20m, ValorEstimado = Math.Round(presupuesto * 0.85m * 0.20m) },
                        new() { Categoria = "Imprevistos", Porcentaje = 10m, ValorEstimado = Math.Round(presupuesto * 0.85m * 0.10m) }
                    },
                    Tareas = new List<PropuestaTareaDto>
                    {
                        new() { Nombre = "Preparación de stands y material demo", Descripcion = "Montaje interactivo para pruebas de producto", Responsable = "Líder Técnico", DiasAntes = 20 },
                        new() { Nombre = "Campaña publicitaria geolocalizada en redes sociales", Descripcion = $"Pauta segmentada para PYMES y profesionales en {ciudad}", Responsable = "Marketing Digital", DiasAntes = 14 },
                        new() { Nombre = "Contratación de sonido profesional y ambientación", Descripcion = "Micrófonos inalámbricos y música de fondo", Responsable = "Proveedor Audiovisual", DiasAntes = 8 }
                    }
                }
            }
        };
    }

    private string GenerarRespuestaFallbackConversacional(Pyme pyme, string mensaje)
    {
        return $"¡Hola! Como asistente de eventos para {pyme.RazonSocial} en {pyme.Ciudad}, te puedo ayudar a planificar eventos corporativos, cotizar rubros en pesos colombianos (COP), organizar tareas con fechas límite y controlar tus gastos reales. Si deseas que arme una propuesta completa, indícame el tipo de evento, aforo estimado y tu presupuesto disponible en COP.";
    }

    private bool DetectarIntencionPlanificacion(string mensaje)
    {
        var palabrasClave = new[] { "organizar", "planear", "planificar", "crear evento", "propuesta", "presupuesto", "aforo", "cotizar", "lanzamiento", "personas", "cuánto cuesta", "evento para" };
        var lower = mensaje.ToLower();
        return palabrasClave.Any(k => lower.Contains(k));
    }

    private int ExtraerNumero(string texto, string patron, int defecto)
    {
        var match = Regex.Match(texto, patron, RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int n))
            return n;
        return defecto;
    }

    private decimal ExtraerMontoCOP(string texto, decimal defecto)
    {
        // Buscar patrones como $5.000.000 o 5000000 o 5 millones
        var matchMillones = Regex.Match(texto, @"(\d+([\.,]\d+)?)\s*(millones|millón)", RegexOptions.IgnoreCase);
        if (matchMillones.Success)
        {
            var strVal = matchMillones.Groups[1].Value.Replace(",", ".");
            if (decimal.TryParse(strVal, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal mills))
                return mills * 1_000_000m;
        }

        var matchPesos = Regex.Match(texto, @"\$\s*([0-9\.\,]+)");
        if (matchPesos.Success)
        {
            var strVal = matchPesos.Groups[1].Value.Replace(".", "").Replace(",", "");
            if (decimal.TryParse(strVal, out decimal val) && val > 100_000m)
                return val;
        }

        return defecto;
    }
}
