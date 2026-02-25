# FixHub — Matriz de Pruebas Funcionales (Harvard Level)

| Metadato | Valor |
|---|---|
| Preparado por | QA Lead — Functional Audit |
| Fecha | 2026-02-25 |
| Baseline | `docs/QA/00_BASELINE_CONFIRMATION.md` |
| Prefijo datos | `FUNC_<timestamp>` (ej: FUNC_20260225_180000) |
| Entorno | Solo local / SIT / QA — prohibido producción |
| Total casos | 51 |
| P0 (Bloqueante) | 29 |
| P1 (Alta) | 17 |
| P2 (Media/Baja) | 5 |

---

## SECCIÓN A — CUSTOMER (Cliente)

---

### TC-001
| Campo | Detalle |
|---|---|
| **ID** | TC-001 |
| **Rol** | Customer |
| **Acción del usuario** | El cliente se registra en la plataforma con datos válidos |
| **Endpoint(s)** | `POST /api/v1/auth/register` |
| **Precondiciones** | Ninguna. Email no debe existir en BD. |
| **Pasos exactos** | 1. `POST /api/v1/auth/register` con body: `{"fullName":"FUNC_<ts> Customer","email":"FUNC_<ts>_cust@test.local","password":"Password1!","role":1,"phone":"+50712345678"}` |
| **Resultado esperado** | HTTP 201. Body contiene: `userId` (GUID), `email` igual al enviado, `role` = "Customer", `token` (JWT no vacío), `fullName`. |
| **Evidencia requerida** | Request body (sin password), response body (redactar token mostrando solo primeros 10 chars + "..."), status code, timestamp. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — sin registro, ningún flujo Customer es posible |

---

### TC-002
| Campo | Detalle |
|---|---|
| **ID** | TC-002 |
| **Rol** | Customer |
| **Acción del usuario** | El cliente inicia sesión con email y contraseña correctos |
| **Endpoint(s)** | `POST /api/v1/auth/login` |
| **Precondiciones** | Usuario Customer registrado (TC-001 exitoso) |
| **Pasos exactos** | 1. `POST /api/v1/auth/login` con body: `{"email":"FUNC_<ts>_cust@test.local","password":"Password1!"}` |
| **Resultado esperado** | HTTP 200. Body: `token` (JWT), `userId`, `email`, `role` = "Customer". |
| **Evidencia requerida** | Request (sin password), response (redactar token), status code. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — sin login, el token no se puede obtener |

---

### TC-003
| Campo | Detalle |
|---|---|
| **ID** | TC-003 |
| **Rol** | Customer |
| **Acción del usuario** | El cliente crea una solicitud de servicio con todos los campos válidos |
| **Endpoint(s)** | `POST /api/v1/jobs` |
| **Precondiciones** | Token Customer válido (TC-002). CategoryId=1 existe en BD. |
| **Pasos exactos** | 1. `POST /api/v1/jobs` con header `Authorization: Bearer <customerToken>` y body: `{"categoryId":1,"title":"FUNC_<ts> Reparación Plomería","description":"Se requiere revisión urgente de tubería","addressText":"Calle 50, Ciudad de Panamá","lat":8.9936,"lng":-79.5197,"budgetMin":50.00,"budgetMax":200.00}` |
| **Resultado esperado** | HTTP 201. Body `JobDto`: `id` (GUID), `status` = "Open" o "Assigned" (si hay técnico disponible), `categoryName` = "Plomería", `customerId` = ID del Customer logueado, `title` contiene "FUNC_". Guardar `jobId` para TCs siguientes. |
| **Evidencia requerida** | Request body completo, response body (jobId, status, categoryName), status 201, timestamp. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — flujo principal de negocio |

---

### TC-004
| Campo | Detalle |
|---|---|
| **ID** | TC-004 |
| **Rol** | Customer |
| **Acción del usuario** | El cliente intenta crear una solicitud sin título (validación de campo requerido) |
| **Endpoint(s)** | `POST /api/v1/jobs` |
| **Precondiciones** | Token Customer válido |
| **Pasos exactos** | 1. `POST /api/v1/jobs` con `Authorization: Bearer <customerToken>` y body sin campo `title`: `{"categoryId":1,"description":"Descripción","addressText":"Dirección"}` |
| **Resultado esperado** | HTTP 400. Body tipo ProblemDetails con `errors.Title` o `errors.title` no vacío. Sin stacktrace en body. |
| **Evidencia requerida** | Response body (campo de error y mensaje), status 400. |
| **Prioridad** | P1 |
| **Severidad si falla** | Alta — validación incorrecta puede generar datos corruptos en BD |

---

### TC-005
| Campo | Detalle |
|---|---|
| **ID** | TC-005 |
| **Rol** | Customer |
| **Acción del usuario** | El cliente intenta crear una solicitud con categoría inexistente |
| **Endpoint(s)** | `POST /api/v1/jobs` |
| **Precondiciones** | Token Customer válido |
| **Pasos exactos** | 1. `POST /api/v1/jobs` con `Authorization: Bearer <customerToken>` y body: `{"categoryId":99999,"title":"FUNC_<ts> Test","description":"Test","addressText":"Test"}` |
| **Resultado esperado** | HTTP 400 o 404. Body con error indicando categoría no encontrada. Sin 201. |
| **Evidencia requerida** | Response body con código/mensaje de error, status code. |
| **Prioridad** | P1 |
| **Severidad si falla** | Alta — puede crear jobs con categoría inválida |

---

### TC-006
| Campo | Detalle |
|---|---|
| **ID** | TC-006 |
| **Rol** | Customer |
| **Acción del usuario** | El cliente consulta el listado de sus propias solicitudes |
| **Endpoint(s)** | `GET /api/v1/jobs/mine` |
| **Precondiciones** | Token Customer con al menos un job creado (TC-003 exitoso) |
| **Pasos exactos** | 1. `GET /api/v1/jobs/mine?page=1&pageSize=20` con `Authorization: Bearer <customerToken>` |
| **Resultado esperado** | HTTP 200. Body: `{ items: [...], totalCount: ≥1, page: 1, pageSize: 20 }`. Al menos un item con `id` = jobId creado en TC-003. |
| **Evidencia requerida** | Response body (estructura PagedResult, al menos primer item con jobId y status), status 200. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — cliente no puede ver sus propias solicitudes |

