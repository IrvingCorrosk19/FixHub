# FixHub — Pruebas de seguridad (OWASP) — Controladas (Fase 2)

**Branch:** `audit/fixhub-100`  
**Alcance:** Pruebas tipo pentest **no destructivas**, ejecutables en local/SIT/QA. Sin tocar PROD.

---

## 1. Escalación de privilegios

### 1.1 Register con Role=Admin

| Prueba | Acción | Resultado esperado | Estado (code review) |
|--------|--------|--------------------|----------------------|
| Register Role Admin | POST /api/v1/auth/register con `"role": 3` (Admin) | **Debe fallar** (400 o rechazo explícito). Actualmente el validator permite `IsInEnum()` y Admin=3; el handler acepta cualquier rol. | **FALLA:** Register acepta Role=Admin (H03). Fix: rechazar `UserRole.Admin` en RegisterCommandValidator o Handler. |

**Evidencia (code):** `src/FixHub.Application/Features/Auth/RegisterCommand.cs` — validator línea 36: `RuleFor(x => x.Role).IsInEnum().Must(r => r != 0)` no excluye Admin (valor 3). Handler asigna `request.Role` sin restricción.

### 1.2 Payload manipulado (role en otro campo)

| Prueba | Acción | Resultado esperado |
|--------|--------|---------------------|
| Campo extra "role" en body | Incluir `"role": 3` en JSON de register; API usa DTO con propiedad Role. | Si el binding acepta Role, mismo fallo que 1.1. Si se corrige 1.1 rechazando Admin, debe devolver 400. |

---

## 2. IDOR (Insecure Direct Object Reference)

### 2.1 Customer intenta ver job de otro Customer

| Prueba | Acción | Resultado esperado | Evidencia |
|--------|--------|--------------------|-----------|
| GET job ajeno | Customer A crea job; Customer B llama GET /api/v1/jobs/{jobId_A} con token B. | **403 Forbidden** | Integration test: `Customer_Cannot_View_Other_Customers_Job_Returns_403`. Handler GetJobQuery comprueba `job.CustomerId != req.RequesterId` para rol Customer. |

### 2.2 Customer intenta ver propuestas de job ajeno

| Prueba | Acción | Resultado esperado | Estado |
|--------|--------|--------------------|--------|
| GET proposals de job ajeno | Customer B llama GET /api/v1/jobs/{jobId_de_A}/proposals. | **Debe 403** si no es dueño. Actualmente no se valida ownership; Customer recibe lista filtrada por TechnicianId (vacía). | **Riesgo:** No hay 403; comportamiento incorrecto (H04). Corregir: validar `job.CustomerId == RequesterId` para Customer. |

### 2.3 Customer intenta ver notificaciones de otro

| Prueba | Acción | Resultado esperado |
|--------|--------|---------------------|
| GET notifications / mark read | Las notificaciones se filtran por UserId en handler (GetMyNotificationsQuery, MarkNotificationReadCommand). Marcar read con ID de notificación ajena. | Solo propias: 200 con datos propios. Marcar read de notificación ajena: 404 (no encontrada) porque la query usa `n.UserId == req.UserId`. | **OK** en código: MarkNotificationReadCommand línea 16 `n.Id == req.NotificationId && n.UserId == req.UserId`. |

### 2.4 Technician intenta ver job sin asignación

| Prueba | Acción | Resultado esperado |
|--------|--------|---------------------|
| GET job (no asignado, sin propuesta) | Technician T2 llama GET /api/v1/jobs/{jobId} donde el job está asignado a T1. | **403 Forbidden** | Integration test: `Technician_Cannot_View_Unassigned_Job_Returns_403`. GetJobQuery: Technician solo ve si isAssigned, isOpen o hasOwnProposal. |

---

## 3. JWT tampering

### 3.1 Token con firma alterada

| Prueba | Acción | Resultado esperado |
|--------|--------|---------------------|
| Firma inválida | Obtener token válido; cambiar un byte en el payload o firma; enviar en Authorization: Bearer. | **401 Unauthorized** | JWT Bearer valida SigningKey; firma alterada falla. |

### 3.2 Token expirado

| Prueba | Acción | Resultado esperado |
|--------|--------|---------------------|
| Token expirado | Usar token con exp en el pasado. | **401 Unauthorized** | TokenValidationParameters.ValidateLifetime = true. |

