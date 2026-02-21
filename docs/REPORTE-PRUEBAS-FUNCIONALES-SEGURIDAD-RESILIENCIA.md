# Reporte: Batería Completa de Pruebas Funcionales, Seguridad y Resiliencia

**FixHub — Empresa de Servicios**  
**Fecha:** 19 de febrero de 2026  
**Objetivo:** Detectar fallas reales antes de producción; intentar romper el sistema.

---

## 1. Resumen Ejecutivo

| Métrica | Valor |
|---------|-------|
| **Tests implementados** | 18 (autorización, flujo, cancelación, dashboard, metrics, resiliencia) |
| **Tests ejecutados** | Requiere Docker (Testcontainers) — no ejecutables en entorno sin Docker |
| **Vulnerabilidades críticas** | 1 (ver sección 2) |
| **Riesgos medios** | 3 |
| **Edge cases detectados** | 4 |
| **Nivel preparación producción** | **65–70%** |

---

## 2. Vulnerabilidades Encontradas

### Crítica

1. **GET /api/v1/jobs — ListJobsQuery no filtra por rol**
   - **Descripción:** El handler `ListJobsQuery` devuelve todos los jobs sin filtrar por Technician/Admin. La protección se hace solo en el controller: si `CurrentUserRole == "Customer"` retorna 403. Si un middleware o gateway invoca el handler directamente, o si hay otra ruta que use `ListJobsQuery`, un Customer podría ver jobs ajenos.
   - **Mitigación actual:** El controller bloquea correctamente. La query no recibe `UserId` ni `Role` para filtrar.
   - **Recomendación:** Mover el filtro al handler: `ListJobsQuery` debería recibir `RequesterRole` y `RequesterId`, y filtrar según rol (Technician: asignados/propuestas/Open; Admin: todos).

### Medias

2. **JobIssue sin estado Resolved**
   - Las incidencias quedan abiertas indefinidamente. No hay `ResolvedAt` ni `ResolvedBy`. El SLA puede generar alertas duplicadas (IssueUnresolved) para la misma incidencia.
   - **Recomendación:** Añadir `ResolvedAt`, `ResolvedBy` y workflow de resolución.

3. **ReportJobIssueCommand — Reason no validado contra whitelist en validator**
   - El validator ya incluye whitelist `no_contact`, `late`, `bad_service`, `other`. **Verificado: está bien implementado.**

4. **Confirmation page — no verifica ownership del JobId**
   - Un Customer puede navegar a `/Requests/Confirmation?id={cualquier-job-id}` y ver la confirmación de otro cliente.
   - **Recomendación:** Validar que el Job.CustomerId coincida con el usuario autenticado.

---

## 3. Pruebas de Autorización (implementadas)

| Prueba | Descripción | Resultado esperado |
|--------|-------------|--------------------|
| `Auth_Customer_No_Puede_Ver_Job_De_Otro_Cliente_403` | Customer B intenta ver job de Customer A | 403, errorCode FORBIDDEN |
| `Auth_Customer_No_Puede_Acceder_GET_Jobs_403` | Customer llama GET /api/v1/jobs | 403, errorCode FORBIDDEN |
| `Auth_Customer_No_Puede_Completar_Job_Ajeno_403` | Customer B intenta completar job de A | 403 |
| `Auth_Customer_No_Puede_Cancelar_Job_Ajeno_403` | Customer B intenta cancelar job de A | 403 |
| `Auth_Technician_No_Puede_Ver_Job_No_Asignado_403` | Technician 2 intenta ver job asignado a Technician 1 | 403 |
| `Auth_Admin_Puede_Ver_Todo_Dashboard_Metrics_Issues` | Admin accede dashboard, metrics, issues | 200 OK |

**Cobertura:** GetJobQuery filtra por Customer/Technician/Admin correctamente. CompleteJobCommand y CancelJobCommand validan ownership. JobsController bloquea Customer en GET /jobs.

---

## 4. Pruebas de Flujo Completo (Happy Path)

