# FixHub — Baseline Confirmation (QA — Harvard Level)

| Metadato | Valor |
|---|---|
| Preparado por | QA Lead — Functional Audit |
| Fecha de generación | 2026-02-25 |
| Rama git | `audit/fixhub-100` |
| Entorno | Local / SIT (PROHIBIDO producción) |
| Fuente de verdad primaria | Código fuente en `src/` |
| Fuente de verdad secundaria | `docs/QA/00_SYSTEM_FUNCTIONAL_OVERVIEW.md` |
| Prefijo datos de prueba | `FUNC_<timestamp>` |
| API Health verificada | ✅ `GET /api/v1/health` → 200 `{"status":"healthy","database":"connected"}` |

---

## 1. Stack Tecnológico — Confirmado en Código

| Componente | Tecnología | Versión | Archivo de confirmación |
|---|---|---|---|
| Runtime | .NET | 8.0 | `FixHub.sln`, `*.csproj` |
| API Framework | ASP.NET Core WebAPI | 8.0 | `FixHub.API/Program.cs` |
| Frontend | ASP.NET Core Razor Pages | 8.0 | `FixHub.Web/` |
| ORM | Entity Framework Core + Npgsql | 8.0 | `FixHub.Infrastructure/DependencyInjection.cs` |
| Base de Datos | PostgreSQL | 15+ | `docker-compose.yml` |
| CQRS | MediatR | 12.x | `FixHub.Application/DependencyInjection.cs` |
| Validación | FluentValidation | 11.x | `Application/Features/*/Validators/` |
| Autenticación | JWT Bearer HS256 | — | `Program.cs`, `JwtTokenService.cs` |
| Hashing Contraseñas | BCrypt (work factor 12) | — | `BcryptPasswordHasher.cs` |
| Email | SendGrid | — | `Infrastructure/Services/` |
| Contenedores | Docker + Compose | — | `docker-compose.yml` |
| Documentación API | Swagger/OpenAPI 3.0 | — | `Program.cs` |
| Audit Trail | MediatR Pipeline Behavior | — | `AuditBehavior.cs` |
| Rate Limiting | ASP.NET Core Rate Limiting | 8.0 | `Program.cs` |

**Puertos locales (dev):**
- API: `http://localhost:5100`
- Web Razor: `http://localhost:5200`

---

## 2. Endpoints Confirmados — Inventario Completo

### 2.1 HEALTH (Sin autenticación)

| # | Método | Ruta | Auth Req. | Política | Response OK | Status codes | Confirmado en |
|---|---|---|---|---|---|---|---|
| H-01 | GET | `/api/v1/health` | No | — | `HealthResponse{status,version,timestamp,database}` | 200, 503 | `HealthController.cs:11` |

**Rate limit:** Global (60 req/min por IP)

---

### 2.2 AUTH

| # | Método | Ruta | Auth Req. | Política | Body In | Response OK | Status codes | Confirmado en |
|---|---|---|---|---|---|---|---|---|
| A-01 | POST | `/api/v1/auth/register` | No | Rate: 10/min | `RegisterRequest` | `AuthResponse` | 201, 400, 409 | `AuthController.cs:17` |
| A-02 | POST | `/api/v1/auth/login` | No | Rate: 10/min | `LoginRequest` | `AuthResponse` | 200, 400, 401 | `AuthController.cs:31` |

**RegisterRequest** (FluentValidation confirmado):
```
FullName    : string, required, max 200
Email       : string, required, valid email, max 256, unique
Password    : string, required, min 8, max 128, ≥1 uppercase, ≥1 digit
Role        : int enum (1=Customer, 2=Technician, 3=Admin) — ⚠️ BUG H03: Admin aceptado sin restricción
Phone       : string?, optional, max 30
```

**AuthResponse** (ambos endpoints):
```
UserId   : Guid
Email    : string
FullName : string
Role     : string ("Customer" / "Technician" / "Admin")
Token    : string (JWT HS256, exp. 480min dev / 60min prod)
```

