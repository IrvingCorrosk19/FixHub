# Configuración FixHub - Despliegue en VPS

**Para continuar paso a paso:** ver **`PASOS_PARA_CONTINUAR.md`** (desplegar → configurar HTTPS → corregir 502/SSL → pruebas).

---

## Dominio autonomousflow.lat — Cómo está dividido

El dominio base es **autonomousflow.lat**. Cada aplicación tiene un subdominio que apunta a la misma IP (164.68.99.83). Nginx usa el `Host` para enrutar a cada app.

| Subdominio | URL | Puerto | Aplicación |
|------------|-----|--------|------------|
| carnet | https://carnet.autonomousflow.lat | 80 | CarnetQR |
| travel | https://travel.autonomousflow.lat | 8082 | PanamaTravelHub |
| n8n | https://n8n.autonomousflow.lat | 8083 | n8n |
| fixhub | https://fixhub.autonomousflow.lat | 8081 | FixHub |

**En el proveedor DNS (donde gestionas autonomousflow.lat):** cada subdominio debe tener un registro **A** con la IP **164.68.99.83**.

**FixHub (subdominio fixhub):**
- Tipo: **A**
- Nombre: **fixhub** (o fixhub.autonomousflow.lat, según el panel)
- Valor / IP: **164.68.99.83**
- Estado: configurado (A record fixhub → 164.68.99.83)

## HTTPS para FixHub

FixHub usa la **misma estructura** que carnet, travel y n8n:
- **HTTP (80):** `server_name fixhub.autonomousflow.lat` con `location /.well-known/acme-challenge/` (root `/var/www/certbot`) para certbot.
- **HTTPS (443):** proxy a `http://127.0.0.1:8081`.

Configuración recomendada (scripts que aplican este patrón):
- **`configurar-fixhub-dominio.ps1`** — Añade bloques HTTP + HTTPS y ejecuta **certbot --webroot** (no --nginx) para evitar 404 en acme-challenge.
- **`corregir-502-y-https-fixhub.ps1`** — Misma lógica: webroot + bloques como los otros subdominios.

Ampliar certificado manualmente (en VPS, con `/var/www/certbot` y bloque HTTP ya configurados):
```bash
certbot certonly --webroot -w /var/www/certbot -d autonomousflow.lat -d carnet.autonomousflow.lat -d n8n.autonomousflow.lat -d travel.autonomousflow.lat -d fixhub.autonomousflow.lat --expand --non-interactive --agree-tos
systemctl reload nginx
```

## Resumen de puertos (sin conflictos)

| Aplicación     | Puerto | Directorio             | Contenedores        |
|----------------|--------|------------------------|---------------------|
| **CarnetQR**   | 80     | /opt/apps/aspnet       | carnetqr_*          |
| **PanamaTravelHub** | 8082 | /opt/apps/panamatravelhub | panamatravelhub_* |
| **n8n**        | 8083   | /opt/apps/n8n          | n8n_*               |
| **FixHub**     | 8081   | /opt/apps/fixhub       | fixhub_*            |

## Aislamiento FixHub

- **Red:** fixhub_net (aislada)
- **Volúmenes:** fixhub_postgres_data, fixhub_dataprotection_keys
- **PostgreSQL:** NO expuesto externamente (solo red interna)
- **Prefijo:** fixhub_ en todos los recursos

## Desplegar FixHub

```powershell
cd "C:\Proyectos\FixHub\src\Com"
.\deploy-fixhub.ps1
```

**Requisitos:**
- Repositorio FixHub clonado en el VPS en `/opt/apps/fixhub`
- Plink (PuTTY) en `C:\Program Files\PuTTY\plink.exe`
- Credenciales SSH configuradas en el script

## Archivos creados

| Archivo | Descripción |
|---------|-------------|
| `fixhub/docker-compose.yml` | Compose de referencia (context desde Com/fixhub) |
| `fixhub/env.example.txt` | Ejemplo de variables de entorno |
| `fixhub/nginx-fixhub.conf` | Referencia: bloques HTTP (80) + HTTPS (443) como los otros subdominios |
| `fixhub/nginx-fixhub-http.conf` | Solo HTTP (80) con `/.well-known/acme-challenge/` para certbot |
| `fixhub/nginx-fixhub-https.conf` | Solo HTTPS (443) proxy a 8081 |
| `configurar-fixhub-dominio.ps1` | Configura dominio + SSL (webroot, igual que carnet/travel/n8n) |
| `corregir-502-y-https-fixhub.ps1` | Corrige 502 y "No seguro" (webroot + nginx) |
| `PASOS_PARA_CONTINUAR.md` | Guía paso a paso: desplegar, HTTPS, corregir, pruebas |
| `AUDIT_FIXHUB_HTTPS_REPORT.md` | Auditoría DevOps (DNS, certs, Nginx): comandos de diagnóstico sin aplicar cambios |
| `deploy-fixhub.ps1` | Script de despliegue por SSH |
| `../docker-compose.yml` | Compose en raíz del repo (usado en deploy) |

## Variables .env (servidor)

Generar en `/opt/apps/fixhub/.env`:

