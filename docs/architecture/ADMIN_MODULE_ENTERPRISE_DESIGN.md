# Módulo Administrativo Empresarial — FixHub

**Autor:** Arquitectura de Software Senior (empresa propia, uso interno).  
**Versión:** 1.0  
**Contexto:** FixHub como sistema propio de la empresa (no SaaS, no multi-tenant). Portal clientes + operación interna.

---

## Resumen ejecutivo

Este documento define el **módulo administrativo** para operar FixHub de forma profesional: roles internos, modelo de datos, API admin, flujos operativos, KPIs y roadmap. El diseño es **enterprise pero pragmático**, con auditoría, control de concurrencia y trazabilidad, sin complejidad multi-tenant.

**Convención:** Se usa **Job** (no Order) como entidad principal de trabajo; en UI se puede mostrar como "Solicitud" o "Orden" según contexto.

---

# FASE 1 — ROLES Y PERMISOS (RBAC + Policies)

## 1.1 Roles internos recomendados

| Rol | Descripción | Uso recomendado |
|-----|-------------|------------------|
| **Admin** | Control total: usuarios, técnicos, jobs, disputas, catálogos, auditoría, finanzas. | 1–2 personas (dueño/gerente). |
| **Supervisor** | Operación de campo: reasignar jobs, resolver incidencias, ver métricas y SLA. Sin alta/baja de usuarios ni finanzas. | Jefe de operaciones / coordinador. |
| **SupportAgent** | Soporte: ver jobs, ver incidencias, agregar notas, escalar a Admin. Sin cambiar estados ni reasignar. | Atención al cliente. |
| **Finance** | Pagos, reembolsos, marcar impago/bloqueo. Solo lectura en operación. | Contabilidad. |
| **OpsDispatcher** | Despacho: listar jobs, aceptar propuestas (asignar técnico), reasignar, cambiar estados. Sin gestionar usuarios ni catálogos. | Despachador / coordinador técnicos. |

**Nota:** Admin ya existe en el dominio (`UserRole.Admin`). Los demás son **opcionales** para una primera fase; se pueden introducir gradualmente.

## 1.2 Matriz de permisos (por rol)

| Capacidad | Admin | Supervisor | OpsDispatcher | SupportAgent | Finance |
|-----------|:-----:|:-----------:|:-------------:|:------------:|:-------:|
| Alta/baja clientes y técnicos | ✅ | ❌ | ❌ | ❌ | ❌ |
| Suspender / reactivar / bloquear usuario | ✅ | ❌ | ❌ | ❌ | ❌ |
| Aprobar/rechazar técnicos (TechnicianProfile) | ✅ | ❌ | ❌ | ❌ | ❌ |
| Listar/buscar usuarios | ✅ | 👁 | 👁 | 👁 | 👁 |
| Aceptar propuesta (asignar técnico) | ✅ | ✅ | ✅ | ❌ | ❌ |
| Reasignar job manualmente | ✅ | ✅ | ✅ | ❌ | ❌ |
| Cambiar estado de job (AdminUpdateJobStatus) | ✅ | ✅ | ✅ | ❌ | ❌ |
| Cancelar job (force cancel) | ✅ | ✅ | ✅ | ❌ | ❌ |
| Ver/resolver incidencias (JobIssue) | ✅ | ✅ | ❌ | 👁 + notas | ❌ |
| Abrir/cerrar disputas | ✅ | ✅ | ❌ | ❌ | ❌ |
| Aplicar reembolso / marcar impago | ✅ | ❌ | ❌ | ❌ | ✅ |
| Ver auditoría completa | ✅ | 👁 (propias acciones) | 👁 (propias) | 👁 (lectura) | 👁 (finanzas) |
| CRUD catálogos (categorías, zonas, tarifas) | ✅ | ❌ | ❌ | ❌ | ❌ |
| Dashboard y métricas operativas | ✅ | ✅ | ✅ | 👁 | 👁 (finanzas) |

