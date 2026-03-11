# FixHub — Plan de pruebas funcionales end-to-end

**Objetivo:** Validar el flujo completo del negocio simulando el comportamiento real de **Cliente**, **Técnico** y **Administrador**, usando **endpoints reales** (sin mocks ni unit tests).

**Base URL API:** `api/v1`  
**Autenticación:** Bearer JWT devuelto en `POST /auth/login`.

---

## 1. Referencia rápida de endpoints

| Rol        | Método | Ruta | Descripción |
|-----------|--------|------|-------------|
| Público   | POST   | `auth/register` | Registro (Customer/Technician/Admin) |
| Público   | POST   | `auth/login` | Login → JWT |
| Cliente   | POST   | `jobs` | Crear solicitud |
| Cliente   | GET    | `jobs/mine` | Listar mis jobs |
| Cliente   | GET    | `jobs/{id}` | Ver job |
| Cliente   | POST   | `jobs/{id}/cancel` | Cancelar job (solo Open/Assigned) |
| Cliente   | POST   | `jobs/{id}/complete` | Marcar completado |
| Técnico   | GET    | `jobs` | Listar jobs (para proponer) |
| Técnico   | POST   | `jobs/{id}/proposals` | Enviar propuesta |
| Técnico   | POST   | `jobs/{id}/start` | Iniciar trabajo (Assigned → InProgress) |
| Admin     | GET    | `jobs/{id}/proposals` | Ver propuestas del job |
| Admin     | POST   | `proposals/{id}/accept` | Aceptar propuesta → crea JobAssignment |
| Admin     | POST   | `admin/jobs/{id}/start` | Forzar inicio (Open/Assigned → InProgress) |
| Admin     | PATCH  | `admin/jobs/{id}/status` | Forzar estado (p. ej. Cancelled) |
| Admin/Ops | POST   | `admin/jobs/{id}/reassign` | Reasignar a otro técnico (policy OpsOnly: Admin, Supervisor, OpsDispatcher) |
| Admin     | POST   | `admin/users/{id}/suspend` | Suspender usuario |
| Admin     | POST   | `admin/users/{id}/unsuspend` | Quitar suspensión |

**Estados de Job:** `Open` → `Assigned` → `InProgress` → `Completed` | `Cancelled` (desde Open/Assigned/InProgress).

---

## 2. Validaciones transversales (todos los escenarios)

Tras cada flujo, comprobar:

- **Job:** `Status`, `CancelledAt`/`CompletedAt`/`AssignedAt` según corresponda; una sola fila por job; sin estados imposibles.
- **JobAssignment:** Un único registro activo por job (el actual); `StartedAt`/`CompletedAt` coherentes con el estado del job; no asignaciones huérfanas (JobId existente).
- **AssignmentOverride:** Solo cuando hubo reasignación; `FromTechnicianId`, `ToTechnicianId`, `Reason`, `AdminUserId` y `JobId` coherentes.
- **UserStatusHistory:** Registro por cada cambio de estado (suspender/reactivar); `PreviousIsActive`/`NewIsActive`, `PreviousIsSuspended`/`NewIsSuspended`, `ActorUserId`.
- **AuditLog:** Entradas para las acciones ejecutadas (p. ej. JOB_CREATE, PROPOSAL_SUBMIT, PROPOSAL_ACCEPT, JOB_START, JOB_COMPLETE, JOB_CANCEL, Job.Reassign, User.Suspend, etc.); `EntityType`/`EntityId` correctos; sin huecos en flujos críticos.

**Detectar y reportar:**

- Inconsistencias (p. ej. Job `Completed` sin `CompletedAt`).
- Estados imposibles (p. ej. dos JobAssignment activos para el mismo job).
- Datos huérfanos (assignments sin job, overrides sin job).
- Errores de concurrencia (dos reasignaciones simultáneas, doble cancel, etc.).

---

## 3. Escenario 1 — Flujo completo normal

**Descripción:** Cliente crea job → Técnico envía propuesta → Admin acepta → Técnico inicia → Cliente completa.

### Paso 1 — Registro / login (precondición)

- **POST** `api/v1/auth/register`  
  **Body:**
  ```json
  {
    "fullName": "Cliente E2E",
    "email": "cliente-e2e-1@test.fixhub",
    "password": "Test123!",
    "role": 1,
    "phone": "+34000000001"
  }
  ```
  **Respuesta esperada:** 201, `AuthResponse`: `userId`, `email`, `fullName`, `role`, `token`.  
  Guardar `token` como **TokenCliente** y `userId` como **ClienteId**.

