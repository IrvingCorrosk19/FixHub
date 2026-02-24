# FixHub — Ejecución de pruebas E2E funcionales por API

**Rol:** QA Lead — Pruebas funcionales E2E  
**Sistema:** FixHub (API REST, JWT, PostgreSQL, Docker Compose)  
**Entorno permitido:** SOLO local / SIT / QA. **PROHIBIDO** producción o datos reales.  
**Prefijo de datos de prueba:** `AUDIT_<timestamp>` (ej. `AUDIT_1734567890`).

---

## FASE 1 — Preparación

### 1.1 Levantar entorno

```powershell
cd c:\Proyectos\FixHub
# Crear .env con: POSTGRES_DB, POSTGRES_USER, POSTGRES_PASSWORD, JWT_SECRET_KEY, WEB_ORIGIN
docker compose up -d --build
```

Esperar a que `fixhub_migrator` termine y `fixhub_api` esté en estado running. Si la API se expone en el host, mapear puerto (ej. en compose añadir para fixhub_api: `ports: ["5100:8080"]`) o usar la URL del contenedor desde otro contenedor en la misma red.

### 1.2 Verificaciones iniciales

| Verificación | Comando / Request | Resultado esperado |
|--------------|-------------------|--------------------|
| API responde | `GET {{baseUrl}}/api/v1/health` | 200, body con `"status":"healthy"`, `"database":"connected"` |
| DB accesible | Implícito en health | Si health devuelve 503, revisar fixhub_postgres y connection string |

### 1.3 Variables de entorno para pruebas

Definir en Postman (collection o environment) o en scripts:

| Variable | Descripción | Ejemplo (redactado) |
|----------|-------------|----------------------|
| `baseUrl` | URL base de la API | `http://localhost:5100` |
| `auditTimestamp` | Timestamp único por ejecución | `{{$timestamp}}` (Postman) o valor fijo por run |
| `customerToken` | JWT tras login como Customer | (se llena al ejecutar Login Customer) |
| `technicianToken` | JWT tras login como Technician | (se llena al ejecutar Login Technician) |
| `adminToken` | JWT tras login como Admin (seed) | (se llena con Login Admin; usuario seed) |
| `jobId` | ID del job creado por Customer | (se llena al crear job) |
| `proposalId` | ID de la propuesta creada por Technician | (se llena al enviar proposal o al listar) |

**Admin seed:** Email y contraseña del usuario admin creado por migración (ver documentación interna; no incluir en este doc). En Postman, setear en el environment las variables `adminEmail` y `adminPassword` para el request "Login Admin (seed)". Usar solo en local/SIT/QA.

---

## FASE 2 — Flujo completo Customer

| # | Paso | Request | Expected | Validaciones |
|---|------|---------|----------|--------------|
| 2.1 | Register Customer | POST /api/v1/auth/register — body: fullName `AUDIT_{{timestamp}} Customer`, email `AUDIT_{{timestamp}}_cust@audit.local`, password, **role: 1** (Customer), phone null | 201 | Body: userId, email, role "Customer", token presente. No exponer password. |
| 2.2 | Login Customer | POST /api/v1/auth/login — email y password del paso 2.1 | 200 | token en response; guardar en `customerToken`. |
| 2.3 | Crear Job | POST /api/v1/jobs — Authorization Bearer `customerToken`. Body: categoryId 1, title `AUDIT_JOB_{{timestamp}}`, description, addressText, budgetMin/Max | 201 | Body: id, title, status "Open", customerId coherente. Guardar `id` en `jobId`. |
| 2.4 | Listar mis Jobs | GET /api/v1/jobs/mine?page=1&pageSize=20 — Bearer `customerToken` | 200 | PagedResult: items (array), totalCount, page, pageSize. El job creado debe aparecer. |
| 2.5 | Obtener Job por ID | GET /api/v1/jobs/{{jobId}} — Bearer `customerToken` | 200 | Job con mismo id; campos obligatorios (id, title, status, etc.). |
| 2.6 | **Negativa:** Customer B intenta ver job de Customer A | Registrar segundo Customer (B); Login B; GET /api/v1/jobs/{{jobId}} con token de B (jobId es el de A) | **403** | Body: errorCode FORBIDDEN o equivalente; no devolver datos del job. |

**Evidencia (redactada):** Request 2.6 — Response 403, body: `{"title":"Forbidden","status":403,"detail":"Access denied to this job.","errorCode":"FORBIDDEN"}` (o similar). Sin tokens ni IDs reales en documentación pública.

---

## FASE 3 — Flujo completo Technician

