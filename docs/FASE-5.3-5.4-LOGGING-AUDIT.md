# FASE 5.3 y 5.4 — Logging + Correlation ID y Audit Logs

## Archivos modificados / creados

| Capa | Archivo | Descripción |
|------|---------|-------------|
| **API** | `Middleware/CorrelationIdMiddleware.cs` | Nuevo: lee/genera X-Correlation-Id, propaga en respuesta y scope |
| **API** | `Middleware/RequestLoggingMiddleware.cs` | Nuevo: log por request (Path, StatusCode, elapsedMs) |
| **API** | `Services/CorrelationIdAccessor.cs` | Nuevo: implementa ICorrelationIdAccessor desde HttpContext |
| **API** | `Program.cs` | Registro CorrelationId + RequestLogging, HttpContextAccessor, ICorrelationIdAccessor |
| **Domain** | `Entities/AuditLog.cs` | Nuevo: entidad audit_logs (sin PII) |
| **Application** | `Common/Interfaces/ICorrelationIdAccessor.cs` | Nuevo |
| **Application** | `Common/Interfaces/IAuditService.cs` | Nuevo |
| **Application** | `Common/Behaviors/AuditBehavior.cs` | Nuevo: pipeline MediatR para registrar eventos de auditoría |
| **Application** | `Common/Interfaces/IApplicationDbContext.cs` | + DbSet\<AuditLog\> |
| **Application** | `DependencyInjection.cs` | + AuditBehavior |
| **Infrastructure** | `Persistence/AppDbContext.cs` | + DbSet\<AuditLog\> |
| **Infrastructure** | `Persistence/Configurations/AuditLogConfiguration.cs` | Nuevo: Fluent API audit_logs |
| **Infrastructure** | `Persistence/Migrations/20260218000000_AddAuditLogs.cs` | Nuevo: migración |
| **Infrastructure** | `Persistence/Migrations/AppDbContextModelSnapshot.cs` | + entidad AuditLog |
| **Infrastructure** | `Services/AuditService.cs` | Nuevo: implementación IAuditService |
| **Infrastructure** | `DependencyInjection.cs` | + IAuditService → AuditService |

---

## 1. CorrelationId middleware (FASE 5.3)

- **Cabecera**: `X-Correlation-Id`. Si el request la trae, se reutiliza; si no, se genera un GUID.
- **Respuesta**: la misma cabecera se devuelve en la respuesta.
- **Logging scope**: se añade `CorrelationId` al scope para que todos los logs del request lo incluyan (si el logger soporta propiedades de scope).

Orden en pipeline: `CorrelationIdMiddleware` → `RequestLoggingMiddleware` → resto.

---

## 2. Logging estructurado (FASE 5.3)

- Un único middleware de request: **RequestLoggingMiddleware**.
- Tras `next()`, se loguea: `Path`, `StatusCode`, `ElapsedMs`.
- No se loguean cuerpos, tokens ni contraseñas.
- Ejemplo de log (texto):

```
info: FixHub.API.Middleware.RequestLoggingMiddleware[0]
      Request /api/v1/health completed with 200 in 12ms
```

Con CorrelationId en scope (si el sink lo muestra):

```
info: FixHub.API.Middleware.RequestLoggingMiddleware[0]
      [CorrelationId: abc123] Request /api/v1/jobs completed with 201 in 45ms
```

---

## 3. Tabla audit_logs (FASE 5.4)

| Columna | Tipo | Descripción |
|---------|------|-------------|
| id | uuid | PK |
| created_at_utc | timestamptz | default NOW() |
| actor_user_id | uuid | nullable |
| action | text (varchar 200) | no null |
| entity_type | text (varchar 100) | nullable |
| entity_id | uuid | nullable |
| metadata_json | jsonb | nullable (sin PII) |
| correlation_id | text (varchar 64) | nullable |

Índices: `created_at_utc`, `action`, `correlation_id`.

---

## 4. Eventos auditados (sin PII)

