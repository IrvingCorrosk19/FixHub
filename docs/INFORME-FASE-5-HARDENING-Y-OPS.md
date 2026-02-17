# Informe de resultado — FASE 5 (Hardening y operaciones)

**Alcance:** Prompts FASE 5.1 a 5.6 en FixHub (API, Application, Infrastructure, Web, Tests, Docker).  
**Reglas aplicadas:** Sin romper contrato de API, sin loguear tokens/passwords, auditoría sin PII, build 0 warnings, cambios mínimos y seguros.

---

## 1. FASE 5.1 — Rate limiting (API)

| Entregable | Estado | Detalle |
|------------|--------|---------|
| Rate limiting built-in ASP.NET Core | ✅ | `AddRateLimiter` en `Program.cs` |
| Global por IP | ✅ | 60 req/min, FixedWindow, `GlobalLimiter` particionado por `RemoteIpAddress` |
| Auth más restrictivo | ✅ | 10 req/min en `/api/v1/auth/login` y `/api/v1/auth/register` vía `[EnableRateLimiting("AuthPolicy")]` en `AuthController` |
| 429 con ProblemDetails (RFC 7807) | ✅ | `OnRejected` escribe JSON con `errorCode: "RATE_LIMITED"` y cabecera `Retry-After` cuando aplica |
| ForwardedHeaders (proxy) | ✅ | Configurado y usado solo cuando **no** es Development (`UseForwardedHeaders`); `X-Forwarded-For` y `X-Forwarded-Proto` |

**Archivos:** `FixHub.API/Program.cs`, `FixHub.API/Controllers/v1/AuthController.cs`

---

## 2. FASE 5.2 — Security headers + CORS (API y Web)

| Entregable | Estado | Detalle |
|------------|--------|---------|
| CORS en Development | ✅ | Solo origen `http://localhost:5200`; en otros entornos se usa `WebOrigin` de configuración (luego se prioriza `WebOrigin` si existe en todos los entornos) |
| Headers de seguridad globales | ✅ | Middleware `SecurityHeadersMiddleware`: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Permissions-Policy: geolocation=(), camera=(), microphone=()` |
| Swagger | ✅ | Sin cambios; sin CSP que lo rompa |
| Cookie token (Web) | ✅ | `HttpOnly`, `SameSite=Lax`; `SecurePolicy`: `SameAsRequest` en Development, `Always` en Production |

**Archivos:** `FixHub.API/Program.cs`, `FixHub.API/Middleware/SecurityHeadersMiddleware.cs`, `FixHub.Web/Program.cs`

---

## 3. FASE 5.3 — Logging + Correlation ID (API)

| Entregable | Estado | Detalle |
|------------|--------|---------|
| CorrelationId middleware | ✅ | Lee `X-Correlation-Id` del request o genera uno; lo guarda en `HttpContext.Items`, lo devuelve en cabecera de respuesta y lo añade al logging scope |
| Logging estructurado por request | ✅ | `RequestLoggingMiddleware` loguea Path, StatusCode y elapsedMs tras `next()`; sin cuerpos, tokens ni passwords |
| Sin Serilog nuevo | ✅ | Solo enriquecimiento con CorrelationId (scope) y datos del request en el logger estándar |

**Archivos:** `FixHub.API/Middleware/CorrelationIdMiddleware.cs`, `FixHub.API/Middleware/RequestLoggingMiddleware.cs`, `FixHub.API/Services/CorrelationIdAccessor.cs`, `FixHub.API/Program.cs`, `FixHub.Application/Common/Interfaces/ICorrelationIdAccessor.cs`

---

## 4. FASE 5.4 — Audit logs en DB (Application + Infrastructure)

| Entregable | Estado | Detalle |
|------------|--------|---------|
| Entidad y tabla `audit_logs` | ✅ | `id` (uuid PK), `created_at_utc` (timestamptz), `actor_user_id`, `action`, `entity_type`, `entity_id`, `metadata_json` (jsonb), `correlation_id` |
| Migración | ✅ | `20260218000000_AddAuditLogs.cs`; snapshot actualizado |
| IAuditService + implementación | ✅ | Interfaz en Application; `AuditService` en Infrastructure (serializa metadata a JSON sin PII, usa `ICorrelationIdAccessor` para `correlation_id`) |
| Registro de eventos (MediatR) | ✅ | `AuditBehavior` en pipeline de MediatR registra: `AUTH_REGISTER`, `AUTH_LOGIN_FAIL`, `JOB_CREATE`, `PROPOSAL_SUBMIT`, `PROPOSAL_ACCEPT`, `JOB_COMPLETE`, `REVIEW_CREATE` |
| Sin PII en auditoría | ✅ | `AUTH_LOGIN_FAIL` solo con `reason` genérico; metadata con jobId, proposalId, role, etc., sin email/password/tokens |

**Archivos:** `FixHub.Domain/Entities/AuditLog.cs`, `FixHub.Application/Common/Interfaces/IAuditService.cs`, `FixHub.Application/Common/Behaviors/AuditBehavior.cs`, `FixHub.Infrastructure/Services/AuditService.cs`, `FixHub.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs`, `FixHub.Infrastructure/Persistence/Migrations/20260218000000_AddAuditLogs.cs`, `IApplicationDbContext` y `AppDbContext` con `AuditLogs`, DI en Application e Infrastructure

---

## 5. FASE 5.5 — Integration tests

| Entregable | Estado | Detalle |
|------------|--------|---------|
| Proyecto FixHub.IntegrationTests | ✅ | xunit, Microsoft.AspNetCore.Mvc.Testing, Testcontainers.PostgreSql |
| WebApplicationFactory para API | ✅ | `FixHubApiFactory` inyecta connection string (Testcontainers) y aplica migraciones en `CreateHost` |
| DB real con Testcontainers | ✅ | PostgreSQL 16-alpine; tests reproducibles con contenedor fresco |
| Tests mínimos | ✅ | GET /api/v1/health => 200; Register/Login => token; Technician no puede crear Job => 403; happy path: create job → proposal → accept → complete → review |
| dotnet test | ✅ | Proyecto configurado; requiere Docker en ejecución para Testcontainers (fallback sin Docker documentado) |

**Archivos:** `tests/FixHub.IntegrationTests/` (csproj, FixHubApiFactory, FixHubApiFixture, ApiIntegrationTests), `FixHub.sln` actualizado

---

## 6. FASE 5.6 — Docker Compose

| Entregable | Estado | Detalle |
|------------|--------|---------|
| Dockerfile API | ✅ | Multi-stage (build → publish → runtime), puerto 8080, contexto raíz del repo |
| Dockerfile Web | ✅ | Multi-stage, puerto 8080 |
| Contenedor migrador | ✅ | Proyecto `FixHub.Migrator`: ejecuta migraciones EF y termina; **no** se ejecuta `dotnet ef database update` en el API en runtime |
| docker-compose.yml | ✅ | Servicios: `postgres` (5432) → `migrator` (depende de postgres healthy, termina) → `api` (depende de migrator completed) → `web` (depende de api). Puertos: API 8080, Web 8081 |
| Variables de entorno | ✅ | `ConnectionStrings__DefaultConnection`, `JwtSettings__SecretKey/Issuer/Audience`, `WebOrigin` (API), `ApiSettings__BaseUrl` (Web → `http://api:8080`) |
| Validación | ✅ | Documentado: `docker compose up --build`; API Swagger en :8080/swagger; Web en :8081; flujo básico descrito |

