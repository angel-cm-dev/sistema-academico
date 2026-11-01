using SistemaAcademico.Application.Abstractions;
using SistemaAcademico.Application.Common;
using SistemaAcademico.Application.Features.Notas;
using SistemaAcademico.Application.Features.Pensiones;
using SistemaAcademico.Application.Features.Personas;
using SistemaAcademico.Domain;

namespace SistemaAcademico.UnitTests.Aplicacion;

internal sealed class RelojFijo(DateOnly hoy) : IClock
{
    public DateTime UtcNow => hoy.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
    public DateOnly Hoy => hoy;
}

public class ValidadoresTests
{
    private static readonly RelojFijo Reloj = new(new DateOnly(2026, 10, 15));

    [Theory]
    [InlineData("1234567", false)]
    [InlineData("123456789", false)]
    [InlineData("1234567A", false)]
    [InlineData("12345678", true)]
    public void Dni_debe_tener_8_digitos(string dni, bool valido)
    {
        var r = new GuardarAlumnoValidator(Reloj).Validate(new GuardarAlumnoRequest(dni, "Ana", "Pérez", new DateOnly(2013, 5, 1), 1));
        Assert.Equal(valido, r.IsValid);
    }

    [Fact]
    public void Alumno_de_edad_no_escolar_es_rechazado()
    {
        var r = new GuardarAlumnoValidator(Reloj).Validate(new GuardarAlumnoRequest("12345678", "Ana", "Pérez", new DateOnly(1990, 1, 1), 1));
        Assert.Contains(r.Errors, e => e.PropertyName == "FechaNacimiento");
    }

    [Theory]
    [InlineData(20.5, false)]
    [InlineData(-1, false)]
    [InlineData(15.25, true)]
    [InlineData(20, true)]
    public void Nota_debe_estar_en_escala_vigesimal(decimal valor, bool valido)
    {
        var r = new GuardarNotasValidator().Validate(new GuardarNotasRequest(1, 1, [new NotaInput(1, 1, valor)]));
        Assert.Equal(valido, r.IsValid);
    }

    [Fact]
    public void Bimestre_fuera_de_rango_es_invalido()
    {
        var r = new GuardarNotasValidator().Validate(new GuardarNotasRequest(1, 5, []));
        Assert.False(r.IsValid);
    }

    [Fact]
    public void Pago_no_presencial_exige_numero_de_operacion()
    {
        var validator = new RegistrarPagoValidator(Reloj);
        Assert.False(validator.Validate(new RegistrarPagoRequest(null, MedioPago.Transferencia, null)).IsValid);
        Assert.True(validator.Validate(new RegistrarPagoRequest(null, MedioPago.Efectivo, null)).IsValid);
    }

    [Fact]
    public void Pago_con_fecha_futura_es_invalido() =>
        Assert.False(new RegistrarPagoValidator(Reloj)
            .Validate(new RegistrarPagoRequest(new DateOnly(2026, 12, 1), MedioPago.Efectivo, null)).IsValid);

    [Fact]
    public void PageQuery_acota_valores_peligrosos()
    {
        var q = new PageQuery { Page = -5, PageSize = 5000, Search = "  PÉREZ " };
        Assert.Equal(1, q.SafePage);
        Assert.Equal(PageQuery.MaxPageSize, q.SafePageSize);
        Assert.Equal("pérez", q.Term);
    }
}
