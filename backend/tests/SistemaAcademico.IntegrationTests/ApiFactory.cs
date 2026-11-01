using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SistemaAcademico.Application.Abstractions;
using SistemaAcademico.Application.Features.Auth;

namespace SistemaAcademico.IntegrationTests;

/// <summary>Reloj fijo: los cálculos de mora y bimestres dependen de "hoy".</summary>
public sealed class RelojFijo : IClock
{
    public static readonly DateOnly Fecha = new(2026, 10, 15);
    public DateTime UtcNow => Fecha.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
    public DateOnly Hoy => Fecha;
}

/// <summary>
/// Levanta la API completa en memoria (sin Docker): SQLite en memoria compartida,
/// seed demo y reloj fijo. La conexión "keep-alive" mantiene viva la BD en memoria
/// mientras dura la colección de pruebas.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = $"Data Source=tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public ApiFactory()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("Database:Seed", "true");
        builder.UseSetting("Jwt:Key", "clave-de-pruebas-de-integracion-0123456789-abcdef");
        builder.UseSetting("RateLimiting:LoginPorMinuto", "10000");
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock, RelojFijo>();
        });
    }

    public async Task<HttpClient> ClienteAsync(string email)
    {
        var client = CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Demo123!"));
        resp.EnsureSuccessStatusCode();
        var login = await resp.Content.ReadFromJsonAsync<LoginResponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _keepAlive.Dispose();
    }
}

[CollectionDefinition(Nombre)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Nombre = "api";
}

public static class Usuarios
{
    public const string Admin = "admin@demo.pe";
    public const string Docente = "docente@demo.pe";   // Matemática
    public const string Docente2 = "docente2@demo.pe"; // Comunicación
    public const string Apoderado = "apoderado@demo.pe";
    public const string Apoderado2 = "apoderado2@demo.pe";
}
