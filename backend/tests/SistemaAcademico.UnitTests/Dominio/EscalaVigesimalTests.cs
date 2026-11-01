using SistemaAcademico.Domain.Common;
using SistemaAcademico.Domain.Reglas;

namespace SistemaAcademico.UnitTests.Dominio;

public class EscalaVigesimalTests
{
    [Theory]
    [InlineData(10.5, 11)] // punto medio: sube y aprueba (no redondeo bancario)
    [InlineData(10.49, 10)]
    [InlineData(12.5, 13)] // con redondeo bancario daría 12
    [InlineData(19.6, 20)]
    [InlineData(0, 0)]
    [InlineData(20, 20)]
    public void Redondear_usa_punto_medio_hacia_arriba(decimal valor, int esperado) =>
        Assert.Equal(esperado, EscalaVigesimal.Redondear(valor));

    [Theory]
    [InlineData(-3, 0)]
    [InlineData(25, 20)]
    public void Redondear_acota_a_la_escala(decimal valor, int esperado) =>
        Assert.Equal(esperado, EscalaVigesimal.Redondear(valor));

    [Theory]
    [InlineData(11, true)]
    [InlineData(10, false)]
    public void Aprueba_desde_11(int nota, bool aprueba) => Assert.Equal(aprueba, EscalaVigesimal.Aprueba(nota));

    [Theory]
    [InlineData(20, "AD")]
    [InlineData(18, "AD")]
    [InlineData(17, "A")]
    [InlineData(14, "A")]
    [InlineData(13, "B")]
    [InlineData(11, "B")]
    [InlineData(10, "C")]
    public void Literal_equivale_a_escala_cneb(int nota, string literal) =>
        Assert.Equal(literal, EscalaVigesimal.Literal(nota));

    [Theory]
    [InlineData(-0.5)]
    [InlineData(20.01)]
    public void Validar_rechaza_notas_fuera_de_rango(decimal nota) =>
        Assert.Throws<DomainException>(() => EscalaVigesimal.Validar(nota));
}
