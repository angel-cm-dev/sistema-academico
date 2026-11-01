using System.Net;
using System.Net.Http.Json;
using SistemaAcademico.Application.Common;
using SistemaAcademico.Application.Features.Estructura;
using SistemaAcademico.Application.Features.Libretas;
using SistemaAcademico.Application.Features.Matriculas;
using SistemaAcademico.Application.Features.Notas;
using SistemaAcademico.Application.Features.Pensiones;
using SistemaAcademico.Application.Features.Personas;

namespace SistemaAcademico.IntegrationTests;

/// <summary>
/// Verifica que la autorización se aplica en el backend por recurso:
/// no basta con ocultar botones en la UI.
/// </summary>
[Collection(ApiCollection.Nombre)]
public class AutorizacionPorRecursoTests(ApiFactory factory)
{
    private static async Task<T> Leer<T>(HttpResponseMessage resp)
    {
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<T>(ApiFactory.Json))!;
    }

    [Fact]
    public async Task Apoderado_solo_ve_a_sus_hijos()
    {
        var client = await factory.ClienteAsync(Usuarios.Apoderado);
        var alumnos = await Leer<PagedResult<AlumnoDto>>(await client.GetAsync("/api/alumnos?pageSize=50"));

        Assert.Equal(2, alumnos.TotalCount);
        Assert.All(alumnos.Items, a => Assert.StartsWith("Mendoza", a.Apellidos));
    }

    [Fact]
    public async Task Apoderado_no_puede_ver_libreta_de_otro_alumno()
    {
        var admin = await factory.ClienteAsync(Usuarios.Admin);
        var ajena = (await Leer<PagedResult<MatriculaDto>>(await admin.GetAsync("/api/matriculas?search=Gutiérrez&pageSize=1"))).Items[0];
        var propia = (await Leer<PagedResult<MatriculaDto>>(await admin.GetAsync("/api/matriculas?search=Diego&pageSize=1"))).Items[0];

        var apoderado = await factory.ClienteAsync(Usuarios.Apoderado);

        Assert.Equal(HttpStatusCode.Forbidden, (await apoderado.GetAsync($"/api/libretas/{ajena.Id}")).StatusCode);

        var libreta = await Leer<LibretaDto>(await apoderado.GetAsync($"/api/libretas/{propia.Id}"));
        Assert.Equal(3, libreta.Cursos.Count);
        Assert.All(libreta.Cursos, c => Assert.NotNull(c.PromediosBimestrales[0])); // bimestre 1 cerrado en el seed
    }

    [Fact]
    public async Task Libreta_inexistente_devuelve_404()
    {
        var admin = await factory.ClienteAsync(Usuarios.Admin);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/libretas/99999")).StatusCode);
    }

    [Fact]
    public async Task Apoderado_no_ve_pensiones_ajenas_aunque_filtre_por_su_id()
    {
        var admin = await factory.ClienteAsync(Usuarios.Admin);
        var ajena = (await Leer<PagedResult<MatriculaDto>>(await admin.GetAsync("/api/matriculas?search=Herrera&pageSize=1"))).Items[0];

        var apoderado = await factory.ClienteAsync(Usuarios.Apoderado);
        var cuotas = await Leer<PagedResult<CuotaDto>>(await apoderado.GetAsync($"/api/pensiones?matriculaId={ajena.Id}"));

        Assert.Equal(0, cuotas.TotalCount);
    }

    [Fact]
    public async Task Docente_solo_lista_sus_asignaciones()
    {
        var docente = await factory.ClienteAsync(Usuarios.Docente);
        var asignaciones = await Leer<List<AsignacionDto>>(await docente.GetAsync("/api/asignaciones"));

        Assert.NotEmpty(asignaciones);
        Assert.All(asignaciones, a => Assert.Equal("Matemática", a.Curso));
    }

    [Fact]
    public async Task Docente_no_puede_ver_ni_calificar_cursos_de_otro_docente()
    {
        var admin = await factory.ClienteAsync(Usuarios.Admin);
        var todas = await Leer<List<AsignacionDto>>(await admin.GetAsync("/api/asignaciones"));
        var comunicacion = todas.First(a => a.Curso == "Comunicación");

        var docenteMate = await factory.ClienteAsync(Usuarios.Docente);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await docenteMate.GetAsync($"/api/notas?asignacionId={comunicacion.Id}&bimestre=1")).StatusCode);

        var put = await docenteMate.PutAsJsonAsync("/api/notas",
            new GuardarNotasRequest(comunicacion.Id, 3, [new NotaInput(1, 1, 20)]));
        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
    }

    [Fact]
    public async Task Docente_registra_notas_y_obtiene_promedio_ponderado()
    {
        var docente = await factory.ClienteAsync(Usuarios.Docente);
        var asignacion = (await Leer<List<AsignacionDto>>(await docente.GetAsync("/api/asignaciones"))).First();
        var registro = await Leer<RegistroNotasDto>(await docente.GetAsync($"/api/notas?asignacionId={asignacion.Id}&bimestre=4"));
        var fila = registro.Filas[0];
        var comps = registro.Competencias; // Matemática: pesos 2,1,1,1

        var notas = new List<NotaInput>
        {
            new(fila.MatriculaId, comps[0].Id, 16), new(fila.MatriculaId, comps[1].Id, 10),
            new(fila.MatriculaId, comps[2].Id, 12), new(fila.MatriculaId, comps[3].Id, 9)
        };
        var actualizado = await Leer<RegistroNotasDto>(
            await docente.PutAsJsonAsync("/api/notas", new GuardarNotasRequest(asignacion.Id, 4, notas), ApiFactory.Json));

        var resultado = actualizado.Filas.First(f => f.MatriculaId == fila.MatriculaId);
        // (16*2 + 10 + 12 + 9) / 5 = 63/5 = 12.6 → 13
        Assert.Equal(12.6m, resultado.PromedioExacto);
        Assert.Equal(13, resultado.Promedio);
        Assert.True(resultado.Aprobado);
    }

    [Fact]
    public async Task Docente_no_puede_colar_notas_de_alumnos_de_otra_seccion()
    {
        var admin = await factory.ClienteAsync(Usuarios.Admin);
        var docente = await factory.ClienteAsync(Usuarios.Docente);
        var asignaciones = await Leer<List<AsignacionDto>>(await docente.GetAsync("/api/asignaciones"));
        var a1 = asignaciones.First();
        var otraSeccion = (await Leer<PagedResult<MatriculaDto>>(
            await admin.GetAsync($"/api/matriculas?pageSize=100"))).Items.First(m => m.SeccionId != a1.SeccionId);
        var registro = await Leer<RegistroNotasDto>(await docente.GetAsync($"/api/notas?asignacionId={a1.Id}&bimestre=4"));

        var resp = await docente.PutAsJsonAsync("/api/notas",
            new GuardarNotasRequest(a1.Id, 4, [new NotaInput(otraSeccion.Id, registro.Competencias[0].Id, 20)]));

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Nota_fuera_de_escala_devuelve_400()
    {
        var docente = await factory.ClienteAsync(Usuarios.Docente);
        var asignacion = (await Leer<List<AsignacionDto>>(await docente.GetAsync("/api/asignaciones"))).First();
        var resp = await docente.PutAsJsonAsync("/api/notas", new GuardarNotasRequest(asignacion.Id, 4, [new NotaInput(1, 1, 25)]));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Apoderado_no_puede_registrar_notas_ni_asistencia()
    {
        var apoderado = await factory.ClienteAsync(Usuarios.Apoderado);
        Assert.Equal(HttpStatusCode.Forbidden, (await apoderado.GetAsync("/api/notas?asignacionId=1&bimestre=1")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await apoderado.GetAsync("/api/asistencias?seccionId=1&fecha=2026-10-14")).StatusCode);
    }

    [Fact]
    public async Task Docente_no_toma_asistencia_en_secciones_donde_no_dicta()
    {
        var admin = await factory.ClienteAsync(Usuarios.Admin);
        var secciones = await Leer<List<SeccionDto>>(await admin.GetAsync("/api/secciones"));
        var docente = await factory.ClienteAsync(Usuarios.Docente);
        var mias = await Leer<List<SeccionDto>>(await docente.GetAsync("/api/secciones"));

        // En el seed, el docente de Matemática dicta en todas las secciones; creamos una nueva sin asignación.
        var nueva = await Leer<SeccionDto>(await admin.PostAsJsonAsync("/api/secciones",
            new CrearSeccionRequest(secciones[0].GradoId, secciones[0].PeriodoId, "Z", 25)));

        Assert.DoesNotContain(mias, s => s.Id == nueva.Id);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await docente.GetAsync($"/api/asistencias?seccionId={nueva.Id}&fecha=2026-10-14")).StatusCode);
    }
}
