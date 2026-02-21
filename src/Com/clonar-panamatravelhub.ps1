# Script para clonar el repositorio PanamaTravelHub en el servidor VPS
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CLONAR REPOSITORIO PANAMATRAVELHUB" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# URL del repositorio (configurada automaticamente)
$repoUrl = "https://github.com/IrvingCorrosk19/PanamaTravelHub.git"

Write-Host ""
Write-Host "Repositorio a clonar: $repoUrl" -ForegroundColor Yellow
Write-Host "Directorio destino: /opt/apps/panamatravelhub" -ForegroundColor Yellow
Write-Host ""

# Verificar si el directorio tiene contenido
Write-Host "Verificando directorio /opt/apps/panamatravelhub..." -ForegroundColor Yellow
$cmdCheck = "ls -la /opt/apps/panamatravelhub 2>&1 | head -5"
$resultCheck = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCheck 2>&1
Write-Host $resultCheck
Write-Host ""

# Clonar repositorio (eliminar directorio existente si existe)
Write-Host ""
Write-Host "Clonando repositorio (borrando directorio existente si existe)..." -ForegroundColor Yellow
$cmdClone = "cd /opt/apps && rm -rf panamatravelhub && git clone $repoUrl panamatravelhub"
Write-Host "Ejecutando: $cmdClone" -ForegroundColor Gray
$resultClone = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdClone 2>&1
Write-Host $resultClone
Write-Host ""

# Verificar que se clono correctamente
Write-Host "Verificando archivos clonados..." -ForegroundColor Yellow
$cmdVerify = "cd /opt/apps/panamatravelhub && ls -la Dockerfile docker-compose.yml 2>&1"
Write-Host "Ejecutando: $cmdVerify" -ForegroundColor Gray
$resultVerify = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdVerify 2>&1
Write-Host $resultVerify
Write-Host ""

if ($resultVerify -match "No such file")
{
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  ADVERTENCIA" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Los archivos Dockerfile o docker-compose.yml no se encontraron." -ForegroundColor Yellow
    Write-Host "Asegurate de que estos archivos esten en el repositorio." -ForegroundColor Yellow
    Write-Host ""
}
else
{
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  CLONACION COMPLETADA" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Repositorio clonado exitosamente en:" -ForegroundColor Green
    Write-Host "  /opt/apps/panamatravelhub" -ForegroundColor White
    Write-Host ""
    Write-Host "Proximo paso: Ejecutar deploy-panamatravelhub.ps1" -ForegroundColor Yellow
    Write-Host ""
}
