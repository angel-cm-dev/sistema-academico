using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SistemaAcademico.Infrastructure.Persistence;

/// <summary>
/// Usada solo por las herramientas de EF (dotnet ef migrations add ...).
/// Las migraciones se generan para PostgreSQL, el proveedor de producción.
/// La cadena es local y ficticia: no se usa en tiempo de ejecución.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=sistema_academico;Username=postgres;Password=postgres")
            .Options;
        return new AppDbContext(options);
    }
}