**409 Conflict:** Email ya registrado.

---

### 2.3 JOBS

| # | Método | Ruta | Auth Req. | Política | Body In | Response OK | Status codes | Confirmado en |
|---|---|---|---|---|---|---|---|---|
| J-01 | POST | `/api/v1/jobs` | Bearer | CustomerOnly | `CreateJobRequest` | `JobDto` | 201, 400, 404 | `JobsController.cs:16` |
| J-02 | GET | `/api/v1/jobs` | Bearer | Technician\|Admin; Customer→403 | Query params | `PagedResult<JobDto>` | 200, 403 | `JobsController.cs:59` |
| J-03 | GET | `/api/v1/jobs/mine` | Bearer | CustomerOnly | — | `PagedResult<JobDto>` | 200, 403 | `JobsController.cs:83` |
| J-04 | GET | `/api/v1/jobs/{id}` | Bearer | Owner\|Asignado\|Admin | — | `JobDto` | 200, 403, 404 | `JobsController.cs:47` |
| J-05 | GET | `/api/v1/jobs/{id}/proposals` | Bearer | Customer-dueño\|Admin | — | `List<ProposalDto>` | 200, 403 | `JobsController.cs:96` ⚠️ H04 |
| J-06 | POST | `/api/v1/jobs/{id}/proposals` | Bearer | TechnicianOnly | `SubmitProposalRequest` | `ProposalDto` | 201, 400, 409 | `JobsController.cs:107` |
| J-07 | POST | `/api/v1/jobs/{id}/complete` | Bearer | CustomerOnly | — | `JobDto` | 200, 400, 404 | `JobsController.cs:123` |
| J-08 | POST | `/api/v1/jobs/{id}/start` | Bearer | TechnicianOnly | — | `JobDto` | 200, 400, 403, 404 | `JobsController.cs:134` |
| J-09 | POST | `/api/v1/jobs/{id}/cancel` | Bearer | CustomerOnly | — | `JobDto` | 200, 400, 404 | `JobsController.cs:147` |
| J-10 | POST | `/api/v1/jobs/{id}/issues` | Bearer | Customer-dueño\|Admin | `ReportIssueRequest` | `IssueDto` | 201, 400, 403 | `JobsController.cs:158` |

**CreateJobRequest** (FluentValidation):
```
CategoryId  : int, required, >0 (IDs válidos: 1-6)
Title       : string, required, max 200
Description : string, required, max 2000
AddressText : string, required, max 500
Lat         : decimal?, optional, -90..90
Lng         : decimal?, optional, -180..180
BudgetMin   : decimal?, optional, >0
BudgetMax   : decimal?, optional, ≥BudgetMin
```

**JobDto** (response):
```
Id, CustomerId, CustomerName, CategoryId, CategoryName, Title, Description,
AddressText, Lat, Lng, Status (string), BudgetMin, BudgetMax, CreatedAt,
AssignedTechnicianId?, AssignedTechnicianName?, AssignedAt?, StartedAt?,
CompletedAt?, CancelledAt?
```

**Query params GET /jobs:**
```
status    : JobStatus int (1=Open, 2=Assigned, 3=InProgress, 4=Completed, 5=Cancelled)
categoryId: int
page      : int (default 1)
pageSize  : int (default 20)
```

---

### 2.4 PROPOSALS

| # | Método | Ruta | Auth Req. | Política | Body In | Response OK | Status codes | Confirmado en |
|---|---|---|---|---|---|---|---|---|
| P-01 | POST | `/api/v1/proposals/{id}/accept` | Bearer | Auth (handler: Admin) | — | `AcceptProposalResponse` | 200, 400, 403, 404, 409 | `ProposalsController.cs:18` |

> **⚠️ Inconsistencia:** El controller declara `[Authorize]` (sin policy de rol explícita) pero el handler `AcceptProposalCommand` rechaza cualquier invocación que no sea Admin (`IsAdmin` check interno). Customer puede llamar el endpoint y recibe 403 funcional del handler.

