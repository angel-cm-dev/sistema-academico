using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SistemaAcademico.Application.Abstractions;
using SistemaAcademico.Application.Common;
using SistemaAcademico.Domain;
using SistemaAcademico.Domain.Common;
using SistemaAcademico.Domain.Entities;
using SistemaAcademico.Domain.Reglas;

namespace SistemaAcademico.Application.Features.Estructura;

// ---------- DTOs ----------
public sealed record PeriodoDto(int Id, int Anio, string Nombre, DateOnly FechaInicio, DateOnly FechaFin,
    decimal PensionMensual, bool Activo);

public sealed record CrearPeriodoRequest(int Anio, string Nombre, DateOnly FechaInicio, DateOnly FechaFin,
    decimal PensionMensual, bool Activo);

public sealed record GradoDto(int Id, string Nombre, string Nivel, int Orden);

public sealed record CrearGradoRequest(string Nombre, NivelEducativo Nivel, int Orden);

public sealed record SeccionDto(int Id, int GradoId, string Grado, string Letra, int PeriodoId, int Anio,
    int Capacidad, int Matriculados, int Vacantes, decimal Ocupacion);

public sealed record CrearSeccionRequest(int GradoId, int PeriodoId, string Letra, int Capacidad);

public sealed record CompetenciaDto(int Id, string Nombre, decimal Peso, int Orden);

public sealed record CursoDto(int Id, string Codigo, string Nombre, int GradoId, string Grado,
    IReadOnlyList<CompetenciaDto> Competencias);

public sealed record CompetenciaInput(int? Id, string Nombre, decimal Peso);

public sealed record CrearCursoRequest(string Codigo, string Nombre, int GradoId, IReadOnlyList<CompetenciaInput> Competencias);

public sealed record ActualizarCompetenciasRequest(IReadOnlyList<CompetenciaInput> Competencias);

public sealed record AsignacionDto(int Id, int DocenteId, string Docente, int CursoId, string Curso,
    int SeccionId, string Seccion, int PeriodoId);

public sealed record CrearAsignacionRequest(int DocenteId, int CursoId, int SeccionId);

// ---------- Validadores ----------
public sealed class CrearPeriodoValidator : AbstractValidator<CrearPeriodoRequest>
{
    public CrearPeriodoValidator()
    {
        RuleFor(x => x.Anio).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(60);
        RuleFor(x => x.FechaFin).GreaterThan(x => x.FechaInicio).WithMessage("La fecha de fin debe ser posterior al inicio.");
        RuleFor(x => x.PensionMensual).GreaterThan(0).LessThanOrEqualTo(10000);
    }
}

public sealed class CrearGradoValidator : AbstractValidator<CrearGradoRequest>
{
    public CrearGradoValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Nivel).IsInEnum();
        RuleFor(x => x.Orden).InclusiveBetween(1, 20);
    }
}

public sealed class CrearSeccionValidator : AbstractValidator<CrearSeccionRequest>
{
    public CrearSeccionValidator()
    {
        RuleFor(x => x.Letra).NotEmpty().MaximumLength(2).Matches("^[A-Z]+$").WithMessage("La sección debe ser una letra mayúscula.");
        RuleFor(x => x.Capacidad).InclusiveBetween(1, 60);
    }
}

public sealed class CompetenciaInputValidator : AbstractValidator<CompetenciaInput>
{
    public CompetenciaInputValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Peso).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

public sealed class CrearCursoValidator : AbstractValidator<CrearCursoRequest>
{
    public CrearCursoValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Competencias).NotEmpty();
        RuleForEach(x => x.Competencias).SetValidator(new CompetenciaInputValidator());
    }
}

public sealed class ActualizarCompetenciasValidator : AbstractValidator<ActualizarCompetenciasRequest>
{
    public ActualizarCompetenciasValidator()
    {
        RuleFor(x => x.Competencias).NotEmpty();
        RuleForEach(x => x.Competencias).SetValidator(new CompetenciaInputValidator());
    }
}

