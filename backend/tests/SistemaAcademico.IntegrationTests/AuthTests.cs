using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SistemaAcademico.Application.Features.Auth;

namespace SistemaAcademico.IntegrationTests;

[Collection(ApiCollection.Nombre)]
public class AuthTests(ApiFactory factory)
{
    [Fact]
    public async Task Health_responde_200()
    {
        var resp = await factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task OpenApi_esta_publicado()
    {
        var resp = await factory.CreateClient().GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("/api/matriculas", await resp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_valido_devuelve_token_y_rol()
    {
        var resp = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest("ADMIN@demo.pe", "Demo123!"));
        resp.EnsureSuccessStatusCode();
        var login = await resp.Content.ReadFromJsonAsync<LoginResponse>(ApiFactory.Json);

        Assert.False(string.IsNullOrWhiteSpace(login!.Token));
        Assert.Equal("Admin", login.Usuario.Rol);
    }

    [Fact]
    public async Task Login_invalido_devuelve_401_con_problem_details()
    {
        var resp = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@demo.pe", "incorrecta"));

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        Assert.Equal("application/problem+json", resp.Content.Headers.ContentType?.MediaType);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(401, doc.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Login_con_email_mal_formado_devuelve_400_con_errores_por_campo()
    {
        var resp = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest("no-es-email", ""));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var errors = doc.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("email", out _));
        Assert.True(errors.TryGetProperty("password", out _));
    }

    [Fact]
    public async Task Endpoint_protegido_sin_token_devuelve_401()
    {
        var resp = await factory.CreateClient().GetAsync("/api/alumnos");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Token_manipulado_es_rechazado()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIiwicm9sZSI6IkFkbWluIn0.firma-falsa");
        var resp = await client.GetAsync("/api/alumnos");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Me_devuelve_el_usuario_del_token()
    {
        var client = await factory.ClienteAsync(Usuarios.Apoderado);
        var me = await client.GetFromJsonAsync<UsuarioDto>("/api/auth/me", ApiFactory.Json);
        Assert.Equal("Apoderado", me!.Rol);
        Assert.NotNull(me.ApoderadoId);
    }

    [Theory]
    [InlineData(Usuarios.Docente)]
    [InlineData(Usuarios.Apoderado)]
    public async Task Solo_admin_gestiona_usuarios(string email)
    {
        var client = await factory.ClienteAsync(email);
        var resp = await client.GetAsync("/api/usuarios");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }
}
