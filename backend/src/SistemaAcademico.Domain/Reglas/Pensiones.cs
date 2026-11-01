using SistemaAcademico.Domain.Common;

namespace SistemaAcademico.Domain.Reglas;

/// <summary>
/// Política de mora configurable (appsettings: <c>Pensiones:Mora</c>).
/// </summary>
/// <param name="TasaDiaria">Fracción del monto que se cobra por día de atraso (0.001 = 0.1 % diario).</param>
/// <param name="DiasGracia">Días posteriores al vencimiento en los que todavía no se cobra mora.</param>
/// <param name="TopePorcentaje">Tope de la mora como fracción del monto (0.10 = máximo 10 %).</param>
public sealed record PoliticaMora(decimal TasaDiaria, int DiasGracia, decimal TopePorcentaje)
{
    public static PoliticaMora PorDefecto { get; } = new(0.001m, 3, 0.10m);
}

public static class CalculadoraMora
{
    /// <summary>
    /// Calcula la mora de una cuota a una fecha de referencia.
    /// Reglas:
    /// 1. Si se paga hasta el día de vencimiento + días de gracia, no hay mora
    ///    (la gracia absorbe demoras bancarias o feriados).
    /// 2. Superada la gracia, la mora corre desde el día siguiente al vencimiento,
    ///    no desde que terminó la gracia: la gracia es una tolerancia, no un plazo nuevo.
    /// 3. La mora se topa en un porcentaje del monto para evitar cobros abusivos
    ///    (el valor real debe alinearse a la normativa vigente del BCRP/Indecopi).
    /// 4. Se redondea a céntimos con redondeo comercial.
    /// </summary>
    public static decimal Calcular(decimal monto, DateOnly fechaVencimiento, DateOnly fechaReferencia, PoliticaMora politica)
    {
        if (monto <= 0) throw new DomainException("El monto de la cuota debe ser positivo.");
        if (politica.TasaDiaria < 0 || politica.TopePorcentaje < 0 || politica.DiasGracia < 0)
            throw new DomainException("La política de mora tiene valores negativos.");

        var diasAtraso = fechaReferencia.DayNumber - fechaVencimiento.DayNumber;
        if (diasAtraso <= politica.DiasGracia) return 0m;

        var mora = monto * politica.TasaDiaria * diasAtraso;
        var tope = monto * politica.TopePorcentaje;
        return Math.Round(Math.Min(mora, tope), 2, MidpointRounding.AwayFromZero);
    }

    public static int DiasAtraso(DateOnly fechaVencimiento, DateOnly fechaReferencia) =>
        Math.Max(0, fechaReferencia.DayNumber - fechaVencimiento.DayNumber);
}

/// <summary>Una cuota del cronograma antes de persistirse.</summary>
public sealed record CuotaProgramada(int Numero, string Concepto, decimal Monto, DateOnly FechaVencimiento);

public static class CronogramaPensiones
{
    // Nombres fijos en lugar de CultureInfo("es-PE"): la imagen Docker corre con
    // globalización invariante (sin ICU) y el resultado debe ser idéntico en cualquier SO.
    private static readonly string[] Meses =
    [
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Setiembre", "Octubre", "Noviembre", "Diciembre"
    ];

    /// <summary>
    /// Genera las pensiones mensuales del año escolar (por defecto marzo a diciembre),
    /// con vencimiento el último día de cada mes.
    /// Un alumno que se matricula a mitad de año solo paga las cuotas cuyo
    /// vencimiento es posterior o igual a su fecha de matrícula: no se cobran meses no cursados.
    /// </summary>
    public static IReadOnlyList<CuotaProgramada> Generar(int anio, decimal montoMensual, DateOnly fechaMatricula,
        int mesInicio = 3, int mesFin = 12)
    {
        if (montoMensual <= 0) throw new DomainException("La pensión mensual debe ser positiva.");
        if (mesInicio is < 1 or > 12 || mesFin is < 1 or > 12 || mesInicio > mesFin)
            throw new DomainException("Rango de meses del cronograma inválido.");

        var cuotas = new List<CuotaProgramada>();
        var numero = 1;
        for (var mes = mesInicio; mes <= mesFin; mes++)
        {
            var vencimiento = new DateOnly(anio, mes, DateTime.DaysInMonth(anio, mes));
            if (vencimiento < fechaMatricula) continue;

            cuotas.Add(new CuotaProgramada(numero++, $"Pensión {Meses[mes - 1]} {anio}", montoMensual, vencimiento));
        }
        return cuotas;
    }
}
