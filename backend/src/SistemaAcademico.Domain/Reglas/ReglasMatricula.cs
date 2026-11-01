using SistemaAcademico.Domain.Common;

namespace SistemaAcademico.Domain.Reglas;

public static class ReglasMatricula
{
    /// <summary>
    /// Una sección no puede superar su capacidad: el aforo responde a normas de
    /// seguridad del local y a la ratio alumno/docente comprometida con las familias.
    /// </summary>
    public static void ValidarVacante(int capacidad, int matriculados)
    {
        if (capacidad <= 0)
            throw new DomainException("La sección no tiene capacidad configurada.");
        if (matriculados >= capacidad)
            throw new DomainException($"La sección no tiene vacantes disponibles ({matriculados}/{capacidad}).");
    }

    public static int VacantesDisponibles(int capacidad, int matriculados) => Math.Max(0, capacidad - matriculados);

    /// <summary>Porcentaje de ocupación con 1 decimal, útil para el dashboard.</summary>
    public static decimal PorcentajeOcupacion(int capacidad, int matriculados) =>
        capacidad <= 0 ? 0 : Math.Round(matriculados * 100m / capacidad, 1, MidpointRounding.AwayFromZero);
}