---

### TC-007
| Campo | Detalle |
|---|---|
| **ID** | TC-007 |
| **Rol** | Customer |
| **Acción del usuario** | El cliente ve el detalle de una solicitud propia |
| **Endpoint(s)** | `GET /api/v1/jobs/{id}` |
| **Precondiciones** | Token Customer. jobId del TC-003 disponible. |
| **Pasos exactos** | 1. `GET /api/v1/jobs/{jobId}` con `Authorization: Bearer <customerToken>` |
| **Resultado esperado** | HTTP 200. Body `JobDto` con `id` = jobId, `customerId` = Customer ID, `title` con "FUNC_", `status` válido. |
| **Evidencia requerida** | Response body (id, customerId, status, title), status 200. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — cliente no puede ver detalle de su solicitud |

---

### TC-008
| Campo | Detalle |
|---|---|
| **ID** | TC-008 |
| **Rol** | Customer |
| **Acción del usuario** | El cliente intenta ver el detalle de una solicitud de otro cliente (control de acceso) |
| **Endpoint(s)** | `GET /api/v1/jobs/{id}` |
| **Precondiciones** | Token Customer A. Un job creado por Customer B (diferente usuario). |
| **Pasos exactos** | 1. Registrar Customer B y crear un job. 2. Con token de Customer A: `GET /api/v1/jobs/{jobId-de-B}` con `Authorization: Bearer <customerA-Token>` |
| **Resultado esperado** | HTTP 403. Body con mensaje de acceso denegado. Customer A no debe ver jobs de Customer B. |
| **Evidencia requerida** | Response body, status 403. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — IDOR: fuga de información de otros clientes |

---

### TC-009
| Campo | Detalle |
|---|---|
| **ID** | TC-009 |
| **Rol** | Customer |
| **Acción del usuario** | El cliente intenta ver las propuestas de su propia solicitud (**Bug conocido H04**) |
| **Endpoint(s)** | `GET /api/v1/jobs/{id}/proposals` |
| **Precondiciones** | Token Customer. Job propio con al menos una propuesta enviada por un Technician. |
| **Pasos exactos** | 1. `GET /api/v1/jobs/{jobId}/proposals` con `Authorization: Bearer <customerToken>` |
| **Resultado esperado** | **ESPERADO CORRECTO:** HTTP 200 con lista de propuestas del job (≥1 item si hay propuestas). **RESULTADO ACTUAL CONOCIDO (Bug H04):** HTTP 200 con lista vacía `[]`. |
| **Evidencia requerida** | Response body completo (lista), status 200. Registrar como BUG si lista vacía siendo dueño con propuestas existentes. |
| **Prioridad** | P0 |
| **Severidad si falla** | Alta — cliente no puede evaluar propuestas; H04 debe registrarse si se reproduce |

---

### TC-010
| Campo | Detalle |
|---|---|
| **ID** | TC-010 |
| **Rol** | Customer |
| **Acción del usuario** | El cliente cancela una solicitud en estado Open |
| **Endpoint(s)** | `POST /api/v1/jobs/{id}/cancel` |
| **Precondiciones** | Token Customer. Job propio en estado Open. |
| **Pasos exactos** | 1. Crear job nuevo en estado Open (sin propuesta aceptada). 2. `POST /api/v1/jobs/{jobId}/cancel` con `Authorization: Bearer <customerToken>` |
| **Resultado esperado** | HTTP 200. Body `JobDto` con `status` = "Cancelled", `cancelledAt` no nulo. |
| **Evidencia requerida** | Response body (status Cancelled, cancelledAt), status 200. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — cliente no puede cancelar solicitudes inválidas |

---

### TC-011
| Campo | Detalle |
|---|---|
| **ID** | TC-011 |
| **Rol** | Customer |
| **Acción del usuario** | El cliente intenta cancelar una solicitud ya completada (transición inválida) |
| **Endpoint(s)** | `POST /api/v1/jobs/{id}/cancel` |
| **Precondiciones** | Token Customer. Job propio en estado Completed. |
| **Pasos exactos** | 1. Job debe estar en Completed (ejecutar flujo completo o admin override). 2. `POST /api/v1/jobs/{completedJobId}/cancel` con `Authorization: Bearer <customerToken>` |
| **Resultado esperado** | HTTP 400. Mensaje de error indicando estado inválido para cancelar. |
| **Evidencia requerida** | Response body con error/código, status 400. |
| **Prioridad** | P1 |
| **Severidad si falla** | Alta — permite anular jobs ya terminados |

---

### TC-012
| Campo | Detalle |
|---|---|
| **ID** | TC-012 |
| **Rol** | Customer |
| **Acción del usuario** | El cliente completa una solicitud en estado InProgress |
| **Endpoint(s)** | `POST /api/v1/jobs/{id}/complete` |
| **Precondiciones** | Token Customer. Job propio en estado InProgress (técnico inició). |
| **Pasos exactos** | 1. Job debe estar en InProgress. 2. `POST /api/v1/jobs/{jobId}/complete` con `Authorization: Bearer <customerToken>` |
| **Resultado esperado** | HTTP 200. Body `JobDto` con `status` = "Completed", `completedAt` no nulo. |
| **Evidencia requerida** | Response (status Completed, completedAt), status 200. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — cliente no puede marcar trabajo como completado |

---

### TC-013
| Campo | Detalle |
|---|---|
| **ID** | TC-013 |
| **Rol** | Customer |
| **Acción del usuario** | El cliente intenta completar una solicitud en estado Open (transición inválida) |
| **Endpoint(s)** | `POST /api/v1/jobs/{id}/complete` |
| **Precondiciones** | Token Customer. Job propio en estado Open. |
| **Pasos exactos** | 1. Crear job en Open. 2. `POST /api/v1/jobs/{jobId}/complete` con `Authorization: Bearer <customerToken>` |
| **Resultado esperado** | HTTP 400. Error de estado inválido. |
| **Evidencia requerida** | Response body con error, status 400. |
| **Prioridad** | P1 |
| **Severidad si falla** | Alta — permite completar jobs sin trabajo realizado |

