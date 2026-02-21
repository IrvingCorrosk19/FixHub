# Script para limpiar y desplegar PanamaTravelHub
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  LIMPIAR Y DESPLEGAR PANAMATRAVELHUB" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Limpiar contenedores fallidos
Write-Host "Limpiando contenedores fallidos..." -ForegroundColor Yellow
$cmdClean = "cd /opt/apps/panamatravelhub && docker compose down 2>&1 || true"
Write-Host "Ejecutando: $cmdClean" -ForegroundColor Gray
$resultClean = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdClean 2>&1
Write-Host $resultClean
Write-Host ""

# Eliminar contenedor huérfano si existe
Write-Host "Eliminando contenedor huérfano..." -ForegroundColor Yellow
$cmdRemove = "docker rm -f panamatravelhub-postgres 2>&1 || true"
Write-Host "Ejecutando: $cmdRemove" -ForegroundColor Gray
$resultRemove = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdRemove 2>&1
Write-Host $resultRemove
Write-Host ""

# Desplegar
Write-Host "Desplegando PanamaTravelHub..." -ForegroundColor Yellow
$cmdDeploy = "cd /opt/apps/panamatravelhub && docker compose up -d --build"
Write-Host "Ejecutando: $cmdDeploy" -ForegroundColor Gray
$resultDeploy = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdDeploy 2>&1
Write-Host $resultDeploy
Write-Host ""

# Verificar estado
Write-Host "Verificando contenedores..." -ForegroundColor Yellow
$cmdStatus = "docker ps --filter name=panamatravelhub --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'"
Write-Host "Ejecutando: $cmdStatus" -ForegroundColor Gray
$resultStatus = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdStatus 2>&1
Write-Host $resultStatus
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "  PROCESO COMPLETADO" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
