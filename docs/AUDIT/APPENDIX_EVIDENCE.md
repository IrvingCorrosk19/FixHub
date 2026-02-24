# Appendix A — Evidence Logs

**Documento:** Evidencia de auditoría para FINAL_REPORT.md (estándar Big4/ISO 27001).  
**Branch:** `audit/fixhub-100`  
**Uso:** Registro auditable de resultados de secret scan, pruebas IDOR y escalación de privilegios. Valores sensibles **REDACTED**.

---

## A.1 Evidencia de Secret Scan (Gitleaks / Trivy / CodeQL)

### A.1.1 Estado actual

El pipeline CI (`.github/workflows/ci.yml`) incluye jobs **secrets-scan** (Gitleaks) y **container-scan** (Trivy); **CodeQL** para SAST. Las evidencias que se obtienen al ejecutar el workflow en GitHub Actions son las siguientes.

### A.1.2 Cómo obtener la evidencia

| Herramienta | Dónde se genera | Cómo obtener la evidencia |
|-------------|-----------------|----------------------------|
| **Gitleaks** | Job `secrets-scan` en GitHub Actions | 1) Ir a *Actions* → workflow *CI* → run del commit deseado. 2) Abrir el job *Secrets scan (Gitleaks)*. 3) Revisar la salida del step *Run Gitleaks*: si hay hallazgos, aparecerán en el log (no incluir valores reales en este documento). 4) Para evidencia auditable: captura de pantalla del job en verde (0 findings) o export del log con hallazgos redactados. |
| **Trivy** | Job `container-scan` en GitHub Actions | 1) Ir al mismo run de *CI*. 2) Abrir el job *Container scan (Trivy)*. 3) El step *Run Trivy* genera `trivy-api-results.sarif`; si está configurado upload a Code Scanning, los resultados aparecen en *Security* → *Code scanning*. 4) Para evidencia: captura del job o del dashboard de Code scanning (sin pegar detalles sensibles de vulnerabilidades si las hubiera). |
| **CodeQL** | Job `codeql` en GitHub Actions | 1) En el mismo run, job *CodeQL (SAST)*. 2) Resultados en *Security* → *Code scanning* (alertas por código). 3) Evidencia: captura que muestre 0 alertas críticas/altas o lista de alertas con estado (open/fixed). |

### A.1.3 Placeholder — Salida de secret scan (cuando CI no se ha ejecutado)

Si el workflow **no se ha ejecutado** aún (por ejemplo, el branch no está en GitHub o el run falla por entorno), usar este placeholder como registro de “evidencia pendiente”:

```
[PLACEHOLDER — Secret scan evidence]

- Gitleaks:  Run the CI workflow (push to branch audit/fixhub-100 or main with .github/workflows/ci.yml).
             Job "Secrets scan (Gitleaks)" → copy the step output (redact any real secrets if present).
             Expected after remediation: "No leaks found" or zero findings.

- Trivy:    Job "Container scan (Trivy)" → output in runner or in Security → Code scanning.
             Attach screenshot or SARIF summary (e.g. 0 CRITICAL, 0 HIGH) for evidence log.

- CodeQL:    Job "CodeQL (SAST)" → Security → Code scanning alerts.
             Evidence: screenshot or export showing C# analysis completed and open critical/high count.
```

**Criterio de cierre:** Tras remediación de H01/H02/H11, un run de CI con Gitleaks sin hallazgos (o con excepciones documentadas) constituye la evidencia de cierre para secretos en repo.

---

## A.2 Evidencia de pruebas IDOR (403 — redactado)

Se documentan **3 escenarios** donde un actor no autorizado intenta acceder a un recurso ajeno y debe recibir **403 Forbidden**. Request/response están **redactados** (sin tokens ni IDs reales).

### A.2.1 IDOR 1 — Customer intenta ver job de otro Customer

