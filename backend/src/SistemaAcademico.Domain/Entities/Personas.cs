using SistemaAcademico.Domain.Common;

namespace SistemaAcademico.Domain.Entities;

/// <summary>
/// Cuenta de acceso. Se separa de Docente/Apoderado porque no toda persona registrada
/// necesita credenciales (p. ej. un apoderado que aún no activa su cuenta).
/// </summary>
public class Usuario : Entity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public Rol Rol { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; }

    // Vínculo opcional con la persona que representa. Solo uno aplica según el rol.
    public int? DocenteId { get; set; }
    public Docente? Docente { get; set; }
    public int? ApoderadoId { get; set; }
    public Apoderado? Apoderado { get; set; }
}

public class Apoderado : Entity
{
    public string Dni { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public List<Alumno> Alumnos { get; set; } = [];

    public string NombreCompleto => $"{Apellidos}, {Nombres}";
}

public class Alumno : Entity
{
    public string Codigo { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public DateOnly FechaNacimiento { get; set; }
    public int ApoderadoId { get; set; }
    public Apoderado Apoderado { get; set; } = null!;

    /// <summary>
    /// Baja lógica: un alumno nunca se elimina físicamente porque sus notas y pagos
    /// históricos deben conservarse (certificados de estudios, auditoría).
    /// </summary>
    public bool Activo { get; set; } = true;
    public List<Matricula> Matriculas { get; set; } = [];

    public string NombreCompleto => $"{Apellidos}, {Nombres}";
}

public class Docente : Entity
{
    public string Dni { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string? Email { get; set; }
    public List<AsignacionDocente> Asignaciones { get; set; } = [];

    public string NombreCompleto => $"{Apellidos}, {Nombres}";
}
