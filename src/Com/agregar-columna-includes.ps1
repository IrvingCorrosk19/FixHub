# Script para agregar columna 'includes' a la tabla 'tours'
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  AGREGAR COLUMNA 'includes' A 'tours'" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar si la columna ya existe
Write-Host "PASO 1: Verificando si la columna 'includes' existe..." -ForegroundColor Yellow
$cmdCheck = "docker exec panamatravelhub_postgres psql -U panamatravelhub_user -d panamatravelhub_db -c 'SELECT column_name FROM information_schema.columns WHERE table_name = ''tours'' AND column_name = ''includes'';' 2>&1"
$resultCheck = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCheck 2>&1
Write-Host $resultCheck
Write-Host ""

# Si la columna no existe, agregarla
if ($resultCheck -notmatch "includes") {
    Write-Host "PASO 2: La columna 'includes' NO existe. Agregandola..." -ForegroundColor Yellow
    
    # SQL para agregar columna 'includes' a la tabla 'tours'
    $addColumnSQL = @"
-- Agregar columna 'includes' a la tabla 'tours' si no existe
ALTER TABLE public.tours 
ADD COLUMN IF NOT EXISTS includes TEXT;
"@

    # Copiar SQL al servidor usando base64
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($addColumnSQL)
    $base64 = [System.Convert]::ToBase64String($bytes)

    $tempScript = "add_includes_column_$(Get-Date -Format 'yyyyMMdd_HHmmss').sql"
    $remoteScriptPath = "/tmp/$tempScript"

    $cmdCopy = "echo '$base64' | base64 -d > $remoteScriptPath"
    & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCopy 2>&1 | Out-Null

    # Ejecutar ALTER TABLE
    $dbName = "panamatravelhub_db"
    $dbUser = "panamatravelhub_user"

    $cmdAdd = "docker exec -i panamatravelhub_postgres psql -U $dbUser -d $dbName < $remoteScriptPath 2>&1"
    $resultAdd = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdAdd 2>&1
    Write-Host $resultAdd
    Write-Host ""

    # Limpiar archivo temporal
    $cmdCleanup = "rm -f $remoteScriptPath"
    & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCleanup 2>&1 | Out-Null

    # Verificar que la columna se agregó
    Write-Host "PASO 3: Verificando que la columna se agregó correctamente..." -ForegroundColor Yellow
    $cmdVerify = "docker exec panamatravelhub_postgres psql -U $dbUser -d $dbName -c '\d tours' 2>&1 | grep -i 'includes' || echo 'Verificando estructura completa...'"
    $resultVerify = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdVerify 2>&1
    Write-Host $resultVerify
    Write-Host ""
} else {
    Write-Host "PASO 2: La columna 'includes' ya existe. No se requiere accion." -ForegroundColor Green
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Green
Write-Host "  COLUMNA 'includes' VERIFICADA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Ahora intenta cargar las reservas nuevamente en:" -ForegroundColor Yellow
Write-Host "  http://164.68.99.83:8082/admin.html (Tab Reservas)" -ForegroundColor Green
Write-Host ""