| Campo | Valor (redactado) |
|-------|-------------------|
| **Prueba** | Customer B llama GET job creado por Customer A. |
| **Request** | `GET /api/v1/jobs/{jobId_de_A} HTTP/1.1` |
| **Headers** | `Authorization: Bearer <REDACTED_token_Customer_B>` |
| **Response esperada** | `403 Forbidden` |
| **Body de respuesta (ejemplo)** | `{"title":"Forbidden","status":403,"detail":"Access denied to this job.","errorCode":"FORBIDDEN"}` (o equivalente ProblemDetails) |
| **Evidencia** | Test de integración: `Customer_Cannot_View_Other_Customers_Job_Returns_403` (ApiIntegrationTests.cs). Para evidencia manual: captura de Postman/curl con status 403 y body redactado. |

### A.2.2 IDOR 2 — Technician intenta ver job no asignado a él

| Campo | Valor (redactado) |
|-------|-------------------|
| **Prueba** | Technician T2 llama GET job que está asignado a Technician T1 (T2 no tiene propuesta ni asignación). |
| **Request** | `GET /api/v1/jobs/{jobId_asignado_a_T1} HTTP/1.1` |
| **Headers** | `Authorization: Bearer <REDACTED_token_Technician_T2>` |
| **Response esperada** | `403 Forbidden` |
| **Body de respuesta (ejemplo)** | `{"title":"Forbidden","status":403,"detail":"Access denied to this job.","errorCode":"FORBIDDEN"}` |
| **Evidencia** | Test de integración: `Technician_Cannot_View_Unassigned_Job_Returns_403`. Evidencia manual: captura con 403 y body redactado. |

### A.2.3 IDOR 3 — Customer intenta completar job de otro Customer

| Campo | Valor (redactado) |
|-------|-------------------|
| **Prueba** | Customer B llama POST complete sobre job creado y asignado por Customer A. |
| **Request** | `POST /api/v1/jobs/{jobId_de_A}/complete HTTP/1.1` |
| **Headers** | `Authorization: Bearer <REDACTED_token_Customer_B>` |
| **Body** | (vacío o según API) |
| **Response esperada** | `403 Forbidden` |
| **Body de respuesta (ejemplo)** | `{"title":"Forbidden","status":403,"detail":"...","errorCode":"FORBIDDEN"}` |
| **Evidencia** | Test de integración: `Customer_Cannot_Complete_Other_Customers_Job_Returns_403`. Evidencia manual: captura con 403 redactado. |

*Para el cierre auditable, adjuntar capturas o exports de los 3 escenarios con status 403 y sin exponer tokens ni IDs sensibles.*

---

## A.3 Evidencia de test de escalación de privilegios (Register Role=Admin → 400/403)

El registro con **Role=Admin** debe ser rechazado por la API (400 Bad Request o 403 Forbidden) con un mensaje o código identificable.

### A.3.1 Request / Response (redactado)

| Campo | Valor (redactado) |
|-------|-------------------|
| **Prueba** | POST register con `role: 3` (Admin). |
| **Request** | `POST /api/v1/auth/register HTTP/1.1` |
| **Headers** | `Content-Type: application/json` |
| **Body** | `{"fullName":"<REDACTED>","email":"<REDACTED>@audit.local","password":"<REDACTED>","role":3,"phone":null}` |
| **Response esperada (tras fix H03)** | `400 Bad Request` o `403 Forbidden` |
| **Body de respuesta (ejemplo deseado)** | `{"title":"Validation failed","status":400,"errors":{"Role":["Administrators cannot be registered via this endpoint."]}}` o similar que indique rechazo explícito al rol Admin. |
| **Estado actual (pre-remediación)** | La API acepta el request y devuelve **201 Created** con token; el usuario queda registrado como Admin. Esto es el hallazgo H03. |

### A.3.2 Criterio de aceptación para cierre

- Tras implementar el fix (RegisterCommandValidator o handler): al menos un test de integración que envíe `role: 3` y aserte status 400 (o 403) y que el usuario no exista en BD con rol Admin.
- Evidencia en este appendix: captura o log redactado mostrando **status 400/403** y un mensaje/código que identifique el rechazo al rol Admin.

*No incluir en este documento: emails reales, contraseñas ni tokens.*

---

*Appendix A — Evidence Logs. Referenciado desde FINAL_REPORT.md. Mantener actualizado con evidencias reales cuando se ejecuten CI y pruebas de seguridad.*
