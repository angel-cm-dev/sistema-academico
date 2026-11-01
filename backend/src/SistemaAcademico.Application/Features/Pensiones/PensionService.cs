using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SistemaAcademico.Application.Abstractions;
using SistemaAcademico.Application.Common;
using SistemaAcademico.Domain;
using SistemaAcademico.Domain.Entities;
using SistemaAcademico.Domain.Reglas;

namespace SistemaAcademico.Application.Features.Pensiones;

public sealed record CuotaDto(int Id, int MatriculaId, string Alumno, string Seccion, int Numero, string Concepto,
    decimal Monto, DateOnly FechaVencimiento, string Estado, int DiasAtraso, decimal Mora, decimal TotalAPagar,
    DateOnly? FechaPago, string? MedioPago, string? NumeroOperacion);

public sealed record CuotaQuery : PageQuery
{
    public int? MatriculaId { get; init; }
    public int? PeriodoId { get; init; }
    public EstadoCuota? Estado { get; init; }
}

public sealed record RegistrarPagoRequest(DateOnly? FechaPago, MedioPago MedioPago, string? NumeroOperacion);

public sealed record PagoRegistradoDto(CuotaDto Cuota, decimal MontoCobrado);

public sealed class RegistrarPagoValidator : AbstractValidator<RegistrarPagoRequest>
{
    public RegistrarPagoValidator(IClock clock)
    {
        RuleFor(x => x.MedioPago).IsInEnum();
        RuleFor(x => x.FechaPago).LessThanOrEqualTo(_ => clock.Hoy).When(x => x.FechaPago is not null)
            .WithMessage("La fecha de pago no puede ser futura.");
        // Trazabilidad: todo pago no presencial debe poder conciliarse con el banco o la billetera.
        RuleFor(x => x.NumeroOperacion).NotEmpty().MaximumLength(50)
            .When(x => x.MedioPago != MedioPago.Efectivo)
            .WithMessage("El número de operación es obligatorio para pagos no presenciales.");
    }
}

public sealed class PensionService(IAppDbContext db, ControlAcceso acceso, IClock clock, PoliticaMora politica)
{
    private static readonly SortMap<Cuota> Orden = new SortMap<Cuota>()
        .Add("vencimiento", c => c.FechaVencimiento, isDefault: true)
        .Add("alumno", c => c.Matricula.Alumno.Apellidos)
        .Add("monto", c => c.Monto)
        .Add("numero", c => c.Numero);

    public async Task<PagedResult<CuotaDto>> ListarAsync(CuotaQuery q, CancellationToken ct)
    {
        var hoy = clock.Hoy;
        var visibles = acceso.FiltrarMatriculas(db.Matriculas).Select(m => m.Id);
        var query = db.Cuotas.AsNoTracking().Where(c => visibles.Contains(c.MatriculaId));

        if (q.MatriculaId is not null) query = query.Where(c => c.MatriculaId == q.MatriculaId);
        if (q.PeriodoId is not null) query = query.Where(c => c.Matricula.PeriodoId == q.PeriodoId);
        if (q.Term is { } t)
            query = query.Where(c => c.Matricula.Alumno.Apellidos.ToLower().Contains(t) ||
                                     c.Matricula.Alumno.Nombres.ToLower().Contains(t) ||
                                     c.Matricula.Alumno.Codigo.ToLower().Contains(t));
        // El estado se deriva de la fecha de consulta, por eso el filtro se arma con "hoy".
        query = q.Estado switch
        {
            EstadoCuota.Pagada => query.Where(c => c.FechaPago != null),
            EstadoCuota.Vencida => query.Where(c => c.FechaPago == null && c.FechaVencimiento < hoy),
            EstadoCuota.Pendiente => query.Where(c => c.FechaPago == null && c.FechaVencimiento >= hoy),
            _ => query
        };

        var pagina = await Orden.Apply(query, q.SortBy, q.Descending)
            .Include(c => c.Matricula).ThenInclude(m => m.Alumno)
            .Include(c => c.Matricula).ThenInclude(m => m.Seccion).ThenInclude(s => s.Grado)
            .ToPagedAsync(q, ct);

        return new PagedResult<CuotaDto>(pagina.Items.Select(c => ToDto(c, hoy)).ToList(),
            pagina.Page, pagina.PageSize, pagina.TotalCount);
    }

    public async Task<PagoRegistradoDto> RegistrarPagoAsync(int cuotaId, RegistrarPagoRequest r, CancellationToken ct)
    {
        var cuota = await db.Cuotas
                        .Include(c => c.Matricula).ThenInclude(m => m.Alumno)
                        .Include(c => c.Matricula).ThenInclude(m => m.Seccion).ThenInclude(s => s.Grado)
                        .FirstOrDefaultAsync(c => c.Id == cuotaId, ct)
                    ?? throw new NotFoundException("Cuota", cuotaId);

        var total = cuota.RegistrarPago(r.FechaPago ?? clock.Hoy, r.MedioPago, r.NumeroOperacion?.Trim(), politica);
        await db.SaveChangesAsync(ct);
        return new PagoRegistradoDto(ToDto(cuota, clock.Hoy), total);
    }

    private CuotaDto ToDto(Cuota c, DateOnly hoy)
    {
        var estado = c.EstadoAl(hoy);
        var mora = c.MoraAl(hoy, politica);
        var referencia = c.FechaPago ?? hoy;
        return new CuotaDto(c.Id, c.MatriculaId, $"{c.Matricula.Alumno.Apellidos}, {c.Matricula.Alumno.Nombres}",
            $"{c.Matricula.Seccion.Grado.Nombre} {c.Matricula.Seccion.Letra}", c.Numero, c.Concepto, c.Monto,
            c.FechaVencimiento, estado.ToString(), CalculadoraMora.DiasAtraso(c.FechaVencimiento, referencia), mora,
            c.Monto + mora, c.FechaPago, c.MedioPago?.ToString(), c.NumeroOperacion);
    }
}