- Registrar y loguear **Técnico** (role 2) y **Admin** (role 3). El **técnico** debe tener perfil aprobado (vía `PATCH admin/technicians/{tecnicoId}/status` con `{ "status": 2 }` — TechnicianStatus.Approved) para que un Admin pueda aceptar su propuesta.  
  Guardar **TokenTecnico**, **TecnicoId**, **TokenAdmin**, **AdminId**.

### Paso 2 — Cliente crea job

- **POST** `api/v1/jobs`  
  **Headers:** `Authorization: Bearer {TokenCliente}`  
  **Body:**
  ```json
  {
    "categoryId": 1,
    "title": "Reparación E2E flujo normal",
    "description": "Descripción del trabajo",
    "addressText": "Calle Test 1",
    "lat": null,
    "lng": null,
    "budgetMin": 50,
    "budgetMax": 150
  }
  ```
  **Respuesta esperada:** 201, `JobDto`: `id`, `status: "Open"` (o "Assigned" si hay auto-asignación), `customerId`, `createdAt`.  
  Guardar **JobId**.

- **Validación:** Si `status === "Open"`, no debe existir `JobAssignment` para este job. Si `status === "Assigned"`, debe existir exactamente un `JobAssignment` y el job debe tener `assignedAt`.

### Paso 3 — Técnico envía propuesta (solo si job está Open)

- **POST** `api/v1/jobs/{JobId}/proposals`  
  **Headers:** `Authorization: Bearer {TokenTecnico}`  
  **Body:**
  ```json
  {
    "price": 100,
    "message": "Puedo ir mañana"
  }
  ```
  **Respuesta esperada:** 201, `ProposalDto`: `id`, `jobId`, `technicianId`, `status: "Pending"`.  
  Guardar **ProposalId**.

### Paso 4 — Admin acepta propuesta

- **POST** `api/v1/proposals/{ProposalId}/accept`  
  **Headers:** `Authorization: Bearer {TokenAdmin}`  
  **Body:** (ninguno)

  **Respuesta esperada:** 200, `AcceptProposalResponse`: `assignmentId`, `jobId`, `proposalId`, `technicianId`, `acceptedAt`.

- **Validación:** Job `status === "Assigned"`, un `JobAssignment` con ese `jobId` y `proposalId`; propuesta en estado Accepted; otras propuestas del mismo job Rejected.

### Paso 5 — Técnico inicia trabajo

- **POST** `api/v1/jobs/{JobId}/start`  
  **Headers:** `Authorization: Bearer {TokenTecnico}`  

  **Respuesta esperada:** 200, `JobDto` con `status: "InProgress"`.

- **Validación:** Job `InProgress`; `JobAssignment.StartedAt` no nulo.

### Paso 6 — Cliente marca completado

- **POST** `api/v1/jobs/{JobId}/complete`  
  **Headers:** `Authorization: Bearer {TokenCliente}`  

  **Respuesta esperada:** 200, `JobDto`: `status: "Completed"`, `completedAt` presente.

### Estado final esperado

- **Job:** `Status = Completed`, `CompletedAt` y `AssignedAt` no nulos; sin `CancelledAt`.
- **JobAssignment:** Un registro; `AcceptedAt`, `StartedAt`, `CompletedAt` no nulos.
- **AssignmentOverride:** Ninguno (no hubo reasignación).
- **AuditLog:** JOB_CREATE, PROPOSAL_SUBMIT, PROPOSAL_ACCEPT, JOB_START, JOB_COMPLETE.
- **UserStatusHistory:** Solo si en el flujo se suspendió/reactivó usuario.

---

## 4. Escenario 2 — Cliente cancela antes de asignación

**Descripción:** Cliente crea job (Open) y cancela antes de que se acepte ninguna propuesta.

### Pasos

1. Cliente crea job (como en Escenario 1, paso 2). Asegurar que el job queda **Open** (sin auto-asignación o sin aceptar propuesta).
2. **POST** `api/v1/jobs/{JobId}/cancel`  
   **Headers:** `Authorization: Bearer {TokenCliente}`  
   **Body:** ninguno.

   **Respuesta esperada:** 200, `JobDto`: `status: "Cancelled"`, `cancelledAt` presente.

### Estado final esperado

