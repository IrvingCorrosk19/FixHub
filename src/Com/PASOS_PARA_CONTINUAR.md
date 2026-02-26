# FixHub — Pasos para continuar

Todo lo necesario para dejar FixHub operativo en **https://fixhub.autonomousflow.lat** y seguir con pruebas.

---

## 1. Desplegar FixHub en el VPS

Desde tu PC (PowerShell):

```powershell
cd C:\Proyectos\FixHub\src\Com
.\deploy-fixhub.ps1
```

- Requiere: **plink** (PuTTY), repo en el VPS en `/opt/apps/fixhub`, `.env` en el servidor.
- Resultado: contenedores `fixhub_*` en marcha, web en puerto **8081**.

---

## 2. Configurar dominio y HTTPS (primera vez)

```powershell
cd C:\Proyectos\FixHub\src\Com
.\configurar-fixhub-dominio.ps1
```

- Requiere: **pscp** y **plink** (PuTTY), DNS `fixhub` → 164.68.99.83.
- Hace: bloque HTTP (80) con acme-challenge, **certbot --webroot**, bloque HTTPS (443), recarga nginx.
- Resultado: https://fixhub.autonomousflow.lat con certificado válido (sin “No seguro”).

---

## 3. Si aparece 502 Bad Gateway o “No seguro”

```powershell
cd C:\Proyectos\FixHub\src\Com
.\corregir-502-y-https-fixhub.ps1
```

- Asegura puerto 8081, redeploy, bloques nginx (HTTP + HTTPS), certbot --webroot y recarga nginx.
- Ver también: `SOLUCION_502_Y_HTTPS_FIXHUB.md`.

---

## 4. Probar en el navegador

- **URL:** https://fixhub.autonomousflow.lat  
- Debe cargar la app y mostrar conexión segura (candado).

---

## 5. Pruebas funcionales (QA)

- **Matriz de pruebas:** `docs/QA/01_TEST_MATRIX.md`
- **Colección Postman:** `tests/FUNCTIONAL_E2E/postman/FixHub_Functional_E2E.postman_collection.json`
- **Cómo ejecutar:** `docs/QA/02_HOW_TO_RUN.md` (Postman o Newman)
- **Resultados / reporte:** `docs/QA/03_EXECUTION_RESULTS.md`, `docs/QA/FUNCTIONAL_FINAL_REPORT.md`

Requisitos para ejecutar las pruebas: API en marcha (local o VPS), Node.js + Newman si usas CLI.

---

## Resumen de scripts (src/Com)

| Script | Uso |
|--------|-----|
| `deploy-fixhub.ps1` | Subir código y levantar FixHub en el VPS |
| `configurar-fixhub-dominio.ps1` | Primera configuración de dominio + SSL (como carnet/travel/n8n) |
| `corregir-502-y-https-fixhub.ps1` | Corregir 502 o “No seguro” (webroot + nginx) |
| `reset-fixhub-vps.ps1` | Reiniciar/limpiar servicios FixHub en el VPS |

---

## Documentación de referencia

- **Configuración general:** `CONFIGURACION_FIXHUB_VPS.md`
- **502 / HTTPS:** `SOLUCION_502_Y_HTTPS_FIXHUB.md`
- **Vista funcional del sistema (QA):** `docs/QA/00_SYSTEM_FUNCTIONAL_OVERVIEW.md`

Con estos pasos y recursos puedes continuar el despliegue, la configuración y las pruebas de FixHub.
