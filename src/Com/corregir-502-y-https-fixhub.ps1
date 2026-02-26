# Corrige 502 Bad Gateway y HTTPS para fixhub.autonomousflow.lat
# Misma lógica que carnet/travel/n8n: HTTP con acme-challenge + certbot --webroot + HTTPS
# Uso: .\corregir-502-y-https-fixhub.ps1

$plink = "C:\Program Files\PuTTY\plink.exe"
$pscp = "C:\Program Files\PuTTY\pscp.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"
$remoteDir = "/opt/apps/fixhub"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not (Test-Path $pscp)) {
    Write-Host "ERROR: pscp no encontrado en: $pscp" -ForegroundColor Red
    Write-Host "Copia manualmente al VPS nginx-fixhub-http.conf y nginx-fixhub-https.conf a /tmp/" -ForegroundColor Yellow
    exit 1
}
if (-not (Test-Path $plink)) {
    Write-Host "ERROR: plink no encontrado en: $plink" -ForegroundColor Red
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CORRECCION 502 + HTTPS - fixhub.autonomousflow.lat" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Asegurar puerto 8081 en compose y redeploy (una sola linea para evitar CRLF en bash)
Write-Host "PASO 1: Asegurando puerto 8081 y redeploy FixHub..." -ForegroundColor Yellow
$cmd1 = "cd $remoteDir && (grep -q '\"8081:8080\"' docker-compose.yml || sed -i 's/\"8084:8080\"/\"8081:8080\"/' docker-compose.yml) && docker compose down 2>/dev/null; docker compose up -d --build 2>&1"
$r1 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd1 2>&1
Write-Host $r1
Write-Host ""

# 2. Esperar y verificar 8081
Write-Host "PASO 2: Esperando 20s y verificando puerto 8081..." -ForegroundColor Yellow
Start-Sleep -Seconds 20
$cmd2 = "curl -sI http://127.0.0.1:8081 2>&1 | head -1; docker ps --filter name=fixhub --format 'table {{.Names}}\t{{.Ports}}'"
$r2 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd2 2>&1
Write-Host $r2
Write-Host ""

# 2b. Nginx proxy 8081 y bloque HTTP con acme-challenge (como los otros subdominios)
Write-Host "PASO 2b: Nginx proxy 8081 y bloque HTTP (acme-challenge)..." -ForegroundColor Yellow
& $pscp -pw $password -batch "$scriptDir\fixhub\nginx-fixhub-http.conf" "${hostname}:/tmp/nginx-fixhub-http.conf" 2>&1 | Out-Null
& $pscp -pw $password -batch "$scriptDir\fixhub\nginx-fixhub-https.conf" "${hostname}:/tmp/nginx-fixhub-https.conf" 2>&1 | Out-Null
$cmd2b = "sed -i 's/127.0.0.1:8084/127.0.0.1:8081/g' /etc/nginx/sites-available/autonomousflow 2>/dev/null; mkdir -p /var/www/certbot && chmod -R 755 /var/www/certbot; grep -q 'FixHub HTTP (80) acme-challenge' /etc/nginx/sites-available/autonomousflow || (cat /tmp/nginx-fixhub-http.conf >> /etc/nginx/sites-available/autonomousflow); nginx -t && systemctl reload nginx && echo Nginx_reload_OK"
$r2b = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd2b 2>&1
Write-Host $r2b
Write-Host ""

# 3. Ampliar certificado SSL con certbot --webroot (igual que carnet/travel/n8n)
Write-Host "PASO 3: Ampliando certificado SSL (certbot --webroot)..." -ForegroundColor Yellow
$cmd3 = "certbot certonly --webroot -w /var/www/certbot -d autonomousflow.lat -d carnet.autonomousflow.lat -d n8n.autonomousflow.lat -d travel.autonomousflow.lat -d fixhub.autonomousflow.lat --expand --non-interactive --agree-tos 2>&1"
$r3 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd3 2>&1
Write-Host $r3
Write-Host ""

# 4. Añadir bloque HTTPS si falta y recargar nginx
Write-Host "PASO 4: Bloque HTTPS y recargar nginx..." -ForegroundColor Yellow
$cmd4 = "grep -B5 'proxy_pass http://127.0.0.1:8081' /etc/nginx/sites-available/autonomousflow 2>/dev/null | grep -q 'fixhub.autonomousflow.lat' || (cat /tmp/nginx-fixhub-https.conf >> /etc/nginx/sites-available/autonomousflow); nginx -t && systemctl reload nginx && echo Nginx_OK"
$r4 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd4 2>&1
Write-Host $r4
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "  Probar: https://fixhub.autonomousflow.lat" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Si aun ves 502: revisar en VPS: docker logs fixhub_web" -ForegroundColor Gray
