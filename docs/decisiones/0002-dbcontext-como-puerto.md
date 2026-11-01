# ADR 0002 — `IAppDbContext` como puerto en lugar de repositorios genéricos

- **Estado:** aceptada
- **Fecha:** 2026-11

## Contexto

En Clean Architecture la capa de aplicación no debe depender de la infraestructura. La opción "de libro" es definir repositorios (`IAlumnoRepository`, `IMatriculaRepository`…) para cada agregado. En la práctica, con EF Core esto suele producir repositorios que solo reenvían llamadas a `DbSet`, duplican métodos de consulta y dificultan proyecciones eficientes (paginación, filtros dinámicos, `Select` a DTO).

## Decisión

Application define la interfaz `IAppDbContext`, que expone `DbSet<T>` y `SaveChangesAsync`. Application referencia solo el paquete base `Microsoft.EntityFrameworkCore` (abstracciones de LINQ y `DbSet`), **nunca** un proveedor (Npgsql, SQLite). Infrastructure implementa la interfaz con `AppDbContext`.

## Consecuencias

- Los casos de uso escriben consultas LINQ expresivas con proyección directa a DTOs y paginación en la BD.
- El proveedor es intercambiable por configuración: PostgreSQL en producción, SQLite en memoria en las pruebas de integración.
- Application queda acoplada a EF Core como tecnología de consulta. Es un compromiso consciente: cambiar de ORM es improbable en este proyecto, y el costo de abstraerlo sería mayor que el beneficio.
- Las reglas de negocio importantes no viven en consultas sino en Domain (calculadoras y métodos de entidad), que sigue sin dependencias y se prueba de forma aislada.