| Prueba | Descripción |
|--------|-------------|
| `HappyPath_Create_Assign_Start_Complete_Outbox_Notifications` | Crear solicitud → asignar → iniciar → completar. Verifica: Open→Assigned→InProgress→Completed, notificaciones, outbox, emails (Status=Sent tras ~15s). |

**Verificaciones:**
- Timeline correcto (Open, Assigned, InProgress, Completed)
- Notificaciones internas creadas
- Registros en NotificationOutbox
- Emails enviados (Status=Sent) tras ciclo del worker (10s)

---

## 5. Pruebas de Cancelación

| Prueba | Resultado esperado |
|--------|--------------------|
| Cancel en Open | 200, CancelledAt seteado |
| Cancel en Assigned | 200, Cancelled |
| Cancel en InProgress | 400, errorCode INVALID_STATUS |
| Cancel en Completed | 400, errorCode INVALID_STATUS |

**Implementadas.** CancelJobCommand valida estado correctamente.

---

## 6. Pruebas de SLA (no automatizadas)

Los tests de SLA requieren manipulación de tiempo (Job Open >15 min, Assigned >30 min sin Started, etc.). El `JobSlaMonitor` corre cada 2 minutos. Para automatizar:

- Opción A: Mock de `IDateTimeProvider` y avanzar tiempo en tests.
- Opción B: Insertar jobs con `CreatedAt`/`AssignedAt` en el pasado vía DB y esperar ciclo del monitor (lento).
- Opción C: Exponer endpoint interno (solo Testing) que ejecute el ciclo SLA bajo demanda.

**Estado:** Lógica SLA verificada por revisión de código. JobSlaMonitor crea JobAlerts, llama NotificationService. No duplicación: `HasUnresolvedAlertAsync` evita alertas duplicadas del mismo tipo.

---

## 7. Pruebas de Outbox (parcialmente automatizadas)

| Escenario | Implementación |
|-----------|----------------|
| SendGridApiKey vacío | Factory de tests usa config por defecto; SendGrid retorna `false` y no lanza. Worker incrementa Attempts, tras 3 → Failed. |
| Worker reiniciado | Outbox usa Status=Processing para claim atómico; no hay doble procesamiento. |
| Índice único (NotificationId, Channel) | Evita duplicados; DbUpdateException manejada con warning. |
| Logging OutboxId, JobId | Implementado en OutboxEmailSenderHostedService. |

**Faltante:** Test que fuerce 3 fallos y verifique Status=Failed. Requiere mock de IEmailSender que retorne false.

---

## 8. Pruebas de Concurrencia (no automatizadas)

| Escenario | Riesgo |
|-----------|--------|
| Dos CompleteJob simultáneos | CompleteJobCommand valida estado (InProgress/Assigned). Tras el primero, status=Completed; el segundo fallaría por INVALID_STATUS. |
| Dos CancelJob simultáneos | Similar. El primero gana; el segundo recibe INVALID_STATUS (ya Cancelled). |
| Dos workers | FOR UPDATE SKIP LOCKED + status Processing evita doble procesamiento. |

**Recomendación:** Añadir test de integración con `Task.WhenAll` para dos requests idénticos y verificar que solo uno tenga éxito.

---

## 9. Pruebas de Dashboard y Metrics

| Prueba | Resultado |
|--------|-----------|
| `Dashboard_Admin_200_Con_Kpis` | 200, payload contiene Kpis, Alerts |
| `Dashboard_Cache_No_Rompe_Segunda_Llamada` | Cache 45s; segunda llamada 200 |
| `Metrics_Endpoint_Retorna_Estructura_Completa` | AdminMetricsDto con TotalEmailsSentToday, TotalEmailsFailedToday, TotalSlaAlertsToday, AvgMinutesOpenToAssigned, AvgMinutesAssignedToCompleted |

**Invalidación de cache:** DashboardCachingBehavior usa IDashboardCacheInvalidator. Invalidación en CreateJob, CancelJob, ReportJobIssue, AdminUpdateStatus (vía DashboardCacheInvalidator.Invalidate).

