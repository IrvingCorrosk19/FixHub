# Script para instalar la base de datos de PanamaTravelHub
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  INSTALAR BASE DE DATOS PANAMATRAVELHUB" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Lista de scripts SQL en orden
$scripts = @(
    "01_create_extensions.sql",
    "02_create_enums.sql",
    "03_create_tables.sql",
    "04_create_indexes.sql",
    "05_create_functions.sql",
    "06_seed_data.sql"
)

$dbName = "panamatravelhub_db"
$dbUser = "panamatravelhub_user"

foreach ($script in $scripts) {
    Write-Host "Ejecutando script: $script..." -ForegroundColor Yellow
    
    # Copiar script al contenedor y ejecutarlo
    $scriptPath = "/opt/apps/panamatravelhub/database/$script"
    
    # Verificar que el script existe
    $cmdCheck = "test -f $scriptPath && echo 'EXISTS' || echo 'NOT_FOUND'"
    $resultCheck = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCheck 2>&1
    
    if ($resultCheck -match "NOT_FOUND") {
        Write-Host "  Advertencia: Script $script no encontrado. Saltando..." -ForegroundColor Yellow
        continue
    }
    
    # Ejecutar script
    $cmd = "docker exec -i panamatravelhub_postgres psql -U $dbUser -d $dbName < $scriptPath 2>&1"
    Write-Host "  Ejecutando: docker exec -i panamatravelhub_postgres psql -U $dbUser -d $dbName < $scriptPath" -ForegroundColor Gray
    $result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd 2>&1
    
    if ($result -match "ERROR" -or $result -match "error") {
        Write-Host "  ERROR: $result" -ForegroundColor Red
    } else {
        Write-Host "  OK: Script ejecutado correctamente" -ForegroundColor Green
    }
    Write-Host ""
}

# Verificar tablas creadas
Write-Host "Verificando tablas creadas..." -ForegroundColor Yellow
$cmdVerify = "docker exec panamatravelhub_postgres psql -U $dbUser -d $dbName -c '\dt' 2>&1"
$resultVerify = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdVerify 2>&1
Write-Host $resultVerify
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "  INSTALACION COMPLETADA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
