namespace AsistenteEventos.Security.MultiTenancy;

public interface ITenantContext
{
    int? PymeId { get; }
    int? UsuarioId { get; }
    string? UserEmail { get; }
    string? UserRole { get; }
    bool IsAuthenticated { get; }

    void SetTenant(int pymeId, int usuarioId, string userEmail, string userRole);
    void Clear();
}
