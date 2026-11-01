using SistemaAcademico.Api.Infrastructure;
using SistemaAcademico.Application.Common;
using SistemaAcademico.Application.Features.Personas;

namespace SistemaAcademico.Api.Endpoints;

public static class PersonasEndpoints
{
    public static IEndpointRouteBuilder MapPersonasEndpoints(this IEndpointRouteBuilder app)
    {
        var alumnos = app.MapGroup("/api/alumnos").WithTags("Alumnos").RequireAuthorization(Politicas.Autenticado);

        alumnos.MapGet("/", async ([AsParameters] AlumnoQuery q, PersonasService s, CancellationToken ct) =>
                Results.Ok(await s.ListarAlumnosAsync(q, ct)))
            .WithSummary("Lista paginada de alumnos (filtrada por rol: apoderado → sus hijos; docente → sus secciones)")
            .Produces<PagedResult<AlumnoDto>>();
        alumnos.MapGet("/{id:int}", async (int id, PersonasService s, CancellationToken ct) => Results.Ok(await s.ObtenerAlumnoAsync(id, ct)))
            .WithSummary("Detalle de un alumno (403 si no le pertenece)");
        alumnos.MapPost("/", async (GuardarAlumnoRequest r, PersonasService s, CancellationToken ct) =>
            {
                var alumno = await s.CrearAlumnoAsync(r, ct);
                return Results.Created($"/api/alumnos/{alumno.Id}", alumno);
            })
            .Validar<GuardarAlumnoRequest>().RequireAuthorization(Politicas.Admin)
            .WithSummary("Registra un alumno");
        alumnos.MapPut("/{id:int}", async (int id, GuardarAlumnoRequest r, PersonasService s, CancellationToken ct) =>
                Results.Ok(await s.ActualizarAlumnoAsync(id, r, ct)))
            .Validar<GuardarAlumnoRequest>().RequireAuthorization(Politicas.Admin)
            .WithSummary("Actualiza un alumno");
        alumnos.MapDelete("/{id:int}", async (int id, PersonasService s, CancellationToken ct) =>
            {
                await s.DesactivarAlumnoAsync(id, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Politicas.Admin)
            .WithSummary("Baja lógica del alumno");

        var apoderados = app.MapGroup("/api/apoderados").WithTags("Apoderados").RequireAuthorization(Politicas.Admin);
        apoderados.MapGet("/", async ([AsParameters] PageQuery q, PersonasService s, CancellationToken ct) =>
                Results.Ok(await s.ListarApoderadosAsync(q, ct)))
            .WithSummary("Lista paginada de apoderados");
        apoderados.MapPost("/", async (GuardarApoderadoRequest r, PersonasService s, CancellationToken ct) =>
                Results.Created("/api/apoderados", await s.CrearApoderadoAsync(r, ct)))
            .Validar<GuardarApoderadoRequest>()
            .WithSummary("Registra un apoderado");

        var docentes = app.MapGroup("/api/docentes").WithTags("Docentes").RequireAuthorization(Politicas.Admin);
        docentes.MapGet("/", async ([AsParameters] PageQuery q, PersonasService s, CancellationToken ct) =>
                Results.Ok(await s.ListarDocentesAsync(q, ct)))
            .WithSummary("Lista paginada de docentes");
        docentes.MapPost("/", async (GuardarDocenteRequest r, PersonasService s, CancellationToken ct) =>
                Results.Created("/api/docentes", await s.CrearDocenteAsync(r, ct)))
            .Validar<GuardarDocenteRequest>()
            .WithSummary("Registra un docente");

        return app;
    }
}
