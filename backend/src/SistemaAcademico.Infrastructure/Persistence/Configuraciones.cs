using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAcademico.Domain.Entities;

namespace SistemaAcademico.Infrastructure.Persistence;

// Mapeo Fluent API: el dominio queda libre de atributos de persistencia.
// Las restricciones únicas e índices replican en la BD las reglas de negocio
// críticas, como última línea de defensa ante condiciones de carrera.

internal sealed class UsuarioConfig : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuarios");
        b.Property(x => x.Email).HasMaxLength(150).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        b.Property(x => x.NombreCompleto).HasMaxLength(150).IsRequired();
        b.Property(x => x.Rol).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Docente).WithMany().HasForeignKey(x => x.DocenteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Apoderado).WithMany().HasForeignKey(x => x.ApoderadoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ApoderadoConfig : IEntityTypeConfiguration<Apoderado>
{
    public void Configure(EntityTypeBuilder<Apoderado> b)
    {
        b.ToTable("apoderados");
        b.Property(x => x.Dni).HasMaxLength(8).IsFixedLength().IsRequired();
        b.HasIndex(x => x.Dni).IsUnique();
        b.Property(x => x.Nombres).HasMaxLength(100).IsRequired();
        b.Property(x => x.Apellidos).HasMaxLength(100).IsRequired();
        b.Property(x => x.Telefono).HasMaxLength(9);
        b.Property(x => x.Email).HasMaxLength(150);
        b.Ignore(x => x.NombreCompleto);
    }
}

internal sealed class AlumnoConfig : IEntityTypeConfiguration<Alumno>
{
    public void Configure(EntityTypeBuilder<Alumno> b)
    {
        b.ToTable("alumnos");
        b.Property(x => x.Codigo).HasMaxLength(40).IsRequired();
        b.HasIndex(x => x.Codigo).IsUnique();
        b.Property(x => x.Dni).HasMaxLength(8).IsFixedLength().IsRequired();
        b.HasIndex(x => x.Dni).IsUnique();
        b.Property(x => x.Nombres).HasMaxLength(100).IsRequired();
        b.Property(x => x.Apellidos).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Apellidos);
        b.HasOne(x => x.Apoderado).WithMany(x => x.Alumnos).HasForeignKey(x => x.ApoderadoId).OnDelete(DeleteBehavior.Restrict);
        b.Ignore(x => x.NombreCompleto);
    }
}

internal sealed class DocenteConfig : IEntityTypeConfiguration<Docente>
{
    public void Configure(EntityTypeBuilder<Docente> b)
    {
        b.ToTable("docentes");
        b.Property(x => x.Dni).HasMaxLength(8).IsFixedLength().IsRequired();
        b.HasIndex(x => x.Dni).IsUnique();
        b.Property(x => x.Nombres).HasMaxLength(100).IsRequired();
        b.Property(x => x.Apellidos).HasMaxLength(100).IsRequired();
        b.Property(x => x.Especialidad).HasMaxLength(100).IsRequired();
        b.Property(x => x.Email).HasMaxLength(150);
        b.Ignore(x => x.NombreCompleto);
    }
}

internal sealed class PeriodoConfig : IEntityTypeConfiguration<PeriodoAcademico>
{
    public void Configure(EntityTypeBuilder<PeriodoAcademico> b)
    {
        b.ToTable("periodos");
        b.HasIndex(x => x.Anio).IsUnique();
        b.Property(x => x.Nombre).HasMaxLength(60).IsRequired();
        b.Property(x => x.PensionMensual).HasPrecision(10, 2);
    }
}

internal sealed class GradoConfig : IEntityTypeConfiguration<Grado>
{
    public void Configure(EntityTypeBuilder<Grado> b)
    {
        b.ToTable("grados");
        b.Property(x => x.Nombre).HasMaxLength(60).IsRequired();
        b.Property(x => x.Nivel).HasConversion<string>().HasMaxLength(20);
    }
}

internal sealed class SeccionConfig : IEntityTypeConfiguration<Seccion>
{
    public void Configure(EntityTypeBuilder<Seccion> b)
    {
        b.ToTable("secciones", t => t.HasCheckConstraint("ck_secciones_cupo", "\"Matriculados\" <= \"Capacidad\""));
        b.Property(x => x.Letra).HasMaxLength(2).IsRequired();
        b.HasIndex(x => new { x.GradoId, x.PeriodoId, x.Letra }).IsUnique();
        // Token de concurrencia optimista (ver ADR 0003).
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasOne(x => x.Grado).WithMany(x => x.Secciones).HasForeignKey(x => x.GradoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Periodo).WithMany().HasForeignKey(x => x.PeriodoId).OnDelete(DeleteBehavior.Restrict);
        b.Ignore(x => x.VacantesDisponibles);
    }
}

