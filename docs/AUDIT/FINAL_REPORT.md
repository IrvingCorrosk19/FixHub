# FixHub — Reporte final de auditoría (production-grade 100/100)

**Clasificación:** Confidencial — Uso interno  
**Branch:** `audit/fixhub-100`  
**Fecha:** 2025-02-23  
**Metodología:** Auditoría estática + pruebas funcionales y de seguridad controladas (local/SIT/QA). Sin modificación de código de producto; solo documentación y pipeline en branch.

---

## 1. Resumen ejecutivo (1 página)

FixHub es un marketplace de servicios del hogar (ASP.NET Core 8, EF Core, PostgreSQL, CQRS/MediatR, JWT, Docker). Se realizó una auditoría completa en seis fases: inventario y mapa del sistema (Fase 0), pruebas funcionales E2E (Fase 1), pruebas de seguridad OWASP (Fase 2), auditoría estática de código (Fase 3), DevSecOps y CI/CD (Fase 4), y este reporte final (Fase 5).

**Conclusiones principales:**

- **Arquitectura:** Separación de capas (Domain, Application, Infrastructure, API) correcta; sin violaciones de Clean Architecture.
- **Seguridad:** Existen **tres hallazgos críticos** que bloquean producción: (1) credenciales en repositorio (`appsettings.Development.json`), (2) credenciales y secretos en script de despliegue (`deploy-fixhub.ps1`), (3) registro público permite Role=Admin (escalación de privilegios). Además, un hallazgo **alto**: GetJobProposalsQuery no valida ownership para Customer y la funcionalidad para Customer es incorrecta (lista vacía).
- **DevSecOps:** Se ha **implementado** en este branch un pipeline CI (Build, Test, CodeQL, dependency scan, Gitleaks, Trivy). La falta de pipeline como código (H07) queda cubierta por `.github/workflows/ci.yml`; el resto de hallazgos requieren cambios de código y configuración.
- **Datos:** No se tocaron datos reales ni PROD; todas las pruebas y referencias son para entorno local/SIT/QA. No se realizaron deletes destructivos.

**Conclusión final:** El sistema se considera **NOT READY** para go-live hasta remediar los hallazgos críticos (H01, H02, H03) y los altos indicados (H04, H05, H11). Tras remediación y verificación, el score de madurez puede alcanzar el rango objetivo (80+). El objetivo 100/100 se alcanza con la aplicación completa del plan de remediación y de la checklist de go-live.

---

## 2. Score de madurez 0–100 (criterios y pesos)

| Criterio | Peso | Puntuación (0–10) | Notas |
|----------|------|-------------------|--------|
| Arquitectura y separación de capas | 15% | 9 | Clean Architecture correcta; sin dependencias invertidas. |
| Autorización y ownership (IDOR) | 15% | 6 | Mayoría correcta; GetJobProposals (Customer) falla. |
| Autenticación y gestión de secretos | 15% | 3 | JWT correcto; secretos en repo y script (H01, H02). |
| Control de privilegios (registro Admin) | 10% | 2 | Register acepta Admin (H03). |
| Pruebas (E2E / integración / unitarias) | 10% | 6 | Integración buena; E2E Postman añadida; sin unitarias. |
| Pipeline CI/CD y DevSecOps | 10% | 8 | Pipeline implementado en branch (build, test, CodeQL, deps, secrets, container). |
| Base de datos (índices, transacciones, concurrencia) | 10% | 8 | Índices y xmin correctos; cascadas a documentar. |
| Performance y paginación | 5% | 7 | Paginación y límites; un Include innecesario (GetJobQuery). |
| Documentación y gobernanza | 5% | 7 | Documentación de auditoría completa; branch protection documentada. |
| Cumplimiento (OWASP, sin secretos en repo) | 5% | 3 | OWASP parcial; secretos en repo. |

**Cálculo:** (9×0.15 + 6×0.15 + 3×0.15 + 2×0.10 + 6×0.10 + 8×0.10 + 8×0.10 + 7×0.05 + 7×0.05 + 3×0.05) × 10 = **5.95** → **Score redondeado: 59/100** (estado actual, pre-remediación).