// ---------- Servicio ----------
public sealed class EstructuraService(IAppDbContext db, ICurrentUser user)
{
    public async Task<IReadOnlyList<PeriodoDto>> ListarPeriodosAsync(CancellationToken ct) =>
        await db.Periodos.OrderByDescending(p => p.Anio)
            .Select(p => new PeriodoDto(p.Id, p.Anio, p.Nombre, p.FechaInicio, p.FechaFin, p.PensionMensual, p.Activo))
            .ToListAsync(ct);

    public async Task<PeriodoDto> CrearPeriodoAsync(CrearPeriodoRequest r, CancellationToken ct)
    {
        if (await db.Periodos.AnyAsync(p => p.Anio == r.Anio, ct))
            throw new ConflictException($"Ya existe el periodo {r.Anio}.");

        // Invariante: un solo periodo activo. Activar uno nuevo desactiva el resto.
        if (r.Activo)
            await db.Periodos.Where(p => p.Activo).ForEachAsync(p => p.Activo = false, ct);

        var periodo = new PeriodoAcademico
        {
            Anio = r.Anio, Nombre = r.Nombre.Trim(), FechaInicio = r.FechaInicio, FechaFin = r.FechaFin,
            PensionMensual = r.PensionMensual, Activo = r.Activo
        };
        db.Periodos.Add(periodo);
        await db.SaveChangesAsync(ct);
        return new PeriodoDto(periodo.Id, periodo.Anio, periodo.Nombre, periodo.FechaInicio, periodo.FechaFin,
            periodo.PensionMensual, periodo.Activo);
    }

    public async Task<IReadOnlyList<GradoDto>> ListarGradosAsync(CancellationToken ct) =>
        await db.Grados.OrderBy(g => g.Orden)
            .Select(g => new GradoDto(g.Id, g.Nombre, g.Nivel.ToString(), g.Orden))
            .ToListAsync(ct);

    public async Task<GradoDto> CrearGradoAsync(CrearGradoRequest r, CancellationToken ct)
    {
        var grado = new Grado { Nombre = r.Nombre.Trim(), Nivel = r.Nivel, Orden = r.Orden };
        db.Grados.Add(grado);
        await db.SaveChangesAsync(ct);
        return new GradoDto(grado.Id, grado.Nombre, grado.Nivel.ToString(), grado.Orden);
    }

    public async Task<IReadOnlyList<SeccionDto>> ListarSeccionesAsync(int? periodoId, int? gradoId, CancellationToken ct)
    {
        var query = db.Secciones.AsQueryable();
        if (periodoId is not null) query = query.Where(s => s.PeriodoId == periodoId);
        if (gradoId is not null) query = query.Where(s => s.GradoId == gradoId);
        // El docente solo ve las secciones donde dicta (para asistencia y notas).
        if (user.Rol == Rol.Docente)
            query = query.Where(s => db.Asignaciones.Any(a => a.SeccionId == s.Id && a.DocenteId == user.DocenteId));
        else if (user.Rol == Rol.Apoderado)
            query = query.Where(s => db.Matriculas.Any(m => m.SeccionId == s.Id && m.Alumno.ApoderadoId == user.ApoderadoId));

        var datos = await query
            .OrderBy(s => s.Grado.Orden).ThenBy(s => s.Letra)
            .Select(s => new { s.Id, s.GradoId, Grado = s.Grado.Nombre, s.Letra, s.PeriodoId, s.Periodo.Anio, s.Capacidad, s.Matriculados })
            .ToListAsync(ct);

        return datos.Select(s => new SeccionDto(s.Id, s.GradoId, s.Grado, s.Letra, s.PeriodoId, s.Anio, s.Capacidad,
            s.Matriculados, ReglasMatricula.VacantesDisponibles(s.Capacidad, s.Matriculados),
            ReglasMatricula.PorcentajeOcupacion(s.Capacidad, s.Matriculados))).ToList();
    }