---

### TC-014
| Campo | Detalle |
|---|---|
| **ID** | TC-014 |
| **Rol** | Customer |
| **Acción del usuario** | El cliente deja una calificación (review) para el servicio completado |
| **Endpoint(s)** | `POST /api/v1/reviews` |
| **Precondiciones** | Token Customer. Job propio en estado Completed, sin review previa. TechnicianId del job disponible. |
| **Pasos exactos** | 1. `POST /api/v1/reviews` con `Authorization: Bearer <customerToken>` y body: `{"jobId":"<jobId>","stars":5,"comment":"FUNC_<ts> Excelente servicio"}` |
| **Resultado esperado** | HTTP 201. Body `ReviewDto`: `id` (GUID), `jobId`, `technicianId`, `stars` = 5, `comment` con "FUNC_", `createdAt`. |
| **Evidencia requerida** | Request body, response body (reviewId, stars, technicianId), status 201. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — sistema de reputación no funciona |

---

### TC-015
| Campo | Detalle |
|---|---|
| **ID** | TC-015 |
| **Rol** | Customer |
| **Acción del usuario** | El cliente intenta dejar una segunda calificación para el mismo servicio (evitar duplicados) |
| **Endpoint(s)** | `POST /api/v1/reviews` |
| **Precondiciones** | Token Customer. Job Completed con review ya creada (TC-014). |
| **Pasos exactos** | 1. `POST /api/v1/reviews` con mismo `jobId` del TC-014, `stars` = 3. |
| **Resultado esperado** | HTTP 409. Error indicando review duplicada. |
| **Evidencia requerida** | Response body con error, status 409. |
| **Prioridad** | P1 |
| **Severidad si falla** | Alta — permite manipular rating de técnico con múltiples reviews |

---

### TC-016
| Campo | Detalle |
|---|---|
| **ID** | TC-016 |
| **Rol** | Customer |
| **Acción del usuario** | El cliente reporta una incidencia en su solicitud |
| **Endpoint(s)** | `POST /api/v1/jobs/{id}/issues` |
| **Precondiciones** | Token Customer. Job propio activo (cualquier estado no-Cancelled). |
| **Pasos exactos** | 1. `POST /api/v1/jobs/{jobId}/issues` con `Authorization: Bearer <customerToken>` y body: `{"reason":"late","detail":"FUNC_<ts> El técnico llegó 2 horas tarde"}` |
| **Resultado esperado** | HTTP 201. Body `IssueDto`: `id` (GUID), `jobId`, `reason` = "late", `detail` con "FUNC_", `createdAt`, `resolvedAt` = null. |
| **Evidencia requerida** | Request body, response body (issueId, reason, detail), status 201. |
| **Prioridad** | P1 |
| **Severidad si falla** | Media — módulo de issues no funcional |

---

### TC-017
| Campo | Detalle |
|---|---|
| **ID** | TC-017 |
| **Rol** | Customer |
| **Acción del usuario** | El cliente consulta sus notificaciones |
| **Endpoint(s)** | `GET /api/v1/notifications` |
| **Precondiciones** | Token Customer. Al menos una notificación generada (ej: por TC-003 - job creado). |
| **Pasos exactos** | 1. `GET /api/v1/notifications?page=1&pageSize=20` con `Authorization: Bearer <customerToken>` |
| **Resultado esperado** | HTTP 200. Body `PagedResult<NotificationDto>`: `items` (lista), `totalCount` ≥ 0. Cada item: `id`, `userId`, `type`, `message`, `isRead`. |
| **Evidencia requerida** | Response body (estructura, primer item), status 200. |
| **Prioridad** | P1 |
| **Severidad si falla** | Media — cliente sin visibilidad de eventos de su cuenta |

---

### TC-018
| Campo | Detalle |
|---|---|
| **ID** | TC-018 |
| **Rol** | Customer |
| **Acción del usuario** | El cliente marca una notificación como leída |
| **Endpoint(s)** | `POST /api/v1/notifications/{id}/read` |
| **Precondiciones** | Token Customer. Notificación propia no leída (obtener ID del TC-017). |
| **Pasos exactos** | 1. Obtener `notificationId` del listado (TC-017 donde `isRead: false`). 2. `POST /api/v1/notifications/{notificationId}/read` con `Authorization: Bearer <customerToken>` |
| **Resultado esperado** | HTTP 204. Sin body. La notificación debe aparecer con `isRead: true` en subsiguiente GET. |
| **Evidencia requerida** | Status 204. Opcionalmente GET /notifications después para verificar isRead=true. |
| **Prioridad** | P2 |
| **Severidad si falla** | Baja — funcionalidad complementaria |

---

## SECCIÓN B — TECHNICIAN (Técnico)

---

### TC-019
| Campo | Detalle |
|---|---|
| **ID** | TC-019 |
| **Rol** | Technician |
| **Acción del usuario** | El técnico se registra en la plataforma |
| **Endpoint(s)** | `POST /api/v1/auth/register` |
| **Precondiciones** | Ninguna. Email no debe existir. |
| **Pasos exactos** | 1. `POST /api/v1/auth/register` con body: `{"fullName":"FUNC_<ts> Técnico","email":"FUNC_<ts>_tech@test.local","password":"Password1!","role":2}` |
| **Resultado esperado** | HTTP 201. Body: `role` = "Technician", `token` presente. TechnicianProfile creado con `Status = Pending`. |
| **Evidencia requerida** | Response body (role, userId), status 201. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — técnicos no pueden registrarse |

---

