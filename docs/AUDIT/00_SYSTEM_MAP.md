# FixHub — Inventario y Mapa del Sistema (Fase 0)

**Branch:** `audit/fixhub-100`  
**Fecha:** 2025-02-23  
**Alcance:** Inventario obligatorio y mapa de sistema para auditoría production-grade.

---

## 1. Estructura de carpetas

```
FixHub/
├── FixHub.sln
├── docker-compose.yml          # Orquestación: postgres, migrator, api, web
├── run-dev.ps1                 # Script desarrollo local
├── docs/
│   ├── AUDIT/                  # Salida de esta auditoría
│   │   └── 00_SYSTEM_MAP.md    # Este documento
│   └── AUDITORIA_SISTEMA_FIXHUB.md
├── src/
│   ├── FixHub.Domain/          # Entidades, enums, sin dependencias
│   ├── FixHub.Application/     # CQRS (MediatR), interfaces, DTOs, validadores
│   ├── FixHub.Infrastructure/  # EF Core, PostgreSQL, JWT, email, outbox, hosted services
│   ├── FixHub.API/             # ASP.NET Core 8, controladores v1, middleware
│   ├── FixHub.Web/             # Razor Pages, consume API por HTTP
│   ├── FixHub.Migrator/        # Consola: ejecuta migraciones EF Core
│   └── Com/                    # Scripts operativos y documentación
│       ├── deploy-fixhub.ps1, deploy-docker.ps1, sync-*.ps1, ...
│       ├── fixhub/, n8n/, Documentacion/
│       └── *.ps1 (múltiples scripts de deploy/verificación)
└── tests/
    └── FixHub.IntegrationTests/  # WebApplicationFactory + Testcontainers (PostgreSQL)
```

---

## 2. Proyectos .NET

| Proyecto | Tipo | Referencias | Propósito |
|----------|------|-------------|-----------|
| **FixHub.Domain** | Class Library | ninguna | Entidades, enums, excepciones (núcleo) |
| **FixHub.Application** | Class Library | Domain | CQRS (MediatR), IApplicationDbContext, DTOs, FluentValidation |
| **FixHub.Infrastructure** | Class Library | Application, Domain | AppDbContext, PostgreSQL, JWT, Email (SendGrid), Outbox, Auditoría |
| **FixHub.API** | Web API | Application, Infrastructure | Controladores v1, JWT Bearer, rate limiting, CORS, Swagger |
| **FixHub.Web** | Web (Razor) | ninguna | UI; consume API vía HttpClient + cookie auth |
| **FixHub.Migrator** | Console | Infrastructure | Ejecuta migraciones; ConnectionString por env |
| **FixHub.IntegrationTests** | Test | API | Tests E2E con Testcontainers |

---

## 3. Archivos de despliegue

| Archivo | Uso |
|---------|-----|
| `docker-compose.yml` (raíz) | fixhub_postgres (5432 interno), fixhub_migrator, fixhub_api (8080 interno), fixhub_web (8084:8080); red fixhub_net |
| `src/FixHub.API/Dockerfile` | Multi-stage: SDK 8.0 build → publish → aspnet:8.0; EXPOSE 8080 |
| `src/FixHub.Web/Dockerfile` | Idem para Web; puerto 8080 interno |
| `src/FixHub.Migrator/Dockerfile` | Build + runtime 8.0 (no aspnet); ejecuta migraciones |
| `src/Com/deploy-fixhub.ps1` | Deploy a VPS (plink/SSH, docker compose up) |
| `src/Com/deploy-docker.ps1` | Alternativa deploy Docker |
| `src/Com/sync-fixhub-db.ps1`, `sync-db-local-to-server.ps1` | Sincronización de BD (uso controlado) |

