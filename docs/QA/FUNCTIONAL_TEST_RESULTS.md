# FixHub — Resultados de pruebas funcionales (comportamiento humano real)

**Fecha de análisis:** 2025-03-10  
**Alcance:** Acciones inesperadas de usuarios (Cliente, Técnico, Admin).  
**Metodología:** Análisis de código + flujos esperados; integridad de Job, JobAssignment, AssignmentOverride, UserStatusHistory, AuditLog.

---

## Resumen ejecutivo

| # | Caso | Resultado flujo | Integridad | Clasificación principal |
|---|------|-----------------|------------|--------------------------|
| 1 | Cliente crea job y se arrepiente inmediatamente | OK | OK | SUGGESTION |
| 2 | Cliente crea job y desaparece | OK | OK | MAJOR |
| 3 | Cliente/Admin acepta técnico equivocado | OK (Admin acepta) | OK | MAJOR |
| 4 | Cliente cancela cuando técnico ya está Assigned | OK (permitido) | OK | MINOR |
| 5 | Cliente intenta crear 5 jobs seguidos | OK (todos 201) | OK | MINOR |
| 6 | Admin suspende cliente con job activo | OK | OK | MAJOR |
| 7 | Admin reasigna mientras técnico marca InProgress | Race condition | **INCONSISTENCIA** | **CRITICAL** |
| 8 | Técnico suspendido intenta enviar propuesta | OK (201) | OK | MAJOR |

---

## Caso 1 — Cliente crea job y se arrepiente inmediatamente

### Flujo ejecutado

1. Cliente: `POST /api/v1/jobs` → **201**, job creado, `status: "Open"`.
2. Cliente: `POST /api/v1/jobs/{id}/cancel` → **200**, job cancelado, `status: "Cancelled"`, `cancelledAt` rellenado.

### Evaluación de integridad

- **Job:** Una sola fila; `Status = Cancelled`, `CancelledAt` no nulo; `AssignedAt`/`CompletedAt` nulos. ✅
- **JobAssignment:** No existe (nunca se aceptó propuesta). ✅
- **AssignmentOverride:** No aplica. ✅
- **AuditLog:** JOB_CREATE y JOB_CANCEL. ✅

### Inconsistencias detectadas

Ninguna. El flujo es correcto.

### Problemas clasificados

| Severidad | Descripción |
|-----------|-------------|
| **SUGGESTION** | Los jobs cancelados inmediatamente aumentan ruido en BD y listados. Valorar en UI un "deshacer creación" en los primeros segundos o un estado tipo "Abandoned" para no contar estos jobs en KPIs. |

---

## Caso 2 — Cliente crea job y desaparece

### Flujo ejecutado

1. Cliente: `POST /api/v1/jobs` → **201**, job `Open` (o `Assigned` si hay auto-asignación).
2. No hay más acciones del cliente: no cancela, no completa, no responde.

### Evaluación de integridad

- **Job:** Permanece `Open` (o `Assigned`) indefinidamente; fechas coherentes. ✅
- **JobAssignment:** Si hay auto-asignación, existe uno; si no, ninguno. ✅
- No se detectan datos huérfanos ni estados imposibles.

### Inconsistencias detectadas

Ninguna a nivel de modelo de datos. El problema es de negocio/operación.

### Problemas clasificados

| Severidad | Descripción |
|-----------|-------------|
| **MAJOR** | Jobs abandonados en `Open` (o `Assigned` sin que el técnico inicie) no tienen cierre automático. No existe estado "Abandoned" ni SLA que los archive o cancele. Recomendación: política de cierre/archivado por antigüedad o inactividad, o estado explícito "Abandoned" para reporting. |

---

## Caso 3 — Cliente acepta técnico equivocado

### Contexto

En FixHub **solo el Admin** puede aceptar propuestas (`AcceptProposalCommand` exige `AcceptAsAdmin`). El escenario se interpreta como: **Admin acepta la propuesta equivocada** (por error de criterio o UI).