- **Job:** `Status = Cancelled`, `CancelledAt` no nulo; `AssignedAt`/`CompletedAt` nulos.
- **JobAssignment:** Ninguno (nunca se creó).
- **AssignmentOverride:** Ninguno.
- **AuditLog:** JOB_CREATE, JOB_CANCEL.

---

## 5. Escenario 3 — Cliente cancela después de asignación

**Descripción:** Job creado → propuesta aceptada (Assigned) → Cliente cancela.

### Pasos

1. Cliente crea job (Open).
2. Técnico envía propuesta; Admin acepta propuesta (job pasa a Assigned).
3. **POST** `api/v1/jobs/{JobId}/cancel` con token del cliente.

   **Respuesta esperada:** 200, `JobDto`: `status: "Cancelled"`, `cancelledAt` no nulo.

### Estado final esperado

- **Job:** `Status = Cancelled`, `CancelledAt` no nulo; puede tener `AssignedAt` (se asignó antes de cancelar).
- **JobAssignment:** Sigue existiendo el registro creado al aceptar la propuesta; no se borra; el job está cancelado.
- **AssignmentOverride:** Ninguno.
- **AuditLog:** JOB_CREATE, PROPOSAL_SUBMIT, PROPOSAL_ACCEPT, JOB_CANCEL.

---

## 6. Escenario 4 — Admin reasigna técnico

**Descripción:** Job Assigned con Técnico A → Admin reasigna a Técnico B.

### Pasos

1. Job en estado Assigned con un técnico (TecnicoAId). Técnico B registrado y (si aplica) con perfil aprobado.
2. **POST** `api/v1/admin/jobs/{JobId}/reassign`  
   **Headers:** `Authorization: Bearer {TokenAdmin}` (o usuario con política OpsOnly).  
   **Body:**
   ```json
   {
     "toTechnicianId": "{TecnicoBId}",
     "reason": "Reasignación E2E por disponibilidad",
     "reasonDetail": "Detalle opcional"
   }
   ```

   **Respuesta esperada:** 200, `ReassignJobResponse`: `jobId`, `newAssignmentId`, `toTechnicianId`, `overrideId`.

3. **GET** `api/v1/jobs/{JobId}` con token Admin o Técnico B: job sigue `Assigned`, técnico asignado debe ser B.

### Estado final esperado

- **Job:** `Status = Assigned`, `AssignedAt` actualizado (o al menos coherente).
- **JobAssignment:** Un solo registro activo: `ProposalId` de la propuesta del técnico B (nueva o reutilizada); el anterior eliminado.
- **AssignmentOverride:** Un registro: `JobId`, `FromTechnicianId = TecnicoAId`, `ToTechnicianId = TecnicoBId`, `Reason`, `AdminUserId`.
- **AuditLog:** Entrada `Job.Reassign` con before/after y `overrideId`.

---

## 7. Escenario 5 — Cliente suspendido intenta crear job

**Descripción:** Admin suspende al cliente; el cliente (aún puede hacer login si no está desactivado) intenta crear un job.

### Pasos

1. Cliente registrado y con token válido.
2. **POST** `api/v1/admin/users/{ClienteId}/suspend`  
   **Headers:** `Authorization: Bearer {TokenAdmin}`  
   **Body:**
   ```json
   {
     "suspendedUntil": null,
     "suspensionReason": "Prueba E2E suspensión"
   }
   ```
   **Respuesta esperada:** 204.

3. Cliente hace login de nuevo (si la implementación no revoca tokens).  
4. **POST** `api/v1/jobs` con body de creación de job y token del cliente.

**Comportamiento actual (según código):** La creación de job **no** comprueba `IsSuspended`; la petición puede devolver **201** y crearse el job.

**Recomendación:** Incluir en la suite la comprobación del estado actual (201 o 403 según política de negocio). Si el producto exige bloquear a usuarios suspendidos, añadir validación en `CreateJobCommand` y esperar **403** con `errorCode` apropiado; en ese caso, comprobar que no se crea fila en `Jobs` y que **UserStatusHistory** tiene el registro de suspensión y **AuditLog** no tiene JOB_CREATE para esa llamada.

### Validaciones

- **UserStatusHistory:** Al menos un registro para el cliente: `NewIsSuspended = true`, `ActorUserId = AdminId`.
- **AuditLog:** User.Suspend. Si se decide bloquear creación: no JOB_CREATE para este intento.

---