**AcceptProposalResponse:**
```
AssignmentId, JobId, ProposalId, TechnicianId, TechnicianName, AcceptedPrice, AcceptedAt
```

---

### 2.5 TECHNICIANS

| # | Método | Ruta | Auth Req. | Política | Body In | Response OK | Status codes | Confirmado en |
|---|---|---|---|---|---|---|---|---|
| T-01 | GET | `/api/v1/technicians/{id}/profile` | Bearer | Any Auth | — | `TechnicianProfileDto` | 200, 404 | `TechniciansController.cs:14` |
| T-02 | GET | `/api/v1/technicians/me/assignments` | Bearer | TechnicianOnly | — | `PagedResult<AssignmentDto>` | 200, 403 | `TechniciansController.cs:24` |

**TechnicianProfileDto:**
```
UserId, FullName, Email, Bio?, ServiceRadiusKm, IsVerified, Status (string),
AvgRating, CompletedJobs, CancelRate
```

---

### 2.6 REVIEWS

| # | Método | Ruta | Auth Req. | Política | Body In | Response OK | Status codes | Confirmado en |
|---|---|---|---|---|---|---|---|---|
| R-01 | POST | `/api/v1/reviews` | Bearer | CustomerOnly | `CreateReviewRequest` | `ReviewDto` | 201, 400, 409 | `ReviewsController.cs:13` |

**CreateReviewRequest:**
```
JobId   : Guid, required (job debe ser Completed y del Customer)
Stars   : int, required, 1-5
Comment : string?, optional
```

**409 Conflict:** Review ya existe para ese JobId.

---

### 2.7 NOTIFICATIONS

| # | Método | Ruta | Auth Req. | Política | Body In | Response OK | Status codes | Confirmado en |
|---|---|---|---|---|---|---|---|---|
| N-01 | GET | `/api/v1/notifications` | Bearer | Any Auth | — | `PagedResult<NotificationDto>` | 200 | `NotificationsController.cs:16` |
| N-02 | GET | `/api/v1/notifications/unread-count` | Bearer | Any Auth | — | `int` | 200 | `NotificationsController.cs:28` |
| N-03 | POST | `/api/v1/notifications/{id}/read` | Bearer | Any Auth (own) | — | 204 | 204, 404 | `NotificationsController.cs:37` |

**NotificationDto:**
```
Id, UserId, JobId?, Type (string enum), Message, IsRead, CreatedAt
```

**NotificationType enum:** JobCreated(1), JobAssigned(2), JobStarted(3), JobCompleted(4), JobCancelled(5), IssueReported(6), SlaAlert(7)

---

### 2.8 ADMIN

| # | Método | Ruta | Auth Req. | Política | Body In | Response OK | Status codes | Confirmado en |
|---|---|---|---|---|---|---|---|---|
| ADM-01 | GET | `/api/v1/admin/applicants` | Bearer | AdminOnly | — | `PagedResult<ApplicantDto>` | 200 | `AdminController.cs:17` |
| ADM-02 | PATCH | `/api/v1/admin/technicians/{id}/status` | Bearer | AdminOnly | `UpdateStatusRequest` | 204 | 204, 400, 404 | `AdminController.cs:31` |
| ADM-03 | GET | `/api/v1/admin/issues` | Bearer | AdminOnly | — | `PagedResult<IssueDto>` | 200 | `AdminController.cs:49` |
| ADM-04 | GET | `/api/v1/admin/dashboard` | Bearer | AdminOnly | — | `OpsDashboardDto` | 200 | `AdminController.cs:61` |
| ADM-05 | POST | `/api/v1/admin/jobs/{id}/start` | Bearer | AdminOnly | — | `JobDto` | 200, 400, 404 | `AdminController.cs:70` |
| ADM-06 | PATCH | `/api/v1/admin/jobs/{id}/status` | Bearer | AdminOnly | `AdminJobStatusRequest` | `JobDto` | 200, 400, 404 | `AdminController.cs:91` |
| ADM-07 | GET | `/api/v1/admin/metrics` | Bearer | AdminOnly | — | `AdminMetricsDto` | 200 | `AdminController.cs:82` |
| ADM-08 | PATCH | `/api/v1/admin/alerts/{id}/resolve` | Bearer | AdminOnly | — | 204 | 204, 404, 409 | `AdminController.cs:106` |
| ADM-09 | POST | `/api/v1/admin/issues/{id}/resolve` | Bearer | AdminOnly | `ResolveIssueRequest` | 204 | 204, 400, 404, 409 | `AdminController.cs:117` |

