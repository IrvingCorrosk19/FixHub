# Elimina FixHub del VPS (contenedores + BD y volúmenes) y lo vuelve a crear de cero.
# NO toca CarnetQR, PanamaTravelHub ni n8n (solo fixhub_* y fixhub_net).

param(
    [string]$Password
)

$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"
$remoteDir = "/opt/apps/fixhub"

$pw = if ($Password) { $Password } else { $env:FIXHUB_VPS_PASSWORD }
if (-not $pw) {
    Write-Host "ERROR: Indica la contraseña SSH (variable FIXHUB_VPS_PASSWORD o -Password)." -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $plink)) {
    Write-Host "ERROR: No se encontro plink.exe (PuTTY)." -ForegroundColor Red
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  RESET FIXHUB EN VPS" -ForegroundColor Cyan
Write-Host "  1. Bajar contenedores y eliminar BD/volumenes" -ForegroundColor Gray
Write-Host "  2. Volver a desplegar FixHub desde cero" -ForegroundColor Gray
Write-Host "  No se toca: CarnetQR, PanamaTravelHub, n8n" -ForegroundColor Gray
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# --- PASO 1: Bajar solo FixHub y eliminar volúmenes (incluye la BD)
Write-Host "PASO 1: Bajando FixHub y eliminando base de datos/volumenes..." -ForegroundColor Yellow
$cmdDown = "cd $remoteDir 2>/dev/null && docker compose down -v 2>&1 || echo 'Directorio o compose no encontrado'"
$r1 = & $plink -ssh -pw $pw -batch -hostkey $hostkey $hostname $cmdDown 2>&1
Write-Host $r1
Write-Host ""

# Eliminar red si quedó huérfana (compose down suele quitarla)
$cmdNet = "docker network rm fixhub_net 2>/dev/null || true"
& $plink -ssh -pw $pw -batch -hostkey $hostkey $hostname $cmdNet 2>&1 | Out-Null

# --- PASO 2: Actualizar repo
Write-Host "PASO 2: Actualizando codigo en $remoteDir..." -ForegroundColor Yellow
$cmdGit = "mkdir -p $remoteDir && cd $remoteDir && if [ -d .git ]; then git fetch origin; git reset --hard origin/main 2>/dev/null || git reset --hard origin/master; else git clone https://github.com/IrvingCorrosk19/FixHub.git .; fi && ls -la docker-compose.yml 2>/dev/null || echo 'FALTA docker-compose.yml'"
$r2 = & $plink -ssh -pw $pw -batch -hostkey $hostkey $hostname $cmdGit 2>&1
Write-Host $r2
Write-Host ""

# --- PASO 3: Crear .env
Write-Host "PASO 3: Creando .env..." -ForegroundColor Yellow
$envContent = @"
POSTGRES_DB=FixHub
POSTGRES_USER=fixhubuser
POSTGRES_PASSWORD=FixHub2024!Secure

JWT_SECRET_KEY=ChangeMeProductionMin32CharsSecretKey!!

WEB_ORIGIN=https://fixhub.autonomousflow.lat

ASPNETCORE_ENVIRONMENT=Production
"@
$envB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($envContent))
$cmdEnv = "cd $remoteDir && echo '$envB64' | base64 -d > .env && echo '.env creado'"
$r3 = & $plink -ssh -pw $pw -batch -hostkey $hostkey $hostname $cmdEnv 2>&1
Write-Host $r3
Write-Host ""

# --- PASO 4: Build y levantar de cero (migrador creará la BD)
Write-Host "PASO 4: Construyendo y levantando FixHub (puede tardar 5-10 min)..." -ForegroundColor Yellow
$cmdUp = "cd $remoteDir && docker compose up -d --build 2>&1"
$r4 = & $plink -ssh -pw $pw -batch -hostkey $hostkey $hostname $cmdUp 2>&1
Write-Host $r4
Write-Host ""

# --- PASO 5: Verificar
Write-Host "PASO 5: Verificando..." -ForegroundColor Yellow
Start-Sleep -Seconds 8
$cmdPs = "docker ps --filter name=fixhub --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'"
$r5 = & $plink -ssh -pw $pw -batch -hostkey $hostkey $hostname $cmdPs 2>&1
Write-Host $r5
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "  FIXHUB RESETEADO Y DESPLEGADO" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "  App: https://fixhub.autonomousflow.lat (puerto 8081)" -ForegroundColor Green
Write-Host "  BD y volumenes recreados desde cero." -ForegroundColor Gray
Write-Host ""
