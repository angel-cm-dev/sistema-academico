# ADR 0003 — Vacantes con contador y concurrencia optimista

- **Estado:** aceptada
- **Fecha:** 2026-11

## Contexto

Durante la campaña de matrícula, dos personas pueden matricular a la vez en la **última vacante** de una sección. El patrón ingenuo "contar matrículas → si hay espacio, insertar" tiene una condición de carrera: ambas peticiones cuentan 29/30 y ambas insertan, dejando 31/30.

Alternativas evaluadas:

| Opción | Pros | Contras |
|---|---|---|
| Transacción `SERIALIZABLE` | Correcta | Específica del motor; reintentos complejos; peor rendimiento. |
| `SELECT … FOR UPDATE` sobre la sección | Correcta | SQL específico de PostgreSQL; no funciona igual en SQLite (pruebas). |
| **Contador en la sección + token de concurrencia** | Portable, simple, testeable | Requiere mantener el contador sincronizado. |

## Decisión

- `Seccion` guarda `Matriculados` y un `Version` (Guid) marcado como **token de concurrencia** en EF Core.
- `Seccion.OcuparVacante()` valida la regla de dominio (`ReglasMatricula.ValidarVacante`), incrementa el contador y cambia `Version`.
- Al guardar, EF Core emite `UPDATE secciones … WHERE Id = @id AND Version = @versionOriginal`. Si otra transacción cambió la sección, se afectan 0 filas y se lanza `DbUpdateConcurrencyException`, que la API traduce a **409 Conflict** ("otro usuario tomó la última vacante, reintente").
- Como defensa adicional, la tabla tiene `CHECK ("Matriculados" <= "Capacidad")`.
- La matrícula, el cambio de cupo y las cuotas del cronograma se guardan en un único `SaveChanges` (atómico).
- Retirar una matrícula libera la vacante con `LiberarVacante()`.

## Consecuencias

- Nunca se supera el aforo, incluso con peticiones simultáneas, sin SQL propietario.
- La sección llena se rechaza con **422** (regla de negocio) antes de intentar guardar; el **409** queda para la carrera real.
- El contador es un dato desnormalizado: toda alta/retiro debe pasar por los métodos de la entidad (no por SQL manual).
