# FixHub — Cómo ejecutar pruebas funcionales E2E

**Artefactos:** Colección Postman + Environment en `tests/FUNCTIONAL_E2E/postman/`.  
**Prefijo datos:** `FUNC_<timestamp>`.  
**Entorno:** Solo local / SIT / QA (prohibido producción).

---

## 1. Requisitos previos

- **API FixHub** levantada y accesible (ej. `http://localhost:5100` en desarrollo).
- **Postman** o **Newman** (CLI) instalado.
- **Credenciales Admin** para flujos que usan Login Admin (seed o usuario creado con Role=Admin): email y contraseña.  
  - Si usas registro con Role=3 (bug H03), el usuario creado en "Register Admin" puede usarse para Login Admin en la misma ejecución.

---

## 2. Ubicación de artefactos

| Artefacto | Ruta |
|-----------|------|
| Colección | `tests/FUNCTIONAL_E2E/postman/FixHub_Functional_E2E.postman_collection.json` |
| Environment | `tests/FUNCTIONAL_E2E/postman/FixHub_ENV.postman_environment.json` |

---

## 3. Configurar environment

1. En Postman: **Import** → seleccionar `FixHub_ENV.postman_environment.json`.
2. Editar el environment **FixHub FUNC_ E2E (local/SIT/QA)**:
   - **baseUrl:** URL de la API (ej. `http://localhost:5100` para local, o la URL de SIT/QA).
   - **adminEmail** y **adminPassword:** credenciales de un usuario Admin (necesarias para carpetas 03_ADMIN y 05_END2END si se usa Login Admin desde seed).

Si no tienes Admin, puedes ejecutar primero "Register Admin - BUG H03 (TC-030)" en 01_AUTH (el sistema actualmente crea el usuario con Role=Admin) y usar ese email/password para el resto, o setear después en el environment el email del usuario registrado.

---

## 4. Ejecutar en Postman (GUI)

1. **Import** la colección `FixHub_Functional_E2E.postman_collection.json`.
2. Seleccionar el environment **FixHub FUNC_ E2E (local/SIT/QA)** en el desplegable superior derecho.
3. **Orden recomendado** (las variables se rellenan en cadena):
   - **01_AUTH:** Health, Register Customer, Login Customer, Register Technician, (opcional Register Admin).
   - **02_CUSTOMER:** Create Job (guarda `jobId`), List My Jobs, Get Job, Get Proposals, etc.  
     - Si quieres probar Cancel (TC-010), ejecuta Cancel antes de Accept Proposal. Para flujo completo no canceles.
   - **04_TECHNICIAN:** Login Technician, List Jobs, Submit Proposal (guarda `proposalId`), My Assignments, Start Job.
   - **03_ADMIN:** Login Admin (si no lo hiciste por seed), Dashboard, List Jobs, Get Proposals, **Accept Proposal**, Admin Start Job, Admin Update Status, etc.
   - **05_END2END:** Secuencia completa (crear job, proposal, accept, start, complete, review). Requiere tener ya Customer y Technician registrados y Admin logueado; puede usar un `jobId` nuevo creado en E2E o reutilizar uno de 02_CUSTOMER.
   - **06_NEGATIVE:** Casos que esperan 403/400. Algunos dependen de tener `customer2Token` (registrar Customer 2 en 01_AUTH y hacer Login Customer 2 si existe en la colección) o de que el job no esté asignado al técnico que hace Start.

4. Para **guardar evidencia**: en cada request, revisar la pestaña **Test Results** y la respuesta (Status, Body). Exportar resultados o capturas según política de evidencia (ver docs/QA/01_TEST_MATRIX.md).

---

## 5. Ejecutar con Newman (CLI)

Desde la raíz del repositorio:

```bash
# Instalar Newman (una vez)
npm install -g newman

# Ejecutar colección con environment
newman run tests/FUNCTIONAL_E2E/postman/FixHub_Functional_E2E.postman_collection.json \
  -e tests/FUNCTIONAL_E2E/postman/FixHub_ENV.postman_environment.json \
  --reporters cli,json \
  --reporter-json-export docs/QA/evidence/newman_run.json
```

Ajustar `baseUrl` en el environment si la API no está en `http://localhost:5100`. Para SIT/QA, crear un segundo environment (ej. `FixHub_ENV_SIT.json`) con la URL correspondiente y pasarlo con `-e`.

**Reporte JSON:** opcionalmente guardar en `docs/QA/evidence/` (crear la carpeta si no existe) para evidencia automatizada.

---

## 6. Levantar API en local (desarrollo)

Si usas el proyecto desde código:

```powershell
cd c:\Proyectos\FixHub
dotnet run --project src/FixHub.API/FixHub.API.csproj
```

La API suele quedar en `http://localhost:5100` (revisar `launchSettings.json`). Asegurar que PostgreSQL esté corriendo y las migraciones aplicadas (o usar docker-compose según docs del proyecto).

---

## 7. Datos de prueba y limpieza

- **Prefijo:** Todas las pruebas que crean datos usan `FUNC_<timestamp>` en títulos, emails o notas (ej. `FUNC_1730123456_cust@test.local`). Así se distinguen en BD de otros entornos.
- **Limpieza:** No se hacen deletes destructivos desde la colección. Para resetear:
  - Usar una BD de pruebas dedicada y restaurar snapshot, o
  - Ejecutar migraciones en blanco en un entorno aislado para cada batería.

---

## 8. Requisitos del entorno (resumen)

Para **ejecutar** las pruebas hace falta al menos una de estas opciones:

- **Opción A — Postman/Newman:** API accesible (ej. `http://localhost:5100`), **Node.js** instalado para usar `npx newman run ...`, y PostgreSQL disponible para la API.
- **Opción B — Tests de integración:** **Docker** en ejecución y correctamente configurado (Testcontainers levanta PostgreSQL automáticamente). Comando: `dotnet test tests/FixHub.IntegrationTests/FixHub.IntegrationTests.csproj`.

Si Docker no está corriendo, los integration tests fallan con: `Docker is either not running or misconfigured`. Si Node/npx no está en el PATH, no se puede ejecutar Newman desde la CLI.

---

## 9. Evidencia requerida por caso

Para cada TC, documentar (en 03_EXECUTION_RESULTS.md o anexo):

- **Endpoint** y método.
- **Request** (cuerpo redactado: sin contraseñas, tokens truncados).
- **Response** (status code, cuerpo resumido o relevante).
- **Timestamp** de ejecución.
- **PASS/FAIL** y, si falla, pasos exactos para reproducir (BUG).

Ver matriz completa en `docs/QA/01_TEST_MATRIX.md`.