**Score post-remediación (objetivo):** Con H01, H02, H03, H04, H05, H11 resueltos y pipeline en main: estimado **78–82/100**. Con mejoras adicionales (H06, H09, H10, H12, CSP, unit tests): **85–100/100**.

---

## 2.1 Definición de 100/100 (criterios medibles — Big4/ISO 27001)

Para que el sistema alcance el estándar **100/100 audit-grade**, deben cumplirse **todos** los criterios siguientes (medibles y verificables):

| # | Criterio | Medición / Evidencia |
|---|----------|----------------------|
| 1 | **0 hallazgos críticos** | Ningún ítem clasificado como 🔴 Crítico abierto. H01, H02, H03 cerrados con evidencia. |
| 2 | **0 hallazgos altos** | Ningún ítem 🟠 Alto abierto. H04, H05, H11 (y H07 ya mitigado) cerrados con evidencia. |
| 3 | **CI en main con checks obligatorios** | Pipeline `.github/workflows/ci.yml` en rama `main`; branch protection que exija que los jobs *Build & Test*, *CodeQL*, *Secrets scan* (y opcionalmente *Container scan*) pasen antes de merge. |
| 4 | **Secretos fuera del repo** | No existen credenciales, API keys ni secretos en archivos versionados. Gitleaks (o equivalente) en CI sin hallazgos; `appsettings.Development.json` en .gitignore o con valores vacíos/plantilla. Scripts de deploy sin literales sensibles. |
| 5 | **Tests mínimos** | Suite de integración (FixHub.IntegrationTests) pasando en CI; al menos un test que verifique rechazo de registro con Role=Admin (400/403); al menos 3 escenarios IDOR documentados con evidencia (403). Ver [APPENDIX_EVIDENCE.md](APPENDIX_EVIDENCE.md). |
| 6 | **Backups y restore probado** | Política de backups de BD documentada; al menos una prueba de restore ejecutada y documentada (evidencia en runbook o registro de cambio). |
| 7 | **Observabilidad mínima** | Health check (`/api/v1/health`) operativo; logs estructurados (ej. CorrelationId, RequestLogging); sin logueo de secretos. Opcional: métricas o APM según política interna. |

*Cualquier criterio no cumplido impide la calificación 100/100 hasta su cierre con evidencia.*

---

## 3. Matriz de riesgos (Crítico / Alto / Medio / Mejora)

| ID | Hallazgo | Clasificación | Urgencia |
|----|----------|----------------|----------|
| H01 | Credenciales en repositorio (appsettings.Development.json) | 🔴 Crítico | Inmediata |
| H02 | Credenciales en script de despliegue (deploy-fixhub.ps1) | 🔴 Crítico | Inmediata |
| H03 | Registro permite Role=Admin | 🔴 Crítico | Inmediata |
| H04 | GetJobProposalsQuery: Customer sin ownership + comportamiento incorrecto | 🟠 Alto | Alta |
| H05 | Contraseña por defecto admin (seed/documentación) | 🟠 Alto | Alta |
| H07 | Falta pipeline CI/CD | 🟠 Alto → **Mitigado** | Pipeline en branch (ci.yml). |
| H11 | appsettings.Development.json no en .gitignore | 🟠 Alto | Alta |
| H06 | Antiforgery en Web (cookies) | 🟡 Medio | Media |
| H09 | GetJobQuery Include(Proposals) innecesario | 🟡 Medio | Media |
| H10 | DeleteBehavior.Cascade riesgos | 🟡 Medio | Media |
| H12 | Sin tests unitarios | 🟡 Medio | Media |
| H08 | FromSqlRaw (Outbox) | 🟢 Mejora | Baja |

---

## 4. Hallazgos (evidencia, impacto, fix concreto)

Cada hallazgo incluye: **evidencia (archivo + línea)**, **impacto de negocio**, **fix concreto**. Los valores sensibles se redactan (REDACTED); pasos de rotación se indican sin pegar credenciales.

### H01 — Credenciales en repositorio

- **Evidencia:** `src/FixHub.API/appsettings.Development.json` — ConnectionStrings con Password=REDACTED, JwtSettings:SecretKey=REDACTED.
- **Impacto:** Exposición de BD y JWT si el repo es público o se filtra.
- **Fix:** Vaciar valores en el archivo o usar User Secrets; añadir `appsettings.Development.json` a `.gitignore` o mantener solo plantilla. Rotar contraseña PostgreSQL y JWT secret expuestos.

