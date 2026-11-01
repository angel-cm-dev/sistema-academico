using Microsoft.EntityFrameworkCore;
using SistemaAcademico.Application.Abstractions;
using SistemaAcademico.Domain.Entities;

namespace SistemaAcademico.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Apoderado> Apoderados => Set<Apoderado>();
    public DbSet<Alumno> Alumnos => Set<Alumno>();
    public DbSet<Docente> Docentes => Set<Docente>();
    public DbSet<PeriodoAcademico> Periodos => Set<PeriodoAcademico>();
    public DbSet<Grado> Grados => Set<Grado>();
    public DbSet<Seccion> Secciones => Set<Seccion>();
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Competencia> Competencias => Set<Competencia>();
    public DbSet<AsignacionDocente> Asignaciones => Set<AsignacionDocente>();
    public DbSet<Matricula> Matriculas => Set<Matricula>();
    public DbSet<Nota> Notas => Set<Nota>();
    public DbSet<Asistencia> Asistencias => Set<Asistencia>();
    public DbSet<Cuota> Cuotas => Set<Cuota>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // SQLite (usado en pruebas) no soporta agregados ni ORDER BY sobre decimal.
        // Solo para ese proveedor guardamos los decimales como REAL; en PostgreSQL
        // se mantienen como numeric(p,s), que es lo correcto para dinero y notas.
        if (Database.IsSqlite())
        {
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                         .SelectMany(t => t.GetProperties())
                         .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetValueConverter(typeof(decimal?) == property.ClrType
                    ? new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<decimal?, double?>(
                        v => (double?)v, v => (decimal?)v)
                    : new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<decimal, double>(
                        v => (double)v, v => (decimal)v));
            }
        }
    }
}