### 3.3 Role falsificado (sin firma válida)

| Prueba | Acción | Resultado esperado |
|--------|--------|---------------------|
| Decodificar token, cambiar "role" a "Admin", reenviar sin re-firmar | El token no será válido porque la firma no coincidirá. | **401 Unauthorized** | La verificación de firma ocurre antes de leer claims. |

---

## 4. Mass assignment

| Prueba | Acción | Resultado esperado |
|--------|--------|---------------------|
| Register con campos extra | Enviar `"isActive": true`, `"id": "guid"` en body de register. | DTO es record con propiedades fijas; propiedades no definidas en RegisterRequest son ignoradas por System.Text.Json. No hay binding a entidad. | **OK** (sin over-posting a entidad). |
| Create Job con campos extra | Enviar `"id"`, `"customerId"` en body. | CreateJobRequest no incluye id ni customerId; CustomerId viene del token (CurrentUserId). | **OK**. |

---

## 5. Rate limiting

| Prueba | Acción | Resultado esperado |
|--------|--------|---------------------|
| Auth brute force | >10 requests a POST /api/v1/auth/login (o register) en 1 minuto desde misma IP. | **429 Too Many Requests** con errorCode RATE_LIMITED. | Program.cs: AuthPolicy 10 req/min; OnRejected devuelve 429. |

**Evidencia:** `src/FixHub.API/Program.cs` líneas 87–94 (AuthPolicy), 60–74 (OnRejected).

---

## 6. Headers de seguridad

| Header | Valor esperado | Evidencia |
|--------|----------------|-----------|
| X-Content-Type-Options | nosniff | SecurityHeadersMiddleware.cs línea 13. |
| X-Frame-Options | DENY | SecurityHeadersMiddleware.cs línea 14. |
| Referrer-Policy | no-referrer | Línea 15. |
| Permissions-Policy | geolocation=(), camera=(), microphone=() | Línea 16. |
| Content-Security-Policy | No configurado en middleware | **Mejora:** Valorar CSP si la API sirve HTML o si se usa desde Web; para API pura no es obligatorio. |

---

## 7. CORS

| Prueba | Acción | Resultado esperado |
|--------|--------|---------------------|
| Origen permitido | Request desde WebOrigin (config) con credentials. | 200 con header Access-Control-Allow-Origin según WebOrigin. |
| Origen no permitido | Request desde `https://evil.com`. | Sin header Access-Control-Allow-Origin; navegador bloqueará lectura de respuesta. | Program.cs: WithOrigins(allowedOrigin); AllowCredentials(). |

---

## 8. Secretos en repositorio

| Prueba | Acción | Resultado |
|--------|--------|-----------|
| Búsqueda de patrones | Buscar Password=, SecretKey=, ApiKey=, private key, connection string con credenciales en repo. | **Hallazgos:** Ver FINAL_REPORT / auditoría estática. Archivos con riesgo: appsettings.Development.json (REDACTED), deploy-fixhub.ps1 (REDACTED). **No se pegan valores reales en este documento.** |
| Rotación recomendada | Si se detectaron credenciales expuestas: | 1) Rotar contraseña PostgreSQL (dev/prod según alcance). 2) Rotar JWT SecretKey y invalidar tokens existentes si procede. 3) Rotar credenciales SSH/VPS y cualquier secreto en deploy-fixhub.ps1. 4) Mover secretos a User Secrets / variables de entorno / gestor de secretos. 5) Añadir appsettings.Development.json a .gitignore o vaciar valores y documentar. |

---

## 9. Resumen de ejecución (controlado)

Las pruebas **1.1, 2.1, 2.4** están cubiertas por integration tests existentes.  
Las pruebas **1.1 (Register Admin), 2.2 (proposals ownership), 5, 6, 7** pueden ejecutarse manualmente o con Postman/scripts contra entorno local/SIT.  
**No se ejecutaron** pruebas contra PROD ni datos reales.  
**Secreto:** Cualquier valor sensible detectado en repo se trata como REDACTED; pasos de rotación documentados en FINAL_REPORT y 04_CICD_PLAN.

---

## 10. Entregable

- Este documento: `docs/AUDIT/02_SECURITY_TESTS.md`.
- Matriz OWASP: `docs/AUDIT/02_SECURITY_MATRIX.md`.
