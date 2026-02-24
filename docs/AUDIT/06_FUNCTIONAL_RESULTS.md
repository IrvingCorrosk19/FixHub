# FixHub — Resultados de pruebas funcionales E2E

**Rol:** QA Lead — Pruebas funcionales E2E por API  
**Sistema:** FixHub (Clean Architecture, JWT, PostgreSQL)  
**Entorno:** Solo local / SIT / QA. **Prohibido** producción.  
**Prefijo de datos:** `TEST_<timestamp>`  
**Código:** No modificado. Sin deletes destructivos.

---

## 1. Cómo ejecutar

### 1.1 Requisitos

- API en ejecución (ej. `http://localhost:5100`).
- Usuario Admin (seed) para Login Admin: setear `adminEmail` y `adminPassword` en el environment de Postman.

### 1.2 Orden de ejecución (Postman)

1. Importar colección y environment desde `tests/FUNCTIONAL_E2E/`:
   - `FixHub-Functional-E2E.postman_collection.json`
   - `FixHub-Functional-E2E.postman_environment.json`
2. En el environment: `baseUrl` (ej. `http://localhost:5100`), `adminEmail`, `adminPassword`.
3. Ejecutar carpetas en orden: **1** (Prep) → **2** (Customer) → **3** (Technician) → **4** (Interaction) → **5** (Admin). La carpeta 4 incluye como primer request **Login Admin** (necesario para Accept Proposal); setear `adminEmail` y `adminPassword` en el environment antes de ejecutar.

Las variables `customerToken`, `technicianToken`, `adminToken`, `jobId`, `proposalId` se rellenan por los scripts de test de cada request.

### 1.3 Newman

```bash
newman run tests/FUNCTIONAL_E2E/FixHub-Functional-E2E.postman_collection.json -e tests/FUNCTIONAL_E2E/FixHub-Functional-E2E.postman_environment.json
```

La colección tiene Login Admin como primer request de la carpeta 4, por lo que el orden 1 → 2 → 3 → 4 → 5 es suficiente para Newman.

### 1.4 Limpieza

No ejecutar deletes destructivos. Para entorno local con Docker: `docker compose down -v` solo en BD de pruebas. Datos con prefijo `TEST_` pueden quedar en BD de SIT/QA; documentar política de limpieza interna.

---

## 2. Evidencia (request / response)

Valores sensibles **REDACTED**. Respuestas resumidas.

### 2.1 FASE 1 — Health

| Request | Response (esperado) | Ejemplo body (redactado) |
|---------|---------------------|---------------------------|
| GET /api/v1/health | 200 | `{"status":"healthy","version":"1.0.0","timestamp":"...","database":"connected"}` |

### 2.2 FASE 2 — Customer: otro customer no ve job (403)

| Request | Response (esperado) | Ejemplo body (redactado) |
|---------|---------------------|---------------------------|
| GET /api/v1/jobs/{{jobId}} con Bearer `customer2Token` (job es de Customer 1) | 403 | `{"title":"Forbidden","status":403,"detail":"Access denied to this job.","errorCode":"FORBIDDEN"}` |

### 2.3 FASE 3 — Proposal duplicada

| Request | Response (esperado) | Ejemplo body (redactado) |
|---------|---------------------|---------------------------|
| POST /api/v1/jobs/{{jobId}}/proposals (mismo technician, segunda vez) | 400 o 409 | `{"errorCode":"DUPLICATE_PROPOSAL"}` o equivalente |

### 2.4 FASE 5 — Customer/Technician → Admin (403)

| Request | Response (esperado) | Ejemplo body (redactado) |
|---------|---------------------|---------------------------|
| GET /api/v1/admin/dashboard con Bearer `customerToken` | 403 | 403 Forbidden |
| GET /api/v1/admin/dashboard con Bearer `technicianToken` | 403 | 403 Forbidden |

---

## 3. Tabla resumen Pass/Fail

Rellenar **Resultado obtenido** y **Pass/Fail** al ejecutar. Criterio: Pass si código HTTP y comportamiento coinciden con lo esperado.

