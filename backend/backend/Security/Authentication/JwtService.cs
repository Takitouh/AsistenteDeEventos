using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AsistenteEventos.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace AsistenteEventos.Security.Authentication;

public interface IJwtService
{
    string GenerateToken(Usuario usuario, Pyme pyme);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(Usuario usuario, Pyme pyme)
    {
        var secret = _configuration["Jwt:Secret"] ?? "SuperClaveSecretaUltraSeguraParaPymesColombia2026!*#";
        var issuer = _configuration["Jwt:Issuer"] ?? "AsistenteEventosPyme";
        var audience = _configuration["Jwt:Audience"] ?? "AsistenteEventosFrontend";
        var expiryHours = int.TryParse(_configuration["Jwt:ExpiryHours"], out int h) ? h : 12;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Email, usuario.Correo),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Role, usuario.Rol),
            new("pyme_id", usuario.PymeId.ToString()),
            new("pyme_nit", pyme.NIT),
            new("pyme_nombre", pyme.RazonSocial),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
