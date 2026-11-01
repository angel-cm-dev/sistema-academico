using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SistemaAcademico.Application.Abstractions;
using SistemaAcademico.Domain.Entities;

namespace SistemaAcademico.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "sistema-academico";
    public string Audience { get; set; } = "sistema-academico-web";

    /// <summary>Clave simétrica HMAC-SHA256. Debe tener al menos 32 caracteres y venir de una variable de entorno.</summary>
    public string Key { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 120;
}

/// <summary>Nombres de claims propios. Cortos para no inflar el token.</summary>
public static class ClaimsPersonalizados
{
    public const string Rol = "role";
    public const string DocenteId = "docente_id";
    public const string ApoderadoId = "apoderado_id";
}

/// <summary>
/// Adaptador sobre <see cref="PasswordHasher{TUser}"/> de ASP.NET Core Identity:
/// PBKDF2 con HMAC-SHA512, sal aleatoria e iteraciones altas, en un formato versionado
/// que permite subir el costo en el futuro sin invalidar los hashes existentes.
/// </summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<Usuario> _inner = new();
    private static readonly Usuario Dummy = new();

    public string Hash(string password) => _inner.HashPassword(Dummy, password);

    public bool Verify(string hash, string password) =>
        _inner.VerifyHashedPassword(Dummy, hash, password) != PasswordVerificationResult.Failed;
}

/// <remarks>
/// Usa <see cref="TimeProvider"/> (hora real) y no <see cref="IClock"/>: la vigencia de un token
/// es un asunto de seguridad y debe coincidir con el reloj con el que el middleware lo valida,
/// mientras que IClock representa la "fecha de negocio" (que en pruebas se fija).
/// </remarks>
public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider time) : ITokenService
{
    public TokenGenerado Generar(Usuario usuario)
    {
        var opt = options.Value;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(JwtRegisteredClaimNames.Name, usuario.NombreCompleto),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimsPersonalizados.Rol, usuario.Rol.ToString())
        };
        // Los ids de docente/apoderado viajan en el token para resolver la autorización
        // por recurso sin consultar la tabla de usuarios en cada petición.
        if (usuario.DocenteId is { } d) claims.Add(new Claim(ClaimsPersonalizados.DocenteId, d.ToString()));
        if (usuario.ApoderadoId is { } a) claims.Add(new Claim(ClaimsPersonalizados.ApoderadoId, a.ToString()));

        var ahora = time.GetUtcNow().UtcDateTime;
        var expira = ahora.AddMinutes(opt.ExpirationMinutes);
        var credenciales = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opt.Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(opt.Issuer, opt.Audience, claims, ahora, expira, credenciales);
        return new TokenGenerado(new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}

public sealed class SystemClock : IClock
{
    // Perú usa UTC-5 todo el año (sin horario de verano), por eso basta un desfase fijo
    // y no dependemos de la base de datos de zonas horarias del sistema operativo.
    private static readonly TimeSpan OffsetLima = TimeSpan.FromHours(-5);

    public DateTime UtcNow => DateTime.UtcNow;
    public DateOnly Hoy => DateOnly.FromDateTime(DateTime.UtcNow.Add(OffsetLima));
}
