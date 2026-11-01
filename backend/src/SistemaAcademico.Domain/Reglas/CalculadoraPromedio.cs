using SistemaAcademico.Domain.Common;

namespace SistemaAcademico.Domain.Reglas;

/// <summary>Nota de una competencia junto con el peso configurado para esa competencia.</summary>
public readonly record struct NotaPonderada(decimal Nota, decimal Peso);

/// <summary>
/// Cálculo de promedios. Está aislado en el dominio (sin EF ni HTTP) para poder
/// probarlo exhaustivamente con pruebas unitarias rápidas.
/// </summary>
public static class CalculadoraPromedio
{
    /// <summary>
    /// Promedio ponderado exacto (sin redondeo vigesimal), con 2 decimales.
    /// Devuelve null si no hay notas: un curso sin evaluar no es un cero.
    /// </summary>
    /// <remarks>
    /// Si faltan competencias por evaluar, el promedio se calcula con las que existen,
    /// normalizando por la suma de sus pesos. Así el docente y la familia ven un
    /// promedio "parcial" coherente durante el bimestre en lugar de uno castigado
    /// por notas aún no registradas.
    /// </remarks>
    public static decimal? PromedioPonderado(IEnumerable<NotaPonderada> notas)
    {
        var lista = notas.ToList();
        if (lista.Count == 0) return null;

        foreach (var n in lista)
        {
            EscalaVigesimal.Validar(n.Nota);
            if (n.Peso <= 0)
                throw new DomainException("El peso de una competencia debe ser mayor que cero.");
        }

        var sumaPesos = lista.Sum(n => n.Peso);
        var sumaPonderada = lista.Sum(n => n.Nota * n.Peso);
        return Math.Round(sumaPonderada / sumaPesos, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Promedio del bimestre ya redondeado a la escala vigesimal (entero 0-20).</summary>
    public static int? PromedioBimestral(IEnumerable<NotaPonderada> notas)
    {
        var exacto = PromedioPonderado(notas);
        return exacto is null ? null : EscalaVigesimal.Redondear(exacto.Value);
    }

    /// <summary>
    /// Promedio final del curso: media simple de los promedios bimestrales disponibles.
    /// Se promedian los bimestrales ya redondeados porque así figuran en la libreta
    /// y la familia debe poder reproducir el cálculo con lo que ve impreso.
    /// </summary>
    public static int? PromedioFinal(IEnumerable<int?> promediosBimestrales)
    {
        var disponibles = promediosBimestrales.Where(p => p.HasValue).Select(p => (decimal)p!.Value).ToList();
        if (disponibles.Count == 0) return null;
        return EscalaVigesimal.Redondear(disponibles.Average());
    }

    /// <summary>Valida que los pesos de las competencias de un curso sean positivos.</summary>
    public static void ValidarPesos(IEnumerable<decimal> pesos)
    {
        var lista = pesos.ToList();
        if (lista.Count == 0)
            throw new DomainException("El curso debe tener al menos una competencia.");
        if (lista.Any(p => p <= 0 || p > 100))
            throw new DomainException("Cada peso debe estar entre 0 (exclusivo) y 100.");
    }
}
