using Microsoft.EntityFrameworkCore;
using SistemaAcademico.Application.Abstractions;
using SistemaAcademico.Domain;
using SistemaAcademico.Domain.Reglas;

namespace SistemaAcademico.Application.Features.Dashboard;

/// <summary>KPI genérico: el frontend lo pinta según <see cref="Formato"/> (numero | moneda | porcentaje).</summary>
public sealed record KpiDto(string Clave, string Titulo, decimal Valor, string Formato, string? Detalle = null);

public sealed record OcupacionSeccionDto(int SeccionId, string Seccion, int Capacidad, int Matriculados, decimal Ocupacion);

public sealed record DashboardDto(string Rol, int? PeriodoId, int? Anio, IReadOnlyList<KpiDto> Kpis,
    IReadOnlyList<OcupacionSeccionDto> Secciones);

/// <summary>Dashboard según rol: cada perfil ve solo indicadores de lo que le corresponde.</summary>
public sealed class DashboardService(IAppDbContext db, ICurrentUser user, IClock clock, PoliticaMora politica)
{
    public async Task<DashboardDto> ObtenerAsync(CancellationToken ct)
    {
        var periodo = await db.Periodos.AsNoTracking().FirstOrDefaultAsync(p => p.Activo, ct);
        var periodoId = periodo?.Id ?? 0;
        var hoy = clock.Hoy;

        var kpis = user.Rol switch
        {
            Rol.Admin => await KpisAdminAsync(periodoId, hoy, ct),
            Rol.Docente => await KpisDocenteAsync(periodoId, hoy, ct),
            _ => await KpisApoderadoAsync(periodoId, hoy, ct)
        };

        var seccionesQuery = db.Secciones.AsNoTracking().Where(s => s.PeriodoId == periodoId);
        if (user.Rol == Rol.Docente)
            seccionesQuery = seccionesQuery.Where(s => db.Asignaciones.Any(a => a.SeccionId == s.Id && a.DocenteId == user.DocenteId));
        else if (user.Rol == Rol.Apoderado)
            seccionesQuery = seccionesQuery.Where(_ => false);

        var secciones = (await seccionesQuery.OrderBy(s => s.Grado.Orden).ThenBy(s => s.Letra)
                .Select(s => new { s.Id, Nombre = s.Grado.Nombre + " " + s.Letra, s.Capacidad, s.Matriculados })
                .ToListAsync(ct))
            .Select(s => new OcupacionSeccionDto(s.Id, s.Nombre, s.Capacidad, s.Matriculados,
                ReglasMatricula.PorcentajeOcupacion(s.Capacidad, s.Matriculados)))
            .ToList();

        return new DashboardDto(user.Rol.ToString(), periodo?.Id, periodo?.Anio, kpis, secciones);
    }

    private async Task<List<KpiDto>> KpisAdminAsync(int periodoId, DateOnly hoy, CancellationToken ct)
    {
        var alumnos = await db.Alumnos.CountAsync(a => a.Activo, ct);
        var matriculas = await db.Matriculas.CountAsync(m => m.PeriodoId == periodoId && m.Estado == EstadoMatricula.Activa, ct);
        var capacidad = await db.Secciones.Where(s => s.PeriodoId == periodoId).SumAsync(s => s.Capacidad, ct);

        // Las cuotas vencidas se cargan en memoria para calcular la mora con la regla de dominio
        // (no se duplica la fórmula en SQL). Para volúmenes grandes convendría una vista materializada.
        var vencidas = await db.Cuotas.AsNoTracking()
            .Where(c => c.Matricula.PeriodoId == periodoId && c.FechaPago == null && c.FechaVencimiento < hoy)
            .Select(c => new { c.Monto, c.FechaVencimiento })
            .ToListAsync(ct);
        var montoVencido = vencidas.Sum(c => c.Monto);
        var mora = vencidas.Sum(c => CalculadoraMora.Calcular(c.Monto, c.FechaVencimiento, hoy, politica));

        var inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);
        var pagosMes = await db.Cuotas.AsNoTracking()
            .Where(c => c.FechaPago != null && c.FechaPago >= inicioMes && c.FechaPago <= hoy)
            .Select(c => new { c.Monto, c.MoraPagada })
            .ToListAsync(ct);

