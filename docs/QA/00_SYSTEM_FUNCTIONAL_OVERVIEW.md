# FixHub — Documento de Comprensión Funcional (QA)

**Objetivo:** Entendimiento preciso del sistema antes de diseñar casos de prueba.  
**Fuente:** Código (controladores, handlers, entidades, DTOs), docs/AUDIT/, docs existentes.  
**Regla:** No se inventan funcionalidades; lo no verificado se marca como DESCONOCIDO.

---

## 1. Qué es FixHub

| Aspecto | Descripción (evidencia) |
|---------|---------------------------|
| **Propósito** | Plataforma de **servicios del hogar** (empresa de servicios, no marketplace público abierto). Los clientes crean solicitudes (jobs); la empresa asigna técnicos; el cliente confirma cierre y puede calificar. |
| **Evidencia** | `docs/QA_FUNCTIONAL_REPORT.md` ("empresa de servicios"); `AcceptProposalCommand` exige `AcceptAsAdmin` ("Solo un administrador puede asignar técnicos"); `CreateJobCommand` puede auto-asignar un técnico aprobado si existe. |
| **Stack** | ASP.NET Core 8, Clean Architecture (Domain, Application, Infrastructure, API), EF Core, PostgreSQL, JWT, MediatR/CQRS, Docker. |

---

## 2. Roles y quiénes lo usan

| Rol | Valor enum | Descripción (código) |
|-----|------------|------------------------|
| **Customer** | `UserRole.Customer = 1` | Crea jobs (solicitudes de servicio), ve solo los suyos, completa/cancela/reporta incidencias, deja reviews. |
| **Technician** | `UserRole.Technician = 2` | Ve jobs Open (oportunidades) y donde tiene propuesta/asignación; envía propuestas; inicia el job si está asignado. Tiene perfil (`TechnicianProfile`) con estado (Pending/InterviewScheduled/Approved/Rejected). |
| **Admin** | `UserRole.Admin = 3` | Acceso total: dashboard, listado de jobs, aceptar propuestas (asignar técnico), forzar inicio/estado de jobs, listar/resolver incidencias y alertas SLA, métricas, ranking AI de técnicos. |

**Evidencia:** `FixHub.Domain/Enums/UserRole.cs`; políticas en `FixHub.API/Program.cs` (CustomerOnly, TechnicianOnly, AdminOnly); `GetJobQuery` y `ListJobsQuery` filtran por rol.

---

## 3. Capacidades por rol (resumen)

- **Customer:** Crear job, listar “mis jobs” (GET /jobs/mine), ver job por ID (solo propio), ver propuestas de su job (actualmente fallo conocido: recibe lista vacía — ver H04), completar/cancelar/reportar incidencia, crear review (job completado, una por job).
- **Technician:** Listar jobs (Open + donde tiene propuesta), ver job (asignado, Open o con propuesta propia), enviar propuesta (job Open, una por job), iniciar job (solo si asignado), ver mis asignaciones, ver perfil de técnico.
- **Admin:** Todo lo anterior + listar todos los jobs con filtros, aceptar propuestas (único que puede asignar), forzar start/status de job, listar/resolver issues y alertas, dashboard operativo, métricas, rank técnicos por job (AI scoring).

---

## 4. Inventario técnico

### 4.1 Controladores y rutas API (verbo + path)

Base: `ApiControllerBase` usa `[Route("api/v1/[controller]")]` → Jobs = `api/v1/jobs`, Auth = `api/v1/auth`, etc. Admin, Notifications y AiScoring tienen ruta explícita.