👁 = solo lectura (o alcance limitado).

## 1.3 Policies de ASP.NET Core

Extensión de `Program.cs` (o `AuthorizationOptions`):

```csharp
// Roles adicionales (opcional): extend UserRole enum o usar claims
// public enum UserRole { Customer = 1, Technician = 2, Admin = 3, Supervisor = 4, SupportAgent = 5, Finance = 6, OpsDispatcher = 7 }

options.AddPolicy("AdminOnly", policy =>
    policy.RequireAuthenticatedUser().RequireRole("Admin"));

options.AddPolicy("OpsOnly", policy =>
    policy.RequireAuthenticatedUser().RequireRole("Admin", "Supervisor", "OpsDispatcher"));

options.AddPolicy("FinanceOnly", policy =>
    policy.RequireAuthenticatedUser().RequireRole("Admin", "Finance"));

options.AddPolicy("SupportOnly", policy =>
    policy.RequireAuthenticatedUser().RequireRole("Admin", "Supervisor", "SupportAgent"));

options.AddPolicy("AdminOrSupervisor", policy =>
    policy.RequireAuthenticatedUser().RequireRole("Admin", "Supervisor"));
```

**Ejemplo en controlador:**

```csharp
[Authorize(Policy = "AdminOnly")]
[HttpGet("users")]
public async Task<IActionResult> ListUsers([FromQuery] UserListRequest req, CancellationToken ct) { ... }

[Authorize(Policy = "OpsOnly")]
[HttpPost("jobs/{jobId:guid}/reassign")]
public async Task<IActionResult> ReassignJob(Guid jobId, [FromBody] ReassignRequest req, CancellationToken ct) { ... }

[Authorize(Policy = "FinanceOnly")]
[HttpPost("payments/{id:guid}/refund")]
public async Task<IActionResult> Refund(Guid id, [FromBody] RefundRequest req, CancellationToken ct) { ... }
```

**Recomendación inicial:** Mantener solo **Admin** y **AdminOnly**; cuando el negocio lo pida, añadir **Supervisor** y **OpsOnly** para reasignación y despacho sin dar acceso a usuarios/finanzas.

---

# FASE 2 — MODELO DE DATOS (DB)

## 2.1 User: estado y suspensión (extender entidad existente)

**User** (actual: `IsActive`, `Role`). Añadir campos para suspensión y auditoría de estado:

| Campo | Tipo | Descripción |
|-------|------|-------------|
| IsSuspended | bool | Suspensión temporal. |
| SuspendedUntil | DateTime? | Fin de suspensión (null = indefinida hasta que Admin reactive). |
| SuspensionReason | string? | Motivo interno (fraude, impago, solicitud, etc.). |
| DeactivatedAt | DateTime? | Baja definitiva (cuenta desactivada). |
| CustomerRiskFlag | bool | Marca de riesgo (impago, disputas). Solo admin/finance. |
| RowVersion | byte[] | Concurrency token (opcional pero recomendado). |

**Nueva tabla: UserStatusHistory** (auditable)

| Columna | Tipo | Descripción |
|---------|------|-------------|
| Id | Guid | PK |
| UserId | Guid | FK User |
| PreviousIsActive | bool | |
| PreviousIsSuspended | bool | |
| NewIsActive | bool | |
| NewIsSuspended | bool | |
| Reason | string? | Motivo del cambio |
| ActorUserId | Guid | Quién hizo el cambio |
| CreatedAtUtc | DateTime | |

**Índices:** `UserId`, `CreatedAtUtc`; `ActorUserId` para auditoría por actor.

## 2.2 Auditoría administrativa (extender AuditLog existente)

**AuditLog** actual: `ActorUserId`, `Action`, `EntityType`, `EntityId`, `MetadataJson`, `CreatedAtUtc`, `CorrelationId`.

Para acciones admin con detalle antes/después, **opción A:** ampliar `MetadataJson` con `BeforeJson`/`AfterJson` en el mismo registro. **Opción B:** tabla separada **AdminAuditLog** para alto volumen y consultas específicas.

