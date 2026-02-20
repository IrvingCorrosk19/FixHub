# REPORTE MAESTRO COMPLETO — FixHub (Fase 1 a Fase 7.2)

**Fecha:** 19 de febrero de 2026  
**Alcance:** Evaluación integral post-implementación de todas las fases  
**Método:** Análisis de código, arquitectura, modelo de datos, seguridad, rendimiento y UX

---

## 1. Resumen Ejecutivo

| Aspecto | Valoración |
|---------|------------|
| **Nivel actual del sistema** | **6.5/10** |
| **Estado** | MVP avanzado con capacidad operativa limitada |
| **Riesgo general** | **MEDIO-ALTO** |

### Diagnóstico

FixHub ha evolucionado desde un marketplace genérico hacia una **empresa de servicios con técnicos propios**. El sistema permite flujo end-to-end: solicitud → asignación → ejecución → cierre, con dashboard operativo y manejo de incidencias. Sin embargo:

- **No es aún producción lista** por brechas de seguridad y auditoría.
- **No es escalable** sin refactor de consultas y caching.
- **Es MVP funcional** que demuestra el modelo de negocio y permite operación manual con pocos técnicos.

### Conclusión

El sistema está en un punto de inflexión: puede usarse para **pruebas piloto con <10 técnicos y <100 solicitudes/día**, pero requiere trabajo de endurecimiento antes de lanzar a producción o buscar inversión.

---

## 2. Evolución por Fase

### FASE 1: Limpieza marketplace → empresa de servicios

| Aspecto | Evaluación |
|---------|------------|
| **Problema resuelto** | Pivote conceptual: FixHub pasa a ser empresa que asigna técnicos, no marketplace abierto |
| **Impacto UX** | Cliente ve "Mis solicitudes" y "Solicitar servicio"; no hay feed público. Mensaje claro: *"FixHub envía técnicos verificados"* |
| **Impacto operación** | Admin asigna técnicos; técnico único aprobado puede auto-asignarse al crear job |
| **Deuda técnica** | API `GET /jobs` sin filtro por rol: Customer podría ver todos los trabajos si llama directamente. La Web redirige pero el contrato API no protege |

---

### FASE 2: Confirmación post-solicitud

| Aspecto | Evaluación |
|---------|------------|
| **Problema resuelto** | Cliente recibe feedback inmediato tras enviar solicitud |
| **Impacto UX** | Página `/Requests/Confirmation` con JobId y enlace a detalle. Reduce ansiedad post-submit |
| **Impacto operación** | Ninguno directo; es puente hacia detalle |
| **Deuda técnica** | Confirmation solo valida que es Customer; no verifica ownership del JobId. Cualquier Customer puede navegar a Confirmation?id={cualquier-job-id} |

---

### FASE 3: Rediseño detalle + stepper

| Aspecto | Evaluación |
|---------|------------|
| **Problema resuelto** | Detalle del trabajo con timeline visual de estados |
| **Impacto UX** | Stepper muestra Open → Assigned → InProgress → Completed. Cliente entiende dónde está su solicitud |
| **Impacto operación** | Admin ve propuestas y asigna; técnico ve acciones disponibles según estado |
| **Deuda técnica** | **CRÍTICO:** `GetJobQuery` no filtra por rol ni ownership. Cualquier usuario autenticado puede ver cualquier job por ID. La Web no restringe acceso a Detail; la API tampoco |

---

### FASE 4: ETA + microinteracciones + UX avanzada

| Aspecto | Evaluación |
|---------|------------|
| **Problema resuelto** | Sensación de seguimiento y estimación temporal |
| **Impacto UX** | ETA mostrada como "~30 min" (hardcodeada), microinteracciones en stepper, badges de técnico |
| **Impacto operación** | Ninguno; ETA es cosmética |
| **Deuda técnica** | ETA no proviene del backend. No hay campo `EstimatedArrival` ni `StartedAt` visible para el cliente. Si el técnico tarda 2h, el cliente sigue viendo "~30 min" |

---

### FASE 5: Mobile-first + mejoras visuales

| Aspecto | Evaluación |
|---------|------------|
| **Problema resuelto** | Interfaz usable en móvil; rate limiting, CORS, headers de seguridad, auditoría |
| **Impacto UX** | CSS responsive, media queries, contenedores adaptativos |
| **Impacto operación** | Rate limiting evita abuso; auditoría permite trazabilidad básica |
| **Deuda técnica** | Auditoría no cubre Cancel, ReportIssue, AdminUpdateStatus, StartJob. Logging estructurado presente pero sin métricas agregadas |

