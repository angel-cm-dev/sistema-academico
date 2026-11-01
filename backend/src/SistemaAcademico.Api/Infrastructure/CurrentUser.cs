using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SistemaAcademico.Application.Abstractions;
using SistemaAcademico.Domain;
using SistemaAcademico.Infrastructure.Security;

namespace SistemaAcademico.Api.Infrastructure;

/// <summary>Lee la identidad desde los claims del JWT validado por el middleware de autenticación.</summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    // MapInboundClaims = false (ver Program.cs): los claims llegan con su nombre JWT original.
    public int UserId => int.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : 0;

    public Rol Rol => Enum.TryParse<Rol>(Principal?.FindFirstValue(ClaimsPersonalizados.Rol), out var rol)
        ? rol
        : throw new UnauthorizedAccessException("Token sin rol válido.");

    public int? DocenteId => ParseNullable(ClaimsPersonalizados.DocenteId);
    public int? ApoderadoId => ParseNullable(ClaimsPersonalizados.ApoderadoId);

    private int? ParseNullable(string claim) =>
        int.TryParse(Principal?.FindFirstValue(claim), out var v) ? v : null;
}
