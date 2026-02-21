# Script para verificar el estado de la base de datos
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  VERIFICACIÓN DE BASE DE DATOS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar estado del contenedor PostgreSQL
Write-Host "PASO 1: Estado del contenedor PostgreSQL..." -ForegroundColor Yellow
$cmd1 = "docker ps | grep postgres"
Write-Host "Ejecutando: $cmd1" -ForegroundColor Gray
$result1 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd1 2>&1
Write-Host $result1
Write-Host ""

# Ver logs de PostgreSQL
Write-Host "PASO 2: Últimas 30 líneas de logs de PostgreSQL..." -ForegroundColor Yellow
$cmd2 = "docker logs --tail 30 carnetqr_postgres"
Write-Host "Ejecutando: $cmd2" -ForegroundColor Gray
$result2 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd2 2>&1
Write-Host $result2
Write-Host ""

# Intentar conectar a PostgreSQL desde el contenedor
Write-Host "PASO 3: Verificando conexión a PostgreSQL..." -ForegroundColor Yellow
$cmd3 = "docker exec carnetqr_postgres psql -U carnetqruser -d carnetqrdb -c 'SELECT version();'"
Write-Host "Ejecutando: $cmd3" -ForegroundColor Gray
$result3 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd3 2>&1
Write-Host $result3
Write-Host ""

# Listar bases de datos
Write-Host "PASO 4: Listando bases de datos..." -ForegroundColor Yellow
$cmd4 = "docker exec carnetqr_postgres psql -U carnetqruser -d carnetqrdb -c '\l'"
Write-Host "Ejecutando: $cmd4" -ForegroundColor Gray
$result4 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd4 2>&1
Write-Host $result4
Write-Host ""

# Verificar tablas en la base de datos
Write-Host "PASO 5: Verificando tablas en carnetqrdb..." -ForegroundColor Yellow
$cmd5 = "docker exec carnetqr_postgres psql -U carnetqruser -d carnetqrdb -c '\dt'"
Write-Host "Ejecutando: $cmd5" -ForegroundColor Gray
$result5 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd5 2>&1
Write-Host $result5
Write-Host ""

# Verificar logs de la aplicación web para errores de DB
Write-Host "PASO 6: Verificando logs de la aplicacion (ultimas 50 lineas)..." -ForegroundColor Yellow
$cmd6 = "docker logs --tail 50 carnetqr_web | grep -i -E '(error|database|connection|postgres|failed)'"
Write-Host "Ejecutando: $cmd6" -ForegroundColor Gray
$result6 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd6 2>&1
if ($result6) {
    Write-Host $result6 -ForegroundColor Red
} else {
    Write-Host "No se encontraron errores relacionados con la base de datos" -ForegroundColor Green
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  VERIFICACIÓN COMPLETADA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
