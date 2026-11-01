namespace SistemaAcademico.Domain.Common;

/// <summary>
/// Clase base de todas las entidades. Usamos claves enteras autoincrementales
/// porque son compactas, legibles en reportes y suficientes para un colegio.
/// </summary>
public abstract class Entity
{
    public int Id { get; set; }
}

/// <summary>
/// Excepción para violaciones de reglas de negocio (p. ej. "no hay vacantes").
/// La API la traduce a 422 Unprocessable Entity con ProblemDetails.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
