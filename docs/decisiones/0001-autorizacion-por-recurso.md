# ADR 0001 — Autorización por recurso en la capa de aplicación

- **Estado:** aceptada
- **Fecha:** 2026-11

## Contexto

Los roles no bastan para proteger los datos: un apoderado tiene permiso para "ver libretas", pero solo las de **sus** hijos; un docente puede "registrar notas", pero solo en **sus** cursos y secciones. Ocultar botones en el frontend no protege nada: cualquiera puede llamar a la API con Postman cambiando un id.

## Decisión

Aplicar la autorización en dos niveles, ambos en el backend:

1. **Por rol, en el endpoint** (políticas `Admin`, `AdminODocente`, `Autenticado`): filtro grueso y declarativo.
2. **Por recurso, en Application** mediante un servicio único `ControlAcceso`:
   - `FiltrarMatriculas` / `FiltrarAlumnos`: los listados ya salen filtrados según el usuario (un apoderado que pide `?matriculaId=` de otro alumno recibe una lista vacía).
   - `AsegurarAccesoMatriculaAsync`, `AsegurarAsignacionPropiaAsync`, `AsegurarAccesoSeccionAsync`: lanzan `ForbiddenException` (403) o `NotFoundException` (404).
   - Además, los guardados en bloque verifican que **cada** fila del payload pertenezca al recurso autorizado (un docente no puede "colar" una matrícula de otra sección en su registro de notas).

Los ids de docente y apoderado viajan como claims del JWT para no consultar la tabla de usuarios en cada petición.

## Consecuencias

- Las reglas están en un solo lugar y se prueban con pruebas de integración (`AutorizacionPorRecursoTests`).
- Se distingue 404 (no existe) de 403 (existe pero no es tuyo). Revela la existencia del id, lo que se considera aceptable en este dominio interno; si se requiriera, bastaría con responder 404 en ambos casos.
- Si se desvincula a un docente de un curso, el cambio aplica de inmediato (se consulta la tabla de asignaciones), aunque su token siga vigente.