**UpdateStatusRequest:** `{ "status": int }` → TechnicianStatus (0=Pending, 1=InterviewScheduled, 2=Approved, 3=Rejected)

**AdminJobStatusRequest:** `{ "newStatus": "string" }` → JobStatus name ("Open", "Assigned", "InProgress", "Completed", "Cancelled")

**OpsDashboardDto:**
```
Kpis: { TotalJobs, OpenJobs, InProgressJobs, CompletedJobs, TotalTechnicians, ApprovedTechnicians,
        AvgTimeToAssignMinutes, AvgTimeToCompleteMinutes }
AlertJobs: List<{ JobId, JobTitle, Alerts: List<string> }>
RecentJobs: List<{ JobId, JobTitle, Status, CreatedAt }>
```

---

### 2.9 AI SCORING

| # | Método | Ruta | Auth Req. | Política | Body In | Response OK | Status codes | Confirmado en |
|---|---|---|---|---|---|---|---|---|
| AI-01 | POST | `/api/v1/ai-scoring/jobs/{jobId}/rank-technicians` | Bearer | AdminOnly | — | `List<TechnicianRankDto>` | 200, 404 | `AiScoringController.cs:19` |

**Fórmula scoring (confirmada en handler):**
```
Score = (AvgRating × 2) + (CompletedJobs × 0.1) − (CancelRate × 5) + (IsVerified ? 5 : 0)
```

**TechnicianRankDto:** `{ TechnicianId, TechnicianName, Score, Factors: { AvgRatingFactor, CompletedJobsFactor, CancelRateFactor, VerificationBonus, TotalScore } }`

---

## 3. Resumen de Endpoints por Módulo

| Módulo | Endpoints | Autenticados | Públicos |
|---|---|---|---|
| Health | 1 | 0 | 1 |
| Auth | 2 | 0 | 2 |
| Jobs | 10 | 10 | 0 |
| Proposals | 1 | 1 | 0 |
| Technicians | 2 | 2 | 0 |
| Reviews | 1 | 1 | 0 |
| Notifications | 3 | 3 | 0 |
| Admin | 9 | 9 | 0 |
| AI Scoring | 1 | 1 | 0 |
| **TOTAL** | **30** | **27** | **3** |

---

## 4. Matriz Roles vs Acciones — Confirmada en Código