**AdminAuditLog** (opcional, si se quiere separar de auditoría genérica):

| Columna | Tipo | Descripción |
|---------|------|-------------|
| Id | Guid | PK |
| ActorUserId | Guid | Quién ejecutó la acción |
| Action | string | Ej: "User.Suspend", "Job.Reassign", "Proposal.Accept" |
| EntityType | string | "User", "Job", "Proposal", "Payment", ... |
| EntityId | Guid? | Id de la entidad afectada |
| BeforeJson | string? | JSON snapshot antes (sin PII sensible). |
| AfterJson | string? | JSON snapshot después. |
| IpAddress | string? | IP del request (desde HttpContext). |
| UserAgent | string? | User-Agent (opcional). |
| CreatedAtUtc | DateTime | |

**Índices:** `ActorUserId`, `EntityType`, `EntityId`, `CreatedAtUtc` (rango por fecha).

## 2.3 Disputas

**Dispute**

| Columna | Tipo | Descripción |
|---------|------|-------------|
| Id | Guid | PK |
| JobId | Guid | FK Job (único: un job una disputa abierta) |
| OpenedByUserId | Guid | Quién abre (Customer o Admin) |
| OpenedAt | DateTime | |
| Reason | string | Código: payment_issue, quality, no_show, other |
| Description | string? | Descripción libre |
| Status | DisputeStatus | Open, InReview, Resolved, Closed |
| ResolvedAt | DateTime? | |
| ResolvedByUserId | Guid? | Admin que resolvió |
| ResolutionNote | string? | Nota interna |
| ResolutionKind | string? | refund_full, refund_partial, no_refund, credit, etc. |

**DisputeMessage** (hilo de mensajes)

| Columna | Tipo | Descripción |
|---------|------|-------------|
| Id | Guid | PK |
| DisputeId | Guid | FK Dispute |
| AuthorUserId | Guid | |
| Body | string | Texto del mensaje |
| IsInternal | bool | Solo visible para admin/soporte |
| CreatedAtUtc | DateTime | |

**DisputeStatus (enum):** Open, InReview, Resolved, Closed.

**Índices:** Dispute(JobId, Status), DisputeMessage(DisputeId, CreatedAtUtc).

## 2.4 Reasignación / despacho

**AssignmentOverride** (reasignación manual por admin/ops)

| Columna | Tipo | Descripción |
|---------|------|-------------|
| Id | Guid | PK |
| JobId | Guid | FK Job (único por job: una reasignación activa) |
| FromTechnicianId | Guid? | Técnico anterior (null si era auto-asignación sin asignado) |
| ToTechnicianId | Guid | Nuevo técnico |
| Reason | string | Código: no_show, customer_request, performance, other |
| ReasonDetail | string? | Texto libre |
| AdminUserId | Guid | Quién reasignó |
| CreatedAtUtc | DateTime | |

**Relación:** Job puede tener un `JobAssignment` actual; al reasignar se puede crear un nuevo Assignment y marcar el anterior como reemplazado (o guardar solo en AssignmentOverride y dejar Assignment como “fuente de verdad” del técnico actual). Recomendación: al reasignar, crear nuevo `JobAssignment` con el nuevo técnico y dejar `AssignmentOverride` como registro de auditoría.

**Índices:** JobId, AdminUserId, CreatedAtUtc.

## 2.5 Finanzas / impago (extender Payment y User)

**Payment** (existente): `JobId`, `Amount`, `Currency`, `Status`, `Provider`, `ProviderRef`, `CreatedAt`.

Añadir (o usar tabla **Refund**):

| Refund (nueva tabla) | Tipo | Descripción |
|----------------------|------|-------------|
| Id | Guid | PK |
| PaymentId | Guid | FK Payment |
| Amount | decimal | Monto reembolsado |
| Reason | string | refund_request, dispute, no_show, other |
| Status | RefundStatus | Pending, Completed, Rejected |
| RequestedByUserId | Guid? | Admin o sistema |
| ProcessedAt | DateTime? | |
| CreatedAtUtc | DateTime | |

