# FixHub — Documento de Rediseño de Producto
## De Marketplace a Portal Oficial de Servicios

> **Versión:** 1.0
> **Fecha:** 2026-02-24
> **Rol:** Product Designer Senior + UX Strategist
> **Stack:** ASP.NET Core + Clean Architecture + PostgreSQL 16
> **Objetivo:** Transformar FixHub en la plataforma digital oficial de servicios técnicos de la empresa.

---

## DIAGNÓSTICO DEL SISTEMA ACTUAL

### Lo que ya funciona bien (mantener)

Después de auditar el código completo, el backend de FixHub tiene una base sólida que **ya opera en modo empresa**, no en modo marketplace puro:

| Característica | Estado actual | Evaluación |
|---|---|---|
| Admin asigna técnicos (no el cliente) | ✅ Implementado | Correcto, mantener |
| Sistema de notificaciones con email outbox | ✅ Implementado | Excelente, mantener |
| Timeline visual en detalle del trabajo | ✅ Implementado | Mejorar UX |
| Audit log completo | ✅ Implementado | Mantener |
| SLA monitoring | ✅ Implementado | Mantener |
| JWT + BCrypt + Rate limiting | ✅ Implementado | Mantener |

### Lo que tiene mentalidad marketplace (transformar)

| Elemento marketplace | Impacto actual | Acción recomendada |
|---|---|---|
| Entidad `Proposal` (propuestas de técnicos) | Técnicos "licitan" trabajos | Deprecar flujo público, conservar como herramienta interna |
| Técnicos ven todos los trabajos abiertos (`GET /jobs`) | Simula marketplace de ofertas | Restringir a solo trabajos asignados |
| Página `/jobs` lista trabajos abiertos para técnicos | UX de marketplace | Eliminar como vista pública de técnicos |
| Campos `budget_min` / `budget_max` en Job | El cliente negocia precio | Ocultar del wizard del cliente |
| `ScoreSnapshot` ranking visible | Transparencia de licitación | Mantener como herramienta interna de Admin |
| Registro de Técnicos vía web pública | Cualquiera puede aplicar | Migrar a proceso interno HR |
| Campo `price` en Proposal | Negociación pública | El precio lo define la empresa, no el técnico |

### Insight crítico del código

El sistema YA funciona como empresa: `AcceptProposalCommand` solo lo puede ejecutar el Admin, no el cliente. El cliente nunca eligió técnico. **El problema es de UX y percepción, no de lógica de negocio core.**

---

## FASE 1 — NUEVO MODELO FUNCIONAL

### Nuevo flujo end-to-end

```
CLIENTE                              EMPRESA (INTERNA)
  │                                       │
  │── 1. Abre portal                      │
  │── 2. Wizard (4 pasos):               │
  │      Tipo de servicio                 │
  │      Descripción + fotos             │
  │      Ubicación                        │
  │      Fecha preferida                  │
  │── 3. Confirma solicitud ─────────────►│
  │                                       │── 4. Recibe solicitud (Dashboard)
  │◄── Email: "Solicitud recibida" ───────│── 5. Evalúa y asigna técnico
  │                                       │── 6. Define precio y confirma fecha
  │◄── Email: "Técnico asignado" ─────────│
  │── 7. Ve tracking en portal            │── 8. Técnico se desplaza
  │                                       │── 9. Técnico marca "En camino"
  │◄── Notif: "Tu técnico está en camino"─│
  │                                       │── 10. Técnico ejecuta servicio
  │── 11. Confirma servicio completado    │
  │◄── Email: "Servicio completado" ──────│
  │── 12. Califica (1-5 estrellas)       │
  │                                       │── 13. Admin ve calificación
```

### Estados del trabajo (sin cambios en backend)

```
Open → Assigned → InProgress → Completed
                              ↘ Cancelled
```

La máquina de estados existente es perfecta. No se toca.

---

## FASE 2 — CAMBIOS EN BACKEND

### Principio guía

> **No romper nada. Extender con cuidado. Deprecar en UI, no en DB.**

Las entidades `Proposal` y `JobAssignment` se conservan porque son la forma en que el sistema registra la asignación interna. El cambio está en ocultar el flujo de propuestas del cliente y del técnico.

### 2.1 Nuevos campos requeridos en Job

```sql
-- Migración: AddJobPhotoAndScheduleFields
ALTER TABLE jobs ADD COLUMN preferred_date DATE NULL;
ALTER TABLE jobs ADD COLUMN preferred_time_slot VARCHAR(20) NULL; -- morning|afternoon|evening
ALTER TABLE jobs ADD COLUMN confirmed_price DECIMAL(10,2) NULL;
ALTER TABLE jobs ADD COLUMN confirmed_date TIMESTAMPTZ NULL;
ALTER TABLE jobs ADD COLUMN photo_urls TEXT[] NULL; -- Array de URLs de fotos
ALTER TABLE jobs ADD COLUMN urgency VARCHAR(20) NULL DEFAULT 'normal'; -- normal|urgent|scheduled
```

