using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SistemaAcademico.Application.Abstractions;
using SistemaAcademico.Application.Common;
using SistemaAcademico.Domain;
using SistemaAcademico.Domain.Common;
using SistemaAcademico.Domain.Entities;
using SistemaAcademico.Domain.Reglas;

namespace SistemaAcademico.Application.Features.Asistencias;

public sealed record FilaAsistenciaDto(int MatriculaId, string Alumno, string CodigoAlumno, string? Estado, string? Observacion);

public sealed record RegistroAsistenciaDto(int SeccionId, string Seccion, DateOnly Fecha, IReadOnlyList<FilaAsistenciaDto> Filas);

public sealed record AsistenciaInput(int MatriculaId, EstadoAsistencia Estado, string? Observacion);

public sealed record GuardarAsistenciaRequest(int SeccionId, DateOnly Fecha, IReadOnlyList<AsistenciaInput> Registros);

public sealed record ResumenAsistenciaDto(int MatriculaId, int Presentes, int Tardanzas, int Faltas, int FaltasJustificadas,
    int Total, decimal Porcentaje, bool EnRiesgo);

public sealed class GuardarAsistenciaValidator : AbstractValidator<GuardarAsistenciaRequest>
{
    public GuardarAsistenciaValidator(IClock clock)
    {
        RuleFor(x => x.SeccionId).GreaterThan(0);
        // No se registra asistencia "a futuro": sería un dato inventado.
        RuleFor(x => x.Fecha).LessThanOrEqualTo(_ => clock.Hoy).WithMessage("No se puede registrar asistencia de una fecha futura.");
        RuleFor(x => x.Registros).NotEmpty();
        RuleForEach(x => x.Registros).ChildRules(r =>
        {
            r.RuleFor(x => x.Estado).IsInEnum();
            r.RuleFor(x => x.Observacion).MaximumLength(250);
        });
    }
}

public sealed class AsistenciaService(IAppDbContext db, ControlAcceso acceso, ICurrentUser user)
{
    public async Task<RegistroAsistenciaDto> ObtenerRegistroAsync(int seccionId, DateOnly fecha, CancellationToken ct)
    {
        await acceso.AsegurarAccesoSeccionAsync(seccionId, ct);
        var seccion = await db.Secciones.Include(s => s.Grado).FirstAsync(s => s.Id == seccionId, ct);

        var filas = await db.Matriculas.AsNoTracking()
            .Where(m => m.SeccionId == seccionId && m.Estado == EstadoMatricula.Activa)
            .OrderBy(m => m.Alumno.Apellidos).ThenBy(m => m.Alumno.Nombres)
            .Select(m => new
            {
                m.Id,
                Alumno = m.Alumno.Apellidos + ", " + m.Alumno.Nombres,
                m.Alumno.Codigo,
                Registro = m.Asistencias.Where(a => a.Fecha == fecha).Select(a => new { a.Estado, a.Observacion }).FirstOrDefault()
            })
            .ToListAsync(ct);

        return new RegistroAsistenciaDto(seccion.Id, $"{seccion.Grado.Nombre} {seccion.Letra}", fecha,
            filas.Select(f => new FilaAsistenciaDto(f.Id, f.Alumno, f.Codigo, f.Registro?.Estado.ToString(), f.Registro?.Observacion)).ToList());
    }

    public async Task<RegistroAsistenciaDto> GuardarAsync(GuardarAsistenciaRequest r, CancellationToken ct)
    {
        await acceso.AsegurarAccesoSeccionAsync(r.SeccionId, ct);
        var seccion = await db.Secciones.Include(s => s.Periodo).FirstAsync(s => s.Id == r.SeccionId, ct);
        if (r.Fecha < seccion.Periodo.FechaInicio || r.Fecha > seccion.Periodo.FechaFin)
            throw new DomainException("La fecha está fuera del periodo académico de la sección.");

        var validas = (await db.Matriculas
            .Where(m => m.SeccionId == r.SeccionId && m.Estado == EstadoMatricula.Activa)
            .Select(m => m.Id).ToListAsync(ct)).ToHashSet();
        var invalida = r.Registros.FirstOrDefault(x => !validas.Contains(x.MatriculaId));
        if (invalida is not null)
            throw new ForbiddenException($"La matrícula {invalida.MatriculaId} no pertenece a la sección.");

        var ids = r.Registros.Select(x => x.MatriculaId).ToList();
        var existentes = await db.Asistencias.Where(a => a.Fecha == r.Fecha && ids.Contains(a.MatriculaId)).ToListAsync(ct);

        foreach (var input in r.Registros)
        {
            var registro = existentes.FirstOrDefault(a => a.MatriculaId == input.MatriculaId);
            if (registro is null)
            {
                registro = new Asistencia { MatriculaId = input.MatriculaId, Fecha = r.Fecha };
                db.Asistencias.Add(registro);
                existentes.Add(registro);
            }
            registro.Estado = input.Estado;
            registro.Observacion = string.IsNullOrWhiteSpace(input.Observacion) ? null : input.Observacion.Trim();
            registro.RegistradoPorUsuarioId = user.UserId;
        }
        await db.SaveChangesAsync(ct);
        return await ObtenerRegistroAsync(r.SeccionId, r.Fecha, ct);
    }

    public async Task<ResumenAsistenciaDto> ResumenAsync(int matriculaId, CancellationToken ct)
    {
        await acceso.AsegurarAccesoMatriculaAsync(matriculaId, ct);
        var estados = await db.Asistencias.Where(a => a.MatriculaId == matriculaId).Select(a => a.Estado).ToListAsync(ct);
        var r = ResumenAsistencia.Desde(estados);
        return new ResumenAsistenciaDto(matriculaId, r.Presentes, r.Tardanzas, r.Faltas, r.FaltasJustificadas, r.Total, r.Porcentaje, r.EnRiesgo);
    }
}
