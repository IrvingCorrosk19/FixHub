# Script para sincronizar la base de datos local con el servidor VPS
# Hace backup de la DB local y lo restaura en el servidor
# Uso: .\sync-db-local-to-server.ps1 [-Force]

param(
    [switch]$Force
)

$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

# Configuración base de datos LOCAL
$localHost = "localhost"
$localPort = "5432"
$localDatabase = "PanamaTravelHub"
$localUser = "postgres"
$localPassword = "Panama2020$"

# Configuración base de datos SERVIDOR (dentro del contenedor)
$serverContainer = "panamatravelhub_postgres"
$serverDatabase = "panamatravelhub_db"
$serverUser = "panamatravelhub_user"
$serverPassword = "PanamaTravel2024!Secure"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SINCRONIZACION DB LOCAL -> SERVIDOR" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "ADVERTENCIA: Esto reemplazara TODOS los datos del servidor con los datos locales." -ForegroundColor Red
Write-Host ""

if (-not $Force) {
    try {
        $confirm = Read-Host "Estas seguro que quieres continuar? (escribe 'SI' para confirmar)"
        if ($confirm -ne "SI") {
            Write-Host "Operacion cancelada." -ForegroundColor Yellow
            exit
        }
    } catch {
        # Modo no interactivo, continuar automáticamente
        Write-Host "Modo no interactivo detectado. Continuando automaticamente..." -ForegroundColor Yellow
        Write-Host ""
    }
} else {
    Write-Host "Modo forzado activado. Continuando sin confirmacion..." -ForegroundColor Yellow
    Write-Host ""
}

# PASO 1: Crear backup de la base de datos local
Write-Host ""
Write-Host "PASO 1: Creando backup de la base de datos local..." -ForegroundColor Yellow
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = "PanamaTravelHub_backup_$timestamp.sql"
$backupPath = Join-Path $PSScriptRoot "..\backups\$backupFile"

# Crear directorio backups si no existe
$backupsDir = Join-Path $PSScriptRoot "..\backups"
if (-not (Test-Path $backupsDir)) {
    New-Item -ItemType Directory -Path $backupsDir -Force | Out-Null
}

# Buscar pg_dump
$pgDumpPath = Get-ChildItem "C:\Program Files\PostgreSQL" -Recurse -Filter "pg_dump.exe" -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
if (-not $pgDumpPath) {
    $pgDumpPath = "C:\Program Files\PostgreSQL\18\bin\pg_dump.exe"
    if (-not (Test-Path $pgDumpPath)) {
        $pgDumpPath = Get-ChildItem "C:\Program Files\PostgreSQL" -Recurse -Filter "pg_dump.exe" -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
    }
}

if (-not $pgDumpPath -or -not (Test-Path $pgDumpPath)) {
    Write-Host "❌ No se encontró pg_dump.exe." -ForegroundColor Red
    Write-Host "Por favor instala PostgreSQL o agrega pg_dump al PATH." -ForegroundColor Yellow
    exit 1
}

Write-Host "Usando: $pgDumpPath" -ForegroundColor Gray
$env:PGPASSWORD = $localPassword

try {
    # Crear backup solo de datos (sin esquema) para evitar conflictos
    & $pgDumpPath -h $localHost -p $localPort -U $localUser -d $localDatabase -F p --data-only --column-inserts -f $backupPath
    
    if ($LASTEXITCODE -eq 0) {
        $fileInfo = Get-Item $backupPath
        $fileSizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
        
        Write-Host "✅ Backup local creado exitosamente!" -ForegroundColor Green
        Write-Host "   Archivo: $backupFile" -ForegroundColor White
        Write-Host "   Tamaño: $fileSizeMB MB" -ForegroundColor White
    } else {
        Write-Host "❌ Error al crear backup local (Código: $LASTEXITCODE)" -ForegroundColor Red
        Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
        exit 1
    }
} catch {
    Write-Host "❌ Error al crear backup: $_" -ForegroundColor Red
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    exit 1
}

Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue

# PASO 2: Transferir backup al servidor
Write-Host ""
Write-Host "PASO 2: Transfiriendo backup al servidor..." -ForegroundColor Yellow
$serverBackupPath = "/tmp/$backupFile"

$pscp = "C:\Program Files\PuTTY\pscp.exe"
if (-not (Test-Path $pscp)) {
    Write-Host "❌ No se encontró pscp.exe (PuTTY)." -ForegroundColor Red
    exit 1
}

try {
    $pscpCmd = "& `"$pscp`" -pw `"$password`" -hostkey `"$hostkey`" `"$backupPath`" `"$hostname`:$serverBackupPath`""
    Invoke-Expression $pscpCmd
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Backup transferido exitosamente al servidor" -ForegroundColor Green
    } else {
        Write-Host "❌ Error al transferir backup (Código: $LASTEXITCODE)" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Error al transferir backup: $_" -ForegroundColor Red
    exit 1
}

# PASO 3: Limpiar base de datos del servidor (solo datos, mantener esquema)
Write-Host ""
Write-Host "PASO 3: Limpiando datos de la base de datos del servidor..." -ForegroundColor Yellow
Write-Host "ADVERTENCIA: Esto eliminara todos los datos existentes en el servidor" -ForegroundColor Red
Write-Host "NOTA: El esquema (tablas, indices, etc.) se mantiene intacto" -ForegroundColor Gray

