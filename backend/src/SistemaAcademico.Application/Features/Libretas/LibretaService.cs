using Microsoft.EntityFrameworkCore;
using SistemaAcademico.Application.Abstractions;
using SistemaAcademico.Application.Common;
using SistemaAcademico.Application.Features.Asistencias;
using SistemaAcademico.Domain.Entities;
using SistemaAcademico.Domain.Reglas;

namespace SistemaAcademico.Application.Features.Libretas;

public sealed record CompetenciaLibretaDto(int Id, string Nombre, decimal Peso, IReadOnlyList<decimal?> Notas);

public sealed record CursoLibretaDto(int CursoId, string Curso, string? Docente, IReadOnlyList<CompetenciaLibretaDto> Competencias,
    IReadOnlyList<int?> PromediosBimestrales, int? PromedioFinal, string? Literal, bool? Aprobado);

public sealed record LibretaDto(int MatriculaId, string CodigoAlumno, string Alumno, string Dni, string Apoderado,
    string Grado, string Seccion, string Nivel, int Anio, IReadOnlyList<CursoLibretaDto> Cursos,
    ResumenAsistenciaDto Asistencia, decimal? PromedioGeneral, int CursosAprobados, int CursosDesaprobados,
    string Situacion, DateOnly FechaEmision);

/// <summary>
/// Arma la libreta de notas (boleta) de una matrícula: notas por competencia y bimestre,
/// promedios ponderados, promedio final por curso, resumen de asistencia y situación final.
/// </summary>
public sealed class LibretaService(IAppDbContext db, ControlAcceso acceso, AsistenciaService asistencia, IClock clock)
{
    public async Task<LibretaDto> ObtenerAsync(int matriculaId, CancellationToken ct)
    {
        await acceso.AsegurarAccesoMatriculaAsync(matriculaId, ct);

        var m = await db.Matriculas.AsNoTracking()
            .Include(x => x.Alumno).ThenInclude(a => a.Apoderado)
            .Include(x => x.Seccion).ThenInclude(s => s.Grado)
            .Include(x => x.Periodo)
            .Include(x => x.Notas)
            .FirstAsync(x => x.Id == matriculaId, ct);

        var cursos = await db.Cursos.AsNoTracking().Include(c => c.Competencias)
            .Where(c => c.GradoId == m.Seccion.GradoId).OrderBy(c => c.Nombre).ToListAsync(ct);

        var docentes = await db.Asignaciones.AsNoTracking()
            .Where(a => a.SeccionId == m.SeccionId)
            .Select(a => new { a.CursoId, Docente = a.Docente.Apellidos + ", " + a.Docente.Nombres })
            .ToDictionaryAsync(a => a.CursoId, a => a.Docente, ct);

        var bimestres = Enumerable.Range(Nota.BimestreMinimo, Nota.BimestreMaximo - Nota.BimestreMinimo + 1).ToList();
        var evaluacionCompleta = true;
        var filas = new List<CursoLibretaDto>();

        foreach (var curso in cursos)
        {
            var competencias = curso.Competencias.OrderBy(c => c.Orden).ToList();
            var detalle = competencias.Select(c => new CompetenciaLibretaDto(c.Id, c.Nombre, c.Peso,
                bimestres.Select(b => m.Notas.FirstOrDefault(n => n.CompetenciaId == c.Id && n.Bimestre == b)?.Valor).ToList()))
                .ToList();

            var promedios = bimestres.Select(b => CalculadoraPromedio.PromedioBimestral(
                competencias
                    .Select(c => (c.Peso, Nota: m.Notas.FirstOrDefault(n => n.CompetenciaId == c.Id && n.Bimestre == b)))
                    .Where(x => x.Nota is not null)
                    .Select(x => new NotaPonderada(x.Nota!.Valor, x.Peso)))).ToList();

            if (promedios.Any(p => p is null)) evaluacionCompleta = false;
            var final = CalculadoraPromedio.PromedioFinal(promedios);
            filas.Add(new CursoLibretaDto(curso.Id, curso.Nombre, docentes.GetValueOrDefault(curso.Id), detalle, promedios, final,
                final is null ? null : EscalaVigesimal.Literal(final.Value),
                final is null ? null : EscalaVigesimal.Aprueba(final.Value)));
        }

        var finales = filas.Where(f => f.PromedioFinal is not null).Select(f => f.PromedioFinal!.Value).ToList();
        var desaprobados = filas.Count(f => f.Aprobado == false);
        decimal? promedioGeneral = finales.Count == 0 ? null
            : Math.Round((decimal)finales.Average(), 2, MidpointRounding.AwayFromZero);

        return new LibretaDto(m.Id, m.Alumno.Codigo, $"{m.Alumno.Apellidos}, {m.Alumno.Nombres}", m.Alumno.Dni,
            $"{m.Alumno.Apoderado.Apellidos}, {m.Alumno.Apoderado.Nombres}", m.Seccion.Grado.Nombre, m.Seccion.Letra,
            m.Seccion.Grado.Nivel.ToString(), m.Periodo.Anio, filas,
            await asistencia.ResumenAsync(matriculaId, ct), promedioGeneral,
            filas.Count(f => f.Aprobado == true), desaprobados,
            ReglasPromocion.Situacion(desaprobados, evaluacionCompleta && cursos.Count > 0), clock.Hoy);
    }
}