**Justificación:**
- `preferred_date` + `preferred_time_slot`: el cliente elige cuándo. Actualmente no existe.
- `confirmed_price` + `confirmed_date`: la empresa confirma precio y fecha (reemplaza `budget_min/max`).
- `photo_urls`: adjuntar fotos al problema. Actualmente no existe.
- `urgency`: diferenciador de nivel de servicio.

### 2.2 Nuevos endpoints necesarios

```
POST   /api/v1/jobs/{id}/photos          Upload de fotos (multipart/form-data)
GET    /api/v1/jobs/{id}/photos          Lista de fotos del trabajo
POST   /api/v1/jobs/{id}/confirm-service Admin confirma precio y fecha al cliente
POST   /api/v1/jobs/{id}/technician-enroute  Técnico marca "en camino"
GET    /api/v1/services/categories       Lista de categorías con iconos (público, sin auth)
GET    /api/v1/jobs/{id}/tracking        Estado público del trabajo (token en URL, sin auth)
```

### 2.3 Endpoints a deprecar o restringir

| Endpoint | Acción | Motivo |
|---|---|---|
| `GET /api/v1/jobs` (lista abiertos para técnicos) | Restringir: solo devuelve asignados | Técnicos no deben "buscar" trabajos |
| `POST /api/v1/jobs/{id}/proposals` | Solo desde Admin panel | Técnicos no proponen; Admin asigna directamente |
| `GET /api/v1/jobs/{id}/proposals` | Solo Admin | Propuestas son internas |
| `POST /api/v1/technicians/{id}/apply` | Mover a proceso RRHH interno | No recrutar vía portal público |

### 2.4 Flujo simplificado de asignación (nuevo CreateJobCommand)

El comando `CreateJobCommand` ya tiene auto-asignación parcial. Completar con:

```csharp
// En CreateJobCommand.Handler:
// 1. Crear Job con preferred_date, time_slot, urgency, photo_urls
// 2. Notificar a Admin inmediatamente
// 3. NO auto-asignar (la empresa revisa primero para dar precio)
// 4. Enviar email de "Solicitud recibida" al cliente con tracking URL

// Nuevo flujo de asignación Admin:
// POST /admin/jobs/{id}/assign-technician (nuevo endpoint)
// Body: { technicianId, confirmedPrice, confirmedDate, adminNote }
// 1. Crear Proposal interna con precio confirmed
// 2. Aceptar Proposal (AcceptProposalCommand existente)
// 3. Actualizar Job.confirmed_price + Job.confirmed_date
// 4. Notificar cliente con precio y fecha confirmada
```

### 2.5 Nuevo comando: `AssignTechnicianDirectlyCommand`

```csharp
// Reemplaza el flujo manual de AcceptProposalCommand para el Admin
// Admin no necesita: crear propuesta → aceptar propuesta → asignar
// Nuevo flujo: Admin asigna directamente en un paso

public record AssignTechnicianDirectlyCommand(
    Guid JobId,
    Guid TechnicianId,
    decimal ConfirmedPrice,
    DateTime ConfirmedDate,
    string? AdminNote
) : IRequest<Result<JobDto>>;

// Handler:
// 1. Validar Job.Status == Open
// 2. Validar Technician existe y Status == Approved
// 3. Crear Proposal interna (price = confirmedPrice, status = Accepted)
// 4. Crear JobAssignment
// 5. Actualizar Job (status = Assigned, confirmed_price, confirmed_date, assigned_at)
// 6. Notificar cliente + técnico
```

### 2.6 Nuevo estado: `TechnicianEnRoute` (opcional, high-impact)

```csharp
// Agrega valor percibido enorme: el cliente sabe que viene el técnico
// Opción A: Nuevo estado en JobStatus enum (entre Assigned e InProgress)
// Opción B: Campo bool job.technician_en_route_at TIMESTAMPTZ
// RECOMENDACIÓN: Opción B (no rompe FSM existente)

ALTER TABLE jobs ADD COLUMN en_route_at TIMESTAMPTZ NULL;
// Solo visual, no cambia Job.Status
```

### 2.7 Tracking público (link compartible)

```csharp
// Nuevo campo en Job:
ALTER TABLE jobs ADD COLUMN tracking_token VARCHAR(32) NULL UNIQUE;
// Generado al crear el trabajo (GUID corto o nanoid)
// URL: /track/{tracking_token} (sin autenticación)
// Solo muestra: estado actual, técnico asignado (nombre), estimado llegada
// NO muestra: precio, datos privados, historial completo
```

---

## FASE 3 — CAMBIOS EN FRONTEND (FixHub.Web)

### 3.1 Páginas a crear (nuevas)

