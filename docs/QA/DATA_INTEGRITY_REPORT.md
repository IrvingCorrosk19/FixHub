# FixHub — Reporte de integridad de datos

**Objetivo:** Definir y comprobar las reglas de integridad del modelo de datos (Job, JobAssignment, AssignmentOverride, UserStatusHistory, AuditLog) y detectar duplicados, huérfanos y estados incoherentes.

**Documentos relacionados:** [FUNCTIONAL_TEST_PLAN.md](FUNCTIONAL_TEST_PLAN.md), [FUNCTIONAL_TEST_RESULTS.md](FUNCTIONAL_TEST_RESULTS.md), [STRESS_TEST_REPORT.md](STRESS_TEST_REPORT.md).

---

## 1. Reglas de integridad

### Jobs (`jobs`)

| Regla | Descripción |
|-------|-------------|
| **Estado válido** | `status` ∈ { 1=Open, 2=Assigned, 3=InProgress, 4=Completed, 5=Cancelled }. |
| **Fechas coherentes** | Si `status = 4` (Completed) → `completed_at` no nulo. Si `status = 5` (Cancelled) → `cancelled_at` no nulo. Si existe asignación activa → `assigned_at` no nulo. |
| **Unicidad** | Cada `id` es único; no hay filas duplicadas. |
| **FK** | `customer_id` y `category_id` deben existir en sus tablas. |

### JobAssignments (`job_assignments`)

| Regla | Descripción |
|-------|-------------|
| **Uno por job** | A lo sumo un assignment activo por `job_id` (constraint único en BD). |
| **FK** | `job_id` existe en `jobs`; `proposal_id` existe en `proposals`. |
| **Coherencia con Job** | Si el job está `InProgress`, el assignment activo debería tener `started_at` no nulo. Si el job está `Completed`, el assignment activo debería tener `completed_at` no nulo. |
| **Sin huérfanos** | No debe haber assignments cuyo `job_id` no exista en `jobs`. |

### AssignmentOverrides (`assignment_overrides`)

| Regla | Descripción |
|-------|-------------|
| **Solo por reasignación** | Se crea únicamente cuando un Admin/Ops ejecuta Reassign. |
| **FK** | `job_id` existe en `jobs`; `from_technician_id` y `to_technician_id` existen en `users`; `admin_user_id` existe. |
| **Sin huérfanos** | No debe haber overrides cuyo `job_id` haya sido eliminado. |

### UserStatusHistory (`user_status_histories`)

| Regla | Descripción |
|-------|-------------|
| **Por cambio de estado** | Una fila por cada Suspend/Unsuspend/Activate/Deactivate. |
| **FK** | `user_id` y `actor_user_id` existen en `users`. |
| **Campos** | `previous_is_active`, `new_is_active`, `previous_is_suspended`, `new_is_suspended` coherentes con la transición. |

### AuditLog (`audit_logs`)

| Regla | Descripción |
|-------|-------------|
| **Acciones registradas** | JOB_CREATE, PROPOSAL_SUBMIT, PROPOSAL_ACCEPT, JOB_START, JOB_COMPLETE, JOB_CANCEL, Job.Reassign, User.Suspend, User.Unsuspend, etc. |
| **EntityType / EntityId** | Coherentes con la entidad afectada (Job, Proposal, User). |
| **Sin huecos** | Para flujos críticos (crear job → propuesta → aceptar → start → complete) debe existir la secuencia de acciones esperada. |

### Proposals (`proposals`)

| Regla | Descripción |
|-------|-------------|
| **Unicidad (job, técnico)** | No hay dos propuestas con el mismo `(job_id, technician_id)` (constraint único). |
| **FK** | `job_id` en `jobs`, `technician_id` en `users`. |
| **Estado** | `status` ∈ { Pending, Accepted, Rejected }. |

---

## 2. Comprobaciones recomendadas

### 2.1 Sin duplicaciones

- **Jobs:** `SELECT id, COUNT(*) FROM jobs GROUP BY id HAVING COUNT(*) > 1` → debe devolver 0 filas.
- **JobAssignments por job:** `SELECT job_id, COUNT(*) FROM job_assignments GROUP BY job_id HAVING COUNT(*) > 1` → 0 filas.
- **Proposals (job + technician):** La BD debe tener constraint único; no puede haber duplicados.

### 2.2 Estados correctos

- **Jobs Completed sin completed_at:** `SELECT * FROM jobs WHERE status = 4 AND completed_at IS NULL` → 0 filas.
- **Jobs Cancelled sin cancelled_at:** `SELECT * FROM jobs WHERE status = 5 AND cancelled_at IS NULL` → 0 filas.
- **Job InProgress con assignment sin started_at:** posible tras condición de carrera Reassign vs Start (ver [FUNCTIONAL_TEST_RESULTS.md](FUNCTIONAL_TEST_RESULTS.md) Caso 7). Investigar y corregir si se detecta.

### 2.3 Sin datos huérfanos

- **JobAssignments sin job:** `SELECT ja.* FROM job_assignments ja LEFT JOIN jobs j ON ja.job_id = j.id WHERE j.id IS NULL` → 0 filas.
- **AssignmentOverrides sin job:** `SELECT ao.* FROM assignment_overrides ao LEFT JOIN jobs j ON ao.job_id = j.id WHERE j.id IS NULL` → 0 filas.
- **Proposals sin job:** `SELECT p.* FROM proposals p LEFT JOIN jobs j ON p.job_id = j.id WHERE j.id IS NULL` → 0 filas.

### 2.4 AuditLog completo

- Conteo por acción: `SELECT action, COUNT(*) FROM audit_logs GROUP BY action ORDER BY action`.
- Para un job concreto: filtrar por `entity_type = 'Job'` y `entity_id = <job_id>` y comprobar que existan las acciones esperadas según el flujo (JOB_CREATE, PROPOSAL_SUBMIT, PROPOSAL_ACCEPT, JOB_START, JOB_COMPLETE o JOB_CANCEL, Job.Reassign si aplica).

---

## 3. Resultado de la última validación

_(Rellenar tras ejecutar las comprobaciones, por ejemplo después del stress test o de una batería funcional.)_

| Comprobación           | Fecha       | Resultado | Notas |
|------------------------|------------|-----------|-------|
| Sin duplicaciones      |            |           |       |
| Estados correctos      |            |           |       |
| Sin datos huérfanos    |            |           |       |
| AuditLog coherente     |            |           |       |

---

## 4. Inconsistencias conocidas (según análisis de código)

- **Reassign + Start (condición de carrera):** Si el técnico hace Start y después un Admin hace Reassign del mismo job, puede quedar un JobAssignment activo con `started_at` nulo mientras el job está InProgress. Ver [FUNCTIONAL_TEST_RESULTS.md](FUNCTIONAL_TEST_RESULTS.md) — Caso 7, clasificado como CRITICAL.

Este reporte debe actualizarse cuando se añadan nuevas reglas de integridad o se corrijan comportamientos en el sistema.
