# Auditoría DevOps — fixhub.autonomousflow.lat (HTTPS "No seguro")

**Rol:** DevOps Senior (Nginx, Let's Encrypt, Certbot, Docker, VPS Ubuntu)  
**Tarea:** AUDITORÍA Y DIAGNÓSTICO — sin aplicar cambios.  
**VPS:** 164.68.99.83  
**Subdominios OK:** carnet, travel, n8n (HTTPS válido)  
**Subdominio con fallo:** fixhub (aparece "No seguro")

---

## Cómo usar este informe

- Ejecuta en el VPS (SSH `root@164.68.99.83`) los comandos de cada fase.
- Anota resultados en las tablas o debajo de cada comando.
- Al final, usa la **Fase 6 — Conclusión** para determinar la causa y el comando de corrección (no ejecutado en esta auditoría).

---

## FASE 1 — DNS

**Objetivo:** Comprobar que fixhub.autonomousflow.lat tiene registro A a la misma IP que los demás y no hay NXDOMAIN ni conflicto AAAA.

### 1.1 Comandos a ejecutar (desde tu PC o desde el VPS)

```bash
# Comparar resolución A de fixhub vs carnet (IP esperada: 164.68.99.83)
dig +short A fixhub.autonomousflow.lat
dig +short A carnet.autonomousflow.lat
dig +short A travel.autonomousflow.lat
dig +short A n8n.autonomousflow.lat
```

```bash
# Comprobar que no hay NXDOMAIN
dig fixhub.autonomousflow.lat +noall +answer +authority
```

```bash
# Si no usas IPv6: comprobar que no hay AAAA que apunte a otra IP
dig +short AAAA fixhub.autonomousflow.lat
dig +short AAAA carnet.autonomousflow.lat
```

### 1.2 Qué debe cumplirse

| Comprobación | fixhub.autonomousflow.lat | carnet.autonomousflow.lat | ¿OK? |
|--------------|---------------------------|---------------------------|------|
| Registro A presente | _________________ | _________________ | |
| IP del registro A | _________________ | _________________ | |
| Misma IP que carnet (164.68.99.83) | Sí / No | 164.68.99.83 | |
| NXDOMAIN | No | No | |
| AAAA (si no usas IPv6) | Preferible vacío o mismo host | — | |

### 1.3 Conclusión Fase 1

- **Si fixhub resuelve a otra IP (ej. 198.54.117.242):** el problema puede ser DNS (igual que en DIAGNOSTICO_CARNET: carnet apuntaba a 198.54.117.242).
- **Si fixhub resuelve a 164.68.99.83:** DNS está correcto; seguir a Fase 2 y 3.

**Comando de corrección (solo si el problema es DNS):** En el panel DNS del dominio, poner registro **A** para **fixhub** (o fixhub.autonomousflow.lat) → **164.68.99.83**. No ejecutar nada en el VPS.

---

## FASE 2 — Certificados SSL

**Objetivo:** Ver si existe certificado para fixhub, si está en un SAN, si está expirado o no vinculado en Nginx.

### 2.1 Comandos a ejecutar (en el VPS)

```bash
sudo certbot certificates
```

Anotar salida (sobre todo nombres de certificado, rutas, dominios y fechas de validez).

### 2.2 Verificaciones

| Pregunta | Cómo comprobarlo | Resultado (anotar) |
|----------|------------------|--------------------|
| ¿Existe certificado que liste fixhub.autonomousflow.lat? | Buscar "fixhub.autonomousflow.lat" en la salida de `certbot certificates` | Sí / No |
| ¿Está en un cert SAN (multi-dominio)? | Ver "Domains:" del certificado que usa carnet/travel/n8n | Lista: ____________ |
| ¿Certificado expirado? | Ver "Expiry Date" | Válido / Expirado |
| ¿Ruta del cert que usa Nginx para carnet? | Ver Fase 3 (ssl_certificate en bloque carnet) | ____________ |
| ¿Ruta del cert que usa Nginx para fixhub? | Ver Fase 3 (ssl_certificate en bloque fixhub) | ____________ |

### 2.3 Comparación esperada (según repo)

- **Carnet/travel/n8n:** usan el certificado en `/etc/letsencrypt/live/autonomousflow.lat/` (fullchain.pem + privkey.pem), con **Domains:** que incluyen al menos carnet, travel, n8n y probablemente autonomousflow.lat.
- **FixHub (según repo):** está configurado para usar el **mismo** certificado: `ssl_certificate /etc/letsencrypt/live/autonomousflow.lat/fullchain.pem`.  
  Si ese certificado **no** incluye `fixhub.autonomousflow.lat` en sus SAN, el navegador mostrará "No seguro" (nombre no coincide).

### 2.4 Conclusión Fase 2

- **Si fixhub.autonomousflow.lat NO aparece en ningún "Domains:"** → problema de **certificado no generado/ampliado** para fixhub.
- **Si el cert existe e incluye fixhub pero Nginx usa otra ruta para fixhub** → problema de **certificado no vinculado** (config Nginx incorrecta).
- **Si el cert está expirado** → problema de **renovación**.

**Comando de corrección (solo si el cert no incluye fixhub — NO ejecutar en esta auditoría):**

```bash
# Requiere bloque HTTP (80) con location /.well-known/acme-challenge/ root /var/www/certbot (Fase 3)
sudo certbot certonly --webroot -w /var/www/certbot \
  -d autonomousflow.lat \
  -d carnet.autonomousflow.lat \
  -d n8n.autonomousflow.lat \
  -d travel.autonomousflow.lat \
  -d fixhub.autonomousflow.lat \
  --expand --non-interactive --agree-tos
sudo systemctl reload nginx
```

---

## FASE 3 — Nginx configuración

**Objetivo:** Comparar server_name, listen 443, ssl_certificate, proxy_pass y redirección 80→443 entre carnet (que funciona) y fixhub.

### 3.1 Archivos a revisar en el VPS

```bash
ls -la /etc/nginx/sites-available/
ls -la /etc/nginx/sites-enabled/
```

```bash
# Ver configuración completa que aplica Nginx (incluye includes)
sudo nginx -T 2>/dev/null | grep -A200 "server_name carnet.autonomousflow.lat" | head -80
sudo nginx -T 2>/dev/null | grep -A200 "server_name fixhub.autonomousflow.lat" | head -80
```

O abrir manualmente:

- Archivo(s) que definen **carnet.autonomousflow.lat** (p. ej. `/etc/nginx/sites-available/autonomousflow` o `carnet.autonomousflow.lat`).
- Archivo(s) que definen **fixhub.autonomousflow.lat** (mismo archivo o `fixhub.autonomousflow.lat`).

### 3.2 Tabla comparativa (rellenar con lo que veas en el VPS)

| Elemento | carnet.autonomousflow.lat (OK) | fixhub.autonomousflow.lat (No seguro) | ¿Coinciden / Correcto? |
|----------|--------------------------------|---------------------------------------|-------------------------|
| **server_name** | | | |
| **listen 80** (bloque HTTP) | Sí / No | Sí / No | |
| **listen 443 ssl** (bloque HTTPS) | Sí / No | Sí / No | |
| **ssl_certificate** | (ruta completa) | (ruta completa) | Misma ruta que carnet |
| **ssl_certificate_key** | (ruta completa) | (ruta completa) | Misma ruta que carnet |
| **include options-ssl / letsencrypt** | Sí / No / Ruta | Sí / No / Ruta | |
| **ssl_dhparam** | Sí / No / Ruta | Sí / No / Ruta | |
| **Redirección 80 → 443** | Sí / No | Sí / No | |
| **location /.well-known/acme-challenge/** (solo para certbot) | — | Sí / No, root | Fixhub debe tenerlo para --webroot |
| **proxy_pass** | http://127.0.0.1:80 | http://127.0.0.1:8081 | Fixhub debe ser 8081 |

### 3.3 Configuración esperada para FixHub (según repo)

- **HTTP (80):**  
  `server_name fixhub.autonomousflow.lat;`  
  `location /.well-known/acme-challenge/ { root /var/www/certbot; }`  
  `location / { return 301 https://$host$request_uri; }`

- **HTTPS (443):**  
  `listen 443 ssl http2;`  
  `server_name fixhub.autonomousflow.lat;`  
  `ssl_certificate /etc/letsencrypt/live/autonomousflow.lat/fullchain.pem;`  
  `ssl_certificate_key /etc/letsencrypt/live/autonomousflow.lat/privkey.pem;`  
  `include /etc/letsencrypt/options-ssl-nginx.conf;`  
  `ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem;`  
  `location / { proxy_pass http://127.0.0.1:8081; ... }`

### 3.4 Diferencias a detectar

- Fixhub **sin** bloque `listen 443 ssl` → problema de **configuración Nginx incompleta**.
- Fixhub con **ssl_certificate** apuntando a otro cert (o a un cert que no incluye fixhub) → **certificado no vinculado correctamente**.
- Fixhub **sin** bloque `listen 80` con `location /.well-known/acme-challenge/` → certbot --webroot no puede validar; no se puede ampliar el cert.

**Comando de corrección (solo si falta bloque 443 o está mal — NO ejecutar):** Añadir o corregir el bloque HTTPS de fixhub según `src/Com/fixhub/nginx-fixhub-https.conf` en el archivo que use el sitio (p. ej. `/etc/nginx/sites-available/autonomousflow`) y luego `sudo nginx -t && sudo systemctl reload nginx`.

---

## FASE 4 — Bloque 443 en default o archivo principal

**Objetivo:** Ver si fixhub usa un bloque 443 compartido, uno propio o ninguno.

### 4.1 Comandos

```bash
# Ver todos los server que escuchan 443
sudo nginx -T 2>/dev/null | grep -B2 "listen 443"

# Ver en qué archivo está definido fixhub
grep -r "fixhub.autonomousflow.lat" /etc/nginx/sites-available/
grep -r "fixhub.autonomousflow.lat" /etc/nginx/sites-enabled/
```

### 4.2 Rellenar

| Pregunta | Resultado |
|----------|-----------|
| ¿Existe algún bloque con `server_name fixhub.autonomousflow.lat` y `listen 443`? | Sí / No |
| ¿Ese bloque está en el mismo archivo que carnet (ej. autonomousflow)? | Sí / No |
| ¿O en archivo separado (ej. fixhub.autonomousflow.lat)? | Sí / No |
| ¿El archivo de fixhub está enlazado en sites-enabled? | Sí / No |

### 4.3 Conclusión Fase 4

- **Si fixhub no tiene ningún bloque 443** → problema de **falta de bloque 443** (Nginx no sirve HTTPS para fixhub).
- **Si tiene bloque 443 pero el certificado no incluye fixhub** → problema de certificado (Fase 2).

---

## FASE 5 — Verificación de puertos y backend

**Objetivo:** Comprobar que el backend de FixHub está arriba, responde en localhost y en el puerto correcto (8081).

### 5.1 Comandos (en el VPS)

```bash
# Contenedores FixHub
docker ps --filter name=fixhub --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'

# Algo debe escuchar en 8081 (ej. fixhub_web -> 0.0.0.0:8081->8080/tcp)
ss -tlnp | grep 8081
# o
netstat -tlnp | grep 8081

# Respuesta local
curl -sI http://127.0.0.1:8081/
```

### 5.2 Rellenar

| Comprobación | Resultado |
|--------------|-----------|
| ¿Contenedor fixhub_web (o equivalente) en ejecución? | Sí / No |
| ¿Puerto 8081 expuesto en host (ej. 0.0.0.0:8081)? | Sí / No |
| ¿curl 127.0.0.1:8081 devuelve HTTP 200/302? | Sí / No |
| ¿Firewall bloquea 8081? (solo si aplica) | Revisar ufw/iptables |

### 5.3 Conclusión Fase 5

- Si el backend no responde en 8081, Nginx puede devolver 502; no explica por sí solo "No seguro".  
- "No seguro" suele deberse a certificado o a bloque 443 (Fases 2, 3 y 4).

---

## FASE 6 — Conclusión del diagnóstico

Responde Sí/No según lo que hayas visto en las fases anteriores.

| # | Pregunta | Sí / No | Notas |
|---|----------|---------|--------|
| 1 | ¿El problema es DNS? (fixhub apunta a otra IP o NXDOMAIN) | | |
| 2 | ¿El problema es certificado no generado? (fixhub no está en ningún cert) | | |
| 3 | ¿El problema es certificado no vinculado? (cert incluye fixhub pero Nginx usa otra ruta/otro cert) | | |
| 4 | ¿El problema es configuración Nginx incompleta? (falta bloque 80 o 443, o proxy_pass incorrecto) | | |
| 5 | ¿El problema es renovación fallida? (cert expirado) | | |
| 6 | ¿El problema es conflicto IPv6? (AAAA apunta a otro host) | | |
| 7 | ¿El problema es falta de bloque 443? (no hay server { listen 443 } para fixhub) | | |

### Resumen de causas más probables (según evidencia del repo)

- **Certificado sin fixhub:** En el pasado, `certbot --nginx --expand` falló con 404 en acme-challenge para todos los dominios; si no se ejecutó después `certbot --webroot --expand` con fixhub, el certificado de autonomousflow.lat **no incluye** fixhub.autonomousflow.lat → el navegador muestra "No seguro".
- **Bloque 443 faltante o incorrecto:** Si en el VPS no se añadió el bloque HTTPS de fixhub (o se usa otro certificado), mismo efecto.
- **Bloque 80 sin acme-challenge:** Sin `location /.well-known/acme-challenge/` para fixhub, no se puede ampliar el cert con --webroot.

### Comando exacto recomendado para corregir (NO ejecutado en esta auditoría)

Solo después de confirmar en Fase 2 que el certificado **no** incluye fixhub.autonomousflow.lat, y en Fase 3 que existe bloque HTTP con `location /.well-known/acme-challenge/` y root `/var/www/certbot`:

```bash
sudo certbot certonly --webroot -w /var/www/certbot \
  -d autonomousflow.lat \
  -d carnet.autonomousflow.lat \
  -d n8n.autonomousflow.lat \
  -d travel.autonomousflow.lat \
  -d fixhub.autonomousflow.lat \
  --expand --non-interactive --agree-tos
sudo nginx -t && sudo systemctl reload nginx
```

Si falta el bloque HTTP o el bloque 443 para fixhub, primero hay que añadirlos según:

- `src/Com/fixhub/nginx-fixhub-http.conf`
- `src/Com/fixhub/nginx-fixhub-https.conf`

(o ejecutar los scripts `configurar-fixhub-dominio.ps1` / `corregir-502-y-https-fixhub.ps1` desde Windows, que aplican esa configuración).

---

**Fin del informe de auditoría. No se ha modificado DNS, certificados ni Nginx.**
