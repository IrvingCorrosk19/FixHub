# FixHub — Auditoría estática de código (Fase 3)

**Branch:** `audit/fixhub-100`  
**Alcance:** Revisión estática (archivo + línea). Sin modificación de código.

---

## 1. Endpoints sin [Authorize] o sin policy donde corresponde

| Ubicación | Estado | Detalle |
|-----------|--------|---------|
| HealthController | OK (intencional) | Sin [Authorize]; health check público. `src/FixHub.API/Controllers/v1/HealthController.cs` línea 7. |
| AuthController | OK (intencional) | Sin [Authorize]; register/login públicos. Rate limit AuthPolicy aplicado. Línea 9. |
| JobsController | OK | [Authorize] en clase; CustomerOnly/TechnicianOnly en acciones según doc. |
| ProposalsController | OK | [Authorize] en clase; aceptar propuesta requiere IsAdmin (handler). |
| NotificationsController | OK | [Authorize]; ownership en handler (UserId). |
| TechniciansController | OK | [Authorize]; TechnicianOnly en GET me/assignments. |
| ReviewsController | OK | [Authorize] + CustomerOnly en POST. |
| AdminController | OK | [Authorize(Policy = "AdminOnly")] en clase. |
| AiScoringController | OK | [Authorize(Policy = "AdminOnly")]. |

**Hallazgo:** Ningún endpoint expuesto requiere [Authorize] y carece de él. GET /api/v1/jobs (lista) no tiene policy a nivel de acción pero el controller devuelve 403 para Customer en código (líneas 60–68 JobsController) — correcto.

---

## 2. Validación de ownership en handlers que reciben IDs

| Handler | ID(s) | Verificación de ownership | Archivo:Línea |
|---------|-------|---------------------------|---------------|
| GetJobQuery | JobId | RequesterRole + CustomerId / TechnicianId / Admin | GetJobQuery.cs:44–66 |
| GetJobProposalsQuery | JobId | **Falta:** no se valida job.CustomerId == RequesterId para Customer. Admin ve todo; Technician solo sus propuestas. | GetJobProposalsQuery.cs:18–41 |
| CancelJobCommand | JobId | job.CustomerId != req.CustomerId → Failure | CancelJobCommand.cs:28 |
| CompleteJobCommand | JobId | job.CustomerId != req.CustomerId → Failure | CompleteJobCommand.cs:28 |
| TechnicianStartJobCommand | JobId | job.Assignment?.Proposal?.TechnicianId != req.TechnicianId → Failure | TechnicianStartJobCommand.cs:37 |
| MarkNotificationReadCommand | NotificationId | query con n.UserId == req.UserId | MarkNotificationReadCommand.cs:16 |
| CreateReviewCommand | JobId | job.CustomerId != req.CustomerId → Failure | CreateReviewCommand.cs:54 |
| ReportJobIssueCommand | JobId | Customer: job.CustomerId == req; Admin: permitido | ReportJobIssueCommand.cs (IsAdmin + ownership) |
| AcceptProposalCommand | ProposalId | Solo AcceptAsAdmin (controller no expone Customer accept) | AcceptProposalCommand.cs:41–42 |
| ResolveJobIssueCommand | IssueId | Admin only (controller policy) | — |
| ResolveJobAlertCommand | AlertId | Admin only | — |
| StartJobCommand (Admin) | JobId | Admin only | — |
| GetMyNotificationsQuery | — | UserId en query | GetMyNotificationsQuery.cs:29 |
| GetMyAssignmentsQuery | — | TechnicianId en query | GetMyAssignmentsQuery.cs:38 |
| ListMyJobsQuery | — | CustomerId en query | ListMyJobsQuery.cs:22 |

**Hallazgo (crítico para IDOR/lógica):** GetJobProposalsQuery no comprueba que el Customer sea dueño del job antes de devolver datos; además para Customer devuelve lista vacía (filtro por TechnicianId). Ver H04 en FINAL_REPORT.

---

## 3. Uso de Include innecesario (performance)

| Ubicación | Include | Observación |
|-----------|--------|-------------|
| GetJobQuery.cs:22–27 | .Include(j => j.Proposals) | Se usa solo para `job.Proposals.Any(p => p.TechnicianId == req.RequesterId)`. Puede sustituirse por `db.Proposals.AnyAsync(p => p.JobId == req.JobId && p.TechnicianId == req.RequesterId, ct)` y eliminar Include de Proposals. |
| ListJobsQuery (Technician) | query.Proposals en Where (Any) | No es Include; EF traduce a EXISTS. OK. |
| GetOpsDashboardQuery | Múltiples Include(j => j.Customer), etc. | Necesarios para DTOs que exponen nombres; aceptable. |
| Otros handlers | Include/ThenInclude para DTOs | Coherentes con datos devueltos. |

**Hallazgo:** Solo GetJobQuery carga la colección Proposals completa cuando basta un booleano. Ver 03_PERFORMANCE_FINDINGS.

---

## 4. FromSqlRaw / SQL raw y seguridad

| Ubicación | Uso | Seguridad |
|-----------|-----|-----------|
| OutboxEmailSenderHostedService.cs:69–76 | FromSqlRaw(..., BatchSize) | Parámetro BatchSize es int de código; resto de la query son columnas y constantes. No hay entrada de usuario. **Aceptable.** Mejora: usar FromSqlInterpolated para consistencia. |

No se encontró ExecuteSqlRaw/FromSqlRaw con concatenación de entrada de usuario.

---

## 5. Validación de inputs (FluentValidation)