| Módulo | Método | Ruta completa | Auth | Política / Notas |
|--------|--------|----------------|------|------------------|
| **Health** | GET | `/api/v1/health` | No | Público; verifica DB. |
| **Auth** | POST | `/api/v1/auth/register` | No | Rate limit AuthPolicy (10 req/min). |
| **Auth** | POST | `/api/v1/auth/login` | No | Idem. |
| **Jobs** | POST | `/api/v1/jobs` | Sí | CustomerOnly. |
| **Jobs** | GET | `/api/v1/jobs` | Sí | Technician/Admin; Customer recibe 403 (usar /mine). |
| **Jobs** | GET | `/api/v1/jobs/mine` | Sí | CustomerOnly. |
| **Jobs** | GET | `/api/v1/jobs/{id}` | Sí | Ownership: Customer propio, Technician asignado/Open/con propuesta, Admin todo. |
| **Jobs** | GET | `/api/v1/jobs/{id}/proposals` | Sí | Customer (own job) / Admin; Technician ve solo sus propuestas. **H04:** Customer no ve correctamente (lista vacía). |
| **Jobs** | POST | `/api/v1/jobs/{id}/proposals` | Sí | TechnicianOnly. |
| **Jobs** | POST | `/api/v1/jobs/{id}/complete` | Sí | CustomerOnly; ownership en handler. |
| **Jobs** | POST | `/api/v1/jobs/{id}/start` | Sí | TechnicianOnly; solo técnico asignado. |
| **Jobs** | POST | `/api/v1/jobs/{id}/cancel` | Sí | CustomerOnly; solo Open/Assigned. |
| **Jobs** | POST | `/api/v1/jobs/{id}/issues` | Sí | Customer (own job) o Admin. |
| **Proposals** | POST | `/api/v1/proposals/{id}/accept` | Sí | Customer o Admin en controller; **handler exige AcceptAsAdmin** → solo Admin asigna. |
| **Technicians** | GET | `/api/v1/technicians/{id}/profile` | Sí | Público autenticado. |
| **Technicians** | GET | `/api/v1/technicians/me/assignments` | Sí | TechnicianOnly. |
| **Reviews** | POST | `/api/v1/reviews` | Sí | CustomerOnly. |
| **Notifications** | GET | `/api/v1/notifications` | Sí | Por UserId. |
| **Notifications** | GET | `/api/v1/notifications/unread-count` | Sí | Por UserId. |
| **Notifications** | POST | `/api/v1/notifications/{id}/read` | Sí | Solo propio. |
| **Admin** | GET | `/api/v1/admin/applicants` | Sí | AdminOnly. |
| **Admin** | PATCH | `/api/v1/admin/technicians/{id}/status` | Sí | AdminOnly. |
| **Admin** | GET | `/api/v1/admin/issues` | Sí | AdminOnly. |
| **Admin** | GET | `/api/v1/admin/dashboard` | Sí | AdminOnly. |
| **Admin** | POST | `/api/v1/admin/jobs/{id}/start` | Sí | AdminOnly. |
| **Admin** | PATCH | `/api/v1/admin/jobs/{id}/status` | Sí | AdminOnly. |
| **Admin** | GET | `/api/v1/admin/metrics` | Sí | AdminOnly. |
| **Admin** | PATCH | `/api/v1/admin/alerts/{id}/resolve` | Sí | AdminOnly. |
| **Admin** | POST | `/api/v1/admin/issues/{id}/resolve` | Sí | AdminOnly. |
| **AiScoring** | POST | `/api/v1/ai-scoring/jobs/{jobId}/rank-technicians` | Sí | AdminOnly. |

**DESCONOCIDO:** No existe endpoint público GET de categorías (`/api/v1/categories`). Las categorías se validan en CreateJob contra `ServiceCategories`; el catálogo se asume existente en BD (migraciones/seed). Para pruebas, revisar migraciones o seed en `AppDbContext`/migrations.

### 4.2 Pantallas / Páginas Web (Razor)

| Ruta / Página | Uso |
|---------------|-----|
| `/` (Index) | Landing. |
| `/Account/Login`, `/Account/Register` | Login y registro (Web llama a API auth). |
| `/Account/Logout` | Cerrar sesión. |
| `/Jobs/Index` | Listado jobs (según rol). |
| `/Jobs/My` | Mis jobs (Customer). |
| `/Jobs/Create` | Crear solicitud (Customer). |
| `/Jobs/Detail` | Detalle de job. |
| `/Requests/New` | Nueva solicitud. |
| `/Requests/My` | Mis solicitudes. |
| `/Requests/Confirmation` | Confirmación tras crear. |
| `/Technician/MyAssignments` | Asignaciones del técnico. |
| `/Technicians/Profile` | Perfil técnico. |
| `/Recruit/Apply` | Postulación como técnico. |
| `/Admin/Dashboard` | Dashboard operativo. |
| `/Admin/Applicants` | Postulantes (técnicos). |
| `/Admin/Issues` | Incidencias. |
| `/Notifications/Index` | Centro de notificaciones. |
| `/Reviews/Create` | Crear reseña. |
| `/Health` | Health check (UI). |
| `/Privacy`, `/Error` | Legales y error. |

