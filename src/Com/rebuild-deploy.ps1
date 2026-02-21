# Script para hacer rebuild y redeploy de la aplicación
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  REBUILD Y REDEPLOY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# PASO 1: Actualizar repositorio
Write-Host "PASO 1: Actualizando repositorio..." -ForegroundColor Yellow
$cmd1 = "cd /opt/apps/aspnet && git pull"
Write-Host "Ejecutando: $cmd1" -ForegroundColor Gray
$result1 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd1 2>&1
Write-Host $result1
Write-Host ""

# PASO 2: Detener contenedores
Write-Host "PASO 2: Deteniendo contenedores..." -ForegroundColor Yellow
$cmd2 = "cd /opt/apps/aspnet && docker compose down"
Write-Host "Ejecutando: $cmd2" -ForegroundColor Gray
$result2 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd2 2>&1
Write-Host $result2
Write-Host ""

# PASO 3: Rebuild y levantar contenedores
Write-Host "PASO 3: Reconstruyendo y levantando contenedores..." -ForegroundColor Yellow
Write-Host "Esto puede tardar varios minutos..." -ForegroundColor Gray
$cmd3 = "cd /opt/apps/aspnet && docker compose up -d --build"
Write-Host "Ejecutando: $cmd3" -ForegroundColor Gray
$result3 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd3 2>&1
Write-Host $result3
Write-Host ""

# PASO 4: Verificar contenedores
Write-Host "PASO 4: Verificando contenedores..." -ForegroundColor Yellow
$cmd4 = "docker ps"
Write-Host "Ejecutando: $cmd4" -ForegroundColor Gray
$result4 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd4 2>&1
Write-Host $result4
Write-Host ""

# PASO 5: Ver logs de la aplicación
Write-Host "PASO 5: Ultimas 30 lineas de logs de la aplicacion..." -ForegroundColor Yellow
$cmd5 = "docker logs --tail 30 carnetqr_web"
Write-Host "Ejecutando: $cmd5" -ForegroundColor Gray
$result5 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd5 2>&1
Write-Host $result5
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  REBUILD COMPLETADO" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Aplicacion disponible en: http://164.68.99.83" -ForegroundColor Green
Write-Host ""
Write-Host "Para ver logs en tiempo real:" -ForegroundColor Yellow
Write-Host "  docker logs -f carnetqr_web" -ForegroundColor Gray
Write-Host ""
