using SistemaAcademico.Domain.Common;
using SistemaAcademico.Domain.Reglas;

namespace SistemaAcademico.Domain.Entities;

/// <summary>Año escolar. Define el monto de pensión y agrupa secciones y matrículas.</summary>
public class PeriodoAcademico : Entity
{
    public int Anio { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public decimal PensionMensual { get; set; }

    /// <summary>Solo un periodo está activo: es el que se usa por defecto en matrícula y dashboard.</summary>
    public bool Activo { get; set; }
}

public class Grado : Entity
{
    public string Nombre { get; set; } = string.Empty;
    public NivelEducativo Nivel { get; set; }

    /// <summary>Orden de presentación (1° primaria antes que 2° primaria, etc.).</summary>
    public int Orden { get; set; }
    public List<Seccion> Secciones { get; set; } = [];
    public List<Curso> Cursos { get; set; } = [];
}

public class Seccion : Entity
{
    public int GradoId { get; set; }
    public Grado Grado { get; set; } = null!;
    public int PeriodoId { get; set; }
    public PeriodoAcademico Periodo { get; set; } = null!;
    public string Letra { get; set; } = string.Empty;
    public int Capacidad { get; set; }

    /// <summary>
    /// Contador desnormalizado de matrículas activas. Junto con <see cref="Version"/>
    /// permite control de concurrencia optimista: si dos secretarias matriculan a la vez
    /// en la última vacante, solo una transacción gana y la otra recibe 409 (ver ADR 0003).
    /// </summary>
    public int Matriculados { get; set; }

    /// <summary>Token de concurrencia: cambia en cada modificación del cupo.</summary>
    public Guid Version { get; set; } = Guid.NewGuid();

    public int VacantesDisponibles => ReglasMatricula.VacantesDisponibles(Capacidad, Matriculados);

    public void OcuparVacante()
    {
        ReglasMatricula.ValidarVacante(Capacidad, Matriculados);
        Matriculados++;
        Version = Guid.NewGuid();
    }

    public void LiberarVacante()
    {
        if (Matriculados > 0) Matriculados--;
        Version = Guid.NewGuid();
    }
}

public class Curso : Entity
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int GradoId { get; set; }
    public Grado Grado { get; set; } = null!;
    public List<Competencia> Competencias { get; set; } = [];
}

/// <summary>
/// Competencia evaluable de un curso (enfoque por competencias del CNEB).
/// El <see cref="Peso"/> es configurable por el administrador y define el promedio ponderado.
/// </summary>
public class Competencia : Entity
{
    public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;
    public string Nombre { get; set; } = string.Empty;
    public decimal Peso { get; set; } = 1m;
    public int Orden { get; set; }
}

/// <summary>
/// "El docente X dicta el curso Y en la sección Z". Es la pieza central de la
/// autorización por recurso: un docente solo ve y califica lo que tiene asignado.
/// </summary>
public class AsignacionDocente : Entity
{
    public int DocenteId { get; set; }
    public Docente Docente { get; set; } = null!;
    public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;
    public int SeccionId { get; set; }
    public Seccion Seccion { get; set; } = null!;
}
