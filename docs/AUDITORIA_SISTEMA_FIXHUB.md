# Auditoría exhaustiva del sistema FixHub

**Clasificación:** Confidencial — Uso interno  
**Alcance:** Arquitectura, Seguridad, Base de datos, Performance, DevOps, Riesgos críticos  
**Metodología:** Revisión estática de código y configuración (sin ejecución ni modificación)  
**Fecha de corte:** 23 de febrero de 2025  

---

## 1. Resumen ejecutivo

Se realizó una auditoría técnica de la solución FixHub (marketplace de servicios del hogar) desde el punto de vista de **Clean Architecture**, **seguridad (OWASP, ISO 27001/27002)**, **base de datos (PostgreSQL/EF Core)**, **rendimiento** y **DevSecOps**. El sistema está construido con ASP.NET Core 8, EF Core, PostgreSQL, CQRS (MediatR), JWT y Docker.

**Conclusión principal:** El sistema **no está listo para producción** en su estado actual. Existen hallazgos **críticos** que deben resolverse antes de un despliegue en entorno productivo: **credenciales en repositorio**, **escalación de privilegios en registro (Role=Admin)** y **credenciales hardcodeadas en script de despliegue**. La arquitectura de capas es correcta y la mayoría de los controles de autorización por recurso (IDOR) están bien aplicados en los handlers; las deficiencias se concentran en configuración, gestión de secretos y un fallo de diseño en el endpoint de registro.

**Score de madurez del sistema:** **62/100**  
**Nivel de preparación para producción:** **No recomendado** hasta resolver los hallazgos críticos y altos indicados.

---

## 2. Matriz de riesgos

| ID | Hallazgo | Fase | Clasificación | Urgencia |
|----|----------|------|----------------|----------|
| H01 | Credenciales en repositorio (appsettings.Development.json) | Seguridad / DevOps | 🔴 Crítico | Inmediata |
| H02 | Credenciales y secretos en script de despliegue (deploy-fixhub.ps1) | Seguridad / DevOps | 🔴 Crítico | Inmediata |
| H03 | Registro permite Role=Admin (escalación de privilegios) | Seguridad | 🔴 Crítico | Inmediata |
| H04 | GetJobProposalsQuery: Customer sin verificación de propiedad del job + comportamiento incorrecto | Seguridad / Lógica | 🟠 Alto | Alta |
| H05 | Contraseña por defecto del admin en migración y documentación (Admin123!) | Seguridad | 🟠 Alto | Alta |
| H06 | API REST sin antiforgery (riesgo bajo por Bearer; Web con cookies requiere validación) | Seguridad | 🟡 Medio | Media |
| H07 | Falta de pipeline CI/CD como código (GitHub Actions / Azure Pipelines) | DevOps | 🟠 Alto | Alta |
| H08 | Uso de FromSqlRaw en OutboxEmailSenderHostedService (parámetro limit seguro) | Seguridad / BD | 🟢 Mejora | Baja |
| H09 | GetJobQuery incluye todas las Proposals cuando solo se necesita un booleano | Performance | 🟡 Medio | Media |
| H10 | Borrado en cascada User → TechnicianProfile (y otros) | Base de datos | 🟡 Medio | Media |
| H11 | appsettings.Development.json no excluido en .gitignore | DevOps | 🟠 Alto | Alta |
| H12 | Sin tests unitarios; solo tests de integración | Calidad | 🟡 Medio | Media |

---

## 3. Detalle técnico por hallazgo

### H01 — Credenciales en repositorio (appsettings.Development.json)

- **Evidencia:**  
  - Archivo: `src/FixHub.API/appsettings.Development.json`  
  - Contenido: `ConnectionStrings:DefaultConnection` con `Password=Panama2020$`; `JwtSettings:SecretKey` con clave de desarrollo en texto plano.

- **Impacto de negocio:** Exposición de base de datos y capacidad de emisión de JWTs si el repositorio es público o se filtra. Acceso no autorizado a datos y suplantación de identidad.