---

### FASE 6: Cancelación + Incidencias persistentes

| Aspecto | Evaluación |
|---------|------------|
| **Problema resuelto** | Cliente puede cancelar (antes de InProgress); cliente/admin pueden reportar incidencias |
| **Impacto UX** | Botones Cancelar y Reportar problema en detalle; página Admin/Issues para listar incidencias |
| **Impacto operación** | Incidencias visibles en Dashboard; permiten reacción operativa |
| **Deuda técnica** | `ReportJobIssueCommand` no valida `Reason` contra whitelist (no_contact, late, bad_service, other). Cualquier string se persiste. JobIssue no tiene estado (ResolvedAt, ResolvedBy): incidencias quedan abiertas indefinidamente |

---

### FASE 7: Dashboard operativo + SLA

| Aspecto | Evaluación |
|---------|------------|
| **Problema resuelto** | Vista operativa con KPIs, alertas SLA, trabajos recientes |
| **Impacto UX** | Admin ve métricas del día, alertas por Open>15min, InProgress>2h, incidencias 24h |
| **Impacto operación** | Permite priorizar asignaciones y detectar cuellos de botella |
| **Deuda técnica** | SLA usa `Job.CreatedAt` para InProgress overdue; debería usar `Assignment.StartedAt`. Cuando Admin usa "Marcar En camino" vía UpdateStatus, `StartedAt` no se establece (usa AdminUpdateJobStatusCommand, no StartJobCommand) |

---

### FASE 7.2: Dashboard accionable + métricas

| Aspecto | Evaluación |
|---------|------------|
| **Problema resuelto** | Acciones inline desde tabla de solicitudes recientes (Marcar En camino, Completada, Cancelar) |
| **Impacto UX** | Admin actúa sin salir del Dashboard; toasts de confirmación |
| **Impacto operación** | Reduce fricción; Admin puede resolver estados desde una sola vista |
| **Deuda técnica** | Misma que Fase 7: UpdateStatus no setea StartedAt. StartJob API existe pero no se usa desde el Dashboard; las acciones inline llaman solo a UpdateStatus |

---

## 3. Arquitectura Global

### ¿Se respeta Clean Architecture?

**Sí, en gran medida.**

- **Domain:** Entidades puras (Job, User, Proposal, JobAssignment, JobIssue, AuditLog). Sin dependencias externas.
- **Application:** CQRS con MediatR, casos de uso en Commands/Queries. Depende solo de `IApplicationDbContext` e interfaces (IAuditService, ICorrelationIdAccessor).
- **Infrastructure:** Implementa EF Core, AppDbContext, migraciones, AuditService.
- **API:** Controllers delgados; delegan en MediatR. No contienen lógica de negocio.
- **Web:** Razor Pages que consumen `IFixHubApiClient`; no acceden a DB directamente.

### ¿La capa Application concentra la lógica?

**Sí.** La lógica de negocio está en handlers (AcceptProposalCommand, CancelJobCommand, CreateJobCommand, etc.). Validación con FluentValidation. Los controllers solo mapean requests a commands y resultados a respuestas HTTP.

### ¿Hay lógica indebida en Web?

**Limitada.** La Web tiene:
- Helpers de presentación (`StatusHelper`, `RelativeTime`, `ReasonLabel`) — aceptable.
- Validación de rol en páginas (Customer/Admin/Technician) — duplicación parcial con API; la API debería ser la fuente de verdad.
- Llamadas secuenciales (GetJob + GetTechnicianProfile + GetProposals) — N+1 en el cliente; no crítico para volumen bajo.

### ¿Domain está limpio?

**Sí.** Entidades anémicas, enums (JobStatus, TechnicianStatus, ProposalStatus). No hay servicios de dominio; la lógica está en Application.

### ¿Infraestructura acoplada?

**Moderadamente.** AppDbContext implementa IApplicationDbContext; las queries usan EF Core directamente en los handlers. Para escalar, habría que introducir repositorios o CQRS con lecturas desacopladas (ej. vistas materializadas).

---

## 4. Modelo de Datos

### ¿Faltan timestamps críticos?

| Entidad | CreatedAt | UpdatedAt | Comentario |
|---------|-----------|-----------|------------|
| Job | ✅ | ❌ | Sin UpdatedAt; no se puede auditar cuándo cambió status por última vez |
| Proposal | ✅ | ❌ | - |
| JobAssignment | AcceptedAt, StartedAt, CompletedAt | - | StartedAt puede quedar null si Admin usa UpdateStatus en vez de StartJob |
| JobIssue | ✅ | ❌ | Sin ResolvedAt/ResolvedBy; no hay workflow de cierre |
| User | ✅ | ❌ | - |

