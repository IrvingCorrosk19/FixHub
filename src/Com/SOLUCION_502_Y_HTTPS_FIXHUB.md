# Solución 502 Bad Gateway y HTTPS (fixhub.autonomousflow.lat)

**Síntomas:** Al abrir `https://fixhub.autonomousflow.lat` aparece **502 Bad Gateway** (nginx) y/o **"No seguro"** en el navegador.

---

## Causas habituales

1. **502:** Nginx hace `proxy_pass` a `http://127.0.0.1:8081` pero no hay nada escuchando en 8081 (contenedores parados o FixHub desplegado con el puerto antiguo 8084).
2. **No seguro:** El certificado SSL no incluye `fixhub.autonomousflow.lat` o el bloque HTTPS de fixhub no está bien configurado.

---

## Pasos a ejecutar EN EL VPS (SSH)

Conéctate al VPS y ejecuta en este orden.

### 1. Comprobar si FixHub está corriendo y en qué puerto

```bash
docker ps -a --filter name=fixhub --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'
```

- Si no hay contenedores o están Exited: hay que levantar FixHub.
- Si ves `0.0.0.0:8084->8080/tcp`: la app está en **8084** pero Nginx apunta a **8081**. Hay que redesplegar con el compose actualizado (puerto 8081).

### 2. Ir al directorio de FixHub y usar el puerto 8081

```bash
cd /opt/apps/fixhub
```

Asegúrate de que el `docker-compose.yml` del repo tiene **8081** para la web (no 8084). Si acabas de hacer pull/clone del repo con los últimos cambios, ya debería ser 8081.

```bash
grep -A1 "ports:" docker-compose.yml
# Debe mostrar: - "8081:8080"
```

Si sigue saliendo 8084, edita el compose y cambia a 8081:

```bash
sed -i 's/"8084:8080"/"8081:8080"/' docker-compose.yml
```

### 3. Levantar o reiniciar FixHub en 8081

```bash
cd /opt/apps/fixhub
docker compose down
docker compose up -d --build
```

Espera ~30 segundos (migrator, luego api y web). Luego:

```bash
docker ps --filter name=fixhub --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'
curl -sI http://127.0.0.1:8081 | head -1
```

Debes ver `HTTP/1.1 302` o `200` desde 127.0.0.1:8081. Si eso funciona, Nginx ya puede hacer proxy correctamente.

### 4. Comprobar Nginx para fixhub

```bash
# Ver si existe el bloque para fixhub
grep -A2 "server_name fixhub" /etc/nginx/sites-available/autonomousflow
# o
grep -l "fixhub.autonomousflow.lat" /etc/nginx/sites-enabled/*
```

Debe haber un `server` con:
- `listen 443 ssl`
- `server_name fixhub.autonomousflow.lat;`
- `proxy_pass http://127.0.0.1:8081;`

Si el bloque está en otro archivo (p. ej. `sites-available/fixhub.autonomousflow.lat`), comprueba que esté enlazado en `sites-enabled` y que use puerto 8081 y SSL.

### 5. Incluir fixhub en el certificado SSL (quitar “No seguro”)

El certificado debe incluir `fixhub.autonomousflow.lat`. Si no está, amplía el cert:

```bash
certbot certonly --nginx \
  -d autonomousflow.lat \
  -d carnet.autonomousflow.lat \
  -d n8n.autonomousflow.lat \
  -d travel.autonomousflow.lat \
  -d fixhub.autonomousflow.lat \
  --expand --non-interactive --agree-tos
```

Luego recarga Nginx:

```bash
nginx -t && systemctl reload nginx
```

### 6. Comprobar en el navegador

- `https://fixhub.autonomousflow.lat` → debe cargar la app y el candado seguro (sin “No seguro”).
- Si sigue 502, revisa `docker logs fixhub_web` y `docker logs fixhub_api` para errores de arranque.

---

## Resumen rápido (copiar/pegar en el VPS)

```bash
cd /opt/apps/fixhub
grep -q '"8081:8080"' docker-compose.yml || sed -i 's/"8084:8080"/"8081:8080"/' docker-compose.yml
docker compose down
docker compose up -d --build
sleep 15
curl -sI http://127.0.0.1:8081 | head -1
certbot certonly --nginx -d autonomousflow.lat -d carnet.autonomousflow.lat -d n8n.autonomousflow.lat -d travel.autonomousflow.lat -d fixhub.autonomousflow.lat --expand --non-interactive --agree-tos
nginx -t && systemctl reload nginx
```

Después prueba de nuevo: **https://fixhub.autonomousflow.lat** (debe responder bien y con HTTPS correcto).

---

FixHub debe usar **el mismo patrón** que carnet, travel y n8n: bloque HTTP (80) con `location /.well-known/acme-challenge/` y **certbot --webroot** (no --nginx). Los scripts `configurar-fixhub-dominio.ps1` y `corregir-502-y-https-fixhub.ps1` ya aplican este patrón.

## Si certbot falla con 404 (acme-challenge)

Si al ampliar el certificado ves **"Invalid response from http://.../.well-known/acme-challenge/...: 404"**, es que Nginx no está sirviendo la ruta que Let's Encrypt usa para validar. Para arreglarlo en el VPS:

1. **Asegurar que en puerto 80 existe un `server`** que responda para `fixhub.autonomousflow.lat` (aunque sea solo para el challenge). Por ejemplo en `/etc/nginx/sites-available/autonomousflow` (o el archivo que use tu sitio):

```nginx
# Bloque temporal solo para certbot (puerto 80)
server {
    listen 80;
    server_name fixhub.autonomousflow.lat;
    location /.well-known/acme-challenge/ {
        root /var/www/certbot;
        allow all;
    }
    location / {
        return 200 'ok';
        add_header Content-Type text/plain;
    }
}
```

2. Crear el directorio y dar permisos:  
   `mkdir -p /var/www/certbot && chmod -R 755 /var/www/certbot`

3. Recargar nginx y volver a ejecutar certbot:  
   `nginx -t && systemctl reload nginx`  
   `certbot certonly --webroot -w /var/www/certbot -d fixhub.autonomousflow.lat --non-interactive --agree-tos`

4. Si ya tienes un certificado que incluye otros dominios y solo quieres añadir fixhub, usa `--expand` con los mismos dominios y el webroot:  
   `certbot certonly --webroot -w /var/www/certbot -d autonomousflow.lat -d carnet.autonomousflow.lat -d n8n.autonomousflow.lat -d travel.autonomousflow.lat -d fixhub.autonomousflow.lat --expand --non-interactive --agree-tos`

5. Recargar nginx de nuevo para que use el certificado nuevo:  
   `systemctl reload nginx`
