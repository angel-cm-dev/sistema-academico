using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;
using SistemaAcademico.Api.Endpoints;
using SistemaAcademico.Api.Infrastructure;
using SistemaAcademico.Application;
using SistemaAcademico.Application.Abstractions;
using SistemaAcademico.Domain;
using SistemaAcademico.Infrastructure;
using SistemaAcademico.Infrastructure.Persistence;
using SistemaAcademico.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// ---------- Logging estructurado (JSON compacto en consola, ideal para Docker/ELK/Loki) ----------
builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter()));

// ---------- Capas ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// ---------- Errores → ProblemDetails ----------
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Enums como texto en JSON ("Docente" en vez de 1): contratos legibles y estables.
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---------- Autenticación JWT + autorización por rol ----------
var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false; // conservar "sub", "role", etc. tal como se emitieron
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimsPersonalizados.Rol,
            NameClaimType = "name"
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Politicas.Admin, p => p.RequireRole(nameof(Rol.Admin)))
    .AddPolicy(Politicas.AdminODocente, p => p.RequireRole(nameof(Rol.Admin), nameof(Rol.Docente)))
    .AddPolicy(Politicas.Autenticado, p => p.RequireAuthenticatedUser());

// Limita intentos de login por IP para frenar ataques de fuerza bruta.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("RateLimiting:LoginPorMinuto", 10),
            Window = TimeSpan.FromMinutes(1)
        }));
});

// ---------- CORS (solo necesario si el frontend no pasa por el proxy de nginx/Vite) ----------
var origenes = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod()));

// ---------- OpenAPI + Scalar ----------
builder.Services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapOpenApi();
app.MapScalarApiReference(o => o.WithTitle("Sistema Académico API"));
app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
app.MapHealthChecks("/health");

app.MapAuthEndpoints();
app.MapEstructuraEndpoints();
app.MapPersonasEndpoints();
app.MapAcademicoEndpoints();

await app.Services.InicializarBaseDeDatosAsync(app.Configuration);
await app.RunAsync();

public partial class Program;