### ¿Faltan eventos históricos?

**Sí.** No existe tabla de historial de estados (JobStatusHistory). Si un job pasa Open→Assigned→InProgress→Completed, no hay registro de cuándo ocurrió cada transición más allá de AcceptedAt/StartedAt/CompletedAt en JobAssignment. Para auditoría y SLA preciso, falta un evento por transición.

### ¿Hay campos que deberían existir?

- **Job:** `UpdatedAt`, `AssignedAt` (redundante con Assignment.AcceptedAt pero útil para queries).
- **JobIssue:** `Status` (Open/InProgress/Resolved), `ResolvedAt`, `ResolvedByUserId`.
- **Job:** `CancelledAt`, `CancelledByUserId` — actualmente CancelJob solo cambia Status.

### ¿Riesgo de inconsistencias?

- **Race en AcceptProposal:** Se verifica con `AnyAsync` que no exista JobAssignment; constraint UNIQUE en DB mitiga. Aceptable.
- **StartedAt null:** Si Admin usa UpdateStatus( aucune formation) para InProgress, StartedAt queda null. Las métricas de SLA que usan CreatedAt para InProgress overdue son aproximadas; si usaran StartedAt, fallarían para esos casos.

---

## 5. Rendimiento

### Análisis de GetOpsDashboardQuery

El handler ejecuta **múltiples consultas**:

1. `todayJobs` — GROUP BY con filtro CreatedAt >= today
2. `issuesLast24h` — COUNT JobIssues
3. `assignmentPairs` — Jobs con Assignment, AcceptedAt últimas 24h
4. `completionPairs` — Jobs con Assignment completado últimas 24h
5. `openOverdueRaw` — Jobs Open, CreatedAt <= threshold
6. `inProgressOverdueRaw` — Jobs InProgress, CreatedAt <= threshold
7. `jobIdsWithIssues` — JobIds con incidencias 24h
8. `issueAlertsRaw` — Jobs por esos ids
9. `recentJobs` — últimos 20 con Include Customer, Category
10. `recentIssues` — últimos 10 con Include Job, ReportedBy

**Problemas:**
- **10+ round-trips a DB** por cada carga del Dashboard.
- **Include innecesarios** en algunas queries: `openOverdueRaw` y `inProgressOverdueRaw` usan `.Include(j => j.Customer)` pero luego `.Select()` proyecta; EF puede optimizar pero el Include es redundante.
- **jobIdsWithIssues + issueAlertsRaw:** dos consultas; podría consolidarse con una subconsulta.

### Riesgo de N+1

- En `recentIssues`, el Select usa `i.Job.Title` y `i.ReportedBy.FullName`; el Include los trae. No hay N+1.
- En `ListJobsQueryHandler`, se hace `ToListAsync()` y luego `Select` en memoria para ToDto; las navigations (Customer, Category) ya están cargadas. Correcto.
- En `GetJobQueryHandler`, Include encadenado (Assignment→Proposal→Technician) evita N+1.

**Veredicto:** Sin N+1 clásicos; sí múltiples consultas secuenciales que suman latencia.

### Consultas pesadas

- `todayJobs` con GroupBy puede ser costoso si hay miles de jobs creados hoy.
- Índices existentes: Status, CustomerId, CategoryId, CreatedAt, (Status, CategoryId). Cubren las consultas del Dashboard.

### Necesidad de caching futura

- **Dashboard:** Candidato a cache de 30–60 segundos. Los KPIs no requieren consistencia inmediata.
- **Listado de jobs (técnico):** Con 1000+ jobs, paginación ya limita; cache menos prioritario.
- **Categorías:** Lista estable; cache de larga duración razonable.

---

## 6. Seguridad

### Validaciones backend reales

| Comando | Validación | Estado |
|---------|------------|--------|
| CreateJobCommand | FluentValidation (CategoryId, Title, Description, etc.) | ✅ |
| CancelJobCommand | Ownership (CustomerId), Status válido | ✅ |
| CompleteJobCommand | Ownership | ✅ (verificado en handler) |
| ReportJobIssueCommand | Ownership o IsAdmin | ✅ |
| AcceptProposalCommand | Solo Admin (AcceptAsAdmin) | ✅ |
| ReportJobIssueCommand | Reason | ❌ Sin whitelist |
| AdminUpdateJobStatusCommand | Transiciones permitidas | ✅ |
| GetJobQuery | Ownership/rol | ❌ Ninguno |
| ListJobsQuery | Rol | ❌ Ninguno |

