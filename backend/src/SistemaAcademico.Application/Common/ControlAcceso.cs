using Microsoft.EntityFrameworkCore;
using SistemaAcademico.Application.Abstractions;
using SistemaAcademico.Domain;
using SistemaAcademico.Domain.Entities;

namespace SistemaAcademico.Application.Common;

/// <summary>
/// Autorización por recurso (no solo por rol). Los endpoints ya filtran por rol con
/// políticas; aquí se responde la pregunta más fina: "¿este recurso concreto es tuyo?".
/// Centralizarla evita que cada servicio reimplemente (y olvide) las reglas:
/// <list type="bullet">
/// <item>Admin: acceso total.</item>
/// <item>Docente: solo secciones/cursos donde tiene una <see cref="AsignacionDocente"/>.</item>
/// <item>Apoderado: solo matrículas de sus hijos.</item>
/// </list>
/// </summary>
public sealed class ControlAcceso(IAppDbContext db, ICurrentUser user)
{
    public bool EsAdmin => user.Rol == Rol.Admin;

    /// <summary>Filtra matrículas visibles para el usuario actual.</summary>
    public IQueryable<Matricula> FiltrarMatriculas(IQueryable<Matricula> query) => user.Rol switch
    {
        Rol.Admin => query,
        Rol.Docente => query.Where(m => db.Asignaciones.Any(a => a.SeccionId == m.SeccionId && a.DocenteId == user.DocenteId)),
        Rol.Apoderado => query.Where(m => m.Alumno.ApoderadoId == user.ApoderadoId),
        _ => query.Where(_ => false)
    };

    /// <summary>Filtra alumnos visibles: el docente ve a los alumnos matriculados en sus secciones.</summary>
    public IQueryable<Alumno> FiltrarAlumnos(IQueryable<Alumno> query) => user.Rol switch
    {
        Rol.Admin => query,
        Rol.Docente => query.Where(al => al.Matriculas.Any(m => m.Estado == EstadoMatricula.Activa &&
            db.Asignaciones.Any(a => a.SeccionId == m.SeccionId && a.DocenteId == user.DocenteId))),
        Rol.Apoderado => query.Where(al => al.ApoderadoId == user.ApoderadoId),
        _ => query.Where(_ => false)
    };

    public async Task AsegurarAccesoMatriculaAsync(int matriculaId, CancellationToken ct)
    {
        var existe = await db.Matriculas.AnyAsync(m => m.Id == matriculaId, ct);
        if (!existe) throw new NotFoundException("Matrícula", matriculaId);

        var permitido = await FiltrarMatriculas(db.Matriculas).AnyAsync(m => m.Id == matriculaId, ct);
        if (!permitido) throw new ForbiddenException("La matrícula no pertenece a sus alumnos o cursos.");
    }

    public async Task AsegurarAccesoAlumnoAsync(int alumnoId, CancellationToken ct)
    {
        var existe = await db.Alumnos.AnyAsync(a => a.Id == alumnoId, ct);
        if (!existe) throw new NotFoundException("Alumno", alumnoId);

        var permitido = await FiltrarAlumnos(db.Alumnos).AnyAsync(a => a.Id == alumnoId, ct);
        if (!permitido) throw new ForbiddenException("El alumno no pertenece a sus hijos o secciones.");
    }

    /// <summary>Solo el docente titular de la asignación (o un admin) puede registrar sus notas.</summary>
    public async Task<AsignacionDocente> AsegurarAsignacionPropiaAsync(int asignacionId, CancellationToken ct)
    {
        var asignacion = await db.Asignaciones
            .Include(a => a.Curso).ThenInclude(c => c.Competencias)
            .Include(a => a.Seccion).ThenInclude(s => s.Grado)
            .Include(a => a.Docente)
            .FirstOrDefaultAsync(a => a.Id == asignacionId, ct)
            ?? throw new NotFoundException("Asignación", asignacionId);

        if (!EsAdmin && (user.Rol != Rol.Docente || asignacion.DocenteId != user.DocenteId))
            throw new ForbiddenException("Solo el docente asignado puede gestionar este curso.");
        return asignacion;
    }

    /// <summary>Un docente puede tomar asistencia en cualquier sección donde dicta al menos un curso.</summary>
    public async Task AsegurarAccesoSeccionAsync(int seccionId, CancellationToken ct)
    {
        var existe = await db.Secciones.AnyAsync(s => s.Id == seccionId, ct);
        if (!existe) throw new NotFoundException("Sección", seccionId);
        if (EsAdmin) return;

        var dicta = user.Rol == Rol.Docente &&
                    await db.Asignaciones.AnyAsync(a => a.SeccionId == seccionId && a.DocenteId == user.DocenteId, ct);
        if (!dicta) throw new ForbiddenException("No tiene cursos asignados en esta sección.");
    }
}