| # | Paso | Request | Expected | Validaciones |
|---|------|---------|----------|--------------|
| 3.1 | Register Technician | POST /api/v1/auth/register — fullName, email `AUDIT_{{timestamp}}_tech@audit.local`, password, **role: 2** (Technician) | 201 | role "Technician"; token presente. |
| 3.2 | Login Technician | POST /api/v1/auth/login | 200 | Guardar token en `technicianToken`. |
| 3.3 | Listar jobs disponibles | GET /api/v1/jobs?page=1&pageSize=20 — Bearer `technicianToken` | 200 | PagedResult; Technician ve jobs Open o donde tiene propuesta. |
| 3.4 | Enviar Proposal | POST /api/v1/jobs/{{jobId}}/proposals — Bearer `technicianToken`. Body: price 150, message "AUDIT proposal" | 201 | Body: id, jobId, technicianId, status "Pending". Guardar proposalId. |
| 3.5 | **Negativa:** Proposal duplicada | POST /api/v1/jobs/{{jobId}}/proposals — mismo technician, mismo job, de nuevo | **400** o **409** | errorCode DUPLICATE_PROPOSAL o mensaje equivalente. |
| 3.6 | **Negativa:** Proposal a job inexistente | POST /api/v1/jobs/00000000-0000-0000-0000-000000000000/proposals — Bearer `technicianToken` | **404** o **400** | JOB_NOT_FOUND o equivalente. |

---

## FASE 4 — Interacción Customer ↔ Technician (y Admin)

En FixHub **solo Admin** puede aceptar propuestas (asignar técnico). Flujo: Customer crea job → Technician envía proposal → **Admin** acepta proposal → Technician inicia job → Customer completa job.

| # | Paso | Request | Expected | Validaciones |
|---|------|---------|----------|--------------|
| 4.1 | Customer: Listar proposals de SU job | GET /api/v1/jobs/{{jobId}}/proposals — Bearer `customerToken` | 200 | **Nota:** En código actual, Customer recibe lista filtrada por TechnicianId (comportamiento incorrecto H04); puede devolver lista vacía. Tras fix: debe devolver todas las propuestas del job si es dueño. |
| 4.2 | Admin: Aceptar proposal (asignar technician) | POST /api/v1/proposals/{{proposalId}}/accept — Bearer `adminToken` | 200 | Body: assignmentId, jobId, technicianId, status. Job pasa a Assigned. |
| 4.3 | Technician: Iniciar job | POST /api/v1/jobs/{{jobId}}/start — Bearer `technicianToken` | 200 | Job pasa a InProgress. |
| 4.4 | Customer: Completar job | POST /api/v1/jobs/{{jobId}}/complete — Bearer `customerToken` | 200 | Job pasa a Completed. |
| 4.5 | **Negativa:** Technician no asignado intenta Start | Otro Technician (no asignado) llama POST .../start con el mismo jobId | **403** | FORBIDDEN. |
| 4.6 | **Negativa:** Customer ajeno intenta Complete | Customer B llama POST .../complete con jobId de Customer A | **403** | FORBIDDEN. |

---

## FASE 5 — Flujo Admin

| # | Paso | Request | Expected | Validaciones |
|---|------|---------|----------|--------------|
| 5.1 | Login Admin | POST /api/v1/auth/login — email y contraseña de usuario seed admin | 200 | Guardar token en `adminToken`. |
| 5.2 | Listar todos los jobs | GET /api/v1/jobs?page=1&pageSize=20 — Bearer `adminToken` | 200 | PagedResult con jobs de todos los clientes. |
| 5.3 | Listar proposals de un job | GET /api/v1/jobs/{{jobId}}/proposals — Bearer `adminToken` | 200 | Lista de propuestas del job. |
| 5.4 | Dashboard | GET /api/v1/admin/dashboard — Bearer `adminToken` | 200 | Body: KPIs, alerts, recent jobs, etc. |
| 5.5 | **Negativa:** Customer intenta dashboard | GET /api/v1/admin/dashboard — Bearer `customerToken` | **403** | AdminOnly. |
| 5.6 | **Negativa:** Technician intenta dashboard | GET /api/v1/admin/dashboard — Bearer `technicianToken` | **403** | AdminOnly. |

---

## FASE 6 — Pruebas negativas críticas