| # | Escenario | Resultado esperado | Resultado obtenido | Pass/Fail | Notas |
|---|-----------|--------------------|--------------------|-----------|--------|
| 1.1 | GET Health | 200, healthy | | | |
| 2.1 | Register Customer | 201, token y email | | | API exige `role`; sin role → 400. |
| 2.2 | Login Customer | 200, token | | | |
| 2.3 | Create Job (TEST_JOB_<timestamp>) | 201, id | | | |
| 2.4 | List My Jobs | 200, job en items | | | Validar campos obligatorios y status Open. |
| 2.5 | Get Job by ID (owner) | 200, mismo job | | | |
| 2.6 | Get Job (other customer) | 403 | | | |
| 3.1 | Register Technician | 201 | | | |
| 3.2 | Login Technician | 200 | | | |
| 3.3 | List Jobs (Technician) | 200 | | | |
| 3.4 | Create Proposal | 201, id | | | |
| 3.5 | Proposal duplicada | 400 o 409 | | | |
| 3.6 | Proposal job inexistente | 404 o 400 | | | |
| 4.1 | Customer - List Proposals (own job) | 200, lista | | | **Bug conocido:** puede devolver vacía (H04). |
| 4.2 | Admin - Accept Proposal | 200 | | | Solo Admin puede asignar. |
| 4.3 | Technician - Start Job | 200 | | | |
| 4.4 | Customer - Complete Job | 200 | | | |
| 5.1 | Login Admin | 200 | | | |
| 5.2 | Admin - List Jobs | 200 | | | |
| 5.3 | Admin - List Proposals | 200 | | | |
| 5.4 | Customer → Admin Dashboard | 403 | | | |
| 5.5 | Technician → Admin Dashboard | 403 | | | |

---

## 4. Validación final (FASE 6)

| Verificación | Cómo validar | Resultado esperado |
|--------------|--------------|--------------------|
| No duplicados | Una sola proposal por (JobId, TechnicianId) | Constraint único; segunda proposal mismo par → 400/409. |
| Flujo de estados | Job: Open → Assigned → InProgress → Completed | Transiciones solo con las acciones correctas por rol. |
| Sin registros inconsistentes | Tras flujo completo, FKs coherentes | Assignments con proposalId y jobId existentes; no huérfanos. |

---

## 5. Lista de bugs encontrados

Basado en revisión de código y comportamiento conocido (sin ejecutar contra entorno en este documento):

| ID | Descripción | Severidad | Detalle técnico |
|----|-------------|-----------|------------------|
| B01 | Register Customer "sin enviar role" | Info | La API exige el campo `role` en el body (RegisterRequest). Si no se envía, deserialización puede dar default (0); el validator rechaza role 0 → 400. Para flujo correcto hay que enviar `role: 1` (Customer). |
| B02 | Customer no ve proposals de su job | Alto (H04) | GET /api/v1/jobs/{id}/proposals con token Customer devuelve solo propuestas donde TechnicianId == CustomerId (lista vacía). El negocio espera que el dueño del job vea todas las propuestas de ese job. |
| B03 | Solo Admin puede asignar technician | Esperado | En FixHub la asignación la hace Admin (POST proposals/{id}/accept), no el Customer. No es bug; documentado para evitar confusión con "Customer asigna". |
| B04 | Register con role=Admin aceptado | Crítico (H03) | POST register con `role: 3` (Admin) devuelve 201 y crea usuario Admin. Debe rechazarse con 400/403. |

---

## 6. Resumen

- **Flujos que deben pasar** (tras ejecución): Health, Register/Login Customer y Technician, Create Job (TEST_JOB_<timestamp>), List My Jobs, Get Job by ID, Get Job other → 403, List Jobs Technician, Create Proposal, Proposal duplicada/inexistente → 400/404, Admin Login/List/Accept, Technician Start, Customer Complete, Customer/Technician → Admin → 403.
- **Bugs conocidos:** B02 (Customer no ve proposals de su job), B04 (Register Admin permitido). B01 es limitación de contrato API (role obligatorio).
- **Entregables:** `tests/FUNCTIONAL_E2E/` (colección Postman + environment + README), `docs/AUDIT/06_FUNCTIONAL_RESULTS.md` (este documento).

---

*Documento: docs/AUDIT/06_FUNCTIONAL_RESULTS.md. Referencia: FINAL_REPORT.md, 05_E2E_EXECUTION.md.*
