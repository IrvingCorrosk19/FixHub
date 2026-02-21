# Despliega FixHub en el VPS - NO afecta otras aplicaciones
# Puertos: FixHub=8084 | CarnetQR=8001 | PanamaTravelHub=8082 | n8n=8083

$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"
$remoteDir = "/opt/apps/fixhub"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DESPLIEGUE FIXHUB - VPS" -ForegroundColor Cyan
Write-Host "  Sin afectar CarnetQR, PanamaTravelHub, n8n" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificacion pre-deploy
Write-Host "Estado actual del VPS:" -ForegroundColor Yellow
$cmdCheck = "docker ps --format 'table {{.Names}}\t{{.Ports}}'"
$resultCheck = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCheck 2>&1
Write-Host $resultCheck
Write-Host ""
Write-Host "FixHub usara: puerto 8084, contenedores fixhub_*, red fixhub_net" -ForegroundColor Cyan
Write-Host ""

# PASO 1: Directorio y repo (fuerza actualizacion, descarta cambios locales)
Write-Host "PASO 1: Clonando/actualizando FixHub en $remoteDir..." -ForegroundColor Yellow
$cmd1 = "mkdir -p $remoteDir && cd $remoteDir && if [ -d .git ]; then git fetch origin; git reset --hard origin/main 2>/dev/null || git reset --hard origin/master; else git clone https://github.com/IrvingCorrosk19/FixHub.git .; fi && ls -la docker-compose.yml 2>/dev/null || echo 'FALTA: docker-compose.yml'"
$result1 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd1 2>&1
Write-Host $result1
Write-Host ""

# PASO 2: Crear .env
Write-Host "PASO 2: Creando .env..." -ForegroundColor Yellow
$envContent = @"
POSTGRES_DB=FixHub
POSTGRES_USER=fixhubuser
POSTGRES_PASSWORD=FixHub2024!Secure

JWT_SECRET_KEY=ChangeMeProductionMin32CharsSecretKey!!

WEB_ORIGIN=http://164.68.99.83:8084

ASPNETCORE_ENVIRONMENT=Production
"@
$envB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($envContent))
$cmd2 = "cd $remoteDir && echo '$envB64' | base64 -d > .env && echo '.env creado'"
$result2 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd2 2>&1
Write-Host $result2
Write-Host ""

# PASO 3: Build y deploy (puede tardar 5-10 min la primera vez)
Write-Host "PASO 3: Construyendo y levantando contenedores (5-10 min primera vez)..." -ForegroundColor Yellow
Write-Host "  Si hay timeout, ejecuta: ssh root@164.68.99.83" -ForegroundColor Gray
Write-Host "  Luego: cd $remoteDir && docker compose up -d --build" -ForegroundColor Gray
$cmd3 = "cd $remoteDir && docker compose up -d --build 2>&1"
$result3 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd3 2>&1
Write-Host $result3
Write-Host ""

# PASO 4: Verificar
Write-Host "PASO 4: Verificando FixHub..." -ForegroundColor Yellow
Start-Sleep -Seconds 5
$cmd4 = "docker ps --filter name=fixhub --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'"
$result4 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd4 2>&1
Write-Host $result4
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "  DESPLIEGUE COMPLETADO" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "FixHub:        http://164.68.99.83:8084" -ForegroundColor Green
Write-Host "CarnetQR:      http://164.68.99.83 (puerto 80)" -ForegroundColor Gray
Write-Host "PanamaTravelHub: http://164.68.99.83:8082" -ForegroundColor Gray
Write-Host "n8n:           http://164.68.99.83:8083" -ForegroundColor Gray
Write-Host ""
Write-Host "Comandos utiles:" -ForegroundColor Yellow
Write-Host "  docker logs -f fixhub_web" -ForegroundColor White
Write-Host "  cd $remoteDir && docker compose down" -ForegroundColor White
Write-Host ""
