using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SistemaAcademico.Application.Abstractions;
using SistemaAcademico.Application.Common;
using SistemaAcademico.Domain;
using SistemaAcademico.Domain.Common;
using SistemaAcademico.Domain.Entities;
using SistemaAcademico.Domain.Reglas;

namespace SistemaAcademico.Application.Features.Matriculas;

public sealed record MatriculaDto(int Id, int AlumnoId, string Alumno, string CodigoAlumno, int SeccionId, string Seccion,
    int PeriodoId, int Anio, DateOnly FechaMatricula, string Estado, DateOnly? FechaRetiro);

public sealed record CrearMatriculaRequest(int AlumnoId, int SeccionId, DateOnly? FechaMatricula);

public sealed record MatriculaCreadaDto(MatriculaDto Matricula, int CuotasGeneradas, decimal TotalCronograma);

public sealed record MatriculaQuery : PageQuery
{
    public int? PeriodoId { get; init; }
    public int? SeccionId { get; init; }
    public EstadoMatricula? Estado { get; init; }
}

public sealed class CrearMatriculaValidator : AbstractValidator<CrearMatriculaRequest>
{
    public CrearMatriculaValidator()
    {
        RuleFor(x => x.AlumnoId).GreaterThan(0);
        RuleFor(x => x.SeccionId).GreaterThan(0);
    }
}

public sealed class MatriculaService(IAppDbContext db, ControlAcceso acceso, IClock clock)
{
    private static readonly SortMap<Matricula> Orden = new SortMap<Matricula>()
        .Add("alumno", m => m.Alumno.Apellidos, isDefault: true)
        .Add("fecha", m => m.FechaMatricula)
        .Add("seccion", m => m.Seccion.Grado.Orden)
        .Add("estado", m => m.Estado);

    public async Task<PagedResult<MatriculaDto>> ListarAsync(MatriculaQuery q, CancellationToken ct)
    {
        var query = acceso.FiltrarMatriculas(db.Matriculas.AsNoTracking());
        if (q.PeriodoId is not null) query = query.Where(m => m.PeriodoId == q.PeriodoId);
        if (q.SeccionId is not null) query = query.Where(m => m.SeccionId == q.SeccionId);
        if (q.Estado is not null) query = query.Where(m => m.Estado == q.Estado);
        if (q.Term is { } t)
            query = query.Where(m => m.Alumno.Nombres.ToLower().Contains(t) || m.Alumno.Apellidos.ToLower().Contains(t) ||
                                     m.Alumno.Codigo.ToLower().Contains(t) || m.Alumno.Dni.Contains(t));

        return await Orden.Apply(query, q.SortBy, q.Descending).Select(Proyeccion).ToPagedAsync(q, ct);
    }

    public async Task<MatriculaDto> ObtenerAsync(int id, CancellationToken ct)
    {
        await acceso.AsegurarAccesoMatriculaAsync(id, ct);
        return await db.Matriculas.Where(m => m.Id == id).Select(Proyeccion).FirstAsync(ct);
    }

    /// <summary>
    /// Matricula a un alumno validando, en este orden:
    /// alumno activo → sin otra matrícula en el periodo → vacante disponible.
    /// En la misma transacción genera el cronograma de pensiones, de modo que nunca
    /// exista una matrícula sin cuotas ni cuotas sin matrícula.
    /// </summary>
    public async Task<MatriculaCreadaDto> CrearAsync(CrearMatriculaRequest r, CancellationToken ct)
    {
        var alumno = await db.Alumnos.FindAsync([r.AlumnoId], ct) ?? throw new NotFoundException("Alumno", r.AlumnoId);
        if (!alumno.Activo) throw new DomainException("No se puede matricular a un alumno dado de baja.");

        var seccion = await db.Secciones.Include(s => s.Periodo).Include(s => s.Grado)
                          .FirstOrDefaultAsync(s => s.Id == r.SeccionId, ct)
                      ?? throw new NotFoundException("Sección", r.SeccionId);

        if (await db.Matriculas.AnyAsync(m => m.AlumnoId == alumno.Id && m.PeriodoId == seccion.PeriodoId, ct))
            throw new ConflictException($"El alumno ya tiene una matrícula en el periodo {seccion.Periodo.Anio}.");

        var fecha = r.FechaMatricula ?? clock.Hoy;
        if (fecha > seccion.Periodo.FechaFin)
            throw new DomainException("La fecha de matrícula es posterior al fin del periodo.");

        // Regla de vacantes (lanza DomainException → 422 si la sección está llena).
        seccion.OcuparVacante();

        var matricula = new Matricula
        {
            Alumno = alumno, Seccion = seccion, PeriodoId = seccion.PeriodoId, FechaMatricula = fecha,
            Estado = EstadoMatricula.Activa
        };
        var cronograma = CronogramaPensiones.Generar(seccion.Periodo.Anio, seccion.Periodo.PensionMensual, fecha);
        matricula.Cuotas.AddRange(cronograma.Select(c => new Cuota
        {
            Numero = c.Numero, Concepto = c.Concepto, Monto = c.Monto, FechaVencimiento = c.FechaVencimiento
        }));
        db.Matriculas.Add(matricula);

        // SaveChanges es atómico: la matrícula, el cupo y las cuotas se guardan juntos.
        // Si otra petición tomó la última vacante, el token de concurrencia de la sección
        // provoca DbUpdateConcurrencyException, que la API traduce a 409.
        await db.SaveChangesAsync(ct);

        var dto = await db.Matriculas.Where(m => m.Id == matricula.Id).Select(Proyeccion).FirstAsync(ct);
        return new MatriculaCreadaDto(dto, cronograma.Count, cronograma.Sum(c => c.Monto));
    }

    public async Task<MatriculaDto> RetirarAsync(int id, CancellationToken ct)
    {
        var matricula = await db.Matriculas.Include(m => m.Seccion).Include(m => m.Cuotas)
                            .FirstOrDefaultAsync(m => m.Id == id, ct)
                        ?? throw new NotFoundException("Matrícula", id);
        matricula.Retirar(clock.Hoy);
        await db.SaveChangesAsync(ct);
        return await db.Matriculas.Where(m => m.Id == id).Select(Proyeccion).FirstAsync(ct);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Matricula, MatriculaDto>> Proyeccion = m =>
        new MatriculaDto(m.Id, m.AlumnoId, m.Alumno.Apellidos + ", " + m.Alumno.Nombres, m.Alumno.Codigo, m.SeccionId,
            m.Seccion.Grado.Nombre + " " + m.Seccion.Letra, m.PeriodoId, m.Periodo.Anio, m.FechaMatricula,
            m.Estado.ToString(), m.FechaRetiro);
}
