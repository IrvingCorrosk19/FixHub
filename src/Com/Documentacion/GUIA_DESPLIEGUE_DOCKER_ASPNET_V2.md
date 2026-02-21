# 🚀 Guía Completa: Despliegue de Aplicación ASP.NET Core con Docker y PostgreSQL
## Versión 2.0 - Multi-App Edition

## 📋 Tabla de Contenidos

1. [Preparación del Servidor VPS](#1-preparación-del-servidor-vps)
2. [Instalación de Docker](#2-instalación-de-docker)
3. [Preparación del Repositorio Git](#3-preparación-del-repositorio-git)
4. [Creación del Dockerfile](#4-creación-del-dockerfile)
5. [Configuración de docker-compose.yml](#5-configuración-de-docker-composeyml)
6. [Configuración de Variables de Entorno (.env)](#6-configuración-de-variables-de-entorno-env)
7. [Configuración de Program.cs (ASP.NET Core)](#7-configuración-de-programcs-aspnet-core)
8. [Configuración de DataProtection](#8-configuración-de-dataprotection)
9. [Configuración de Cookies y Autenticación](#9-configuración-de-cookies-y-autenticación)
10. [Scripts de Despliegue](#10-scripts-de-despliegue)
11. [Despliegue Inicial](#11-despliegue-inicial)
12. [Verificación y Pruebas](#12-verificación-y-pruebas)
13. [Solución de Problemas Comunes](#13-solución-de-problemas-comunes)
14. [Mantenimiento y Actualización](#14-mantenimiento-y-actualización)
15. [Checklist de Despliegue](#15-checklist-de-despliegue)
16. [Mejoras Futuras (Opcional)](#16-mejoras-futuras-opcional)
17. [Notas Finales](#17-notas-finales)
18. [**NUEVO:** Despliegue de Múltiples Aplicaciones](#18-despliegue-de-múltiples-aplicaciones-en-un-solo-vps)

---

*[Todos los capítulos 1-17 permanecen igual que en la versión 1.0]*

---

## 18. Despliegue de Múltiples Aplicaciones en un Solo VPS

### 18.1 Introducción

Esta guía ha sido diseñada para **una instalación limpia** de una aplicación ASP.NET Core. Sin embargo, en escenarios reales, es común necesitar **múltiples aplicaciones en el mismo servidor**.

**Escenarios comunes:**
- Varias aplicaciones ASP.NET Core diferentes
- Entornos staging + production en el mismo VPS
- Múltiples clientes con aplicaciones similares
- Microservicios o aplicaciones relacionadas

**✅ Buenas noticias:** Esta guía **SÍ permite múltiples instalaciones**, con ajustes estructurales mínimos para evitar conflictos.

---

### 18.2 Conflictos Potenciales (Si No Se Ajusta)

Si intentas usar esta guía tal cual para una segunda aplicación, tendrás **conflictos** en:

| Elemento | Conflicto | Impacto |
|----------|-----------|---------|
| **Puertos** | Todas usan `80:8080` | Solo una puede usar puerto 80 |
| **Volúmenes** | Nombres genéricos (`postgres_data`) | Datos se mezclan entre apps |
| **Contenedores** | Nombres genéricos (`tuapp_web`) | Docker no permite duplicados |
| **Cookies** | Mismo nombre (`.TuAplicacion.Auth`) | Sesiones cruzadas entre apps |
| **Redes** | Mismo nombre (`tuapp_net`) | Posible comunicación no deseada |

**👉 Solución:** Usar **prefijos únicos** y **puertos distintos** para cada aplicación.

---

### 18.3 Estructura Recomendada en el VPS

```
/opt/apps/
├── carnetqr/
│   ├── docker-compose.yml
│   ├── Dockerfile
│   ├── .env
│   ├── .git/
│   ├── Com/
│   │   ├── deploy-docker.ps1
│   │   ├── rebuild-deploy.ps1
│   │   └── verificar-db.ps1
│   └── [código fuente de CarnetQR]
│
├── inventario/
│   ├── docker-compose.yml
│   ├── Dockerfile
│   ├── .env
│   ├── .git/
│   ├── Com/
│   │   ├── deploy-docker.ps1
│   │   ├── rebuild-deploy.ps1
│   │   └── verificar-db.ps1
│   └── [código fuente de Inventario]
│
└── crm/
    ├── docker-compose.yml
    ├── Dockerfile
    ├── .env
    ├── .git/
    └── [código fuente de CRM]
```

**✅ Ventajas:**
- Cada aplicación completamente aislada
- Fácil identificación
- Backups independientes
- Rollback sin afectar otras apps
- Permisos granulares

---

### 18.4 Ajuste #1: Nombres Únicos (OBLIGATORIO)

#### docker-compose.yml - Aplicación 1 (CarnetQR)

```yaml
services:
  postgres:
    image: postgres:15
    container_name: carnetqr_postgres  # ⚠️ Prefijo único
    restart: always
    env_file:
      - .env
    volumes:
      - carnetqr_postgres_data:/var/lib/postgresql/data  # ⚠️ Prefijo único
    ports:
      - "5432:5432"  # ⚠️ Puerto único (o no exponer)
    networks:
      - carnetqr_net  # ⚠️ Prefijo único
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 10s
      timeout: 5s
      retries: 5

  web:
    build: .
    container_name: carnetqr_web  # ⚠️ Prefijo único
    restart: always
    depends_on:
      postgres:
        condition: service_healthy
    env_file:
      - .env
    environment:
      ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      ASPNETCORE_URLS: http://+:8080
      ASPNETCORE_DATAPROTECTION_PATH: /app/dataprotection-keys
    volumes:
      - carnetqr_dataprotection_keys:/app/dataprotection-keys  # ⚠️ Prefijo único
      - carnetqr_uploads:/app/wwwroot/uploads  # ⚠️ Prefijo único
    ports:
      - "8081:8080"  # ⚠️ Puerto único (8081, no 80)
    networks:
      - carnetqr_net  # ⚠️ Prefijo único

volumes:
  carnetqr_postgres_data:  # ⚠️ Prefijo único
  carnetqr_dataprotection_keys:  # ⚠️ Prefijo único
  carnetqr_uploads:  # ⚠️ Prefijo único

networks:
  carnetqr_net:  # ⚠️ Prefijo único
```

#### docker-compose.yml - Aplicación 2 (Inventario)

```yaml
services:
  postgres:
    image: postgres:15
    container_name: inventario_postgres  # ⚠️ Prefijo diferente
    restart: always
    env_file:
      - .env
    volumes:
      - inventario_postgres_data:/var/lib/postgresql/data  # ⚠️ Prefijo diferente
    ports:
      - "5433:5432"  # ⚠️ Puerto diferente (5433, no 5432)
    networks:
      - inventario_net  # ⚠️ Prefijo diferente
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 10s
      timeout: 5s
      retries: 5

  web:
    build: .
    container_name: inventario_web  # ⚠️ Prefijo diferente
    restart: always
    depends_on:
      postgres:
        condition: service_healthy
    env_file:
      - .env
    environment:
      ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      ASPNETCORE_URLS: http://+:8080
      ASPNETCORE_DATAPROTECTION_PATH: /app/dataprotection-keys
    volumes:
      - inventario_dataprotection_keys:/app/dataprotection-keys  # ⚠️ Prefijo diferente
      - inventario_uploads:/app/wwwroot/uploads  # ⚠️ Prefijo diferente
    ports:
      - "8082:8080"  # ⚠️ Puerto diferente (8082)
    networks:
      - inventario_net  # ⚠️ Prefijo diferente

volumes:
  inventario_postgres_data:  # ⚠️ Prefijo diferente
  inventario_dataprotection_keys:  # ⚠️ Prefijo diferente
  inventario_uploads:  # ⚠️ Prefijo diferente

networks:
  inventario_net:  # ⚠️ Prefijo diferente
```

**📌 Regla de Oro:**
> **TODO debe llevar prefijo del nombre del proyecto** (contenedores, volúmenes, redes)

---

### 18.5 Ajuste #2: Puertos Distintos

#### Opción A: Puertos Directos (Simple, para pruebas)

**Mapeo de puertos:**

| Aplicación | Puerto Host | Puerto Contenedor | URL |
|------------|-------------|-------------------|-----|
| CarnetQR | 8081 | 8080 | `http://164.68.99.83:8081` |
| Inventario | 8082 | 8080 | `http://164.68.99.83:8082` |
| CRM | 8083 | 8080 | `http://164.68.99.83:8083` |

**✅ Ventajas:** Configuración simple, no requiere componentes adicionales  
**❌ Desventajas:** URLs poco amigables, no recomendado para producción

---

#### Opción B: Reverse Proxy con Nginx (Recomendado para Producción)

**Arquitectura:**

```
Internet
    ↓
Nginx (puerto 80/443)
    ├─→ carnetqr.ejemplo.com → localhost:8081 (CarnetQR)
    ├─→ inventario.ejemplo.com → localhost:8082 (Inventario)
    └─→ crm.ejemplo.com → localhost:8083 (CRM)
```

**Configuración básica de Nginx para una aplicación:**

```nginx
server {
    listen 80;
    server_name carnetqr.ejemplo.com;

    location / {
        proxy_pass http://localhost:8081;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

**✅ Ventajas:** URLs amigables, HTTPS fácil con Let's Encrypt, profesional

---

### 18.6 Ajuste #3: Cookies Únicas (MUY IMPORTANTE)

**Problema:** Si dos aplicaciones usan el mismo nombre de cookie, pueden ocurrir sesiones cruzadas.

**Solución:** Nombre de cookie único por aplicación.

#### Program.cs - CarnetQR

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    // ... configuración normal
    
    // ⚠️ IMPORTANTE: Nombre único por aplicación
    options.Cookie.Name = ".CarnetQR.Auth";
    options.Cookie.HttpOnly = true;
});
```

#### Program.cs - Inventario

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    // ... configuración normal
    
    // ⚠️ Nombre diferente
    options.Cookie.Name = ".Inventario.Auth";
    options.Cookie.HttpOnly = true;
});
```

**📌 Regla:** Cada aplicación debe tener un nombre de cookie único.

---

### 18.7 Ajuste #4: DataProtection con Nombre de Aplicación

#### Program.cs - CarnetQR

```csharp
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("CarnetQR")  // ⚠️ Único
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
```

#### Program.cs - Inventario

```csharp
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("Inventario")  // ⚠️ Único
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
```

---

### 18.8 Verificar Múltiples Aplicaciones

#### Ver todos los contenedores

```bash
docker ps

# Deberías ver algo como:
# carnetqr_web        Up 2 hours    0.0.0.0:8081->8080/tcp
# carnetqr_postgres   Up 2 hours    0.0.0.0:5432->5432/tcp
# inventario_web      Up 1 hour     0.0.0.0:8082->8080/tcp
# inventario_postgres Up 1 hour     0.0.0.0:5433->5432/tcp
```

#### Ver todos los volúmenes

```bash
docker volume ls

# Deberías ver algo como:
# aspnet_carnetqr_postgres_data
# aspnet_carnetqr_dataprotection_keys
# aspnet_inventario_postgres_data
# aspnet_inventario_dataprotection_keys
```

---

### 18.9 Gestión Individual de Aplicaciones

```bash
# CarnetQR
cd /opt/apps/carnetqr
docker compose restart        # Reiniciar
docker compose logs -f web   # Ver logs
docker compose down          # Detener
docker compose up -d --build # Rebuild

# Inventario
cd /opt/apps/inventario
docker compose restart
# ... etc
```

---

### 18.10 Consideraciones de Recursos

#### Recursos por aplicación (aproximado)

| Recurso | PostgreSQL | ASP.NET Web | Total por App |
|---------|------------|-------------|---------------|
| RAM | 256-512 MB | 256-512 MB | 512 MB - 1 GB |
| CPU | 0.5 core | 0.5-1 core | 1-1.5 cores |
| Disco | 1-10 GB | 500 MB - 2 GB | 1.5 - 12 GB |

#### Recomendaciones por número de aplicaciones

| Aplicaciones | RAM Mínima | CPU Mínima | Disco Mínimo |
|--------------|------------|------------|--------------|
| 1-2 apps | 2 GB | 2 cores | 20 GB |
| 3-5 apps | 4 GB | 4 cores | 40 GB |
| 6-10 apps | 8 GB | 6 cores | 80 GB |

**⚠️ Importante:** Monitorear uso de recursos con `docker stats`

---

### 18.11 Checklist para Nueva Aplicación

Al agregar una nueva aplicación, verificar:

- [ ] **Directorio único** en `/opt/apps/NOMBRE_APP`
- [ ] **Nombres de contenedores únicos** con prefijo `NOMBRE_APP_`
- [ ] **Nombres de volúmenes únicos** con prefijo `NOMBRE_APP_`
- [ ] **Nombre de red único** con prefijo `NOMBRE_APP_`
- [ ] **Puerto único** (8081, 8082, 8083, etc.)
- [ ] **Puerto PostgreSQL único** (si se expone: 5432, 5433, 5434, etc.)
- [ ] **Cookie.Name único** en Program.cs: `".NOMBRE_APP.Auth"`
- [ ] **SetApplicationName único** en DataProtection
- [ ] **Archivo .env independiente** con credenciales únicas
- [ ] **Script de despliegue adaptado** con nombres correctos
- [ ] **Nginx configurado** (si usas reverse proxy)
- [ ] **DNS configurado** (si usas dominios)

---

### 18.12 Resumen de Buenas Prácticas Multi-App

#### ✅ HACER:

1. **Usar prefijos consistentes** en todo (contenedores, volúmenes, redes)
2. **Puertos únicos** para cada aplicación
3. **Cookies únicas** con nombre del proyecto
4. **DataProtection con nombre único**
5. **Estructura de directorios organizada** (`/opt/apps/NOMBRE`)
6. **Reverse proxy** para producción (Nginx)
7. **Backups independientes** por aplicación
8. **Monitoreo de recursos** con `docker stats`
9. **Documentar** qué puerto usa cada app
10. **Scripts de despliegue** independientes

#### ❌ NO HACER:

1. ❌ Usar nombres genéricos (`web`, `postgres`, `db`)
2. ❌ Compartir volúmenes entre aplicaciones
3. ❌ Usar el mismo puerto para múltiples apps
4. ❌ Compartir redes Docker (sin justificación)
5. ❌ Cookies con el mismo nombre
6. ❌ Mezclar archivos de diferentes apps en un directorio
7. ❌ Usar `docker compose down -v` sin backup
8. ❌ Olvidar documentar la configuración
9. ❌ No monitorear recursos
10. ❌ Exponer PostgreSQL sin necesidad

---

### 18.13 Migración de Aplicación Única a Multi-App

Si ya tienes una aplicación corriendo y quieres agregar otra:

#### Paso 1: Renombrar aplicación existente

```bash
# Detener aplicación actual
cd /opt/apps/aspnet
docker compose down

# Crear nuevo directorio con nombre específico
sudo mkdir -p /opt/apps/carnetqr
sudo mv /opt/apps/aspnet/* /opt/apps/carnetqr/
sudo rmdir /opt/apps/aspnet

# Actualizar docker-compose.yml con prefijos
cd /opt/apps/carnetqr
# Editar docker-compose.yml (agregar prefijos "carnetqr_")

# Actualizar Program.cs
# - Cookie.Name = ".CarnetQR.Auth"
# - SetApplicationName("CarnetQR")

# Levantar con nueva configuración
docker compose up -d --build
```

#### Paso 2: Agregar segunda aplicación

Seguir la guía completa con los ajustes de nombres únicos.

---

## 🎯 Conclusión del Capítulo 18

### ✅ Evaluación de la Guía para Multi-App

| Criterio | Estado | Notas |
|----------|--------|-------|
| **Repetible** | ✅ | Plantilla reutilizable para N aplicaciones |
| **Escalable** | ✅ | Estructura soporta crecimiento |
| **Aislamiento** | ✅ | Cada app independiente |
| **Seguridad** | ✅ | Cookies y DataProtection únicos |
| **Mantenible** | ✅ | Updates sin afectar otras apps |
| **Profesional** | ✅ | Arquitectura nivel producción |

### 📊 Comparación: Antes vs Después

| Aspecto | Antes (Guía Original) | Después (Con Cap. 18) |
|---------|----------------------|----------------------|
| Apps soportadas | 1 | Ilimitadas |
| Conflictos | Posibles | Ninguno |
| Escalabilidad | Limitada | Total |
| Complejidad | Baja | Media |
| Producción real | Sí (1 app) | Sí (Multi-app) |

### 🚀 Nivel Alcanzado

Con esta guía completa (capítulos 1-18), ahora tienes:

✅ **Nivel Básico:** Despliegue de una aplicación ASP.NET Core  
✅ **Nivel Intermedio:** Multi-app en un VPS  
✅ **Nivel Avanzado:** Arquitectura escalable con Nginx  
✅ **Nivel Profesional:** Scripts de automatización  
✅ **Nivel Empresa:** Gestión de múltiples proyectos  

**👉 Este es el estándar oficial de despliegue para tu equipo.**

---

**Fecha de Actualización:** 17 de Enero, 2026  
**Versión:** 2.0 - Multi-App Edition  
**Aplicación de Referencia:** CarnetQR Platform  
**Autor:** Documentación Generada para Despliegues Futuros

---

## 📚 Referencias

- [Documentación oficial de Docker](https://docs.docker.com/)
- [Documentación oficial de Docker Compose](https://docs.docker.com/compose/)
- [ASP.NET Core en Docker](https://docs.microsoft.com/es-es/aspnet/core/host-and-deploy/docker/)
- [DataProtection en ASP.NET Core](https://docs.microsoft.com/es-es/aspnet/core/security/data-protection/)
- [PostgreSQL Docker](https://hub.docker.com/_/postgres)
- [Nginx como Reverse Proxy](https://docs.nginx.com/nginx/admin-guide/web-server/reverse-proxy/)
- [Let's Encrypt y Certbot](https://certbot.eff.org/)