### H02 — Credenciales en script de despliegue

- **Evidencia:** `src/Com/deploy-fixhub.ps1` — líneas 4–7 (REDACTED: password SSH, hostname, hostkey); líneas 32–42 (REDACTED: POSTGRES_PASSWORD, JWT_SECRET_KEY en .env).
- **Impacto:** Compromiso de VPS, BD y aplicación.
- **Fix:** Leer secretos desde variables de entorno o gestor de secretos; no literales en script. Rotar todas las credenciales expuestas (SSH, PostgreSQL, JWT).

### H03 — Registro permite Role=Admin

- **Evidencia:** `src/FixHub.Application/Features/Auth/RegisterCommand.cs` — validator línea 36 (Role IsInEnum, no excluye Admin); handler asigna request.Role.
- **Impacto:** Cualquier usuario puede registrarse como Admin.
- **Fix:** En RegisterCommandValidator: `Must(r => r != UserRole.Admin)` con mensaje; o en handler rechazar con Result.Failure. Administradores solo por seed o proceso interno.

### H04 — GetJobProposalsQuery (Customer)

- **Evidencia:** `src/FixHub.Application/Features/Proposals/GetJobProposalsQuery.cs` líneas 18–41 — no se comprueba job.CustomerId == RequesterId; para no-Admin devuelve solo propuestas con TechnicianId == RequesterId (Customer recibe lista vacía).
- **Impacto:** IDOR si se devuelven todas las propuestas sin validar; funcionalidad incorrecta para Customer.
- **Fix:** Si es Customer, comprobar job.CustomerId == RequesterId; si no, 403. Si el negocio exige que el Customer vea propuestas de su job, para Customer dueño devolver todas las propuestas del job; Technician sigue viendo solo las suyas.

### H05 — Contraseña por defecto admin

- **Evidencia:** `src/FixHub.Infrastructure/Persistence/Migrations/20260220000000_SeedAdminUser.cs` — comentario y hash para REDACTED. Scripts en `src/Com/` mencionan la misma contraseña.
- **Impacto:** Acceso administrativo trivial si no se cambia en prod.
- **Fix:** Documentar cambio obligatorio en primer inicio; valorar hash desde variable de entorno en prod. No documentar la contraseña por defecto en repo.

### H07 — Pipeline CI/CD

- **Estado:** **Mitigado** en este branch. Entregable: `.github/workflows/ci.yml` (build, test, CodeQL, dependency scan, Gitleaks, Trivy). Ver `docs/AUDIT/04_CICD_PLAN.md`.

### H11 — .gitignore

- **Evidencia:** `.gitignore` no incluye `appsettings.Development.json`.
- **Fix:** Añadir `appsettings.Development.json` al `.gitignore` o usar plantilla sin secretos.

---

## 5. Plan de remediación (Owner, evidencia requerida, criterio de aceptación)

Para cada hallazgo se define **responsable**, **evidencia requerida** y **criterio de aceptación** para el cierre auditable.