### Riesgo de ejecución indebida por rol

- **GetJob:** Cualquier autenticado puede obtener cualquier job. **ALTO**.
- **ListJobs:** Customer puede llamar GET /jobs (sin /mine) y ver todos los trabajos. **MEDIO** — la Web usa /mine, pero la API no restringe.
- **GetJobProposals:** Admin ve todas; Technician solo las suyas; Customer no debería ver. El controller pasa `IsAdmin`; si Customer llama, recibe lista vacía (porque la query para no-Admin filtra por TechnicianId). Pero la API no tiene policy específica; depende de que el handler no devuelva datos sensibles. **MEDIO**.

### Protección de endpoints

- `[Authorize(Policy = "CustomerOnly")]` en Create, Mine, Complete, Cancel.
- `[Authorize(Policy = "TechnicianOnly")]` en SubmitProposal.
- `[Authorize(Policy = "AdminOnly")]` en AdminController.
- `[Authorize]` genérico en JobsController para GetById, List, GetProposals, ReportIssue. **GetById y List no filtran por rol.**

### Manejo de ownership

- **CompleteJobCommand, CancelJobCommand, ReportJobIssueCommand:** Verifican `job.CustomerId == req.CustomerId` (o IsAdmin para ReportIssue). Correcto.
- **GetJobQuery:** No verifica. Cualquiera puede ver detalles de cualquier job (incl. dirección, descripción).
- **GetJobProposalsQuery:** Admin ve todo; Technician solo sus propias propuestas. Customer no tiene flujo en Web; si llamara API, el handler para no-Admin filtra por TechnicianId, así que Customer obtendría lista vacía. Pero es frágil: si se añade lógica para "dueño del job", habría que asegurarse de no filtrar mal.

---

## 7. Experiencia Usuario (Cliente)

### Nivel de claridad

- **Alto.** Flujo: Solicitar servicio → Mis solicitudes → Ver detalle. Estados en lenguaje natural (Recibida, Técnico asignado, En camino, Finalizada). Stepper visual refuerza el progreso.
- Página Confirmation da certeza inmediata tras enviar.

### Nivel de confianza

- **Moderado.** Badges de técnico (verificado, trabajos completados) ayudan. Falta: foto, valoraciones visibles en detalle, historial de comunicaciones.
- ETA hardcodeada ("~30 min") puede generar desconfianza si la realidad difiere.

### Puntos de ansiedad restantes

1. **Tiempo de asignación desconocido.** No hay mensaje tipo "Normalmente asignamos en X minutos".
2. **Sin notificaciones.** Cliente debe entrar a la app para ver cambios de estado.
3. **Incidente reportado:** Mensaje "Nuestro equipo se comunicará" pero no hay feedback de seguimiento en la UI.

### Riesgo de abandono

- **Moderado.** Flujo de solicitud es corto; confirmación reduce abandono post-submit. El mayor riesgo está en la espera sin feedback (Open >15–30 min sin explicación).

---

## 8. Experiencia Técnico

### ¿Flujo claro?

- **Sí.** Trabajos → Oportunidades disponibles / Mis asignados / Completados. Puede enviar propuesta desde detalle.
- KPIs (Oportunidades, Asignados, Completados) dan visibilidad rápida.

### ¿Acciones suficientes?

- Enviar propuesta, ver detalle, ver perfil. **No puede** marcar él mismo "En camino" o "Completado"; eso lo hace Admin o Customer (Complete). En el modelo actual (empresa asigna), el técnico es más pasivo; depende de que Admin actualice estados o que el Customer confirme. Para un técnico en sitio, sería útil que pudiera marcar "Llegué" o "Terminé" desde su vista.

### ¿Fricción innecesaria?

- Navegación entre Oportunidades / Asignados / Completados es clara.
- Un técnico con auto-asignación (único Approved) no compite; con varios técnicos, el flujo de propuestas y asignación por Admin añade un paso.

---

## 9. Experiencia Admin

### ¿Puede operar realmente?

- **Sí.** Dashboard con KPIs, alertas SLA, tabla de trabajos recientes con acciones inline (Marcar En camino, Completada, Cancelar).
- Página Issues para ver incidencias.
- Página Applicants para aprobar/rechazar técnicos.
- Detalle de Job para aceptar propuestas y asignar técnicos.