    public async Task<SeccionDto> CrearSeccionAsync(CrearSeccionRequest r, CancellationToken ct)
    {
        var grado = await db.Grados.FindAsync([r.GradoId], ct) ?? throw new NotFoundException("Grado", r.GradoId);
        var periodo = await db.Periodos.FindAsync([r.PeriodoId], ct) ?? throw new NotFoundException("Periodo", r.PeriodoId);
        if (await db.Secciones.AnyAsync(s => s.GradoId == r.GradoId && s.PeriodoId == r.PeriodoId && s.Letra == r.Letra, ct))
            throw new ConflictException($"La sección {grado.Nombre} {r.Letra} ya existe en {periodo.Anio}.");

        var seccion = new Seccion { GradoId = r.GradoId, PeriodoId = r.PeriodoId, Letra = r.Letra, Capacidad = r.Capacidad };
        db.Secciones.Add(seccion);
        await db.SaveChangesAsync(ct);
        return new SeccionDto(seccion.Id, grado.Id, grado.Nombre, seccion.Letra, periodo.Id, periodo.Anio,
            seccion.Capacidad, 0, seccion.Capacidad, 0);
    }

    public async Task<IReadOnlyList<CursoDto>> ListarCursosAsync(int? gradoId, CancellationToken ct)
    {
        var query = db.Cursos.Include(c => c.Grado).Include(c => c.Competencias).AsQueryable();
        if (gradoId is not null) query = query.Where(c => c.GradoId == gradoId);
        if (user.Rol == Rol.Docente)
            query = query.Where(c => db.Asignaciones.Any(a => a.CursoId == c.Id && a.DocenteId == user.DocenteId));

        var cursos = await query.OrderBy(c => c.Grado.Orden).ThenBy(c => c.Nombre).ToListAsync(ct);
        return cursos.Select(ToDto).ToList();
    }

    public async Task<CursoDto> CrearCursoAsync(CrearCursoRequest r, CancellationToken ct)
    {
        var grado = await db.Grados.FindAsync([r.GradoId], ct) ?? throw new NotFoundException("Grado", r.GradoId);
        if (await db.Cursos.AnyAsync(c => c.Codigo == r.Codigo, ct))
            throw new ConflictException($"Ya existe un curso con código {r.Codigo}.");
        CalculadoraPromedio.ValidarPesos(r.Competencias.Select(c => c.Peso));

        var curso = new Curso
        {
            Codigo = r.Codigo.Trim().ToUpperInvariant(), Nombre = r.Nombre.Trim(), GradoId = grado.Id, Grado = grado,
            Competencias = r.Competencias.Select((c, i) => new Competencia { Nombre = c.Nombre.Trim(), Peso = c.Peso, Orden = i + 1 }).ToList()
        };
        db.Cursos.Add(curso);
        await db.SaveChangesAsync(ct);
        return ToDto(curso);
    }