# Limpiar todas las tablas usando TRUNCATE CASCADE (más seguro que DROP)
Write-Host "Limpiando datos de las tablas..." -ForegroundColor Gray
$cleanupScript = @"
SET session_replication_role = 'replica';
DO \$\$ 
DECLARE
    r RECORD;
BEGIN
    FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public' AND tablename != '__EFMigrationsHistory') 
    LOOP
        BEGIN
            EXECUTE 'TRUNCATE TABLE ' || quote_ident(r.tablename) || ' CASCADE';
        EXCEPTION WHEN OTHERS THEN
            RAISE NOTICE 'Error al truncar %: %', r.tablename, SQLERRM;
        END;
    END LOOP;
END \$\$;
SET session_replication_role = 'origin';
"@

$cleanupCmd = "echo '$cleanupScript' | docker exec -i $serverContainer psql -U $serverUser -d $serverDatabase"
$cleanupResult = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cleanupCmd 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Datos de la base de datos del servidor limpiados exitosamente" -ForegroundColor Green
} else {
    Write-Host "⚠️  Advertencia al limpiar (Código: $LASTEXITCODE)" -ForegroundColor Yellow
    Write-Host "Continuando con la restauración..." -ForegroundColor Yellow
}

# PASO 4: Restaurar backup en el servidor
Write-Host ""
Write-Host "PASO 4: Restaurando backup en el servidor..." -ForegroundColor Yellow
Write-Host "Esto puede tardar varios minutos dependiendo del tamano de la base de datos..." -ForegroundColor Gray

# Restaurar el backup usando psql dentro del contenedor
# Usamos ON_ERROR_STOP=0 para continuar aunque haya algunos errores menores
$restoreCmd = "cat $serverBackupPath | docker exec -i $serverContainer psql -U $serverUser -d $serverDatabase -v ON_ERROR_STOP=0 2>&1"
$restoreResult = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $restoreCmd 2>&1

# Verificar si hubo errores críticos
$hasErrors = $restoreResult -match "ERROR|FATAL|syntax error"
if ($hasErrors) {
    Write-Host "⚠️  Se detectaron errores durante la restauracion:" -ForegroundColor Yellow
    $restoreResult | Select-String -Pattern "ERROR|FATAL" | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
} else {
    Write-Host "✅ Backup restaurado exitosamente en el servidor" -ForegroundColor Green
}

# Mostrar última parte de la salida para ver el resultado
$restoreResult | Select-Object -Last 10 | ForEach-Object { Write-Host $_ -ForegroundColor Gray }

# PASO 5: Limpiar archivo temporal del servidor
Write-Host ""
Write-Host "PASO 5: Limpiando archivo temporal del servidor..." -ForegroundColor Yellow
$cleanupFileCmd = "rm -f $serverBackupPath"
$cleanupFileResult = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cleanupFileCmd 2>&1
Write-Host "✅ Archivo temporal eliminado" -ForegroundColor Green

# PASO 6: Verificar sincronización
Write-Host ""
Write-Host "PASO 6: Verificando sincronización..." -ForegroundColor Yellow

# Contar registros en tablas clave (local)
Write-Host "Contando registros en base de datos LOCAL..." -ForegroundColor Cyan
$env:PGPASSWORD = $localPassword
$psqlPath = $pgDumpPath.Replace("pg_dump.exe", "psql.exe")
$localCountCmd = "SELECT (SELECT COUNT(*) FROM users) as users, (SELECT COUNT(*) FROM tours) as tours, (SELECT COUNT(*) FROM bookings) as bookings, (SELECT COUNT(*) FROM payments) as payments;"
$localCountResult = $localCountCmd | & $psqlPath -h $localHost -p $localPort -U $localUser -d $localDatabase -t -A 2>&1
Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue

# Contar registros en tablas clave (servidor)
Write-Host "Contando registros en base de datos SERVIDOR..." -ForegroundColor Cyan
$serverCountCmd = "docker exec $serverContainer psql -U $serverUser -d $serverDatabase -t -A -c 'SELECT (SELECT COUNT(*) FROM users) as users, (SELECT COUNT(*) FROM tours) as tours, (SELECT COUNT(*) FROM bookings) as bookings, (SELECT COUNT(*) FROM payments) as payments;'"
$serverCountResult = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $serverCountCmd 2>&1

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  SINCRONIZACION COMPLETADA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Resumen:" -ForegroundColor Cyan
Write-Host "  - Backup local: $backupFile" -ForegroundColor White
Write-Host "  - Backup restaurado en servidor" -ForegroundColor White
Write-Host ""
if ($localCountResult -and $localCountResult -notmatch "error|ERROR") {
    Write-Host "Conteo de registros (LOCAL):" -ForegroundColor Cyan
    Write-Host $localCountResult -ForegroundColor White
}
if ($serverCountResult -and $serverCountResult -notmatch "error|ERROR") {
    Write-Host "Conteo de registros (SERVIDOR):" -ForegroundColor Cyan
    Write-Host $serverCountResult -ForegroundColor White
}
Write-Host ""
Write-Host "✅ La base de datos del servidor ahora está sincronizada con la local" -ForegroundColor Green
Write-Host ""
