using System.Security.Claims;

namespace AsistenteEventos.Security.MultiTenancy;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var pymeIdClaim = context.User.FindFirst("pyme_id")?.Value 
                              ?? context.User.FindFirst("PymeId")?.Value;

            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? context.User.FindFirst("sub")?.Value
                              ?? context.User.FindFirst("user_id")?.Value;

            var emailClaim = context.User.FindFirst(ClaimTypes.Email)?.Value 
                             ?? context.User.FindFirst("email")?.Value;

            var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value 
                            ?? context.User.FindFirst("role")?.Value;

            if (int.TryParse(pymeIdClaim, out int pymeId) && int.TryParse(userIdClaim, out int userId))
            {
                tenantContext.SetTenant(pymeId, userId, emailClaim ?? string.Empty, roleClaim ?? "Usuario");
                _logger.LogDebug("TenantContext establecido para PymeId: {PymeId}, UsuarioId: {UsuarioId}", pymeId, userId);
            }
            else
            {
                _logger.LogWarning("Usuario autenticado sin claims válidos de PymeId o UserId.");
            }
        }

        await _next(context);
    }
}