| # | Prueba | Request | Expected | Estado actual (code review) |
|---|--------|---------|----------|-----------------------------|
| 6.1 | Registrar con role=Admin | POST /api/v1/auth/register — body con **role: 3** (Admin) | **400** o **403** | **FALLO:** API devuelve 201 y crea usuario Admin (H03). Tras fix: debe ser 400/403. |
| 6.2 | Acceder a job ajeno | GET /api/v1/jobs/{{jobId_ajeno}} — Bearer Customer B | **403** | OK (handler valida ownership). |
| 6.3 | Token expirado | Cualquier endpoint con Bearer token expirado | **401** | OK (JWT valida lifetime). |
| 6.4 | Token alterado | Cualquier endpoint con Bearer token con un byte cambiado | **401** | OK (firma inválida). |
| 6.5 | Sin token | GET /api/v1/jobs/mine sin header Authorization | **401** | OK. |
| 6.6 | Rate limiting Auth | >10 requests a POST /auth/login (o register) en 1 min desde misma IP | **429** | OK (AuthPolicy 10 req/min). |

**Evidencia 6.1 (redactada):** Request: `POST /api/v1/auth/register` body `{"fullName":"...","email":"...@audit.local","password":"<REDACTED>","role":3,"phone":null}`. Expected: 400/403. Actual (pre-remediación): 201 Created. Incluir en evidencia: status code y fragmento de body sin datos sensibles.

---

## FASE 7 — Validación de integridad

| # | Verificación | Cómo validar | Resultado esperado |
|---|--------------|--------------|--------------------|
| 7.1 | No datos huérfanos | Tras flujo completo, revisar en BD (si se tiene acceso): proposals con jobId existente; assignments con proposalId y jobId existentes. | Todas las FKs resuelven a filas existentes. |
| 7.2 | No propuestas duplicadas | Constraint único (JobId, TechnicianId) en tabla proposals. Intentar insert duplicado → 409 o 400. | Ya cubierto en 3.5. |
| 7.3 | Estados del Job | Flujo: Open → Assigned (accept) → InProgress (start) → Completed (complete). Intentar complete en Open → 400. | Transiciones válidas; invalid status devuelve 400 con errorCode. |
| 7.4 | Concurrency | Dos admins intentan aceptar dos propuestas distintas para el mismo job; solo uno debe tener éxito; el segundo debe recibir JOB_ALREADY_ASSIGNED o similar. | Opcional: ejecutar dos requests en paralelo y verificar uno 200 y otro 400. |

---

## Tabla de resultados (Pass / Fail)

Rellenar tras ejecutar la batería. **Entorno:** _____________  **Fecha:** _____________  **Ejecutor:** _____________

| ID | Descripción | Expected | Actual (HTTP) | Pass/Fail | Notas |
|----|-------------|----------|---------------|-----------|-------|
| 2.1 | Register Customer | 201 | | | |
| 2.2 | Login Customer | 200 | | | |
| 2.3 | Create Job | 201 | | | |
| 2.4 | List My Jobs | 200 | | | |
| 2.5 | Get Job by ID (owner) | 200 | | | |
| 2.6 | Get Job other customer | 403 | | | |
| 3.1 | Register Technician | 201 | | | |
| 3.2 | Login Technician | 200 | | | |
| 3.3 | List Jobs (Technician) | 200 | | | |
| 3.4 | Submit Proposal | 201 | | | |
| 3.5 | Proposal duplicada | 400/409 | | | |
| 3.6 | Proposal job inexistente | 404/400 | | | |
| 4.1 | Customer list proposals own job | 200 | | | Lista puede estar vacía (H04). |
| 4.2 | Admin Accept Proposal | 200 | | | |
| 4.3 | Technician Start Job | 200 | | | |
| 4.4 | Customer Complete Job | 200 | | | |
| 4.5 | Technician no asignado Start | 403 | | | |
| 4.6 | Customer ajeno Complete | 403 | | | |
| 5.1 | Login Admin | 200 | | | |
| 5.2 | Admin List Jobs | 200 | | | |
| 5.3 | Admin Get Proposals | 200 | | | |
| 5.4 | Admin Dashboard | 200 | | | |
| 5.5 | Customer → Dashboard | 403 | | | |
| 5.6 | Technician → Dashboard | 403 | | | |
| 6.1 | Register role=Admin | 400/403 | 201 (pre-fix) | **Fail** | H03. |
| 6.2 | Job ajeno | 403 | | | |
| 6.3 | Token expirado | 401 | | | |
| 6.4 | Token alterado | 401 | | | |
| 6.5 | Sin token | 401 | | | |
| 6.6 | Rate limit Auth | 429 | | | |

---

## Evidencia (requests/responses redactados)

### Evidencia A — 403 (Customer intenta ver job ajeno)