### TC-020
| Campo | Detalle |
|---|---|
| **ID** | TC-020 |
| **Rol** | Technician |
| **Acción del usuario** | El técnico consulta los trabajos disponibles (Open) |
| **Endpoint(s)** | `GET /api/v1/jobs` |
| **Precondiciones** | Token Technician válido. Al menos un job en estado Open en BD. |
| **Pasos exactos** | 1. `GET /api/v1/jobs?page=1&pageSize=20` con `Authorization: Bearer <technicianToken>` |
| **Resultado esperado** | HTTP 200. Body `PagedResult<JobDto>`: `items` con jobs visibles para Technician. Customer NO puede usar este endpoint (ver TC-029). |
| **Evidencia requerida** | Response body (items, totalCount), status 200. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — técnico no puede ver trabajos disponibles |

---

### TC-021
| Campo | Detalle |
|---|---|
| **ID** | TC-021 |
| **Rol** | Technician |
| **Acción del usuario** | El técnico ve el detalle de un trabajo disponible |
| **Endpoint(s)** | `GET /api/v1/jobs/{id}` |
| **Precondiciones** | Token Technician. Job Open disponible en BD. |
| **Pasos exactos** | 1. `GET /api/v1/jobs/{jobId-Open}` con `Authorization: Bearer <technicianToken>` |
| **Resultado esperado** | HTTP 200. Body `JobDto` completo. |
| **Evidencia requerida** | Response body (id, status, title), status 200. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — técnico no puede evaluar trabajo antes de proponer |

---

### TC-022
| Campo | Detalle |
|---|---|
| **ID** | TC-022 |
| **Rol** | Technician |
| **Acción del usuario** | El técnico envía una propuesta a un trabajo abierto |
| **Endpoint(s)** | `POST /api/v1/jobs/{id}/proposals` |
| **Precondiciones** | Token Technician (Approved o Pending — verificar si hay restricción). Job en estado Open. Técnico no tiene propuesta previa en ese job. |
| **Pasos exactos** | 1. `POST /api/v1/jobs/{jobId}/proposals` con `Authorization: Bearer <technicianToken>` y body: `{"price":150.00,"message":"FUNC_<ts> Tengo 5 años de experiencia en plomería"}` |
| **Resultado esperado** | HTTP 201. Body `ProposalDto`: `id` (GUID, guardar como proposalId), `jobId`, `technicianId`, `price` = 150, `status` = "Pending". |
| **Evidencia requerida** | Request body, response body (proposalId, status Pending), status 201. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — técnicos no pueden competir por trabajos |

---

### TC-023
| Campo | Detalle |
|---|---|
| **ID** | TC-023 |
| **Rol** | Technician |
| **Acción del usuario** | El técnico intenta enviar una segunda propuesta al mismo trabajo (duplicado) |
| **Endpoint(s)** | `POST /api/v1/jobs/{id}/proposals` |
| **Precondiciones** | Token Technician. Ya envió propuesta a ese job (TC-022). |
| **Pasos exactos** | 1. `POST /api/v1/jobs/{jobId}/proposals` con mismo token y body: `{"price":100.00,"message":"FUNC_<ts> Segunda propuesta"}` |
| **Resultado esperado** | HTTP 400 o 409. Error de propuesta duplicada. |
| **Evidencia requerida** | Response body (error code/message), status 400/409. |
| **Prioridad** | P1 |
| **Severidad si falla** | Alta — técnico puede inflar su presencia en un job |

---

### TC-024
| Campo | Detalle |
|---|---|
| **ID** | TC-024 |
| **Rol** | Technician |
| **Acción del usuario** | El técnico consulta sus asignaciones actuales |
| **Endpoint(s)** | `GET /api/v1/technicians/me/assignments` |
| **Precondiciones** | Token Technician con al menos una asignación (propuesta aceptada por Admin). |
| **Pasos exactos** | 1. `GET /api/v1/technicians/me/assignments?page=1&pageSize=20` con `Authorization: Bearer <technicianToken>` |
| **Resultado esperado** | HTTP 200. Body `PagedResult<AssignmentDto>`: items con `jobId`, `jobTitle`, `proposalId`, `acceptedAt`. |
| **Evidencia requerida** | Response body (al menos primer item), status 200. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — técnico no sabe qué trabajos tiene asignados |

---

### TC-025
| Campo | Detalle |
|---|---|
| **ID** | TC-025 |
| **Rol** | Technician |
| **Acción del usuario** | El técnico inicia un trabajo que le fue asignado (Assigned → InProgress) |
| **Endpoint(s)** | `POST /api/v1/jobs/{id}/start` |
| **Precondiciones** | Token Technician. Job en estado Assigned con este técnico como asignado. |
| **Pasos exactos** | 1. `POST /api/v1/jobs/{assignedJobId}/start` con `Authorization: Bearer <technicianToken>` |
| **Resultado esperado** | HTTP 200. Body `JobDto` con `status` = "InProgress", `startedAt` no nulo. |
| **Evidencia requerida** | Response body (status InProgress, startedAt), status 200. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — técnico no puede comenzar a trabajar |

---

### TC-026
| Campo | Detalle |
|---|---|
| **ID** | TC-026 |
| **Rol** | Technician |
| **Acción del usuario** | Un técnico diferente intenta iniciar un trabajo asignado a otro técnico (control de acceso) |
| **Endpoint(s)** | `POST /api/v1/jobs/{id}/start` |
| **Precondiciones** | Token Technician B. Job Assigned a Technician A (diferente). |
| **Pasos exactos** | 1. Registrar Technician B. 2. Con token de Technician B: `POST /api/v1/jobs/{jobId-asignado-a-A}/start` con `Authorization: Bearer <technicianB-token>` |
| **Resultado esperado** | HTTP 403. Acceso denegado. Técnico B no puede iniciar trabajo de Técnico A. |
| **Evidencia requerida** | Response body, status 403. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — cualquier técnico podría iniciar trabajos ajenos |

---