Evidencia: `src/FixHub.Web/Pages/**/*.cshtml` y `IFixHubApiClient`.

### 4.3 DTOs principales (request/response) por módulo

| Módulo | Request / Response | Ubicación |
|--------|--------------------|-----------|
| **Auth** | `RegisterRequest` (FullName, Email, Password, Role, Phone?), `LoginRequest` (Email, Password) | Controller. |
| **Auth** | `AuthResponse` (UserId, Email, FullName, Role, Token) | Application. |
| **Jobs** | `CreateJobRequest` (CategoryId, Title, Description, AddressText, Lat?, Lng?, BudgetMin?, BudgetMax?) | Controller. |
| **Jobs** | `JobDto` (Id, CustomerId, CustomerName, CategoryId, CategoryName, Title, Description, AddressText, Lat?, Lng?, Status, BudgetMin?, BudgetMax?, CreatedAt, AssignedTechnicianId?, AssignedTechnicianName?, AssignedAt?, StartedAt?, CompletedAt?, CancelledAt?) | Application/Features/Jobs. |
| **Jobs** | `SubmitProposalRequest` (Price, Message?), `ReportIssueRequest` (Reason, Detail?) | Controller. |
| **Proposals** | `ProposalDto` (Id, JobId, TechnicianId, TechnicianName, Price, Message?, Status, CreatedAt) | Application/Features/Proposals. |
| **Proposals** | `AcceptProposalResponse` (AssignmentId, JobId, ProposalId, TechnicianId, TechnicianName, AcceptedPrice, AcceptedAt) | Application. |
| **Technicians** | `TechnicianProfileDto`, `AssignmentDto` | Application/Features/Technicians. |
| **Reviews** | `CreateReviewRequest` (JobId, Stars, Comment?) | Controller. |
| **Reviews** | `ReviewDto` (Id, JobId, TechnicianId, TechnicianName, Stars, Comment?, CreatedAt) | Application/Features/Reviews. |
| **Notifications** | `NotificationDto` (listado, unread-count, mark read sin body) | Application/Features/Notifications. |
| **Admin** | `UpdateStatusRequest` (Status), `AdminJobStatusRequest` (NewStatus), `ResolveIssueRequest` (ResolutionNote) | Controller. |
| **Admin** | `ApplicantDto`, `IssueDto`, `OpsDashboardDto`, `AdminMetricsDto` | Application/Features/Admin. |
| **Paginación** | `PagedResult<T>` (Items, TotalCount, Page, PageSize) | Application/Common/Models. |

### 4.4 Entidades principales (Domain)

| Entidad | Propósito |
|---------|-----------|
| **User** | Id, Email, PasswordHash, FullName, Role, Phone?, IsActive, CreatedAt. |
| **Job** | Id, CustomerId, CategoryId, Title, Description, AddressText, Lat?, Lng?, Status, BudgetMin?, BudgetMax?, CreatedAt, AssignedAt?, CompletedAt?, CancelledAt?. |
| **ServiceCategory** | Id, Name, Icon?, IsActive. |
| **Proposal** | Id, JobId, TechnicianId, Price, Message?, Status, CreatedAt. |
| **JobAssignment** | Id, JobId, ProposalId, AcceptedAt, StartedAt?, CompletedAt?. |
| **Review** | Id, JobId, CustomerId, TechnicianId, Stars (1–5), Comment?, CreatedAt. |
| **TechnicianProfile** | UserId (PK), Status, Bio?, ServiceRadiusKm, IsVerified, DocumentsJson?, AvgRating, CompletedJobs, CancelRate. |
| **Notification** | Id, UserId, JobId?, Type, Message, IsRead, CreatedAt. |
| **NotificationOutbox** | Id, NotificationId?, Channel, ToEmail, Subject, HtmlBody, Status, Attempts, NextRetryAt?, SentAt?, JobId?, CreatedAt, UpdatedAt. |
| **JobIssue** | Id, JobId, ReportedByUserId, Reason, Detail?, CreatedAt, ResolvedAt?, ResolvedByUserId?, ResolutionNote?. |
| **JobAlert** | Id, JobId, Type, Message, CreatedAt, IsResolved, ResolvedAt?, ResolvedByUserId?. |
| **Payment** | Id, JobId, Amount, Currency, Status, Provider?, ProviderRef?, CreatedAt. |
| **AuditLog** | Auditoría de acciones. |
| **ScoreSnapshot** | Auditoría de scoring AI por técnico/job. |

