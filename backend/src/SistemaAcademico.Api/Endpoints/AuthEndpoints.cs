using SistemaAcademico.Api.Infrastructure;
using SistemaAcademico.Application.Features.Auth;
using SistemaAcademico.Application.Features.Dashboard;

namespace SistemaAcademico.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Autenticación");

        auth.MapPost("/login", async (LoginRequest request, AuthService service, CancellationToken ct) =>
                Results.Ok(await service.LoginAsync(request, ct)))
            .Validar<LoginRequest>()
            .AllowAnonymous()
            .RequireRateLimiting("login")
            .WithSummary("Inicia sesión y devuelve un JWT")
            .Produces<LoginResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapGet("/me", async (AuthService service, CancellationToken ct) => Results.Ok(await service.MeAsync(ct)))
            .RequireAuthorization(Politicas.Autenticado)
            .WithSummary("Datos del usuario autenticado")
            .Produces<UsuarioDto>();

        var usuarios = app.MapGroup("/api/usuarios").WithTags("Usuarios").RequireAuthorization(Politicas.Admin);
        usuarios.MapGet("/", async (AuthService service, CancellationToken ct) => Results.Ok(await service.ListarAsync(ct)))
            .WithSummary("Lista los usuarios del sistema");
        usuarios.MapPost("/", async (CrearUsuarioRequest request, AuthService service, CancellationToken ct) =>
            {
                var usuario = await service.CrearAsync(request, ct);
                return Results.Created($"/api/usuarios/{usuario.Id}", usuario);
            })
            .Validar<CrearUsuarioRequest>()
            .WithSummary("Crea una cuenta (admin, docente o apoderado)");

        app.MapGet("/api/dashboard", async (DashboardService service, CancellationToken ct) => Results.Ok(await service.ObtenerAsync(ct)))
            .WithTags("Dashboard")
            .RequireAuthorization(Politicas.Autenticado)
            .WithSummary("KPIs según el rol del usuario")
            .Produces<DashboardDto>();

        return app;
    }
}