| ID | Owner/Responsable | Evidencia requerida | Criterio de aceptación |
|----|-------------------|---------------------|-------------------------|
| **H01** | DevOps / Dev lead | 1) Diff que muestre que `appsettings.Development.json` no contiene secretos (o archivo en .gitignore). 2) Captura o log de User Secrets / env en uso en desarrollo. 3) Registro de rotación de credenciales expuestas (REDACTED). | Repo sin credenciales en appsettings versionado; rotación documentada. |
| **H02** | DevOps / Infra | 1) Versión de `deploy-fixhub.ps1` (o equivalente) que lea secretos desde variables de entorno o secret store; sin literales. 2) Documentación de variables requeridas (nombres, no valores). 3) Registro de rotación SSH, POSTGRES, JWT (REDACTED). | Script sin secretos; despliegue ejecutado al menos una vez con secretos desde entorno. |
| **H03** | Backend lead | 1) Diff del validator o handler que rechace `UserRole.Admin`. 2) Test de integración que envíe POST register con `role: 3` y espere 400 (o 403) con código/mensaje identificable. 3) Evidencia en Appendix A (request/response redactado). | Register con Role=Admin devuelve 400/403; test en CI en verde. |
| **H04** | Backend lead | 1) Diff de GetJobProposalsQuery: verificación `job.CustomerId == RequesterId` para Customer; devolución de propuestas del job cuando el Customer es dueño. 2) Test(s) que verifiquen 403 cuando Customer pide proposals de job ajeno; 200 con lista cuando es dueño. | Customer solo ve propuestas de sus jobs; Customer ajeno recibe 403. |
| **H05** | DevOps / Security | 1) Runbook o paso de post-deploy: “Cambiar contraseña de admin en primer uso”. 2) Eliminación o redacción en repo de referencias a la contraseña por defecto (scripts en `src/Com/`, comentarios en migración). | Prod sin contraseña por defecto documentada; runbook con paso de cambio. |
| **H07** | DevOps | 1) Merge de `.github/workflows/ci.yml` en `main`. 2) Captura de GitHub: branch protection con checks obligatorios (build, test, CodeQL, secrets scan). | CI en main; PRs bloqueados si los checks fallan. |
| **H11** | Dev lead | 1) Diff de `.gitignore`: línea que incluya `appsettings.Development.json` (o equivalente). | .gitignore actualizado; commit sin reintroducir el archivo con secretos. |
| **H06** | Frontend / Web | 1) Revisión de páginas Razor que envían formularios: presencia de antiforgery (automático o explícito); ausencia de `[IgnoreAntiforgeryToken]` en acciones sensibles. 2) Documento de una línea en AUDIT. | Formularios de login/acción protegidos; sin IgnoreAntiforgeryToken en esas acciones. |
| **H09** | Backend | 1) Diff: eliminación de `.Include(j => j.Proposals)` en GetJobQuery y uso de `AnyAsync` para el booleano Technician. | Misma funcionalidad; menor carga de datos en GetJob. |
| **H10** | Backend / DBA | 1) Documento (en AUDIT o en repo) que liste relaciones con Cascade y justificación o decisión de mantener/ajustar. | Cascadas documentadas; cambios de Restrict si aplica. |
| **H12** | Dev lead | 1) Nuevo proyecto de tests unitarios (ej. FixHub.UnitTests) con al menos un test por capa Application/Domain. 2) CI ejecutando unit tests. | Suite unitaria existente y en CI. |
| **H08** | Backend | 1) (Opcional) Sustitución de FromSqlRaw por FromSqlInterpolated en OutboxEmailSenderHostedService; mismo comportamiento. | Sin cambio de comportamiento; código alineado con política de no-raw. |

---

## 6. Checklist de go-live

- [ ] H01, H02, H03 remediados y verificados.
- [ ] H04, H05, H11 remediados.
- [ ] Pipeline CI en `main` y pasando (build, test, CodeQL, secrets scan).
- [ ] Secretos de producción en gestor seguro (no en repo ni en scripts).
- [ ] Contraseña de admin cambiada en producción; sin contraseña por defecto documentada en repo.
- [ ] Rate limit y SecurityHeaders verificados en entorno de pre-producción.
- [ ] CORS configurado solo para orígenes permitidos.
- [ ] BD: backups y política de retención documentadas; cascadas revisadas.
- [ ] Branch protection y convenciones de versionado aplicadas (ver 04_CICD_PLAN.md).

---

## 6.1 Condiciones para cambiar NOT READY → READY (checklist y sign-off)

El estado **NOT READY** solo puede cambiarse a **READY** cuando se cumplan **todas** las condiciones siguientes y quede registrado el sign-off.

### Checklist obligatorio

- [ ] **C1.** Cero hallazgos críticos abiertos (H01, H02, H03 cerrados con evidencia en Plan de remediación §5).
- [ ] **C2.** Cero hallazgos altos abiertos (H04, H05, H11 cerrados; H07 ya mitigado con CI en main).
- [ ] **C3.** Pipeline CI en rama `main`; jobs Build & Test, CodeQL y Secrets scan configurados como required en branch protection.
- [ ] **C4.** Secret scan (Gitleaks o equivalente) ejecutado en el repo actual; resultado sin hallazgos o con excepciones documentadas y aprobadas.
- [ ] **C5.** Test de escalación de privilegios: register con Role=Admin devuelve 400 o 403; evidencia en Appendix A.
- [ ] **C6.** Al menos 3 pruebas IDOR documentadas con respuesta 403 (evidencia redactada en Appendix A).
- [ ] **C7.** Backups de BD: política documentada y al menos una prueba de restore documentada.
- [ ] **C8.** Observabilidad mínima: health check operativo; logs sin secretos; CorrelationId/RequestLogging en uso.