- **Recomendación técnica:**  
  - Eliminar valores sensibles de `appsettings.Development.json` y usar **User Secrets** (`dotnet user-secrets`) o variables de entorno en desarrollo.  
  - Añadir `appsettings.Development.json` al `.gitignore` **o** mantener el archivo solo con claves vacías y documentar el uso de user secrets/env.  
  - Rotar la contraseña de PostgreSQL y el JWT secret que hayan podido quedar expuestos.

- **Nivel de urgencia:** Inmediata.

---

### H02 — Credenciales y secretos en script de despliegue (deploy-fixhub.ps1)

- **Evidencia:**  
  - Archivo: `src/Com/deploy-fixhub.ps1`  
  - Líneas 4–7: `$password = "DC26Y0U5ER6sWj"`, `$hostname = "root@164.68.99.83"`, `$hostkey` con huella SSH.  
  - Líneas 32–42: bloque `.env` con `POSTGRES_PASSWORD=FixHub2024!Secure`, `JWT_SECRET_KEY=ChangeMeProductionMin32CharsSecretKey!!`.

- **Impacto de negocio:** Compromiso total del VPS (root), base de datos y aplicación si el script está versionado o se comparte. Riesgo operativo y de cumplimiento (ISO 27001).

- **Recomendación técnica:**  
  - No almacenar contraseñas ni claves en scripts. Usar variables de entorno en la máquina que ejecuta el deploy, o un gestor de secretos (Azure Key Vault, HashiCorp Vault, etc.).  
  - Autenticación SSH por clave privada sin contraseña en script, o uso de agente SSH.  
  - Si el script debe crear `.env` en el servidor, que lea valores desde entorno o secretos, nunca desde literales en código.  
  - Considerar añadir `src/Com/deploy-fixhub.ps1` a `.gitignore` si contiene datos sensibles hasta refactorizar, y rotar **todas** las credenciales expuestas.

- **Nivel de urgencia:** Inmediata.

---

### H03 — Registro permite Role=Admin (escalación de privilegios)

- **Evidencia:**  
  - `AuthController`: `RegisterRequest` incluye `UserRole Role` y se reenvía a `RegisterCommand`.  
  - `RegisterCommandValidator`: `RuleFor(x => x.Role).IsInEnum().Must(r => r != 0)` — permite cualquier rol definido en el enum, incluido Admin.  
  - `RegisterCommandHandler`: asigna `request.Role` al usuario sin restricción (líneas 65–66 de `RegisterCommand.cs`).

- **Impacto de negocio:** Cualquier persona puede crear una cuenta con rol Admin y obtener privilegios de administrador (asignar técnicos, ver dashboard, resolver incidencias, etc.). Bloqueante para producción.

- **Recomendación técnica:**  
  - En el handler (o validator), rechazar explícitamente `UserRole.Admin` en registro (por ejemplo `Must(r => r != UserRole.Admin)` con mensaje claro).  
  - Los administradores deben crearse solo por migración/seed o por un proceso administrativo seguro (otro admin o herramienta interna), nunca por el endpoint público de registro.

- **Nivel de urgencia:** Inmediata.

---

### H04 — GetJobProposalsQuery: Customer sin verificación de propiedad + comportamiento incorrecto

- **Evidencia:**  
  - `JobsController`: comentario indica "Customer (own job) / Admin" para `GET jobs/{id}/proposals`.  
  - `GetJobProposalsQuery`: recibe `JobId`, `RequesterId`, `IsAdmin`. Si `!IsAdmin`, devuelve solo propuestas donde `TechnicianId == RequesterId` (líneas 36–40). No se comprueba `job.CustomerId == RequesterId`.  
  - Consecuencia: un Customer que llama a `GET /api/v1/jobs/{id}/proposals` puede usar **cualquier** JobId; no se valida que el job sea suyo. Además, según la documentación del API, el Customer debería ver **todas** las propuestas de su job, pero actualmente recibe una lista vacía (porque filtra por técnico).

- **Impacto de negocio:** (1) Riesgo IDOR si en el futuro se “arregla” devolviendo todas las propuestas sin comprobar propiedad. (2) Funcionalidad incorrecta: el cliente no puede ver las propuestas de sus propios jobs.