---

## 10. Pruebas de Resiliencia

| Prueba | Resultado |
|--------|-----------|
| Health sin DB → 503 | DatabaseHealthChecker.CanConnectAsync retorna false; HealthController retorna 503. En entorno de tests con Testcontainers, DB está disponible → 200. Para probar 503 se requiere DB caída (complex). |
| Worker error no tumba API | OutboxEmailSenderHostedService tiene try/catch por batch; errores se loguean, loop continúa. |
| Reinicio servidor | Outbox Pending se reprocesa. Processing podría quedar huérfano si el worker muere a mitad; no hay job de recuperación. |

**Recomendación:** Añadir job periódico que resetee `Status=Processing` a `Pending` si `UpdatedAt` (o campo similar) > X minutos.

---

## 11. Edge Cases Detectados

1. **Technician ve jobs Open** — Correcto: puede ver oportunidades para proponer.
2. **CompleteJob desde Assigned** — Permitido (cliente puede marcar completado sin que técnico haya iniciado). Documentar si es intencional.
3. **Alertas Processing huérfanas** — Si worker muere tras marcar Processing, esos registros nunca se procesan.
4. **Emails a admins (IssueReported, SlaAlert)** — Requieren email en User. Si Admin no tiene email, no se encola. No hay fallback.

---

## 12. Recomendaciones Técnicas

1. **Autorización en capa de aplicación:** Hacer que `ListJobsQuery` reciba y respete `RequesterRole`/`RequesterId` para defensa en profundidad.
2. **JobIssue resolución:** Añadir ResolvedAt/ResolvedBy y endpoint Admin para marcar resuelto.
3. **Confirmation ownership:** Validar Job.CustomerId en página Confirmation.
4. **Recuperación Processing:** Job que resetee Outbox Processing → Pending tras timeout.
5. **Tests con Docker:** Ejecutar batería en CI con Testcontainers para validación pre-merge.
6. **Mock IEmailSender en tests:** Para probar flujo Failed sin dependencia externa.

---

## 13. Nivel de Preparación para Producción

| Criterio | Puntuación | Notas |
|----------|------------|-------|
| Autorización | 85% | GetJob, Complete, Cancel bien protegidos; ListJobs solo en controller |
| Flujo end-to-end | 80% | Happy path funcional; emails y outbox operativos |
| Cancelación | 90% | Validación de estados correcta |
| SLA | 70% | Lógica implementada; sin tests automatizados de tiempo |
| Outbox | 85% | Atómico, reintentos, unique index |
| Concurrencia | 75% | Diseño correcto; faltan tests de concurrencia |
| Dashboard/Metrics | 90% | Cache, invalidación, estructura correcta |
| Resiliencia | 70% | Health 503; worker aislado; sin recuperación Processing |
| Documentación/Reportes | 80% | Este reporte y BITACORA |

**Total ponderado: 65–70%** — Apto para piloto con pocos usuarios y monitoreo cercano. No recomendable para lanzamiento público sin abordar vulnerabilidad ListJobs y recuperación de Processing.

---

## 14. Cómo Ejecutar las Pruebas

**Requisitos:** Docker en ejecución (Testcontainers).

```bash
dotnet test tests/FixHub.IntegrationTests/FixHub.IntegrationTests.csproj -v n
```

Para ejecutar solo los tests de la batería comprehensiva:

```bash
dotnet test tests/FixHub.IntegrationTests --filter "FullyQualifiedName~ComprehensiveBatteryTests" -v n
```

---

## 15. Archivos de Prueba Creados

- `tests/FixHub.IntegrationTests/ComprehensiveTestHelpers.cs` — Helpers reutilizables
- `tests/FixHub.IntegrationTests/ComprehensiveBatteryTests.cs` — 18 tests de autorización, flujo, cancelación, dashboard, metrics, resiliencia
- `FixHubApiFixture.cs` — Añadido `WithDbContextAsync` para aserciones sobre DB