Variables de entorno esperadas (compose): `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `JWT_SECRET_KEY`, `WEB_ORIGIN` (desde `.env`).

---

## 4. Archivos de configuración

| Archivo | Contenido relevante |
|---------|---------------------|
| `src/FixHub.API/appsettings.json` | ConnectionStrings (vacío), JwtSettings (SecretKey vacío), WebOrigin, SendGrid (ApiKey vacío) |
| `src/FixHub.API/appsettings.Development.json` | Valores de desarrollo (ConnectionString, JwtSettings.SecretKey) — **no debe contener secretos en repo** |
| `src/FixHub.API/Properties/launchSettings.json` | API: http 5100, https 7100; launchUrl swagger |
| `src/FixHub.Web/appsettings.json` | ApiSettings.BaseUrl (ej. http://localhost:5100) |
| `src/FixHub.Web/Properties/launchSettings.json` | Web: http 5200, https 7200 |
| `.env` (no versionado) | POSTGRES_*, JWT_SECRET_KEY, WEB_ORIGIN; usado por docker-compose |

---

## 5. Mapa del sistema (1 página)

### 5.1 Componentes y puertos

| Componente | Puerto (host) | Puerto (contenedor) | Descripción |
|------------|--------------|--------------------|-------------|
| **FixHub.Web** | 8084 (compose) / 5200 (dev) | 8080 | Cliente Razor; cookie `fixhub_token`; llama a API |
| **FixHub.API** | 5100 (dev) / 8080 (compose interno) | 8080 | REST api/v1; JWT Bearer; Swagger en /swagger |
| **PostgreSQL** | — | 5432 (solo red fixhub_net) | BD principal |
| **FixHub.Migrator** | — | — | Un solo run al levantar stack; aplica migraciones |

### 5.2 Flujos de datos

```
[Usuario] → [Web :8084] → HTTP + JWT (cookie) → [API :8080] → [PostgreSQL]
                                                      ↓
                                              [IApplicationDbContext]
                                                      ↓
                                    [MediatR] → Application (Handlers)
                                                      ↓
                                    [Infrastructure] Outbox → NotificationOutbox
                                                      ↓
                                    [OutboxEmailSenderHostedService] → SendGrid (Email)
                                    [JobSlaMonitor] → Alertas SLA
```

- **Auth:** POST `/api/v1/auth/register`, POST `/api/v1/auth/login` → JWT (rate limit AuthPolicy 10 req/min).
- **Jobs/Proposals/Notifications:** API v1; autorización por rol y ownership en handlers.

### 5.3 Roles y rutas principales

| Rol | Rutas principales (API) | Notas |
|-----|--------------------------|--------|
| **Customer** | POST /api/v1/jobs, GET /api/v1/jobs/mine, GET /api/v1/jobs/{id}, GET /api/v1/jobs/{id}/proposals, POST complete/cancel/issues, POST /api/v1/reviews | Solo sus jobs; complete/cancel solo propio |
| **Technician** | GET /api/v1/jobs (lista abiertos + donde tiene propuesta), POST /api/v1/jobs/{id}/proposals, POST /api/v1/jobs/{id}/start, GET /api/v1/technicians/me/assignments, GET /api/v1/technicians/{id}/profile | Start solo si asignado |
| **Admin** | Todo lo anterior + GET/PATCH /api/v1/admin/* (applicants, issues, dashboard, jobs/start, jobs/status, alerts/resolve, issues/resolve), POST /api/v1/proposals/{id}/accept, POST /api/v1/ai-scoring/jobs/{id}/rank-technicians | Solo Admin puede aceptar propuestas |
| **Todos (autenticados)** | GET /api/v1/notifications, GET unread-count, POST {id}/read | Notificaciones filtradas por UserId |
| **Público** | GET /api/v1/health | Sin auth |

### 5.4 Políticas de autorización (API)

- **CustomerOnly** — Customer.
- **TechnicianOnly** — Technician.
- **AdminOnly** — Admin (AdminController, AiScoringController).
- **CustomerOrAdmin** — No usado en controladores actuales; algunos flujos combinan Customer + IsAdmin en handler.
- **Rate limiting:** Global 60 req/min; Auth 10 req/min (AuthPolicy).

### 5.5 Resumen de endpoints por controlador

| Prefijo | Controlador | Auth | Endpoints clave |
|--------|-------------|------|------------------|
| api/v1/health | HealthController | No | GET / |
| api/v1/auth | AuthController | No (rate limit) | POST register, POST login |
| api/v1/jobs | JobsController | [Authorize] + políticas | POST, GET, GET mine, GET {id}, GET {id}/proposals, POST proposals/complete/start/cancel/issues |
| api/v1/proposals | ProposalsController | [Authorize] | POST {id}/accept |
| api/v1/technicians | TechniciansController | [Authorize] | GET {id}/profile, GET me/assignments (TechnicianOnly) |
| api/v1/notifications | NotificationsController | [Authorize] | GET, GET unread-count, POST {id}/read |
| api/v1/reviews | ReviewsController | [Authorize] + CustomerOnly | POST |
| api/v1/admin | AdminController | AdminOnly | applicants, issues, dashboard, jobs/start, jobs/status, alerts/resolve, issues/resolve |
| api/v1/ai-scoring | AiScoringController | AdminOnly | POST jobs/{jobId}/rank-technicians |

---

## 6. Entregable Fase 0

- **Documento:** `docs/AUDIT/00_SYSTEM_MAP.md` (este archivo).
- **Branch:** `audit/fixhub-100` creado; cambios solo en docs/AUDIT.

Próxima fase: **Fase 1 — Pruebas funcionales E2E** (suite repetible en local con docker-compose; datos de prueba con prefijo `AUDIT_<timestamp>`).
