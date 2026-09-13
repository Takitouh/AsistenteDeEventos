using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AsistenteEventos.Application.DTOs;
using AsistenteEventos.Common.Responses;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AsistenteEventos.Tests;

public class MultiTenantSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MultiTenantSecurityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> ObtenerTokenAsync(string email, string password)
    {
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Correo = email,
            Password = password
        });

        loginRes.EnsureSuccessStatusCode();
        var body = await loginRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        return body!.Data!.Token;
    }

    [Fact]
    public async Task Caso1_UsuarioA_NoPuedeConsultarEventoDePYME_B()
    {
        // 1. Obtener token de Usuario A (Innovatech, PymeId: 1)
        var tokenA = await ObtenerTokenAsync("admin@innovatech.com.co", "Password123*");

        // 2. Obtener token de Usuario B (Café del Valle, PymeId: 2) para saber el Id de un evento de B
        var tokenB = await ObtenerTokenAsync("gerencia@cafedelvalle.co", "Password123*");

        var clientB = _factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var eventosB = await clientB.GetFromJsonAsync<ApiResponse<List<EventoDto>>>("/api/eventos");
        var eventoDeB = eventosB!.Data!.First();

        // 3. Usuario A intenta solicitar el evento de B
        var clientA = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        var respuesta = await clientA.GetAsync($"/api/eventos/{eventoDeB.Id}");

        // Resultado esperado: 404 Not Found (no revela existencia a PYME A)
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Caso2_UsuarioA_IntentaInyectarPymeIdDeB_ElServidorLoIgnora()
    {
        var tokenA = await ObtenerTokenAsync("admin@innovatech.com.co", "Password123*");
        var clientA = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        // Payload con intento de inyección de PymeId = 2 (de Café del Valle)
        var payloadMalicioso = new
        {
            pymeId = 2,
            nombre = "Evento Infiltrado desde Frontend",
            descripcion = "Intento de suplantación de tenencia",
            fechaEvento = DateTime.UtcNow.AddDays(15),
            ubicacion = "Bogotá",
            aforoEstimado = 50,
            presupuestoBase = 2000000m,
            estado = "En Planificación"
        };

        var postRes = await clientA.PostAsJsonAsync("/api/eventos", payloadMalicioso);
        postRes.EnsureSuccessStatusCode();

        var body = await postRes.Content.ReadFromJsonAsync<ApiResponse<EventoDto>>();
        var eventoCreado = body!.Data!;

        // Verificación crítica: El evento DEBE pertenecer a la PYME de Usuario A (PymeId: 1), nunca a B
        Assert.Equal(1, eventoCreado.PymeId);

        // Verificación adicional: Usuario B no debe poder ver este evento
        var tokenB = await ObtenerTokenAsync("gerencia@cafedelvalle.co", "Password123*");
        var clientB = _factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var getResB = await clientB.GetAsync($"/api/eventos/{eventoCreado.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResB.StatusCode);
    }

    [Fact]
    public async Task Caso3_UsuarioA_IntentaModificarOBorrarGastoDeB_OperacionRechazada()
    {
        // 1. Obtener un gasto perteneciente a B
        var tokenB = await ObtenerTokenAsync("gerencia@cafedelvalle.co", "Password123*");
        var clientB = _factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var eventosB = await clientB.GetFromJsonAsync<ApiResponse<List<EventoDto>>>("/api/eventos");
        var eventoBId = eventosB!.Data!.First().Id;
        var gastosB = await clientB.GetFromJsonAsync<ApiResponse<List<GastoDto>>>($"/api/eventos/{eventoBId}/gastos");
        var gastoDeB = gastosB!.Data!.First();

        // 2. Usuario A intenta eliminar el gasto de B
        var tokenA = await ObtenerTokenAsync("admin@innovatech.com.co", "Password123*");
        var clientA = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        var deleteRes = await clientA.DeleteAsync($"/api/gastos/{gastoDeB.Id}");

        // Resultado esperado: 404 Not Found (rechazado sin revelar el registro)
        Assert.Equal(HttpStatusCode.NotFound, deleteRes.StatusCode);
    }

    [Fact]
    public async Task Caso4_SolicitudPrivadaSinToken_Retorna401Unauthorized()
    {
        var clientAnonimo = _factory.CreateClient();
        var res = await clientAnonimo.GetAsync("/api/eventos");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Caso5_SolicitudConTokenInvalido_Retorna401Unauthorized()
    {
        var clientInvalido = _factory.CreateClient();
        clientInvalido.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "TokenTotalmenteFalso.Invalido.12345");

        var res = await clientInvalido.GetAsync("/api/eventos");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Caso6_RutasProtegidas_NoPermitenAccesoAnonimo_TareasPresupuestoNotificaciones()
    {
        var clientAnonimo = _factory.CreateClient();

        var resTareas = await clientAnonimo.GetAsync("/api/eventos/1/tareas");
        Assert.Equal(HttpStatusCode.Unauthorized, resTareas.StatusCode);

        var resPresupuesto = await clientAnonimo.GetAsync("/api/eventos/1/presupuesto");
        Assert.Equal(HttpStatusCode.Unauthorized, resPresupuesto.StatusCode);

        var resNotificaciones = await clientAnonimo.GetAsync("/api/notificaciones");
        Assert.Equal(HttpStatusCode.Unauthorized, resNotificaciones.StatusCode);

        var resDashboard = await clientAnonimo.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, resDashboard.StatusCode);
    }

    [Fact]
    public async Task Caso7_GeminiChat_ManejoSeguroDeErrores_SinStackTraceNiPerdidaDeDatos()
    {
        var tokenA = await ObtenerTokenAsync("admin@innovatech.com.co", "Password123*");
        var clientA = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        // Petición al chat del asistente
        var chatRes = await clientA.PostAsJsonAsync("/api/gemini/chat", new GeminiChatRequestDto
        {
            Mensaje = "¿Qué proveedores recomiendas para un evento en Bogotá?",
            EventoId = null
        });

        chatRes.EnsureSuccessStatusCode();
        var body = await chatRes.Content.ReadFromJsonAsync<ApiResponse<GeminiChatResponseDto>>();

        Assert.True(body!.Success);
        Assert.False(string.IsNullOrWhiteSpace(body.Data!.Respuesta));

        // Verificar que la respuesta NUNCA contenga Stack Trace ni palabras clave de excepción interna
        var textoRespuesta = body.Data.Respuesta;
        Assert.DoesNotContain("Exception", textoRespuesta);
        Assert.DoesNotContain("StackTrace", textoRespuesta);
        Assert.DoesNotContain("System.", textoRespuesta);
        Assert.DoesNotContain("Google.Apis", textoRespuesta);
    }
}
