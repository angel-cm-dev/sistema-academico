using SistemaAcademico.Api.Infrastructure;
using SistemaAcademico.Application.Common;
using SistemaAcademico.Application.Features.Asistencias;
using SistemaAcademico.Application.Features.Libretas;
using SistemaAcademico.Application.Features.Matriculas;
using SistemaAcademico.Application.Features.Notas;
using SistemaAcademico.Application.Features.Pensiones;

namespace SistemaAcademico.Api.Endpoints;

public static class AcademicoEndpoints
{
    public static IEndpointRouteBuilder MapAcademicoEndpoints(this IEndpointRouteBuilder app)
    {
        // ---------- Matrículas ----------
        var matriculas = app.MapGroup("/api/matriculas").WithTags("Matrículas").RequireAuthorization(Politicas.Autenticado);
        matriculas.MapGet("/", async ([AsParameters] MatriculaQuery q, MatriculaService s, CancellationToken ct) =>
                Results.Ok(await s.ListarAsync(q, ct)))
            .WithSummary("Lista paginada de matrículas (filtros: periodoId, seccionId, estado, search)")
            .Produces<PagedResult<MatriculaDto>>();
        matriculas.MapGet("/{id:int}", async (int id, MatriculaService s, CancellationToken ct) => Results.Ok(await s.ObtenerAsync(id, ct)))
            .WithSummary("Detalle de una matrícula");
        matriculas.MapPost("/", async (CrearMatriculaRequest r, MatriculaService s, CancellationToken ct) =>
            {
                var creada = await s.CrearAsync(r, ct);
                return Results.Created($"/api/matriculas/{creada.Matricula.Id}", creada);
            })
            .Validar<CrearMatriculaRequest>().RequireAuthorization(Politicas.Admin)
            .WithSummary("Matricula a un alumno validando vacantes y genera su cronograma de pensiones")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
        matriculas.MapPost("/{id:int}/retiro", async (int id, MatriculaService s, CancellationToken ct) =>
                Results.Ok(await s.RetirarAsync(id, ct)))
            .RequireAuthorization(Politicas.Admin)
            .WithSummary("Retira la matrícula y libera la vacante");

        // ---------- Notas ----------
        var notas = app.MapGroup("/api/notas").WithTags("Notas").RequireAuthorization(Politicas.AdminODocente);
        notas.MapGet("/", async (int asignacionId, int bimestre, NotaService s, CancellationToken ct) =>
                Results.Ok(await s.ObtenerRegistroAsync(asignacionId, bimestre, ct)))
            .WithSummary("Registro auxiliar: alumnos × competencias de una asignación y bimestre")
            .Produces<RegistroNotasDto>();
        notas.MapPut("/", async (GuardarNotasRequest r, NotaService s, CancellationToken ct) => Results.Ok(await s.GuardarAsync(r, ct)))
            .Validar<GuardarNotasRequest>()
            .WithSummary("Guarda notas en bloque (solo el docente titular o admin)");

        // ---------- Asistencia ----------
        var asistencias = app.MapGroup("/api/asistencias").WithTags("Asistencia").RequireAuthorization(Politicas.Autenticado);
        asistencias.MapGet("/", async (int seccionId, DateOnly fecha, AsistenciaService s, CancellationToken ct) =>
                Results.Ok(await s.ObtenerRegistroAsync(seccionId, fecha, ct)))
            .RequireAuthorization(Politicas.AdminODocente)
            .WithSummary("Asistencia de una sección en una fecha");
        asistencias.MapPut("/", async (GuardarAsistenciaRequest r, AsistenciaService s, CancellationToken ct) =>
                Results.Ok(await s.GuardarAsync(r, ct)))
            .Validar<GuardarAsistenciaRequest>().RequireAuthorization(Politicas.AdminODocente)
            .WithSummary("Registra asistencia en bloque");
        asistencias.MapGet("/resumen/{matriculaId:int}", async (int matriculaId, AsistenciaService s, CancellationToken ct) =>
                Results.Ok(await s.ResumenAsync(matriculaId, ct)))
            .WithSummary("Resumen y porcentaje de asistencia de una matrícula");

        // ---------- Pensiones ----------
        var pensiones = app.MapGroup("/api/pensiones").WithTags("Pensiones").RequireAuthorization(Politicas.Autenticado);
        pensiones.MapGet("/", async ([AsParameters] CuotaQuery q, PensionService s, CancellationToken ct) =>
                Results.Ok(await s.ListarAsync(q, ct)))
            .WithSummary("Cuotas con estado y mora calculada a hoy (apoderado → solo sus hijos)")
            .Produces<PagedResult<CuotaDto>>();
        pensiones.MapPost("/{id:int}/pago", async (int id, RegistrarPagoRequest r, PensionService s, CancellationToken ct) =>
                Results.Ok(await s.RegistrarPagoAsync(id, r, ct)))
            .Validar<RegistrarPagoRequest>().RequireAuthorization(Politicas.Admin)
            .WithSummary("Registra el pago total de una cuota (congela la mora)");

        // ---------- Libreta ----------
        app.MapGet("/api/libretas/{matriculaId:int}", async (int matriculaId, LibretaService s, CancellationToken ct) =>
                Results.Ok(await s.ObtenerAsync(matriculaId, ct)))
            .WithTags("Libretas").RequireAuthorization(Politicas.Autenticado)
            .WithSummary("Libreta de notas de una matrícula")
            .Produces<LibretaDto>();

        return app;
    }
}