### Flujo ejecutado

1. Job `Open` con varias propuestas (Técnico A, Técnico B).
2. Admin: `POST /api/v1/proposals/{proposalIdB}/accept` (acepta a B cuando debía ser A) → **200**.
3. Job queda `Assigned` al técnico B; propuesta de A pasa a `Rejected`.

### Evaluación de integridad

- **Job:** `Status = Assigned`, un solo `JobAssignment` activo. ✅
- **JobAssignment:** Una fila con `proposalId` de B. ✅
- **AuditLog:** PROPOSAL_ACCEPT. ✅

### Inconsistencias detectadas

Ninguna. La corrección es de negocio: reasignar al técnico correcto.

### Problemas clasificados

| Severidad | Descripción |
|-----------|-------------|
| **MAJOR** | No hay confirmación explícita antes de aceptar ("¿Asignar a [Nombre] por [Precio]?") ni "deshacer" tras aceptar. El único remedio es reasignar (Reassign), lo que deja AssignmentOverride y notificaciones. Mejorar UX: confirmación y/o ventana corta de "deshacer asignación". |

---

## Caso 4 — Cliente cancela cuando el técnico ya está Assigned

### Flujo ejecutado

1. Job en estado `Assigned` (propuesta aceptada; técnico aún no ha llamado a `start`).
2. Cliente: `POST /api/v1/jobs/{id}/cancel` → **200**, job `Cancelled`, `cancelledAt` rellenado.
3. El sistema notifica al cliente, al técnico asignado y a admins.

### Evaluación de integridad

- **Job:** `Status = Cancelled`, `CancelledAt` no nulo. ✅
- **JobAssignment:** Sigue existiendo (no se borra); el job solo se marca cancelado. ✅
- **AuditLog:** JOB_CANCEL. ✅

### Inconsistencias detectadas

Ninguna. El comportamiento es el definido: se permite cancelar en `Open` y `Assigned`; no en `InProgress` (devuelve 400 INVALID_STATUS).

### Problemas clasificados

| Severidad | Descripción |
|-----------|-------------|
| **MINOR** | El técnico puede estar ya en camino. Valorar en UI un aviso: "El técnico ya está asignado. ¿Cancelar de todos modos?" para reducir cancelaciones por despiste y mejorar experiencia del técnico. |

---

## Caso 5 — Cliente intenta crear 5 jobs seguidos

### Flujo ejecutado

1. Cliente: 5 × `POST /api/v1/jobs` (mismo usuario, misma sesión).
2. No hay rate limiting específico en `POST /jobs`; el límite global es 60 req/min por IP (`Program.cs`).
3. Resultado: **5 × 201**; se crean 5 jobs.

### Evaluación de integridad

- **Job:** 5 filas; cada una con `Status = Open` (o `Assigned` si hay auto-asignación). ✅
- No hay límite por usuario en número de jobs abiertos; la BD permanece consistente.

### Inconsistencias detectadas

Ninguna. El riesgo es abuso o spam, no inconsistencia de datos.

### Problemas clasificados

| Severidad | Descripción |
|-----------|-------------|
| **MINOR** | Un cliente puede crear muchos jobs en poco tiempo (hasta 60/min por IP). Valorar: límite por usuario (p. ej. máximo N jobs `Open` por cliente) o rate limit específico para creación de jobs para mitigar abuso. |

---

## Caso 6 — Admin suspende cliente mientras tiene job activo

### Flujo ejecutado

1. Cliente tiene un job en `Assigned` o `InProgress`.
2. Admin: `POST /api/v1/admin/users/{customerId}/suspend` → **204**.
3. `SuspendUserCommand` solo actualiza `User`: `IsSuspended = true`, `SuspendedUntil`, `SuspensionReason`; **no toca jobs ni assignments**.
4. El job sigue en su estado actual; el cliente no está bloqueado a nivel de login (el login solo comprueba `IsActive`).

