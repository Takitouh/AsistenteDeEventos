using System.Text;
using AsistenteEventos.Application.Services;
using AsistenteEventos.Common.Middlewares;
using AsistenteEventos.Infrastructure.Data;
using AsistenteEventos.Security.Authentication;
using AsistenteEventos.Security.MultiTenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de Base de Datos (PostgreSQL primario con soporte SQLite)
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "SQLite";
if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
{
    var pgConn = builder.Configuration.GetConnectionString("PostgreSQL");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(pgConn));
}
else
{
    var sqliteConn = builder.Configuration.GetConnectionString("SQLite") ?? "Data Source=asistente_eventos.db";
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(sqliteConn));
}

// 2. Multi-Tenancy Aislado
builder.Services.AddScoped<ITenantContext, TenantContext>();

// 3. Servicios de Seguridad y Autenticación
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<IJwtService, JwtService>();

// Configuración de JWT Bearer
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "SuperClaveSecretaUltraSeguraParaPymesColombia2026!*#";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AsistenteEventosPyme";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AsistenteEventosFrontend";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Permite pruebas en desarrollo local
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 4. Inyección de Servicios de Aplicación
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEventoService, EventoService>();
builder.Services.AddScoped<ITareaService, TareaService>();
builder.Services.AddScoped<IPresupuestoService, PresupuestoService>();
builder.Services.AddScoped<IGastoService, GastoService>();
builder.Services.AddScoped<INotificacionService, NotificacionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Servicio Gemini con HttpClient
builder.Services.AddHttpClient<IGeminiService, GeminiService>();

// 5. Controladores y JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddOpenApi();

// Configuración CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Inicialización de Base de Datos y Semilla Multi-Tenant
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher>();
        await DbInitializer.InitializeAsync(context, hasher);
        app.Logger.LogInformation("Base de datos y datos de prueba inicializados exitosamente.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error al inicializar la base de datos: {Message}", ex.Message);
    }
}

// 6. Pipeline HTTP Seguro
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowLocalhost");

// Archivos estáticos del frontend (HTML5, Bootstrap, CSS, JS)
app.UseDefaultFiles();
app.UseStaticFiles();

// Autenticación -> Tenant Middleware -> Autorización
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

app.MapControllers();

// Fallback para SPA / navegación limpia
app.MapFallbackToFile("index.html");

app.Run();

// Habilitar acceso a WebApplicationFactory para pruebas de integración
public partial class Program { }