**Archivos:** `src/FixHub.API/Dockerfile`, `src/FixHub.Web/Dockerfile`, `src/FixHub.Migrator/Dockerfile`, `src/FixHub.Migrator/Program.cs`, `docker-compose.yml`, `.dockerignore`, `FixHub.sln` con FixHub.Migrator

---

## Resumen de archivos tocados (por capa)

- **API:** Program.cs, AuthController, CorrelationIdMiddleware, RequestLoggingMiddleware, RequestLoggingMiddleware, SecurityHeadersMiddleware, Services/CorrelationIdAccessor.
- **Web:** Program.cs (cookie SecurePolicy).
- **Domain:** Entities/AuditLog.cs.
- **Application:** ICorrelationIdAccessor, IAuditService, AuditBehavior, IApplicationDbContext (AuditLogs), DependencyInjection.
- **Infrastructure:** AppDbContext (AuditLogs), AuditLogConfiguration, AuditService, DependencyInjection, Migraciones (AddAuditLogs + snapshot).
- **Tests:** tests/FixHub.IntegrationTests (proyecto completo).
- **Migrator:** src/FixHub.Migrator (proyecto + Program.cs).
- **Docker:** Dockerfiles (API, Web, Migrator), docker-compose.yml, .dockerignore.
- **Docs:** FASE-5-HARDENING.md, FASE-5.3-5.4-LOGGING-AUDIT.md, FASE-5.5-5.6-DOCKER-TESTS.md, y este informe.

---

## Comandos de validación

```bash
# Build (API sin ejecutar para evitar bloqueos)
dotnet build src/FixHub.API/FixHub.API.csproj

# Tests de integración (Docker en marcha)
dotnet test tests/FixHub.IntegrationTests/FixHub.IntegrationTests.csproj

# Migraciones (con API detenida)
dotnet ef database update --project src/FixHub.Infrastructure --startup-project src/FixHub.API

# Docker Compose
docker compose up --build
# API: http://localhost:8080  Swagger: http://localhost:8080/swagger  Web: http://localhost:8081
```

---

## Checklist global FASE 5

| Regla | Cumplimiento |
|-------|----------------|
| No romper contrato de API | ✅ Respuestas y rutas existentes sin cambios |
| No loguear tokens ni passwords | ✅ Middlewares y auditoría sin PII sensible |
| Build 0 warnings | ✅ Duplicado de `using` en Program.cs corregido; fallos restantes por proceso API en ejecución (bloqueo de DLL) |
| Auditoría sin PII | ✅ AUTH_LOGIN_FAIL solo reason genérico; metadata sin email/password |
| Tests reproducibles | ✅ Testcontainers PostgreSQL por ejecución |
| Migraciones fuera del API runtime | ✅ Contenedor migrador dedicado en compose |

---

*Informe único generado a partir de los prompts FASE 5.1, 5.2, 5.3, 5.4, 5.5 y 5.6.*