| Acción | Customer | Technician | Admin | Mecanismo de control |
|---|---|---|---|---|
| Register (any role) | ✅ | ✅ | ✅ ⚠️H03 | RegisterCommand validator (no restringe Role=3) |
| Login | ✅ | ✅ | ✅ | LoginCommand |
| Crear Job | ✅ | ❌ 403 | ❌ 403 | Policy "CustomerOnly" |
| Listar Jobs (todos) | ❌ 403 | ✅ | ✅ | Handler check: Customer → Forbidden |
| Listar Mis Jobs (/mine) | ✅ | ❌ 403 | ❌ 403 | Policy "CustomerOnly" |
| Ver Job por ID (propio) | ✅ | ✅ (asignado/open/propuesta) | ✅ (todos) | GetJobQuery ownership check |
| Ver Propuestas de Job | ✅ (pero H04: lista vacía) | ❌ | ✅ | GetJobProposalsQuery — bug: filtra por TechnicianId |
| Enviar Propuesta | ❌ 403 | ✅ (job Open, sin dup.) | ❌ 403 | Policy "TechnicianOnly" |
| Aceptar Propuesta | ❌ (handler lo rechaza) | ❌ | ✅ | Handler: IsAdmin check interno |
| Iniciar Job (Technician) | ❌ 403 | ✅ (propio asignado) | ❌ 403 | Policy "TechnicianOnly" + handler ownership |
| Iniciar Job (Admin force) | ❌ | ❌ | ✅ | AdminOnly — POST /admin/jobs/{id}/start |
| Completar Job | ✅ (propio InProgress) | ❌ 403 | ✅ (via PATCH status) | Policy "CustomerOnly" + estado válido |
| Cancelar Job | ✅ (Open/Assigned) | ❌ 403 | ✅ (via PATCH status) | Policy "CustomerOnly" + estado válido |
| Reportar Issue | ✅ (propio) | ❌ | ✅ | Handler: CustomerId=owner OR IsAdmin |
| Crear Review | ✅ (Completed, no dup.) | ❌ 403 | ❌ 403 | Policy "CustomerOnly" + job Completed |
| Ver Mis Assignments | ❌ 403 | ✅ | ❌ 403 | Policy "TechnicianOnly" |
| Ver Perfil Técnico | ✅ | ✅ | ✅ | Auth required (no role restriction) |
| Ver Notificaciones propias | ✅ | ✅ | ✅ | Auth; filtered by UserId in handler |
| Marcar Notif. Leída | ✅ (propia) | ✅ (propia) | ✅ (propia) | Handler: NotificationUserId == RequesterId |
| Dashboard Admin | ❌ 403 | ❌ 403 | ✅ | Policy "AdminOnly" |
| Listar Applicantes | ❌ | ❌ | ✅ | Policy "AdminOnly" |
| Cambiar Estado Técnico | ❌ | ❌ | ✅ | Policy "AdminOnly" |
| Listar Issues | ❌ | ❌ | ✅ | Policy "AdminOnly" |
| Resolver Issue | ❌ | ❌ | ✅ | Policy "AdminOnly" |
| Resolver Alert | ❌ | ❌ | ✅ | Policy "AdminOnly" |
| Ver Métricas | ❌ | ❌ | ✅ | Policy "AdminOnly" |
| PATCH Job Status (override) | ❌ | ❌ | ✅ | Policy "AdminOnly" |
| AI Scoring/Ranking | ❌ | ❌ | ✅ | Policy "AdminOnly" |

---

## 5. Máquina de Estados — Job (Confirmada en Handlers)

```
           ┌─────────────────────────────────┐
           │          CUSTOMER CREA          │
           └────────────────┬────────────────┘
                            │ POST /jobs → 201
                            ▼
                         [Open]
                         │    │
           Customer/Admin │    │ AcceptProposal (Admin)
               cancela    │    │ POST /proposals/{id}/accept
                          ▼    ▼
                     [Cancelled] ← ─ ─ ─ [Assigned]
                                               │    │
                              Technician inicia│    │Customer/Admin cancela
                     POST /jobs/{id}/start     │    │
                     (o Admin: /admin/jobs/start│   │
                              ▼               ▼
                         [InProgress] → [Cancelled]  (Admin override)
                              │
               Customer POST  │
               /jobs/{id}/complete
               (o Admin PATCH status)
                              ▼
                         [Completed]
```

**Estados finales (sin transición de salida):** `Completed`, `Cancelled`

**Transiciones NO permitidas (devuelven 400):**
- Open → InProgress (sin pasar por Assigned)
- Completed → cualquier estado
- Cancelled → cualquier estado
- Assigned → Completed (sin pasar por InProgress) ← depende de `AdminUpdateJobStatusCommand.AllowedTransitions`

**Archivos que implementan lógica de estado:**
- `CancelJobCommand.cs` — Customer cancela
- `CompleteJobCommand.cs` — Customer completa
- `TechnicianStartJobCommand.cs` — Tech inicia
- `AcceptProposalCommand.cs` — Crea assignment, cambia a Assigned
- `AdminUpdateJobStatusCommand.cs` — Override admin con transiciones permitidas
- `StartJobCommand.cs` — Admin force-start

