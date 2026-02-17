# FASE 5.1 y 5.2 — Hardening (Rate Limiting, Security Headers, CORS)

## Archivos modificados

| Proyecto   | Archivo | Cambio |
|-----------|---------|--------|
| FixHub.API | `Program.cs` | Rate limiting, ForwardedHeaders, CORS (Dev = localhost:5200), pipeline con SecurityHeaders y UseRateLimiter |
| FixHub.API | `Middleware/SecurityHeadersMiddleware.cs` | **Nuevo**: headers X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy |
| FixHub.API | `Controllers/v1/AuthController.cs` | `[EnableRateLimiting("AuthPolicy")]` para 10 req/min en login/register |
| FixHub.Web | `Program.cs` | Cookie SecurePolicy: Development = SameAsRequest, Production = Always |

---

## Comandos para probar

### 1. Verificar headers de seguridad (API)

Con la API en marcha (por ejemplo en `https://localhost:5100` o `http://localhost:5100`):

```bash
curl -sI -k "https://localhost:5100/api/v1/health"
```

Comprobar que aparezcan:

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: no-referrer`
- `Permissions-Policy: geolocation=(), camera=(), microphone=()`

En PowerShell (sin -k si usas http):

```powershell
Invoke-WebRequest -Uri "http://localhost:5100/api/v1/health" -Method Head | Select-Object -ExpandProperty Headers
```

### 2. Probar rate limit global (60 req/min)

Hacer más de 60 peticiones en un minuto al mismo endpoint (por IP); la siguiente debe devolver **429** y cuerpo ProblemDetails con `errorCode: RATE_LIMITED`:

```bash
# Linux/macOS: 65 peticiones seguidas; las últimas deberían devolver 429
for i in $(seq 1 65); do curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5100/api/v1/health; done
```

En PowerShell:

```powershell
1..65 | ForEach-Object { (Invoke-WebRequest -Uri "http://localhost:5100/api/v1/health" -UseBasicParsing).StatusCode }
```

Comprobar una respuesta 429 con cuerpo:

```bash
curl -s "http://localhost:5100/api/v1/health"  # repetir hasta 429
# Debe devolver JSON tipo: {"type":...,"title":"Too Many Requests","status":429,"instance":"/api/v1/health","errorCode":"RATE_LIMITED"}
```

### 3. Probar rate limit en auth (10 req/min)

Más de 10 peticiones/minuto a login o register desde la misma IP deben devolver **429**:

```bash
# 12 POSTs a login; las últimas deberían ser 429
for i in $(seq 1 12); do
  curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5100/api/v1/auth/login \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"test@test.com\",\"password\":\"x\"}"
done
```

PowerShell:

```powershell
$body = '{"email":"test@test.com","password":"x"}'
1..12 | ForEach-Object {
  try { (Invoke-WebRequest -Uri "http://localhost:5100/api/v1/auth/login" -Method Post -Body $body -ContentType "application/json" -UseBasicParsing).StatusCode }
  catch { $_.Exception.Response.StatusCode.value__ }
}
```

### 4. Swagger

En Development, abrir:

- `https://localhost:5100/swagger` o `http://localhost:5100/swagger`

y confirmar que la UI carga y que las operaciones se pueden ejecutar (incluido auth).

### 5. CORS (Development)

Desde el navegador con la Web en `http://localhost:5200`, hacer login/llamadas a la API y comprobar que no haya error CORS. Origen permitido en Dev es solo `http://localhost:5200`.

---

## Checklist final

### FASE 5.1 — Rate limiting (API)

- [x] Rate limiting built-in ASP.NET Core (no paquete extra).
- [x] Global por IP: 60 req/min, FixedWindow.
- [x] Auth (`/api/v1/auth/login`, `/api/v1/auth/register`): 10 req/min con `[EnableRateLimiting("AuthPolicy")]`.
- [x] Respuesta 429 con ProblemDetails (RFC 7807) y `errorCode`: `RATE_LIMITED`.
- [x] Retry-After en respuesta 429 cuando esté disponible.
- [x] ForwardedHeaders (X-Forwarded-For, X-Forwarded-Proto) solo en **Production**; KnownNetworks/KnownProxies vacíos (documentado en código).

### FASE 5.2 — Security headers + CORS (API)

- [x] CORS: en Development solo `http://localhost:5200`; en otros entornos `WebOrigin` de configuración.
- [x] Headers globales: X-Content-Type-Options: nosniff, X-Frame-Options: DENY, Referrer-Policy: no-referrer, Permissions-Policy: geolocation=(), camera=(), microphone=().
- [x] Swagger sigue funcionando en Development (sin CSP estricta que lo rompa).

### FASE 5.2 — Web

- [x] Cookie del token: HttpOnly, SameSite=Lax.
- [x] Secure = solo en Production (Development: SameAsRequest para no fallar en http local).

### Reglas generales

- [x] No se rompe el contrato de la API existente.
- [x] No se registran tokens ni contraseñas en logs.
- [x] Build sin warnings (FixHub.API compila correctamente; los errores de bloqueo de .exe son por tener la API/Web en ejecución).