## 8. Escenario 6 — Técnico suspendido intenta participar

**Descripción:** Admin suspende al técnico; el técnico intenta enviar una propuesta o iniciar un job ya asignado a él.

### Pasos

1. Técnico registrado, aprobado, con token. Job Open existente (o asignado a ese técnico para el caso “iniciar”).
2. **POST** `api/v1/admin/users/{TecnicoId}/suspend` con token Admin (body opcional).
3. **POST** `api/v1/jobs/{JobId}/proposals` con token del técnico (Price, Message).

**Comportamiento actual:** No se valida `IsSuspended` en `SubmitProposalCommand`; la propuesta puede aceptarse (**201**).

**Recomendación:** Igual que escenario 5: documentar comportamiento actual y, si el negocio exige bloquear, añadir validación y esperar 403; validar que no queden propuestas/assignments incoherentes con usuarios suspendidos.

- Para “intentar iniciar”: job Assigned a ese técnico → **POST** `jobs/{JobId}/start` con token del técnico. Si se bloquea por suspensión, esperar 403 y que el job siga Assigned y `JobAssignment.StartedAt` nulo.

### Validaciones

- **UserStatusHistory:** Registro de suspensión del técnico.
- **AuditLog:** User.Suspend; según política, PROPOSAL_SUBMIT o JOB_START permitidos o rechazados.

---

## 9. Escenario 7 — Concurrencia básica en reasignación

**Descripción:** Dos reasignaciones simultáneas del mismo job (mismo Admin o dos usuarios Ops) a dos técnicos distintos.

### Pasos

1. Job en estado Assigned con Técnico A. Técnicos B y C disponibles.
2. En paralelo (o lo más cercano posible):
   - **POST** `api/v1/admin/jobs/{JobId}/reassign` con `toTechnicianId: TecnicoBId`, reason "R1".
   - **POST** `api/v1/admin/jobs/{JobId}/reassign` con `toTechnicianId: TecnicoCId`, reason "R2".

**Respuesta esperada:** Una petición 200 con `ReassignJobResponse`; la otra **409** con `errorCode: "CONCURRENCY_CONFLICT"` (o 400 si la validación de negocio lo impide).

### Estado final esperado

- **Job:** Sigue `Assigned`; un solo **JobAssignment** activo (o B o C, según cuál ganó).
- **AssignmentOverride:** Solo **una** fila para este job en esta ejecución (la reasignación que se aplicó).
- No dos assignments activos para el mismo job; no overrides duplicados o contradictorios.
- **AuditLog:** Una sola entrada `Job.Reassign` para esta secuencia.

---

## 10. Escenario 8 — Cliente crea job y nunca se acepta propuesta

**Descripción:** Job creado (Open) → uno o más técnicos envían propuestas → ningún Admin acepta ninguna propuesta. El job permanece Open; comprobar que no hay asignación ni completado.

### Pasos

1. Cliente crea job (Open).
2. Uno o dos técnicos envían propuestas (201, Pending).
3. No llamar a `proposals/{id}/accept`.
4. **GET** `api/v1/jobs/{JobId}/proposals` (Admin o Cliente): debe listar propuestas en Pending.
5. **GET** `api/v1/jobs/{JobId}`: `status: "Open"`.

### Estado final esperado

- **Job:** `Status = Open`; `AssignedAt`, `CompletedAt`, `CancelledAt` nulos.
- **JobAssignment:** Ninguno.
- **AssignmentOverride:** Ninguno.
- **AuditLog:** JOB_CREATE, PROPOSAL_SUBMIT (por cada propuesta).

---

## 11. Escenario 9 — Técnico acepta pero no inicia trabajo

**Descripción:** Job Assigned (propuesta aceptada) y el técnico nunca llama a `start`. El job se queda en Assigned; el cliente no puede completar “en sitio” hasta que esté InProgress, pero en la implementación actual el cliente **sí** puede marcar completado desde Assigned (CompleteJobCommand acepta InProgress o Assigned).

### Pasos

1. Job en estado Assigned (propuesta aceptada).
2. No llamar a `POST jobs/{JobId}/start`.
3. **GET** `api/v1/jobs/{JobId}`: `status: "Assigned"`.
4. Opcional: Cliente llama **POST** `jobs/{JobId}/complete`. Según código actual: **200**, job pasa a Completed y `JobAssignment.CompletedAt` se rellena.

### Estado final esperado (sin complete)

