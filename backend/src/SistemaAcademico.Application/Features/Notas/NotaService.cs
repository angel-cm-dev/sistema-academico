using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SistemaAcademico.Application.Abstractions;
using SistemaAcademico.Application.Common;
using SistemaAcademico.Application.Features.Estructura;
using SistemaAcademico.Domain;
using SistemaAcademico.Domain.Common;
using SistemaAcademico.Domain.Entities;
using SistemaAcademico.Domain.Reglas;

namespace SistemaAcademico.Application.Features.Notas;

public sealed record NotaCeldaDto(int CompetenciaId, decimal? Valor);

public sealed record FilaNotasDto(int MatriculaId, string Alumno, string CodigoAlumno, IReadOnlyList<NotaCeldaDto> Notas,
    decimal? PromedioExacto, int? Promedio, string? Literal, bool? Aprobado);

public sealed record RegistroNotasDto(int AsignacionId, string Curso, string Seccion, string Docente, int Bimestre,
    IReadOnlyList<CompetenciaDto> Competencias, IReadOnlyList<FilaNotasDto> Filas);

public sealed record NotaInput(int MatriculaId, int CompetenciaId, decimal? Valor);

public sealed record GuardarNotasRequest(int AsignacionId, int Bimestre, IReadOnlyList<NotaInput> Notas);

public sealed class GuardarNotasValidator : AbstractValidator<GuardarNotasRequest>
{
    public GuardarNotasValidator()
    {
        RuleFor(x => x.AsignacionId).GreaterThan(0);
        RuleFor(x => x.Bimestre).InclusiveBetween(Nota.BimestreMinimo, Nota.BimestreMaximo);
        RuleFor(x => x.Notas).NotNull();
        RuleForEach(x => x.Notas).ChildRules(n =>
        {
            n.RuleFor(x => x.Valor!.Value)
                .InclusiveBetween(EscalaVigesimal.NotaMinima, EscalaVigesimal.NotaMaxima)
                .WithMessage("La nota debe estar entre 0 y 20.")
                .PrecisionScale(4, 2, true)
                .When(x => x.Valor is not null);
        });
    }
}

public sealed class NotaService(IAppDbContext db, ControlAcceso acceso, ICurrentUser user, IClock clock)
{
    public async Task<RegistroNotasDto> ObtenerRegistroAsync(int asignacionId, int bimestre, CancellationToken ct)
    {
        Nota.ValidarBimestre(bimestre);
        var asignacion = await acceso.AsegurarAsignacionPropiaAsync(asignacionId, ct);
        var competencias = asignacion.Curso.Competencias.OrderBy(c => c.Orden).ToList();
        var competenciaIds = competencias.Select(c => c.Id).ToList();

        var matriculas = await db.Matriculas.AsNoTracking()
            .Where(m => m.SeccionId == asignacion.SeccionId && m.Estado == EstadoMatricula.Activa)
            .OrderBy(m => m.Alumno.Apellidos).ThenBy(m => m.Alumno.Nombres)
            .Select(m => new { m.Id, Alumno = m.Alumno.Apellidos + ", " + m.Alumno.Nombres, m.Alumno.Codigo })
            .ToListAsync(ct);

        var matriculaIds = matriculas.Select(m => m.Id).ToList();
        var notas = await db.Notas.AsNoTracking()
            .Where(n => n.Bimestre == bimestre && matriculaIds.Contains(n.MatriculaId) && competenciaIds.Contains(n.CompetenciaId))
            .ToListAsync(ct);

        var filas = matriculas.Select(m =>
        {
            var celdas = competencias
                .Select(c => new NotaCeldaDto(c.Id, notas.FirstOrDefault(n => n.MatriculaId == m.Id && n.CompetenciaId == c.Id)?.Valor))
                .ToList();
            var ponderadas = celdas.Where(c => c.Valor is not null)
                .Select(c => new NotaPonderada(c.Valor!.Value, competencias.First(x => x.Id == c.CompetenciaId).Peso));
            var exacto = CalculadoraPromedio.PromedioPonderado(ponderadas);
            int? redondeado = exacto is null ? null : EscalaVigesimal.Redondear(exacto.Value);
            return new FilaNotasDto(m.Id, m.Alumno, m.Codigo, celdas, exacto, redondeado,
                redondeado is null ? null : EscalaVigesimal.Literal(redondeado.Value),
                redondeado is null ? null : EscalaVigesimal.Aprueba(redondeado.Value));
        }).ToList();

        return new RegistroNotasDto(asignacion.Id, asignacion.Curso.Nombre,
            $"{asignacion.Seccion.Grado.Nombre} {asignacion.Seccion.Letra}",
            $"{asignacion.Docente.Apellidos}, {asignacion.Docente.Nombres}", bimestre,
            competencias.Select(c => new CompetenciaDto(c.Id, c.Nombre, c.Peso, c.Orden)).ToList(), filas);
    }

    /// <summary>
    /// Guarda notas en bloque (upsert). Valor null borra la nota, lo que permite corregir
    /// un registro accidental. Todo o nada: si una celda es inválida no se guarda ninguna.
    /// </summary>
    public async Task<RegistroNotasDto> GuardarAsync(GuardarNotasRequest r, CancellationToken ct)
    {
        Nota.ValidarBimestre(r.Bimestre);
        var asignacion = await acceso.AsegurarAsignacionPropiaAsync(r.AsignacionId, ct);
        var competenciaIds = asignacion.Curso.Competencias.Select(c => c.Id).ToHashSet();

        var matriculasValidas = (await db.Matriculas
            .Where(m => m.SeccionId == asignacion.SeccionId && m.Estado == EstadoMatricula.Activa)
            .Select(m => m.Id).ToListAsync(ct)).ToHashSet();

        // Defensa contra manipulación del payload: un docente no puede "colar" notas
        // de alumnos de otra sección o de competencias de otro curso en su asignación.
        foreach (var n in r.Notas)
        {
            if (!matriculasValidas.Contains(n.MatriculaId))
                throw new ForbiddenException($"La matrícula {n.MatriculaId} no pertenece a la sección de esta asignación.");
            if (!competenciaIds.Contains(n.CompetenciaId))
                throw new DomainException($"La competencia {n.CompetenciaId} no pertenece al curso.");
        }

        var ids = r.Notas.Select(n => n.MatriculaId).Distinct().ToList();
        var compIds = competenciaIds.ToList();
        var existentes = await db.Notas
            .Where(n => n.Bimestre == r.Bimestre && ids.Contains(n.MatriculaId) && compIds.Contains(n.CompetenciaId))
            .ToListAsync(ct);

        var ahora = clock.UtcNow;
        foreach (var input in r.Notas)
        {
            var nota = existentes.FirstOrDefault(n => n.MatriculaId == input.MatriculaId && n.CompetenciaId == input.CompetenciaId);
            if (input.Valor is null)
            {
                if (nota is not null) db.Notas.Remove(nota);
                continue;
            }
            if (nota is null)
            {
                nota = new Nota { MatriculaId = input.MatriculaId, CompetenciaId = input.CompetenciaId, Bimestre = r.Bimestre };
                db.Notas.Add(nota);
                existentes.Add(nota);
            }
            nota.Asignar(input.Valor.Value, user.UserId, ahora);
        }
        await db.SaveChangesAsync(ct);
        return await ObtenerRegistroAsync(r.AsignacionId, r.Bimestre, ct);
    }
}