        var asistencias = await db.Asistencias.AsNoTracking()
            .Where(a => a.Matricula.PeriodoId == periodoId && a.Fecha >= hoy.AddDays(-30))
            .Select(a => a.Estado).ToListAsync(ct);

        return
        [
            new("alumnos", "Alumnos activos", alumnos, "numero"),
            new("matriculas", "Matrículas del periodo", matriculas, "numero", $"{ReglasMatricula.VacantesDisponibles(capacidad, matriculas)} vacantes libres"),
            new("ocupacion", "Ocupación de aulas", ReglasMatricula.PorcentajeOcupacion(capacidad, matriculas), "porcentaje"),
            new("recaudado", "Recaudado este mes", pagosMes.Sum(p => p.Monto + (p.MoraPagada ?? 0)), "moneda"),
            new("vencido", "Pensiones vencidas", montoVencido, "moneda", $"{vencidas.Count} cuotas · mora S/ {mora:0.00}"),
            new("asistencia", "Asistencia (últimos 30 días)", ResumenAsistencia.Desde(asistencias).Porcentaje, "porcentaje")
        ];
    }

    private async Task<List<KpiDto>> KpisDocenteAsync(int periodoId, DateOnly hoy, CancellationToken ct)
    {
        var asignaciones = await db.Asignaciones.AsNoTracking()
            .Where(a => a.DocenteId == user.DocenteId && a.Seccion.PeriodoId == periodoId)
            .Select(a => new { a.Id, a.SeccionId, a.CursoId }).ToListAsync(ct);
        var seccionIds = asignaciones.Select(a => a.SeccionId).Distinct().ToList();
        var cursoIds = asignaciones.Select(a => a.CursoId).Distinct().ToList();

        var alumnos = await db.Matriculas.CountAsync(m => seccionIds.Contains(m.SeccionId) && m.Estado == EstadoMatricula.Activa, ct);
        var notas = await db.Notas.CountAsync(n => cursoIds.Contains(n.Competencia.CursoId) && seccionIds.Contains(n.Matricula.SeccionId), ct);
        var asistenciaHoy = await db.Asistencias.CountAsync(a => a.Fecha == hoy && seccionIds.Contains(a.Matricula.SeccionId), ct);

        return
        [
            new("cursos", "Cursos a cargo", asignaciones.Count, "numero"),
            new("secciones", "Secciones", seccionIds.Count, "numero"),
            new("alumnos", "Alumnos a cargo", alumnos, "numero"),
            new("notas", "Notas registradas", notas, "numero"),
            new("asistenciaHoy", "Asistencias tomadas hoy", asistenciaHoy, "numero")
        ];
    }

    private async Task<List<KpiDto>> KpisApoderadoAsync(int periodoId, DateOnly hoy, CancellationToken ct)
    {
        var matriculaIds = await db.Matriculas
            .Where(m => m.Alumno.ApoderadoId == user.ApoderadoId && m.PeriodoId == periodoId)
            .Select(m => m.Id).ToListAsync(ct);

        var pendientes = await db.Cuotas.AsNoTracking()
            .Where(c => matriculaIds.Contains(c.MatriculaId) && c.FechaPago == null)
            .Select(c => new { c.Monto, c.FechaVencimiento }).ToListAsync(ct);
        var vencidas = pendientes.Where(c => c.FechaVencimiento < hoy).ToList();
        var mora = vencidas.Sum(c => CalculadoraMora.Calcular(c.Monto, c.FechaVencimiento, hoy, politica));

        var estados = await db.Asistencias.Where(a => matriculaIds.Contains(a.MatriculaId)).Select(a => a.Estado).ToListAsync(ct);

        return
        [
            new("hijos", "Hijos matriculados", matriculaIds.Count, "numero"),
            new("vencido", "Deuda vencida", vencidas.Sum(c => c.Monto) + mora, "moneda", $"Incluye mora de S/ {mora:0.00}"),
            new("pendiente", "Cuotas por vencer", pendientes.Count - vencidas.Count, "numero"),
            new("asistencia", "Asistencia acumulada", ResumenAsistencia.Desde(estados).Porcentaje, "porcentaje")
        ];
    }
}
