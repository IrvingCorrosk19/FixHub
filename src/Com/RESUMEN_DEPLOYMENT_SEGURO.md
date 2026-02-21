# 📋 Resumen de Deployment Seguro - CarnetQR Platform

## 🎯 Objetivo
Desplegar la aplicación CarnetQR Platform en el VPS sin afectar otras aplicaciones existentes.

---

## ✅ Cambios Realizados para Aislamiento

### 1. **Docker Compose - Aislamiento Completo**

#### Contenedores con nombres únicos:
- `carnetqr_postgres` - PostgreSQL 15
- `carnetqr_web` - Aplicación ASP.NET Core

#### Volúmenes con nombres únicos:
- `carnetqr_postgres_data` - Datos de PostgreSQL
- `carnetqr_dataprotection_keys` - Claves de DataProtection

#### Red aislada:
- `carnetqr_net` - Red interna solo para esta aplicación

#### Puertos:
- **Aplicación Web:** `8001:8080` (cambió de 80 a 8001 para evitar conflictos)
- **PostgreSQL:** NO expuesto externamente (solo red interna)

### 2. **Health Checks**
- PostgreSQL tiene health check para asegurar que esté listo antes de iniciar la aplicación

---

## 🚀 Script de Deployment Completo

### Archivo: `Com/deploy-completo.ps1`

Este script realiza:

1. ✅ **Verificación de conflictos** - Verifica puertos y contenedores existentes
2. ✅ **Actualización del repositorio** - `git pull` desde GitHub
3. ✅ **Verificación de archivos** - Dockerfile, docker-compose.yml, .env
4. ✅ **Creación/Actualización de .env** - Variables de entorno
5. ✅ **Backup de base de datos** - Si existe, hace backup antes de actualizar
6. ✅ **Detención de contenedores** - Detiene contenedores existentes
7. ✅ **Build y deployment** - Construye y levanta contenedores
8. ✅ **Espera de PostgreSQL** - Espera hasta que PostgreSQL esté listo
9. ✅ **Verificación de contenedores** - Verifica que estén corriendo
10. ✅ **Verificación de migraciones** - Verifica que las migraciones se aplicaron
11. ✅ **Verificación de tablas** - Verifica que las tablas se crearon

---

## 📝 Configuración de Puertos

### Antes (Podría conflictuar):
```yaml
ports:
  - "80:8080"      # Conflicto con otras apps
  - "5432:5432"    # Conflicto con otros PostgreSQL
```

### Ahora (Aislado):
```yaml
ports:
  - "8001:8080"    # Puerto único, sin conflictos
  # PostgreSQL NO expuesto externamente
```

---

## 🔒 Seguridad y Aislamiento

### ✅ Aislamiento Completo:
- **Red propia:** `carnetqr_net` (bridge, aislada)
- **Volúmenes propios:** Prefijo `carnetqr_` para evitar conflictos
- **Contenedores propios:** Prefijo `carnetqr_` para identificación única
- **PostgreSQL interno:** Solo accesible desde la red interna

### ✅ No Afecta Otras Aplicaciones:
- No usa puerto 80 (usa 8001)
- No expone PostgreSQL externamente
- Nombres únicos en todos los recursos
- Red aislada

---

## 🗄️ Base de Datos

### Migraciones Automáticas:
- Las migraciones se aplican automáticamente al iniciar la aplicación
- Se ejecutan en `DbInitializer.InitializeAsync()` en `Program.cs`
- Incluye seeding de roles, tipos de institución y usuario SuperAdmin

### Backup Automático:
- El script `deploy-completo.ps1` hace backup antes de actualizar
- Backups guardados en: `/opt/apps/aspnet/backups/`
- Formato: `carnetqrdb_backup_YYYYMMDD_HHMMSS.sql`

---

## 📊 Estructura en el VPS

```
/opt/apps/aspnet/
├── Dockerfile
├── docker-compose.yml
├── .env
├── backups/
│   └── carnetqrdb_backup_*.sql
├── CarnetQRPlatform.sln
├── CarnetQRPlatform.Application/
├── CarnetQRPlatform.Domain/
├── CarnetQRPlatform.Infrastructure/
└── CarnetQRPlatform.Web/
```