### TC-027
| Campo | Detalle |
|---|---|
| **ID** | TC-027 |
| **Rol** | Technician |
| **Acción del usuario** | El técnico intenta iniciar un trabajo en estado Open (sin asignación previa) |
| **Endpoint(s)** | `POST /api/v1/jobs/{id}/start` |
| **Precondiciones** | Token Technician. Job en estado Open (no asignado). |
| **Pasos exactos** | 1. `POST /api/v1/jobs/{openJobId}/start` con `Authorization: Bearer <technicianToken>` |
| **Resultado esperado** | HTTP 400 o 403. Error de estado inválido o acceso denegado. |
| **Evidencia requerida** | Response body con error, status code. |
| **Prioridad** | P1 |
| **Severidad si falla** | Alta — técnico inicia jobs sin estar asignado |

---

### TC-028
| Campo | Detalle |
|---|---|
| **ID** | TC-028 |
| **Rol** | Technician |
| **Acción del usuario** | Verificar que el Technician NO puede completar un job (solo Customer puede completar) |
| **Endpoint(s)** | `POST /api/v1/jobs/{id}/complete` |
| **Precondiciones** | Token Technician. Job en estado InProgress. |
| **Pasos exactos** | 1. `POST /api/v1/jobs/{jobId}/complete` con `Authorization: Bearer <technicianToken>` |
| **Resultado esperado** | HTTP 403. Policy "CustomerOnly" rechaza al Technician. |
| **Evidencia requerida** | Status 403. |
| **Prioridad** | P1 |
| **Severidad si falla** | Alta — Technician podría marcar trabajo como completado sin validación del cliente |

---

### TC-029
| Campo | Detalle |
|---|---|
| **ID** | TC-029 |
| **Rol** | Technician |
| **Acción del usuario** | El técnico intenta acceder a la ruta exclusiva de Customer `/jobs/mine` |
| **Endpoint(s)** | `GET /api/v1/jobs/mine` |
| **Precondiciones** | Token Technician válido |
| **Pasos exactos** | 1. `GET /api/v1/jobs/mine` con `Authorization: Bearer <technicianToken>` |
| **Resultado esperado** | HTTP 403. Policy "CustomerOnly" bloquea al Technician. |
| **Evidencia requerida** | Status 403. |
| **Prioridad** | P1 |
| **Severidad si falla** | Media — Technician podría ver listados de Customer |

---

## SECCIÓN C — ADMIN / OPERACIONES

---

### TC-030
| Campo | Detalle |
|---|---|
| **ID** | TC-030 |
| **Rol** | Admin (Bug H03) |
| **Acción del usuario** | Cualquier persona intenta registrarse con rol Admin (debe fallar — Bug H03) |
| **Endpoint(s)** | `POST /api/v1/auth/register` |
| **Precondiciones** | Ninguna. |
| **Pasos exactos** | 1. `POST /api/v1/auth/register` con body: `{"fullName":"FUNC_<ts> FakeAdmin","email":"FUNC_<ts>_fakeadmin@test.local","password":"Password1!","role":3}` |
| **Resultado esperado** | **CORRECTO:** HTTP 400 o 403 — Role Admin no permitido en registro. **ACTUAL (Bug H03):** HTTP 201, usuario Admin creado. Si 201: REGISTRAR BUG H03. |
| **Evidencia requerida** | Request body (role=3), response body completo, status code. Documentar BUG si status=201. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — escalación de privilegios crítica |

---

### TC-031
| Campo | Detalle |
|---|---|
| **ID** | TC-031 |
| **Rol** | Admin |
| **Acción del usuario** | El admin consulta el dashboard operativo |
| **Endpoint(s)** | `GET /api/v1/admin/dashboard` |
| **Precondiciones** | Token Admin (admin@fixhub.com / Admin123!). |
| **Pasos exactos** | 1. Login admin: `POST /api/v1/auth/login` `{"email":"admin@fixhub.com","password":"Admin123!"}`. 2. `GET /api/v1/admin/dashboard` con `Authorization: Bearer <adminToken>` |
| **Resultado esperado** | HTTP 200. Body `OpsDashboardDto`: `kpis` (TotalJobs, OpenJobs, etc.), `alertJobs` (lista), `recentJobs` (lista). |
| **Evidencia requerida** | Response body (kpis con valores numéricos), status 200. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — admin sin visibilidad del sistema |

---

### TC-032
| Campo | Detalle |
|---|---|
| **ID** | TC-032 |
| **Rol** | Admin |
| **Acción del usuario** | El admin lista todas las solicitudes con filtro por estado |
| **Endpoint(s)** | `GET /api/v1/jobs` |
| **Precondiciones** | Token Admin. Jobs en BD. |
| **Pasos exactos** | 1. `GET /api/v1/jobs?status=1&page=1&pageSize=20` (status=1=Open) con `Authorization: Bearer <adminToken>` |
| **Resultado esperado** | HTTP 200. Body `PagedResult<JobDto>`: items (solo Open), totalCount. |
| **Evidencia requerida** | Response body (items, totalCount, todos con status=Open), status 200. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — admin sin visibilidad de solicitudes |

---

### TC-033
| Campo | Detalle |
|---|---|
| **ID** | TC-033 |
| **Rol** | Admin |
| **Acción del usuario** | El admin ve todas las propuestas de un job |
| **Endpoint(s)** | `GET /api/v1/jobs/{id}/proposals` |
| **Precondiciones** | Token Admin. Job con al menos una propuesta. |
| **Pasos exactos** | 1. `GET /api/v1/jobs/{jobId}/proposals` con `Authorization: Bearer <adminToken>` |
| **Resultado esperado** | HTTP 200. Lista completa de `ProposalDto` del job (≥1). Cada item: `id`, `technicianId`, `price`, `status`. |
| **Evidencia requerida** | Response body (lista de propuestas con datos), status 200. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — admin no puede seleccionar técnico |

---

