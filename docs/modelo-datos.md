# Modelo de datos

Esquema relacional en PostgreSQL generado por la migración `Inicial` de EF Core (`backend/src/SistemaAcademico.Infrastructure/Persistence/Migrations`).

```mermaid
erDiagram
    USUARIOS ||--o| DOCENTES : "vincula"
    USUARIOS ||--o| APODERADOS : "vincula"
    APODERADOS ||--o{ ALUMNOS : "es responsable de"
    PERIODOS ||--o{ SECCIONES : "tiene"
    GRADOS ||--o{ SECCIONES : "se divide en"
    GRADOS ||--o{ CURSOS : "tiene"
    CURSOS ||--o{ COMPETENCIAS : "evalúa"
    DOCENTES ||--o{ ASIGNACIONES_DOCENTES : "dicta"
    CURSOS ||--o{ ASIGNACIONES_DOCENTES : ""
    SECCIONES ||--o{ ASIGNACIONES_DOCENTES : ""
    ALUMNOS ||--o{ MATRICULAS : "se matricula"
    SECCIONES ||--o{ MATRICULAS : "recibe"
    PERIODOS ||--o{ MATRICULAS : ""
    MATRICULAS ||--o{ NOTAS : "obtiene"
    COMPETENCIAS ||--o{ NOTAS : ""
    MATRICULAS ||--o{ ASISTENCIAS : "registra"
    MATRICULAS ||--o{ CUOTAS : "genera"

    USUARIOS {
        int Id PK
        string Email UK
        string PasswordHash
        string NombreCompleto
        string Rol "Admin | Docente | Apoderado"
        bool Activo
        int DocenteId FK "nullable"
        int ApoderadoId FK "nullable"
    }
    APODERADOS {
        int Id PK
        char8 Dni UK
        string Nombres
        string Apellidos
        string Telefono
        string Email
    }
    ALUMNOS {
        int Id PK
        string Codigo UK "ALU-000123"
        char8 Dni UK
        string Nombres
        string Apellidos
        date FechaNacimiento
        int ApoderadoId FK
        bool Activo "baja lógica"
    }
    DOCENTES {
        int Id PK
        char8 Dni UK
        string Nombres
        string Apellidos
        string Especialidad
    }
    PERIODOS {
        int Id PK
        int Anio UK
        date FechaInicio
        date FechaFin
        numeric PensionMensual
        bool Activo
    }
    GRADOS {
        int Id PK
        string Nombre
        string Nivel
        int Orden
    }
    SECCIONES {
        int Id PK
        int GradoId FK
        int PeriodoId FK
        string Letra
        int Capacidad
        int Matriculados "CHECK <= Capacidad"
        uuid Version "token de concurrencia"
    }
    CURSOS {
        int Id PK
        string Codigo UK
        string Nombre
        int GradoId FK
    }
    COMPETENCIAS {
        int Id PK
        int CursoId FK
        string Nombre
        numeric Peso "configurable"
        int Orden
    }
    ASIGNACIONES_DOCENTES {
        int Id PK
        int DocenteId FK
        int CursoId FK "UK con SeccionId"
        int SeccionId FK
    }
    MATRICULAS {
        int Id PK
        int AlumnoId FK "UK con PeriodoId"
        int SeccionId FK
        int PeriodoId FK
        date FechaMatricula
        string Estado "Activa | Retirada"
        date FechaRetiro
    }
    NOTAS {
        int Id PK
        int MatriculaId FK
        int CompetenciaId FK
        int Bimestre "CHECK 1-4"
        numeric Valor "CHECK 0-20"
        int RegistradoPorUsuarioId
        timestamptz ActualizadoEn
    }
    ASISTENCIAS {
        int Id PK
        int MatriculaId FK "UK con Fecha"
        date Fecha
        string Estado
        string Observacion
    }
    CUOTAS {
        int Id PK
        int MatriculaId FK "UK con Numero"
        int Numero
        string Concepto
        numeric Monto
        date FechaVencimiento
        date FechaPago "null = no pagada"
        numeric MoraPagada "congelada al pagar"
        string MedioPago
        string NumeroOperacion
    }
```

## Tablas

| Tabla | Propósito | Restricciones clave |
|---|---|---|
| `usuarios` | Cuentas de acceso. Un usuario docente/apoderado se vincula a su persona. | `Email` único. |
| `apoderados` | Padres o tutores responsables. | `Dni` único (8 dígitos). |
| `alumnos` | Estudiantes. Nunca se borran: baja lógica con `Activo`. | `Dni` y `Codigo` únicos; FK a apoderado con `RESTRICT`. |
| `docentes` | Plana docente. | `Dni` único. |
| `periodos` | Año escolar; define la pensión mensual. Solo uno activo. | `Anio` único. |
| `grados` | 1° Secundaria, 2° Secundaria… | — |
| `secciones` | Aula de un grado en un periodo, con capacidad. | Único (`GradoId`, `PeriodoId`, `Letra`); `CHECK Matriculados <= Capacidad`; `Version` como token de concurrencia. |
| `cursos` | Áreas curriculares por grado. | `Codigo` único. |
| `competencias` | Competencias evaluables de un curso con su **peso**. | Cascada al borrar el curso. |
| `asignaciones_docentes` | Docente que dicta un curso en una sección. Base de la autorización del docente. | Único (`CursoId`, `SeccionId`): un titular por curso y sección. |
| `matriculas` | Inscripción de un alumno en una sección para un periodo. | Único (`AlumnoId`, `PeriodoId`). |
| `notas` | Nota de una competencia en un bimestre. | Único (`MatriculaId`, `CompetenciaId`, `Bimestre`); `CHECK` 0-20 y bimestre 1-4. |
| `asistencias` | Asistencia diaria de una matrícula. | Único (`MatriculaId`, `Fecha`). |
| `cuotas` | Cronograma de pensiones de una matrícula. | Único (`MatriculaId`, `Numero`); índice por `FechaVencimiento`. |

## Notas de diseño

- **Dinero y notas con `numeric(p,s)`**: nunca `float`, para evitar errores de redondeo binario (0.1 + 0.2 ≠ 0.3). En SQLite (solo pruebas) se convierten a `REAL` porque SQLite no soporta agregados sobre decimal.
- **Estado de la cuota calculado**: solo se persiste `FechaPago`; "Vencida" o "Pendiente" dependen de la fecha de consulta. Así no hace falta un job nocturno que actualice estados.
- **`PeriodoId` redundante en `matriculas`**: permite el índice único que impide matricular dos veces a un alumno en el mismo año.
- **Restricciones en la BD además de en el código**: la validación de la aplicación da buenos mensajes; las restricciones de la BD son la última defensa ante condiciones de carrera o scripts manuales.
- **Nombres de tablas en snake_case** y columnas como las propiedades C# (convención por defecto de EF Core).
