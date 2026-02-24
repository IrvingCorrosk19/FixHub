# FixHub — Informe de pruebas funcionales (nivel producción)

**Clasificación:** Uso interno / Auditoría funcional  
**Sistema:** FixHub (ASP.NET Core 8, Clean Architecture, EF Core, PostgreSQL, JWT, Docker)  
**Modo:** Empresa de servicios (no marketplace público)  
**Metodología:** Revisión de comportamiento por código + flujos E2E documentados. Sin modificación de código.  
**Fecha:** 2025-02-23  

---

## 1. Resumen ejecutivo

Se realizó una batería de pruebas funcionales de nivel producción sobre la API FixHub, cubriendo autenticación, flujo completo de servicio, autorización (IDOR), validaciones de negocio y robustez. La evaluación se basa en el análisis estático del código y en el comportamiento documentado en auditorías previas (docs/AUDIT/).

**Conclusiones principales:**

- **Crítico:** El registro permite crear usuarios con **Role=Admin** (`role: 3`). Cualquier atacante puede obtener privilegios de administrador. **Bloqueante para producción.**
- **Alto:** El endpoint de propuestas por job no valida que el Customer sea dueño del job; además, para Customer devuelve solo las propuestas donde él es el técnico (lista vacía), por lo que el dueño del job no puede ver las propuestas de su propio servicio.
- **Resto de flujos:** Autenticación (login, email duplicado, contraseña débil), ownership (job ajeno → 403), transiciones de estado (cancel/complete/start), proposal duplicada, review duplicada y manejo de errores (sin stack en respuesta) se comportan según lo esperado en el código.

**Score funcional:** **68/100** (penalizado por el fallo crítico de registro Admin y por el fallo alto en listado de propuestas para Customer).

**Veredicto:** El sistema **no** está listo para producción desde el punto de vista funcional y de seguridad hasta corregir el registro con Role=Admin y el acceso a propuestas por parte del Customer dueño del job.

---

## 2. Tabla de pruebas ejecutadas

Cada fila corresponde a un escenario; el resultado (PASS/FAIL) se basa en el análisis del código y en la documentación existente. Donde se indica "Evidencia", se referencia el archivo y las líneas que determinan el comportamiento.

| ID | Fase | Escenario | Resultado esperado | Resultado obtenido | Pass/Fail | Clasificación |
|----|------|-----------|--------------------|--------------------|-----------|----------------|
| A1 | 1 | Register Customer válido | 201, token | 201, token | PASS | — |
| A2 | 1 | Register con Role=Admin | 400 o 403 | 201, usuario Admin creado | **FAIL** | Crítico |
| A3 | 1 | Register email duplicado | 409 o 400 | Failure EMAIL_TAKEN | PASS | — |
| A4 | 1 | Register password débil | 400 validación | 400 (FluentValidation: min 8, mayúscula, dígito) | PASS | — |
| A5 | 1 | Register campos faltantes | 400 | 400 (NotEmpty, etc.) | PASS | — |
| A6 | 1 | Login correcto | 200, token | 200, token | PASS | — |
| A7 | 1 | Login password incorrecto | 401 | 401 | PASS | — |
| A8 | 1 | Token expirado | 401 | 401 (ValidateLifetime) | PASS | — |
| A9 | 1 | Token manipulado | 401 | 401 (firma inválida) | PASS | — |
| A10 | 1 | Respuesta error sin stacktrace | No exponer stack en body | ProblemDetails sin stack (ExceptionMiddleware) | PASS | — |
| B1 | 2 | Customer crea servicio (job) | 201, status Open | 201, Open | PASS | — |
| B2 | 2 | Admin ve solicitud | 200 list/dashboard | AdminOnly + handlers | PASS | — |
| B3 | 2 | Admin asigna técnico (accept proposal) | 200, job Assigned | 200, AcceptProposalCommand | PASS | — |
| B4 | 2 | Técnico inicia servicio | 200, InProgress | 200, solo si asignado (TechnicianStartJobCommand) | PASS | — |
| B5 | 2 | Completar servicio | 200, Completed | **Customer** completa (no Technician); 200 si InProgress/Assigned | PASS | — |
| B6 | 2 | Customer califica (review) | 201 | 201 si job Completed y no hay review previa | PASS | — |
| B7 | 2 | No saltar estados (ej. Complete en Open) | 400 | INVALID_STATUS (Complete: solo InProgress/Assigned) | PASS | — |
| C1 | 3 | Customer A intenta ver job de B | 403 | 403 (GetJobQuery ownership) | PASS | — |
| C2 | 3 | Técnico intenta completar job no asignado | 403 o N/A | Complete es CustomerOnly; Technician ni siquiera tiene endpoint | PASS | — |
| C3 | 3 | Customer intenta cancelar job ya completado | 400 | INVALID_STATUS (CancelJobCommand) | PASS | — |
| C4 | 3 | Customer intenta ver proposals de job ajeno | 403 | 200 con lista vacía (no valida ownership; comentario "Customer: no ve") | **FAIL** | Alto |
| D1 | 4 | Múltiples proposals mismo técnico mismo job | 400/409 | DUPLICATE_PROPOSAL | PASS | — |
| D2 | 4 | Review duplicado mismo job | 400 | REVIEW_EXISTS | PASS | — |
| D3 | 4 | Iniciar job si no está asignado | 403 | Solo asignado puede Start (TechnicianStartJobCommand) | PASS | — |
| D4 | 4 | Completar job si no está InProgress/Assigned | 400 | INVALID_STATUS | PASS | — |
| E1 | 5 | JSON inválido | 400 | 400 (deserialización o validación) | PASS | — |
| E2 | 5 | Campos adicionales no esperados | Ignorados o 400 | DTOs con propiedades fijas; extra ignorados (no mass assignment a entidad) | PASS | — |
| E3 | 5 | Modificar IDs en URL (ej. jobId ajeno) | 403/404 | Ownership en handlers; IDs en body no sobrescriben (CustomerId del token) | PASS | — |
| E4 | 5 | Stress básico / rate limit | 429 tras límite | AuthPolicy 10 req/min; global 60 req/min | PASS | — |

