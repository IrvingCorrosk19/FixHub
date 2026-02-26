# Diagnóstico: Certbot 404 — Puerto 80 ocupado por Docker

**Fecha:** 26 feb 2026  
**VPS:** 164.68.99.83  

## Hallazgo

Al intentar ampliar el certificado con:

```bash
sudo certbot certonly --webroot -w /var/www/certbot -d ... -d fixhub.autonomousflow.lat --expand ...
```

certbot devuelve **404** para todos los dominios (autonomousflow.lat, carnet, n8n, travel, fixhub).

## Causa raíz

- **El puerto 80 del host está en uso por `docker-proxy`**, no por Nginx.
- Un contenedor (CarnetQR) publica **80:8080** en el host.
- Nginx **no llega a escuchar en el puerto 80** porque ya está ocupado.
- Toda la petición HTTP a :80 la atiende el contenedor (Kestrel), que devuelve 404 para `/.well-known/acme-challenge/`.

Comprobación:

```bash
ss -tlnp | grep ':80 '
# LISTEN ... 0.0.0.0:80 ... users:(("docker-proxy",pid=...))
```

Y una petición a `http://127.0.0.1/.well-known/acme-challenge/testfile -H "Host: fixhub.autonomousflow.lat"` devuelve **404** con cabecera **Server: Kestrel** (la app), no Nginx.

## Qué se hizo en esta sesión (sin romper servicios)

1. **Bloque HTTP para fixhub:** Se añadió en `/etc/nginx/sites-available/fixhub.autonomousflow.lat` un `server { listen 80; server_name fixhub.autonomousflow.lat; location /.well-known/acme-challenge/ { root /var/www/certbot; } }`.
2. **Bloques HTTP para acme en autonomousflow:** Se añadieron al inicio de `/etc/nginx/sites-available/autonomousflow` bloques `listen 80` con `location /.well-known/acme-challenge/` para autonomousflow.lat, carnet, n8n y travel (archivo de referencia: `fixhub/nginx-acme-challenge-blocks.conf`).
3. **Nginx -t y reload:** Correctos, pero Nginx **sigue sin poder escuchar en 80** porque el puerto está tomado por Docker.

## Qué hay que hacer para que certbot funcione

Liberar el **puerto 80** en el host para Nginx y que el tráfico a carnet pase por Nginx:

1. **CarnetQR (u otra app que use 80):** Cambiar el mapeo de puertos para que el contenedor **no** use el puerto 80 del host. Por ejemplo, en el `docker-compose` de esa app:
   - Antes: `ports: - "80:8080"`
   - Después: `ports: - "127.0.0.1:8080:8080"` (o solo otro puerto libre, p. ej. 8080)
2. **Nginx:** Ya tiene configurado para carnet `proxy_pass http://127.0.0.1:80`. Cambiarlo a **proxy_pass http://127.0.0.1:8080** (o el puerto que uses en el paso anterior).
3. Reiniciar el contenedor que antes usaba 80 y comprobar que Nginx escucha en 80: `ss -tlnp | grep ':80 '` debe mostrar **nginx**.
4. Ejecutar de nuevo certbot (webroot) para ampliar el certificado con fixhub y recargar Nginx.

Mientras el puerto 80 lo siga usando Docker, certbot --webroot no podrá validar y seguirá el 404.

---

## Aplicado (26 feb 2026)

1. **CarnetQR (aspnet):** En el VPS se editó `/opt/apps/aspnet/docker-compose.yml`: `ports: - "80:8080"` → `- "127.0.0.1:8080:8080"`. Se ejecutó `docker compose down` y `docker compose up -d` en `/opt/apps/aspnet`.
2. **Nginx:** En `/etc/nginx/sites-available/autonomousflow` se cambió el `proxy_pass` del bloque carnet de `http://127.0.0.1:80` a `http://127.0.0.1:8080`. Se recargó nginx.
3. **Certbot:** Se ejecutó `certbot certonly --webroot -w /var/www/certbot -d ... -d fixhub.autonomousflow.lat --expand` con éxito. El certificado incluye fixhub.autonomousflow.lat en el SAN.
4. **Resultado:** Nginx escucha en 80 y 443; fixhub, carnet, travel y n8n responden correctamente por HTTPS.