### TC-034
| Campo | Detalle |
|---|---|
| **ID** | TC-034 |
| **Rol** | Admin |
| **Acción del usuario** | El admin asigna un técnico aceptando una propuesta (flujo principal de asignación) |
| **Endpoint(s)** | `POST /api/v1/proposals/{id}/accept` |
| **Precondiciones** | Token Admin. Propuesta Pending de un Technician Approved en job Open. proposalId disponible. |
| **Pasos exactos** | 1. `POST /api/v1/proposals/{proposalId}/accept` con `Authorization: Bearer <adminToken>` |
| **Resultado esperado** | HTTP 200. Body `AcceptProposalResponse`: `assignmentId`, `jobId`, `proposalId`, `technicianId`, `technicianName`, `acceptedPrice`, `acceptedAt`. Job cambia a "Assigned". |
| **Evidencia requerida** | Request (proposalId en URL), response body completo (assignmentId, jobId, status Assigned), status 200. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — sin asignación el flujo principal no funciona |

---

### TC-035
| Campo | Detalle |
|---|---|
| **ID** | TC-035 |
| **Rol** | Admin |
| **Acción del usuario** | El admin fuerza el inicio de un job (override administrativo) |
| **Endpoint(s)** | `POST /api/v1/admin/jobs/{id}/start` |
| **Precondiciones** | Token Admin. Job en estado Open o Assigned. |
| **Pasos exactos** | 1. `POST /api/v1/admin/jobs/{jobId}/start` con `Authorization: Bearer <adminToken>` |
| **Resultado esperado** | HTTP 200. Body `JobDto` con `status` = "InProgress". |
| **Evidencia requerida** | Response body (status InProgress), status 200. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — admin sin capacidad de intervención operacional |

---

### TC-036
| Campo | Detalle |
|---|---|
| **ID** | TC-036 |
| **Rol** | Admin |
| **Acción del usuario** | El admin cambia el estado de un job a Completed (override) |
| **Endpoint(s)** | `PATCH /api/v1/admin/jobs/{id}/status` |
| **Precondiciones** | Token Admin. Job en estado InProgress. |
| **Pasos exactos** | 1. `PATCH /api/v1/admin/jobs/{jobId}/status` con `Authorization: Bearer <adminToken>` y body: `{"newStatus":"Completed"}` |
| **Resultado esperado** | HTTP 200. Body `JobDto` con `status` = "Completed". |
| **Evidencia requerida** | Request body, response body (status Completed), status 200. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — admin sin capacidad de cerrar trabajos operativos |

---

### TC-037
| Campo | Detalle |
|---|---|
| **ID** | TC-037 |
| **Rol** | Admin |
| **Acción del usuario** | El admin consulta la lista de técnicos en proceso de reclutamiento |
| **Endpoint(s)** | `GET /api/v1/admin/applicants` |
| **Precondiciones** | Token Admin. Al menos un Technician registrado (cualquier status). |
| **Pasos exactos** | 1. `GET /api/v1/admin/applicants?page=1&pageSize=20` con `Authorization: Bearer <adminToken>` |
| **Resultado esperado** | HTTP 200. Body `PagedResult<ApplicantDto>`: items con `userId`, `fullName`, `email`, `status`. |
| **Evidencia requerida** | Response body (items, totalCount), status 200. |
| **Prioridad** | P1 |
| **Severidad si falla** | Media — admin sin visibilidad de pipeline de técnicos |

---

### TC-038
| Campo | Detalle |
|---|---|
| **ID** | TC-038 |
| **Rol** | Admin |
| **Acción del usuario** | El admin aprueba un técnico (Pending → Approved) |
| **Endpoint(s)** | `PATCH /api/v1/admin/technicians/{id}/status` |
| **Precondiciones** | Token Admin. TechnicianProfile con Status=Pending. technicianUserId disponible. |
| **Pasos exactos** | 1. `PATCH /api/v1/admin/technicians/{technicianUserId}/status` con `Authorization: Bearer <adminToken>` y body: `{"status":2}` (2=Approved) |
| **Resultado esperado** | HTTP 204. Sin body. TechnicianProfile.Status cambia a Approved (verificable via GET profile). |
| **Evidencia requerida** | Status 204. Opcionalmente GET /technicians/{id}/profile para verificar Status=Approved. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — sin aprobación, técnico no puede recibir asignaciones |

---

### TC-039
| Campo | Detalle |
|---|---|
| **ID** | TC-039 |
| **Rol** | Admin |
| **Acción del usuario** | El admin consulta el listado de incidencias reportadas |
| **Endpoint(s)** | `GET /api/v1/admin/issues` |
| **Precondiciones** | Token Admin. Al menos un issue reportado (TC-016). |
| **Pasos exactos** | 1. `GET /api/v1/admin/issues?page=1&pageSize=20` con `Authorization: Bearer <adminToken>` |
| **Resultado esperado** | HTTP 200. Body `PagedResult<IssueDto>`: items con `id`, `jobId`, `reason`, `detail`, `createdAt`, `resolvedAt` (null si no resuelto). |
| **Evidencia requerida** | Response body (items), status 200. |
| **Prioridad** | P1 |
| **Severidad si falla** | Media — admin no puede gestionar reclamaciones |

---

### TC-040
| Campo | Detalle |
|---|---|
| **ID** | TC-040 |
| **Rol** | Admin |
| **Acción del usuario** | El admin resuelve una incidencia reportada |
| **Endpoint(s)** | `POST /api/v1/admin/issues/{id}/resolve` |
| **Precondiciones** | Token Admin. Issue existente sin resolver (issueId del TC-016 o TC-039). |
| **Pasos exactos** | 1. `POST /api/v1/admin/issues/{issueId}/resolve` con `Authorization: Bearer <adminToken>` y body: `{"resolutionNote":"FUNC_<ts> Issue resuelto: técnico contactado y reprogramado"}` |
| **Resultado esperado** | HTTP 204. Sin body. Issue marcado como resuelto (verificable via GET /admin/issues). |
| **Evidencia requerida** | Request body (resolutionNote), status 204. |
| **Prioridad** | P1 |
| **Severidad si falla** | Media — issues no pueden cerrarse |

---

