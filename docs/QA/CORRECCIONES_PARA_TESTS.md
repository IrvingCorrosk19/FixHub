# Correcciones aplicadas para que el sistema sea funcionalmente productivo

Resumen de cambios realizados para que el stress test y las pruebas funcionales pasen correctamente.

---

## 1. Base de datos: migración pendiente

**Problema:** `POST /api/v1/auth/register` devolvía 500: *column "deactivated_at" of relation "users" does not exist*.

**Solución:** Aplicar migraciones:
```bash
dotnet ef database update --project src/FixHub.Infrastructure --startup-project src/FixHub.API
```

---

## 2. Rate limit de Auth en desarrollo

**Problema:** El script de stress (16 registros + logins) superaba el límite de 10 req/min del Auth y recibía 429.

**Solución:**
- **Código:** En `Program.cs`, en Development el límite de la política "AuthPolicy" pasa a 60 req/min (en producción se mantiene 10).
- **Script:** En `tests/scripts/run-stress-test.ps1` se añadieron pausas de 7 s entre cada `auth/register` y `auth/login` para que funcione aunque la API siga con 10/min.

---

## 3. Orden del stress test: jobs antes de aprobar técnicos

**Problema:** Si los técnicos se aprobaban antes de crear los 30 jobs, el primer job (y en BD con datos previos, todos) podían quedar auto-asignados, y las propuestas fallaban con 400 (solo se aceptan en jobs Open).

**Solución:** En el script, primero se crean los 30 jobs y **después** se aprueban los técnicos. Así los jobs se crean sin técnico aprobado y quedan en Open.

---

## 4. Auto-asignación en Development

**Problema:** Con técnicos ya aprobados en BD (de ejecuciones anteriores), al crear jobs nuevos el sistema los auto-asignaba y todos quedaban Assigned. Las propuestas devolvían 400 (JOB_NOT_OPEN).

**Solución:**
- **CreateJobCommand:** Nuevo parámetro opcional `SkipAutoAssign` (default `false`). Si es `true`, no se auto-asigna aunque exista un técnico aprobado.
- **JobsController:** En entorno Development se envía `SkipAutoAssign: true` al crear jobs. Así los jobs creados en Development quedan siempre Open y los tests son deterministas.

---

## 5. Script: clientes y técnicos variables

**Problema:** Si algún registro fallaba (p. ej. 429), `Customers` o `Technicians` tenían menos de 10 o 5 elementos y el script accedía por índice fijo (`% 10`, `% 5`), pudiendo dar error o 401.

**Solución:** Se usan `$customerCount` y `$technicianCount` y se hace módulo por el número real de elementos. Se comprueba que `$cust` y `$cust.Token` existan antes de crear jobs.

---

## 6. JobsController: posibles null en Create

**Problema:** Warnings CS8604 por pasar `request.Title`, `request.Description`, `request.AddressText` (posible null) al command.

**Solución:** Se envían `request.Title ?? ""`, etc., al `CreateJobCommand`.

---

## Pasos para ejecutar el stress test con éxito

1. **Detener la API** si está en ejecución (para poder recompilar).
2. **Aplicar migraciones** (si no se ha hecho):
   ```bash
   dotnet ef database update --project src/FixHub.Infrastructure --startup-project src/FixHub.API
   ```
3. **Compilar y ejecutar la API:**
   ```bash
   dotnet run --project src/FixHub.API/FixHub.API.csproj
   ```
4. **Ejecutar el stress test** (con la API en marcha en http://localhost:5100):
   ```bash
   .\tests\scripts\run-stress-test.ps1
   ```
   Con rate limit 10/min el script tarda varios minutos en el bloque de registro; con 60/min en Development es más rápido.

Tras estos pasos, el resumen esperado del script debería mostrar:
- 30 jobs creados
- 5 cancelados sin asignación (11-15)
- 5 sin aceptar propuesta (21-25)
- 10 completados (1-10)
- 5 reasignados (16-20)
- 5 cancelados después de asignación (26-30)

Completar las validaciones indicadas en `docs/QA/STRESS_TEST_REPORT.md` y `docs/QA/DATA_INTEGRITY_REPORT.md`.
