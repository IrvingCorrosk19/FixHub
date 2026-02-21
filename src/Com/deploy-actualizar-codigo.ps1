# Script para actualizar el código y reiniciar con los cambios más recientes
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ACTUALIZAR CÓDIGO Y REINICIAR" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# PASO 1: Verificar cambios locales en el servidor
Write-Host "PASO 1: Verificando cambios locales en el servidor..." -ForegroundColor Yellow
$cmd1 = "cd /opt/apps/aspnet; git status"
Write-Host "Ejecutando: Verificando cambios..." -ForegroundColor Gray
$result1 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd1 2>&1
Write-Host $result1
Write-Host ""

# PASO 2: Hacer stash de cambios locales
Write-Host "PASO 2: Guardando cambios locales (stash)..." -ForegroundColor Yellow
$cmd2 = "cd /opt/apps/aspnet; git stash"
Write-Host "Ejecutando: Guardando cambios..." -ForegroundColor Gray
$result2 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd2 2>&1
Write-Host $result2
Write-Host ""

# PASO 3: Actualizar desde GitHub
Write-Host "PASO 3: Actualizando código desde GitHub..." -ForegroundColor Yellow
$cmd3 = "cd /opt/apps/aspnet; git fetch origin; git pull origin main"
Write-Host "Ejecutando: Actualizando desde GitHub..." -ForegroundColor Gray
$result3 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd3 2>&1
Write-Host $result3
Write-Host ""

# PASO 4: Verificar que se actualizó
Write-Host "PASO 4: Verificando última versión..." -ForegroundColor Yellow
$cmd4 = "cd /opt/apps/aspnet; git log --oneline -5"
Write-Host "Ejecutando: Verificando versión..." -ForegroundColor Gray
$result4 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd4 2>&1
Write-Host $result4
Write-Host ""

# PASO 5: Detener contenedores
Write-Host "PASO 5: Deteniendo contenedores..." -ForegroundColor Yellow
$cmd5 = "cd /opt/apps/aspnet; docker compose down"
Write-Host "Ejecutando: Deteniendo contenedores..." -ForegroundColor Gray
$result5 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd5 2>&1
Write-Host $result5
Write-Host ""

# PASO 6: Reconstruir y levantar contenedores (sin cache para forzar rebuild)
Write-Host "PASO 6: Reconstruyendo contenedores (sin cache)..." -ForegroundColor Yellow
Write-Host "Esto puede tardar varios minutos..." -ForegroundColor Gray
$cmd6 = "cd /opt/apps/aspnet; docker compose build --no-cache; docker compose up -d"
Write-Host "Ejecutando: Rebuild completo..." -ForegroundColor Gray
$result6 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd6 2>&1
Write-Host $result6
Write-Host ""

# PASO 7: Esperar a que los contenedores estén listos
Write-Host "PASO 7: Esperando a que los contenedores estén listos..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# PASO 8: Verificar contenedores
Write-Host "PASO 8: Verificando contenedores..." -ForegroundColor Yellow
$cmd8 = "docker ps --filter 'name=carnetqr' --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'"
Write-Host "Ejecutando: Verificando contenedores..." -ForegroundColor Gray
$result8 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd8 2>&1
Write-Host $result8
Write-Host ""

# PASO 9: Ver logs recientes
Write-Host "PASO 9: Últimas 30 líneas de logs..." -ForegroundColor Yellow
$cmd9 = "docker logs --tail 30 carnetqr_web"
Write-Host "Ejecutando: Verificando logs..." -ForegroundColor Gray
$result9 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd9 2>&1
Write-Host $result9
Write-Host ""

# PASO 10: Verificar versión del código
Write-Host "PASO 10: Verificando versión del código desplegado..." -ForegroundColor Yellow
$cmd10 = "cd /opt/apps/aspnet; git log -1 --oneline"
Write-Host "Ejecutando: Verificando versión..." -ForegroundColor Gray
$result10 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd10 2>&1
Write-Host $result10
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ACTUALIZACIÓN COMPLETADA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Código actualizado y contenedores reconstruidos" -ForegroundColor Green
Write-Host ""
Write-Host "URLs:" -ForegroundColor Yellow
Write-Host "  - https://carnet.autonomousflow.lat/" -ForegroundColor White
Write-Host "  - http://164.68.99.83" -ForegroundColor White
Write-Host ""
