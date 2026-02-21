# ============================================
# Script: Verificar n8n
# Descripción: Verifica que n8n esté funcionando correctamente
# ============================================

$ErrorActionPreference = "Stop"

# Credenciales SSH
$serverIP = "164.68.99.83"
$sshUser = "root"
$sshPassword = "DC26Y0U5ER6sWj"
$plinkPath = "C:\Program Files\PuTTY\plink.exe"
$hostname = "$sshUser@$serverIP"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

$remoteDir = "/opt/apps/n8n"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Verificar Estado de n8n" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Verificar contenedores
Write-Host "[1/4] Verificando contenedores..." -ForegroundColor Yellow

$checkContainersCmd = @"
docker ps --filter "name=n8n" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
"@

try {
    $containerOutput = & $plinkPath -ssh -pw "$sshPassword" -batch -hostkey "$hostkey" "$hostname" "$checkContainersCmd" 2>&1
    Write-Host $containerOutput
} catch {
    Write-Host "ERROR: No se pudieron verificar contenedores" -ForegroundColor Red
}

Write-Host "[2/4] Verificando logs recientes de n8n..." -ForegroundColor Yellow

$logsCmd = @"
cd $remoteDir
docker compose logs --tail=20 n8n
"@

try {
    $logsOutput = & $plinkPath -ssh -pw "$sshPassword" -batch -hostkey "$hostkey" "$hostname" "$logsCmd" 2>&1
    Write-Host $logsOutput
} catch {
    Write-Host "ADVERTENCIA: No se pudieron obtener logs" -ForegroundColor Yellow
}

Write-Host "[3/4] Verificando salud del servicio..." -ForegroundColor Yellow

$healthCmd = @"
curl -s -o /dev/null -w "HTTP Status: %{http_code}\n" http://localhost:8083/healthz || echo "Health check falló"
"@

try {
    $healthOutput = & $plinkPath -ssh -pw "$sshPassword" -batch -hostkey "$hostkey" "$hostname" "$healthCmd" 2>&1
    Write-Host $healthOutput
} catch {
    Write-Host "ADVERTENCIA: Health check no disponible" -ForegroundColor Yellow
}

Write-Host "[4/4] Verificando puerto 8083..." -ForegroundColor Yellow

$portCmd = @"
netstat -tulpn 2>/dev/null | grep 8083 || ss -tulpn | grep 8083 || echo "Puerto no encontrado en netstat/ss"
"@

try {
    $portOutput = & $plinkPath -ssh -pw "$sshPassword" -batch -hostkey "$hostkey" "$hostname" "$portCmd" 2>&1
    Write-Host $portOutput
} catch {
    Write-Host "ADVERTENCIA: No se pudo verificar puerto" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Resumen" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "URL: http://$serverIP:8083" -ForegroundColor White
Write-Host "Directorio: $remoteDir" -ForegroundColor White
Write-Host ""
Write-Host "Para ver logs en tiempo real:" -ForegroundColor Yellow
Write-Host "  docker compose -f $remoteDir/docker-compose.yml logs -f n8n" -ForegroundColor White
Write-Host ""
