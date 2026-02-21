# Configuración FixHub - Despliegue en VPS

## Dominio autonomousflow.lat — Cómo está dividido

El dominio base es **autonomousflow.lat**. Cada aplicación tiene un subdominio que apunta a la misma IP (164.68.99.83). Nginx usa el `Host` para enrutar a cada app.

| Subdominio | URL | Puerto | Aplicación |
|------------|-----|--------|------------|
| carnet | https://carnet.autonomousflow.lat | 80 | CarnetQR |
| travel | https://travel.autonomousflow.lat | 8082 | PanamaTravelHub |
| n8n | https://n8n.autonomousflow.lat | 8083 | n8n |
| fixhub | https://fixhub.autonomousflow.lat | 8081 | FixHub |

**En el proveedor DNS (donde gestionas autonomousflow.lat):** añadir un registro **A** para cada subdominio con la IP **164.68.99.83** (igual que carnet, travel, n8n).

Ejemplo para fixhub:
- Tipo: **A**
- Nombre: **fixhub** (o fixhub.autonomousflow.lat, según el panel)
- Valor / IP: **164.68.99.83**

## HTTPS para FixHub

Tras crear el registro DNS de fixhub, ampliar el certificado SSL:
```bash
ssh root@164.68.99.83
certbot certonly --nginx -d autonomousflow.lat -d carnet.autonomousflow.lat -d n8n.autonomousflow.lat -d travel.autonomousflow.lat -d fixhub.autonomousflow.lat --expand --non-interactive --agree-tos
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

- FixHub usa **solo** puerto 8081
- No modifica puertos, redes ni volúmenes de CarnetQR, PanamaTravelHub ni n8n
- El script muestra el estado de todas las apps antes y después del deploy

---

## Resultado implementación FixHub (20-feb-2026)

### Estado del dominio
| Elemento | Estado |
|----------|--------|
| DNS | **Pendiente** (fixhub.autonomousflow.lat → NXDOMAIN) |
| Nginx config | **OK** (`/etc/nginx/sites-available/fixhub.autonomousflow.lat`) |
| Proxy puerto 8081 | **OK** (HTTP 302 desde Kestrel) |
| SSL | **Pendiente** (cert actual no incluye fixhub) |

### Archivos creados
- `/etc/nginx/sites-available/fixhub.autonomousflow.lat` (HTTP 80)
- Symlink: `/etc/nginx/sites-enabled/fixhub.autonomousflow.lat`

### Próximos pasos
1. **Crear registro DNS A** en el proveedor:
   - Tipo: A
   - Nombre: `fixhub` (o `fixhub.autonomousflow.lat` según el panel)
   - Valor: `164.68.99.83`
2. Cuando DNS resuelva, ampliar certificado:
   ```bash
   certbot certonly --nginx -d autonomousflow.lat -d carnet.autonomousflow.lat -d n8n.autonomousflow.lat -d travel.autonomousflow.lat -d fixhub.autonomousflow.lat --expand --non-interactive --agree-tos
   systemctl reload nginx
   ```
3. Probar en navegador: `https://fixhub.autonomousflow.lat`

### Problemas detectados
- Ninguno. Configuración aplicada sin errores.

### Recomendaciones
- **Headers de seguridad:** Añadir `X-Real-IP`, `X-Forwarded-For`, `X-Forwarded-Proto` en el bloque `location` (certbot los gestionará en HTTPS)
- **Gzip:** Ya incluido en Nginx por defecto; verificar `gzip on`
- **Caching:** Considerar `proxy_cache_path` para assets estáticos