### ¿Falta algo crítico?

1. **Búsqueda de jobs** por ID, cliente o categoría. Solo hay lista reciente + alertas.
2. **Filtros en Issues** por estado, razón, fecha.
3. **Acción "Iniciar" explícita** que use StartJob (y setee StartedAt) en lugar de solo UpdateStatus.
4. **Resolución de incidencias** — no hay forma de marcar un Issue como resuelto.
5. **Métricas históricas** — solo "hoy"; no hay tendencias por semana/mes.

### ¿Faltan métricas clave?

- Tiempo medio de primera asignación (hay).
- Tiempo medio de resolución (hay).
- Tasa de cancelación hoy (hay).
- **Faltan:** satisfacción (reviews), incidencias por técnico, jobs por categoría en el tiempo.

---

## 10. Escalabilidad

### ¿Soporta 10 técnicos?

**Sí.** Con 10 técnicos y decenas de jobs/día, el sistema aguanta. Queries indexadas; Dashboard con 10+ consultas sigue siendo <500 ms en PostgreSQL típico.

### ¿Soporta 1000 solicitudes al día?

**Con reservas.** 
- **Escritura:** Crear jobs, propuestas, asignaciones — carga moderada; OK.
- **Lectura:** Dashboard se invocaría frecuentemente; 10+ queries × N admins podría ser cuello de botella.
- **ListJobs** sin filtro de rol podría devolver miles de registros (aunque paginado); el Count y la query principal sobre jobs crecerían.

### Primer cuello de botella probable

1. **GetOpsDashboardQuery** — múltiples round-trips; sin cache, cada refresh del Dashboard dispara 10+ queries.
2. **ListJobsQuery** — si se usa sin restricción de rol y crece el volumen, el Count y la consulta principal se resienten.
3. **GetJobQuery** con Include profundo — por request es ligero; bajo alto volumen concurrente, la conexión a DB podría saturarse.

### Cambios necesarios para escalar

1. Cache de Dashboard (30–60 s).
2. Restricción de ListJobs por rol (Technician/Admin; Customer solo /mine).
3. Índices compuestos para consultas frecuentes (CreatedAt+Status).
4. Considerar read replicas o vistas materializadas para lecturas pesadas.
5. Rate limiting por usuario además de por IP (evitar un solo usuario malicioso consumir recursos).

---

## 11. Observabilidad

### Logging

- **RequestLoggingMiddleware:** Path, StatusCode, elapsedMs. Sin cuerpos, tokens ni passwords. Adecuado.
- **CorrelationId** en scope; permite rastrear requests.
- **ExceptionMiddleware:** LogError en excepciones no controladas.
- **Faltan:** logs estructurados en handlers (ej. "Job created", "Proposal accepted") para análisis posterior. AuditLog cubre acciones pero no errores de negocio.

### Manejo de errores

- **FluentValidation** → 400 con ValidationProblemDetails.
- **Result<T>** con Failure → controllers traducen a ProblemDetails (400/404/409).
- **Excepciones** → 500 con mensaje genérico.
- No hay códigos de error estandarizados para el cliente (más allá de ErrorCode en algunos Result).

### Trazabilidad

- **CorrelationId** en request/response y en AuditLog. Permite correlacionar una acción con un request.
- **AuditLog:** ActorUserId, Action, EntityType, EntityId, MetadataJson. Cobertura parcial: faltan JOB_CANCEL, REPORT_ISSUE, ADMIN_UPDATE_STATUS, JOB_START.

### Auditoría real

- Tabla `audit_logs` con campos necesarios. Implementación correcta.
- **Gaps:** CancelJob, ReportJobIssue, AdminUpdateJobStatus, StartJob no se auditan. Un administrador podría cambiar estados o un cliente cancelar sin dejar rastro en auditoría.

---

## 12. Riesgos Críticos Actuales

