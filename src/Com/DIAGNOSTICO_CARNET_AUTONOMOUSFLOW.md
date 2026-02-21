# Diagnóstico: https://carnet.autonomousflow.lat/ no levanta

**Fecha:** 4 de febrero de 2026  
**Dominio:** https://carnet.autonomousflow.lat/  
**VPS:** 164.68.99.83  

---

## Conclusión: la aplicación SÍ está levantada; el fallo es de DNS

El sitio **no responde** en el navegador porque el dominio **carnet.autonomousflow.lat** apunta a **otra IP**, no a la del VPS donde está desplegada la app.

---

## 1. Estado en el VPS (164.68.99.83)

| Comprobación | Resultado |
|--------------|-----------|
| Contenedor `carnetqr_web` | Up 5 days, puerto **80→8080** |
| Contenedor `carnetqr_postgres` | Up 5 days, healthy |
| App responde en `http://127.0.0.1:80/` | 302 → Login (correcto) |
| Nginx | Activo; `carnet.autonomousflow.lat` → `proxy_pass http://127.0.0.1:80` |
| Certificado SSL | Incluye `carnet.autonomousflow.lat`, válido ~73 días |
| Prueba interna HTTPS (Host: carnet.autonomousflow.lat) | 302 → `/Account/Login` (correcto) |

La aplicación y nginx en **164.68.99.83** están bien configurados y responden.

---

## 2. Causa del problema: DNS

Resolución actual del dominio:

- **carnet.autonomousflow.lat** → **198.54.117.242**
- **VPS donde está la app** → **164.68.99.83**

Al abrir https://carnet.autonomousflow.lat/, el navegador va a **198.54.117.242**, no al servidor donde corre CarnetQR (**164.68.99.83**). Por eso “no levanta” desde el dominio.

---

## 3. Comprobaciones realizadas

- `docker ps` → `carnetqr_web` en 0.0.0.0:80→8080.
- `curl -sI http://127.0.0.1:80/` → 302, Kestrel.
- Nginx: `server_name carnet.autonomousflow.lat` con `proxy_pass http://127.0.0.1:80`.
- `curl` desde el VPS a `https://127.0.0.1/` con `Host: carnet.autonomousflow.lat` → 302 a `/Account/Login`.
- `certbot certificates` → certificado con `carnet.autonomousflow.lat`.
- `nslookup carnet.autonomousflow.lat` (desde cliente) → **198.54.117.242**.
- **http://164.68.99.83/** → responde y muestra la pantalla de login de CarnetQR.

---

## 4. Solución

Hay que hacer que **carnet.autonomousflow.lat** apunte al VPS correcto:

1. En el proveedor de DNS (donde está gestionado **autonomousflow.lat**):
   - Crear o editar el registro **A** (o CNAME) de **carnet.autonomousflow.lat**.
   - Poner como destino la IP: **164.68.99.83**.

2. Esperar la propagación DNS (minutos a pocas horas).

3. Probar de nuevo: https://carnet.autonomousflow.lat/

Mientras tanto, la app sigue accesible por IP:

- **http://164.68.99.83/** (sin HTTPS).

---

## 5. Resumen

- La aplicación **sí levanta** en el VPS 164.68.99.83 (contenedor, nginx y SSL correctos).
- **No levanta** en https://carnet.autonomousflow.lat/ porque el dominio apunta a **198.54.117.242** en lugar de **164.68.99.83**.
- **Acción:** Cambiar el DNS de **carnet.autonomousflow.lat** a **164.68.99.83**.
