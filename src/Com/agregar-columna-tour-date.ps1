# Script para agregar columna 'tour_date' a la tabla 'tours'
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  AGREGAR COLUMNA 'tour_date' A 'tours'" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# SQL para agregar columna 'tour_date' a la tabla 'tours'
$addColumnSQL = @"
-- Agregar columna 'tour_date' a la tabla 'tours' si no existe
ALTER TABLE public.tours 
ADD COLUMN IF NOT EXISTS tour_date TIMESTAMP;
"@

# Copiar SQL al servidor usando base64
Write-Host "Agregando columna 'tour_date'..." -ForegroundColor Yellow
$bytes = [System.Text.Encoding]::UTF8.GetBytes($addColumnSQL)
$base64 = [System.Convert]::ToBase64String($bytes)

$tempScript = "add_tour_date_column_$(Get-Date -Format 'yyyyMMdd_HHmmss').sql"
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
Write-Host "Verificando que la columna se agregó correctamente..." -ForegroundColor Yellow
$cmdVerify = "docker exec panamatravelhub_postgres psql -U $dbUser -d $dbName -c '\d tours' 2>&1 | grep -i 'tour_date' || echo 'Verificando estructura completa...'"
$resultVerify = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdVerify 2>&1
Write-Host $resultVerify
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "  COLUMNA 'tour_date' VERIFICADA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Ahora intenta cargar las reservas nuevamente en:" -ForegroundColor Yellow
Write-Host "  http://164.68.99.83:8082/admin.html (Tab Reservas)" -ForegroundColor Green
Write-Host ""