---

## 🚀 Cómo Desplegar

### Opción 1: Deployment Completo (Recomendado)
```powershell
cd "C:\Proyectos\CarnetQR Platform\Com"
.\deploy-completo.ps1
```

### Opción 2: Deployment Simple
```powershell
cd "C:\Proyectos\CarnetQR Platform\Com"
.\deploy-docker.ps1
```

### Opción 3: Rebuild Completo
```powershell
cd "C:\Proyectos\CarnetQR Platform\Com"
.\rebuild-deploy.ps1
```

---

## 🔍 Verificaciones Post-Deployment

### 1. Verificar Contenedores:
```bash
docker ps --filter "name=carnetqr"
```

### 2. Verificar Logs:
```bash
docker logs -f carnetqr_web
docker logs -f carnetqr_postgres
```

### 3. Verificar Base de Datos:
```bash
docker exec carnetqr_postgres psql -U carnetqruser -d carnetqrdb -c "\dt"
```

### 4. Verificar Migraciones:
```bash
docker exec carnetqr_postgres psql -U carnetqruser -d carnetqrdb -c "SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC;"
```

### 5. Acceder a la Aplicación:
- URL: `http://164.68.99.83:8001`
- Usuario SuperAdmin: `admin@qlservices.com`
- Contraseña: `Admin@123456`

---

## ⚠️ Importante

### Puerto de la Aplicación:
- **Antes:** `http://164.68.99.83` (puerto 80)
- **Ahora:** `http://164.68.99.83:8001` (puerto 8001)

### PostgreSQL:
- **NO está expuesto externamente**
- Solo accesible desde la red interna `carnetqr_net`
- Para acceso externo, usar: `docker exec -it carnetqr_postgres psql -U carnetqruser -d carnetqrdb`

---

## 🔧 Comandos Útiles

### Ver estado de contenedores:
```bash
docker ps --filter "name=carnetqr"
```

### Ver logs en tiempo real:
```bash
docker logs -f carnetqr_web
docker logs -f carnetqr_postgres
```

### Detener aplicación:
```bash
cd /opt/apps/aspnet
docker compose down
```

### Reiniciar aplicación:
```bash
cd /opt/apps/aspnet
docker compose restart
```

### Ver volúmenes:
```bash
docker volume ls | grep carnetqr
```

### Ver redes:
```bash
docker network ls | grep carnetqr
```

### Backup manual de base de datos:
```bash
docker exec carnetqr_postgres pg_dump -U carnetqruser carnetqrdb > backup_$(date +%Y%m%d_%H%M%S).sql
```

### Restaurar backup:
```bash
docker exec -i carnetqr_postgres psql -U carnetqruser -d carnetqrdb < backup_YYYYMMDD_HHMMSS.sql
```

---

## ✅ Checklist de Deployment

- [x] Docker Compose configurado con nombres únicos
- [x] Puerto cambiado a 8001 (sin conflictos)
- [x] PostgreSQL no expuesto externamente
- [x] Red aislada creada
- [x] Volúmenes con nombres únicos
- [x] Health checks configurados
- [x] Script de deployment completo creado
- [x] Backup automático implementado
- [x] Migraciones automáticas configuradas

---

## 📅 Fecha de Actualización
**Fecha:** 28 de Enero, 2026  
**Estado:** ✅ LISTO PARA DESPLEGAR DE FORMA SEGURA

---

## ✨ Notas Finales

- ✅ **Aislamiento completo:** No afecta otras aplicaciones
- ✅ **Backup automático:** Protege datos existentes
- ✅ **Migraciones automáticas:** Base de datos siempre actualizada
- ✅ **Health checks:** Asegura que servicios estén listos
- ✅ **Puerto único:** Sin conflictos con otras apps

**Estado General: ✅ DEPLOYMENT SEGURO CONFIGURADO**
