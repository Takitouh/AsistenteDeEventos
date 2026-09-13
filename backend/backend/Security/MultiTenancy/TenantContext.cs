namespace AsistenteEventos.Security.MultiTenancy;

public class TenantContext : ITenantContext
{
    public int? PymeId { get; private set; }
    public int? UsuarioId { get; private set; }
    public string? UserEmail { get; private set; }
    public string? UserRole { get; private set; }
    public bool IsAuthenticated => PymeId.HasValue && UsuarioId.HasValue;

    public void SetTenant(int pymeId, int usuarioId, string userEmail, string userRole)
    {
        PymeId = pymeId;
        UsuarioId = usuarioId;
        UserEmail = userEmail;
        UserRole = userRole;
    }

    public void Clear()
    {
        PymeId = null;
        UsuarioId = null;
        UserEmail = null;
        UserRole = null;
    }
}
