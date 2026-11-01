using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SistemaAcademico.Application.Common;
using SistemaAcademico.Application.Features.Asistencias;
using SistemaAcademico.Application.Features.Dashboard;
using SistemaAcademico.Application.Features.Estructura;
using SistemaAcademico.Application.Features.Matriculas;
using SistemaAcademico.Application.Features.Pensiones;
using SistemaAcademico.Application.Features.Personas;
using SistemaAcademico.Domain;

namespace SistemaAcademico.IntegrationTests;

[Collection(ApiCollection.Nombre)]
public class FlujosNegocioTests(ApiFactory factory)
{
    private static async Task<T> Leer<T>(HttpResponseMessage resp)
    {
        if (!resp.IsSuccessStatusCode)
            throw new Xunit.Sdk.XunitException($"{(int)resp.StatusCode}: {await resp.Content.ReadAsStringAsync()}");
        return (await resp.Content.ReadFromJsonAsync<T>(ApiFactory.Json))!;
    }

    private async Task<AlumnoDto> CrearAlumnoAsync(HttpClient admin, string dni)
    {
        var apoderados = await Leer<PagedResult<ApoderadoDto>>(await admin.GetAsync("/api/apoderados"));
        return await Leer<AlumnoDto>(await admin.PostAsJsonAsync("/api/alumnos",
            new GuardarAlumnoRequest(dni, "Alumno", "De Prueba", new DateOnly(2013, 6, 15), apoderados.Items[0].Id), ApiFactory.Json));
    }

    [Fact]
    public async Task Matricula_valida_genera_cronograma_y_ocupa_vacante()
    {
        var admin = await factory.ClienteAsync(Usuarios.Admin);
        var alumno = await CrearAlumnoAsync(admin, "60000001");
        Assert.StartsWith("ALU-", alumno.Codigo);

        var seccion = (await Leer<List<SeccionDto>>(await admin.GetAsync("/api/secciones"))).First(s => s.Capacidad == 30);
        var resp = await admin.PostAsJsonAsync("/api/matriculas",
            new CrearMatriculaRequest(alumno.Id, seccion.Id, new DateOnly(2026, 5, 10)));
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var creada = await Leer<MatriculaCreadaDto>(resp);

        Assert.Equal(8, creada.CuotasGeneradas); // mayo a diciembre
        Assert.Equal(2800m, creada.TotalCronograma);

        var despues = (await Leer<List<SeccionDto>>(await admin.GetAsync("/api/secciones"))).First(s => s.Id == seccion.Id);
        Assert.Equal(seccion.Matriculados + 1, despues.Matriculados);

        // Una segunda matrícula en el mismo periodo es un conflicto.
        var duplicada = await admin.PostAsJsonAsync("/api/matriculas", new CrearMatriculaRequest(alumno.Id, seccion.Id, null));
        Assert.Equal(HttpStatusCode.Conflict, duplicada.StatusCode);
    }