| # | Riesgo | Severidad | Mitigación prioritaria |
|---|--------|-----------|------------------------|
| 1 | GetJob sin filtro de ownership/rol — cualquier usuario ve cualquier job | CRÍTICA | Añadir validación en GetJobQuery o policy que restrinja por rol (Customer solo propios, Technician/Admin según caso) |
| 2 | ListJobs sin filtro de rol — Customer podría listar todos los jobs | ALTA | Restringir GET /jobs a Technician y Admin; Customer solo GET /mine |
| 3 | ReportJobIssue sin validación de Reason — datos sucios, posibles inyecciones en reportes | MEDIA | FluentValidator con whitelist (no_contact, late, bad_service, other) |
| 4 | Auditoría incompleta (Cancel, ReportIssue, AdminUpdate, StartJob) | ALTA | Extender AuditBehavior para esos comandos |
| 5 | Admin "Marcar En camino" no setea StartedAt — métricas SLA incorrectas | MEDIA | Usar StartJob al pasar a InProgress, o que AdminUpdateJobStatus también setee StartedAt cuando NewStatus=InProgress |
| 6 | JobIssue sin workflow de resolución — incidencias abiertas indefinidamente | MEDIA | Añadir Status, ResolvedAt, ResolvedBy y comando ResolveIssue |
| 7 | ETA hardcodeada — expectativas erróneas del cliente | BAJA | Campo opcional EstimatedMinutes o mensaje genérico "Te avisaremos cuando el técnico esté en camino" |

---

## 13. Recomendaciones Prioridad Alta

1. **Implementar filtro de ownership/rol en GetJobQuery.** Customer solo puede ver sus jobs; Technician jobs en los que participa o está abiertos; Admin todos.
2. **Restringir ListJobsQuery** a Technician y Admin. Endpoint GET /jobs sin /mine no debe ser accesible para Customer (policy o verificación en handler).
3. **Completar auditoría:** Añadir casos en AuditBehavior para CancelJobCommand, ReportJobIssueCommand, AdminUpdateJobStatusCommand, StartJobCommand.
4. **Validar Reason en ReportJobIssueCommand** con whitelist. Rechazar con 400 si Reason no está en la lista.
5. **Corregir StartedAt:** En AdminUpdateJobStatusCommand, cuando NewStatus=InProgress y hay Assignment, setear Assignment.StartedAt = UtcNow.

---

## 14. Recomendaciones Prioridad Media

1. **JobIssue con workflow:** Añadir Status (Open/InProgress/Resolved), ResolvedAt, ResolvedByUserId. Comando ResolveJobIssueCommand para Admin.
2. **Job.UpdatedAt:** Actualizar en cada cambio de estado para auditoría y queries.
3. **Cache del Dashboard:** Response cache o memoria 30–60 s para GetOpsDashboardQuery.
4. **Búsqueda de jobs para Admin:** Endpoint o filtros por texto, categoría, rango de fechas.
5. **Filtros en Issues:** Por razón, por estado (si se implementa), por fecha.

---

## 15. Recomendaciones Prioridad Baja

1. **ETA dinámica o mensaje genérico:** Evitar "~30 min" fijo; usar "Te avisaremos cuando el técnico esté en camino" o campo EstimatedArrival opcional.
2. **JobStatusHistory:** Tabla de eventos de cambio de estado para trazabilidad fina.
3. **Métricas históricas en Dashboard:** Gráficas por semana/mes (requiere agregaciones y posiblemente almacenamiento separado).
4. **Notificaciones (email/push):** Cuando el job cambia de estado; reduce ansiedad del cliente.
5. **Acción "Iniciar" explícita para técnico:** Permitir que el técnico asignado marque "En camino" desde su vista.

---

## 16. Evaluación Final

### ¿Está listo para producción?

**No.** Las brechas de seguridad (GetJob/ListJobs sin restricción de ownership/rol) y la auditoría incompleta lo desaconsejan para datos reales de clientes. Aceptable para **piloto interno** con datos de prueba.

### ¿Listo para inversión?

**Parcialmente.** El modelo de negocio está implementado y la UX es coherente. Un inversor querría ver:
- Corrección de los riesgos críticos.
- Métricas de uso real (aunque sean de piloto).
- Plan claro de escalabilidad (cache, índices, posible separación de lecturas).

### ¿Listo para escalar?

**No.** Soporta decenas de usuarios y cientos de jobs/día. Para 1000+ solicitudes/día y múltiples admins refrescando el Dashboard, se necesitan cache y posiblemente optimización de consultas.

### Próxima fase estratégica recomendada

**Fase 8: Endurecimiento y Go-Live**

1. Implementar recomendaciones de prioridad alta (seguridad y auditoría).
2. Tests de integración que validen restricciones de acceso.
3. Ejecutar piloto con 5–10 clientes reales y 2–3 técnicos.
4. Monitorear logs y auditoría durante 2–4 semanas.
5. Tras validación, planificar cache, métricas históricas y notificaciones (Fase 9).

---

*Reporte generado mediante análisis estático del código fuente. Validar en entorno de staging antes de tomar decisiones de go-live.*