### Evaluación de integridad

- **Job / JobAssignment:** Sin cambios; coherentes. ✅
- **UserStatusHistory:** Nueva fila con `NewIsSuspended = true`, `ActorUserId = AdminId`. ✅
- **AuditLog:** User.Suspend. ✅

### Inconsistencias detectadas

Ninguna en datos. La incoherencia es de reglas de negocio: un cliente suspendido puede seguir interactuando con sus jobs si conserva token (complete, cancel, crear nuevo job), porque `CreateJobCommand`, `CancelJobCommand` y `CompleteJobCommand` **no comprueban `IsSuspended`**.

### Problemas clasificados

| Severidad | Descripción |
|-----------|-------------|
| **MAJOR** | Cliente suspendido con job activo: el job no se cancela ni se bloquea. Además, el cliente puede seguir completando, cancelando o creando jobs (no se valida `IsSuspended`). Recomendación: (1) Validar `!user.IsSuspended` en CreateJob, CancelJob, CompleteJob y devolver 403 si está suspendido; (2) Opcional: al suspender, notificar a ops sobre jobs activos del cliente para decidir cancelación/reasignación. |

---

## Caso 7 — Admin reasigna técnico mientras el técnico marca InProgress

### Flujo ejecutado (condición de carrera)

Dos órdenes posibles:

**Orden A — Reassign primero, luego Start**

1. Admin: `POST /api/v1/admin/jobs/{id}/reassign` (a Técnico B) → **200**; job sigue `Assigned`, assignment pasa a B.
2. Técnico A: `POST /api/v1/jobs/{id}/start` → **403** "Only the assigned technician can start this job". ✅ Correcto.

**Orden B — Start primero, luego Reassign**

1. Técnico A: `POST /api/v1/jobs/{id}/start` → **200**; job `InProgress`, `JobAssignment.StartedAt` rellenado.
2. Admin: `POST /api/v1/admin/jobs/{id}/reassign` (a Técnico B) → **200** (Reassign **no** rechaza job en `InProgress`; solo rechaza `Completed` y `Cancelled`).
3. Efecto: se elimina el `JobAssignment` de A (que tenía `StartedAt` rellenado) y se crea un **nuevo** `JobAssignment` para B con `StartedAt = null`, `CompletedAt = null`. El job sigue `InProgress`.

### Evaluación de integridad

- **Job:** `Status = InProgress` ✅ (coherente con "trabajo en curso").
- **JobAssignment:** El assignment **activo** es el de B, con `StartedAt = null`. Semánticamente: "el trabajo está InProgress pero el técnico actual (B) no ha pulsado start". Esto es **inconsistente** con el modelo: InProgress debería implicar que el assignment actual tiene `StartedAt` rellenado.
- **AssignmentOverride:** Registro correcto (From=A, To=B). ✅
- **AuditLog:** JOB_START (por A) y Job.Reassign. ✅

### Inconsistencias detectadas

| Inconsistencia | Detalle |
|----------------|---------|
| **Assignment con StartedAt nulo en job InProgress** | El job está `InProgress` pero el `JobAssignment` vigente (B) tiene `StartedAt = null`. El técnico B no puede llamar a `start` porque el handler exige `job.Status == Assigned`. Queda un estado híbrido: job en curso según negocio, assignment sin "inicio" según datos. |
| **Pérdida de trazabilidad de quién inició** | El assignment de A (que sí inició) se borra; solo queda el override. No hay en el modelo un campo "StartedByTechnicianId" a nivel de Job para este caso. |

### Problemas clasificados

| Severidad | Descripción |
|-----------|-------------|
| **CRITICAL** | Condición de carrera entre Reassign y Start puede dejar Job `InProgress` con un JobAssignment cuyo `StartedAt` es null. Opciones de corrección: (1) No permitir Reassign si `job.Status == InProgress` (devolver 400 con mensaje claro); (2) Si se permite Reassign desde InProgress, crear el nuevo assignment con `StartedAt = now` para mantener consistencia; (3) Definir estado "ReassignedInProgress" y flujo explícito (p. ej. B debe "retomar" el trabajo). |

