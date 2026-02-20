# Bitácora FixHub — Contexto del sistema

**Última actualización:** 18–20 feb 2026  
**Objetivo:** Tener contexto suficiente para seguir trabajando en el sistema al día siguiente.

---

## 1. Qué es FixHub

- **Modelo de negocio:** Empresa de servicios con **técnicos propios/verificados** (no marketplace abierto). El mensaje es: *“FixHub envía técnicos verificados”*.
- **Flujo cliente:** El cliente **solicita un servicio** → FixHub (o el dueño/Admin) **asigna un técnico** → el cliente ve al técnico asignado con badges de confianza. No hay “feed público” de trabajos para el cliente.
- **Stack:** .NET 8, ASP.NET Core (API + Razor Pages Web), PostgreSQL, CQRS/MediatR, EF Core.

---

## 2. Estructura del proyecto

```
FixHub/
├── src/
│   ├── FixHub.API          # REST API (JWT, policies CustomerOnly/TechnicianOnly/AdminOnly)
│   ├── FixHub.Application  # Casos de uso, CQRS (MediatR), DTOs
│   ├── FixHub.Domain       # Entidades, enums (JobStatus, TechnicianStatus)
│   ├── FixHub.Infrastructure  # EF Core, PostgreSQL, migraciones
│   ├── FixHub.Web          # Razor Pages SSR, consume API vía IFixHubApiClient
│   └── FixHub.Migrator     # Ejecutor de migraciones
├── docs/                   # Informes, bitácora
└── FixHub.sln
```

- **Puertos típicos:** Web HTTPS 7200, HTTP 5200; API HTTP 5100, HTTPS 7100.  
- **Config Web→API:** `ApiSettings:BaseUrl` (ej. `http://localhost:5100`) en `appsettings*.json`.

---

## 3. Roles y rutas principales

| Rol        | Navbar / Acceso principal | Comportamiento / Páginas clave |
|-----------|----------------------------|--------------------------------|
| **Customer** | Mis solicitudes, Solicitar servicio | `/Requests/My`, `/Requests/New`. No ve feed de trabajos; al ir a `/Jobs` redirige a `/Requests/My`. |
| **Technician** | Trabajos | `/Jobs` = panel (KPIs, Oportunidades disponibles, Mis asignados, Completados). Envía propuestas desde detalle del job. |
| **Admin** | Trabajos, Postulantes | `/Jobs` (listado), `/Jobs/Detail/{id}` ve propuestas y puede **Aceptar propuesta (asignar)**. `/Admin/Applicants` para postulantes técnicos. |

- **Usuario Admin por defecto (tras migración SeedAdminUser):** `admin@fixhub.com` / `Admin123!` — usar para asignar técnicos a trabajos sin depender del Customer.
- **Autenticación Web:** Cookie con JWT en claim `jwt_token`; `BearerTokenHandler` inyecta el token en las llamadas a la API.

---

## 4. Flujo de “asignar trabajo” (FixHub = empresa, no marketplace)

1. **Cliente** crea solicitud: `/Requests/New` → POST `/api/v1/jobs` → job en estado **Open** (o **Assigned** si hay técnico Approved y se auto-asigna). En detalle solo ve estado y mensaje “Estamos asignando un técnico a tu solicitud.” (no ve propuestas).
2. **Técnico** ve el trabajo en **Trabajos** → **Oportunidades disponibles** (GET `/api/v1/jobs?status=1`).
3. **Técnico** entra al detalle y envía **propuesta**: POST `/api/v1/jobs/{id}/proposals` (precio, mensaje). No ve propuestas de otros.
4. **Asignar:** **Solo Admin** en `/Jobs/Detail/{id}` ve la sección **Propuestas** y pulsa **“Aceptar propuesta (asignar trabajo)”** → POST `/api/v1/proposals/{proposalId}/accept`. Customer no puede aceptar.
5. Backend: crea `JobAssignment`, pone el job en **Assigned**, rechaza el resto de propuestas pendientes.
6. Cliente en **Mis solicitudes** → Ver detalle ve al **Técnico asignado** con badges (GET job devuelve `AssignedTechnicianId`/`AssignedTechnicianName`).

- **Tablas implicadas:** `jobs`, `proposals`, `job_assignments`. Estado del job: Open → Assigned / InProgress → Completed.

---

## 5. API relevante (sin cambiar contratos)

- **Jobs:**  
  - GET `/api/v1/jobs?page=&pageSize=&status=` — listado (Technician/Admin; status=1 = Open).  
  - GET `/api/v1/jobs/mine` — solo Customer (sus solicitudes).  
  - GET `/api/v1/jobs/{id}` — detalle.  
  - POST `/api/v1/jobs` — crear (CustomerOnly).
- **Proposals:**  
  - GET `/api/v1/jobs/{id}/proposals` — Admin ve todas; Technician solo las suyas. Customer no ve.  
  - POST `/api/v1/jobs/{id}/proposals` — TechnicianOnly.  
  - POST `/api/v1/proposals/{id}/accept` — **Solo Admin** (asignar trabajo).