- **Request:** `GET /api/v1/jobs/{{jobId}}`  
  **Headers:** `Authorization: Bearer <REDACTED_customerB_token>`
- **Response:** `403 Forbidden`  
  **Body (ejemplo):** `{"type":"...","title":"Forbidden","status":403,"detail":"Access denied to this job.","instance":"/api/v1/jobs/...","errorCode":"FORBIDDEN"}`

### Evidencia B — 401 (Sin token)

- **Request:** `GET /api/v1/jobs/mine` (sin Authorization)
- **Response:** `401 Unauthorized`  
  **Body (ejemplo):** `{"type":"...","title":"Unauthorized","status":401,...}`

### Evidencia C — 400 (Register con role=Admin — post-fix esperado)

- **Request:** `POST /api/v1/auth/register`  
  **Body:** `{"fullName":"AUDIT_Test","email":"audit_admin_attempt@audit.local","password":"<REDACTED>","role":3,"phone":null}`
- **Response esperada tras fix:** `400 Bad Request` con errors.Role o mensaje que rechace registro de Admin.
- **Response actual (pre-fix):** `201 Created` — **fallo de seguridad documentado (H03).**

---

## Resumen final

### Flujos que pasan (esperado tras ejecución)

- Health, Register/Login Customer y Technician, Create Job, List My Jobs, Get Job (owner), Get Job ajeno → 403.
- List Jobs (Technician), Submit Proposal, Proposal duplicada → 400/409, Proposal job inexistente → 404/400.
- Admin: Login, List Jobs, Get Proposals, Dashboard; Customer/Technician → Admin dashboard 403.
- Accept Proposal (Admin), Start Job (Technician asignado), Complete Job (Customer owner).
- Token expirado/alterado/sin token → 401; rate limit Auth → 429.

### Flujos que fallan o son parciales (conocidos)

- **6.1 Register con role=Admin:** Actualmente 201 (debe ser 400/403). **Riesgo crítico H03.**
- **4.1 Customer list proposals de su job:** Código actual devuelve lista filtrada por TechnicianId (Customer recibe vacía). **Riesgo alto H04.**

### Riesgos detectados

- Escalación de privilegios por registro con Role=Admin (H03).
- Customer no puede ver propuestas de su propio job (H04); posible IDOR si se “arregla” sin validar ownership.

### Recomendaciones técnicas

1. Implementar rechazo explícito de Role=Admin en registro (validator o handler) y test automático que aserte 400/403.
2. Corregir GetJobProposalsQuery: validar job.CustomerId == RequesterId para Customer; devolver todas las propuestas del job cuando el Customer es dueño.
3. Mantener datos de prueba con prefijo AUDIT_; limpiar solo en BD de pruebas (truncar tablas de test o usar DB efímera). No ejecutar deletes destructivos en datos reales.
4. Añadir a la suite Postman/Newman aserciones por status code y, donde aplique, por body (errorCode, presence of fields) para automatizar la tabla de resultados.

---

## Cómo ejecutar las pruebas

1. **Postman:** Importar `tests/AUDIT_E2E/FixHub-AUDIT-E2E.postman_collection.json` y `FixHub-AUDIT-E2E.postman_environment.json`. Setear `baseUrl` (ej. `http://localhost:5100`) y, para Login Admin, `adminEmail` y `adminPassword` (usuario seed). **Orden de ejecución:** carpetas en orden 0 → 1 → 2 → 3 → 4 → 5 → 6 → 7. Dentro de 1: ejecutar primero Register Customer y Login Customer (para que `customerEmail` y `token` se rellenen), luego Register Technician y Login Technician, luego Login Admin. Las variables `token`, `technicianToken`, `tokenAdmin`, `jobId`, `proposalId` se rellenan automáticamente por los scripts de test de cada request.
2. **Newman:** `newman run tests/AUDIT_E2E/FixHub-AUDIT-E2E.postman_collection.json -e tests/AUDIT_E2E/FixHub-AUDIT-E2E.postman_environment.json`
3. **Integration tests (Testcontainers):** `dotnet test tests/FixHub.IntegrationTests/FixHub.IntegrationTests.csproj` — cubren muchos de los flujos y negativas (403, 401) sin necesidad de Postman.

**Limpieza:** En entorno local con Docker, se puede hacer `docker compose down -v` para eliminar volúmenes y volver a levantar (BD limpia). Solo en entorno dedicado a pruebas; no en datos reales.

---

*Documento: docs/AUDIT/05_E2E_EXECUTION.md. Referencia: FINAL_REPORT.md, 01_FUNCTIONAL_TESTS.md.*