**User:** ya propuesto `CustomerRiskFlag`. Opcional: `BlockedForNonPaymentUntil` (DateTime?) para bloqueo por impago con fecha de revisión.

**PaymentStatus** (existente): Pending, Completed, Failed, Refunded. Suficiente; Refund registra el reembolso.

## 2.6 Catálogos

**ServiceCategory** (existente): mantener. Si se requiere jerarquía o zonas:

**Zone** (nueva, opcional)

| Columna | Tipo | Descripción |
|---------|------|-------------|
| Id | int | PK |
| Name | string | Ej: "Zona Norte", "Centro" |
| Code | string | Único |
| IsActive | bool | |
| RowVersion | byte[] | Concurrency |

**PricingRule** (opcional, para reglas por categoría/zona)

| Columna | Tipo | Descripción |
|---------|------|-------------|
| Id | Guid | PK |
| ServiceCategoryId | int | FK |
| ZoneId | int? | FK null = global |
| MinAmount | decimal? | |
| MaxAmount | decimal? | |
| EffectiveFrom | DateTime | |
| EffectiveTo | DateTime? | |
| RowVersion | byte[] | |

**Control de concurrencia:** En entidades que se actualizan por admin (User, Zone, PricingRule, ServiceCategory), usar `RowVersion` (byte[]) y `IsConcurrencyToken()` en EF Core para actualizaciones optimistas.

---

# FASE 3 — ENDPOINTS ADMIN (API)

Prefijo base: **`/api/v1/admin`**. Todos requieren autenticación y policy adecuada.

## 3.1 Usuarios

| Método | Ruta | Policy | Descripción |
|--------|------|--------|-------------|
| GET | /admin/users | AdminOnly | Listar/buscar usuarios (paginated). Query: role, isActive, isSuspended, q (search). |
| GET | /admin/users/{id} | AdminOnly | Detalle usuario + historial de estado. |
| POST | /admin/users/{id}/activate | AdminOnly | Activar cuenta (IsActive = true, quitar suspensión si aplica). |
| POST | /admin/users/{id}/deactivate | AdminOnly | Desactivar cuenta (IsActive = false, DeactivatedAt = now). |
| POST | /admin/users/{id}/suspend | AdminOnly | Suspender (IsSuspended = true, SuspendedUntil, SuspensionReason). |
| POST | /admin/users/{id}/unsuspend | AdminOnly | Reactivar (IsSuspended = false). |
| PATCH | /admin/users/{id}/risk-flag | FinanceOnly | Marcar/desmarcar CustomerRiskFlag. |

**Ejemplo request/response**

```http
POST /api/v1/admin/users/550e8400-e29b-41d4-a716-446655440000/suspend
Content-Type: application/json

{
  "suspendedUntil": "2026-03-15T23:59:59Z",
  "reason": "Impago reiterado"
}

Response: 200 OK
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "isSuspended": true,
  "suspendedUntil": "2026-03-15T23:59:59Z",
  "message": "Usuario suspendido"
}
```

## 3.2 Técnicos

| Método | Ruta | Policy | Descripción |
|--------|------|--------|-------------|
| GET | /admin/technicians | AdminOnly | Listar perfiles técnicos (status, zona si existe). |
| GET | /admin/technicians/{id} | AdminOnly | Detalle + jobs completados, rating, incidencias. |
| POST | /admin/technicians/{id}/approve | AdminOnly | TechnicianStatus = Approved. |
| POST | /admin/technicians/{id}/reject | AdminOnly | TechnicianStatus = Rejected. |
| POST | /admin/technicians/{id}/suspend | AdminOnly | Suspender técnico (User.IsSuspended + motivo). |

## 3.3 Jobs (órdenes/solicitudes)

