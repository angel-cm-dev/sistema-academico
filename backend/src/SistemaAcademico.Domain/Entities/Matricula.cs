using SistemaAcademico.Domain.Common;
using SistemaAcademico.Domain.Reglas;

namespace SistemaAcademico.Domain.Entities;

public class Matricula : Entity
{
    public int AlumnoId { get; set; }
    public Alumno Alumno { get; set; } = null!;
    public int SeccionId { get; set; }
    public Seccion Seccion { get; set; } = null!;

    /// <summary>
    /// Redundante con Seccion.PeriodoId, pero permite un índice único (AlumnoId, PeriodoId)
    /// que impide en la base de datos matricular dos veces al mismo alumno en un año.
    /// </summary>
    public int PeriodoId { get; set; }
    public PeriodoAcademico Periodo { get; set; } = null!;
    public DateOnly FechaMatricula { get; set; }
    public EstadoMatricula Estado { get; set; } = EstadoMatricula.Activa;
    public DateOnly? FechaRetiro { get; set; }

    public List<Nota> Notas { get; set; } = [];
    public List<Asistencia> Asistencias { get; set; } = [];
    public List<Cuota> Cuotas { get; set; } = [];

    /// <summary>
    /// Retirar libera la vacante pero conserva notas, asistencia y cuotas ya pagadas.
    /// Las cuotas pendientes con vencimiento posterior al retiro dejan de ser exigibles.
    /// </summary>
    public void Retirar(DateOnly fecha)
    {
        if (Estado == EstadoMatricula.Retirada)
            throw new DomainException("La matrícula ya se encuentra retirada.");
        Estado = EstadoMatricula.Retirada;
        FechaRetiro = fecha;
        Seccion.LiberarVacante();
        Cuotas.RemoveAll(c => c.FechaPago is null && c.FechaVencimiento > fecha);
    }
}

public class Nota : Entity
{
    public int MatriculaId { get; set; }
    public Matricula Matricula { get; set; } = null!;
    public int CompetenciaId { get; set; }
    public Competencia Competencia { get; set; } = null!;

    /// <summary>Bimestre 1 a 4 (el año escolar peruano se organiza en 4 bimestres).</summary>
    public int Bimestre { get; set; }
    public decimal Valor { get; set; }
    public int RegistradoPorUsuarioId { get; set; }
    public DateTime ActualizadoEn { get; set; }

    public const int BimestreMinimo = 1;
    public const int BimestreMaximo = 4;

    public static void ValidarBimestre(int bimestre)
    {
        if (bimestre is < BimestreMinimo or > BimestreMaximo)
            throw new DomainException("El bimestre debe estar entre 1 y 4.");
    }

    public void Asignar(decimal valor, int usuarioId, DateTime ahora)
    {
        EscalaVigesimal.Validar(valor);
        Valor = valor;
        RegistradoPorUsuarioId = usuarioId;
        ActualizadoEn = ahora;
    }
}

public class Asistencia : Entity
{
    public int MatriculaId { get; set; }
    public Matricula Matricula { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public EstadoAsistencia Estado { get; set; }
    public string? Observacion { get; set; }
    public int RegistradoPorUsuarioId { get; set; }
}

/// <summary>Cuota del cronograma de pensiones de una matrícula.</summary>
public class Cuota : Entity
{
    public int MatriculaId { get; set; }
    public Matricula Matricula { get; set; } = null!;
    public int Numero { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateOnly FechaVencimiento { get; set; }

    public DateOnly? FechaPago { get; set; }
    public decimal? MoraPagada { get; set; }
    public MedioPago? MedioPago { get; set; }
    public string? NumeroOperacion { get; set; }

    public EstadoCuota EstadoAl(DateOnly fecha) =>
        FechaPago is not null ? EstadoCuota.Pagada
        : fecha > FechaVencimiento ? EstadoCuota.Vencida
        : EstadoCuota.Pendiente;

    /// <summary>Mora a una fecha. Si ya se pagó, es la mora congelada al momento del pago.</summary>
    public decimal MoraAl(DateOnly fecha, PoliticaMora politica) =>
        FechaPago is not null ? MoraPagada ?? 0m : CalculadoraMora.Calcular(Monto, FechaVencimiento, fecha, politica);

    /// <summary>
    /// El pago es total (monto + mora del día). La mora se "congela" en el registro
    /// para que el comprobante no cambie si luego se modifica la política de mora.
    /// </summary>
    public decimal RegistrarPago(DateOnly fechaPago, MedioPago medio, string? numeroOperacion, PoliticaMora politica)
    {
        if (FechaPago is not null)
            throw new DomainException("La cuota ya fue pagada.");
        var mora = CalculadoraMora.Calcular(Monto, FechaVencimiento, fechaPago, politica);
        FechaPago = fechaPago;
        MoraPagada = mora;
        MedioPago = medio;
        NumeroOperacion = numeroOperacion;
        return Monto + mora;
    }
}
