# Configura fixhub.autonomousflow.lat con HTTPS (nginx + certbot)
# Misma estructura que carnet, travel, n8n: HTTP (80) con acme-challenge + certbot --webroot + HTTPS (443)
# Requiere: DNS de fixhub.autonomousflow.lat apuntando a 164.68.99.83

$plink = "C:\Program Files\PuTTY\plink.exe"
$pscp = "C:\Program Files\PuTTY\pscp.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not (Test-Path $pscp)) {
    Write-Host "ERROR: pscp no encontrado en: $pscp" -ForegroundColor Red
    Write-Host "Instala PuTTY (incluye pscp) o copia manualmente al VPS:" -ForegroundColor Yellow
    Write-Host "  - $scriptDir\fixhub\nginx-fixhub-http.conf" -ForegroundColor Gray
    Write-Host "  - $scriptDir\fixhub\nginx-fixhub-https.conf" -ForegroundColor Gray
    Write-Host "  a root@164.68.99.83:/tmp/" -ForegroundColor Gray
    exit 1
}
if (-not (Test-Path $plink)) {
    Write-Host "ERROR: plink no encontrado en: $plink" -ForegroundColor Red
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FIXHUB - Dominio + HTTPS (como carnet/travel/n8n)" -ForegroundColor Cyan
Write-Host "  fixhub.autonomousflow.lat" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "IMPORTANTE: fixhub.autonomousflow.lat debe apuntar a 164.68.99.83 (DNS)" -ForegroundColor Yellow
Write-Host ""

# PASO 0: Crear /var/www/certbot y subir configs nginx (HTTP + HTTPS)
Write-Host "PASO 0: Preparando directorio certbot y configs nginx..." -ForegroundColor Yellow
& $pscp -pw $password -batch "$scriptDir\fixhub\nginx-fixhub-http.conf" "${hostname}:/tmp/nginx-fixhub-http.conf" 2>&1
& $pscp -pw $password -batch "$scriptDir\fixhub\nginx-fixhub-https.conf" "${hostname}:/tmp/nginx-fixhub-https.conf" 2>&1
$cmd0 = "mkdir -p /var/www/certbot && chmod -R 755 /var/www/certbot && echo Certbot_dir_OK"
$r0 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd0 2>&1
Write-Host $r0
Write-Host ""

# PASO 1: Añadir bloque HTTP (puerto 80) con location /.well-known/acme-challenge/ como los otros subdominios
Write-Host "PASO 1: Añadiendo bloque HTTP (acme-challenge) para FixHub..." -ForegroundColor Yellow
$cmd1 = "grep -q 'FixHub HTTP (80) acme-challenge' /etc/nginx/sites-available/autonomousflow || (cat /tmp/nginx-fixhub-http.conf >> /etc/nginx/sites-available/autonomousflow && echo Bloque_HTTP_anadido); nginx -t && systemctl reload nginx && echo NginxOK"
$r1 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd1 2>&1
Write-Host $r1
Write-Host ""

# PASO 2: Ampliar certificado con certbot --webroot (igual que cuando los otros subdominios requieren validación)
Write-Host "PASO 2: Ampliando certificado SSL (certbot --webroot)..." -ForegroundColor Yellow
$cmd2 = "certbot certonly --webroot -w /var/www/certbot -d autonomousflow.lat -d carnet.autonomousflow.lat -d n8n.autonomousflow.lat -d travel.autonomousflow.lat -d fixhub.autonomousflow.lat --expand --non-interactive --agree-tos 2>&1"
$r2 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd2 2>&1
Write-Host $r2
Write-Host ""

# PASO 3: Añadir bloque HTTPS (443) para FixHub si no existe
Write-Host "PASO 3: Añadiendo bloque HTTPS para FixHub..." -ForegroundColor Yellow
$cmd3 = "grep -B5 'proxy_pass http://127.0.0.1:8081' /etc/nginx/sites-available/autonomousflow 2>/dev/null | grep -q 'fixhub.autonomousflow.lat' || (cat /tmp/nginx-fixhub-https.conf >> /etc/nginx/sites-available/autonomousflow && echo Bloque_HTTPS_anadido); nginx -t && systemctl reload nginx && echo Nginx_reload_OK"
$r3 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd3 2>&1
Write-Host $r3
Write-Host ""

# PASO 4: Actualizar .env y reiniciar FixHub
Write-Host "PASO 4: Actualizando FixHub (WEB_ORIGIN)..." -ForegroundColor Yellow
$cmd4 = "cd /opt/apps/fixhub && (grep -q '^WEB_ORIGIN=' .env && sed -i 's|^WEB_ORIGIN=.*|WEB_ORIGIN=https://fixhub.autonomousflow.lat|' .env || echo 'WEB_ORIGIN=https://fixhub.autonomousflow.lat' >> .env) && docker compose up -d api web 2>&1"
$r4 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd4 2>&1
Write-Host $r4
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "  LISTO: https://fixhub.autonomousflow.lat" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""