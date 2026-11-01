using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SistemaAcademico.Application.Abstractions;
using SistemaAcademico.Application.Common;
using SistemaAcademico.Domain;
using SistemaAcademico.Domain.Entities;

namespace SistemaAcademico.Application.Features.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record UsuarioDto(int Id, string Email, string NombreCompleto, string Rol, int? DocenteId, int? ApoderadoId);

public sealed record LoginResponse(string Token, DateTime ExpiraEn, UsuarioDto Usuario);

public sealed record CrearUsuarioRequest(string Email, string Password, string NombreCompleto, Rol Rol,
    int? DocenteId, int? ApoderadoId);

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class CrearUsuarioRequestValidator : AbstractValidator<CrearUsuarioRequest>
{
    public CrearUsuarioRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.NombreCompleto).NotEmpty().MaximumLength(150);
        // Política mínima razonable: longitud + mezcla de tipos de caracteres.
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("La contraseña debe incluir una mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe incluir una minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe incluir un número.");
        RuleFor(x => x.Rol).IsInEnum();
        // Coherencia rol ↔ persona vinculada: sin esto un "docente" no tendría cursos que ver.
        RuleFor(x => x.DocenteId).NotNull().When(x => x.Rol == Rol.Docente)
            .WithMessage("Un usuario docente debe vincularse a un docente.");
        RuleFor(x => x.ApoderadoId).NotNull().When(x => x.Rol == Rol.Apoderado)
            .WithMessage("Un usuario apoderado debe vincularse a un apoderado.");
    }
}

public sealed class AuthService(IAppDbContext db, IPasswordHasher hasher, ITokenService tokens,
    ICurrentUser currentUser, IClock clock)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Mismo mensaje para "no existe" y "contraseña incorrecta": no revelamos qué correos están registrados.
        if (usuario is null || !usuario.Activo || !hasher.Verify(usuario.PasswordHash, request.Password))
            throw new UnauthorizedException("Correo o contraseña incorrectos.");

        var token = tokens.Generar(usuario);
        return new LoginResponse(token.Token, token.ExpiraEn, ToDto(usuario));
    }

    public async Task<UsuarioDto> MeAsync(CancellationToken ct)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct)
                      ?? throw new NotFoundException("Usuario", currentUser.UserId);
        return ToDto(usuario);
    }

    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken ct) =>
        (await db.Usuarios.OrderBy(u => u.Email).ToListAsync(ct)).Select(ToDto).ToList();

    public async Task<UsuarioDto> CrearAsync(CrearUsuarioRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException($"Ya existe un usuario con el correo {email}.");
        if (request.DocenteId is { } d && !await db.Docentes.AnyAsync(x => x.Id == d, ct))
            throw new NotFoundException("Docente", d);
        if (request.ApoderadoId is { } a && !await db.Apoderados.AnyAsync(x => x.Id == a, ct))
            throw new NotFoundException("Apoderado", a);

        var usuario = new Usuario
        {
            Email = email,
            NombreCompleto = request.NombreCompleto.Trim(),
            Rol = request.Rol,
            DocenteId = request.Rol == Rol.Docente ? request.DocenteId : null,
            ApoderadoId = request.Rol == Rol.Apoderado ? request.ApoderadoId : null,
            PasswordHash = hasher.Hash(request.Password),
            CreadoEn = clock.UtcNow
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);
        return ToDto(usuario);
    }

    private static UsuarioDto ToDto(Usuario u) =>
        new(u.Id, u.Email, u.NombreCompleto, u.Rol.ToString(), u.DocenteId, u.ApoderadoId);
}