internal sealed class CursoConfig : IEntityTypeConfiguration<Curso>
{
    public void Configure(EntityTypeBuilder<Curso> b)
    {
        b.ToTable("cursos");
        b.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.Codigo).IsUnique();
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.HasOne(x => x.Grado).WithMany(x => x.Cursos).HasForeignKey(x => x.GradoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CompetenciaConfig : IEntityTypeConfiguration<Competencia>
{
    public void Configure(EntityTypeBuilder<Competencia> b)
    {
        b.ToTable("competencias");
        b.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
        b.Property(x => x.Peso).HasPrecision(5, 2);
        b.HasOne(x => x.Curso).WithMany(x => x.Competencias).HasForeignKey(x => x.CursoId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AsignacionConfig : IEntityTypeConfiguration<AsignacionDocente>
{
    public void Configure(EntityTypeBuilder<AsignacionDocente> b)
    {
        b.ToTable("asignaciones_docentes");
        // Un curso tiene un solo docente titular por sección.
        b.HasIndex(x => new { x.CursoId, x.SeccionId }).IsUnique();
        b.HasIndex(x => x.DocenteId);
        b.HasOne(x => x.Docente).WithMany(x => x.Asignaciones).HasForeignKey(x => x.DocenteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Curso).WithMany().HasForeignKey(x => x.CursoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Seccion).WithMany().HasForeignKey(x => x.SeccionId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MatriculaConfig : IEntityTypeConfiguration<Matricula>
{
    public void Configure(EntityTypeBuilder<Matricula> b)
    {
        b.ToTable("matriculas");
        // Un alumno, una matrícula por periodo: lo garantiza la BD aunque falle la validación previa.
        b.HasIndex(x => new { x.AlumnoId, x.PeriodoId }).IsUnique();
        b.HasIndex(x => x.SeccionId);
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Alumno).WithMany(x => x.Matriculas).HasForeignKey(x => x.AlumnoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Seccion).WithMany().HasForeignKey(x => x.SeccionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Periodo).WithMany().HasForeignKey(x => x.PeriodoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class NotaConfig : IEntityTypeConfiguration<Nota>
{
    public void Configure(EntityTypeBuilder<Nota> b)
    {
        b.ToTable("notas", t =>
        {
            t.HasCheckConstraint("ck_notas_vigesimal", "\"Valor\" >= 0 AND \"Valor\" <= 20");
            t.HasCheckConstraint("ck_notas_bimestre", "\"Bimestre\" BETWEEN 1 AND 4");
        });
        b.Property(x => x.Valor).HasPrecision(4, 2);
        b.HasIndex(x => new { x.MatriculaId, x.CompetenciaId, x.Bimestre }).IsUnique();
        b.HasOne(x => x.Matricula).WithMany(x => x.Notas).HasForeignKey(x => x.MatriculaId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Competencia).WithMany().HasForeignKey(x => x.CompetenciaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AsistenciaConfig : IEntityTypeConfiguration<Asistencia>
{
    public void Configure(EntityTypeBuilder<Asistencia> b)
    {
        b.ToTable("asistencias");
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Observacion).HasMaxLength(250);
        b.HasIndex(x => new { x.MatriculaId, x.Fecha }).IsUnique();
        b.HasIndex(x => x.Fecha);
        b.HasOne(x => x.Matricula).WithMany(x => x.Asistencias).HasForeignKey(x => x.MatriculaId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CuotaConfig : IEntityTypeConfiguration<Cuota>
{
    public void Configure(EntityTypeBuilder<Cuota> b)
    {
        b.ToTable("cuotas");
        b.Property(x => x.Concepto).HasMaxLength(100).IsRequired();
        b.Property(x => x.Monto).HasPrecision(10, 2);
        b.Property(x => x.MoraPagada).HasPrecision(10, 2);
        b.Property(x => x.MedioPago).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.NumeroOperacion).HasMaxLength(50);
        b.HasIndex(x => new { x.MatriculaId, x.Numero }).IsUnique();
        b.HasIndex(x => x.FechaVencimiento);
        b.HasOne(x => x.Matricula).WithMany(x => x.Cuotas).HasForeignKey(x => x.MatriculaId).OnDelete(DeleteBehavior.Cascade);
    }
}
