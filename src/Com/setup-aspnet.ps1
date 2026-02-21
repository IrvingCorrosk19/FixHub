$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"
$repoUrl = "https://github.com/IrvingCorrosk19/CarnetQR-Platform.git"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  PREPARANDO ENTORNO DE APLICACIONES" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# PASO 1: Crear estructura de directorios
Write-Host "PASO 1: Creando estructura de directorios..." -ForegroundColor Yellow
$cmd1 = "mkdir -p /opt/apps/aspnet && cd /opt/apps/aspnet && pwd"
Write-Host "Ejecutando: $cmd1" -ForegroundColor Gray
$result1 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd1 2>&1
Write-Host $result1
Write-Host ""

# PASO 2: Clonar el proyecto desde GitHub
Write-Host "PASO 2: Clonando proyecto desde GitHub..." -ForegroundColor Yellow
Write-Host "Repositorio: $repoUrl" -ForegroundColor Gray
$cmd2 = "cd /opt/apps/aspnet && git clone $repoUrl ."
Write-Host "Ejecutando: $cmd2" -ForegroundColor Gray
$result2 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd2 2>&1
Write-Host $result2
Write-Host ""

# Verificar que el proyecto se clonó correctamente
Write-Host "Verificando contenido del directorio..." -ForegroundColor Yellow
$cmd3 = "cd /opt/apps/aspnet && ls -la"
Write-Host "Ejecutando: $cmd3" -ForegroundColor Gray
$result3 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd3 2>&1
Write-Host $result3
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CONFIGURACION COMPLETADA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Proyecto ya esta en /opt/apps/aspnet" -ForegroundColor Green
Write-Host "Metodo usado: Git" -ForegroundColor Green