### TC-041
| Campo | Detalle |
|---|---|
| **ID** | TC-041 |
| **Rol** | Admin |
| **Acción del usuario** | El admin consulta métricas operacionales del sistema |
| **Endpoint(s)** | `GET /api/v1/admin/metrics` |
| **Precondiciones** | Token Admin. |
| **Pasos exactos** | 1. `GET /api/v1/admin/metrics` con `Authorization: Bearer <adminToken>` |
| **Resultado esperado** | HTTP 200. Body `AdminMetricsDto`: `emailsSent`, `slaAlertCount`, `avgTimeToAssignMinutes`, `avgTimeToCompleteMinutes`, `issueCount`, `issuesResolvedCount`. |
| **Evidencia requerida** | Response body (estructura con campos numéricos), status 200. |
| **Prioridad** | P2 |
| **Severidad si falla** | Baja — métricas complementarias |

---

### TC-042
| Campo | Detalle |
|---|---|
| **ID** | TC-042 |
| **Rol** | Customer (test de acceso denegado a área Admin) |
| **Acción del usuario** | Un Customer intenta acceder al dashboard Admin (debe fallar) |
| **Endpoint(s)** | `GET /api/v1/admin/dashboard` |
| **Precondiciones** | Token Customer válido. |
| **Pasos exactos** | 1. `GET /api/v1/admin/dashboard` con `Authorization: Bearer <customerToken>` |
| **Resultado esperado** | HTTP 403. Policy "AdminOnly" bloquea al Customer. |
| **Evidencia requerida** | Status 403. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — Customer con acceso a datos Admin |

---

## SECCIÓN D — END-TO-END REAL

---

### TC-043
| Campo | Detalle |
|---|---|
| **ID** | TC-043 |
| **Rol** | E2E (Customer + Admin + Technician) |
| **Acción del usuario** | **Flujo Feliz Completo:** Cliente crea solicitud → Admin asigna técnico → Técnico inicia → Cliente completa → Cliente califica |
| **Endpoint(s)** | POST /jobs, GET /jobs/{id}/proposals, POST /proposals/{id}/accept, POST /jobs/{id}/start, POST /jobs/{id}/complete, POST /reviews |
| **Precondiciones** | Customer registrado y logueado. Technician registrado, logueado y aprobado (Admin). Admin logueado. |
| **Pasos exactos** | 1. **[Customer]** `POST /jobs` → guardar jobId. Status=Open. 2. **[Technician]** `POST /jobs/{jobId}/proposals` con price y message → guardar proposalId. 3. **[Admin]** `GET /jobs/{jobId}/proposals` → ver lista. 4. **[Admin]** `POST /proposals/{proposalId}/accept` → job status=Assigned. 5. **[Technician]** `POST /jobs/{jobId}/start` → status=InProgress. 6. **[Customer]** `POST /jobs/{jobId}/complete` → status=Completed. 7. **[Customer]** `POST /reviews` con jobId, stars=4, comment → reviewId. |
| **Resultado esperado** | Cada paso responde 200/201. Estado final: job=Completed, review creada. Transiciones de estado válidas en orden. |
| **Evidencia requerida** | jobId, proposalId, assignmentId, reviewId capturados. Response de cada paso con status code y status del job donde aplica. Timestamp de inicio y fin del flujo. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — flujo central de negocio no funciona |

---

### TC-044
| Campo | Detalle |
|---|---|
| **ID** | TC-044 |
| **Rol** | E2E (Customer) |
| **Acción del usuario** | **Flujo Alterno:** Cliente crea solicitud y la cancela antes de ser asignada |
| **Endpoint(s)** | POST /jobs, POST /jobs/{id}/cancel |
| **Precondiciones** | Customer registrado y logueado. |
| **Pasos exactos** | 1. **[Customer]** `POST /jobs` → jobId. Status=Open. 2. **[Customer]** `POST /jobs/{jobId}/cancel` → status=Cancelled. |
| **Resultado esperado** | Paso 1: HTTP 201, status=Open. Paso 2: HTTP 200, status=Cancelled, cancelledAt no nulo. |
| **Evidencia requerida** | Response de paso 1 (jobId, status Open) y paso 2 (status Cancelled). |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — cliente no puede cancelar solicitudes |

---

### TC-045
| Campo | Detalle |
|---|---|
| **ID** | TC-045 |
| **Rol** | E2E (Technician — acceso denegado) |
| **Acción del usuario** | **Flujo Alterno de Seguridad:** Un técnico no asignado intenta iniciar el trabajo de otro técnico |
| **Endpoint(s)** | POST /jobs/{id}/start |
| **Precondiciones** | Job Assigned a Technician A. Token de Technician B (diferente). |
| **Pasos exactos** | 1. Setup: job Assigned a TechA (vía flujo TC-043 pasos 1-4). 2. **[TechnicianB]** `POST /jobs/{jobId}/start` con token TechB. |
| **Resultado esperado** | HTTP 403. TechB no puede iniciar job de TechA. |
| **Evidencia requerida** | Status 403, response body. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — cualquier técnico puede interferir en trabajos ajenos |

---

### TC-046
| Campo | Detalle |
|---|---|
| **ID** | TC-046 |
| **Rol** | E2E (Customer — transición inválida) |
| **Acción del usuario** | **Flujo Alterno Negativo:** Cliente intenta completar solicitud en estado Open (sin asignación) |
| **Endpoint(s)** | POST /jobs/{id}/complete |
| **Precondiciones** | Token Customer. Job propio en estado Open. |
| **Pasos exactos** | 1. **[Customer]** `POST /jobs` → jobId (status=Open). 2. **[Customer]** `POST /jobs/{jobId}/complete` inmediatamente. |
| **Resultado esperado** | HTTP 400. Error de transición de estado inválida. |
| **Evidencia requerida** | Response body con error, status 400. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — clientes podrían marcar solicitudes no realizadas como completadas |

---

## SECCIÓN E — ROBUSTEZ FUNCIONAL

---

