# FixHub — Top 10 mejoras de performance (Fase 3)

**Branch:** `audit/fixhub-100`  
**Alcance:** Hallazgos de rendimiento por revisión estática. Sin profiling en ejecución.

---

## 1. GetJobQuery: evitar cargar todas las Proposals

**Archivo:** `src/FixHub.Application/Features/Jobs/GetJobQuery.cs` líneas 22–27, 58.

**Problema:** Se hace `.Include(j => j.Proposals)` y luego se usa `job.Proposals.Any(p => p.TechnicianId == req.RequesterId)` solo para un booleano. Para jobs con muchas propuestas se carga toda la colección.

**Mejora:** Eliminar `.Include(j => j.Proposals)`. Para el rol Technician, antes del return comprobar con:
`await db.Proposals.AnyAsync(p => p.JobId == req.JobId && p.TechnicianId == req.RequesterId, ct)`.

**Impacto:** Menor uso de memoria y menos datos transferidos desde BD.

---

## 2. ListJobsQuery (Technician): subquery Proposals

**Archivo:** `src/FixHub.Application/Features/Jobs/ListJobsQuery.cs` líneas 52–56.

**Estado:** La condición `j.Proposals.Any(p => p.TechnicianId == techId)` se traduce a EXISTS en SQL; no hay N+1. **Sin cambio necesario.** Verificar con EXPLAIN que exista índice en (JobId, TechnicianId) en proposals — ya existe (ProposalConfiguration).

---

## 3. GetOpsDashboardQuery: múltiples round-trips

**Archivo:** `src/FixHub.Application/Features/Admin/GetOpsDashboardQuery.cs`.

**Problema:** Varias consultas secuenciales (todayJobs, issuesLast24h, assignmentPairs, alerts, recentJobs, recentIssues). Aumenta latencia total.

**Mejora:** Valorar combinar en menos round-trips (raw SQL con CTEs o múltiples consultas en paralelo con Task.WhenAll donde no compartan contexto). Medir antes/después.

---

## 4. Paginación: límite máximo PageSize

**Archivo:** Varios handlers (ListJobsQuery, ListMyJobsQuery, GetMyNotificationsQuery, ListApplicantsQuery, ListJobIssuesQuery, GetMyAssignmentsQuery).

**Estado:** ListJobsQuery tiene validator PageSize 1–100. Otros usan default 20 y parámetro desde query. **Recomendación:** Asegurar validación FluentValidation con máximo (ej. 100) en todos los listados paginados para evitar pageSize=10000.

---

## 5. GetJobProposalsQuery: orden y Take

**Archivo:** `src/FixHub.Application/Features/Proposals/GetJobProposalsQuery.cs`.

**Estado:** OrderByDescending(p => p.CreatedAt) y ToListAsync sin Take. Para jobs con muchas propuestas podría ser pesado. **Mejora:** Valorar paginación o límite máximo (ej. 100) y/o índice (JobId, CreatedAt) — ya existe índice en JobId.

---

## 6. Notificaciones: índice (UserId, IsRead, CreatedAt)

**Archivo:** NotificationConfiguration.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt }).

**Estado:** Índice compuesto adecuado para listado y unread-count. **Sin cambio.** Mantener.

---

## 7. Dashboard cache

**Archivo:** DashboardCachingBehavior / GetOpsDashboardQuery.

**Estado:** Existe caché para GetOpsDashboardQuery (DashboardCachingBehavior). Invalidación vía IDashboardCacheInvalidator. **Sin cambio.** Revisar TTL y estrategia de invalidación si el dashboard crece.

---

## 8. Outbox: batch size y frecuencia

**Archivo:** OutboxEmailSenderHostedService.

**Estado:** BatchSize fijo; ejecución periódica. Ajustar BatchSize y intervalo según carga para no saturar SendGrid ni dejar cola creciendo.

---

## 9. N+1 en mapeos a DTO

**Archivo:** Varios handlers que hacen ToListAsync y luego Select(x => x.ToDto(...)).

**Estado:** Los ToDto suelen usar datos ya cargados por Include. No se detectó N+1 evidente en los flujos revisados. En cualquier nuevo endpoint, evitar iterar navegaciones no incluidas.

---

## 10. Connection pooling y DbContext lifetime

**Estado:** DbContext registrado como Scoped; conexiones manejadas por Npgsql (pool por defecto). **Sin cambio.** En alta concurrencia, monitorizar pool y timeouts.

---

## Resumen priorizado

| # | Prioridad | Acción |
|---|-----------|--------|
| 1 | Alta | GetJobQuery: quitar Include(Proposals), usar AnyAsync. |
| 2 | Baja | ListJobsQuery: ya correcto; verificar índices en producción. |
| 3 | Media | GetOpsDashboardQuery: valorar paralelismo o menos round-trips. |
| 4 | Media | Validar PageSize máximo en todos los listados paginados. |
| 5 | Baja | GetJobProposalsQuery: valorar límite o paginación. |
| 6–10 | Mantener / monitorear | Índices, caché, outbox, pooling. |
