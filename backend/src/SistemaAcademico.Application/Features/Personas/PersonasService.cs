using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SistemaAcademico.Application.Abstractions;
using SistemaAcademico.Application.Common;
using SistemaAcademico.Domain;
using SistemaAcademico.Domain.Common;
using SistemaAcademico.Domain.Entities;

namespace SistemaAcademico.Application.Features.Personas;

// ---------- DTOs ----------
public sealed record AlumnoDto(int Id, string Codigo, string Dni, string Nombres, string Apellidos,
    DateOnly FechaNacimiento, int ApoderadoId, string Apoderado, bool Activo, string? SeccionActual);

public sealed record GuardarAlumnoRequest(string Dni, string Nombres, string Apellidos, DateOnly FechaNacimiento, int ApoderadoId);

public sealed record AlumnoQuery : PageQuery
{
    public bool? Activo { get; init; }
    public int? SeccionId { get; init; }
}

public sealed record ApoderadoDto(int Id, string Dni, string Nombres, string Apellidos, string? Telefono, string? Email, int Hijos);

public sealed record GuardarApoderadoRequest(string Dni, string Nombres, string Apellidos, string? Telefono, string? Email);

public sealed record DocenteDto(int Id, string Dni, string Nombres, string Apellidos, string Especialidad, string? Email, int Cursos);

public sealed record GuardarDocenteRequest(string Dni, string Nombres, string Apellidos, string Especialidad, string? Email);

// ---------- Validadores ----------
internal static class ReglasPersona
{
    // El DNI peruano tiene exactamente 8 dígitos.
    public const string PatronDni = "^[0-9]{8}$";
    public const string PatronTelefono = "^9[0-9]{8}$";
}

public sealed class GuardarAlumnoValidator : AbstractValidator<GuardarAlumnoRequest>
{
    public GuardarAlumnoValidator(IClock clock)
    {
        RuleFor(x => x.Dni).NotEmpty().Matches(ReglasPersona.PatronDni).WithMessage("El DNI debe tener 8 dígitos.");
        RuleFor(x => x.Nombres).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Apellidos).NotEmpty().MaximumLength(100);
        // Educación básica regular: un alumno tiene entre 3 y 20 años.
        RuleFor(x => x.FechaNacimiento)
            .Must(f => f <= clock.Hoy.AddYears(-3) && f >= clock.Hoy.AddYears(-20))
            .WithMessage("La fecha de nacimiento no corresponde a un alumno de educación básica (3 a 20 años).");
        RuleFor(x => x.ApoderadoId).GreaterThan(0);
    }
}

public sealed class GuardarApoderadoValidator : AbstractValidator<GuardarApoderadoRequest>
{
    public GuardarApoderadoValidator()
    {
        RuleFor(x => x.Dni).NotEmpty().Matches(ReglasPersona.PatronDni).WithMessage("El DNI debe tener 8 dígitos.");
        RuleFor(x => x.Nombres).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Apellidos).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Telefono).Matches(ReglasPersona.PatronTelefono).When(x => !string.IsNullOrEmpty(x.Telefono))
            .WithMessage("El celular debe tener 9 dígitos y empezar con 9.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
    }
}

public sealed class GuardarDocenteValidator : AbstractValidator<GuardarDocenteRequest>
{
    public GuardarDocenteValidator()
    {
        RuleFor(x => x.Dni).NotEmpty().Matches(ReglasPersona.PatronDni).WithMessage("El DNI debe tener 8 dígitos.");
        RuleFor(x => x.Nombres).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Apellidos).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Especialidad).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
    }
}

// ---------- Servicio ----------
public sealed class PersonasService(IAppDbContext db, ControlAcceso acceso)
{
    private static readonly SortMap<Alumno> OrdenAlumnos = new SortMap<Alumno>()
        .Add("apellidos", a => a.Apellidos, isDefault: true)
        .Add("nombres", a => a.Nombres)
        .Add("codigo", a => a.Codigo)
        .Add("dni", a => a.Dni)
        .Add("fechaNacimiento", a => a.FechaNacimiento);