---

## 3. Resultado por escenario (detalle)

### FASE 1 — Autenticación

- **A1 Register Customer válido:** El handler crea usuario con Role del request; validator exige FullName, Email, Password (8+ chars, mayúscula, dígito), Role IsInEnum y ≠ 0. **PASS.**
- **A2 Register Role=Admin:** El validator no excluye Admin (valor 3). `RegisterCommandValidator` línea 36: `RuleFor(x => x.Role).IsInEnum().Must(r => r != 0)`. El handler asigna `request.Role` sin restricción. **FAIL — Crítico.** Evidencia: `src/FixHub.Application/Features/Auth/RegisterCommand.cs` (validator y handler).
- **A3 Email duplicado:** Handler comprueba `emailExists` y devuelve `Result.Failure("Email already registered.", "EMAIL_TAKEN")`. **PASS.** Evidencia: `RegisterCommandHandler` líneas 52–56.
- **A4 Password débil:** Validator: MinimumLength(8), Matches mayúscula y dígito. **PASS.**
- **A5 Campos faltantes:** NotEmpty en FullName, Email, Password; Role IsInEnum. **PASS.**
- **A6–A7 Login:** LoginCommand verifica credenciales; fallo → 401. **PASS.**
- **A8–A9 Token expirado/manipulado:** JWT con ValidateLifetime y validación de firma. **PASS.**
- **A10 Stacktrace:** ExceptionMiddleware devuelve ProblemDetails con Title, Status, Instance; no incluye excepción ni stack en el body. **PASS.** Evidencia: `ExceptionMiddleware.cs` líneas 46–54.

### FASE 2 — Flujo completo de servicio

Flujo real en FixHub (empresa de servicios):

1. **Customer crea job** → POST /jobs (CustomerOnly). **PASS.**
2. **Admin ve solicitud** → GET /admin/dashboard, GET /jobs (AdminOnly). **PASS.**
3. **Admin asigna técnico** → POST /proposals/{id}/accept (solo Admin; AcceptProposalCommand exige AcceptAsAdmin). **PASS.**
4. **Técnico inicia** → POST /jobs/{id}/start (TechnicianOnly; solo el técnico asignado). **PASS.**
5. **Completar servicio** → En FixHub lo hace el **Customer** (POST /jobs/{id}/complete), no el técnico. **PASS.** (Si se esperaba “técnico completa”, el modelo de negocio actual es “cliente confirma cierre”.)
6. **Customer califica** → POST /reviews (CustomerOnly; job debe estar Completed; no review duplicado). **PASS.**

