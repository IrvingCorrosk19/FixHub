# Script para verificar el estado actual del VPS para CarnetQR Platform
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  VERIFICACIÓN VPS - CARNETQR PLATFORM" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# PASO 1: Verificar contenedores Docker existentes
Write-Host "PASO 1: Contenedores Docker existentes..." -ForegroundColor Yellow
$cmd1 = 'docker ps -a --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}\t{{.Image}}"'
Write-Host "Ejecutando: Verificando contenedores..." -ForegroundColor Gray
$result1 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd1 2>&1
Write-Host $result1
Write-Host ""

# PASO 2: Verificar contenedores específicos de CarnetQR
Write-Host "PASO 2: Contenedores de CarnetQR..." -ForegroundColor Yellow
$cmd2 = 'docker ps -a --filter "name=carnetqr" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"'
Write-Host "Ejecutando: Verificando contenedores CarnetQR..." -ForegroundColor Gray
$result2 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd2 2>&1
Write-Host $result2
Write-Host ""

# PASO 3: Verificar puertos en uso
Write-Host "PASO 3: Puertos en uso (80, 443, 8001, 5432)..." -ForegroundColor Yellow
$cmd3 = 'netstat -tuln'
Write-Host "Ejecutando: Verificando puertos..." -ForegroundColor Gray
$result3 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd3 2>&1
$ports = $result3 | Select-String -Pattern ":(80|443|8001|5432)"
if ($ports) {
    Write-Host $ports
} else {
    Write-Host "No se encontraron puertos en uso" -ForegroundColor Gray
}
Write-Host ""

# PASO 4: Verificar directorio de la aplicación
Write-Host "PASO 4: Verificando directorio /opt/apps/aspnet..." -ForegroundColor Yellow
$cmd4 = 'ls -la /opt/apps/aspnet'
Write-Host "Ejecutando: Verificando directorio..." -ForegroundColor Gray
$result4 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd4 2>&1
$result4 | Select-Object -First 20 | ForEach-Object { Write-Host $_ }
Write-Host ""

# PASO 5: Verificar si existe docker-compose.yml
Write-Host "PASO 5: Verificando archivos Docker..." -ForegroundColor Yellow
$cmd5 = 'cd /opt/apps/aspnet; ls -la Dockerfile docker-compose.yml .env'
Write-Host "Ejecutando: Verificando archivos Docker..." -ForegroundColor Gray
$result5 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd5 2>&1
Write-Host $result5
Write-Host ""

# PASO 6: Verificar volúmenes Docker
Write-Host "PASO 6: Volúmenes Docker existentes..." -ForegroundColor Yellow
$cmd6 = 'docker volume ls'
Write-Host "Ejecutando: Verificando volúmenes..." -ForegroundColor Gray
$result6 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd6 2>&1
$volumes = $result6 | Select-String -Pattern "(carnetqr|postgres)"
if ($volumes) {
    Write-Host $volumes
} else {
    Write-Host "No se encontraron volúmenes relacionados" -ForegroundColor Gray
}
Write-Host ""

# PASO 7: Verificar redes Docker
Write-Host "PASO 7: Redes Docker existentes..." -ForegroundColor Yellow
$cmd7 = 'docker network ls'
Write-Host "Ejecutando: Verificando redes..." -ForegroundColor Gray
$result7 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd7 2>&1
$networks = $result7 | Select-String -Pattern "(carnetqr|bridge)"
Write-Host $networks
Write-Host ""

# PASO 8: Verificar si hay nginx o proxy reverso
Write-Host "PASO 8: Verificando nginx o proxy reverso..." -ForegroundColor Yellow
$cmd8 = 'docker ps --filter "name=nginx" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"'
Write-Host "Ejecutando: Verificando nginx en Docker..." -ForegroundColor Gray
$result8 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd8 2>&1
if ($result8 -match "nginx") {
    Write-Host $result8
} else {
    Write-Host "No hay nginx en Docker" -ForegroundColor Gray
}
Write-Host ""

$cmd8b = 'which nginx'
Write-Host "Ejecutando: Verificando nginx en sistema..." -ForegroundColor Gray
$result8b = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd8b 2>&1
if ($result8b -match "nginx") {
    Write-Host "Nginx encontrado: $result8b" -ForegroundColor Green
    $cmd8c = 'systemctl status nginx'
    $result8c = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd8c 2>&1
    $result8c | Select-Object -First 5 | ForEach-Object { Write-Host $_ }
} else {
    Write-Host "Nginx no instalado en el sistema" -ForegroundColor Gray
}
Write-Host ""

# PASO 9: Verificar base de datos si existe
Write-Host "PASO 9: Verificando base de datos (si existe)..." -ForegroundColor Yellow
$cmd9Check = 'docker ps'
$result9Check = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd9Check 2>&1
if ($result9Check -match "carnetqr_postgres") {
    Write-Host "Contenedor PostgreSQL encontrado, obteniendo información..." -ForegroundColor Green
    
    $cmd9a = 'docker exec carnetqr_postgres psql -U carnetqruser -d carnetqrdb -c "SELECT version();"'
    Write-Host "=== Información de PostgreSQL ===" -ForegroundColor Cyan
    $result9a = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd9a 2>&1
    $result9a | Select-Object -First 3 | ForEach-Object { Write-Host $_ }
    Write-Host ""
    
    $cmd9b = 'docker exec carnetqr_postgres psql -U carnetqruser -d carnetqrdb -c "\l"'
    Write-Host "=== Bases de datos ===" -ForegroundColor Cyan
    $result9b = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd9b 2>&1
    $result9b | Select-Object -First 10 | ForEach-Object { Write-Host $_ }
    Write-Host ""
    
    $cmd9c = 'docker exec carnetqr_postgres psql -U carnetqruser -d carnetqrdb -c "\dt"'
    Write-Host "=== Tablas ===" -ForegroundColor Cyan
    $result9c = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd9c 2>&1
    $result9c | Select-Object -First 20 | ForEach-Object { Write-Host $_ }
    Write-Host ""
    
    $cmd9d = 'docker exec carnetqr_postgres psql -U carnetqruser -d carnetqrdb -c "SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 10;"'
    Write-Host "=== Migraciones aplicadas ===" -ForegroundColor Cyan
    $result9d = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd9d 2>&1
    Write-Host $result9d
} else {
    Write-Host "Contenedor PostgreSQL no está corriendo" -ForegroundColor Yellow
}
Write-Host ""

# PASO 10: Verificar logs de aplicación si existe
Write-Host "PASO 10: Últimas 20 líneas de logs (si existe)..." -ForegroundColor Yellow
$cmd10 = 'docker logs --tail 20 carnetqr_web'
Write-Host "Ejecutando: Verificando logs..." -ForegroundColor Gray
$result10 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd10 2>&1
if ($result10) {
    if ($result10 -match "error|Error|ERROR") {
        Write-Host $result10 -ForegroundColor Red
    } else {
        Write-Host $result10
    }
} else {
    Write-Host "Contenedor carnetqr_web no existe o no está corriendo" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  VERIFICACIÓN COMPLETADA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