- **Technicians:**  
  - GET `/api/v1/technicians/me/assignments` — asignaciones del técnico.  
  - GET `/api/v1/technicians/{id}/profile` — perfil técnico.
- **Admin:**  
  - GET `/api/v1/admin/applicants` — postulantes (TechnicianStatus).  
  - PATCH `/api/v1/admin/technicians/{id}/status` — actualizar estado postulante.

---

## 6. Base de datos (tablas principales)

- **users** — cuentas (rol Customer/Technician/Admin).  
- **jobs** — solicitudes/trabajos (CustomerId, CategoryId, Status, etc.).  
- **proposals** — propuestas de técnicos por job (TechnicianId, JobId, Price, Status: Pending/Accepted/Rejected).  
- **job_assignments** — asignación job–propuesta (JobId, ProposalId, AcceptedAt, CompletedAt).  
- **technician_profiles** — perfil técnico (user_id, status: 0=Pending, 1=InterviewScheduled, **2=Approved**, 3=Rejected, is_verified, etc.). Para **auto-asignación** al crear un job, se usa el primer técnico con **status = 2 (Approved)**.  
- **service_categories** — categorías (plomería, electricidad, etc.).  
- **reviews** — reseñas cliente→técnico.

---

## 7. Cambios recientes (sesión 17–18 feb 2026)

- **UX Customer:**  
  - `/Jobs` para Customer → redirect a `/Requests/My`.  
  - Nuevas páginas: **Solicitar servicio** (`/Requests/New`) con cards de categoría y formulario; **Mis solicitudes** (`/Requests/My`) con cards (estado en lenguaje cliente: Recibida, Técnico asignado / En progreso, Finalizada).  
  - Navbar Customer: “Mis solicitudes” y “Solicitar servicio”; Technician/Admin: “Trabajos”.  
  - Detail: dueño ve “Tu solicitud”; Volver para dueño → `/Requests/My`.
- **Asignar trabajo:**  
  - Dueño (Customer) y Admin ven **propuestas** en `/Jobs/Detail/{id}` y pueden **Aceptar propuesta (asignar trabajo)**.  
  - API `AcceptProposal`: permite Admin además del dueño; comando `AcceptProposalCommand(ProposalId, AcceptedByUserId, AcceptAsAdmin)`.
- **Jobs/Index (técnico):**  
  - Fix **ModelState Invalid**: propiedad `Page` renombrada a `PageNumber` (binding `Name = "page"`).  
  - Logging: “API devolvió X oportunidades abiertas (TotalCount=Y)”.  
  - Botón “Refrescar” en Oportunidades disponibles; mensaje si 0 oportunidades.
- **Admin:** Panel postulantes (`/Admin/Applicants`), TechnicianStatus (Pending, InterviewScheduled, Approved, Rejected), migración `AddTechnicianStatus`.
- **Otros:** Recruit/Apply, badges técnico asignado, Create redirect a `/Requests/My`, ResultExtensions 204/NOT_FOUND.

---

## 8. Usuario Admin y migración

- **Migración:** `20260220000000_SeedAdminUser.cs` inserta un usuario **Admin** si no existe: email `admin@fixhub.com`, contraseña `Admin123!` (role = 3 en tabla `users`).
- **Aplicar migración:** desde la raíz del repo, `dotnet run --project src/FixHub.Migrator/FixHub.Migrator.csproj` (o ejecutar migraciones desde la API/Web al arrancar, si está configurado).
- **Asignar técnico con Admin:** iniciar sesión en la Web con `admin@fixhub.com` / `Admin123!` → **Trabajos** → abrir el trabajo (detalle) → en **Propuestas**, pulsar **Aceptar propuesta (asignar trabajo)**. No hace falta que el Customer vea las propuestas; el Admin puede asignar.

---

## 9. Puntos a tener en cuenta

- **Compilación:** Si la Web o la API están en ejecución, `dotnet build` puede fallar por bloqueo de .exe/dll; detener el proceso o compilar solo el proyecto que no está corriendo.
- **Customer no ve trabajos de otros:** Solo usa GET `/api/v1/jobs/mine` (Mis solicitudes). El feed de “trabajos disponibles” es solo para Technician/Admin en `/Jobs`.
- **Oportunidades vacías para técnico:** Si GET `/api/v1/jobs?status=1` devuelve 0 ítems, revisar: misma BD para Web y API, jobs en estado Open, y en logs “API devolvió X oportunidades” para ver qué devuelve la API.

---

## 10. Repositorio

- **Remoto:** `https://github.com/IrvingCorrosk19/FixHub.git`  
- **Último push:** commit `82a4fba` (main) — UX Customer (Requests), asignar trabajo, Admin postulantes, TechnicianStatus.

---

*Documento para continuidad; actualizar esta bitácora cuando se añadan features o se cambien flujos importantes.*
