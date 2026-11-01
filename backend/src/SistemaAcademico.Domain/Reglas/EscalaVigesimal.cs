using SistemaAcademico.Domain.Common;

namespace SistemaAcademico.Domain.Reglas;

/// <summary>
/// Escala vigesimal (0-20) usada en la educación peruana.
/// </summary>
public static class EscalaVigesimal
{
    public const decimal NotaMinima = 0m;
    public const decimal NotaMaxima = 20m;

    /// <summary>
    /// Nota mínima aprobatoria. En la escala vigesimal se aprueba con 11:
    /// un promedio de 10.5 se redondea a 11 y, por lo tanto, aprueba.
    /// </summary>
    public const int NotaAprobatoria = 11;

    public static bool EsValida(decimal nota) => nota is >= NotaMinima and <= NotaMaxima;

    public static void Validar(decimal nota)
    {
        if (!EsValida(nota))
            throw new DomainException($"La nota {nota} está fuera de la escala vigesimal (0 a 20).");
    }

    /// <summary>
    /// Redondea al entero más cercano y, en el punto medio, hacia arriba (10.5 → 11).
    /// Se usa <see cref="MidpointRounding.AwayFromZero"/> a propósito: el redondeo por
    /// defecto de .NET es "bancario" (10.5 → 10) y desaprobaría injustamente al alumno.
    /// </summary>
    public static int Redondear(decimal valor)
    {
        var acotado = Math.Clamp(valor, NotaMinima, NotaMaxima);
        return (int)Math.Round(acotado, 0, MidpointRounding.AwayFromZero);
    }

    public static bool Aprueba(int promedioRedondeado) => promedioRedondeado >= NotaAprobatoria;

    /// <summary>
    /// Equivalencia referencial con la escala literal del Currículo Nacional
    /// (AD logro destacado, A logro esperado, B en proceso, C en inicio).
    /// Se muestra en la libreta como apoyo para las familias.
    /// </summary>
    public static string Literal(int promedioRedondeado) => promedioRedondeado switch
    {
        >= 18 => "AD",
        >= 14 => "A",
        >= 11 => "B",
        _ => "C"
    };
}