### Sign-off (registro auditable)

| Rol | Nombre (o equipo) | Fecha | Firma / Comentario |
|-----|-------------------|--------|---------------------|
| Responsable técnico | ________________ | ______ | Checklist C1–C8 verificado |
| Seguridad / Cumplimiento | ________________ | ______ | Sin objeciones para go-live |
| Product Owner / Sponsor | ________________ | ______ | Aprobación para producción |

*Una vez completada la checklist y los sign-offs, el reporte puede actualizarse a **READY** y archivarse como evidencia de auditoría.*

---

## 7. Evidencias (links a docs/AUDIT/)

| Documento | Descripción |
|------------|-------------|
| [00_SYSTEM_MAP.md](00_SYSTEM_MAP.md) | Inventario y mapa del sistema (componentes, puertos, flujos, roles, rutas). |
| [01_FUNCTIONAL_TESTS.md](01_FUNCTIONAL_TESTS.md) | Pruebas funcionales E2E: cómo ejecutar, cobertura, resultados esperados. |
| [02_SECURITY_TESTS.md](02_SECURITY_TESTS.md) | Pruebas de seguridad OWASP (escalación, IDOR, JWT, rate limit, headers, CORS, secretos). |
| [02_SECURITY_MATRIX.md](02_SECURITY_MATRIX.md) | Matriz OWASP Top 10 vs estado. |
| [03_STATIC_REVIEW.md](03_STATIC_REVIEW.md) | Auditoría estática: Authorize, ownership, Include, SQL raw, validación, logging, errores, cookies, Cascade, concurrencia. |
| [03_PERFORMANCE_FINDINGS.md](03_PERFORMANCE_FINDINGS.md) | Top 10 mejoras de performance. |
| [04_CICD_PLAN.md](04_CICD_PLAN.md) | Plan DevSecOps; branch protection; versionado; pipeline `.github/workflows/ci.yml`. |
| [APPENDIX_EVIDENCE.md](APPENDIX_EVIDENCE.md) | **Appendix A — Evidence Logs:** secret scan, IDOR (403), escalación de privilegios (400/403), placeholders y cómo obtener evidencias. |

**Assets de pruebas:**

- `tests/AUDIT_E2E/` — Colección Postman y environment para E2E por API.
- `tests/FixHub.IntegrationTests/` — Tests de integración existentes (Testcontainers).

---

## 8. Declaraciones y conclusión

- **No se modificó** código de producto sin branch (todos los cambios están en `audit/fixhub-100`: documentación y `.github/workflows/ci.yml`).
- **No se tocaron** datos reales ni PROD; pruebas solo local/SIT/QA.
- **Secretos:** No se pegaron en este reporte; se usó REDACTED y pasos de rotación.
- **Lo no verificado:** Configuración detallada de cookies (HttpOnly, Secure, SameSite) en Web no fue verificada en profundidad; se recomienda revisión manual. Evidencias de secret scan (Gitleaks/Trivy/CodeQL) y de pruebas IDOR/escalación se documentan en [APPENDIX_EVIDENCE.md](APPENDIX_EVIDENCE.md), con placeholders cuando el CI no ha sido ejecutado aún.

---

## Conclusión final

**¿Está el sistema listo para producción?**

**NOT READY.**

Deben resolverse los hallazgos **críticos (H01, H02, H03)** y los **altos (H04, H05, H11)** antes de un go-live. Con el pipeline CI implementado en este branch (H07 mitigado), la base técnica y la documentación de auditoría permiten alcanzar un nivel production-grade una vez aplicado el plan de remediación y la checklist de go-live anteriores.

---

*Reporte generado en el marco de la auditoría FixHub production-grade. Branch: audit/fixhub-100.*