- **Recomendación técnica:**  
  - Añadir en el handler: si el solicitante es Customer (por ejemplo pasando un flag o rol), comprobar `job.CustomerId == req.RequesterId` antes de devolver datos; si no es dueño, devolver 403.  
  - Si el negocio exige que el Customer vea las propuestas de su job: para Customer dueño del job, devolver todas las propuestas del job (como hace Admin); para Technician, mantener el filtro por `TechnicianId == RequesterId`.

- **Nivel de urgencia:** Alta.

---

### H05 — Contraseña por defecto del admin en migración y documentación

- **Evidencia:**  
  - `20260220000000_SeedAdminUser.cs`: comentario "Password: Admin123!" y uso de `BCrypt.Net.BCrypt.HashPassword("Admin123!", 12)` en código.  
  - Scripts en `src/Com/`: varios mencionan o usan "Admin123!" (crear-tablas-faltantes.ps1, actualizar-password-admin.ps1, verificar-usuarios.ps1, etc.).

- **Impacto de negocio:** Si la contraseña por defecto no se cambia en producción, acceso administrativo trivial. Cumplimiento (ISO 27001) y políticas de contraseñas.

- **Recomendación técnica:**  
  - La migración puede seguir creando el admin con un hash; la contraseña real debería establecerse por variable de entorno (hash pregenerado) o por un job/post-deploy que la cambie.  
  - Documentar que el admin debe cambiar la contraseña en el primer inicio.  
  - Evitar documentar la contraseña por defecto en repositorio; usar referencias a “contraseña de seed/documentada en runbook seguro”.

- **Nivel de urgencia:** Alta.

---

### H06 — API REST sin antiforgery; Web con cookies

- **Evidencia:**  
  - API: autenticación por Bearer JWT; no se usa `[ValidateAntiForgeryToken]`.  
  - Web (Razor): autenticación por cookie (`fixhub_token`). Solo se encontró `[IgnoreAntiforgeryToken]` en `Error.cshtml.cs`. No se verificó explícitamente antiforgery en formularios de login/acción.

- **Impacto de negocio:** Para API pura con Bearer el riesgo CSRF es bajo. Para la Web, si hay formularios que cambian estado y no están protegidos con antiforgery, existe riesgo CSRF.

- **Recomendación técnica:**  
  - API: mantener sin antiforgery; es el enfoque habitual para APIs con Bearer.  
  - Web: asegurar que las páginas que realizan operaciones sensibles (login, acciones con estado) usen el token antiforgery automático de Razor Pages o `@Html.AntiForgeryToken()` y validación en el servidor. Confirmar en código que no se usa `[IgnoreAntiforgeryToken]` en esas páginas.

- **Nivel de urgencia:** Media.

---

### H07 — Falta de pipeline CI/CD como código

- **Evidencia:**  
  - No existen archivos `.github/workflows/*.yml` ni `azure-pipelines*.yml` en el repositorio.  
  - El despliegue se realiza mediante script manual `src/Com/deploy-fixhub.ps1` (plink/SSH).

- **Impacto de negocio:** Despliegues no reproducibles, sin trazabilidad ni aprobaciones formales. Dificulta GitOps y auditoría de cambios (ISO 27001).

- **Recomendación técnica:**  
  - Introducir pipeline como código (GitHub Actions o Azure Pipelines): build, tests, análisis estático, y opcionalmente deploy a entornos no productivos.  
  - Producción: deploy desde pipeline con aprobación manual o desde rama protegida, usando secretos del pipeline (no en script en repo).  
  - Mantener separación clara DEV/SIT/QA/PROD mediante variables de entorno y secretos por entorno.

- **Nivel de urgencia:** Alta.

---

### H08 — Uso de FromSqlRaw en OutboxEmailSenderHostedService

- **Evidencia:**  
  - `OutboxEmailSenderHostedService.cs` (aprox. líneas 69–76): `FromSqlRaw(..., BatchSize)`. El único parámetro es `BatchSize` (numérico, controlado por código). El resto de la consulta son columnas y condiciones fijas.

- **Impacto de negocio:** Bajo. No hay entrada de usuario; riesgo de inyección SQL es mínimo. Uso de `FOR UPDATE SKIP LOCKED` es correcto para procesamiento concurrente.