---

## 5. Flujos principales del negocio

### 5.1 Flujo Customer (happy path)

1. **Registro/Login** → POST `/api/v1/auth/register` (Role=Customer) o `/api/v1/auth/login`.  
2. **Crear solicitud** → POST `/api/v1/jobs` (CreateJobRequest). Categoría debe existir y estar activa; si hay técnico aprobado, el sistema puede auto-asignar (job queda Assigned).  
3. **Ver mis jobs** → GET `/api/v1/jobs/mine`.  
4. **Ver detalle** → GET `/api/v1/jobs/{id}` (solo si es el dueño).  
5. **Ver propuestas** → GET `/api/v1/jobs/{id}/proposals` (actualmente bug: lista vacía para Customer — H04).  
6. **Admin asigna** → POST `/api/v1/proposals/{id}/accept` (solo Admin; no lo hace el Customer).  
7. **Completar** → POST `/api/v1/jobs/{id}/complete` (estado debe ser InProgress o Assigned).  
8. **Calificar** → POST `/api/v1/reviews` (job Completed, una sola review por job).

Flujos alternos: **Cancelar** → POST `/api/v1/jobs/{id}/cancel` (solo Open o Assigned). **Reportar incidencia** → POST `/api/v1/jobs/{id}/issues` (Reason: no_contact | late | bad_service | other).

### 5.2 Flujo Technician (happy path)

1. **Registro** → Role=Technician; se crea `TechnicianProfile` con Status=Pending.  
2. **Aprobación** → Admin cambia estado (PATCH `/api/v1/admin/technicians/{id}/status`) a Approved para poder ser asignado/auto-asignado.  
3. **Ver oportunidades** → GET `/api/v1/jobs` (lista Open + jobs donde tiene propuesta).  
4. **Enviar propuesta** → POST `/api/v1/jobs/{id}/proposals` (job Open, una por job; no puede ser el mismo que el Customer).  
5. **Tras asignación por Admin** → Ver asignaciones: GET `/api/v1/technicians/me/assignments`.  
6. **Iniciar servicio** → POST `/api/v1/jobs/{id}/start` (solo si está asignado; Job status Assigned → InProgress).

No hay endpoint para que el técnico “complete” el job; quien completa es el Customer (o Admin).

### 5.3 Flujo Admin / Empresa

1. **Dashboard** → GET `/api/v1/admin/dashboard` (KPIs, alertas SLA, jobs recientes).  
2. **Listar jobs** → GET `/api/v1/jobs` (todos, filtros status/categoryId).  
3. **Listar propuestas de un job** → GET `/api/v1/jobs/{id}/proposals` (ve todas).  
4. **Asignar técnico** → POST `/api/v1/proposals/{id}/accept` (único rol que puede).  
5. **Forzar inicio** → POST `/api/v1/admin/jobs/{id}/start` (Open/Assigned → InProgress).  
6. **Forzar estado** → PATCH `/api/v1/admin/jobs/{id}/status` (según matriz de transiciones).  
7. **Incidencias** → GET `/api/v1/admin/issues`, POST `/api/v1/admin/issues/{id}/resolve`.  
8. **Alertas SLA** → PATCH `/api/v1/admin/alerts/{id}/resolve`.  
9. **Postulantes** → GET `/api/v1/admin/applicants`, PATCH `/api/v1/admin/technicians/{id}/status`.  
10. **Métricas** → GET `/api/v1/admin/metrics`.  
11. **Ranking técnicos** → POST `/api/v1/ai-scoring/jobs/{jobId}/rank-technicians`.

---

## 6. Estados y reglas de transición

### 6.1 Job (JobStatus)

| Estado | Valor | Descripción |
|--------|--------|-------------|
| Open | 1 | Recién creado; acepta propuestas; puede cancelarse. |
| Assigned | 2 | Admin aceptó una propuesta; hay JobAssignment. |
| InProgress | 3 | Técnico (o Admin) inició el servicio. |
| Completed | 4 | Customer (o Admin) marcó completado. |
| Cancelled | 5 | Cancelado (Customer solo en Open/Assigned). |

**Transiciones permitidas (lógica de negocio):**