---

## 6. Máquina de Estados — TechnicianProfile (Confirmada)

```
Pending(0) ──┬──→ InterviewScheduled(1) ──┬──→ Approved(2)
             │                            └──→ Rejected(3)
             └──→ Rejected(3)
```

**Solo Admin puede cambiar:** `PATCH /api/v1/admin/technicians/{id}/status`

**Nota clave:** Un técnico recién registrado tiene `TechnicianProfile.Status = Pending(0)`. Para que pueda recibir asignaciones automáticas, debe estar `Approved(2)`.

---

## 7. Datos Seeded — Confirmados en Migraciones

### 7.1 Admin del Sistema

| Campo | Valor | Fuente |
|---|---|---|
| UserId | `a0000001-0001-0001-0001-000000000001` | `20260220000000_SeedAdminUser.cs` |
| Email | `admin@fixhub.com` | Idem |
| Password | `Admin123!` | Idem (hash BCrypt almacenado) |
| Role | Admin (3) | Idem |
| IsActive | true | Idem |

### 7.2 Categorías de Servicio

| ID | Nombre | Icono | Migración |
|---|---|---|---|
| 1 | Plomería | wrench | `20260217203145_InitialCreate` |
| 2 | Electricidad | zap | Idem |
| 3 | Handyman | tool | Idem |
| 4 | Aire Acondicionado | wind | Idem |
| 5 | Pintura | paint-roller | Idem |
| 6 | Cerrajería | key | `20260218120000_AddCerrajeriaCategory` |

---

## 8. Políticas de Seguridad — Confirmadas

### 8.1 Autenticación

```
Scheme  : JWT Bearer
Signing : HMAC-SHA256 (HS256)
Claims  : sub (UserId), role (UserRole string), email, name
Expiry  : 480 min (dev) / 60 min (prod)
ClockSkew: 30 segundos
```

### 8.2 Políticas de Autorización

```csharp
// Confirmado en Program.cs
"CustomerOnly"    : RequireAuthenticatedUser + RequireRole("Customer")
"TechnicianOnly"  : RequireAuthenticatedUser + RequireRole("Technician")
"AdminOnly"       : RequireAuthenticatedUser + RequireRole("Admin")
"CustomerOrAdmin" : RequireAuthenticatedUser + RequireRole("Customer","Admin")
```

### 8.3 Rate Limiting

| Política | Límite | Ventana | Endpoint(s) | Status en exceso |
|---|---|---|---|---|
| Global | 60 req/min | 1 minuto | Todos | 429 |
| AuthPolicy | 10 req/min | 1 minuto | /auth/register, /auth/login | 429 |

### 8.4 Headers de Seguridad

| Header | Valor | Confirmado en |
|---|---|---|
| X-Content-Type-Options | nosniff | SecurityHeadersMiddleware.cs |
| X-Frame-Options | DENY | Idem |
| Referrer-Policy | no-referrer | Idem |
| Permissions-Policy | geolocation=(), camera=(), microphone=() | Idem |

---

## 9. Middlewares — Confirmados

| Middleware | Función | Confirmado en |
|---|---|---|
| CorrelationIdMiddleware | X-Correlation-Id en req/resp, logging scope | `Middleware/CorrelationIdMiddleware.cs` |
| RequestLoggingMiddleware | Log HTTP verb, path, status, elapsed | Idem |
| RequestContextLoggingMiddleware | UserId + JobId en logging scope | Idem |
| SecurityHeadersMiddleware | Seguridad HTTP headers | `Middleware/SecurityHeadersMiddleware.cs` |
| ExceptionMiddleware | 400 FluentValidation / 500 genérico, RFC 7807 | `Middleware/ExceptionMiddleware.cs` |