    public async Task<PagedResult<AlumnoDto>> ListarAlumnosAsync(AlumnoQuery q, CancellationToken ct)
    {
        var query = acceso.FiltrarAlumnos(db.Alumnos.AsNoTracking());
        if (q.Term is { } t)
            query = query.Where(a => a.Nombres.ToLower().Contains(t) || a.Apellidos.ToLower().Contains(t) ||
                                     a.Dni.Contains(t) || a.Codigo.ToLower().Contains(t));
        if (q.Activo is not null) query = query.Where(a => a.Activo == q.Activo);
        if (q.SeccionId is not null)
            query = query.Where(a => a.Matriculas.Any(m => m.SeccionId == q.SeccionId && m.Estado == EstadoMatricula.Activa));

        return await OrdenAlumnos.Apply(query, q.SortBy, q.Descending)
            .Select(ProyeccionAlumno)
            .ToPagedAsync(q, ct);
    }

    public async Task<AlumnoDto> ObtenerAlumnoAsync(int id, CancellationToken ct)
    {
        await acceso.AsegurarAccesoAlumnoAsync(id, ct);
        return await db.Alumnos.Where(a => a.Id == id).Select(ProyeccionAlumno).FirstAsync(ct);
    }

    public async Task<AlumnoDto> CrearAlumnoAsync(GuardarAlumnoRequest r, CancellationToken ct)
    {
        if (await db.Alumnos.AnyAsync(a => a.Dni == r.Dni, ct))
            throw new ConflictException($"Ya existe un alumno con DNI {r.Dni}.");
        if (!await db.Apoderados.AnyAsync(a => a.Id == r.ApoderadoId, ct))
            throw new NotFoundException("Apoderado", r.ApoderadoId);

        var alumno = new Alumno
        {
            Dni = r.Dni, Nombres = r.Nombres.Trim(), Apellidos = r.Apellidos.Trim(),
            FechaNacimiento = r.FechaNacimiento, ApoderadoId = r.ApoderadoId,
            // Código temporal único; se reemplaza por uno legible tras obtener el Id.
            Codigo = $"TMP-{Guid.NewGuid():N}"
        };
        db.Alumnos.Add(alumno);
        await db.SaveChangesAsync(ct);
        // Código institucional legible y estable (ALU-000123), independiente del DNI.
        alumno.Codigo = $"ALU-{alumno.Id:D6}";
        await db.SaveChangesAsync(ct);
        return await ObtenerAlumnoAsync(alumno.Id, ct);
    }

    public async Task<AlumnoDto> ActualizarAlumnoAsync(int id, GuardarAlumnoRequest r, CancellationToken ct)
    {
        var alumno = await db.Alumnos.FindAsync([id], ct) ?? throw new NotFoundException("Alumno", id);
        if (await db.Alumnos.AnyAsync(a => a.Dni == r.Dni && a.Id != id, ct))
            throw new ConflictException($"Ya existe otro alumno con DNI {r.Dni}.");
        if (!await db.Apoderados.AnyAsync(a => a.Id == r.ApoderadoId, ct))
            throw new NotFoundException("Apoderado", r.ApoderadoId);

        alumno.Dni = r.Dni;
        alumno.Nombres = r.Nombres.Trim();
        alumno.Apellidos = r.Apellidos.Trim();
        alumno.FechaNacimiento = r.FechaNacimiento;
        alumno.ApoderadoId = r.ApoderadoId;
        await db.SaveChangesAsync(ct);
        return await ObtenerAlumnoAsync(id, ct);
    }

