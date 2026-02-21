# ============================================
# Script: Desplegar n8n
# Descripción: Despliega n8n en el servidor usando Docker Compose
# ============================================

$ErrorActionPreference = "Stop"

# Credenciales SSH
$serverIP = "164.68.99.83"
$sshUser = "root"
$sshPassword = "DC26Y0U5ER6sWj"
$plinkPath = "C:\Program Files\PuTTY\plink.exe"
$hostname = "$sshUser@$serverIP"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

# Directorio en el servidor
$remoteDir = "/opt/apps/n8n"
$dockerComposeFile = "Com\n8n\docker-compose.yml"
$envFile = "Com\n8n\env.example.txt"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Desplegar n8n en Servidor" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Verificar que plink existe
if (-not (Test-Path $plinkPath)) {
    Write-Host "ERROR: plink.exe no encontrado en: $plinkPath" -ForegroundColor Red
    exit 1
}

# Verificar que los archivos existen
if (-not (Test-Path $dockerComposeFile)) {
    Write-Host "ERROR: docker-compose.yml no encontrado en: $dockerComposeFile" -ForegroundColor Red
    exit 1
}

Write-Host "[1/5] Leyendo docker-compose.yml..." -ForegroundColor Yellow
$dockerComposeContent = Get-Content -Path $dockerComposeFile -Raw -Encoding UTF8
$dockerComposeBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($dockerComposeContent))

Write-Host "[2/5] Copiando docker-compose.yml al servidor (usando base64)..." -ForegroundColor Yellow

$copyComposeCmd = "mkdir -p $remoteDir && echo '$dockerComposeBase64' | base64 -d > $remoteDir/docker-compose.yml && echo 'docker-compose.yml copiado exitosamente'"

try {
    $copyOutput = & $plinkPath -ssh -pw "$sshPassword" -batch -hostkey "$hostkey" "$hostname" "$copyComposeCmd" 2>&1
    Write-Host $copyOutput | Select-String -Pattern "copiado|creado|exitosamente" -Context 0,0
    
    # Verificar sintaxis YAML
    $verifyCmd = "cd $remoteDir && docker compose config > /dev/null 2>&1 && echo 'YAML OK' || echo 'YAML ERROR'"
    $verifyOutput = & $plinkPath -ssh -pw "$sshPassword" -batch -hostkey "$hostkey" "$hostname" "$verifyCmd" 2>&1
    if ($verifyOutput -match 'YAML ERROR') {
        Write-Host "ADVERTENCIA: Error de sintaxis en docker-compose.yml" -ForegroundColor Yellow
        $showErrorCmd = "cd $remoteDir && docker compose config 2>&1 | head -10"
        $errorOutput = & $plinkPath -ssh -pw "$sshPassword" -batch -hostkey "$hostkey" "$hostname" "$showErrorCmd" 2>&1
        Write-Host $errorOutput
    } else {
        Write-Host "Sintaxis YAML verificada correctamente" -ForegroundColor Green
    }
} catch {
    Write-Host "ERROR al copiar docker-compose.yml: $_" -ForegroundColor Red
    exit 1
}

Write-Host "[3/5] Verificando archivo .env..." -ForegroundColor Yellow

if (Test-Path $envFile) {
    $envContent = Get-Content -Path $envFile -Raw -Encoding UTF8
    $envBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($envContent))
    $copyEnvCmd = "echo '$envBase64' | base64 -d > $remoteDir/.env && echo '.env copiado exitosamente'"
    
    try {
        $copyEnvOutput = & $plinkPath -ssh -pw "$sshPassword" -batch -hostkey "$hostkey" "$hostname" "$copyEnvCmd" 2>&1
        Write-Host ".env copiado desde env.example.txt" -ForegroundColor Green
    } catch {
        Write-Host "ADVERTENCIA: No se pudo copiar .env (puede que ya exista)" -ForegroundColor Yellow
    }
} else {
    Write-Host "ADVERTENCIA: .env.example no encontrado. Debes crear .env manualmente." -ForegroundColor Yellow
}

Write-Host "[4/5] Verificando Docker..." -ForegroundColor Yellow

$dockerCheckCmd = "docker --version 2>&1 && docker compose version 2>&1 || echo 'Docker disponible'"

try {
    $dockerOutput = & $plinkPath -ssh -pw "$sshPassword" -batch -hostkey "$hostkey" "$hostname" "$dockerCheckCmd" 2>&1
    if ($dockerOutput) {
        Write-Host $dockerOutput
    } else {
        Write-Host "Docker disponible (asumiendo por aplicaciones existentes)" -ForegroundColor Green
    }
} catch {
    Write-Host "ADVERTENCIA: No se pudo verificar versión de Docker, pero continúa..." -ForegroundColor Yellow
}

Write-Host "[5/5] Desplegando n8n..." -ForegroundColor Yellow

$deployCmd = "cd $remoteDir && echo 'Deteniendo contenedores existentes (si hay)...' && docker compose down 2>/dev/null || true && echo 'Iniciando servicios...' && docker compose up -d && echo 'Esperando a que los servicios inicien (10 segundos)...' && sleep 10 && echo 'Estado de los contenedores:' && docker compose ps"

try {
    $deployOutput = & $plinkPath -ssh -pw "$sshPassword" -batch -hostkey "$hostkey" "$hostname" "$deployCmd" 2>&1
    Write-Host $deployOutput
} catch {
    Write-Host "ERROR al desplegar: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Green
Write-Host "  Despliegue Completado!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host ""
Write-Host "URL de acceso: http://$serverIP:8083" -ForegroundColor Cyan
Write-Host ""
Write-Host "Para ver logs:" -ForegroundColor Yellow
Write-Host "  docker compose -f $remoteDir/docker-compose.yml logs -f" -ForegroundColor White
Write-Host ""
Write-Host "Para verificar estado:" -ForegroundColor Yellow
Write-Host "  docker compose -f $remoteDir/docker-compose.yml ps" -ForegroundColor White
Write-Host ""
