using SistemaAcademico.Domain;
using SistemaAcademico.Domain.Common;
using SistemaAcademico.Domain.Entities;
using SistemaAcademico.Domain.Reglas;

namespace SistemaAcademico.UnitTests.Dominio;

public class MatriculaTests
{
    [Fact]
    public void Ocupar_vacante_incrementa_y_cambia_version()
    {
        var seccion = new Seccion { Capacidad = 2 };
        var versionInicial = seccion.Version;

        seccion.OcuparVacante();

        Assert.Equal(1, seccion.Matriculados);
        Assert.Equal(1, seccion.VacantesDisponibles);
        Assert.NotEqual(versionInicial, seccion.Version); // necesario para la concurrencia optimista
    }

    [Fact]
    public void No_se_puede_superar_la_capacidad()
    {
        var seccion = new Seccion { Capacidad = 2, Matriculados = 2 };
        var ex = Assert.Throws<DomainException>(seccion.OcuparVacante);
        Assert.Contains("vacantes", ex.Message);
    }

    [Fact]
    public void Seccion_sin_capacidad_configurada_no_admite_matriculas() =>
        Assert.Throws<DomainException>(() => ReglasMatricula.ValidarVacante(0, 0));

    [Theory]
    [InlineData(30, 15, 50.0)]
    [InlineData(30, 30, 100.0)]
    [InlineData(3, 1, 33.3)]
    [InlineData(0, 0, 0)]
    public void Porcentaje_de_ocupacion(int capacidad, int matriculados, decimal esperado) =>
        Assert.Equal(esperado, ReglasMatricula.PorcentajeOcupacion(capacidad, matriculados));

    [Fact]
    public void Retirar_libera_vacante_y_anula_cuotas_futuras_no_pagadas()
    {
        var seccion = new Seccion { Capacidad = 30, Matriculados = 10 };
        var matricula = new Matricula { Seccion = seccion };
        matricula.Cuotas.AddRange(
        [
            new Cuota { Numero = 1, Monto = 300, FechaVencimiento = new DateOnly(2026, 3, 31), FechaPago = new DateOnly(2026, 3, 30) },
            new Cuota { Numero = 2, Monto = 300, FechaVencimiento = new DateOnly(2026, 4, 30) }, // vencida y no pagada: sigue siendo deuda
            new Cuota { Numero = 3, Monto = 300, FechaVencimiento = new DateOnly(2026, 6, 30) }  // futura: se anula
        ]);

        matricula.Retirar(new DateOnly(2026, 5, 15));

        Assert.Equal(EstadoMatricula.Retirada, matricula.Estado);
        Assert.Equal(9, seccion.Matriculados);
        Assert.Equal([1, 2], matricula.Cuotas.Select(c => c.Numero));
    }

    [Fact]
    public void No_se_puede_retirar_dos_veces()
    {
        var matricula = new Matricula { Seccion = new Seccion { Capacidad = 1, Matriculados = 1 } };
        matricula.Retirar(new DateOnly(2026, 5, 1));
        Assert.Throws<DomainException>(() => matricula.Retirar(new DateOnly(2026, 5, 2)));
    }
}