| Comando/Query | Validator | Observación |
|---------------|-----------|-------------|
| RegisterCommand | RegisterCommandValidator | FullName, Email, Password (reglas 8+ chars, mayúscula, dígito), Role IsInEnum (no 0). **No rechaza Admin (3).** |
| CreateJobCommand | (implícito por MediatR) | Validator para ListJobsQuery (Page, PageSize). CreateJob: validación en handler (category exists, user exists). |
| ListJobsQuery | ListJobsQueryValidator | Page >= 1, PageSize 1–100. OK. |
| Otros | Varios validadores en Application | FluentValidation registrado en assembly. Revisar que todos los comandos que aceptan IDs o paginación tengan reglas (ej. PageSize máximo). |

**Hallazgo:** RegisterCommandValidator debe rechazar Role == Admin. Ver H03.

---

## 6. Logging de datos sensibles

| Ubicación | Dato logueado | Riesgo |
|-----------|----------------|--------|
| Login.cshtml.cs:34, 44 | Input.Email, Role | PII (email). Aceptable en contexto de auditoría de login; valorar política de retención. |
| OutboxEmailSenderHostedService.cs:105, 119, 125 | ToEmail, OutboxId, JobId | ToEmail es PII. Evitar en producción si el sink de logs es compartido; o redactar. |
| SendGridEmailSender.cs:48 | toEmail | PII. Idem. |
| Resto de logs | JobId, StatusCode, Url | No se encontró log de password ni token en claro. |

**Recomendación:** No loguear contraseñas ni tokens. Emails/toEmail valorar redacción en prod. ExceptionMiddleware: LogError(ex, ...) escribe stack al log; la respuesta al cliente no incluye stack — correcto.

---

## 7. Manejo de errores (stack traces en prod)

| Ubicación | Comportamiento |
|-----------|----------------|
| ExceptionMiddleware.cs:39–54 | catch (Exception ex): respuesta con ProblemDetails genérico (Title, Status, Instance). No se envía stack al cliente. |
| ExceptionMiddleware.cs:41 | logger.LogError(ex, "Unhandled exception") — stack va al log. En producción, asegurar que el sink de logs no exponga stacks a usuarios. |

**Hallazgo:** No se filtra el stack en la respuesta (correcto). Opcional: en Development incluir detalles en ProblemDetails.Extensions; en Production no. Actualmente no se hace distinción por entorno en el middleware.

---

## 8. Configuración de cookies y auth en Web

| Aspecto | Evidencia |
|---------|-----------|
| Cookie auth | FixHub.Web usa cookie "CookieAuth" (fixhub_token). JWT almacenado en cookie; BearerTokenHandler lo envía a la API. |
| HttpOnly / Secure | Revisar Program.cs o AddCookie: valorar HttpOnly, Secure (en prod), SameSite. |
| Antiforgery | Error.cshtml.cs tiene [IgnoreAntiforgeryToken]. Otras páginas (Login, acciones) deben usar antiforgery si cambian estado. |

No se revisó en detalle la configuración de AddCookie (SameSite, Secure). **Declarar en FINAL_REPORT:** Cookie auth en Web no verificada en profundidad; recomendación: HttpOnly, Secure en prod, SameSite Lax o Strict.

---

## 9. DeleteBehavior.Cascade — riesgos

| Relación | Comportamiento | Riesgo |
|----------|----------------|--------|
| User → TechnicianProfile | Cascade | Borrar User borra perfil. Aceptable si el modelo es “usuario único”. |
| Job → Assignment, Review, Payment | Cascade | Borrar Job borra asignación, review, pago. Documentar; valorar Restrict si se requiere historial independiente. |
| Job → JobIssue, JobAlert | Cascade | Idem. |
| Proposal → NotificationOutbox | Cascade | Aceptable para outbox efímero. |
| Notification → User (SetNull) | SetNull | Correcto para no borrar User. |

**Hallazgo:** Múltiples cascadas; no hay soft-delete. Para auditoría/compliance, documentar qué se borra en cascada y asegurar que no se requiera retención. Ver H10 en FINAL_REPORT.

---

## 10. Concurrency tokens y transacciones

| Aspecto | Evidencia |
|---------|-----------|
| Job | JobConfiguration.cs:71–72 UseXminAsConcurrencyToken(). OK. |
| Proposal | ProposalConfiguration.cs:39–40 UseXminAsConcurrencyToken(). OK. |
| Transacciones | IApplicationDbContext.BeginTransactionAsync; OutboxEmailSenderHostedService usa transacción para reservar ítems (FOR UPDATE SKIP LOCKED). OK. |

No se detectaron operaciones críticas sin transacción donde fuera necesario.

---

## 11. Resumen de hallazgos estáticos

| ID | Severidad | Descripción |
|----|-----------|-------------|
| S1 | Alto | GetJobProposalsQuery: falta validación de ownership para Customer (y comportamiento incorrecto). |
| S2 | Crítico | RegisterCommandValidator: permitir Role=Admin. |
| S3 | Medio | GetJobQuery: Include(Proposals) innecesario; usar AnyAsync. |
| S4 | Mejora | FromSqlRaw en Outbox: valorar FromSqlInterpolated. |
| S5 | Mejora | Logging: valorar no loguear ToEmail/email en producción o redactar. |
| S6 | Mejora | ExceptionMiddleware: valorar no incluir detalles de excepción en respuesta en Production. |
| S7 | Medio | Cascadas: documentar y valorar Restrict en relaciones que requieran historial. |

---

**Entregable:** `docs/AUDIT/03_STATIC_REVIEW.md`. Performance: `docs/AUDIT/03_PERFORMANCE_FINDINGS.md`.
