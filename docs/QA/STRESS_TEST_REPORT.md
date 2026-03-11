# FixHub â€” Reporte de prueba de carga (stress test ligero)

**Objetivo:** Simular carga ligera: 10 clientes, 5 tÃ©cnicos, 30 jobs con distribuciÃ³n definida.

**Script:** `tests/scripts/run-stress-test.ps1` (ejecutar con API en marcha en http://localhost:5100).

**Requisitos:** 1) Migraciones aplicadas. 2) API en Development (no auto-asigna jobs para tests). 3) Reiniciar API tras cambios.

---

## Resumen de ejecución

- **Inicio:** 2026-03-10 20:31:57
- **Fin:**   2026-03-10 20:36:19
- **Errores durante la ejecuciÃ³n:** 25
- **Jobs creados:** 30
- **Completados (1-10):** 0
- **Cancelados sin asignaciÃ³n (11-15):** 5
- **Reasignados (16-20):** 0
- **Sin aceptar propuesta (21-25):** 0
- **Cancelados despuÃ©s de asignaciÃ³n (26-30):** 0

### Errores

- POST jobs/adfe852d-1c72-4f71-8e98-849f84fc22f6/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/b4b63619-d5a8-40c6-8137-94d3807e1c61/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/a35abcac-38f2-4b6d-ac24-262a01a18968/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/5a453722-ae78-40bb-9fc4-30acb917bc53/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/aef04d36-80d7-4e57-92d8-4b5e271d3d89/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/68854dcb-8bf6-4faa-bc73-c0aaef755e6b/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/c4cd1116-914c-48a2-9b76-7253fe608afc/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/d4e5b6a6-b25a-497a-bde2-6797a3c86e3e/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/a83e5bf5-8899-49fa-b68c-a3ec5d8b669d/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/96432208-34a3-43e2-8414-61f8b624ba1a/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/2555a913-f758-4107-a5e8-ae31a9499d5c/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/2a45b352-2e1f-49ee-a60b-57dbc80550d6/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/94f85e6d-4b9f-4793-9589-1f35214d3329/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/5493950f-5086-468e-8cab-2ce56602996e/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/001da6da-0ee3-4a8a-956a-e19a5a6b5fdc/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/85beb594-eb78-48b3-8db6-993f9aefeb75/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/088d00e7-db69-4737-b173-7946b4c373a3/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/e8e8f1eb-230c-46f8-87ce-8a402f07a719/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/8f93d9dc-8c91-42dc-a3a8-82b517522c65/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/4cdf31a6-2dc0-4a7b-af3a-7419f8e68d55/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/806dc503-bc25-4b9a-8c7f-826ed47f0b27/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/d95866ed-c108-4b76-87e3-37a387b1fc2c/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/40ca7b58-a2a7-4df7-97b0-ea5ff9fe377e/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/bdff826b-18c3-4c04-893b-3efb29ea2a0e/proposals : The remote server returned an error: (400) Bad Request.
- POST jobs/7a66e5c2-d4b0-4318-899f-f46a4d2f793e/proposals : The remote server returned an error: (400) Bad Request.


## DiseÃ±o del test

| Recurso    | Cantidad |
|------------|----------|
| Clientes   | 10       |
| TÃ©cnicos   | 5        |
| Admin      | 1        |
| Jobs       | 30       |

### DistribuciÃ³n de acciones (30 jobs)

| Grupo              | Cantidad | Acciones |
|--------------------|----------|----------|
| Completados        | 10       | Crear â†’ Propuesta â†’ Aceptar â†’ Start â†’ Complete |
| Cancelados (antes) | 5        | Crear â†’ Cancelar (sin propuesta) |
| Reasignados        | 5        | Crear â†’ Propuesta (Tech1) â†’ Aceptar â†’ Reasignar a Tech2 |
| Sin aceptar        | 5        | Crear â†’ Propuesta(s) â†’ no Aceptar |
| Cancelados (despuÃ©s)| 5       | Crear â†’ Propuesta â†’ Aceptar â†’ Cliente cancela |

## Validaciones

Comprobar en BD o API tras ejecutar el script.

### 1. No existan duplicaciones

- [ ] **Jobs:** 30 filas Ãºnicas por Id; ningÃºn Id repetido.
- [ ] **JobAssignments:** A lo sumo un assignment activo por job (por job que tuvo propuesta aceptada); ningÃºn job con dos assignments vigentes.
- [ ] **Proposals:** No hay dos propuestas con el mismo (JobId, TechnicianId) (constraint Ãºnico en BD).

### 2. Estados correctos

- [ ] **10 completados:** Job.Status = 4 (Completed), CompletedAt no nulo, un JobAssignment con CompletedAt no nulo.
- [ ] **5 cancelados antes:** Job.Status = 5 (Cancelled), CancelledAt no nulo, sin JobAssignment para ese job.
- [ ] **5 reasignados:** Job.Status = 2 (Assigned); exactamente un AssignmentOverride por cada uno; un solo JobAssignment activo (tÃ©cnico destino).
- [ ] **5 sin aceptar:** Job.Status = 1 (Open); al menos una propuesta en estado Pending; sin JobAssignment.
- [ ] **5 cancelados despuÃ©s:** Job.Status = 5 (Cancelled), CancelledAt no nulo; existe JobAssignment (el que se creÃ³ al aceptar).

### 3. AuditLog registre todas las acciones

Comprobar que existan entradas de AuditLog coherentes con el flujo:

- [ ] **JOB_CREATE:** 30 (una por job creado).
- [ ] **PROPOSAL_SUBMIT:** al menos 25 (10+5+5+5 para completados, reasignados, sin aceptar, cancelados despuÃ©s).
- [ ] **PROPOSAL_ACCEPT:** 20 (10 completados + 5 reasignados + 5 cancelados despuÃ©s).
- [ ] **JOB_START:** 10 (solo los completados).
- [ ] **JOB_COMPLETE:** 10.
- [ ] **JOB_CANCEL:** 10 (5 antes + 5 despuÃ©s).
- [ ] **Job.Reassign:** 5.

### Consultas SQL de ejemplo (PostgreSQL)

Tabla de auditorÃ­a: udit_logs (columnas: action, entity_type, entity_id, created_at_utc).

`sql
-- Conteo por estado de Job (tabla jobs, columna status)
SELECT status, COUNT(*) FROM jobs GROUP BY status;

-- Jobs con mÃ¡s de un JobAssignment (debe ser 0)
SELECT job_id, COUNT(*) FROM job_assignments GROUP BY job_id HAVING COUNT(*) > 1;

-- Conteo por acciÃ³n en AuditLog
SELECT action, COUNT(*) FROM audit_logs GROUP BY action ORDER BY action;
`

## Resultado de validaciÃ³n

_(Rellenar tras ejecutar las comprobaciones)_

| ValidaciÃ³n           | Resultado | Notas |
|----------------------|-----------|-------|
| Sin duplicaciones    |           |       |
| Estados correctos    |           |       |
| AuditLog completo   |           |       |