- **Job:** `Status = Assigned`, `AssignedAt` no nulo; `StartedAt` (del DTO, si se expone) o el assignment sin `StartedAt` según modelo.
- **JobAssignment:** Un registro; `AcceptedAt` no nulo; `StartedAt` nulo hasta que alguien llame a start (o admin force start).

Si en el test se llama a `complete` desde Assigned:
- **Job:** `Status = Completed`, `CompletedAt` no nulo.
- **JobAssignment:** `CompletedAt` no nulo.

---

## 12. Escenario 10 — Cliente cancela mientras técnico está “en camino”

**Descripción:** Job Assigned (técnico asignado pero aún no ha llamado a `start` — “en camino”). Cliente cancela.

### Pasos

1. Job en estado Assigned (propuesta aceptada; técnico no ha ejecutado `start`).
2. **POST** `api/v1/jobs/{JobId}/cancel` con token del cliente.

**Respuesta esperada:** 200, `JobDto`: `status: "Cancelled"`, `cancelledAt` no nulo.

Si el técnico ya hubiera llamado a `start` (job InProgress), **POST** cancel con token cliente debe devolver **400** con mensaje tipo “Job cannot be cancelled: service is already in progress” y `errorCode: "INVALID_STATUS"`.

### Estado final esperado (cancel en Assigned)

- **Job:** `Status = Cancelled`, `CancelledAt` no nulo.
- **JobAssignment:** El registro sigue existiendo (no se borra); el job simplemente queda cancelado.
- **AuditLog:** JOB_CANCEL.
- Cliente y técnico reciben notificaciones de cancelación (comprobar si el sistema las registra o envía).

---

## 13. Resumen de códigos de error y respuestas

| Situación | HTTP | errorCode (en ProblemDetails) |
|-----------|------|-------------------------------|
| Job no encontrado | 404 | JOB_NOT_FOUND |
| Propuesta no encontrada | 404 | PROPOSAL_NOT_FOUND |
| Solo admin puede aceptar propuesta | 403 | FORBIDDEN |
| Job ya asignado | 409 | JOB_ALREADY_ASSIGNED |
| Cancelar job InProgress/Completed/Cancelled | 400 | INVALID_STATUS |
| Solo el técnico asignado puede iniciar | 403 | FORBIDDEN |
| Reasignación: mismo técnico | 400 | SAME_TECHNICIAN |
| Reasignación: sin asignación actual | 404 | NO_ASSIGNMENT |
| Concurrencia (cancel, reassign, etc.) | 409 | CONCURRENCY_CONFLICT |
| Credenciales inválidas | 401 | INVALID_CREDENTIALS |
| Usuario no activo (login) | 401 | INVALID_CREDENTIALS |

---

## 14. Checklist de consistencia por escenario

Para cada escenario, rellenar:

| Comprobación | Escenario |
|--------------|-----------|
| Job en estado esperado y fechas coherentes | |
| Máximo un JobAssignment “activo” por job | |
| AssignmentOverride solo cuando hubo reassign | |
| UserStatusHistory en cambios de suspensión/activación | |
| AuditLog con acciones realizadas y entity correcta | |
| Sin asignaciones huérfanas (Job eliminado) | |
| Sin dos reasignaciones aplicadas en concurrencia | |

---

## 15. Notas de implementación

- **Categorías:** Usar un `categoryId` existente y activo en BD (p. ej. 1). Si existe endpoint de listado de categorías, usarlo para obtener un id válido.
- **Roles en registro:** `UserRole`: Customer = 1, Technician = 2, Admin = 3, Supervisor = 4, OpsDispatcher = 5. Para técnicos, aprobar el perfil vía `PATCH admin/technicians/{id}/status` con body `{ "status": 2 }` (TechnicianStatus.Approved) antes de aceptar propuestas o reasignar.
- **Aceptar propuesta:** Solo un usuario con rol que cumpla “Admin” en el controller (AcceptAsAdmin) puede aceptar; el cliente no acepta propuestas en la implementación actual.
- **Tokens:** Incluir siempre `Authorization: Bearer {token}` en las peticiones que requieran autenticación.
- **Base URL:** Ajustar host/puerto según entorno (ej. `https://localhost:7xxx/api/v1`).

Este plan sirve como especificación para pruebas funcionales E2E contra la API real de FixHub y para verificar la integridad de Job, JobAssignment, AssignmentOverride, UserStatusHistory y AuditLog en cada flujo.