```
POSTGRES_DB=FixHub
POSTGRES_USER=fixhubuser
POSTGRES_PASSWORD=<contraseña_segura>

JWT_SECRET_KEY=<min_32_caracteres>

WEB_ORIGIN=https://fixhub.autonomousflow.lat

ASPNETCORE_ENVIRONMENT=Production
```

## Verificación post-deploy

```bash
docker ps --filter name=fixhub
curl http://164.68.99.83:8081
```

## IMPORTANTE: No afectar otros servicios

| Aplicación       | Puerto en VPS | Uso                          |
|------------------|---------------|------------------------------|
| CarnetQR         | 80            | carnet.autonomousflow.lat   |
| PanamaTravelHub  | 8082          | travel.autonomousflow.lat   |
| n8n              | 8083          | n8n.autonomousflow.lat      |
| **FixHub**       | **8081**      | fixhub.autonomousflow.lat   |

- FixHub usa **solo** puerto **8081** (fixhub_web: 8081:8080).
- No se modifican puertos, redes ni volúmenes de CarnetQR, PanamaTravelHub ni n8n.
- Nginx enruta por `server_name`: `fixhub.autonomousflow.lat` → `proxy_pass http://127.0.0.1:8081`.

### Verificación rápida en VPS

```bash
# FixHub en 8081
curl -sI http://127.0.0.1:8081 | head -1

# Contenedores solo fixhub_*
docker ps --filter name=fixhub --format 'table {{.Names}}\t{{.Ports}}'
```

---

## Resultado implementación FixHub (20-feb-2026)

### Estado del dominio
| Elemento | Estado |
|----------|--------|
| DNS | **OK** — A record `fixhub` → 164.68.99.83 |
| Nginx config | **OK** — proxy a `http://127.0.0.1:8081` (ver nginx-fixhub.conf o autonomousflow) |
| Puerto FixHub | **8081** (docker-compose: fixhub_web 8081:8080; no conflictos con 80, 8082, 8083) |
| SSL | Ampliar cert con `fixhub.autonomousflow.lat` (certbot --expand) cuando DNS resuelva |

### Archivos creados
- `/etc/nginx/sites-available/fixhub.autonomousflow.lat` (HTTP 80)
- Symlink: `/etc/nginx/sites-enabled/fixhub.autonomousflow.lat`

### Completar FixHub (qué hacer ahora)

| Paso | Acción | Dónde |
|------|--------|--------|
| 1 | Desplegar código y contenedores | `.\deploy-fixhub.ps1` (desde `src\Com`) |
| 2 | Configurar dominio + HTTPS (primera vez) | `.\configurar-fixhub-dominio.ps1` |
| 3 | Si aparece 502 o "No seguro" | `.\corregir-502-y-https-fixhub.ps1` |
| 4 | Probar | https://fixhub.autonomousflow.lat |

**Requisitos en tu PC:** PuTTY (plink + pscp) en `C:\Program Files\PuTTY\`, SSH al VPS (root@164.68.99.83).  
**En el VPS:** FixHub en `/opt/apps/fixhub`, Docker, Nginx, certbot; resto de apps (carnet, travel, n8n) sin cambios.

### Próximos pasos (tras A record configurado)
1. **DNS A** ya configurado: `fixhub` → `164.68.99.83`.
2. Configurar dominio y SSL con el script (recomendado):
   ```powershell
   cd C:\Proyectos\FixHub\src\Com
   .\configurar-fixhub-dominio.ps1
   ```
   O en el VPS, con bloque HTTP ya aplicado (acme-challenge):
   ```bash
   certbot certonly --webroot -w /var/www/certbot -d autonomousflow.lat -d carnet.autonomousflow.lat -d n8n.autonomousflow.lat -d travel.autonomousflow.lat -d fixhub.autonomousflow.lat --expand --non-interactive --agree-tos
   systemctl reload nginx
   ```
3. Probar en navegador: `https://fixhub.autonomousflow.lat`

### Si aparece 502 Bad Gateway o "No seguro" (HTTPS)

1. **502:** Nginx no recibe respuesta del backend. Comprobar que FixHub está en **puerto 8081** (no 8084). Ver script **`corregir-502-y-https-fixhub.ps1`** y doc **`SOLUCION_502_Y_HTTPS_FIXHUB.md`**.
2. **Ejecutar corrección desde Windows (PowerShell):**
   ```powershell
   cd C:\Proyectos\FixHub\src\Com
   .\corregir-502-y-https-fixhub.ps1
   ```
   Esto: asegura 8081 en compose, redeploy, amplía el certificado SSL con fixhub.autonomousflow.lat y recarga nginx.
3. **Probar:** https://fixhub.autonomousflow.lat

### Problemas detectados
- 502 si el backend estaba en 8084 y nginx apunta a 8081; se corrige redeployando con 8081 y/o ejecutando el script anterior.

### Recomendaciones
- **Headers de seguridad:** Añadir `X-Real-IP`, `X-Forwarded-For`, `X-Forwarded-Proto` en el bloque `location` (certbot los gestionará en HTTPS)
- **Gzip:** Ya incluido en Nginx por defecto; verificar `gzip on`
- **Caching:** Considerar `proxy_cache_path` para assets estáticos
