# Guía de la API

Base URL local: `http://localhost:5080` (Docker o `dotnet run`). Documentación interactiva en **`/scalar/v1`** y especificación en **`/openapi/v1.json`**.

Todas las respuestas de error usan **ProblemDetails** (`application/problem+json`):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Datos de entrada inválidos",
  "status": 400,
  "detail": "Revise los campos indicados en 'errors'.",
  "instance": "/api/alumnos",
  "traceId": "0HN…",
  "errors": { "dni": ["El DNI debe tener 8 dígitos."] }
}
```

> Los ejemplos usan sintaxis bash. En PowerShell reemplaza `\` por `` ` `` al final de línea o usa `curl.exe` en una sola línea.

## 1. Autenticación

```bash
curl -s -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@demo.pe","password":"Demo123!"}'
```

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs…",
  "expiraEn": "2026-11-01T22:00:00Z",
  "usuario": { "id": 1, "email": "admin@demo.pe", "nombreCompleto": "Administración Demo", "rol": "Admin", "docenteId": null, "apoderadoId": null }
}
```

Guarda el token para las siguientes llamadas:

```bash
TOKEN=$(curl -s -X POST http://localhost:5080/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"admin@demo.pe","password":"Demo123!"}' | jq -r .token)

curl -s http://localhost:5080/api/auth/me -H "Authorization: Bearer $TOKEN"
```

## 2. Listados con paginación, búsqueda y orden

Parámetros comunes: `page` (desde 1), `pageSize` (máx. 100), `search`, `sortBy`, `sortDir` (`asc`|`desc`).

```bash
curl -s "http://localhost:5080/api/alumnos?search=mendoza&sortBy=apellidos&sortDir=asc&page=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

```json
{
  "items": [ { "id": 1, "codigo": "ALU-000001", "dni": "70000001", "nombres": "Diego Alonso", "apellidos": "Mendoza Flores", "seccionActual": "1° Secundaria A", "activo": true, "…": "…" } ],
  "page": 1, "pageSize": 10, "totalCount": 2, "totalPages": 1
}
```

Columnas ordenables de alumnos: `apellidos`, `nombres`, `codigo`, `dni`, `fechaNacimiento`. Un `sortBy` desconocido se ignora (lista blanca).

## 3. Registrar un alumno y matricularlo

```bash
# Alta del alumno (Admin)
curl -s -X POST http://localhost:5080/api/alumnos -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"dni":"71234567","nombres":"Lucía","apellidos":"Paredes Soto","fechaNacimiento":"2013-04-20","apoderadoId":1}'

# Secciones con vacantes
curl -s "http://localhost:5080/api/secciones" -H "Authorization: Bearer $TOKEN"

# Matrícula: valida vacantes y genera el cronograma de pensiones
curl -s -X POST http://localhost:5080/api/matriculas -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"alumnoId":15,"seccionId":1}'
```

```json
{
  "matricula": { "id": 15, "alumno": "Paredes Soto, Lucía", "seccion": "1° Secundaria A", "estado": "Activa", "…": "…" },
  "cuotasGeneradas": 2,
  "totalCronograma": 700.00
}
```

Si la sección está llena (en el seed, **3° Secundaria A** tiene capacidad 2):

```json
{ "status": 422, "title": "Regla de negocio incumplida", "detail": "La sección no tiene vacantes disponibles (2/2)." }
```

Retirar (libera la vacante):

```bash
curl -s -X POST http://localhost:5080/api/matriculas/15/retiro -H "Authorization: Bearer $TOKEN"
```

## 4. Configurar pesos de competencias (promedio ponderado)

```bash
curl -s http://localhost:5080/api/cursos -H "Authorization: Bearer $TOKEN"

curl -s -X PUT http://localhost:5080/api/cursos/1/competencias -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"competencias":[
        {"id":1,"nombre":"Resuelve problemas de cantidad","peso":2},
        {"id":2,"nombre":"Resuelve problemas de regularidad, equivalencia y cambio","peso":1},
        {"id":3,"nombre":"Resuelve problemas de forma, movimiento y localización","peso":1},
        {"id":4,"nombre":"Resuelve problemas de gestión de datos e incertidumbre","peso":1}]}'
```

## 5. Notas (docente)

```bash
DOC=$(curl -s -X POST http://localhost:5080/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"docente@demo.pe","password":"Demo123!"}' | jq -r .token)

# Mis asignaciones (curso + sección)
curl -s http://localhost:5080/api/asignaciones -H "Authorization: Bearer $DOC"

# Registro auxiliar del bimestre 4
curl -s "http://localhost:5080/api/notas?asignacionId=1&bimestre=4" -H "Authorization: Bearer $DOC"

# Guardar notas en bloque (valor null borra la nota)
curl -s -X PUT http://localhost:5080/api/notas -H "Authorization: Bearer $DOC" -H "Content-Type: application/json" \
  -d '{"asignacionId":1,"bimestre":4,"notas":[
        {"matriculaId":1,"competenciaId":1,"valor":16},
        {"matriculaId":1,"competenciaId":2,"valor":10},
        {"matriculaId":1,"competenciaId":3,"valor":12},
        {"matriculaId":1,"competenciaId":4,"valor":9}]}'
```

Cada fila devuelve `promedioExacto` (12.6 = (16×2+10+12+9)/5), `promedio` redondeado (13), `literal` (B) y `aprobado`.

Si el docente intenta usar una asignación de otro docente → **403**. Si envía una nota de 25 → **400**.

## 6. Asistencia

```bash
curl -s "http://localhost:5080/api/asistencias?seccionId=1&fecha=2026-10-15" -H "Authorization: Bearer $DOC"

curl -s -X PUT http://localhost:5080/api/asistencias -H "Authorization: Bearer $DOC" -H "Content-Type: application/json" \
  -d '{"seccionId":1,"fecha":"2026-10-15","registros":[
        {"matriculaId":1,"estado":"Presente","observacion":null},
        {"matriculaId":3,"estado":"Tarde","observacion":"Llegó 8:15"}]}'

curl -s http://localhost:5080/api/asistencias/resumen/1 -H "Authorization: Bearer $DOC"
```

Estados válidos: `Presente`, `Tarde`, `Falta`, `FaltaJustificada`. No se aceptan fechas futuras.

## 7. Pensiones y pagos

```bash
# Cuotas vencidas con la mora calculada a hoy
curl -s "http://localhost:5080/api/pensiones?estado=Vencida&sortBy=vencimiento" -H "Authorization: Bearer $TOKEN"

# Registrar pago (Admin). La mora se calcula a la fecha de pago y queda congelada.
curl -s -X POST http://localhost:5080/api/pensiones/42/pago -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"medioPago":"Transferencia","numeroOperacion":"OP-000123","fechaPago":null}'
```

```json
{
  "cuota": { "id": 42, "concepto": "Pensión Setiembre 2026", "monto": 350.00, "mora": 11.20, "estado": "Pagada", "…": "…" },
  "montoCobrado": 361.20
}
```

Medios de pago: `Efectivo`, `Transferencia`, `Tarjeta`, `Yape` (los no presenciales exigen `numeroOperacion`).

## 8. Libreta y dashboard

```bash
# Apoderado: solo puede ver las libretas de sus hijos
APO=$(curl -s -X POST http://localhost:5080/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"apoderado@demo.pe","password":"Demo123!"}' | jq -r .token)

curl -s "http://localhost:5080/api/matriculas" -H "Authorization: Bearer $APO"   # solo sus hijos
curl -s http://localhost:5080/api/libretas/1 -H "Authorization: Bearer $APO"     # 200
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5080/api/libretas/5 -H "Authorization: Bearer $APO"  # 403

curl -s http://localhost:5080/api/dashboard -H "Authorization: Bearer $APO"
```

## 9. Salud

```bash
curl -s http://localhost:5080/health   # Healthy (incluye conexión a la base de datos)
```

> Los ids de los ejemplos dependen del seed; consúltalos primero con los endpoints de listado.
