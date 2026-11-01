namespace SistemaAcademico.Application.Common;

/// <summary>Recurso inexistente → 404.</summary>
public sealed class NotFoundException(string recurso, object id)
    : Exception($"{recurso} con id '{id}' no existe.");

/// <summary>
/// El usuario está autenticado pero el recurso no le pertenece → 403.
/// Ej.: un apoderado consultando la libreta de un alumno que no es su hijo.
/// </summary>
public sealed class ForbiddenException(string message = "No tiene permiso para acceder a este recurso.")
    : Exception(message);

/// <summary>Conflicto de estado o de unicidad (DNI duplicado, concurrencia) → 409.</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>Credenciales inválidas → 401.</summary>
public sealed class UnauthorizedException(string message) : Exception(message);
