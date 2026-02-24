# FixHub — Pruebas funcionales E2E (Fase 1)

**Branch:** `audit/fixhub-100`  
**Alcance:** Suite E2E por API (sin UI), repetible en local/docker-compose. Datos de prueba con prefijo `AUDIT_` cuando se documenten en este doc.

---

## 1. Cómo ejecutar

### 1.1 Con docker-compose (local)

1. En la raíz del repo crear `.env` con variables (no versionado): `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `JWT_SECRET_KEY`, `WEB_ORIGIN`.
2. Levantar stack: `docker compose up -d --build`.
3. Esperar a que `fixhub_migrator` termine y `fixhub_api` esté healthy.
4. Si la API se expone en host (ej. mapeando puerto 5100), usar `baseUrl = http://localhost:5100`. Si solo se expone Web en 8084, las llamadas directas a la API deben ir al contenedor o exponer API en un puerto (ej. en compose añadir `ports: ["5100:8080"]` para fixhub_api en entorno de auditoría).
5. Ejecutar Postman/Newman o los integration tests (ver abajo).

### 1.2 Tests de integración (Testcontainers) — recomendado para CI

No usan docker-compose; levantan PostgreSQL efímero y WebApplicationFactory.

```powershell
cd c:\Proyectos\FixHub
dotnet test tests/FixHub.IntegrationTests/FixHub.IntegrationTests.csproj --no-build
```

O con build:

```powershell
dotnet test tests/FixHub.IntegrationTests/FixHub.IntegrationTests.csproj
```

### 1.3 Postman / Newman

- Colección: `tests/AUDIT_E2E/FixHub-AUDIT-E2E.postman_collection.json`
- Environment: `tests/AUDIT_E2E/FixHub-AUDIT-E2E.postman_environment.json`

En el environment, setear `baseUrl` (ej. `http://localhost:5100`). Para Admin, setear `tokenAdmin` manualmente (login con admin@fixhub.com y contraseña de seed) o añadir request "Login Admin" que guarde el token en `tokenAdmin`.

```powershell
npx newman run tests/AUDIT_E2E/FixHub-AUDIT-E2E.postman_collection.json -e tests/AUDIT_E2E/FixHub-AUDIT-E2E.postman_environment.json
```

---

## 2. Cobertura por flujo

| Flujo | Descripción | Cómo se verifica |
|-------|-------------|------------------|
| **A) Auth** | Register (Customer) → Login | Postman: Register Customer (AUDIT), Login. Integration tests: `Register_And_Login_Return_Token`. |
| **A) Rate limit Auth** | 10 req/min en /auth | Ejecutar >10 login/register en 1 min; esperado 429. Documentado; no automatizado en colección. |
| **B) Jobs** | Customer crea job, lista, consulta por ID | Create Job, List My Jobs, Get Job by ID. Integration: múltiples tests con CreateJob + Get. |
| **B) Technician** | Lista jobs, aplica/propone | List Jobs (con token Technician), Submit Proposal. Integration: Technician_Cannot_View_Unassigned_Job_Returns_403, etc. |
| **B) Customer ve propuestas** | GET jobs/{id}/proposals (solo su job) | Get Job Proposals con token Customer (owner). **Nota:** En código actual, Customer recibe lista filtrada por TechnicianId (comportamiento a revisar ver auditoría estática). |
| **B) Admin asigna** | Accept proposal | Accept Proposal con token Admin. Integration: flujos con AcceptProposal. |
| **B) Technician inicia** | POST jobs/{id}/start | Technician_Can_Start_Assigned_Job_Returns_200. |
| **B) Customer completa/cancela** | complete/cancel según reglas | Integration: CompleteJob_Twice_Returns_400, CancelJob_InvalidStatus_Returns_400. |
| **C) Proposals** | Technician solo sus proposals; Customer solo de sus jobs; Admin todo | GetJobProposalsQuery: Admin ve todas; Technician ve solo suyas. Customer: ver hallazgo H04 en auditoría. |
| **D) Notifications** | List, unread-count, mark read (solo propietario) | List Notifications, Unread Count, Mark Read. Integration: notificaciones filtradas por UserId. |
| **E) JobIssues / Alerts** | Report issue (Customer/Admin), resolve (Admin) | Report Job Issue; Admin: dashboard, resolve issue. Integration: ReportJobIssue_InvalidReason_Returns_400, resolución doble. |

---

## 3. Resultados esperados (resumen)

- **Health:** GET /api/v1/health → 200, `"status":"healthy"`.
- **Register Customer:** POST /api/v1/auth/register (role 1) → 201, body con `token`.
- **Login:** POST /api/v1/auth/login → 200, body con `token`.
- **Create Job (Customer):** POST /api/v1/jobs con Bearer → 201, body con `id`.
- **List My Jobs:** GET /api/v1/jobs/mine → 200, PagedResult.
- **Get Job (owner):** GET /api/v1/jobs/{id} con owner → 200; con otro Customer → 403 (integration test cubierto).
- **Submit Proposal (Technician):** POST /api/v1/jobs/{id}/proposals → 201.
- **Accept Proposal (Admin):** POST /api/v1/proposals/{id}/accept → 200.
- **Start Job (Technician asignado):** POST /api/v1/jobs/{id}/start → 200; Technician no asignado → 403.
- **Complete/Cancel (Customer owner):** 200 según estado; otro Customer → 403.
- **Notifications:** GET list/unread-count → 200; POST {id}/read solo propio.
- **Rate limit Auth:** >10 requests en 1 min a /auth → 429 (verificar manualmente o script).

---

## 4. Limpieza de datos

- **Testcontainers:** No requiere limpieza; BD efímera.
- **docker-compose (BD persistente):** Si se crean usuarios/jobs con prefijo AUDIT_, usar solo en BD dedicada a pruebas. No ejecutar DELETE masivos en datos reales. Opción: base `FixHub_Audit` separada o truncar solo tablas de auditoría documentadas en 04/FINAL_REPORT.

---

## 5. Entregables Fase 1

| Entregable | Ubicación |
|------------|-----------|
| Colección Postman | `tests/AUDIT_E2E/FixHub-AUDIT-E2E.postman_collection.json` |
| Environment Postman | `tests/AUDIT_E2E/FixHub-AUDIT-E2E.postman_environment.json` |
| README E2E | `tests/AUDIT_E2E/README.md` |
| Este documento | `docs/AUDIT/01_FUNCTIONAL_TESTS.md` |

Integration tests existentes: `tests/FixHub.IntegrationTests/` (ApiIntegrationTests.cs, ComprehensiveBatteryTests.cs).