No se pueden saltar estados: Complete exige InProgress o Assigned; Start exige Assigned; Cancel no permite InProgress/Completed/Cancelled. **PASS.**

### FASE 3 — Autorización (IDOR)

- **C1 Job ajeno:** GetJobQuery comprueba CustomerId/TechnicianId/Admin y devuelve FORBIDDEN si no corresponde. **PASS.** Evidencia: `GetJobQuery.cs` líneas 44–66.
- **C2 Técnico completa:** No existe endpoint de “complete” para Technician; Complete es CustomerOnly. **PASS.**
- **C3 Cancelar job completado:** CancelJobCommand rechaza status Completed con INVALID_STATUS. **PASS.** Evidencia: `CancelJobCommand.cs` líneas 34–37.
- **C4 Proposals de job ajeno:** GetJobProposalsQuery no comprueba `job.CustomerId == RequesterId`. Para no-Admin devuelve solo propuestas con `TechnicianId == RequesterId`, por lo que un Customer obtiene lista vacía incluso para su propio job. **FAIL — Alto.** Riesgo: si se “arregla” mostrando todas las propuestas sin comprobar propiedad, se produce IDOR. Evidencia: `GetJobProposalsQuery.cs` líneas 18–41.

### FASE 4 — Validaciones de negocio

- **D1 Proposal duplicada:** SubmitProposalCommand comprueba `duplicate` (mismo JobId + TechnicianId) y devuelve DUPLICATE_PROPOSAL. **PASS.** Evidencia: `SubmitProposalCommand.cs` líneas 63–68.
- **D2 Review duplicado:** CreateReviewCommand comprueba `db.Reviews.AnyAsync(r => r.JobId == req.JobId)` y devuelve REVIEW_EXISTS. **PASS.** Evidencia: `CreateReviewCommand.cs` líneas 60–63.
- **D3 Iniciar sin asignación:** TechnicianStartJobCommand exige `job.Assignment?.Proposal?.TechnicianId == req.TechnicianId` y status Assigned. **PASS.** Evidencia: `TechnicianStartJobCommand.cs` líneas 37–43.
- **D4 Completar sin InProgress/Assigned:** CompleteJobCommand exige `job.Status == InProgress || job.Status == Assigned`. **PASS.** Evidencia: `CompleteJobCommand.cs` líneas 31–33.

### FASE 5 — Robustez

- **E1 JSON inválido:** Deserialización o FluentValidation devuelven 400. **PASS.**
- **E2 Campos extra:** DTOs son records con propiedades fijas; JSON extra no se mapea a entidades sensibles. **PASS.**
- **E3 IDs en URL:** jobId/proposalId vienen de la ruta; ownership se valida en handlers. CustomerId en comandos sale del token (CurrentUserId). **PASS.**
- **E4 Rate limit:** AuthPolicy 10 req/min en Auth; global 60 req/min. **PASS.** Evidencia: `Program.cs` líneas 76–94.

---

## 4. Evidencia técnica (archivo y línea)

| Hallazgo | Archivo | Líneas | Comportamiento |
|----------|---------|--------|----------------|
| Register acepta Admin | `FixHub.Application/Features/Auth/RegisterCommand.cs` | 36 (validator), 65 (handler) | Role IsInEnum sin excluir Admin; handler asigna request.Role. |
| Customer no ve proposals de su job | `FixHub.Application/Features/Proposals/GetJobProposalsQuery.cs` | 18–41 | Sin comprobación job.CustomerId == RequesterId; no-Admin filtra por TechnicianId == RequesterId. |
| Cancel job completado rechazado | `FixHub.Application/Features/Jobs/CancelJobCommand.cs` | 34–37 | Status Completed → INVALID_STATUS. |
| Complete solo InProgress/Assigned | `FixHub.Application/Features/Jobs/CompleteJobCommand.cs` | 31–33 | Verificación de status. |
| Start solo técnico asignado | `FixHub.Application/Features/Jobs/TechnicianStartJobCommand.cs` | 37–43 | Assignment.Proposal.TechnicianId == req.TechnicianId; status Assigned. |
| Review duplicado rechazado | `FixHub.Application/Features/Reviews/CreateReviewCommand.cs` | 60–63 | AnyAsync(r => r.JobId == req.JobId) → REVIEW_EXISTS. |
| Error sin stack en respuesta | `FixHub.API/Middleware/ExceptionMiddleware.cs` | 46–54 | ProblemDetails sin detalle de excepción. |