### TC-047
| Campo | Detalle |
|---|---|
| **ID** | TC-047 |
| **Rol** | Cualquiera (Customer) |
| **Acción del usuario** | Enviar JSON malformado al crear una solicitud |
| **Endpoint(s)** | `POST /api/v1/jobs` |
| **Precondiciones** | Token Customer válido. |
| **Pasos exactos** | 1. `POST /api/v1/jobs` con `Content-Type: application/json` y body: `{ "categoryId": 1, "title": "FUNC_test", INVALID_JSON }` |
| **Resultado esperado** | HTTP 400. Body con mensaje de error de deserialización JSON. Sin stacktrace expuesto. |
| **Evidencia requerida** | Response body (mensaje de error sin stack), status 400. |
| **Prioridad** | P1 |
| **Severidad si falla** | Alta — puede exponer detalles internos o causar 500 |

---

### TC-048
| Campo | Detalle |
|---|---|
| **ID** | TC-048 |
| **Rol** | Customer |
| **Acción del usuario** | Enviar campos adicionales no esperados en el body del request |
| **Endpoint(s)** | `POST /api/v1/jobs` |
| **Precondiciones** | Token Customer válido. |
| **Pasos exactos** | 1. `POST /api/v1/jobs` con body válido + campos extra: `{"categoryId":1,"title":"FUNC_<ts> Extra Fields","description":"Test","addressText":"Test","unknownField":"should_be_ignored","internalField":true}` |
| **Resultado esperado** | HTTP 201 (campos extra ignorados) o 400 (si hay validación estricta). En ningún caso 500. Campos extra no deben aparecer en response. |
| **Evidencia requerida** | Response body, status code (201 o 400, no 500). |
| **Prioridad** | P1 |
| **Severidad si falla** | Media — comportamiento inconsistente |

---

### TC-049
| Campo | Detalle |
|---|---|
| **ID** | TC-049 |
| **Rol** | Customer |
| **Acción del usuario** | Intentar completar el mismo job dos veces (idempotencia) |
| **Endpoint(s)** | `POST /api/v1/jobs/{id}/complete` |
| **Precondiciones** | Token Customer. Job ya Completed (TC-012 o TC-043). |
| **Pasos exactos** | 1. Job ya está en Completed. 2. `POST /api/v1/jobs/{completedJobId}/complete` por segunda vez. |
| **Resultado esperado** | HTTP 400 (transición inválida: Completed→Completed no permitida) o HTTP 200 sin cambio si es idempotente. En ningún caso 500 ni datos corruptos. |
| **Evidencia requerida** | Response body, status code. |
| **Prioridad** | P1 |
| **Severidad si falla** | Media — comportamiento ante doble submit |

---

### TC-050
| Campo | Detalle |
|---|---|
| **ID** | TC-050 |
| **Rol** | Cualquiera |
| **Acción del usuario** | Verificar que los errores no exponen stacktrace ni información interna |
| **Endpoint(s)** | `GET /api/v1/jobs/{id}` (ID no existente) |
| **Precondiciones** | Token cualquier usuario autenticado. |
| **Pasos exactos** | 1. `GET /api/v1/jobs/00000000-0000-0000-0000-000000000000` con Bearer token válido. |
| **Resultado esperado** | HTTP 404. Body de tipo ProblemDetails: `{type, title, status, detail}`. Sin `stackTrace`, sin `exception`, sin información de conexión BD. |
| **Evidencia requerida** | Response body completo para verificar ausencia de info sensible, status 404. |
| **Prioridad** | P1 |
| **Severidad si falla** | Alta — exposición de información interna |

---

### TC-051
| Campo | Detalle |
|---|---|
| **ID** | TC-051 |
| **Rol** | — (Sin autenticación) |
| **Acción del usuario** | Verificar el health check del sistema (sin credenciales) |
| **Endpoint(s)** | `GET /api/v1/health` |
| **Precondiciones** | API en ejecución. |
| **Pasos exactos** | 1. `GET /api/v1/health` sin header Authorization. |
| **Resultado esperado** | HTTP 200. Body: `{"status":"healthy","version":"1.0.0","timestamp":"<ISO8601>","database":"connected"}`. |
| **Evidencia requerida** | Response body completo, status 200. |
| **Prioridad** | P0 |
| **Severidad si falla** | Bloqueante — API no está levantada |

---

## Resumen de Prioridades

| Prioridad | IDs | Total |
|---|---|---|
| **P0 (Bloqueante)** | TC-001, TC-002, TC-003, TC-006, TC-007, TC-008, TC-009, TC-010, TC-012, TC-014, TC-019, TC-020, TC-021, TC-022, TC-024, TC-025, TC-026, TC-028, TC-030, TC-031, TC-032, TC-033, TC-034, TC-035, TC-036, TC-038, TC-042, TC-043, TC-044, TC-045, TC-046, TC-051 | 32 |
| **P1 (Alta)** | TC-004, TC-005, TC-011, TC-013, TC-015, TC-016, TC-017, TC-023, TC-027, TC-029, TC-037, TC-039, TC-040, TC-047, TC-048, TC-049, TC-050 | 17 |
| **P2 (Media/Baja)** | TC-018, TC-041 | 2 |
| **TOTAL** | — | **51** |

---

## Convención de Datos de Prueba

| Elemento | Patrón | Ejemplo |
|---|---|---|
| Customer email | `FUNC_<ts>_cust@test.local` | `FUNC_20260225180000_cust@test.local` |
| Technician email | `FUNC_<ts>_tech@test.local` | `FUNC_20260225180000_tech@test.local` |
| Customer 2 email | `FUNC_<ts>_cust2@test.local` | `FUNC_20260225180000_cust2@test.local` |
| Technician 2 email | `FUNC_<ts>_tech2@test.local` | `FUNC_20260225180000_tech2@test.local` |
| FakeAdmin email | `FUNC_<ts>_fakeadmin@test.local` | `FUNC_20260225180000_fakeadmin@test.local` |
| Job title | `FUNC_<ts> <descripcion>` | `FUNC_20260225180000 Reparación Plomería` |
| Password (todos) | `Password1!` | — |
| Admin seed | `admin@fixhub.com` / `Admin123!` | Solo en TC que usan Admin real |

---
*Generado: 2026-02-25 | Rama: audit/fixhub-100*