    /// <summary>Baja lógica (ver comentario en <see cref="Alumno.Activo"/>).</summary>
    public async Task DesactivarAlumnoAsync(int id, CancellationToken ct)
    {
        var alumno = await db.Alumnos.FindAsync([id], ct) ?? throw new NotFoundException("Alumno", id);
        if (await db.Matriculas.AnyAsync(m => m.AlumnoId == id && m.Estado == EstadoMatricula.Activa, ct))
            throw new DomainException("Primero retire la matrícula activa del alumno.");
        alumno.Activo = false;
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<ApoderadoDto>> ListarApoderadosAsync(PageQuery q, CancellationToken ct)
    {
        var query = db.Apoderados.AsNoTracking();
        if (q.Term is { } t)
            query = query.Where(a => a.Nombres.ToLower().Contains(t) || a.Apellidos.ToLower().Contains(t) || a.Dni.Contains(t));
        return await new SortMap<Apoderado>().Add("apellidos", a => a.Apellidos, true).Add("dni", a => a.Dni)
            .Apply(query, q.SortBy, q.Descending)
            .Select(a => new ApoderadoDto(a.Id, a.Dni, a.Nombres, a.Apellidos, a.Telefono, a.Email, a.Alumnos.Count))
            .ToPagedAsync(q, ct);
    }

    public async Task<ApoderadoDto> CrearApoderadoAsync(GuardarApoderadoRequest r, CancellationToken ct)
    {
        if (await db.Apoderados.AnyAsync(a => a.Dni == r.Dni, ct))
            throw new ConflictException($"Ya existe un apoderado con DNI {r.Dni}.");
        var a = new Apoderado { Dni = r.Dni, Nombres = r.Nombres.Trim(), Apellidos = r.Apellidos.Trim(), Telefono = r.Telefono, Email = r.Email };
        db.Apoderados.Add(a);
        await db.SaveChangesAsync(ct);
        return new ApoderadoDto(a.Id, a.Dni, a.Nombres, a.Apellidos, a.Telefono, a.Email, 0);
    }

    public async Task<PagedResult<DocenteDto>> ListarDocentesAsync(PageQuery q, CancellationToken ct)
    {
        var query = db.Docentes.AsNoTracking();
        if (q.Term is { } t)
            query = query.Where(d => d.Nombres.ToLower().Contains(t) || d.Apellidos.ToLower().Contains(t) ||
                                     d.Especialidad.ToLower().Contains(t));
        return await new SortMap<Docente>().Add("apellidos", d => d.Apellidos, true).Add("especialidad", d => d.Especialidad)
            .Apply(query, q.SortBy, q.Descending)
            .Select(d => new DocenteDto(d.Id, d.Dni, d.Nombres, d.Apellidos, d.Especialidad, d.Email, d.Asignaciones.Count))
            .ToPagedAsync(q, ct);
    }

    public async Task<DocenteDto> CrearDocenteAsync(GuardarDocenteRequest r, CancellationToken ct)
    {
        if (await db.Docentes.AnyAsync(d => d.Dni == r.Dni, ct))
            throw new ConflictException($"Ya existe un docente con DNI {r.Dni}.");
        var d = new Docente { Dni = r.Dni, Nombres = r.Nombres.Trim(), Apellidos = r.Apellidos.Trim(), Especialidad = r.Especialidad.Trim(), Email = r.Email };
        db.Docentes.Add(d);
        await db.SaveChangesAsync(ct);
        return new DocenteDto(d.Id, d.Dni, d.Nombres, d.Apellidos, d.Especialidad, d.Email, 0);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Alumno, AlumnoDto>> ProyeccionAlumno = a =>
        new AlumnoDto(a.Id, a.Codigo, a.Dni, a.Nombres, a.Apellidos, a.FechaNacimiento, a.ApoderadoId,
            a.Apoderado.Apellidos + ", " + a.Apoderado.Nombres, a.Activo,
            a.Matriculas.Where(m => m.Estado == EstadoMatricula.Activa)
                .OrderByDescending(m => m.Periodo.Anio)
                .Select(m => m.Seccion.Grado.Nombre + " " + m.Seccion.Letra)
                .FirstOrDefault());
}
