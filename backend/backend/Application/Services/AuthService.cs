using AsistenteEventos.Application.DTOs;
using AsistenteEventos.Domain.Entities;
using AsistenteEventos.Infrastructure.Data;
using AsistenteEventos.Security.Authentication;
using AsistenteEventos.Security.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AsistenteEventos.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<UserProfileDto> GetProfileAsync();
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        ITenantContext tenantContext,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        // Validaciones iniciales
        if (string.IsNullOrWhiteSpace(request.RazonSocial) || string.IsNullOrWhiteSpace(request.NIT))
            throw new ArgumentException("La Razón Social y el NIT de la PYME son obligatorios.");

        if (string.IsNullOrWhiteSpace(request.Correo) || string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("El correo y la contraseña son obligatorios.");

        // Verificar unicidad de correo y NIT utilizando IgnoreQueryFilters
        var emailExists = await _context.Usuarios.IgnoreQueryFilters()
            .AnyAsync(u => u.Correo.ToLower() == request.Correo.Trim().ToLower());
        if (emailExists)
            throw new InvalidOperationException("El correo electrónico ya se encuentra registrado.");

        var nitExists = await _context.Pymes.IgnoreQueryFilters()
            .AnyAsync(p => p.NIT.Trim() == request.NIT.Trim());
        if (nitExists)
            throw new InvalidOperationException("El NIT de la PYME ya se encuentra registrado.");

        // Iniciar transacción de creación atómica de PYME + Usuario
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var pyme = new Pyme
            {
                RazonSocial = request.RazonSocial.Trim(),
                NIT = request.NIT.Trim(),
                Pais = string.IsNullOrWhiteSpace(request.Pais) ? "Colombia" : request.Pais.Trim(),
                Departamento = request.Departamento.Trim(),
                Municipio = request.Municipio.Trim(),
                Ciudad = string.IsNullOrWhiteSpace(request.Ciudad) ? request.Municipio.Trim() : request.Ciudad.Trim(),
                Sector = request.Sector.Trim(),
                FechaRegistro = DateTime.UtcNow
            };

            _context.Pymes.Add(pyme);
            await _context.SaveChangesAsync();

            var passwordHash = _passwordHasher.HashPassword(request.Password);

            var usuario = new Usuario
            {
                PymeId = pyme.Id,
                Nombre = request.Nombre.Trim(),
                Correo = request.Correo.Trim().ToLower(),
                PasswordHash = passwordHash,
                Rol = "Administrador",
                Activo = true,
                FechaRegistro = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Nueva PYME registrada: {RazonSocial} con usuario {Correo}", pyme.RazonSocial, usuario.Correo);

            var token = _jwtService.GenerateToken(usuario, pyme);

            return new AuthResponseDto
            {
                Token = token,
                UserId = usuario.Id,
                PymeId = pyme.Id,
                Nombre = usuario.Nombre,
                Correo = usuario.Correo,
                Rol = usuario.Rol,
                RazonSocial = pyme.RazonSocial,
                NIT = pyme.NIT,
                Ciudad = pyme.Ciudad
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error durante el registro de la PYME: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Correo) || string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("El correo y la contraseña son obligatorios.");

        // Buscar usuario en toda la base (sin filtro tenant inicial ya que aún no hay token)
        var usuario = await _context.Usuarios.IgnoreQueryFilters()
            .Include(u => u.Pyme)
            .FirstOrDefaultAsync(u => u.Correo.ToLower() == request.Correo.Trim().ToLower() && u.Activo);

        if (usuario == null || !_passwordHasher.VerifyPassword(request.Password, usuario.PasswordHash))
        {
            throw new UnauthorizedAccessException("Credenciales de acceso inválidas.");
        }

        if (usuario.Pyme == null)
        {
            throw new InvalidOperationException("La cuenta de usuario no está asociada a una PYME válida.");
        }

        var token = _jwtService.GenerateToken(usuario, usuario.Pyme);

        _logger.LogInformation("Inicio de sesión exitoso para {Correo}, PymeId {PymeId}", usuario.Correo, usuario.PymeId);

        return new AuthResponseDto
        {
            Token = token,
            UserId = usuario.Id,
            PymeId = usuario.PymeId,
            Nombre = usuario.Nombre,
            Correo = usuario.Correo,
            Rol = usuario.Rol,
            RazonSocial = usuario.Pyme.RazonSocial,
            NIT = usuario.Pyme.NIT,
            Ciudad = usuario.Pyme.Ciudad
        };
    }

    public async Task<UserProfileDto> GetProfileAsync()
    {
        if (!_tenantContext.IsAuthenticated || !_tenantContext.UsuarioId.HasValue)
            throw new UnauthorizedAccessException("No se encuentra una sesión autenticada válida.");

        var usuario = await _context.Usuarios
            .Include(u => u.Pyme)
            .FirstOrDefaultAsync(u => u.Id == _tenantContext.UsuarioId.Value && u.PymeId == _tenantContext.PymeId!.Value);

        if (usuario == null || usuario.Pyme == null)
            throw new KeyNotFoundException("Perfil de usuario no encontrado.");

        return new UserProfileDto
        {
            UserId = usuario.Id,
            PymeId = usuario.PymeId,
            Nombre = usuario.Nombre,
            Correo = usuario.Correo,
            Rol = usuario.Rol,
            RazonSocial = usuario.Pyme.RazonSocial,
            NIT = usuario.Pyme.NIT,
            Ciudad = usuario.Pyme.Ciudad,
            Departamento = usuario.Pyme.Departamento,
            Sector = usuario.Pyme.Sector
        };
    }
}
