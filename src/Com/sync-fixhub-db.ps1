# Sincroniza la base de datos FixHub local con el VPS
# Hace backup local y lo restaura en fixhub_postgres
# Uso: .\sync-fixhub-db.ps1 [-Force]

param(
    [switch]$Force
)

$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

# FixHub LOCAL
$localHost = "localhost"
$localPort = "5432"
$localDatabase = "FixHub"
$localUser = "postgres"
$localPassword = "Panama2020$"

# FixHub SERVIDOR (contenedor)
$serverContainer = "fixhub_postgres"
$serverDatabase = "FixHub"
$serverUser = "fixhubuser"
$serverPassword = "FixHub2024!Secure"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SINCRONIZACION FixHub DB LOCAL -> VPS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "ADVERTENCIA: Reemplazara todos los datos de FixHub en el servidor." -ForegroundColor Red
Write-Host ""

if (-not $Force) {
    try {
        $confirm = Read-Host "Continuar? (escribe 'SI' para confirmar)"
        if ($confirm -ne "SI") {
            Write-Host "Cancelado." -ForegroundColor Yellow
            exit
        }
    } catch {
        Write-Host "Modo no interactivo. Continuando..." -ForegroundColor Yellow
    }
} else {
    Write-Host "Modo forzado (-Force). Continuando..." -ForegroundColor Yellow
}
Write-Host ""

# PASO 1: Backup local
Write-Host "PASO 1: Backup de FixHub local..." -ForegroundColor Yellow
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = "FixHub_backup_$timestamp.sql"
$backupsDir = Join-Path $PSScriptRoot "..\..\..\backups"
if (-not (Test-Path $backupsDir)) { New-Item -ItemType Directory -Path $backupsDir -Force | Out-Null }
$backupPath = Join-Path $backupsDir $backupFile

$pgDumpPath = Get-ChildItem "C:\Program Files\PostgreSQL" -Recurse -Filter "pg_dump.exe" -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
if (-not $pgDumpPath) {
    $pgDumpPath = "C:\Program Files\PostgreSQL\18\bin\pg_dump.exe"
    if (-not (Test-Path $pgDumpPath)) {
        $pgDumpPath = "C:\Program Files\PostgreSQL\16\bin\pg_dump.exe"
    }
}
if (-not $pgDumpPath -or -not (Test-Path $pgDumpPath)) {
    Write-Host "ERROR: No se encontro pg_dump. Instala PostgreSQL." -ForegroundColor Red
    exit 1
}

$env:PGPASSWORD = $localPassword
try {
    & $pgDumpPath -h $localHost -p $localPort -U $localUser -d $localDatabase -F p --data-only --column-inserts -f $backupPath
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR al crear backup (codigo $LASTEXITCODE)" -ForegroundColor Red
        Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
        exit 1
    }
    $sizeMB = [math]::Round((Get-Item $backupPath).Length / 1MB, 2)
    Write-Host "  OK Backup: $backupFile ($sizeMB MB)" -ForegroundColor Green
} catch {
    Write-Host "ERROR: $_" -ForegroundColor Red
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    exit 1
}
Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue

# PASO 2: Transferir al VPS
Write-Host ""
Write-Host "PASO 2: Transfiriendo al VPS..." -ForegroundColor Yellow
$serverBackupPath = "/tmp/$backupFile"
$pscp = "C:\Program Files\PuTTY\pscp.exe"
if (-not (Test-Path $pscp)) {
    Write-Host "ERROR: No se encontro pscp.exe (PuTTY)." -ForegroundColor Red
    exit 1
}
& $pscp -pw $password -hostkey $hostkey $backupPath "${hostname}:${serverBackupPath}"
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR al transferir" -ForegroundColor Red
    exit 1
}
Write-Host "  OK Transferido" -ForegroundColor Green

# PASO 3: Limpiar datos en servidor
Write-Host ""
Write-Host "PASO 3: Limpiando datos en servidor..." -ForegroundColor Yellow
$cleanupScript = @'
SET session_replication_role = 'replica';
DO $$
DECLARE r RECORD;
BEGIN
  FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public' AND tablename != '__EFMigrationsHistory')
  LOOP
    BEGIN
      EXECUTE 'TRUNCATE TABLE ' || quote_ident(r.tablename) || ' CASCADE';
    EXCEPTION WHEN OTHERS THEN
      RAISE NOTICE 'Error %: %', r.tablename, SQLERRM;
    END;
  END LOOP;
END $$;
SET session_replication_role = 'origin';
'@
$cleanupB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($cleanupScript))
$cleanupCmd = "echo '$cleanupB64' | base64 -d | docker exec -i $serverContainer psql -U $serverUser -d $serverDatabase"
$cleanupResult = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cleanupCmd 2>&1
Write-Host "  OK Limpiado" -ForegroundColor Green

# PASO 4: Restaurar en servidor
Write-Host ""
Write-Host "PASO 4: Restaurando en servidor..." -ForegroundColor Yellow
$restoreCmd = "cat $serverBackupPath | docker exec -i $serverContainer psql -U $serverUser -d $serverDatabase -v ON_ERROR_STOP=0 2>&1"
$restoreResult = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $restoreCmd 2>&1
if ($restoreResult -match "ERROR|FATAL") {
    Write-Host "  Advertencia: algunos errores durante restauracion" -ForegroundColor Yellow
    $restoreResult | Select-String -Pattern "ERROR|FATAL" | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
} else {
    Write-Host "  OK Restaurado" -ForegroundColor Green
}

# PASO 5: Limpiar temporal
$cleanupFileCmd = "rm -f $serverBackupPath"
& $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cleanupFileCmd 2>&1 | Out-Null

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  SINCRONIZACION COMPLETADA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "  FixHub en VPS actualizado con datos locales." -ForegroundColor White
Write-Host ""
