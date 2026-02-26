# Clona la BD FixHub LOCAL completa (esquema + datos) al VPS. Deja ambas iguales.
# Uso: .\sync-fixhub-db-full.ps1 [-Force]

param([switch]$Force)

$plink = "C:\Program Files\PuTTY\plink.exe"
$pscp = "C:\Program Files\PuTTY\pscp.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"
$serverContainer = "fixhub_postgres"
$serverDatabase = "FixHub"
$serverUser = "fixhubuser"
$localHost = "localhost"
$localPort = "5432"
$localDatabase = "FixHub"
$localUser = "postgres"
$localPassword = "Panama2020$"

if (-not $Force) {
    Write-Host "ADVERTENCIA: Reemplazara esquema y datos de FixHub en el VPS." -ForegroundColor Red
    Write-Host "Ejecuta con -Force para continuar." -ForegroundColor Yellow
    exit 1
}

$pgDumpPath = Get-ChildItem "C:\Program Files\PostgreSQL" -Recurse -Filter "pg_dump.exe" -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
if (-not $pgDumpPath) {
    foreach ($v in @("18","17","16","15")) {
        $p = "C:\Program Files\PostgreSQL\$v\bin\pg_dump.exe"
        if (Test-Path $p) { $pgDumpPath = $p; break }
    }
}
if (-not $pgDumpPath -or -not (Test-Path $pgDumpPath)) {
    Write-Host "ERROR: pg_dump no encontrado." -ForegroundColor Red
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CLON COMPLETO FixHub LOCAL -> VPS" -ForegroundColor Cyan
Write-Host "  (esquema + datos)" -ForegroundColor Gray
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = "FixHub_full_$timestamp.sql"
$backupsDir = Join-Path $PSScriptRoot "..\..\..\backups"
if (-not (Test-Path $backupsDir)) { New-Item -ItemType Directory -Path $backupsDir -Force | Out-Null }
$backupPath = Join-Path $backupsDir $backupFile

# 1. Dump completo (schema + data, sin --data-only)
Write-Host "PASO 1: Dump completo de BD local..." -ForegroundColor Yellow
$env:PGPASSWORD = $localPassword
& $pgDumpPath -h $localHost -p $localPort -U $localUser -d $localDatabase -F p -f $backupPath 2>&1
Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR en pg_dump" -ForegroundColor Red
    exit 1
}
$sizeMB = [math]::Round((Get-Item $backupPath).Length / 1MB, 2)
Write-Host "  OK $backupFile ($sizeMB MB)" -ForegroundColor Green
Write-Host ""

# 2. Transferir al VPS
Write-Host "PASO 2: Transfiriendo al VPS..." -ForegroundColor Yellow
$serverBackupPath = "/tmp/$backupFile"
& $pscp -pw $password -hostkey $hostkey $backupPath "${hostname}:${serverBackupPath}"
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR al transferir" -ForegroundColor Red
    exit 1
}
Write-Host "  OK" -ForegroundColor Green
Write-Host ""

# 3. En VPS: borrar esquema public y recrear (para restaurar limpio)
Write-Host "PASO 3: Limpiando BD en VPS..." -ForegroundColor Yellow
$dropScript = @"
DROP SCHEMA public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO $serverUser;
GRANT ALL ON SCHEMA public TO public;
"@
$dropB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($dropScript))
$dropCmd = "echo '$dropB64' | base64 -d | docker exec -i $serverContainer psql -U $serverUser -d $serverDatabase -v ON_ERROR_STOP=1"
& $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $dropCmd 2>&1
Write-Host "  OK" -ForegroundColor Green
Write-Host ""

# 4. Restaurar dump completo
Write-Host "PASO 4: Restaurando dump en VPS..." -ForegroundColor Yellow
$restoreCmd = "cat $serverBackupPath | docker exec -i $serverContainer psql -U $serverUser -d $serverDatabase -v ON_ERROR_STOP=0 2>&1"
$restoreResult = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $restoreCmd 2>&1
$errs = $restoreResult | Select-String -Pattern "ERROR|FATAL" | Where-Object { $_ -notmatch "transaction_timeout" }
if ($errs) {
    Write-Host "  Algunos avisos (transaction_timeout suele ser inofensivo):" -ForegroundColor Gray
    $errs | Select-Object -First 5 | ForEach-Object { Write-Host "    $_" }
} else {
    Write-Host "  OK" -ForegroundColor Green
}
Write-Host ""

# 5. Limpiar archivo temporal en VPS
& $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "rm -f $serverBackupPath" 2>&1 | Out-Null

Write-Host "========================================" -ForegroundColor Green
Write-Host "  CLON COMPLETADO" -ForegroundColor Green
Write-Host "  BD local y VPS deben ser iguales (esquema + datos)." -ForegroundColor White
Write-Host "========================================" -ForegroundColor Green
