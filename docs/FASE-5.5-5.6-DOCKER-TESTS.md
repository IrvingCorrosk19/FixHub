# FASE 5.5 y 5.6 — Integration Tests y Docker Compose

## Archivos creados / modificados

| Tipo | Archivo | Descripción |
|------|---------|-------------|
| **Tests** | `tests/FixHub.IntegrationTests/FixHub.IntegrationTests.csproj` | Proyecto xunit + WebApplicationFactory + Testcontainers.PostgreSql |
| **Tests** | `tests/FixHub.IntegrationTests/FixHubApiFactory.cs` | Factory que inyecta connection string y aplica migraciones |
| **Tests** | `tests/FixHub.IntegrationTests/FixHubApiFixture.cs` | Fixture que levanta PostgreSQL con Testcontainers |
| **Tests** | `tests/FixHub.IntegrationTests/ApiIntegrationTests.cs` | Health 200, Register/Login token, Technician 403, Happy path |
| **Migrator** | `src/FixHub.Migrator/FixHub.Migrator.csproj` | Ejecutable que aplica migraciones EF y termina |
| **Migrator** | `src/FixHub.Migrator/Program.cs` | Lee `ConnectionStrings__DefaultConnection` de env y ejecuta `MigrateAsync` |
| **Docker** | `src/FixHub.API/Dockerfile` | Multi-stage: build → publish → runtime (puerto 8080) |
| **Docker** | `src/FixHub.Web/Dockerfile` | Multi-stage para la Web (puerto 8080) |
| **Docker** | `src/FixHub.Migrator/Dockerfile` | Build y runtime para el migrador |
| **Compose** | `docker-compose.yml` | postgres, migrator, api, web con dependencias y env |
| **API** | `Program.cs` | CORS: usar `WebOrigin` de config si está definido (para Docker 8081) |
| **Solution** | `FixHub.sln` | Inclusión de FixHub.Migrator y FixHub.IntegrationTests |

---

## Comandos exactos

### Tests (reproducibles con Testcontainers)

```bash
cd c:\Proyectos\FixHub
dotnet test tests/FixHub.IntegrationTests/FixHub.IntegrationTests.csproj
```

En PowerShell:

```powershell
cd c:\Proyectos\FixHub
dotnet test .\tests\FixHub.IntegrationTests\FixHub.IntegrationTests.csproj
```

**Requisitos:** Docker en ejecución (Testcontainers levanta un contenedor PostgreSQL por clase de tests).

**Fallback sin Testcontainers:** Si no puedes usar Docker, configura una base PostgreSQL local y un `appsettings.Testing.json` en la API con esa connection string; luego usa una factory que no arranque el contenedor y apunte a esa DB. No está implementado; se recomienda usar Testcontainers para tests reproducibles.

---

### Docker Compose

```bash
cd c:\Proyectos\FixHub
docker compose up --build
```

- **Postgres:** `localhost:5432` (usuario `postgres`, contraseña `postgres`, DB `FixHub`).
- **API:** `http://localhost:8080` (Swagger en Development: `http://localhost:8080/swagger`).
- **Web:** `http://localhost:8081`.

**Validación:**

1. `curl http://localhost:8080/api/v1/health` → 200.
2. Abrir `http://localhost:8080/swagger` → UI de Swagger responde.
3. Abrir `http://localhost:8081` → Web responde.
4. Flujo básico: registro → login → crear job (como Customer) → enviar propuesta (como Technician) → aceptar (Customer) → completar job → crear review.

---

## Variables de entorno (Docker Compose)

| Servicio | Variable | Ejemplo / descripción |
|----------|----------|------------------------|
| **migrator** | `ConnectionStrings__DefaultConnection` | `Host=postgres;Port=5432;Database=FixHub;Username=postgres;Password=postgres` |
| **api** | `ConnectionStrings__DefaultConnection` | Igual que migrator |
| **api** | `JwtSettings__SecretKey` | Clave mínima 32 caracteres |
| **api** | `JwtSettings__Issuer` | `FixHub.API` |
| **api** | `JwtSettings__Audience` | `FixHub.Clients` |
| **api** | `WebOrigin` | `http://localhost:8081` (CORS para la Web) |
| **web** | `ApiSettings__BaseUrl` | `http://api:8080` (URL interna del servicio API) |

---

## Orden de arranque (compose)

1. **postgres** — arranca y queda con healthcheck.
2. **migrator** — `depends_on: postgres (healthy)`; corre migraciones y termina (`service_completed_successfully`).
3. **api** — `depends_on: migrator (completed)`; no ejecuta migraciones en runtime.
4. **web** — `depends_on: api`; usa `ApiSettings__BaseUrl` para llamar a la API.

---

## Checklist 5.5 / 5.6

### FASE 5.5 — Integration Tests

- [x] Proyecto `FixHub.IntegrationTests` con xunit y WebApplicationFactory.
- [x] DB real con Testcontainers PostgreSQL (postgres:16-alpine).
- [x] Tests mínimos: GET /api/v1/health => 200.
- [x] Register + Login => token.
- [x] Technician no puede crear Job => 403.
- [x] Happy path: create job → proposal → accept → complete → review.
- [x] Tests reproducibles (contenedor fresco por run).
- [ ] `dotnet test` pasa (ejecutar con Docker en marcha; si la API está en ejecución, cerrarla antes para evitar bloqueos de build).

### FASE 5.6 — Docker Compose

- [x] Dockerfile API (multi-stage), puerto 8080.
- [x] Dockerfile Web (multi-stage), puerto 8080.
- [x] Dockerfile Migrator (ejecuta migraciones y termina).
- [x] docker-compose: postgres, migrator, api, web.
- [x] Migraciones solo en contenedor migrator (no en API en runtime).
- [x] api depende de migrator (completed); web depende de api.
- [x] Env: ConnectionStrings__DefaultConnection, JwtSettings (SecretKey, Issuer, Audience), ApiSettings__BaseUrl en Web.
- [ ] Validación manual: `docker compose up --build`, Swagger responde, Web responde, flujo básico funciona.
