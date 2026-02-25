# FixHub — Resultados de ejecución (pruebas funcionales E2E)

**Colección:** `tests/FUNCTIONAL_E2E/postman/FixHub_Functional_E2E.postman_collection.json`  
**Environment:** `tests/FUNCTIONAL_E2E/postman/FixHub_ENV.postman_environment.json`  
**Matriz:** `docs/QA/01_TEST_MATRIX.md`

---

## Estado de la ejecución

**Intentativa de ejecución (2025-02-25):** No fue posible completar la suite en el entorno de generación:

| Requisito | Estado | Nota |
|-----------|--------|------|
| **API FixHub** | No arrancó | `dotnet run` falló (hostpolicy.dll / runtime; API no expuesta). |
| **Tests de integración** (dotnet test) | Fallaron 30/30 | **Docker no está corriendo o no está configurado.** Testcontainers exige Docker para levantar PostgreSQL. Mensaje: `Docker is either not running or misconfigured`. |
| **Newman (Postman CLI)** | No disponible | `npx`/`node` no reconocidos en el PATH; no se pudo ejecutar la colección Postman. |

Cada caso se marca como **NOT EXECUTED** hasta que se ejecute en un entorno con API accesible y/o Docker + Node según la opción elegida.

Cuando se ejecuten en local/SIT/QA, actualizar esta tabla con los resultados reales (Expected, Actual, PASS/FAIL, link a evidencia).

---

## Comando para ejecutar

Desde la raíz del repo:

```bash
newman run tests/FUNCTIONAL_E2E/postman/FixHub_Functional_E2E.postman_collection.json \
  -e tests/FUNCTIONAL_E2E/postman/FixHub_ENV.postman_environment.json \
  --reporters cli,json \
  --reporter-json-export docs/QA/evidence/newman_run.json
```

**Requisitos:** API accesible en `baseUrl` (por defecto `http://localhost:5100`); environment con `adminEmail` y `adminPassword` si se usan flujos Admin. Crear carpeta `docs/QA/evidence/` si se desea exportar el JSON.

---

## Tabla resumen por TC (plantilla)