| Método | Ruta | Policy | Descripción |
|--------|------|--------|-------------|
| GET | /admin/jobs | OpsOnly | Listar jobs (filtros: status, categoryId, dateFrom, dateTo, customerId). Paginated. |
| GET | /admin/jobs/{id} | OpsOnly | Detalle job + propuestas + assignment + incidencias + disputa si existe. |
| POST | /admin/jobs/{id}/status | OpsOnly | Cambiar estado (AdminUpdateJobStatus existente). |
| POST | /admin/jobs/{id}/cancel | OpsOnly | Cancelación forzada (motivo admin). |
| POST | /admin/jobs/{id}/reassign | OpsOnly | Reasignar a otro técnico (crear AssignmentOverride + nuevo Assignment si aplica). |
| POST | /admin/jobs/{id}/flag-dispute | OpsOnly | Abrir o vincular disputa. |

**Ejemplo reassign**

```http
POST /api/v1/admin/jobs/{jobId}/reassign
{
  "toTechnicianId": "guid-del-nuevo-tecnico",
  "reason": "no_show",
  "reasonDetail": "Técnico no se presentó en ventana acordada"
}

Response: 200 OK
{
  "jobId": "...",
  "assignmentId": "...",
  "previousTechnicianId": "...",
  "newTechnicianId": "..."
}
```

## 3.4 Disputas

| Método | Ruta | Policy | Descripción |
|--------|------|--------|-------------|
| GET | /admin/disputes | AdminOrSupervisor | Listar (status, jobId). |
| GET | /admin/disputes/{id} | AdminOrSupervisor | Detalle + mensajes. |
| POST | /admin/disputes | AdminOrSupervisor | Abrir disputa (jobId, reason, description). |
| POST | /admin/disputes/{id}/messages | AdminOrSupervisor | Añadir mensaje (body, isInternal). |
| POST | /admin/disputes/{id}/resolve | AdminOrSupervisor | Resolver (resolutionKind, resolutionNote). |

## 3.5 Auditoría

| Método | Ruta | Policy | Descripción |
|--------|------|--------|-------------|
| GET | /admin/audit | AdminOnly | Query logs. Query: actorUserId, entityType, entityId, from, to, action. Paginated. |

**Response:** Lista de `AdminAuditLog` o `AuditLog` con MetadataJson; sin PII en respuestas.

## 3.6 Catálogos

| Método | Ruta | Policy | Descripción |
|--------|------|--------|-------------|
| GET | /admin/catalogs/categories | AdminOnly | Listar categorías (ya existe ServiceCategory). |
| PUT | /admin/catalogs/categories/{id} | AdminOnly | Actualizar categoría (nombre, activo). |
| GET/POST/PUT/DELETE | /admin/catalogs/zones | AdminOnly | CRUD zonas (si se implementa Zone). |
| GET/POST/PUT/DELETE | /admin/catalogs/pricing-rules | AdminOnly | CRUD reglas de tarifa (si se implementa). |

**Códigos HTTP:** 200 OK, 201 Created, 400 Bad Request (validación), 403 Forbidden (policy), 404 Not Found, 409 Conflict (concurrency RowVersion).

---

# FASE 4 — FLUJOS OPERATIVOS REALES

## 4.1 Cliente conflictivo → suspensión + evidencia + auditoría

1. Admin/Soporte detecta conflicto (incidencias reiteradas, disputas, abuso).
2. Admin revisa historial: jobs, JobIssues, Disputes, UserStatusHistory.
3. Admin ejecuta `POST /admin/users/{id}/suspend` con `reason` y `suspendedUntil`.
4. Backend: actualizar User (IsSuspended, SuspensionReason, SuspendedUntil); insertar **UserStatusHistory**; escribir **AdminAuditLog** (action: User.Suspend, before/after).
5. Notificación opcional al usuario (email o in-app): "Tu cuenta ha sido suspendida hasta...".
6. Para reactivar: `POST /admin/users/{id}/unsuspend`; nuevo registro en UserStatusHistory y AuditLog.

