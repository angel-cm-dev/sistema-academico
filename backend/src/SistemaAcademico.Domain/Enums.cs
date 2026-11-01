namespace SistemaAcademico.Domain;

/// <summary>Roles del sistema. Se guardan como texto para que la BD sea legible.</summary>
public enum Rol
{
    Admin,
    Docente,
    Apoderado
}

public enum NivelEducativo
{
    Primaria,
    Secundaria
}

public enum EstadoMatricula
{
    Activa,
    Retirada
}

public enum EstadoAsistencia
{
    Presente,
    Tarde,
    Falta,
    FaltaJustificada
}

/// <summary>
/// Estado "calculado" de una cuota. Solo <see cref="Pagada"/> se persiste de forma implícita
/// (FechaPago != null); Vencida/Pendiente dependen de la fecha de consulta.
/// </summary>
public enum EstadoCuota
{
    Pendiente,
    Vencida,
    Pagada
}

public enum MedioPago
{
    Efectivo,
    Transferencia,
    Tarjeta,
    Yape
}