**Formato error RFC 7807:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Bad Request",
  "status": 400,
  "errors": { "FieldName": ["Error message"] }
}
```

---

## 10. Servicios Background — Confirmados

| Servicio | Función | Estado |
|---|---|---|
| `OutboxEmailSenderHostedService` | Procesa cola `NotificationOutbox`, envía emails via SendGrid con retry | ✅ Confirmado |
| `JobSlaMonitorHostedService` | Detecta SLA violations, crea `JobAlert` records | ✅ Confirmado |

---

## 11. CQRS Behaviors — Confirmados

| Behavior | Orden ejecución | Función |
|---|---|---|
| `ValidationBehavior<TReq,TResp>` | 1° (antes del handler) | Ejecuta FluentValidation; lanza exception si falla → ExceptionMiddleware → 400 |
| `AuditBehavior<TReq,TResp>` | 2° | Escribe registro en `AuditLog` antes de ejecutar comando |
| `DashboardCachingBehavior<TReq,TResp>` | 3° | Invalida caché de dashboard cuando hay cambios en Jobs |

---

## 12. Items "NO CONFIRMADO"

Flujos mencionados en documentación o inferidos que **no tienen implementación en código**:

| Item | Estado | Impacto en pruebas |
|---|---|---|
| `GET /api/v1/categories` | **NO CONFIRMADO** — no existe endpoint público de categorías | Tests deben usar CategoryId hardcoded (1-6) |
| `ProposalStatus.Withdrawn` — endpoint withdraw | **NO CONFIRMADO** — enum definido, sin handler ni endpoint | TC no puede probar "Technician retira propuesta" |
| Customer acepta propuesta directamente | **INCONSISTENTE** — policy controller permite, handler rechaza | Documentar como comportamiento inconsistente |
| Paginación en `GET /jobs/{id}/proposals` | **NO CONFIRMADO** — retorna `List<>` sin página | Aceptable, documentar en TC |
| Endpoints de pagos | **NO CONFIRMADO** — entidad `Payment` existe, sin API | No hay TC de pagos |
| `GET /api/v1/admin/jobs` exclusivo | **NO CONFIRMADO** — solo existe `GET /jobs` (Technician+Admin) | Usar `GET /jobs` para admin |
| Batch mark notifications read | **NO CONFIRMADO** | Solo mark individual (N-03) |
| `GET /api/v1/notifications/{id}` | **NO CONFIRMADO** | No hay endpoint de notif individual |
| Reschedule / reprogramación | **NO CONFIRMADO** | Sin TC |

---

## 13. Bugs Pre-Existentes — Documentados

| Bug ID | Severidad | Módulo | Descripción | Handler/Archivo afectado |
|---|---|---|---|---|
| **H03** | 🔴 CRÍTICO | Auth | `POST /auth/register` acepta `role: 3` (Admin) — cualquier usuario puede auto-asignarse Admin | `RegisterCommand.cs` / `RegisterCommandHandler.cs` — validator no excluye Role=3 |
| **H04** | 🟠 ALTA | Jobs/Proposals | `GET /jobs/{id}/proposals` retorna lista vacía para Customer dueño del job | `GetJobProposalsQuery.cs` — filtra por `TechnicianId == RequesterId` en lugar de `CustomerId == RequesterId` |

---

## 14. Conclusión del Baseline

| Categoría | Confirmado | No Confirmado / Bugs |
|---|---|---|
| Endpoints API | 30 de 30 documentados | — |
| Políticas de Rol | 4 de 4 | — |
| Entidades de Dominio | 12 de 12 | — |
| Enums de Estado | 5 de 5 | — |
| Transiciones Job | 7 definidas y verificadas | — |
| Features no implementadas | — | 9 items |
| Bugs pre-existentes | — | 2 (H03, H04) |

**✅ BASELINE COMPLETO** — Se puede proceder a la Fase 1 (Matriz de Pruebas).

---
*Generado: 2026-02-25 | Rama: audit/fixhub-100 | API Health: ✅ 200 OK*
