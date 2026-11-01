using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace SistemaAcademico.Application.Common;

/// <summary>
/// Parámetros comunes de listados: página, tamaño, búsqueda libre y orden.
/// Se reciben por query string (?page=1&amp;pageSize=10&amp;search=perez&amp;sortBy=apellidos&amp;sortDir=desc).
/// </summary>
public record PageQuery
{
    public const int MaxPageSize = 100;

    public const int DefaultPageSize = 10;

    // Nullables a propósito: con [AsParameters] una propiedad no anulable se vuelve
    // obligatoria en la query string; así los valores por defecto se aplican aquí.
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string? SortDir { get; init; }

    // Se acotan en vez de rechazar: un cliente que pide pageSize=5000 recibe 100,
    // lo que protege a la BD sin romper integraciones existentes.
    public int SafePage => Math.Max(1, Page ?? 1);
    public int SafePageSize => Math.Clamp(PageSize ?? DefaultPageSize, 1, MaxPageSize);
    public bool Descending => string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase);
    public string? Term => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim().ToLower();
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>
/// Lista blanca de columnas ordenables. Nunca se ordena por un nombre de columna
/// recibido "tal cual" del cliente: evita errores y exposición de campos internos.
/// </summary>
public sealed class SortMap<T>
{
    private readonly Dictionary<string, Func<IQueryable<T>, bool, IOrderedQueryable<T>>> _map =
        new(StringComparer.OrdinalIgnoreCase);
    private string? _default;

    public SortMap<T> Add<TKey>(string key, Expression<Func<T, TKey>> selector, bool isDefault = false)
    {
        _map[key] = (q, desc) => desc ? q.OrderByDescending(selector) : q.OrderBy(selector);
        if (isDefault) _default = key;
        return this;
    }

    public IQueryable<T> Apply(IQueryable<T> query, string? sortBy, bool descending)
    {
        var key = sortBy is not null && _map.ContainsKey(sortBy) ? sortBy : _default;
        return key is null ? query : _map[key](query, descending);
    }
}

public static class PaginacionExtensions
{
    public static async Task<PagedResult<T>> ToPagedAsync<T>(this IQueryable<T> query, PageQuery page,
        CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page.SafePage - 1) * page.SafePageSize)
            .Take(page.SafePageSize)
            .ToListAsync(ct);
        return new PagedResult<T>(items, page.SafePage, page.SafePageSize, total);
    }
}