| Action | Cuándo | actor_user_id | entity_type | entity_id | metadata_json (ejemplo) |
|--------|--------|----------------|-------------|-----------|-------------------------|
| AUTH_REGISTER | Register OK | nuevo UserId | User | UserId | `{"role":"Customer"}` |
| AUTH_LOGIN_FAIL | Login fallido | null | null | null | `{"reason":"INVALID_CREDENTIALS"}` (nunca email/password) |
| JOB_CREATE | Job creado | CustomerId | Job | JobId | null |
| PROPOSAL_SUBMIT | Propuesta enviada | TechnicianId | Proposal | ProposalId | `{"jobId":"..."}` |
| PROPOSAL_ACCEPT | Propuesta aceptada | CustomerId | Proposal | ProposalId | `{"jobId":"..."}` |
| JOB_COMPLETE | Job completado | CustomerId | Job | JobId | null |
| REVIEW_CREATE | Review creada | CustomerId | Review | ReviewId | `{"jobId":"..."}` |

---

## 5. Validación: query SQL

Tras ejecutar el happy path (registro, login, crear job, enviar propuesta, aceptar, completar job, crear review):

```sql
-- Contar por acción
SELECT action, COUNT(*) AS cnt
FROM audit_logs
GROUP BY action
ORDER BY action;

-- Últimos 20 registros
SELECT id, created_at_utc, action, entity_type, entity_id, correlation_id, metadata_json
FROM audit_logs
ORDER BY created_at_utc DESC
LIMIT 20;

-- Comprobar que AUTH_LOGIN_FAIL no tiene PII
SELECT action, metadata_json
FROM audit_logs
WHERE action = 'AUTH_LOGIN_FAIL';
-- metadata_json debe ser tipo {"reason":"INVALID_CREDENTIALS"}, sin email ni password
```

---

## 6. Aplicar migración

Con la API detenida (para evitar bloqueo de DLLs):

```bash
cd c:\Proyectos\FixHub
dotnet ef database update --project src\FixHub.Infrastructure\FixHub.Infrastructure.csproj --startup-project src\FixHub.API\FixHub.API.csproj
```

Si el Designer de la migración falta o da error, regenerar:

```bash
dotnet ef migrations add AddAuditLogs --project src\FixHub.Infrastructure\FixHub.Infrastructure.csproj --startup-project src\FixHub.API\FixHub.API.csproj
```

(En ese caso puede que haya que eliminar antes `20260218000000_AddAuditLogs.cs` si ya existía.)

---

## 7. Checklist 5.3 / 5.4

### FASE 5.3 — Logging + Correlation ID

- [x] CorrelationId middleware: lee X-Correlation-Id o genera uno.
- [x] CorrelationId en cabecera de respuesta.
- [x] CorrelationId en logging scope.
- [x] Logging por request: Path, StatusCode, elapsedMs (RequestLoggingMiddleware).
- [x] No loguear tokens, passwords ni emails.
- [x] Sin Serilog adicional (solo enriquecimiento con CorrelationId/Path/StatusCode/elapsedMs).

### FASE 5.4 — Audit logs en DB

- [x] Entidad AuditLog y tabla audit_logs (id, created_at_utc, actor_user_id, action, entity_type, entity_id, metadata_json, correlation_id).
- [x] Configuración Fluent en AuditLogConfiguration.
- [x] Migración AddAuditLogs.
- [x] IAuditService en Application, implementación en Infrastructure.
- [x] AuditBehavior (MediatR) registrado; eventos: AUTH_REGISTER, AUTH_LOGIN_FAIL, JOB_CREATE, PROPOSAL_SUBMIT, PROPOSAL_ACCEPT, JOB_COMPLETE, REVIEW_CREATE.
- [x] AUTH_LOGIN_FAIL sin email/password; solo reason genérico en metadata.
- [x] metadata_json sin PII (jobId, proposalId, role, reason, etc.).
- [x] Respuestas de API sin cambios.

### Reglas generales

- [x] Build 0 warnings (compilación correcta; fallos por bloqueo de .exe son por tener la API en ejecución).
- [x] Auditoría sin PII sensible.