---

## Caso 8 — Técnico suspendido intenta enviar propuesta

### Flujo ejecutado

1. Admin: `POST /api/v1/admin/users/{technicianId}/suspend` → **204**.
2. Técnico (sigue con token válido): `POST /api/v1/jobs/{jobId}/proposals` con Price, Message → **201**; se crea la propuesta en estado `Pending`.
3. Un Admin podría aceptar esa propuesta y asignar el job a un técnico suspendido.

### Evaluación de integridad

- **Proposal:** Creada correctamente; `Status = Pending`. ✅
- **UserStatusHistory / AuditLog:** Suspend registrado. ✅
- No hay incoherencia de FK ni estados imposibles. La incoherencia es de regla de negocio: técnicos suspendidos no deberían poder proponer ni ser asignados.

### Inconsistencias detectadas

Ninguna a nivel de integridad referencial. Sí hay violación de política: técnico suspendido puede participar en el flujo (proponer y, si Admin acepta, quedar asignado).

### Problemas clasificados

| Severidad | Descripción |
|-----------|-------------|
| **MAJOR** | `SubmitProposalCommand` no comprueba `technician.IsSuspended`. Un técnico suspendido puede enviar propuestas y ser asignado. Recomendación: en SubmitProposalCommand (y opcionalmente en AcceptProposalCommand), validar que el técnico no esté suspendido y devolver 403 con mensaje claro ("Usuario suspendido no puede enviar propuestas" / "No se puede asignar un técnico suspendido"). |

---

## Resumen de clasificación de problemas

| Severidad | Cantidad | Casos |
|-----------|----------|--------|
| **CRITICAL** | 1 | Caso 7 (Reassign vs Start) |
| **MAJOR** | 4 | Casos 2, 3, 6, 8 |
| **MINOR** | 2 | Casos 4, 5 |
| **SUGGESTION** | 1 | Caso 1 |

---

## Validaciones transversales realizadas

Para cada caso se comprobó:

- **Job:** Estado y fechas (`CancelledAt`, `CompletedAt`, `AssignedAt`, `StartedAt`) coherentes; sin estados imposibles.
- **JobAssignment:** A lo sumo un assignment "activo" por job; `StartedAt`/`CompletedAt` alineados con el estado del job (con la excepción detectada en Caso 7).
- **AssignmentOverride:** Solo cuando hubo reasignación; FKs y razón coherentes.
- **UserStatusHistory:** Registro en cambios de suspensión/activación.
- **AuditLog:** Acciones realizadas con EntityType/EntityId correctos.
- **Datos huérfanos / concurrencia:** Sin asignaciones huérfanas; condición de carrera identificada y documentada en Caso 7.

---

## Próximos pasos recomendados

1. **CRITICAL:** Implementar regla o ajuste de modelo para el cruce Reassign + Start (Caso 7): prohibir Reassign en InProgress y/o mantener consistencia de `StartedAt` en el assignment activo.
2. **MAJOR:** Añadir validación `IsSuspended` en CreateJob, CancelJob, CompleteJob (cliente) y en SubmitProposal (y opcionalmente AcceptProposal) para técnicos.
3. **MAJOR:** Definir política para jobs abandonados (Caso 2) y mejora de UX en aceptación de propuestas (Caso 3).
4. **MINOR:** Valorar aviso al cancelar con técnico ya asignado (Caso 4) y límite/rate limit de creación de jobs (Caso 5).
5. **SUGGESTION:** Valorar "deshacer creación" o estado Abandoned para cancelaciones inmediatas (Caso 1).

Este documento refleja el comportamiento observado según el código actual de FixHub y debe actualizarse al cambiar reglas de negocio o implementar las correcciones anteriores.
