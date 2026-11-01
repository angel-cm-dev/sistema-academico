using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaAcademico.Application.Abstractions;
using SistemaAcademico.Domain;
using SistemaAcademico.Domain.Entities;
using SistemaAcademico.Domain.Reglas;

namespace SistemaAcademico.Infrastructure.Persistence;

/// <summary>
/// Carga datos FICTICIOS de demostración. Todos los nombres, DNI y teléfonos son inventados.
/// Es idempotente: si ya hay usuarios, no hace nada.
/// Las matrículas y pagos se crean usando las mismas reglas de dominio que la API,
/// así los datos demo siempre son coherentes (cupos, cronogramas, moras).
/// </summary>
public sealed class DbSeeder(AppDbContext db, IPasswordHasher hasher, IClock clock, ILogger<DbSeeder> logger)
{
    public const string PasswordDemo = "Demo123!";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.Usuarios.AnyAsync(ct))
        {
            logger.LogInformation("La base de datos ya tiene datos; se omite el seed.");
            return;
        }

        var hoy = clock.Hoy;
        var anio = hoy.Year;
        var random = new Random(2026); // semilla fija → datos reproducibles

        var periodo = new PeriodoAcademico
        {
            Anio = anio, Nombre = $"Año escolar {anio}", FechaInicio = new DateOnly(anio, 3, 2),
            FechaFin = new DateOnly(anio, 12, 18), PensionMensual = 350m, Activo = true
        };
        db.Periodos.Add(periodo);

        // ---------- Grados, secciones y cursos ----------
        var grados = Enumerable.Range(1, 3)
            .Select(i => new Grado { Nombre = $"{i}° Secundaria", Nivel = NivelEducativo.Secundaria, Orden = i })
            .ToList();
        db.Grados.AddRange(grados);

        var secciones = new List<Seccion>
        {
            new() { Grado = grados[0], Periodo = periodo, Letra = "A", Capacidad = 30 },
            new() { Grado = grados[0], Periodo = periodo, Letra = "B", Capacidad = 30 },
            new() { Grado = grados[1], Periodo = periodo, Letra = "A", Capacidad = 30 },
            // Sección con aforo mínimo para demostrar la validación de vacantes.
            new() { Grado = grados[2], Periodo = periodo, Letra = "A", Capacidad = 2 }
        };
        db.Secciones.AddRange(secciones);

        var cursos = new List<Curso>();
        foreach (var g in grados)
        {
            cursos.Add(new Curso
            {
                Codigo = $"MAT{g.Orden}", Nombre = "Matemática", Grado = g,
                Competencias =
                [
                    // Peso doble: el colegio prioriza la resolución de problemas de cantidad.
                    new() { Nombre = "Resuelve problemas de cantidad", Peso = 2, Orden = 1 },
                    new() { Nombre = "Resuelve problemas de regularidad, equivalencia y cambio", Peso = 1, Orden = 2 },
                    new() { Nombre = "Resuelve problemas de forma, movimiento y localización", Peso = 1, Orden = 3 },
                    new() { Nombre = "Resuelve problemas de gestión de datos e incertidumbre", Peso = 1, Orden = 4 }
                ]
            });
            cursos.Add(new Curso
            {
                Codigo = $"COM{g.Orden}", Nombre = "Comunicación", Grado = g,
                Competencias =
                [
                    new() { Nombre = "Se comunica oralmente en su lengua materna", Peso = 1, Orden = 1 },
                    new() { Nombre = "Lee diversos tipos de textos escritos", Peso = 1, Orden = 2 },
                    new() { Nombre = "Escribe diversos tipos de textos", Peso = 1, Orden = 3 }
                ]
            });
            cursos.Add(new Curso
            {
                Codigo = $"CYT{g.Orden}", Nombre = "Ciencia y Tecnología", Grado = g,
                Competencias =
                [
                    new() { Nombre = "Indaga mediante métodos científicos", Peso = 1, Orden = 1 },
                    new() { Nombre = "Explica el mundo físico basándose en conocimientos científicos", Peso = 1, Orden = 2 },
                    new() { Nombre = "Diseña y construye soluciones tecnológicas", Peso = 1, Orden = 3 }
                ]
            });
        }
        db.Cursos.AddRange(cursos);

        // ---------- Docentes y asignaciones ----------
        var dMat = new Docente { Dni = "40000001", Nombres = "Rosa Elena", Apellidos = "Quispe Huamán", Especialidad = "Matemática", Email = "docente@demo.pe" };
        var dCom = new Docente { Dni = "40000002", Nombres = "Jorge Luis", Apellidos = "Salazar Ríos", Especialidad = "Comunicación", Email = "docente2@demo.pe" };
        var dCyt = new Docente { Dni = "40000003", Nombres = "Lucía", Apellidos = "Torres Paredes", Especialidad = "Ciencia y Tecnología" };
        db.Docentes.AddRange(dMat, dCom, dCyt);

        foreach (var s in secciones)
        {
            foreach (var c in cursos.Where(c => c.Grado == s.Grado))
            {
                var docente = c.Nombre switch { "Matemática" => dMat, "Comunicación" => dCom, _ => dCyt };
                db.Asignaciones.Add(new AsignacionDocente { Docente = docente, Curso = c, Seccion = s });
            }
        }

        // ---------- Apoderados y alumnos (datos inventados) ----------
        var apoderados = new[]
        {
            new Apoderado { Dni = "10000001", Nombres = "Carlos Alberto", Apellidos = "Mendoza Vargas", Telefono = "900000001", Email = "apoderado@demo.pe" },
            new Apoderado { Dni = "10000002", Nombres = "María Isabel", Apellidos = "Flores Castillo", Telefono = "900000002" },
            new Apoderado { Dni = "10000003", Nombres = "Pedro Pablo", Apellidos = "Ramírez Chávez", Telefono = "900000003" },
            new Apoderado { Dni = "10000004", Nombres = "Ana Lucía", Apellidos = "Gutiérrez Rojas", Telefono = "900000004" },
            new Apoderado { Dni = "10000005", Nombres = "Luis Miguel", Apellidos = "Sánchez Díaz", Telefono = "900000005" },
            new Apoderado { Dni = "10000006", Nombres = "Carmen Rosa", Apellidos = "Herrera Medina", Telefono = "900000006" }
        };
        db.Apoderados.AddRange(apoderados);

        // (nombres, apellidos, índice apoderado, índice sección)
        var datosAlumnos = new (string N, string A, int Apo, int Sec)[]
        {
            ("Diego Alonso", "Mendoza Flores", 0, 0), ("Valeria Sofía", "Mendoza Flores", 0, 2),
            ("Mateo Andrés", "Flores Castillo", 1, 0), ("Camila Fernanda", "Ramírez León", 2, 0),
            ("Sebastián", "Ramírez León", 2, 1), ("Luciana", "Gutiérrez Rojas", 3, 0),
            ("Thiago Gabriel", "Sánchez Paz", 4, 0), ("Ximena", "Sánchez Paz", 4, 1),
            ("Adrián", "Herrera Medina", 5, 1), ("Fabiana", "Herrera Medina", 5, 2),
            ("Joaquín", "Gutiérrez Rojas", 3, 2), ("Renata", "Flores Castillo", 1, 1),
            ("Santiago", "Ramírez León", 2, 3), ("Isabella", "Sánchez Paz", 4, 3)
        };

        var alumnos = new List<(Alumno Alumno, Seccion Seccion)>();
        for (var i = 0; i < datosAlumnos.Length; i++)
        {
            var d = datosAlumnos[i];
            var seccion = secciones[d.Sec];
            var alumno = new Alumno
            {
                Codigo = $"ALU-{i + 1:D6}", Dni = $"7{i + 1:D7}", Nombres = d.N, Apellidos = d.A,
                FechaNacimiento = new DateOnly(anio - 12 - seccion.Grado.Orden, 1 + i % 12, 1 + i), Apoderado = apoderados[d.Apo]
            };
            db.Alumnos.Add(alumno);
            alumnos.Add((alumno, seccion));
        }

        // ---------- Matrículas con cronograma (mismas reglas que la API) ----------
        var matriculas = new List<Matricula>();
        foreach (var (alumno, seccion) in alumnos)
        {
            seccion.OcuparVacante();
            var fecha = periodo.FechaInicio.AddDays(-20);
            var m = new Matricula { Alumno = alumno, Seccion = seccion, Periodo = periodo, FechaMatricula = fecha };
            m.Cuotas.AddRange(CronogramaPensiones.Generar(anio, periodo.PensionMensual, fecha)
                .Select(c => new Cuota { Numero = c.Numero, Concepto = c.Concepto, Monto = c.Monto, FechaVencimiento = c.FechaVencimiento }));
            db.Matriculas.Add(m);
            matriculas.Add(m);
        }

        // ---------- Pagos: la mayoría al día, algunos con atraso o deuda ----------
        for (var i = 0; i < matriculas.Count; i++)
        {
            foreach (var cuota in matriculas[i].Cuotas.Where(c => c.FechaVencimiento < hoy))
            {
                var esUltimaVencida = cuota.FechaVencimiento.AddMonths(2) >= hoy;
                if (i % 4 == 3 && esUltimaVencida) continue; // 1 de cada 4 alumnos debe las últimas cuotas
                var atraso = i % 5 == 0 ? 10 : -3;              // algunos pagan 10 días tarde (genera mora)
                var fechaPago = cuota.FechaVencimiento.AddDays(atraso);
                if (fechaPago > hoy) continue;
                cuota.RegistrarPago(fechaPago, i % 2 == 0 ? MedioPago.Transferencia : MedioPago.Yape,
                    $"OP-{anio}{cuota.Numero:D2}{i:D3}", PoliticaMora.PorDefecto);
            }
        }

        // ---------- Notas (bimestres transcurridos) ----------
        var bimestresCerrados = hoy.Month switch { >= 11 => 3, >= 8 => 2, >= 5 => 1, _ => 0 };
        foreach (var m in matriculas)
        {
            var nivel = random.Next(9, 17); // "talento base" del alumno para que las notas sean verosímiles
            foreach (var curso in cursos.Where(c => c.Grado == m.Seccion.Grado))
            foreach (var comp in curso.Competencias)
            for (var b = 1; b <= bimestresCerrados; b++)
            {
                var valor = Math.Clamp(nivel + random.Next(-3, 5), 5, 20);
                var nota = new Nota { Matricula = m, Competencia = comp, Bimestre = b };
                nota.Asignar(valor, 1, clock.UtcNow);
                db.Notas.Add(nota);
            }
        }

        // ---------- Asistencia de los últimos 10 días hábiles ----------
        var dia = hoy.AddDays(-1);
        var diasRegistrados = 0;
        while (diasRegistrados < 10 && dia >= periodo.FechaInicio)
        {
            if (dia.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday && dia <= periodo.FechaFin)
            {
                foreach (var m in matriculas)
                {
                    var r = random.Next(100);
                    var estado = r < 85 ? EstadoAsistencia.Presente : r < 93 ? EstadoAsistencia.Tarde
                        : r < 97 ? EstadoAsistencia.Falta : EstadoAsistencia.FaltaJustificada;
                    db.Asistencias.Add(new Asistencia { Matricula = m, Fecha = dia, Estado = estado, RegistradoPorUsuarioId = 1 });
                }
                diasRegistrados++;
            }
            dia = dia.AddDays(-1);
        }

        // ---------- Usuarios demo ----------
        var hash = hasher.Hash(PasswordDemo);
        db.Usuarios.AddRange(
            new Usuario { Email = "admin@demo.pe", NombreCompleto = "Administración Demo", Rol = Rol.Admin, PasswordHash = hash, CreadoEn = clock.UtcNow },
            new Usuario { Email = "docente@demo.pe", NombreCompleto = "Rosa Elena Quispe Huamán", Rol = Rol.Docente, Docente = dMat, PasswordHash = hash, CreadoEn = clock.UtcNow },
            new Usuario { Email = "docente2@demo.pe", NombreCompleto = "Jorge Luis Salazar Ríos", Rol = Rol.Docente, Docente = dCom, PasswordHash = hash, CreadoEn = clock.UtcNow },
            new Usuario { Email = "apoderado@demo.pe", NombreCompleto = "Carlos Alberto Mendoza Vargas", Rol = Rol.Apoderado, Apoderado = apoderados[0], PasswordHash = hash, CreadoEn = clock.UtcNow },
            new Usuario { Email = "apoderado2@demo.pe", NombreCompleto = "María Isabel Flores Castillo", Rol = Rol.Apoderado, Apoderado = apoderados[1], PasswordHash = hash, CreadoEn = clock.UtcNow });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seed demo completado: {Alumnos} alumnos, {Matriculas} matrículas.", alumnos.Count, matriculas.Count);
    }
}
