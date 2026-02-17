# FixHub — Powered by AutonomousFlow

Marketplace de servicios del hogar (plomería, electricidad, handyman, A/C, pintura).

## Arquitectura

```
Clean Architecture
├── FixHub.Domain          — Entidades, Enums, Excepciones (sin dependencias)
├── FixHub.Application     — Casos de uso (CQRS/MediatR), interfaces, validators
├── FixHub.Infrastructure  — EF Core + PostgreSQL, JWT, BCrypt
├── FixHub.API             — ASP.NET Core WebAPI (API-first, v1)
└── FixHub.Web             — Razor Pages SSR (consume API vía HttpClient)
```

**Regla de dependencias:**
```
Domain ← Application ← Infrastructure ← API
                                       ← Web (solo HttpClient, NO Application)
```

## Requisitos

- .NET 8 SDK
- PostgreSQL 15+ corriendo en localhost:5432

## Correr en desarrollo

### 1. API (puerto 7100 HTTPS / 5100 HTTP)
```bash
cd src/FixHub.API
dotnet run --launch-profile https
```
Swagger UI: https://localhost:7100/swagger

### 2. Web (puerto 7200 HTTPS / 5200 HTTP)
```bash
cd src/FixHub.Web
dotnet run --launch-profile https
```
Health check: https://localhost:7200/Health

### 3. Ambos en paralelo (desde la raíz)
```bash
# Terminal 1
dotnet run --project src/FixHub.API --launch-profile https

# Terminal 2
dotnet run --project src/FixHub.Web --launch-profile https
```

## Base de datos

### Prerrequisito: PostgreSQL local
```bash
# Con Docker (opción rápida)
docker run -d --name fixhub-pg \
  -e POSTGRES_DB=fixhub_dev \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  postgres:16-alpine
```

### Aplicar migración inicial (Fase 2)
```bash
cd src/FixHub.API
dotnet ef database update --project ../FixHub.Infrastructure
```

## Variables de configuración

| Variable | Default | Descripción |
|----------|---------|-------------|
| `ConnectionStrings:DefaultConnection` | `Host=localhost;...` | Cadena PostgreSQL |
| `JwtSettings:SecretKey` | (dev key) | **CAMBIAR en producción** |
| `JwtSettings:Issuer` | `FixHub.API` | Emisor JWT |
| `JwtSettings:Audience` | `FixHub.Clients` | Audiencia JWT |
| `WebOrigin` | `https://localhost:7200` | Origin CORS permitido para Web |
| `ApiSettings:BaseUrl` | `https://localhost:7100` | URL de la API (Web → API) |

## Fases de desarrollo

- [x] **Fase 1** — Estructura de solución + base
- [ ] **Fase 2** — Modelo de datos + migraciones EF Core
- [ ] **Fase 3** — API v1 (Auth + Jobs + Proposals + Reviews + Scoring)
- [ ] **Fase 4** — Web Razor consumiendo la API
- [ ] **Fase 5** — Hardening (rate limiting, logs, tests)

## Stack técnico

| Layer | Tecnología |
|-------|-----------|
| API | ASP.NET Core 8 WebAPI |
| Web | ASP.NET Core 8 Razor Pages |
| ORM | Entity Framework Core 8 |
| DB | PostgreSQL 15+ |
| Auth | JWT Bearer |
| CQRS | MediatR 12 |
| Validación | FluentValidation 11 |
| Hashing | BCrypt.Net work factor 12 |
| Docs | Swagger / OpenAPI 3 |

## ADR (Architecture Decision Records)

### ADR-001: IApplicationDbContext expone DbSet<T>
**Decisión**: Application referencia `Microsoft.EntityFrameworkCore` para usar `DbSet<T>` en la interfaz `IApplicationDbContext`.
**Justificación**: Evita un repositorio genérico redundante que no aporta valor en este contexto. Application solo usa el contrato `DbSet<T>` + LINQ; la implementación real (`AppDbContext` con Npgsql) vive exclusivamente en Infrastructure.
**Trade-off**: Application tiene dependencia de EF Core, pero sin acoplamiento a ningún proveedor de base de datos.

### ADR-002: Web no referencia Application ni Infrastructure
**Decisión**: FixHub.Web solo tiene referencia a `HttpClient` hacia la API. No importa nada de Application ni Infrastructure.
**Justificación**: Garantiza que toda lógica de negocio pase por la API, habilitando app móvil Flutter sin duplicación.

### ADR-003: Result<T> en lugar de excepciones para control de flujo
**Decisión**: Los casos de uso de Application retornan `Result<T>` en lugar de lanzar excepciones para flujo de negocio (not found, conflicts, etc.).
**Justificación**: Excepciones son costosas y dificultan el testing. Las excepciones reales (bugs, DB down) sí se propagan normalmente.