| ID | Rol | Expected | Actual | PASS/FAIL | Link evidencia |
|----|-----|----------|--------|-----------|----------------|
| TC-001 | Customer | 201, token, role Customer | — | NOT EXECUTED | — |
| TC-002 | Customer | 200, token | — | NOT EXECUTED | — |
| TC-003 | Customer | 201, job id, status Open/Assigned | — | NOT EXECUTED | — |
| TC-004 | Customer | 400 validación | — | NOT EXECUTED | — |
| TC-005 | Customer | 400 CATEGORY_NOT_FOUND | — | NOT EXECUTED | — |
| TC-006 | Customer | 200, PagedResult items | — | NOT EXECUTED | — |
| TC-007 | Customer | 200, JobDto | — | NOT EXECUTED | — |
| TC-008 | Customer | 403 FORBIDDEN | — | NOT EXECUTED | — |
| TC-009 | Customer | 200, lista propuestas (bug H04: vacía) | — | NOT EXECUTED | — |
| TC-010 | Customer | 200, Cancelled | — | NOT EXECUTED | — |
| TC-011 | Customer | 400 INVALID_STATUS | — | NOT EXECUTED | — |
| TC-012 | Customer | 200, Completed | — | NOT EXECUTED | — |
| TC-013 | Customer | 400 INVALID_STATUS | — | NOT EXECUTED | — |
| TC-014 | Customer | 201, ReviewDto | — | NOT EXECUTED | — |
| TC-015 | Customer | 400 REVIEW_EXISTS | — | NOT EXECUTED | — |
| TC-016 | Customer | 201, IssueDto | — | NOT EXECUTED | — |
| TC-017 | Customer | 200, notificaciones | — | NOT EXECUTED | — |
| TC-018 | Customer | 204 | — | NOT EXECUTED | — |
| TC-019 | Technician | 201, role Technician | — | NOT EXECUTED | — |
| TC-020 | Technician | 200, jobs Open/con propuesta | — | NOT EXECUTED | — |
| TC-021 | Technician | 200, JobDto | — | NOT EXECUTED | — |
| TC-022 | Technician | 201, ProposalDto | — | NOT EXECUTED | — |
| TC-023 | Technician | 400/409 DUPLICATE_PROPOSAL | — | NOT EXECUTED | — |
| TC-024 | Technician | 200, assignments | — | NOT EXECUTED | — |
| TC-025 | Technician | 200, InProgress | — | NOT EXECUTED | — |
| TC-026 | Technician | 403 FORBIDDEN | — | NOT EXECUTED | — |
| TC-027 | Technician | 400/403 | — | NOT EXECUTED | — |
| TC-028 | — | N/A (documental) | — | NOT EXECUTED | — |
| TC-029 | Technician | 403 | — | NOT EXECUTED | — |
| TC-030 | Admin | 400/403 (bug: 201) | — | NOT EXECUTED | — |
| TC-031 | Admin | 200, OpsDashboardDto | — | NOT EXECUTED | — |
| TC-032 | Admin | 200, PagedResult | — | NOT EXECUTED | — |
| TC-033 | Admin | 200, lista propuestas | — | NOT EXECUTED | — |
| TC-034 | Admin | 200, job Assigned | — | NOT EXECUTED | — |
| TC-035 | Admin | 200, InProgress | — | NOT EXECUTED | — |
| TC-036 | Admin | 200, Completed | — | NOT EXECUTED | — |
| TC-037 | Admin | 200, applicants | — | NOT EXECUTED | — |
| TC-038 | Admin | 204 | — | NOT EXECUTED | — |
| TC-039 | Admin | 200, issues | — | NOT EXECUTED | — |
| TC-040 | Admin | 204 | — | NOT EXECUTED | — |
| TC-041 | Admin | 200, metrics | — | NOT EXECUTED | — |
| TC-042 | Customer | 403 | — | NOT EXECUTED | — |
| TC-043 | E2E | 200/201/204 en secuencia | — | NOT EXECUTED | — |
| TC-044 | E2E | 200 create, 200 cancel | — | NOT EXECUTED | — |
| TC-045 | E2E | 403 | — | NOT EXECUTED | — |
| TC-046 | E2E | 400 INVALID_STATUS | — | NOT EXECUTED | — |
| TC-047 | — | 400 | — | NOT EXECUTED | — |
| TC-048 | — | 200/201 o 400, no 500 | — | NOT EXECUTED | — |
| TC-049 | Customer | 400 o 200 idempotente | — | NOT EXECUTED | — |
| TC-050 | — | Sin stack en body | — | NOT EXECUTED | — |
| TC-051 | — | 200, healthy | — | NOT EXECUTED | — |

---

## Evidencias mínimas por bloque (cuando se ejecute)

Para cada bloque (Auth, Customer, Admin, Technician, End2End, Negative), capturar:

1. **Request:** URL, método, body (redactar tokens y contraseñas).
2. **Response:** Status code, body (resumido o relevante; redactar token).
3. **Timestamp** de la ejecución.
4. **PASS/FAIL** según resultado esperado de la matriz.

Almacenar en `docs/QA/evidence/` (por ejemplo: `auth_TC001_request.json`, `auth_TC001_response.json`, o un único `newman_run.json` exportado por Newman).

---

## Qué falta para ejecutar en este entorno

- **API FixHub** en ejecución y accesible (ej. `dotnet run` en `src/FixHub.API` o docker-compose).
- **PostgreSQL** con migraciones aplicadas.
- **Postman** o **Newman** instalado.
- **Credenciales Admin** (seed o usuario creado) en el environment para flujos que usan Login Admin.
- (Opcional) Carpeta `docs/QA/evidence/` para exportar reporte Newman.

Una vez ejecutada la suite, reemplazar en este documento los "NOT EXECUTED" por los resultados reales y añadir enlaces a las evidencias.
