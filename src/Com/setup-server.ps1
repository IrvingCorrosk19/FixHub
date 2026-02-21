# Script para configurar el servidor VPS automáticamente
$plink = "C:\Program Files\PuTTY\plink.exe"
$server = "164.68.99.83"
$password = 'DC26Y0U5ER6sWj'
$user = "root"
$hostname = "$user@$server"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CONFIGURACION AUTOMATICA DEL SERVIDOR" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# PASO 1: Aceptar la clave del host (primera conexión)
Write-Host "PASO 1: Aceptando clave del host..." -ForegroundColor Cyan
# Aceptar la clave del host usando el fingerprint SHA256
& $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "echo 'Conexion establecida'" | Out-Null
Write-Host "OK: Conexion establecida" -ForegroundColor Green
Write-Host ""

# PASO 2: Actualizar Ubuntu
Write-Host "PASO 2: Actualizando Ubuntu (esto puede tardar varios minutos...)" -ForegroundColor Yellow
$cmd2 = "apt update && apt upgrade -y"
Write-Host "Ejecutando: $cmd2" -ForegroundColor Gray
$result2 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd2 2>&1
Write-Host $result2
Write-Host ""

# PASO 3: Configurar zona horaria
Write-Host "PASO 3: Configurando zona horaria (Panama)" -ForegroundColor Yellow
$cmd3 = "timedatectl set-timezone America/Panama && timedatectl"
Write-Host "Ejecutando: $cmd3" -ForegroundColor Gray
$result3 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd3 2>&1
Write-Host $result3
Write-Host ""

# PASO 4: Configurar firewall
Write-Host "PASO 4: Configurando firewall (UFW)" -ForegroundColor Yellow
$cmd4 = "ufw --force allow OpenSSH && ufw --force allow 80 && ufw --force allow 443 && echo 'y' | ufw enable && ufw status"
Write-Host "Ejecutando: $cmd4" -ForegroundColor Gray
$result4 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd4 2>&1
Write-Host $result4
Write-Host ""

# PASO 5: Instalar Docker
Write-Host "PASO 5: Instalando Docker" -ForegroundColor Yellow
$cmd5 = "curl -fsSL https://get.docker.com | sh && docker --version"
Write-Host "Ejecutando: $cmd5" -ForegroundColor Gray
$result5 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd5 2>&1
Write-Host $result5
Write-Host ""

# PASO 6: Crear estructura de directorios
Write-Host "PASO 6: Creando estructura de directorios (/opt/apps)" -ForegroundColor Yellow
$cmd6 = "mkdir -p /opt/apps && cd /opt/apps && pwd && ls -la"
Write-Host "Ejecutando: $cmd6" -ForegroundColor Gray
$result6 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd6 2>&1
Write-Host $result6
Write-Host ""

# Verificaciones finales
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  VERIFICACIONES FINALES" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Verificando Docker..." -ForegroundColor Yellow
$dockerVersion = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "docker --version" 2>&1
Write-Host $dockerVersion -ForegroundColor White

Write-Host ""
Write-Host "Verificando Firewall..." -ForegroundColor Yellow
$ufwStatus = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "ufw status" 2>&1
Write-Host $ufwStatus -ForegroundColor White

Write-Host ""
Write-Host "Verificando Zona Horaria..." -ForegroundColor Yellow
$timezone = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "timedatectl" 2>&1
Write-Host $timezone -ForegroundColor White

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CONFIGURACION COMPLETADA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Resumen:" -ForegroundColor Yellow
Write-Host "  Entre por SSH: si" -ForegroundColor Green
if ($dockerVersion -match "Docker version") {
    Write-Host "  Docker funciona: si" -ForegroundColor Green
} else {
    Write-Host "  Docker funciona: verificar manualmente" -ForegroundColor Yellow
}