- **Recomendación técnica:** Opcional: usar `FromSqlInterpolated` o parámetros nombrados para dejar explícito que no hay concatenación de entrada de usuario y cumplir políticas estrictas de “no raw SQL con parámetros literales”.

- **Nivel de urgencia:** Baja.

---

### H09 — GetJobQuery incluye todas las Proposals

- **Evidencia:**  
  - `GetJobQuery.cs`: se hace `.Include(j => j.Proposals)` y luego se usa `job.Proposals.Any(p => p.TechnicianId == req.RequesterId)` para el rol Technician (líneas 58–59). Se cargan todas las propuestas del job cuando solo interesa un booleano.

- **Impacto de negocio:** Para jobs con muchas propuestas, mayor uso de memoria y tiempo de respuesta. Escalabilidad moderada.

- **Recomendación técnica:** Sustituir por una consulta que no cargue la colección: por ejemplo `await db.Proposals.AnyAsync(p => p.JobId == req.JobId && p.TechnicianId == req.RequesterId, ct)` y eliminar `.Include(j => j.Proposals)` para este caso.

- **Nivel de urgencia:** Media.

---

### H10 — Borrado en cascada (User y otras entidades)

- **Evidencia:**  
  - `UserConfiguration`: `TechnicianProfile` con `OnDelete(DeleteBehavior.Cascade)` desde User.  
  - Otras configuraciones: Job → Assignment, Review, Payment, JobIssue, JobAlert, NotificationOutbox con Cascade; Proposal → NotificationOutbox; etc. (varios en `JobConfiguration`, `JobIssueConfiguration`, `JobAlertConfiguration`, `NotificationConfiguration`).

- **Impacto de negocio:** Borrar un User (o un Job) puede eliminar gran cantidad de datos asociados. Si no hay soft-delete ni flujos controlados, riesgo de pérdida de datos o borrados accidentales.

- **Recomendación técnica:** Revisar cada relación: donde sea necesario conservar historial (p. ej. Jobs, Proposals, Reviews), valorar `Restrict` o `SetNull` y manejar borrados en lógica de aplicación. Documentar las cascadas aceptadas y las que son intencionadas para limpieza.

- **Nivel de urgencia:** Media.

---

### H11 — appsettings.Development.json no excluido en .gitignore

- **Evidencia:**  
  - `.gitignore` incluye `appsettings.*.local.json` y `secrets.json`, pero **no** `appsettings.Development.json`. El archivo con credenciales de desarrollo puede quedar versionado.

- **Impacto de negocio:** Mismo que H01: exposición de credenciales si el archivo contiene datos reales.

- **Recomendación técnica:** Añadir `appsettings.Development.json` al `.gitignore` y usar User Secrets o variables de entorno en desarrollo, o mantener un `appsettings.Development.json.example` sin valores sensibles.

- **Nivel de urgencia:** Alta (complementario a H01).

---

### H12 — Sin tests unitarios; solo tests de integración

- **Evidencia:**  
  - En `tests/` solo existe `FixHub.IntegrationTests` (WebApplicationFactory + Testcontainers). No hay proyecto de tests unitarios para Domain ni Application.

- **Impacto de negocio:** Menor cobertura de regresiones en lógica de negocio y reglas de validación; refactorings más arriesgados.

- **Recomendación técnica:** Añadir proyecto(s) de tests unitarios para Application (handlers, validators) y Domain (reglas, value objects si los hubiera). Mantener los tests de integración para flujos de API.

- **Nivel de urgencia:** Media.

---

## 4. Fase 1 – Revisión arquitectónica

### 4.1 Separación de capas

- **Domain:** Sin referencias a otros proyectos. Contiene entidades, enums y se considera núcleo. **Correcto.**
- **Application:** Referencia solo a Domain. Contiene CQRS (MediatR), interfaces (p. ej. `IApplicationDbContext`), DTOs y validadores. **Correcto.**
- **Infrastructure:** Referencia Application y Domain. Implementa persistencia (EF Core, PostgreSQL), JWT, email, auditoría, outbox. **Correcto.**
- **API:** Referencia Application e Infrastructure (no Domain directamente). Registra servicios y middlewares. **Correcto.**
- **Web:** No referencia Application ni Infrastructure; consume la API vía HTTP. **Correcto.**
- **Migrator:** Referencia solo Infrastructure. **Correcto.**

