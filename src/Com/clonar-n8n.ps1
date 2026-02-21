# ============================================
# Script: Clonar y Preparar n8n
# Descripción: Crea el directorio y prepara el entorno para n8n
# ============================================

$ErrorActionPreference = "Stop"

# Credenciales SSH
$serverIP = "164.68.99.83"
$sshUser = "root"
$sshPassword = "DC26Y0U5ER6sWj"
$plinkPath = "C:\Program Files\PuTTY\plink.exe"
$hostname = "$sshUser@$serverIP"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

# Directorio destino en el servidor
$remoteDir = "/opt/apps/n8n"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Preparar Entorno n8n en Servidor" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Verificar que plink existe
if (-not (Test-Path $plinkPath)) {
    Write-Host "ERROR: plink.exe no encontrado en: $plinkPath" -ForegroundColor Red
    Write-Host "Por favor, instala PuTTY o ajusta la ruta en el script." -ForegroundColor Yellow
    exit 1
}

Write-Host "[1/4] Creando directorio..." -ForegroundColor Yellow

# Crear directorio si no existe
$createDirCmd = "mkdir -p $remoteDir && echo 'Directorio creado' || echo 'Directorio ya existe'"

try {
    $createDirOutput = & $plinkPath -ssh -pw "$sshPassword" -batch -hostkey "$hostkey" "$hostname" "$createDirCmd" 2>&1
    Write-Host $createDirOutput -ForegroundColor Green
} catch {
    Write-Host "ERROR al crear directorio: $_" -ForegroundColor Red
    exit 1
}

Write-Host "[2/4] Verificando estructura de directorios..." -ForegroundColor Yellow

$checkCmd = "ls -la /opt/apps/ 2>&1 | head -10"

try {
    $checkOutput = & $plinkPath -ssh -pw "$sshPassword" -batch -hostkey "$hostkey" "$hostname" "$checkCmd" 2>&1
    Write-Host $checkOutput
} catch {
    Write-Host "ADVERTENCIA: No se pudo verificar estructura" -ForegroundColor Yellow
}

Write-Host "[3/4] Verificando puertos en uso..." -ForegroundColor Yellow

$portCheckCmd = "docker ps --format 'table {{.Names}}\t{{.Ports}}' 2>&1 | grep -E '(n8n|carnetqr|panamatravelhub|8081|8082|8083|5432|5433|5434)' || echo 'No hay contenedores relevantes'"

try {
    $portOutput = & $plinkPath -ssh -pw "$sshPassword" -batch -hostkey "$hostkey" "$hostname" "$portCheckCmd" 2>&1
    Write-Host $portOutput
} catch {
    Write-Host "ADVERTENCIA: No se pudo verificar puertos" -ForegroundColor Yellow
}

Write-Host "[4/4] Preparacion completada!" -ForegroundColor Green
Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Estado del Entorno" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Directorio: $remoteDir" -ForegroundColor White
Write-Host "Servidor: $serverIP" -ForegroundColor White
Write-Host ""
Write-Host "Siguiente paso: Ejecutar .\Com\deploy-n8n.ps1 para desplegar" -ForegroundColor Yellow
Write-Host ""
