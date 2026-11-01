using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SistemaAcademico.Application.Abstractions;
using SistemaAcademico.Domain.Reglas;
using SistemaAcademico.Infrastructure.Persistence;
using SistemaAcademico.Infrastructure.Security;

namespace SistemaAcademico.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra persistencia, seguridad y servicios técnicos.
    /// El proveedor de BD se elige por configuración (<c>Database:Provider</c>):
    /// "Postgres" en producción/Docker y "Sqlite" para pruebas o desarrollo sin Docker.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var provider = config["Database:Provider"] ?? "Postgres";
        var connectionString = config.GetConnectionString("Default")
                               ?? throw new InvalidOperationException("Falta ConnectionStrings:Default.");

        services.AddDbContext<AppDbContext>(options =>
        {
            if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(connectionString);
            else
                options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(5));
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddOptions<JwtOptions>()
            .Bind(config.GetSection(JwtOptions.Section))
            .Validate(o => o.Key.Length >= 32, "Jwt:Key debe tener al menos 32 caracteres.")
            .ValidateOnStart();

        // Política de mora configurable; si no se configura, se usa la política por defecto.
        var mora = config.GetSection("Pensiones:Mora");
        services.AddSingleton(new PoliticaMora(
            mora.GetValue("TasaDiaria", PoliticaMora.PorDefecto.TasaDiaria),
            mora.GetValue("DiasGracia", PoliticaMora.PorDefecto.DiasGracia),
            mora.GetValue("TopePorcentaje", PoliticaMora.PorDefecto.TopePorcentaje)));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<DbSeeder>();
        return services;
    }

    /// <summary>
    /// Prepara la BD al iniciar: en PostgreSQL aplica las migraciones pendientes;
    /// en SQLite crea el esquema desde el modelo (las migraciones son específicas de Npgsql).
    /// Después ejecuta el seed demo si está habilitado.
    /// </summary>
    public static async Task InicializarBaseDeDatosAsync(this IServiceProvider services, IConfiguration config)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Database");

        if (db.Database.IsSqlite())
            await db.Database.EnsureCreatedAsync();
        else
            await db.Database.MigrateAsync();
        logger.LogInformation("Esquema de base de datos listo ({Provider}).", db.Database.ProviderName);

        if (config.GetValue("Database:Seed", true))
            await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
    }
}