No se detectaron violaciones de dependencias (nada apunta desde el centro hacia afuera).

### 4.2 Interfaces y servicios

- `IApplicationDbContext` está en Application; la implementación (`AppDbContext`) en Infrastructure. Los handlers usan la interfaz. Correcto.
- Servicios de infraestructura (JWT, email, hasher, auditoría, etc.) están abstraídos por interfaces y registrados en Infrastructure. Correcto.

### 4.3 Acoplamientos indebidos

- No se observan referencias directas a EF Core ni a detalles de infraestructura en la capa Application más allá del uso de `IApplicationDbContext` y `DbContext` en firmas (la interfaz devuelve `DbSet<>`, lo cual es aceptable en muchas implementaciones de Clean Architecture con “DbContext como repositorio”).

**Conclusión Fase 1:** Arquitectura de capas alineada con Clean Architecture. Sin violaciones críticas.

---

## 5. Fase 2 – Seguridad

### 5.1 Autenticación JWT

- JWT configurado con validación de Issuer, Audience, Lifetime y SigningKey. SecretKey con mínimo 32 caracteres y fallo en arranque si falta o es corta. **Correcto.**
- Riesgo: el valor del secret en desarrollo/producción (H01, H02).

### 5.2 Autorización por roles

- Políticas `CustomerOnly`, `TechnicianOnly`, `AdminOnly`, `CustomerOrAdmin` aplicadas en controladores. AdminController con `[Authorize(Policy = "AdminOnly")]`. **Correcto.**
- Fallo crítico: el rol Admin se puede obtener por registro (H03).

### 5.3 Endpoints sin protección

- Health sin autenticación (aceptable). Auth (login/register) sin `[Authorize]` pero con rate limiting "AuthPolicy". Resto de endpoints con `[Authorize]` y políticas por rol. **Correcto.**

### 5.4 IDOR (acceso por ID a recursos ajenos)

- GetJobQuery: comprueba CustomerId/TechnicianId/Admin y devuelve 403 si no corresponde. **Correcto.**  
- CompleteJobCommand, CancelJobCommand, TechnicianStartJobCommand, MarkNotificationReadCommand, CreateReviewCommand: verifican propiedad (CustomerId/TechnicianId/UserId). **Correcto.**  
- GetJobProposalsQuery: no verifica propiedad del job para Customer y además devuelve datos incorrectos (H04). **Incorrecto.**

### 5.5 SQL Injection, XSS, CSRF, Mass Assignment

- **SQL:** Uso de EF Core con parámetros; único raw SQL en Outbox (H08) con parámetro numérico. Sin concatenación de entrada de usuario. **Bajo riesgo.**  
- **XSS:** API devuelve JSON; responsabilidad de sanitización en cliente. Headers de seguridad (X-Content-Type-Options, X-Frame-Options, etc.) presentes en SecurityHeadersMiddleware. **Adecuado.**  
- **CSRF:** API con Bearer; riesgo bajo. Web con cookies; verificar antiforgery en formularios (H06).  
- **Mass Assignment:** DTOs son records con propiedades explícitas; no hay binding genérico a entidades. El riesgo está en RegisterRequest que incluye Role (H03), no en binding automático a entidades.

### 5.6 Secretos y credenciales hardcodeadas

- Ver H01, H02, H05. Resumen: appsettings.Development.json, deploy-fixhub.ps1 y documentación/seed con contraseñas o claves.

---

## 6. Fase 3 – Base de datos

### 6.1 Integridad referencial

- Claves foráneas y relaciones configuradas en EF. Índices en FKs y columnas de filtro (Job, Proposal, Notification, etc.). **Correcto.**

### 6.2 Borrados en cascada

- Ver H10. Múltiples relaciones con `DeleteBehavior.Cascade`. Revisar necesidad de negocio y considerar Restrict/SetNull donde proceda.

### 6.3 Índices