| Página | URL | Descripción |
|---|---|---|
| Landing page | `/` | Hero + CTA + Cómo funciona + Confianza |
| Wizard solicitud | `/solicitar` | 4 pasos (reemplaza `/jobs/create`) |
| Tracking público | `/track/{token}` | Timeline sin login |
| Historial cliente | `/mis-servicios` | Mejora de `/requests/my` |
| Página de categorías | `/servicios` | Muestra todos los tipos de servicio |

### 3.2 Páginas a eliminar (o redirigir)

| Página actual | Acción |
|---|---|
| `/recruit/apply` | Eliminar del menú público → ruta interna RRHH |
| `/technicians/profile/{id}` | Ocultar al cliente (solo Admin) |
| `/jobs` (lista técnicos) | Redirigir a `/technician/assignments` |

### 3.3 Navegación rediseñada

```
CLIENTE (logueado):
  Navbar: [Logo] [Mis servicios] [Solicitar] [🔔] [Mi cuenta]

TÉCNICO (logueado):
  Navbar: [Logo] [Mis trabajos] [🔔] [Mi cuenta]

ADMIN (logueado):
  Navbar: [Logo] [Dashboard] [Solicitudes] [Técnicos] [Alertas] [🔔]

VISITANTE (no logueado):
  Navbar: [Logo] [Servicios] [Contacto] [Iniciar sesión] [Solicitar servicio →]
```

---

## FASE 4 — WIREFRAMES CONCEPTUALES (texto)

### 4.1 Landing Page (`/`)

```
┌─────────────────────────────────────────────────────────┐
│  NAVBAR: [Logo FixHub]              [Iniciar sesión]    │
│                                     [Solicitar →]       │
├─────────────────────────────────────────────────────────┤
│                                                         │
│           HERO SECTION (full viewport)                  │
│                                                         │
│   ┌─────────────────────────────────────────────┐       │
│   │                                             │       │
│   │  "Tu hogar en las mejores manos."           │       │
│   │                                             │       │
│   │  Técnicos certificados. Precios claros.     │       │
│   │  Servicio garantizado.                      │       │
│   │                                             │       │
│   │   [Solicitar servicio ahora  →]             │       │
│   │   ⚡ Respuesta en menos de 2 horas          │       │
│   │                                             │       │
│   └─────────────────────────────────────────────┘       │
│                                                         │
│   Imagen: técnico sonriente con herramientas / home     │
│                                                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│           TRUST BAR (fondo gris claro)                  │
│                                                         │
│   [⭐ 4.9/5]  [🔧 +500 servicios]  [✅ Garantizados]   │
│   [📍 Tu ciudad]  [⏰ Lun-Sab 8-20h]                   │
│                                                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│           CÓMO FUNCIONA (3 pasos)                       │
│                                                         │
│   ┌──────────┐    ┌──────────┐    ┌──────────┐         │
│   │    01    │    │    02    │    │    03    │         │
│   │  📱      │ →  │  🔧      │ →  │  ⭐      │         │
│   │ Solicitas│    │ Enviamos │    │ Calificas│         │
│   │          │    │ técnico  │    │          │         │
│   │ Describe │    │Confirmado│    │ Garantía │         │
│   │ el prob. │    │precio y  │    │ incluida │         │
│   │ en 2 min │    │fecha     │    │          │         │
│   └──────────┘    └──────────┘    └──────────┘         │
│                                                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│           CATEGORÍAS DE SERVICIO                        │
│                                                         │
│   [🔌 Electricidad] [🔧 Plomería] [❄️ Aire A/C]        │
│   [🎨 Pintura]      [🔑 Cerrajería] [🔨 Reparaciones]  │
│                                                         │
│            [Ver todos los servicios]                    │
│                                                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│           CONFIANZA Y GARANTÍAS                         │
│                                                         │
│   ┌──────────────┐  ┌──────────────┐  ┌─────────────┐  │
│   │ ✅ Garantía  │  │ 🏆 Técnicos  │  │ 📋 Precios  │  │
│   │ de servicio  │  │ certificados │  │ sin sorpresa│  │
│   │              │  │              │  │             │  │
│   │ Si no quedas │  │ Verificados  │  │ Cotizamos   │  │
│   │ satisfecho,  │  │ y con        │  │ antes de    │  │
│   │ lo resolvemos│  │ experiencia  │  │ empezar     │  │
│   └──────────────┘  └──────────────┘  └─────────────┘  │
│                                                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│           TESTIMONIOS                                   │
│                                                         │
│   ┌─────────────────────┐  ┌─────────────────────┐     │
│   │ ⭐⭐⭐⭐⭐           │  │ ⭐⭐⭐⭐⭐           │     │
│   │                     │  │                     │     │
│   │ "Llegaron a tiempo, │  │ "El técnico explicó │     │
│   │ arreglaron todo y   │  │ todo antes de       │     │
│   │ limpiaron al salir."│  │ cobrar. Volvería."  │     │
│   │                     │  │                     │     │
│   │ — María G., Centro  │  │ — Roberto F., Norte │     │
│   └─────────────────────┘  └─────────────────────┘     │
│                                                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│           CTA FINAL                                     │
│                                                         │
│   "¿Tienes un problema en casa? Lo resolvemos hoy."    │
│                                                         │
│              [Solicitar servicio →]                     │
│         📞 O llámanos: +XX XXX XXX XXXX                 │
│                                                         │
├─────────────────────────────────────────────────────────┤
│  FOOTER: Logo | Servicios | Contacto | Aviso privacidad │
└─────────────────────────────────────────────────────────┘
```

