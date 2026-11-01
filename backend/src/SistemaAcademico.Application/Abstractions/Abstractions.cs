using Microsoft.EntityFrameworkCore;
using SistemaAcademico.Domain;
using SistemaAcademico.Domain.Entities;

namespace SistemaAcademico.Application.Abstractions;

/// <summary>
/// Puerto de persistencia. Application conoce DbSet/IQueryable (abstracciones de EF Core)
/// pero no el proveedor: PostgreSQL en producción y SQLite en pruebas son intercambiables.
/// Ver docs/decisiones/0002-dbcontext-como-puerto.md.
/// </summary>
public interface IAppDbContext
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Apoderado> Apoderados { get; }
    DbSet<Alumno> Alumnos { get; }
    DbSet<Docente> Docentes { get; }
    DbSet<PeriodoAcademico> Periodos { get; }
    DbSet<Grado> Grados { get; }
    DbSet<Seccion> Secciones { get; }
    DbSet<Curso> Cursos { get; }
    DbSet<Competencia> Competencias { get; }
    DbSet<AsignacionDocente> Asignaciones { get; }
    DbSet<Matricula> Matriculas { get; }
    DbSet<Nota> Notas { get; }
    DbSet<Asistencia> Asistencias { get; }
    DbSet<Cuota> Cuotas { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Usuario autenticado de la petición actual (se obtiene de los claims del JWT).</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    int UserId { get; }
    Rol Rol { get; }
    int? DocenteId { get; }
    int? ApoderadoId { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
}

public sealed record TokenGenerado(string Token, DateTime ExpiraEn);

public interface ITokenService
{
    TokenGenerado Generar(Usuario usuario);
}

/// <summary>
/// Reloj inyectable: las reglas de mora y asistencia dependen de "hoy",
/// y en pruebas necesitamos fijar la fecha para obtener resultados deterministas.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }

    /// <summary>Fecha local de Lima (UTC-5, Perú no usa horario de verano).</summary>
    DateOnly Hoy { get; }
}