**Reglas de negocio:** No permitir login si `IsActive == false` o (`IsSuspended == true` y `SuspendedUntil` > now o null). Mostrar mensaje claro en login.

## 4.2 Técnico no se presenta → reasignación manual + penalización

1. Cliente reporta incidencia "no_show" (JobIssue) o Admin lo detecta por SLA (AssignedNotStarted).
2. Admin/OpsDispatcher abre detalle del job; ve propuesta aceptada y técnico asignado.
3. Admin/Ops ejecuta `POST /admin/jobs/{id}/reassign` con `toTechnicianId` (otro técnico) y `reason: "no_show"`.
4. Backend: crear **AssignmentOverride**; crear nuevo **JobAssignment** para el nuevo técnico; marcar propuesta original como Rejected o mantener y crear nuevo Assignment; actualizar Job.AssignedAt si se considera “nueva asignación”. Registrar en **AdminAuditLog**.
5. Notificar al nuevo técnico; opcional: notificar al técnico anterior (penalización interna o revisión de perfil).
6. (Opcional) Incrementar contador de “no_show” en TechnicianProfile para métricas de desempeño.

## 4.3 Cancelación por el cliente → reglas de reembolso

1. Cliente cancela job (CancelJobCommand) en estado Open o Assigned.
2. Backend: Job.Status = Cancelled, Job.CancelledAt = now. Si había pago previo, según política:
   - **Open:** sin cobro → sin reembolso.
   - **Assigned/InProgress:** si hay Payment.Completed, aplicar regla (ej. reembolso 100% si cancelación > 24h; 50% si < 24h; 0% si ya iniciado).
3. Si aplica reembolso: crear **Refund** (PaymentId, Amount, Reason = refund_request); actualizar Payment.Status a Refunded (o dejar Completed y Refund como registro aparte). Finance o Admin ejecuta `POST /admin/payments/{id}/refund`.
4. Registrar en AuditLog; opcional CustomerRiskFlag si cancelaciones reiteradas.

**Estados sugeridos:** PaymentStatus: Pending, Completed, Failed, Refunded. RefundStatus: Pending, Completed, Rejected.

## 4.4 Disputa → investigación → resolución

1. Cliente o Admin abre disputa: `POST /admin/disputes` (jobId, reason, description). Dispute.Status = Open.
2. Admin/Supervisor revisa: mensajes en DisputeMessage; Job, Payment, JobIssue, Review.
3. Se agregan mensajes internos (`isInternal: true`) para notas de investigación.
4. Decisión: `POST /admin/disputes/{id}/resolve` con resolutionKind (refund_full, refund_partial, no_refund, credit) y resolutionNote.
5. Backend: Dispute.Status = Resolved, ResolvedAt, ResolvedByUserId. Si hay reembolso, crear Refund y actualizar Payment. AuditLog.
6. Notificar al cliente (y opcional al técnico) con resultado.

**DisputeStatus:** Open → InReview → Resolved → Closed (opcional).

## 4.5 Impago → bloqueo automático + desbloqueo por finanzas

1. Proceso batch o manual: si Payment fallido o vencido y política de “impago” (ej. 2+ facturas vencidas), marcar User.CustomerRiskFlag = true y/o User.IsSuspended = true, SuspensionReason = "Impago".
2. Registrar UserStatusHistory y AuditLog.
3. Cliente no puede crear nuevos jobs hasta regularizar (validación en CreateJob).
4. Finance/Admin revisa; si pago recibido: desmarcar risk, `POST /admin/users/{id}/unsuspend` si estaba suspendido. Nuevo registro en UserStatusHistory.

**Estados:** User.IsActive, User.IsSuspended, User.CustomerRiskFlag. No hace falta PaymentStatus en User; Payment sigue en su tabla.

---

# FASE 5 — DASHBOARD Y MÉTRICAS

## 5.1 KPIs para operación interna