---

### 4.2 Wizard de Solicitud (4 pasos + confirmación)

```
URL: /solicitar
ACCESO: Sin login (o con login, misma experiencia)

HEADER DEL WIZARD:
┌─────────────────────────────────────────────────────────┐
│  ← Volver          Solicitar servicio                   │
│                                                         │
│  [●──────────○──────────○──────────○]                   │
│  Servicio   Problema   Ubicación   Fecha                │
│  (Paso 1 de 4)                                          │
└─────────────────────────────────────────────────────────┘

──────────────────────────────────────────
PASO 1: ¿Qué tipo de servicio necesitas?
──────────────────────────────────────────

  "Selecciona la categoría más cercana a tu problema"

  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
  │   🔌     │ │   🔧     │ │   ❄️     │ │   🎨     │
  │          │ │          │ │          │ │          │
  │Electrici-│ │ Plomería │ │ Aire A/C │ │ Pintura  │
  │  dad     │ │          │ │          │ │          │
  └──────────┘ └──────────┘ └──────────┘ └──────────┘
  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
  │   🔑     │ │   🔨     │ │   🪟     │ │   ❓     │
  │          │ │          │ │          │ │          │
  │Cerrajería│ │Reparacion│ │ Ventanas │ │  Otro    │
  └──────────┘ └──────────┘ └──────────┘ └──────────┘

  ¿Es urgente?
  ○ No, puedo esperar    ○ Hoy mismo    ● Sí, es urgente

                              [Continuar →]

──────────────────────────────────────────
PASO 2: Cuéntanos el problema
──────────────────────────────────────────

  "Describe con tus palabras qué está pasando"

  ┌────────────────────────────────────────────────────┐
  │ Ej: "El calentador de agua dejó de funcionar       │
  │ esta mañana, no hay agua caliente en toda          │
  │ la casa..."                                        │
  │                                                    │
  │                                    85/500 palabras │
  └────────────────────────────────────────────────────┘

  📷 Adjunta fotos (opcional pero ayuda mucho)

  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
  │    +     │ │ [foto1]  │ │ [foto2]  │ │    +     │
  │  Agregar │ │    ✕     │ │    ✕     │ │  Agregar │
  └──────────┘ └──────────┘ └──────────┘ └──────────┘
  Máximo 5 fotos · JPG, PNG · Hasta 10MB cada una

  ← Anterior                          [Continuar →]

──────────────────────────────────────────
PASO 3: ¿Dónde realizamos el servicio?
──────────────────────────────────────────

  "Tu dirección exacta para que el técnico llegue sin problemas"

  Calle y número *
  ┌────────────────────────────────────────────────────┐
  │ Ej: Av. Principal 1234                             │
  └────────────────────────────────────────────────────┘

  Colonia / Barrio
  ┌────────────────────────────────────────────────────┐
  │                                                    │
  └────────────────────────────────────────────────────┘

  Ciudad / Municipio *
  ┌────────────────────────────────────────────────────┐
  │                                                    │
  └────────────────────────────────────────────────────┘

  Referencias adicionales (piso, interior, referencias)
  ┌────────────────────────────────────────────────────┐
  │ Ej: Depto 4B, timbre no funciona, llamar al llegar │
  └────────────────────────────────────────────────────┘

  Tu nombre *
  ┌──────────────────────┐  Tu teléfono *
  │                      │  ┌───────────────────────────┐
  └──────────────────────┘  └───────────────────────────┘

  Tu correo electrónico *
  ┌────────────────────────────────────────────────────┐
  │ (Para enviarte el seguimiento)                     │
  └────────────────────────────────────────────────────┘

  ← Anterior                          [Continuar →]

──────────────────────────────────────────
PASO 4: ¿Cuándo te conviene?
──────────────────────────────────────────

  "Elige tu preferencia. Confirmaremos disponibilidad."

  Fecha preferida
  [Calendario visual — min: mañana, max: +30 días]

  Horario preferido
  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
  │ 🌅 Mañana   │ │ ☀️ Tarde     │ │ 🌇 Noche     │
  │ 8:00 - 12:00│ │ 12:00 - 17:00│ │ 17:00 - 20:00│
  └──────────────┘ └──────────────┘ └──────────────┘

  ℹ️ Nota: Confirmaremos la disponibilidad exacta
     por correo en las próximas 2 horas hábiles.

  ← Anterior                    [Confirmar solicitud →]

──────────────────────────────────────────
CONFIRMACIÓN (página final)
──────────────────────────────────────────

  ┌─────────────────────────────────────────────────────┐
  │                                                     │
  │          ✅                                         │
  │                                                     │
  │     ¡Solicitud recibida!                            │
  │                                                     │
  │  Tu número de seguimiento es:  #FH-2024-0392        │
  │                                                     │
  │  Hemos enviado la confirmación a:                   │
  │  maria@ejemplo.com                                  │
  │                                                     │
  │  ¿Qué sigue?                                        │
  │  📋 Revisamos tu solicitud (2 horas hábiles)        │
  │  🔧 Asignamos el mejor técnico disponible           │
  │  📱 Te notificamos por correo con precio y fecha    │
  │                                                     │
  │  [Seguir mi solicitud en tiempo real →]             │
  │                                                     │
  │  📞 ¿Preguntas? +XX XXX XXX XXXX                   │
  │                                                     │
  └─────────────────────────────────────────────────────┘
```