| Acción | Quién | Estado origen | Estado destino |
|--------|--------|----------------|----------------|
| Crear job | Customer | — | Open (o Assigned si auto-asign) |
| Accept proposal | Admin | Open | Assigned |
| Start job | Technician (asignado) | Assigned | InProgress |
| Start job (admin) | Admin | Open o Assigned | InProgress |
| Complete | Customer | InProgress, Assigned | Completed |
| Cancel | Customer | Open, Assigned | Cancelled |
| Admin update status | Admin | Open | InProgress, Cancelled |
| Admin update status | Admin | Assigned | InProgress, Cancelled |
| Admin update status | Admin | InProgress | Completed, Cancelled |

Evidencia: `CancelJobCommand`, `CompleteJobCommand`, `TechnicianStartJobCommand`, `StartJobCommand`, `AdminUpdateJobStatusCommand` (AllowedTransitions).

### 6.2 Proposal (ProposalStatus)

| Estado | Descripción |
|--------|-------------|
| Pending | Enviada por técnico; esperando aceptación. |
| Accepted | Aceptada por Admin; genera JobAssignment. |
| Rejected | Rechazada (p. ej. al aceptar otra). |
| Withdrawn | (Definido en enum; uso en handlers no revisado en detalle). |

Reglas: una propuesta por (JobId, TechnicianId); solo job Open acepta propuestas; solo Admin puede aceptar; al aceptar una, las demás del mismo job pasan a Rejected.

### 6.3 TechnicianProfile (TechnicianStatus)

| Estado | Descripción |
|--------|-------------|
| Pending | Recién registrado / postulante. |
| InterviewScheduled | Entrevista programada. |
| Approved | Puede ser asignado / auto-asignado. |
| Rejected | Rechazado. |

### 6.4 NotificationOutbox (OutboxStatus)

| Estado | Descripción |
|--------|-------------|
| Pending | En cola para envío. |
| Processing | Worker tomó el mensaje. |
| Sent | Email enviado. |
| Failed | Fallo tras reintentos. |

---

## 7. Validaciones de negocio clave

| Regla | Dónde | Comportamiento |
|-------|--------|----------------|
| Categoría activa | CreateJobCommand | CategoryId debe existir y IsActive; si no → CATEGORY_NOT_FOUND. |
| Job Open para propuesta | SubmitProposalCommand | Status != Open → JOB_NOT_OPEN. |
| No auto-propuesta | SubmitProposalCommand | CustomerId == TechnicianId → SELF_PROPOSAL. |
| Una propuesta por técnico por job | SubmitProposalCommand | Duplicado (JobId + TechnicianId) → DUPLICATE_PROPOSAL. |
| Solo Admin acepta propuesta | AcceptProposalCommand | !AcceptAsAdmin → FORBIDDEN. |
| Job Open para aceptar | AcceptProposalCommand | Job.Status != Open → JOB_NOT_OPEN. |
| Una asignación por job | AcceptProposalCommand | Ya existe JobAssignment → JOB_ALREADY_ASSIGNED. |
| Cancel: solo Open/Assigned | CancelJobCommand | InProgress/Completed/Cancelled → INVALID_STATUS. |
| Complete: InProgress o Assigned | CompleteJobCommand | Otro estado → INVALID_STATUS. |
| Start: solo asignado | TechnicianStartJobCommand | Assignment.Proposal.TechnicianId != req.TechnicianId → FORBIDDEN; status != Assigned → INVALID_STATUS. |
| Review: job Completed | CreateReviewCommand | Status != Completed → JOB_NOT_COMPLETED. |
| Una review por job | CreateReviewCommand | Ya existe review para JobId → REVIEW_EXISTS. |
| Stars 1–5 | CreateReviewCommandValidator | InclusiveBetween(1, 5). |
| Issue Reason | ReportJobIssueCommandValidator | no_contact, late, bad_service, other. |
| Register Role | RegisterCommandValidator | IsInEnum, != 0; **no excluye Admin** → H03 (registro Admin permitido). |

---

## 8. Restricciones de acceso (ownership)