---

## 5. Riesgos detectados

| ID | Riesgo | Clasificación | Impacto de negocio |
|----|--------|----------------|--------------------|
| R1 | Cualquier usuario puede registrarse como Admin y acceder a dashboard, asignación de técnicos, resolución de incidencias y métricas. | **Crítico** | Pérdida de control operativo, fraude, acceso a datos sensibles. |
| R2 | Customer dueño del job no puede ver las propuestas de su propio servicio (lista vacía). Si se corrige devolviendo todas las propuestas sin validar propiedad, un Customer podría ver propuestas de jobs ajenos (IDOR). | **Alto** | Experiencia incorrecta; riesgo de fuga de información entre clientes. |
| R3 | Exposición de mensajes de validación (ej. “Password must contain at least one uppercase letter”) puede ayudar a un atacante a ajustar intentos. | **Bajo** | Mejora: mensajes genéricos en producción. |

---

## 6. Recomendaciones priorizadas

| Prioridad | Acción | Criterio de aceptación |
|-----------|--------|-------------------------|
| **P0 (inmediata)** | Rechazar Role=Admin en registro. En RegisterCommandValidator: `Must(r => r != UserRole.Admin)` o rechazo explícito en handler. | POST register con `role: 3` devuelve 400 o 403; test de integración que lo verifique. |
| **P1 (alta)** | En GetJobProposalsQuery, para Customer: comprobar `job.CustomerId == req.RequesterId`; si no es dueño, devolver 403. Si el negocio exige que el dueño vea las propuestas de su job, devolver todas las del job cuando sea dueño. | Customer dueño recibe lista de propuestas; Customer ajeno recibe 403. |
| **P2 (media)** | Revisar mensajes de validación en producción (genéricos donde proceda). | No revelar reglas exactas de contraseña en respuesta. |
| **P3 (baja)** | Añadir tests E2E automatizados que cubran la tabla de este informe (Postman/Newman o integration tests). | Suite estable y repetible en CI. |

---

## 7. Score funcional 0–100

Criterios y pesos (orientados a comportamiento correcto y seguridad funcional):

| Criterio | Peso | Puntuación (0–10) | Notas |
|----------|------|-------------------|--------|
| Autenticación (registro/login, tokens, validaciones) | 25% | 4 | Register Admin aceptado; resto correcto. |
| Flujo de servicio (estados, roles, transiciones) | 25% | 9 | Flujo correcto; completar lo hace Customer (diseño actual). |
| Autorización e IDOR (ownership, 403 donde corresponde) | 25% | 6 | Fallo en proposals (Customer); resto bien. |
| Validaciones de negocio (duplicados, estados inválidos) | 15% | 10 | Proposal duplicada, review duplicada, estados. |
| Robustez (JSON, campos extra, rate limit, no stack) | 10% | 9 | Sin fallos relevantes. |

**Cálculo:** (4×0.25 + 9×0.25 + 6×0.25 + 10×0.15 + 9×0.10) × 10 = **6.85** → **Score: 68/100**.

**Score tras correcciones P0 y P1:** (8×0.25 + 9×0.25 + 9×0.25 + 10×0.15 + 9×0.10) × 10 = **88/100**.

---

## 8. Conclusión

- **Si el sistema pasara todo:** No es el caso; A2 (Register Admin) y C4 (proposals Customer) fallan. No se puede justificar un “pasa todo” sin corregir estos puntos.
- **Impacto de los fallos:** El fallo de registro Admin es **bloqueante para producción** (escalación de privilegios). El fallo de propuestas para Customer es **alto** (lógica incorrecta y riesgo IDOR si se parchea sin validar propiedad).

**Recomendación:** No desplegar a producción hasta aplicar al menos las correcciones P0 y P1 y re-ejecutar las pruebas (manuales o automatizadas) que cubran los escenarios de este informe.

---

*Informe: docs/QA_FUNCTIONAL_REPORT.md. Referencia: docs/AUDIT/FINAL_REPORT.md, 05_E2E_EXECUTION.md, 06_FUNCTIONAL_RESULTS.md.*