---

### 4.3 Página de Seguimiento (Tracking)

```
URL: /track/{token} (pública, sin login)
     /mis-servicios/{id} (logueado, misma vista enriquecida)

┌─────────────────────────────────────────────────────────┐
│  [Logo FixHub]              📞 +XX XXX XXX XXXX         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Seguimiento de servicio  #FH-2024-0392                 │
│  Plomería · Av. Principal 1234                          │
│                                                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  TIMELINE VISUAL (vertical, tipo courier)               │
│                                                         │
│  ●─────────────────────────────────────                 │
│  ✅  Solicitud recibida                                 │
│      Lun 24 Feb · 10:32 am                             │
│      "Revisando tu solicitud..."                        │
│                                                         │
│  ●─────────────────────────────────────                 │
│  ✅  Técnico asignado                                   │
│      Lun 24 Feb · 12:15 pm                             │
│      Juan Pérez · ⭐ 4.8 · Plomero certificado         │
│      📞 +XX XXX XXXX (disponible desde las 14:00)      │
│                                                         │
│  ●─────────────────────────────────────                 │
│  🔄  En camino                          ← ESTADO ACTUAL │
│      Hoy · Llegada estimada: 14:30 pm                   │
│      "Tu técnico está en camino"                        │
│                                                         │
│  ○─────────────────────────────────────                 │
│  ⏳  Servicio en progreso                               │
│                                                         │
│  ○                                                      │
│  ⏳  Completado                                         │
│                                                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  PRECIO CONFIRMADO: $XXX.XX                             │
│  Fecha confirmada: Hoy, 14:00 - 15:00                   │
│                                                         │
│  ¿Necesitas cancelar o reagendar?                       │
│  [Cancelar solicitud]   [Contactar soporte]             │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

### 4.4 Dashboard Admin (Panel Interno)

```
URL: /admin/dashboard
ACCESO: Solo Admin

┌─────────────────────────────────────────────────────────┐
│  [Logo] PANEL INTERNO              🔔  Admin ▾          │
│  Dashboard | Solicitudes | Técnicos | Alertas           │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  KPIs (tarjetas superiores)                             │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐  │
│  │    12    │ │    3     │ │    7     │ │   48     │  │
│  │ Nuevas   │ │ Asignadas│ │ En curso │ │ Hoy comp │  │
│  │🔴 Alerta │ │          │ │          │ │          │  │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘  │
│                                                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  SOLICITUDES NUEVAS (requieren asignación)              │
│                                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │ #FH-0392 │ Plomería │ María G. │ Urgente 🔴     │   │
│  │ Av. Principal 1234 · Hace 15 min               │   │
│  │ "El calentador dejó de funcionar..."            │   │
│  │ [Ver detalle]  [Asignar técnico →]              │   │
│  └─────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────┐   │
│  │ #FH-0391 │ Electricidad │ Roberto F. │ Normal   │   │
│  │ Calle Norte 567 · Hace 1 hora                   │   │
│  │ "Apagón en una habitación..."                   │   │
│  │ [Ver detalle]  [Asignar técnico →]              │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  MODAL: Asignar técnico (al hacer click)                │
│                                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │  Asignar técnico para #FH-0392                  │   │
│  │                                                  │   │
│  │  Técnico disponible:    [Juan Pérez ⭐4.8  ▾]   │   │
│  │  Precio confirmado:     [$_________]             │   │
│  │  Fecha confirmada:      [📅 _______]             │   │
│  │  Horario:               [14:00 ▾]                │   │
│  │  Nota interna:          [_________________]      │   │
│  │                                                  │   │
│  │         [Cancelar]   [Asignar y notificar →]    │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## FASE 5 — IDENTIDAD VISUAL Y MARCA

### 5.1 Sistema de color recomendado

