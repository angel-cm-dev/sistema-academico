namespace SistemaAcademico.Domain.Reglas;

public static class ReglasPromocion
{
    /// <summary>
    /// Situación final del alumno a partir de los cursos desaprobados.
    /// Versión simplificada e ilustrativa de las normas de evaluación de secundaria:
    /// 0 desaprobados → promovido; 1 a 3 → recuperación pedagógica; 4 o más → permanece.
    /// Mientras el año no esté cerrado (faltan notas), la situación es "En curso".
    /// </summary>
    public static string Situacion(int cursosDesaprobados, bool evaluacionCompleta) =>
        !evaluacionCompleta ? "En curso"
        : cursosDesaprobados switch
        {
            0 => "Promovido",
            <= 3 => "Requiere recuperación pedagógica",
            _ => "Permanece en el grado"
        };
}
