using SistemaAcademico.Api.Infrastructure;
using SistemaAcademico.Application.Features.Estructura;

namespace SistemaAcademico.Api.Endpoints;

public static class EstructuraEndpoints
{
    public static IEndpointRouteBuilder MapEstructuraEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api").WithTags("Estructura académica").RequireAuthorization(Politicas.Autenticado);

        g.MapGet("/periodos", async (EstructuraService s, CancellationToken ct) => Results.Ok(await s.ListarPeriodosAsync(ct)))
            .WithSummary("Periodos académicos");
        g.MapPost("/periodos", async (CrearPeriodoRequest r, EstructuraService s, CancellationToken ct) =>
                Results.Created("/api/periodos", await s.CrearPeriodoAsync(r, ct)))
            .Validar<CrearPeriodoRequest>().RequireAuthorization(Politicas.Admin)
            .WithSummary("Crea un periodo (si es activo, desactiva el resto)");

        g.MapGet("/grados", async (EstructuraService s, CancellationToken ct) => Results.Ok(await s.ListarGradosAsync(ct)))
            .WithSummary("Grados");
        g.MapPost("/grados", async (CrearGradoRequest r, EstructuraService s, CancellationToken ct) =>
                Results.Created("/api/grados", await s.CrearGradoAsync(r, ct)))
            .Validar<CrearGradoRequest>().RequireAuthorization(Politicas.Admin)
            .WithSummary("Crea un grado");

        g.MapGet("/secciones", async (int? periodoId, int? gradoId, EstructuraService s, CancellationToken ct) =>
                Results.Ok(await s.ListarSeccionesAsync(periodoId, gradoId, ct)))
            .WithSummary("Secciones con capacidad, matriculados y vacantes");
        g.MapPost("/secciones", async (CrearSeccionRequest r, EstructuraService s, CancellationToken ct) =>
                Results.Created("/api/secciones", await s.CrearSeccionAsync(r, ct)))
            .Validar<CrearSeccionRequest>().RequireAuthorization(Politicas.Admin)
            .WithSummary("Crea una sección con su capacidad (vacantes)");

        g.MapGet("/cursos", async (int? gradoId, EstructuraService s, CancellationToken ct) =>
                Results.Ok(await s.ListarCursosAsync(gradoId, ct)))
            .RequireAuthorization(Politicas.AdminODocente)
            .WithSummary("Cursos con sus competencias (el docente solo ve los suyos)");
        g.MapPost("/cursos", async (CrearCursoRequest r, EstructuraService s, CancellationToken ct) =>
                Results.Created("/api/cursos", await s.CrearCursoAsync(r, ct)))
            .Validar<CrearCursoRequest>().RequireAuthorization(Politicas.Admin)
            .WithSummary("Crea un curso con sus competencias y pesos");
        g.MapPut("/cursos/{id:int}/competencias", async (int id, ActualizarCompetenciasRequest r, EstructuraService s, CancellationToken ct) =>
                Results.Ok(await s.ActualizarCompetenciasAsync(id, r, ct)))
            .Validar<ActualizarCompetenciasRequest>().RequireAuthorization(Politicas.Admin)
            .WithSummary("Configura competencias y pesos del promedio ponderado");

        g.MapGet("/asignaciones", async (int? periodoId, EstructuraService s, CancellationToken ct) =>
                Results.Ok(await s.ListarAsignacionesAsync(periodoId, ct)))
            .RequireAuthorization(Politicas.AdminODocente)
            .WithSummary("Asignaciones docente-curso-sección (el docente solo ve las suyas)");
        g.MapPost("/asignaciones", async (CrearAsignacionRequest r, EstructuraService s, CancellationToken ct) =>
                Results.Created("/api/asignaciones", await s.CrearAsignacionAsync(r, ct)))
            .RequireAuthorization(Politicas.Admin)
            .WithSummary("Asigna un docente a un curso en una sección");

        return app;
    }
}
