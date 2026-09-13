using System.Net;
using System.Text.Json;
using AsistenteEventos.Common.Responses;

namespace AsistenteEventos.Common.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no controlada capturada en el pipeline HTTP: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        string userFriendlyMessage;

        // Comprobación de excepciones específicas sin exponer infraestructura interna
        if (exception is UnauthorizedAccessException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            userFriendlyMessage = "No tienes los permisos necesarios para realizar esta operación.";
        }
        else if (exception is KeyNotFoundException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            userFriendlyMessage = "El recurso solicitado no fue encontrado.";
        }
        else if (exception is HttpRequestException || exception.Message.Contains("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            userFriendlyMessage = "El asistente inteligente no está disponible temporalmente. Tus datos guardados están seguros y no se han perdido.";
        }
        else
        {
            userFriendlyMessage = "No fue posible procesar la solicitud en este momento. Por favor inténtalo nuevamente.";
        }

        var response = ApiResponse.Fail(userFriendlyMessage);
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}
