# Admin Phase A/B — Request/Response Examples

Endpoints implementados (Fase A + parte Fase B). Base URL: `GET/POST .../api/v1/admin/...`. Requieren JWT con rol Admin (users) o OpsOnly (reassign).

---

## 1. POST /admin/users/{id}/suspend

**Policy:** AdminOnly

**Request (body opcional):**
```json
{
  "suspendedUntil": "2025-03-15T23:59:59Z",
  "suspensionReason": "Impago recurrente"
}
```
O `{}` para suspensión sin fecha fin ni motivo.

**Response:** `204 No Content` (éxito).

**Error 404:** User not found.  
**Error 409:** Concurrencia (otro proceso modificó el usuario).

---

## 2. POST /admin/users/{id}/unsuspend

**Policy:** AdminOnly

**Request:** Sin body.

**Response:** `204 No Content`.

**Error 404 / 409:** Igual que suspend.

---

## 3. POST /admin/users/{id}/activate

**Policy:** AdminOnly

**Request:** Sin body.

**Response:** `204 No Content`.

**Error 404 / 409:** Igual que suspend.

---

## 4. POST /admin/users/{id}/deactivate

**Policy:** AdminOnly

**Request:** Sin body.

**Response:** `204 No Content`. Se establece `DeactivatedAt` y `IsActive = false`.

**Error 404 / 409:** Igual que suspend.

---

## 5. POST /admin/jobs/{id}/reassign

**Policy:** OpsOnly (Admin, Supervisor, OpsDispatcher)

**Request:**
```json
{
  "toTechnicianId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "reason": "Técnico no se presentó",
  "reasonDetail": "Cliente reportó ausencia; reasignación a técnico de respaldo."
}
```
- `reason`: obligatorio, máx. 200 caracteres.
- `reasonDetail`: opcional, máx. 1000 caracteres.

**Response 200:**
```json
{
  "jobId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "newAssignmentId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
  "toTechnicianId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "overrideId": "d4e5f6a7-b8c9-0123-def0-234567890123"
}
```

**Error 400:** Job Completed/Cancelled, mismo técnico, motivo vacío o validación FluentValidation.  
**Error 404:** Job o usuario destino no encontrado, o job sin asignación.  
**Error 409:** Concurrencia (xmin/RowVersion).

---

## Concurrencia (User)

En suspend/unsuspend/activate/deactivate, si otro proceso modificó el mismo usuario, la API devuelve **409 Conflict** con un body tipo ProblemDetails:

```json
{
  "title": "The user was modified by another process. Please refresh and try again.",
  "status": 409,
  "extensions": {
    "errorCode": "CONCURRENCY_CONFLICT"
  }
}
```

El cliente debe refrescar el usuario (incluido `RowVersion` si se expone) y reintentar.