> Principio: Confianza + Profesionalismo + Modernidad

```
PALETA PRINCIPAL
─────────────────────────────────────────
Azul corporativo:      #1B4FD8  (primario, CTAs, confianza)
Azul oscuro:           #1E3A8A  (hover, headers)
Azul claro (accent):   #3B82F6  (links, secondary actions)

PALETA DE SOPORTE
─────────────────────────────────────────
Verde éxito:           #10B981  (completado, confirmado, éxito)
Ámbar alerta:          #F59E0B  (en proceso, advertencia)
Rojo error:            #EF4444  (urgente, error, cancelado)
Gris neutro:           #6B7280  (texto secundario, bordes)
Gris claro:            #F9FAFB  (backgrounds de secciones)

NEUTROS
─────────────────────────────────────────
Blanco:                #FFFFFF
Negro texto:           #111827
Texto secundario:      #374151
Borde suave:           #E5E7EB

GRADIENTE HERO (opcional):
Linear: #1B4FD8 → #1E3A8A (diagonal 135°)
```

**Por qué este sistema:** El azul construye confianza (fintech, seguros, servicios). El verde confirma acciones. El ámbar alerta sin alarmar. Es el lenguaje cromático de las apps de servicio que los usuarios ya conocen (Uber, TaskRabbit, MrFix).

### 5.2 Tipografía

```
HEADINGS: Inter (700, 600)
  - Moderna, clara, técnica, disponible gratis (Google Fonts)
  - H1: 48px/56px, H2: 32px, H3: 24px

BODY TEXT: Inter (400, 500)
  - Consistencia con headings
  - Body: 16px, Small: 14px, Caption: 12px

NÚMEROS / DATOS: Inter Numeric (tabulares)
  - Para KPIs, precios, seguimiento

ALTERNATIVA PREMIUM: Outfit (heading) + Inter (body)
  - Outfit da calidez sin perder profesionalismo
```

### 5.3 Componentes UI (style guide)

```
BOTONES
────────────────────────────────
Primary:   bg-blue-700  text-white  px-6 py-3  rounded-lg  shadow-md
           hover:bg-blue-800  transition-all  font-semibold
Secondary: border-2 border-blue-700  text-blue-700  px-6 py-3  rounded-lg
Ghost:     text-gray-600  px-4 py-2  hover:text-blue-700

CARDS (solicitudes, servicios)
────────────────────────────────
bg-white  rounded-xl  shadow-sm  border border-gray-100
hover:shadow-md  transition-shadow  p-6

INPUTS
────────────────────────────────
border border-gray-300  rounded-lg  px-4 py-3
focus:ring-2 focus:ring-blue-500  focus:border-transparent
text-gray-900  placeholder-gray-400

BADGES DE ESTADO
────────────────────────────────
Nueva:      bg-blue-100   text-blue-800   (Open)
Asignada:   bg-yellow-100 text-yellow-800 (Assigned)
En curso:   bg-orange-100 text-orange-800 (InProgress)
Completada: bg-green-100  text-green-800  (Completed)
Cancelada:  bg-gray-100   text-gray-600   (Cancelled)
Urgente:    bg-red-100    text-red-800
```

### 5.4 Microcopy estratégico

> El copy es la voz de la marca. Evitar tecnicismos. Hablar como una empresa que cuida al cliente.

| Momento | Copy actual (malo) | Copy nuevo (bueno) |
|---|---|---|
| Hero H1 | — | "Tu hogar en las mejores manos." |
| Hero subtitle | — | "Técnicos certificados. Precios claros. Garantía en cada servicio." |
| CTA principal | "Crear trabajo" | "Solicitar servicio ahora →" |
| Estado Open | "Open" | "Revisando tu solicitud" |
| Estado Assigned | "Assigned" | "Técnico confirmado" |
| Estado InProgress | "InProgress" | "Servicio en progreso" |
| Estado Completed | "Completed" | "¡Servicio completado! 🎉" |
| Confirmación | "Job created" | "¡Solicitud recibida! En 2 horas hábiles te contactamos." |
| Error 500 | "Internal server error" | "Algo salió mal. Intenta en un momento o llámanos." |
| Sin servicios | "No jobs found" | "Aún no tienes servicios. ¿Necesitas ayuda con algo?" |
| Email asignado | "Técnico asignado" | "¡Buenas noticias! Tu técnico ya está confirmado" |
| Calificación | "Rate service" | "¿Cómo te fue? Tu opinión mejora el servicio" |
| Urgente badge | — | "⚡ Urgente" |
| Trust | — | "Garantía: si no quedas satisfecho, lo resolvemos" |

---

## FASE 6 — MEJORAS DE CONVERSIÓN

### 6.1 Email triggers (aprovechar sistema existente)

El `NotificationOutbox` + `SendGrid` ya está implementado. Solo agregar estos eventos:

| Evento | Asunto sugerido | Momento |
|---|---|---|
| Solicitud recibida | "✅ Solicitud #FH-{id} recibida — te contactamos en 2h" | Inmediato |
| Técnico asignado | "🔧 Tu técnico {nombre} está confirmado para {fecha}" | Al asignar |
| En camino | "📍 {nombre} está en camino · llega a las {hora}" | Al marcar en camino |
| Completado | "✅ Servicio completado — ¿cómo nos fue?" | Al completar |
| Recordatorio calificación | "⭐ ¿Qué te pareció el servicio de {nombre}?" | +24h si no calificó |
| Cancelación | "ℹ️ Tu servicio fue cancelado. ¿Podemos ayudarte de otra forma?" | Al cancelar |

### 6.2 Historial del cliente

La página `/mis-servicios` (mejora de `/requests/my`) debe mostrar:

```
┌─────────────────────────────────────────────────────────┐
│  Mis servicios                           [Solicitar +]  │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ACTIVO                                                 │
│  ┌─────────────────────────────────────────────────┐   │
│  │ 🔄 En progreso        Plomería                   │   │
│  │    Hoy · Av. Principal 1234                      │   │
│  │    Técnico: Juan Pérez                           │   │
│  │    [Ver seguimiento →]                           │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  ANTERIORES                                             │
│  ┌─────────────────────────────────────────────────┐   │
│  │ ✅ Completado          Electricidad              │   │
│  │    15 Feb · ⭐⭐⭐⭐⭐                            │   │
│  │    [Ver detalle]  [Solicitar de nuevo]           │   │
│  └─────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────┐   │
│  │ ✅ Completado          Pintura                   │   │
│  │    3 Feb · ⭐⭐⭐⭐                               │   │
│  │    [Ver detalle]  [Solicitar de nuevo]           │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**"Solicitar de nuevo"** es un conversion booster: precarga el wizard con la misma categoría y dirección.

### 6.3 Sistema de calificación mejorado

```
FLUJO ACTUAL: /reviews/create (formulario plano)

FLUJO PROPUESTO: Modal inline desde /mis-servicios o email link

  ┌─────────────────────────────────────────────────────┐
  │                                                     │
  │   ¿Cómo estuvo el servicio de Juan Pérez?          │
  │   Plomería · 24 Feb · Av. Principal 1234            │
  │                                                     │
  │         ☆  ☆  ☆  ☆  ☆                              │
  │       (toca las estrellas)                          │
  │                                                     │
  │   ┌───────────────────────────────────────────┐     │
  │   │ Cuéntanos más (opcional)                  │     │
  │   │                                           │     │
  │   └───────────────────────────────────────────┘     │
  │                                                     │
  │              [Enviar calificación]                  │
  │         Gracias, mejoras nuestro servicio           │
  │                                                     │
  └─────────────────────────────────────────────────────┘
```

---

## FASE 7 — REGISTRO Y AUTENTICACIÓN SIMPLIFICADA

### 7.1 Registro de clientes

El formulario actual pide `Role` (enum 1/2). El cliente no debe elegir rol.

```
NUEVO FLUJO: El cliente NO elige rol.
- Toda solicitud desde el portal → automáticamente Customer
- La ruta /solicitar puede iniciar sin login
- Al final del wizard (paso 3: datos) → si no está logueado:
  "Para seguir tu solicitud, crea tu cuenta rápido"
  [Email] [Contraseña] → Se registra como Customer automáticamente
- O: "Continuar sin cuenta" → Tracking por email + token

FORMULARIO SIMPLIFICADO:
  ┌────────────────────────────────────────────────────┐
  │           Crea tu cuenta para dar seguimiento       │
  │                                                    │
  │  Nombre completo  ______________________________   │
  │  Correo           ______________________________   │
  │  Contraseña       ______________________________   │
  │                                                    │
  │  [Crear cuenta y confirmar solicitud →]            │
  │                                                    │
  │  ─────────────────── o ─────────────────────────  │
  │  Ya tengo cuenta  [Iniciar sesión]                 │
  └────────────────────────────────────────────────────┘
```

**Cambio en backend:** `RegisterCommand` → agregar parámetro `role = Customer` por defecto cuando viene del portal público. Los técnicos y admins se crean internamente.

---

## FASE 8 — DASHBOARD TÉCNICO SIMPLIFICADO

Los técnicos solo necesitan ver sus trabajos asignados, no el marketplace de trabajos abiertos.

```
URL: /tecnico/mis-trabajos