- Job: Status, CustomerId, CategoryId, CreatedAt, compuesto (Status, CategoryId).  
- Proposal: (JobId, TechnicianId) único, JobId, TechnicianId, Status.  
- Notification: (UserId, IsRead, CreatedAt).  
- NotificationOutbox: (Status, CreatedAt), JobId, (NotificationId, Channel) único.  
- JobIssue, JobAlert, AuditLog, etc. con índices coherentes. **Adecuado.**

### 6.4 N+1

- Los handlers usan `.Include`/`.ThenInclude` donde se accede a navegaciones. ListJobsQuery para Technician usa `j.Proposals.Any(...)` que se traduce a EXISTS, sin N+1. No se detectaron N+1 evidentes en los flujos revisados.

### 6.5 Transacciones y concurrencia

- `IApplicationDbContext` expone `BeginTransactionAsync`. OutboxEmailSenderHostedService usa transacción para reservar ítems con `FOR UPDATE SKIP LOCKED`. **Correcto.**  
- Job y Proposal usan `UseXminAsConcurrencyToken()` (concurrency optimista). **Correcto.**

---

## 7. Fase 4 – Performance

- **Paginación:** ListJobsQuery, ListMyJobsQuery, GetMyNotificationsQuery, ListApplicantsQuery, ListJobIssuesQuery, GetMyAssignmentsQuery usan Skip/Take y PagedResult; ListJobsQuery valida PageSize 1–100. **Correcto.**  
- **GetJobQuery:** Incluye Proposals completas para un solo chequeo (H09). Mejorable.  
- **GetOpsDashboardQuery:** Múltiples consultas acotadas (Take(AlertsLimit), etc.); uso de GroupBy y agregaciones en BD. Sin loops costosos evidentes.  
- No se revisaron posibles fugas de memoria en hosted services más allá de la lógica leída; el outbox procesa por lotes y timeouts. Recomendación: revisar en runtime si hay crecimientos anómalos.

---

## 8. Fase 5 – DevOps

- **Docker:** Dockerfile multi-stage para API, Web y Migrator; docker-compose con postgres, migrator, api y web; variables desde `.env`. **Correcto.**  
- **Variables de entorno:** API y compose usan env para ConnectionStrings, JWT, WebOrigin. El problema es el contenido de `.env` generado por script (H02) y archivos de config en repo (H01).  
- **Secretos en repositorio:** Ver H01, H02, H11.  
- **Pipeline CI/CD:** No existe pipeline como código (H07).  
- **Separación de ambientes:** Compose y Program usan ASPNETCORE_ENVIRONMENT y config por entorno; no hay pipelines que desplieguen a DEV/SIT/QA/PROD de forma automatizada y documentada.

---

## 9. Score de madurez y preparación para producción

### Cálculo aproximado (0–100)

- Arquitectura y capas: 90  
- Autorización y defensa en profundidad (handlers): 85  
- Autenticación JWT y políticas: 85 (penalizado por H03)  
- Gestión de secretos y configuración: 25  
- DevOps y CI/CD: 40  
- Base de datos (índices, transacciones, concurrencia): 80  
- Performance y paginación: 75  
- Tests (solo integración): 50  

**Score de madurez:** **62/100**.

### Nivel de preparación para producción

- **No recomendado** hasta:  
  - Eliminar credenciales y secretos del repositorio y scripts (H01, H02, H11).  
  - Impedir registro con Role=Admin (H03).  
  - Corregir GetJobProposalsQuery (propiedad y comportamiento para Customer) (H04).  
  - Definir política de contraseña de admin y documentación (H05).  
  - Introducir pipeline CI/CD y separación de ambientes (H07).

---

## 10. Conclusión

**¿Está el sistema listo para producción?**  
**No.**  

La base técnica (Clean Architecture, CQRS, JWT, autorización por recurso, paginación, concurrencia optimista, Docker) es sólida. Los bloqueantes son de **gestión de secretos**, **control de privilegios en registro** y **autorización/lógica en un endpoint concreto**. Una vez resueltos los hallazgos críticos (H01, H02, H03) y los altos indicados (H04, H05, H07, H11), y con una política clara de secretos y despliegue, el sistema podría ser considerado para un despliegue controlado en producción, manteniendo el seguimiento de los hallazgos medios y de mejora recomendada.

---

*Informe generado por auditoría estática de código y configuración. No se ha modificado ni ejecutado código.*
