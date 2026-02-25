# FixHub — Reporte QA funcional final (auditable)

**Clasificación:** Uso interno / Auditoría funcional  
**Sistema:** FixHub (API REST v1, ASP.NET Core 8, Clean Architecture)  
**Alcance:** Pruebas funcionales E2E por API (Customer, Technician, Admin, flujos E2E, robustez)  
**Fuente de verdad:** `docs/QA/00_SYSTEM_FUNCTIONAL_OVERVIEW.md`, `docs/QA/00_BASELINE_CONFIRMATION.md`  
**Fecha:** 2025-02-25  
**Prefijo datos prueba:** FUNC_&lt;timestamp&gt;

---

## 1. Resumen ejecutivo (1 página)

Se diseñó e implementó una batería de pruebas funcionales end-to-end para FixHub, alineada con el documento de comprensión funcional y el baseline confirmado contra el código. Los artefactos incluyen: (1) baseline de endpoints y roles confirmados, (2) matriz de 51 casos de prueba (TC-001 a TC-051) por rol y flujo, (3) colección Postman ejecutable y environment en `tests/FUNCTIONAL_E2E/postman/`, (4) guía de ejecución y (5) plantilla de resultados y reporte final.

**Ejecución real:** En el entorno de generación de este reporte **no se ejecutó** la suite (no hay API ni Newman/Postman disponibles). Por tanto, los resultados están en estado **NOT EXECUTED**; el reporte sirve como entregable auditable y como plantilla para cuando se ejecute en local/SIT/QA.

**Bugs conocidos (documentados en código y auditorías previas):**

- **H03 (Crítico):** El registro permite crear usuarios con Role=Admin. Cualquier usuario puede obtener privilegios de administrador. Bloqueante para producción.
- **H04 (Alto):** El endpoint GET /jobs/{id}/proposals no valida ownership para Customer; en la práctica el Customer dueño del job recibe lista vacía (filtro por TechnicianId). Impide que el cliente vea las propuestas de su propia solicitud.

**Conclusión funcional:** El sistema se considera **NOT READY** para go-live desde el punto de vista funcional y de control de privilegios hasta corregir al menos H03 y H04 y re-ejecutar la batería de pruebas con resultados PASS en los casos P0 afectados.

---

## 2. Alcance y entorno

| Aspecto | Detalle |
|---------|---------|
| **Entorno permitido** | Solo local / SIT / QA. Prohibido producción. |
| **Datos** | No se tocaron datos reales. Datos de prueba con prefijo FUNC_&lt;timestamp&gt;. |
| **Código** | No se modificó código de producto. Solo documentación y artefactos de prueba. |
| **API** | FixHub.API v1 (REST): Auth, Jobs, Proposals, Technicians, Reviews, Notifications, Admin, AiScoring, Health. |
| **Clientes** | Postman/Newman (colección); opcionalmente scripts REST equivalentes. |

---

## 3. Cobertura (qué se probó por rol)

| Rol / Área | Casos | Descripción |
|------------|-------|-------------|
| **Auth** | TC-001, TC-002, TC-019, TC-030, TC-051 | Registro Customer/Technician/Admin, login, health sin auth. |
| **Customer** | TC-003 a TC-018 | Crear job, validaciones, listar mis jobs, detalle, propuestas (H04), cancelar, completar, review, incidencias, notificaciones. |
| **Technician** | TC-019 a TC-029 | Registro, listar jobs, propuesta, duplicado, asignaciones, iniciar job, restricciones (no iniciar sin asignación, no /mine). |
| **Admin** | TC-030 a TC-042 | Dashboard, listar jobs/propuestas, aceptar propuesta, forzar start/status, applicants, issues, métricas; Customer/Technician → dashboard 403. |
| **E2E** | TC-043 a TC-046 | Flujo feliz (crear → asignar → iniciar → completar → review); cancelar antes de asignar; técnico sin asignación 403; completar en Open 400. |
| **Robustez** | TC-047 a TC-051 | JSON inválido, campos extra, idempotencia complete, error sin stack, health. |

Total: **51 casos** (P0: críticos para flujo y seguridad; P1: validaciones y negativos; P2: complementarios).

---

## 4. Resultados (PASS/FAIL y métricas)

Dado que la suite **no fue ejecutada** en este entorno:

| Métrica | Valor |
|---------|--------|
| Total casos | 51 |
| Ejecutados | 0 |
| PASS | — |
| FAIL | — |
| NOT EXECUTED | 51 |
| Cobertura ejecutada | 0% |

Para obtener resultados reales:

1. Levantar la API (local o SIT/QA).
2. Ejecutar la colección Postman con el environment (ver `docs/QA/02_HOW_TO_RUN.md`).
3. Actualizar `docs/QA/03_EXECUTION_RESULTS.md` con status real y evidencia.
4. Recalcular métricas y actualizar este apartado.

---

## 5. Bugs encontrados (tabla)

Bugs conocidos por código y auditorías previas (docs/QA_FUNCTIONAL_REPORT.md, docs/AUDIT/FINAL_REPORT.md), reproducibles con la batería actual:

| BUG-ID | Severidad | Módulo | Pasos para reproducir | Resultado esperado vs actual | Evidencia | Impacto negocio | Recomendación (funcional) |
|--------|-----------|--------|------------------------|-----------------------------|-----------|------------------|----------------------------|
| **H03** | Crítico | Auth | POST /api/v1/auth/register con body `{ "fullName": "Test", "email": "test@test.local", "password": "Password1!", "role": 3 }`. | 400 o 403 (rechazo de Role=Admin). | Actual: 201, usuario Admin creado. RegisterCommand.cs: validator no excluye Admin; handler asigna request.Role. | Escalación de privilegios; cualquier usuario puede ser Admin. | Rechazar Role=Admin en registro (validator o handler). Administradores solo por seed o proceso interno. |
| **H04** | Alto | Proposals | Como Customer dueño del job: GET /api/v1/jobs/{jobId}/proposals con token del Customer. | 200 con lista de propuestas del job. | Actual: 200 con lista vacía. GetJobProposalsQuery no comprueba job.CustomerId == RequesterId; para no-Admin filtra por TechnicianId == RequesterId. | Cliente no puede ver propuestas de su propia solicitud. Si se “arregla” devolviendo todas sin validar propiedad, riesgo IDOR. | Para Customer: comprobar job.CustomerId == RequesterId; si no es dueño 403. Si es dueño, devolver todas las propuestas del job. |

Otros hallazgos ya documentados (no repetidos aquí): credenciales en repo (H01), script deploy (H02), contraseña por defecto admin (H05), etc. — ver docs/AUDIT/FINAL_REPORT.md.

---

## 6. Conclusión funcional

### READY o NOT READY

**NOT READY** (solo desde funcionalidad y control de privilegios).

Motivos:

1. **H03:** Registro con Role=Admin aceptado; impacto crítico en seguridad y gobernanza.
2. **H04:** Customer no puede ver propuestas de su job; impacto alto en experiencia y posible IDOR si se parchea mal.
3. **Ejecución:** La batería no ha sido ejecutada; no hay evidencia de PASS/FAIL en los 51 casos.

### Condiciones para READY (funcional)

1. **Corrección de H03:** POST register con `role: 3` (Admin) devuelve 400 o 403; test de regresión que lo verifique (incluido en matriz como TC-030).
2. **Corrección de H04:** Customer dueño del job recibe en GET /jobs/{id}/proposals la lista de propuestas de ese job; Customer no dueño recibe 403. Test TC-009 debe pasar.
3. **Ejecución de la suite:** En local o SIT/QA, ejecutar la colección Postman (o equivalente) y documentar resultados en 03_EXECUTION_RESULTS.md.
4. **Criterio de aceptación:** Todos los casos P0 de la matriz en PASS (salvo los que documenten un bug aceptado temporalmente con ticket). Sin bugs críticos abiertos (H03 cerrado).

Cuando se cumplan estas condiciones, el reporte puede actualizarse y la conclusión funcional revisarse a **READY** con las reservas que se indiquen (ej. solo para entorno X, o condicionado a pruebas de regresión en próximos despliegues).

---

## Referencias

| Documento | Uso |
|-----------|-----|
| docs/QA/00_SYSTEM_FUNCTIONAL_OVERVIEW.md | Comprensión funcional y mapa de endpoints/roles. |
| docs/QA/00_BASELINE_CONFIRMATION.md | Confirmación de endpoints y roles contra código. |
| docs/QA/01_TEST_MATRIX.md | Matriz de 51 casos (TC-001 a TC-051). |
| docs/QA/02_HOW_TO_RUN.md | Pasos para ejecutar colección y environment. |
| docs/QA/03_EXECUTION_RESULTS.md | Resultados de ejecución (plantilla / evidencia). |
| tests/FUNCTIONAL_E2E/postman/ | Colección y environment Postman. |
| docs/QA_FUNCTIONAL_REPORT.md | Hallazgos previos y score. |
| docs/AUDIT/FINAL_REPORT.md | Hallazgos de auditoría (H01–H12). |