    /// <summary>
    /// Reconfigura las competencias y sus pesos. Las competencias existentes se actualizan;
    /// no se eliminan si ya tienen notas, porque eso borraría historial académico.
    /// </summary>
    public async Task<CursoDto> ActualizarCompetenciasAsync(int cursoId, ActualizarCompetenciasRequest r, CancellationToken ct)
    {
        var curso = await db.Cursos.Include(c => c.Grado).Include(c => c.Competencias)
                        .FirstOrDefaultAsync(c => c.Id == cursoId, ct)
                    ?? throw new NotFoundException("Curso", cursoId);
        CalculadoraPromedio.ValidarPesos(r.Competencias.Select(c => c.Peso));

        var idsEnviados = r.Competencias.Where(c => c.Id is not null).Select(c => c.Id!.Value).ToHashSet();
        foreach (var eliminar in curso.Competencias.Where(c => !idsEnviados.Contains(c.Id)).ToList())
        {
            if (await db.Notas.AnyAsync(n => n.CompetenciaId == eliminar.Id, ct))
                throw new ConflictException($"La competencia '{eliminar.Nombre}' tiene notas registradas y no puede eliminarse.");
            curso.Competencias.Remove(eliminar);
            db.Competencias.Remove(eliminar);
        }

        var orden = 1;
        foreach (var input in r.Competencias)
        {
            var existente = input.Id is null ? null : curso.Competencias.FirstOrDefault(c => c.Id == input.Id);
            if (input.Id is not null && existente is null)
                throw new NotFoundException("Competencia", input.Id);
            if (existente is null)
            {
                existente = new Competencia();
                curso.Competencias.Add(existente);
            }
            existente.Nombre = input.Nombre.Trim();
            existente.Peso = input.Peso;
            existente.Orden = orden++;
        }
        await db.SaveChangesAsync(ct);
        return ToDto(curso);
    }

    public async Task<IReadOnlyList<AsignacionDto>> ListarAsignacionesAsync(int? periodoId, CancellationToken ct)
    {
        var query = db.Asignaciones.AsQueryable();
        if (periodoId is not null) query = query.Where(a => a.Seccion.PeriodoId == periodoId);
        if (user.Rol == Rol.Docente) query = query.Where(a => a.DocenteId == user.DocenteId);
        else if (user.Rol != Rol.Admin) query = query.Where(_ => false);

        return await query
            .OrderBy(a => a.Seccion.Grado.Orden).ThenBy(a => a.Seccion.Letra).ThenBy(a => a.Curso.Nombre)
            .Select(a => new AsignacionDto(a.Id, a.DocenteId, a.Docente.Apellidos + ", " + a.Docente.Nombres,
                a.CursoId, a.Curso.Nombre, a.SeccionId, a.Seccion.Grado.Nombre + " " + a.Seccion.Letra, a.Seccion.PeriodoId))
            .ToListAsync(ct);
    }

    public async Task<AsignacionDto> CrearAsignacionAsync(CrearAsignacionRequest r, CancellationToken ct)
    {
        var docente = await db.Docentes.FindAsync([r.DocenteId], ct) ?? throw new NotFoundException("Docente", r.DocenteId);
        var curso = await db.Cursos.FindAsync([r.CursoId], ct) ?? throw new NotFoundException("Curso", r.CursoId);
        var seccion = await db.Secciones.Include(s => s.Grado).FirstOrDefaultAsync(s => s.Id == r.SeccionId, ct)
                      ?? throw new NotFoundException("Sección", r.SeccionId);

        // Un curso de 3° no puede dictarse en una sección de 5°.
        if (curso.GradoId != seccion.GradoId)
            throw new DomainException("El curso no pertenece al grado de la sección.");
        // Un curso tiene un único docente titular por sección: evita notas contradictorias.
        if (await db.Asignaciones.AnyAsync(a => a.CursoId == r.CursoId && a.SeccionId == r.SeccionId, ct))
            throw new ConflictException("El curso ya tiene un docente asignado en esta sección.");

        var asignacion = new AsignacionDocente { DocenteId = docente.Id, CursoId = curso.Id, SeccionId = seccion.Id };
        db.Asignaciones.Add(asignacion);
        await db.SaveChangesAsync(ct);
        return new AsignacionDto(asignacion.Id, docente.Id, $"{docente.Apellidos}, {docente.Nombres}", curso.Id, curso.Nombre,
            seccion.Id, $"{seccion.Grado.Nombre} {seccion.Letra}", seccion.PeriodoId);
    }

    private static CursoDto ToDto(Curso c) => new(c.Id, c.Codigo, c.Nombre, c.GradoId, c.Grado.Nombre,
        c.Competencias.OrderBy(x => x.Orden).Select(x => new CompetenciaDto(x.Id, x.Nombre, x.Peso, x.Orden)).ToList());
}