| KPI | Descripción | Agregación sugerida |
|-----|-------------|----------------------|
| Jobs en curso | Count(Job) donde Status in (Assigned, InProgress) | Por zona/categoría si existe Zone. |
| Jobs atrasados (SLA) | Assigned sin StartedAt en X min, o InProgress > Y horas | JobAlert ya existe; exponer count en dashboard. |
| Tasa de cancelación | % jobs Cancelled vs total (por periodo) | Por cliente, por técnico, por categoría. |
| Tasa de reasignación | Count(AssignmentOverride) / Count(JobAssignment) en periodo. | |
| Ingresos | Sum(Payment.Amount) donde Status = Completed, CreatedAt en rango | Diario/semanal/mensual. |
| Disputas abiertas | Count(Dispute) donde Status = Open o InReview. | |
| Técnicos activos/suspendidos | Count(User) por Role=Technician y IsSuspended = false/true. | |
| Tiempo medio Open → Assigned | Promedio (AssignedAt - CreatedAt) en periodo. | GetAdminMetricsQuery ya tiene algo similar. |
| Tiempo medio Assigned → Completed | Promedio (CompletedAt - AssignedAt). | Idem. |

## 5.2 Consultas y caché

- **Dashboard:** Un endpoint `GET /admin/dashboard` que devuelva un DTO con los KPIs anteriores (por día/hoy/semana). Fuente: consultas EF Core con filtros de fecha.
- **Caché:** Cachear resultado del dashboard 1–5 minutos (IMemoryCache o Redis) para no golpear la DB en cada refresh. Invalidar al crear/actualizar Job, Payment, Dispute, User status.
- **Sin sobre-ingeniería:** Evitar tablas de agregados precalculadas en MVP; calcular con queries. Si el volumen crece, considerar vistas materializadas o jobs nocturnos que rellenen tablas de resumen.

---

# FASE 6 — ROADMAP DE IMPLEMENTACIÓN

## Fase A — MVP Admin (4–6 semanas)

- **Roles:** Mantener solo Admin; policy AdminOnly en todos los endpoints admin.
- **User:** Añadir IsSuspended, SuspendedUntil, SuspensionReason, DeactivatedAt; UserStatusHistory; endpoints activate/deactivate/suspend/unsuspend.
- **Auditoría:** Usar AuditLog existente; en cada acción admin escribir Action, EntityType, EntityId, MetadataJson (con before/after si hace falta). Opcional: AdminAuditLog con IpAddress/UserAgent.
- **Endpoints:** GET/POST /admin/users (list, suspend, unsuspend, activate, deactivate). GET /admin/audit (query por actor, entity, fecha).
- **UI Admin:** Página “Usuarios” (listado + detalle + botones suspender/reactivar); página “Auditoría” (tabla filtrable).

## Fase B — Reasignación + Disputas (3–4 semanas)

- **AssignmentOverride:** Tabla + migración; endpoint POST /admin/jobs/{id}/reassign. Integrar con JobAssignment (crear nuevo assignment al reasignar).
- **Dispute + DisputeMessage:** Tablas + enums; endpoints CRUD disputas y mensajes; resolver con resolutionKind.
- **Policies:** Introducir OpsOnly (Admin + Supervisor si se añade rol Supervisor); proteger reassign y status.
- **UI:** Detalle de job con “Reasignar” y “Abrir disputa”; listado de disputas; resolución con nota.

## Fase C — Finanzas + Reembolsos + Antifraude (2–3 semanas)

- **Refund:** Tabla Refund; endpoint POST /admin/payments/{id}/refund (FinanceOnly). User.CustomerRiskFlag; PATCH /admin/users/{id}/risk-flag.
- **Reglas de reembolso:** En CancelJobCommand (o servicio de dominio): si hay pago completado, aplicar política (configurable o en código) y crear Refund pendiente o completado.
- **Bloqueo por impago:** Lógica en batch o manual que setea IsSuspended + CustomerRiskFlag; validación en CreateJob para no permitir jobs si User bloqueado.
- **Policy FinanceOnly:** Admin + Finance.