| Recurso | Customer | Technician | Admin |
|---------|----------|------------|--------|
| Job (GET) | Solo propios | Asignado, Open o con propuesta propia | Todos |
| Job (list) | GET /mine solo; GET / devuelve 403 | GET / (Open + con propuesta) | GET / (todos) |
| Proposals (GET) | **H04:** lista vacía (debería ser solo propio job) | Solo sus propuestas | Todas del job |
| Complete/Cancel/Issues | Solo propio job | N/A (Complete no expuesto a Technician) | Complete/status vía Admin |
| Start | N/A | Solo si asignado | Puede forzar start |
| Accept proposal | No (handler exige Admin) | No | Sí |
| Notifications | Solo las suyas (UserId) | Solo las suyas | Solo las suyas |
| Reviews | Solo puede crear (own job, Completed) | — | — |

---

## 9. Notificaciones (internas y email/outbox)

- **Internas:** Se crean registros en `Notification` (UserId, JobId?, Type, Message, IsRead).  
- **Email:** `NotificationService` escribe en `NotificationOutbox` (ToEmail, Subject, HtmlBody, etc.); un worker (`OutboxEmailSenderHostedService`) procesa cada ~10s y envía vía `IEmailSender` (SendGrid).

**Tipos (NotificationType):** JobCreated, JobAssigned, JobStarted, JobCompleted, JobCancelled, IssueReported, SlaAlert.

**Cuándo se disparan (evidencia en handlers):**

| Evento | Notificación interna | Email (outbox) |
|--------|----------------------|----------------|
| Job created | Customer + Admins | Cliente (JobReceived) |
| Job assigned | Customer + Technician | Cliente (JobAssigned) |
| Job started | Customer (+ Technician si aplica) | Cliente (JobStarted) |
| Job completed | Customer + Technician | Cliente (JobCompleted) |
| Job cancelled | Customer, Technician (si había), Admins | Cliente (JobCancelled) |
| Issue reported | Admins | Admins (IssueReported) |
| SLA alert | Admins | Admins (SlaAlert) |

Lógica de “shouldEmail” en `NotificationService`: para JobCreated/JobAssigned/JobStarted/JobCompleted/JobCancelled el email va al cliente (jobCustomerId); para IssueReported y SlaAlert se envía a los destinatarios (admins).

---

## 10. Alertas SLA (JobSlaMonitor)

Motor en background (~cada 2 min). Umbrales:

| Tipo (JobAlertType) | Condición | Umbral |
|--------------------|-----------|--------|
| OpenTooLong | Job Open sin asignar | CreatedAt &lt; now - 15 min |
| AssignedNotStarted | Job Assigned sin StartedAt | AssignedAt &lt; now - 30 min |
| InProgressTooLong | Job InProgress | StartedAt &lt; now - 3 h |
| IssueUnresolved | Job con issue sin resolver | CreatedAt del issue &lt; now - 1 h |

Se crean `JobAlert` y se notifica a admins. Admin puede resolver con PATCH `/api/v1/admin/alerts/{id}/resolve`.

---

## 11. Dependencias de infraestructura

| Componente | Uso |
|------------|-----|
| **PostgreSQL** | BD principal; ConnectionString vía config/env. |
| **FixHub.Migrator** | Aplica migraciones EF Core (docker-compose o consola). |
| **docker-compose** | fixhub_postgres, fixhub_migrator, fixhub_api, fixhub_web; red fixhub_net. |
| **Variables de entorno** | POSTGRES_DB, POSTGRES_USER, POSTGRES_PASSWORD, JWT_SECRET_KEY (min 32 chars), WEB_ORIGIN; opcional SendGrid para email. |
| **JWT** | SecretKey desde JwtSettings:SecretKey o JWT__SecretKey; Issuer/Audience en config. |
| **Rate limiting** | Global 60 req/min por IP; Auth 10 req/min (AuthPolicy en /auth). |

Evidencia: `FixHub.API/Program.cs`, `docs/AUDIT/00_SYSTEM_MAP.md`, docker-compose.

---

## 12. Tablas de referencia rápida

### A) Endpoints por módulo

| Módulo | Endpoints |
|--------|-----------|
| **Auth** | POST register, POST login. |
| **Jobs** | POST (create), GET (list), GET mine, GET {id}, GET {id}/proposals, POST {id}/proposals, POST {id}/complete, POST {id}/start, POST {id}/cancel, POST {id}/issues. |
| **Proposals** | POST {id}/accept. |
| **Technicians** | GET {id}/profile, GET me/assignments. |
| **Reviews** | POST (create). |
| **Notifications** | GET, GET unread-count, POST {id}/read. |
| **Admin** | GET applicants, PATCH technicians/{id}/status, GET issues, GET dashboard, POST jobs/{id}/start, PATCH jobs/{id}/status, GET metrics, PATCH alerts/{id}/resolve, POST issues/{id}/resolve. |
| **AiScoring** | POST jobs/{jobId}/rank-technicians. |
| **Health** | GET /. |

