# FixHub — Matriz OWASP Top 10 vs Estado (Fase 2)

**Branch:** `audit/fixhub-100`  
**Referencia:** OWASP Top 10 (2021) aplicado a API REST y configuración.

---

## Tabla OWASP Top 10 vs Estado

| # | Categoría OWASP | Estado | Evidencia / Observaciones |
|---|------------------|--------|----------------------------|
| A01 | **Broken Access Control** | 🟠 Parcial | IDOR en GetJobProposals (Customer sin ownership check). Resto de endpoints con ownership (GetJob, Complete, Cancel, Start, MarkNotificationRead) correctos. Admin por política. |
| A02 | **Cryptographic Failures** | 🟡 Revisar | Contraseñas con BCrypt (work factor 12). JWT con HMAC; secret mínimo 32 chars. Riesgo: secretos en repo (appsettings.Development, script deploy) — rotar y usar secret store. |
| A03 | **Injection** | 🟢 OK | EF Core parametrizado; único raw SQL en Outbox (FromSqlRaw con parámetro numérico BatchSize). Sin concatenación de entrada de usuario. |
| A04 | **Insecure Design** | 🟠 Revisar | Registro permite Role=Admin (escalación de privilegios). Diseño debe impedir registro de Admin por API pública. |
| A05 | **Security Misconfiguration** | 🟠 Revisar | Security headers presentes (X-Frame-Options, X-Content-Type-Options, etc.). Secretos en archivos versionados; Swagger en Development only. |
| A06 | **Vulnerable and Outdated Components** | 🟡 Revisar | No ejecutado dependency scan en esta fase; pipeline CI (Fase 4) incluye dependency scan. |
| A07 | **Identification and Authentication Failures** | 🟠 Revisar | JWT y políticas por rol correctos. Rate limit en Auth (10 req/min). Contraseña por defecto admin en seed documentada — debe cambiarse en prod. |
| A08 | **Software and Data Integrity Failures** | 🟢 OK | JWT firma validada; no se detectó deserialización insegura. |
| A09 | **Security Logging and Monitoring Failures** | 🟡 Revisar | RequestLogging, CorrelationId, RequestContextLogging (UserId/JobId). Revisar que no se logueen contraseñas o tokens (auditoría estática Fase 3). |
| A10 | **Server-Side Request Forgery (SSRF)** | 🟢 N/A | No se identifican flujos que tomen URL de usuario y hagan request a terceros. |

**Leyenda:** 🟢 OK / N/A | 🟡 Revisar / Mejora | 🟠 Riesgo / Parcial | 🔴 Crítico

---

## Resumen por prioridad

| Prioridad | OWASP | Acción |
|-----------|-------|--------|
| Crítico / Alto | A01, A04, A05, A07 | Corregir Register (no Admin); ownership en GetJobProposals; eliminar secretos de repo; rotar credenciales; política admin (cambio de password en prod). |
| Medio | A02, A06, A09 | Secret store; dependency scan en CI; revisar logs sensibles. |
| Bajo / OK | A03, A08, A10 | Mantener; único raw SQL documentado y parametrizado. |