┌─────────────────────────────────────────────────────────┐
│  Hola, Juan 👋             🔔 2 notificaciones          │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  HOY (2 trabajos)                                       │
│                                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │ 🔧 Plomería · 14:00 - 15:00                     │   │
│  │    María González                               │   │
│  │    Av. Principal 1234, Depto 4B                 │   │
│  │    📞 +XX XXX XXX XXXX                          │   │
│  │    "El calentador dejó de funcionar..."         │   │
│  │    [Ver detalle]  [Marcar: en camino 📍]        │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │ 🔌 Electricidad · 16:30 - 17:30                 │   │
│  │    Roberto Flores                               │   │
│  │    Calle Norte 567                              │   │
│  │    [Ver detalle]                                │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  PRÓXIMOS (esta semana)                                 │
│  ─ Martes 26: 1 trabajo                                 │
│  ─ Jueves 28: 2 trabajos                               │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## LISTA PRIORIZADA DE IMPLEMENTACIÓN

### Sprint 1 — Fundación (1-2 semanas) 🔴 CRÍTICO

| Tarea | Tipo | Esfuerzo | Impacto |
|---|---|---|---|
| Migración DB: `preferred_date`, `time_slot`, `urgency`, `tracking_token` | Backend | Bajo | Alto |
| Nuevo wizard `/solicitar` (4 pasos, Razor Pages) | Frontend | Medio | Crítico |
| Endpoint: `AssignTechnicianDirectlyCommand` | Backend | Medio | Alto |
| Landing page rediseñada (`/`) | Frontend | Medio | Crítico |
| Ocultar `/jobs` para técnicos (redirect a assignments) | Frontend | Bajo | Medio |
| Registro sin elegir rol (auto Customer) | Backend/Frontend | Bajo | Alto |

### Sprint 2 — Experiencia (2-3 semanas) 🟡 IMPORTANTE

| Tarea | Tipo | Esfuerzo | Impacto |
|---|---|---|---|
| Upload de fotos en wizard (campo `photo_urls`) | Backend/Frontend | Medio | Alto |
| Página de tracking público `/track/{token}` | Frontend | Medio | Alto |
| Dashboard Admin rediseñado (modal de asignación) | Frontend | Medio | Alto |
| Emails con nuevos templates y subjects | Backend | Bajo | Medio |
| Historial del cliente mejorado `/mis-servicios` | Frontend | Bajo | Medio |

### Sprint 3 — Conversión (2-3 semanas) 🟢 NICE TO HAVE

| Tarea | Tipo | Esfuerzo | Impacto |
|---|---|---|---|
| Campo `en_route_at` + notificación "en camino" | Backend/Frontend | Bajo | Alto |
| Modal de calificación inline (reemplaza página plana) | Frontend | Bajo | Medio |
| "Solicitar de nuevo" con precarga de datos | Frontend | Bajo | Medio |
| Dashboard técnico rediseñado (agenda del día) | Frontend | Medio | Medio |
| Recordatorio email +24h sin calificar | Backend | Bajo | Medio |
| Sección testimonios en landing (datos reales de Reviews) | Frontend | Bajo | Bajo |

### Sprint 4 — Escala (futuro) ⚪ BACKLOG

| Tarea | Tipo | Notas |
|---|---|---|
| Confirmación de precio vía SMS (Twilio) | Backend | Alta fricción para el cliente |
| Seguimiento en mapa (Google Maps) | Frontend | Requiere GPS del técnico |
| App móvil (PWA) | Frontend | El portal responsive ya funciona en móvil |
| Portal RRHH interno para técnicos | Separar de portal público | |

---

## RESUMEN EJECUTIVO

### ¿Qué NO se toca?

- Lógica de `Job`, `Proposal`, `JobAssignment` en backend → se conserva
- FSM de estados (Open → Assigned → InProgress → Completed) → se conserva
- Sistema de notificaciones + OutboxPattern → se conserva y amplía
- Autenticación JWT + RBAC → se conserva
- Audit log + SLA monitoring → se conserva
- `RankTechniciansCommand` → se conserva como herramienta interna de Admin

### ¿Qué SÍ cambia?

1. **Frontend:** Landing page → Wizard → Tracking → Historial (nuevas páginas)
2. **UX:** Cliente no ve propuestas, no elige técnico, experiencia en 4 pasos
3. **Admin:** Panel con asignación directa en un click (modal)
4. **Backend:** 1 migración DB + 1 nuevo comando + 2 nuevos endpoints
5. **Copy:** Lenguaje de empresa de servicios, no de marketplace
6. **Visual:** Sistema de color + tipografía consistente

### Propuesta de valor comunicada al cliente

```
ANTES (marketplace):     DESPUÉS (empresa propia):
"Recibe propuestas"  →   "Te asignamos el mejor técnico"
"Elige técnico"      →   "Nos encargamos de todo"
"Negocia precio"     →   "Precio claro antes de empezar"
"¿Quién vendrá?"     →   "Técnico certificado garantizado"
```

---

> **Principio de diseño:** El cliente solo debe tomar 1 decisión: *¿solicito el servicio?*
> Todo lo demás — técnico, precio, fecha — es responsabilidad de la empresa.
> Eso es servicio. Eso es confianza. Eso convierte.

---

*Documento generado para FixHub · audit/fixhub-100 · Febrero 2026*
