using SistemaAcademico.Domain;
using SistemaAcademico.Domain.Common;
using SistemaAcademico.Domain.Entities;
using SistemaAcademico.Domain.Reglas;

namespace SistemaAcademico.UnitTests.Dominio;

public class CalculadoraMoraTests
{
    private static readonly PoliticaMora Politica = new(TasaDiaria: 0.001m, DiasGracia: 3, TopePorcentaje: 0.10m);
    private static readonly DateOnly Vence = new(2026, 4, 30);

    [Fact]
    public void Pago_puntual_no_genera_mora() =>
        Assert.Equal(0m, CalculadoraMora.Calcular(350m, Vence, Vence, Politica));

    [Fact]
    public void Dentro_de_los_dias_de_gracia_no_hay_mora() =>
        Assert.Equal(0m, CalculadoraMora.Calcular(350m, Vence, Vence.AddDays(3), Politica));

    [Fact]
    public void Superada_la_gracia_la_mora_corre_desde_el_vencimiento()
    {
        // 4 días de atraso → 350 * 0.001 * 4 = 1.40 (no 1 día, la gracia no es un plazo nuevo)
        Assert.Equal(1.40m, CalculadoraMora.Calcular(350m, Vence, Vence.AddDays(4), Politica));
    }

    [Fact]
    public void Mora_se_redondea_a_centimos()
    {
        // 333.33 * 0.001 * 10 = 3.3333 → 3.33
        Assert.Equal(3.33m, CalculadoraMora.Calcular(333.33m, Vence, Vence.AddDays(10), Politica));
    }

    [Fact]
    public void Mora_tiene_tope()
    {
        // 300 días → 105.00 sin tope; con tope 10 % → 35.00
        Assert.Equal(35m, CalculadoraMora.Calcular(350m, Vence, Vence.AddDays(300), Politica));
    }

    [Fact]
    public void Politica_sin_gracia_cobra_desde_el_primer_dia()
    {
        var sinGracia = Politica with { DiasGracia = 0 };
        Assert.Equal(0.35m, CalculadoraMora.Calcular(350m, Vence, Vence.AddDays(1), sinGracia));
    }

    [Fact]
    public void Monto_invalido_lanza_excepcion() =>
        Assert.Throws<DomainException>(() => CalculadoraMora.Calcular(0m, Vence, Vence, Politica));

    [Fact]
    public void Pago_congela_la_mora_y_no_permite_doble_pago()
    {
        var cuota = new Cuota { Monto = 350m, FechaVencimiento = Vence };

        var total = cuota.RegistrarPago(Vence.AddDays(10), MedioPago.Yape, "OP-1", Politica);

        Assert.Equal(353.50m, total); // 350 + 3.50
        Assert.Equal(3.50m, cuota.MoraAl(Vence.AddDays(90), Politica)); // ya no crece
        Assert.Equal(EstadoCuota.Pagada, cuota.EstadoAl(Vence.AddDays(90)));
        Assert.Throws<DomainException>(() => cuota.RegistrarPago(Vence.AddDays(11), MedioPago.Efectivo, null, Politica));
    }

    [Fact]
    public void Estado_de_cuota_depende_de_la_fecha()
    {
        var cuota = new Cuota { Monto = 350m, FechaVencimiento = Vence };
        Assert.Equal(EstadoCuota.Pendiente, cuota.EstadoAl(Vence));
        Assert.Equal(EstadoCuota.Vencida, cuota.EstadoAl(Vence.AddDays(1)));
    }
}

public class CronogramaPensionesTests
{
    [Fact]
    public void Genera_diez_cuotas_de_marzo_a_diciembre()
    {
        var cuotas = CronogramaPensiones.Generar(2026, 350m, new DateOnly(2026, 2, 15));

        Assert.Equal(10, cuotas.Count);
        Assert.Equal(new DateOnly(2026, 3, 31), cuotas[0].FechaVencimiento);
        Assert.Equal(new DateOnly(2026, 12, 31), cuotas[^1].FechaVencimiento);
        Assert.Equal("Pensión Marzo 2026", cuotas[0].Concepto);
        Assert.Equal(Enumerable.Range(1, 10), cuotas.Select(c => c.Numero));
        Assert.Equal(3500m, cuotas.Sum(c => c.Monto));
    }

    [Fact]
    public void Vencimiento_es_el_ultimo_dia_del_mes()
    {
        var cuotas = CronogramaPensiones.Generar(2026, 350m, new DateOnly(2026, 1, 1));
        Assert.Equal(new DateOnly(2026, 4, 30), cuotas[1].FechaVencimiento);
        Assert.Equal(new DateOnly(2026, 9, 30), cuotas[6].FechaVencimiento);
    }

    [Fact]
    public void Matricula_a_mitad_de_anio_no_cobra_meses_pasados()
    {
        // Se matricula el 10 de mayo: paga mayo (vence 31/05) a diciembre = 8 cuotas.
        var cuotas = CronogramaPensiones.Generar(2026, 350m, new DateOnly(2026, 5, 10));

        Assert.Equal(8, cuotas.Count);
        Assert.Equal("Pensión Mayo 2026", cuotas[0].Concepto);
        Assert.Equal(1, cuotas[0].Numero);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Monto_no_positivo_es_invalido(decimal monto) =>
        Assert.Throws<DomainException>(() => CronogramaPensiones.Generar(2026, monto, new DateOnly(2026, 1, 1)));
}
