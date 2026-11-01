using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SistemaAcademico.Application.Common;
using SistemaAcademico.Application.Features.Asistencias;
using SistemaAcademico.Application.Features.Auth;
using SistemaAcademico.Application.Features.Dashboard;
using SistemaAcademico.Application.Features.Estructura;
using SistemaAcademico.Application.Features.Libretas;
using SistemaAcademico.Application.Features.Matriculas;
using SistemaAcademico.Application.Features.Notas;
using SistemaAcademico.Application.Features.Pensiones;
using SistemaAcademico.Application.Features.Personas;

namespace SistemaAcademico.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra los casos de uso. Son Scoped porque dependen del DbContext y del usuario
    /// de la petición, que viven lo mismo que una petición HTTP.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>(ServiceLifetime.Scoped);

        services.AddScoped<ControlAcceso>();
        services.AddScoped<AuthService>();
        services.AddScoped<EstructuraService>();
        services.AddScoped<PersonasService>();
        services.AddScoped<MatriculaService>();
        services.AddScoped<NotaService>();
        services.AddScoped<AsistenciaService>();
        services.AddScoped<PensionService>();
        services.AddScoped<LibretaService>();
        services.AddScoped<DashboardService>();
        return services;
    }
}
