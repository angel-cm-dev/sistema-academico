using SistemaAcademico.Domain;
using SistemaAcademico.Domain.Reglas;

namespace SistemaAcademico.UnitTests.Dominio;

public class AsistenciaYPromocionTests
{
    [Fact]
    public void Tardanza_cuenta_como_asistencia_y_falta_justificada_no_penaliza()
    {
        var resumen = ResumenAsistencia.Desde(
        [
            EstadoAsistencia.Presente, EstadoAsistencia.Presente, EstadoAsistencia.Tarde,
            EstadoAsistencia.Falta, EstadoAsistencia.FaltaJustificada
        ]);

        Assert.Equal(5, resumen.Total);
        Assert.Equal(75m, resumen.Porcentaje); // 3 asistencias / 4 computables
        Assert.False(resumen.EnRiesgo);
    }

    [Fact]
    public void Menos_de_70_por_ciento_es_riesgo()
    {
        var resumen = new ResumenAsistencia(Presentes: 6, Tardanzas: 0, Faltas: 4, FaltasJustificadas: 0);
        Assert.Equal(60m, resumen.Porcentaje);
        Assert.True(resumen.EnRiesgo);
    }

    [Fact]
    public void Sin_registros_es_100_por_ciento_y_sin_riesgo()
    {
        var resumen = ResumenAsistencia.Desde([]);
        Assert.Equal(100m, resumen.Porcentaje);
        Assert.False(resumen.EnRiesgo);
    }

    [Theory]
    [InlineData(0, true, "Promovido")]
    [InlineData(2, true, "Requiere recuperación pedagógica")]
    [InlineData(4, true, "Permanece en el grado")]
    [InlineData(0, false, "En curso")]
    public void Situacion_final(int desaprobados, bool completa, string esperado) =>
        Assert.Equal(esperado, ReglasPromocion.Situacion(desaprobados, completa));
}
