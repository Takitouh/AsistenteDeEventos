namespace AsistenteEventos.Application.DTOs;

public record RegisterRequestDto
{
    public string RazonSocial { get; init; } = string.Empty;
    public string NIT { get; init; } = string.Empty;
    public string Pais { get; init; } = "Colombia";
    public string Departamento { get; init; } = string.Empty;
    public string Municipio { get; init; } = string.Empty;
    public string Ciudad { get; init; } = string.Empty;
    public string Sector { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Correo { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record LoginRequestDto
{
    public string Correo { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record AuthResponseDto
{
    public string Token { get; init; } = string.Empty;
    public int UserId { get; init; }
    public int PymeId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Correo { get; init; } = string.Empty;
    public string Rol { get; init; } = string.Empty;
    public string RazonSocial { get; init; } = string.Empty;
    public string NIT { get; init; } = string.Empty;
    public string Ciudad { get; init; } = string.Empty;
}

public record UserProfileDto
{
    public int UserId { get; init; }
    public int PymeId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Correo { get; init; } = string.Empty;
    public string Rol { get; init; } = string.Empty;
    public string RazonSocial { get; init; } = string.Empty;
    public string NIT { get; init; } = string.Empty;
    public string Ciudad { get; init; } = string.Empty;
    public string Departamento { get; init; } = string.Empty;
    public string Sector { get; init; } = string.Empty;
}
