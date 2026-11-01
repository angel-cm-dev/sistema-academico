using SistemaAcademico.Domain.Common;
using SistemaAcademico.Domain.Reglas;

namespace SistemaAcademico.UnitTests.Dominio;

public class CalculadoraPromedioTests
{
    [Fact]
    public void Promedio_ponderado_respeta_los_pesos()
    {
        // (16*2 + 10*1 + 12*1) / 4 = 54/4 = 13.5
        var notas = new[] { new NotaPonderada(16, 2), new NotaPonderada(10, 1), new NotaPonderada(12, 1) };

        Assert.Equal(13.5m, CalculadoraPromedio.PromedioPonderado(notas));
        Assert.Equal(14, CalculadoraPromedio.PromedioBimestral(notas)); // 13.5 → 14
    }

    [Fact]
    public void Con_pesos_iguales_equivale_a_media_simple()
    {
        var notas = new[] { new NotaPonderada(11, 1), new NotaPonderada(12, 1), new NotaPonderada(14, 1) };
        Assert.Equal(12.33m, CalculadoraPromedio.PromedioPonderado(notas));
        Assert.Equal(12, CalculadoraPromedio.PromedioBimestral(notas));
    }

    [Fact]
    public void Peso_alto_puede_cambiar_la_aprobacion()
    {
        // Mismas notas, distinto peso: con media simple 10.0 desaprueba; con peso 3 en la competencia de 14 aprueba.
        var simple = new[] { new NotaPonderada(14, 1), new NotaPonderada(6, 1) };
        var ponderado = new[] { new NotaPonderada(14, 3), new NotaPonderada(6, 1) };

        Assert.Equal(10, CalculadoraPromedio.PromedioBimestral(simple));
        Assert.Equal(12, CalculadoraPromedio.PromedioBimestral(ponderado)); // (42+6)/4 = 12
    }

    [Fact]
    public void Sin_notas_el_promedio_es_nulo_y_no_cero() =>
        Assert.Null(CalculadoraPromedio.PromedioPonderado([]));

    [Fact]
    public void Peso_cero_o_negativo_es_invalido() =>
        Assert.Throws<DomainException>(() => CalculadoraPromedio.PromedioPonderado([new NotaPonderada(15, 0)]));

    [Fact]
    public void Nota_fuera_de_escala_es_invalida() =>
        Assert.Throws<DomainException>(() => CalculadoraPromedio.PromedioPonderado([new NotaPonderada(21, 1)]));

    [Fact]
    public void Promedio_final_promedia_los_bimestrales_disponibles()
    {
        // (12 + 13 + 11) / 3 = 12 ; el bimestre sin notas no cuenta como cero
        Assert.Equal(12, CalculadoraPromedio.PromedioFinal([12, 13, 11, null]));
    }

    [Fact]
    public void Promedio_final_redondea_punto_medio_hacia_arriba()
    {
        // (10 + 11) / 2 = 10.5 → 11 (aprueba)
        Assert.Equal(11, CalculadoraPromedio.PromedioFinal([10, 11]));
    }

    [Fact]
    public void Promedio_final_sin_datos_es_nulo() => Assert.Null(CalculadoraPromedio.PromedioFinal([null, null]));

    [Fact]
    public void Validar_pesos_exige_al_menos_una_competencia() =>
        Assert.Throws<DomainException>(() => CalculadoraPromedio.ValidarPesos([]));
}
