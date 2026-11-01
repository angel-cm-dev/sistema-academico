namespace SistemaAcademico.Api.Endpoints;

/// <summary>
/// Políticas de autorización por endpoint (primer filtro, grueso).
/// El filtro fino "¿este recurso es tuyo?" lo aplica ControlAcceso en Application.
/// </summary>
public static class Politicas
{
    public const string Admin = nameof(Admin);
    public const string AdminODocente = nameof(AdminODocente);
    public const string Autenticado = nameof(Autenticado);
}