## Fase D — Dashboards + Alertas (2–3 semanas)

- **GET /admin/dashboard:** DTO con KPIs (jobs en curso, atrasados, cancelaciones, ingresos, disputas abiertas, técnicos activos/suspendidos).
- **Caché:** IMemoryCache 2–5 min; invalidación en eventos relevantes.
- **Alertas:** Reutilizar JobAlert y SLA existente; exponer en dashboard “Incidencia: AssignedNotStarted”, “InProgress > 3h”.
- **UI:** Dashboard con tarjetas y gráficos simples (Chart.js o similar).

---

# FASE 7 — RIESGOS Y MITIGACIONES

| Riesgo | Mitigación |
|--------|------------|
| **Abuso de rol Admin** | Mínimo número de admins; auditoría obligatoria de todas las acciones admin; revisión periódica de AdminAuditLog. |
| **Pérdida de integridad (doble reasignación, estados inconsistentes)** | Transacciones en reasignación (crear AssignmentOverride + nuevo JobAssignment en una transacción); validaciones de estado (ej. no reasignar si Job ya Completed). Concurrency con RowVersion en User y catálogos. |
| **Concurrencia (dos admins editan el mismo usuario)** | RowVersion en User; 409 Conflict si el token no coincide; cliente debe refrescar y re-intentar. |
| **PII en auditoría** | No guardar password, tokens ni email en BeforeJson/AfterJson si se exponen; anonimizar o truncar en logs. AuditLog ya sin PII según comentario en entidad. |
| **Acceso no autorizado a endpoints admin** | Todas las rutas bajo /admin con [Authorize(Policy = "AdminOnly")] o policy más restrictiva; validar en cada handler que el actor tiene el rol correcto para la acción. |
| **Disputas y reembolsos sin trazabilidad** | Siempre crear Refund con RequestedByUserId; Dispute con ResolvedByUserId y ResolutionNote; todo en AdminAuditLog. |
| **Escalabilidad por sucursales/zonas** | Zone y PricingRule permiten filtrar y reportar por zona; si más adelante se añaden “Sucursal” o “Region”, se puede añadir FK en Job y en User sin cambiar el modelo multi-tenant (sigue siendo un solo tenant por empresa). |

---

## Resumen de entregables

| # | Contenido |
|---|-----------|
| 1 | **Roles y policies:** Admin, Supervisor, OpsDispatcher, SupportAgent, Finance; políticas AdminOnly, OpsOnly, FinanceOnly, SupportOnly, AdminOrSupervisor. |
| 2 | **Tablas y relaciones:** User (extendido), UserStatusHistory, AdminAuditLog (opcional), Dispute, DisputeMessage, AssignmentOverride, Refund, Zone, PricingRule; índices y RowVersion. |
| 3 | **Endpoints:** /admin/users, /admin/technicians, /admin/jobs (reassign, cancel, flag-dispute), /admin/disputes, /admin/audit, /admin/catalogs. |
| 4 | **Flujos:** Cliente conflictivo (suspensión), técnico no show (reasignación), cancelación (reembolso), disputa (resolución), impago (bloqueo). |
| 5 | **KPIs:** Jobs en curso/atrasados, tasas cancelación/reasignación, ingresos, disputas abiertas, técnicos activos/suspendidos, tiempos medios; dashboard con caché. |
| 6 | **Roadmap:** Fase A (MVP admin + auditoría), B (reasignación + disputas), C (finanzas + reembolsos), D (dashboards + alertas). |
| 7 | **Riesgos:** Abuso admin, integridad, concurrencia, PII en auditoría, acceso no autorizado, trazabilidad; mitigaciones por tabla. |

Este documento sirve como especificación de diseño para el módulo administrativo empresarial de FixHub. Implementar en el orden del roadmap y ajustar políticas y entidades según la evolución real del negocio.