    [Fact]
    public async Task Matricula_en_seccion_llena_devuelve_422()
    {
        var admin = await factory.ClienteAsync(Usuarios.Admin);
        var alumno = await CrearAlumnoAsync(admin, "60000002");
        var llena = (await Leer<List<SeccionDto>>(await admin.GetAsync("/api/secciones"))).First(s => s.Vacantes == 0);

        var resp = await admin.PostAsJsonAsync("/api/matriculas", new CrearMatriculaRequest(alumno.Id, llena.Id, null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Contains("vacantes", doc.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Retiro_libera_la_vacante()
    {
        var admin = await factory.ClienteAsync(Usuarios.Admin);
        var alumno = await CrearAlumnoAsync(admin, "60000003");
        var seccion = (await Leer<List<SeccionDto>>(await admin.GetAsync("/api/secciones"))).First(s => s.Vacantes > 5);
        var creada = await Leer<MatriculaCreadaDto>(await admin.PostAsJsonAsync("/api/matriculas",
            new CrearMatriculaRequest(alumno.Id, seccion.Id, null)));

        var retirada = await Leer<MatriculaDto>(await admin.PostAsync($"/api/matriculas/{creada.Matricula.Id}/retiro", null));

        Assert.Equal("Retirada", retirada.Estado);
        var despues = (await Leer<List<SeccionDto>>(await admin.GetAsync("/api/secciones"))).First(s => s.Id == seccion.Id);
        Assert.Equal(seccion.Matriculados, despues.Matriculados);
    }

    [Fact]
    public async Task Alumno_con_datos_invalidos_devuelve_400_con_errores()
    {
        var admin = await factory.ClienteAsync(Usuarios.Admin);
        var resp = await admin.PostAsJsonAsync("/api/alumnos",
            new GuardarAlumnoRequest("123", "", "X", new DateOnly(1990, 1, 1), 1), ApiFactory.Json);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var errors = doc.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("dni", out _));
        Assert.True(errors.TryGetProperty("nombres", out _));
        Assert.True(errors.TryGetProperty("fechaNacimiento", out _));
    }

    [Fact]
    public async Task Dni_duplicado_devuelve_409()
    {
        var admin = await factory.ClienteAsync(Usuarios.Admin);
        await CrearAlumnoAsync(admin, "60000004");
        var apoderados = await Leer<PagedResult<ApoderadoDto>>(await admin.GetAsync("/api/apoderados"));
        var resp = await admin.PostAsJsonAsync("/api/alumnos",
            new GuardarAlumnoRequest("60000004", "Otro", "Alumno", new DateOnly(2012, 1, 1), apoderados.Items[0].Id), ApiFactory.Json);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task Listado_pagina_busca_y_ordena()
    {
        var admin = await factory.ClienteAsync(Usuarios.Admin);

        var pagina = await Leer<PagedResult<AlumnoDto>>(await admin.GetAsync("/api/alumnos?page=1&pageSize=3&sortBy=apellidos&sortDir=desc"));
        Assert.Equal(3, pagina.Items.Count);
        Assert.True(pagina.TotalCount >= 14);
        Assert.Equal(pagina.Items.Select(a => a.Apellidos).OrderByDescending(x => x, StringComparer.Ordinal), pagina.Items.Select(a => a.Apellidos));

        var busqueda = await Leer<PagedResult<AlumnoDto>>(await admin.GetAsync("/api/alumnos?search=herrera"));
        Assert.Equal(2, busqueda.TotalCount);
    }

    [Fact]
    public async Task Pago_de_cuota_vencida_cobra_mora_y_solo_admin_puede_registrarlo()
    {
        var admin = await factory.ClienteAsync(Usuarios.Admin);
        var vencidas = await Leer<PagedResult<CuotaDto>>(await admin.GetAsync("/api/pensiones?estado=Vencida&pageSize=50"));
        Assert.NotEmpty(vencidas.Items);
        var cuota = vencidas.Items.First(c => c.Mora > 0);

        var apoderado = await factory.ClienteAsync(Usuarios.Apoderado);
        var intento = await apoderado.PostAsJsonAsync($"/api/pensiones/{cuota.Id}/pago",
            new RegistrarPagoRequest(null, MedioPago.Efectivo, null), ApiFactory.Json);
        Assert.Equal(HttpStatusCode.Forbidden, intento.StatusCode);

        var pago = await Leer<PagoRegistradoDto>(await admin.PostAsJsonAsync($"/api/pensiones/{cuota.Id}/pago",
            new RegistrarPagoRequest(null, MedioPago.Transferencia, "OP-TEST-1"), ApiFactory.Json));

        Assert.Equal("Pagada", pago.Cuota.Estado);
        Assert.Equal(cuota.Monto + cuota.Mora, pago.MontoCobrado);

        var doble = await admin.PostAsJsonAsync($"/api/pensiones/{cuota.Id}/pago",
            new RegistrarPagoRequest(null, MedioPago.Efectivo, null), ApiFactory.Json);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, doble.StatusCode);
    }

    [Fact]
    public async Task Configurar_pesos_de_competencias_cambia_el_promedio()
    {
        var admin = await factory.ClienteAsync(Usuarios.Admin);
        var curso = (await Leer<List<CursoDto>>(await admin.GetAsync("/api/cursos"))).First(c => c.Nombre == "Ciencia y Tecnología");
        var inputs = curso.Competencias.Select((c, i) => new CompetenciaInput(c.Id, c.Nombre, i == 0 ? 3 : 1)).ToList();

        var actualizado = await Leer<CursoDto>(await admin.PutAsJsonAsync($"/api/cursos/{curso.Id}/competencias",
            new ActualizarCompetenciasRequest(inputs)));

        Assert.Equal(3m, actualizado.Competencias[0].Peso);

        var invalido = await admin.PutAsJsonAsync($"/api/cursos/{curso.Id}/competencias",
            new ActualizarCompetenciasRequest([new CompetenciaInput(curso.Competencias[0].Id, "X", 0)]));
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
    }

    [Fact]
    public async Task Asistencia_se_registra_y_resume()
    {
        var docente = await factory.ClienteAsync(Usuarios.Docente);
        var seccion = (await Leer<List<SeccionDto>>(await docente.GetAsync("/api/secciones"))).First(s => s.Matriculados > 0);
        var registro = await Leer<RegistroAsistenciaDto>(await docente.GetAsync($"/api/asistencias?seccionId={seccion.Id}&fecha=2026-10-15"));

        var payload = new GuardarAsistenciaRequest(seccion.Id, RelojFijo.Fecha,
            registro.Filas.Select(f => new AsistenciaInput(f.MatriculaId, EstadoAsistencia.Presente, null)).ToList());
        var guardado = await Leer<RegistroAsistenciaDto>(await docente.PutAsJsonAsync("/api/asistencias", payload, ApiFactory.Json));
        Assert.All(guardado.Filas, f => Assert.Equal("Presente", f.Estado));

        var resumen = await Leer<ResumenAsistenciaDto>(await docente.GetAsync($"/api/asistencias/resumen/{registro.Filas[0].MatriculaId}"));
        Assert.True(resumen.Total >= 1);

        var futura = await docente.PutAsJsonAsync("/api/asistencias",
            payload with { Fecha = RelojFijo.Fecha.AddDays(1) }, ApiFactory.Json);
        Assert.Equal(HttpStatusCode.BadRequest, futura.StatusCode);
    }

    [Theory]
    [InlineData(Usuarios.Admin, "ocupacion")]
    [InlineData(Usuarios.Docente, "cursos")]
    [InlineData(Usuarios.Apoderado, "hijos")]
    public async Task Dashboard_muestra_kpis_segun_rol(string email, string kpiEsperado)
    {
        var client = await factory.ClienteAsync(email);
        var dashboard = await Leer<DashboardDto>(await client.GetAsync("/api/dashboard"));
        Assert.Contains(dashboard.Kpis, k => k.Clave == kpiEsperado);
        Assert.Equal(2026, dashboard.Anio);
    }
}