### B) Roles vs acciones permitidas

| Acción | Customer | Technician | Admin |
|--------|----------|-------------|--------|
| Register/Login | Sí | Sí | Sí (H03: registro Admin permitido) |
| Crear job | Sí | No | No (no necesario) |
| Listar mis jobs | Sí (/mine) | No | No |
| Listar todos los jobs | No (403) | Sí (Open + con propuesta) | Sí |
| Ver job por ID | Solo propio | Asignado/Open/con propuesta | Todos |
| Ver propuestas de un job | **Bug:** vacío | Solo propias | Todas |
| Enviar propuesta | No | Sí (job Open) | No |
| Aceptar propuesta | No (handler rechaza) | No | Sí |
| Iniciar job | No | Sí (si asignado) | Sí (forzar) |
| Completar job | Sí (propio) | No | Sí (forzar estado) |
| Cancelar job | Sí (propio, Open/Assigned) | No | Sí (forzar estado) |
| Reportar incidencia | Sí (propio) | No | Sí |
| Crear review | Sí (job completado) | No | No |
| Dashboard / issues / alerts / applicants / metrics | No | No | Sí |
| Notificaciones (list/read) | Propias | Propias | Propias |

### C) Estados Job y transiciones

| Estado actual | Transición | Quién | Siguiente estado |
|---------------|------------|--------|-------------------|
| — | Crear job | Customer | Open (o Assigned si auto-asign) |
| Open | Accept proposal | Admin | Assigned |
| Assigned | Start | Technician (asignado) o Admin | InProgress |
| InProgress, Assigned | Complete | Customer o Admin | Completed |
| Open, Assigned | Cancel | Customer o Admin | Cancelled |
| Completed, Cancelled | — | — | (terminales) |

---

## 13. Gaps e información DESCONOCIDA

| Tema | Estado | Acción sugerida |
|------|--------|------------------|
| Listado público de categorías | No hay GET /categories en API | Revisar seed/migraciones para IDs válidos en pruebas; o confirmar si la Web obtiene categorías por otro medio. |
| Flujo de pagos | Entidad `Payment` existe; no hay controlador ni endpoints de pago en la API revisada | Buscar en repo referencias a Payment (creación, actualización); si no hay API, marcar “no implementado” para pruebas funcionales. |
| Withdrawn (Proposal) | Enum existe; no se revisó en qué flujo se usa | Grep en Application/Infrastructure para ProposalStatus.Withdrawn. |
| Reprogramación explícita | No se encontró endpoint “reprogramar” o “reschedule” | Considerar como no existente hasta evidencia. |
| Contenido exacto de emails | Plantillas en Infrastructure (PremiumEmailTemplates, NotificationEmailComposer) | Revisar si QA debe validar contenido de correos (asunto, cuerpo). |

---

## 14. Referencias de evidencia

| Documento / Código | Uso |
|--------------------|-----|
| `docs/QA_FUNCTIONAL_REPORT.md` | Flujos, hallazgos H03/H04, score, recomendaciones. |
| `docs/AUDIT/00_SYSTEM_MAP.md` | Mapa de sistema, roles, endpoints, políticas. |
| `docs/AUDIT/FINAL_REPORT.md` | Hallazgos críticos/altos, criterios 100/100. |
| `docs/AUDIT/01_FUNCTIONAL_TESTS.md` | Cobertura E2E, Postman, integration tests. |
| `src/FixHub.API/Controllers/v1/*.cs` | Rutas y políticas por endpoint. |
| `src/FixHub.Application/Features/**/*.cs` | Handlers, validadores, DTOs, reglas de negocio. |
| `src/FixHub.Domain/Entities/*.cs`, `Enums/*.cs` | Modelo de datos y estados. |
| `src/FixHub.Infrastructure/Services/NotificationService.cs`, `JobSlaMonitor.cs` | Notificaciones y SLA. |

---

**Próximo paso (solo cuando este documento esté aprobado):** Generar el plan de pruebas funcionales (matriz de casos TC-001…, happy paths, negativos, alternos, priorización P0/P1/P2, evidencias y entregables Postman + reporte).
