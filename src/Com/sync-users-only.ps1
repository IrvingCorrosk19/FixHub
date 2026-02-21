# Script para sincronizar SOLO los usuarios (con password hashes correctos) del local al servidor
# Uso: .\sync-users-only.ps1 [-Force]

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

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SINCRONIZACION DE USUARIOS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Este script sincronizara los usuarios y sus password hashes" -ForegroundColor Yellow
Write-Host "del servidor local al servidor Docker." -ForegroundColor Yellow
Write-Host ""

if (-not $Force) {
    try {
        $confirm = Read-Host "Estas seguro que quieres continuar? (escribe 'SI' para confirmar)"
        if ($confirm -ne "SI") {
            Write-Host "Operacion cancelada." -ForegroundColor Yellow
            exit
        }
    } catch {
        Write-Host "Modo no interactivo detectado. Continuando automaticamente..." -ForegroundColor Yellow
        Write-Host ""
    }
} else {
    Write-Host "Modo forzado activado. Continuando sin confirmacion..." -ForegroundColor Yellow
    Write-Host ""
}

# PASO 1: Extraer usuarios de la base de datos local
Write-Host "PASO 1: Extrayendo usuarios de la base de datos local..." -ForegroundColor Yellow

$env:PGPASSWORD = $localPassword
$psqlPath = "C:\Program Files\PostgreSQL\18\bin\psql.exe"

# Verificar que psql existe
if (-not (Test-Path $psqlPath)) {
    $psqlPath = Get-ChildItem "C:\Program Files\PostgreSQL" -Recurse -Filter "psql.exe" -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
    if (-not $psqlPath) {
        Write-Host "❌ No se encontro psql.exe." -ForegroundColor Red
        exit 1
    }
}

# Extraer usuarios con campos compatibles (sin email_verified que no existe en servidor)
$extractQuery = @"
SELECT 
    id::text,
    email,
    password_hash,
    first_name,
    last_name,
    COALESCE(phone, '') as phone,
    is_active::text,
    failed_login_attempts::text,
    COALESCE(locked_until::text, '') as locked_until,
    COALESCE(last_login_at::text, '') as last_login_at,
    created_at::text,
    COALESCE(updated_at::text, '') as updated_at
FROM users
ORDER BY created_at;
"@

Write-Host "Extrayendo datos de usuarios..." -ForegroundColor Gray
$usersData = $extractQuery | & $psqlPath -h $localHost -p $localPort -U $localUser -d $localDatabase -t -A -F "|" 2>&1
Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue

if ($LASTEXITCODE -ne 0 -or -not $usersData) {
    Write-Host "❌ Error al extraer usuarios de la base de datos local" -ForegroundColor Red
    Write-Host $usersData -ForegroundColor Red
    exit 1
}

$userLines = $usersData -split "`n" | Where-Object { $_ -and $_ -notmatch "^\s*$" }
$userCount = ($userLines | Measure-Object).Count

Write-Host "✅ Se encontraron $userCount usuarios en la base de datos local" -ForegroundColor Green
Write-Host ""

# PASO 2: Limpiar usuarios existentes en el servidor (mantener estructura)
Write-Host "PASO 2: Limpiando usuarios existentes en el servidor..." -ForegroundColor Yellow

$cleanupCmd = "docker exec $serverContainer psql -U $serverUser -d $serverDatabase -c 'TRUNCATE TABLE user_roles CASCADE; TRUNCATE TABLE users CASCADE;'"
$cleanupResult = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cleanupCmd 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Usuarios limpiados en el servidor" -ForegroundColor Green
} else {
    Write-Host "⚠️  Advertencia al limpiar usuarios (Codigo: $LASTEXITCODE)" -ForegroundColor Yellow
    Write-Host "Continuando..." -ForegroundColor Yellow
}
Write-Host ""

# PASO 3: Insertar usuarios en el servidor
Write-Host "PASO 3: Insertando usuarios en el servidor..." -ForegroundColor Yellow

$successCount = 0
$errorCount = 0

foreach ($userLine in $userLines) {
    if (-not $userLine -or $userLine -match "^\s*$") { continue }
    
    $fields = $userLine -split "\|"
    if ($fields.Count -lt 12) {
        Write-Host "⚠️  Linea invalida (campos insuficientes): $userLine" -ForegroundColor Yellow
        $errorCount++
        continue
    }
    
    $userId = $fields[0].Trim()
    $email = $fields[1].Trim().Replace("'", "''")
    $passwordHash = $fields[2].Trim().Replace("'", "''")
    $firstName = $fields[3].Trim().Replace("'", "''")
    $lastName = $fields[4].Trim().Replace("'", "''")
    $phone = $fields[5].Trim().Replace("'", "''")
    $isActive = $fields[6].Trim()
    $failedLoginAttempts = $fields[7].Trim()
    $lockedUntil = $fields[8].Trim()
    $lastLoginAt = $fields[9].Trim()
    $createdAt = $fields[10].Trim()
    $updatedAt = $fields[11].Trim()
    
    # Construir query de inserción (sin columnas email_verified que no existen en servidor)
    $insertQuery = "INSERT INTO users ("
    $insertQuery += "id, email, password_hash, first_name, last_name, "
    $insertQuery += "phone, is_active, failed_login_attempts, "
    $insertQuery += "locked_until, last_login_at, "
    $insertQuery += "created_at, updated_at"
    $insertQuery += ") VALUES ("
    $insertQuery += "'$userId', '$email', '$passwordHash', '$firstName', '$lastName', "
    
    if ($phone) {
        $insertQuery += "'$phone', "
    } else {
        $insertQuery += "NULL, "
    }
    
    $insertQuery += "$isActive, $failedLoginAttempts, "
    
    if ($lockedUntil) {
        $insertQuery += "'$lockedUntil', "
    } else {
        $insertQuery += "NULL, "
    }
    
    if ($lastLoginAt) {
        $insertQuery += "'$lastLoginAt', "
    } else {
        $insertQuery += "NULL, "
    }
    
    $insertQuery += "'$createdAt', "
    
    if ($updatedAt) {
        $insertQuery += "'$updatedAt'"
    } else {
        $insertQuery += "NULL"
    }
    
    $insertQuery += ") ON CONFLICT (id) DO UPDATE SET "
    $insertQuery += "email = EXCLUDED.email, "
    $insertQuery += "password_hash = EXCLUDED.password_hash, "
    $insertQuery += "first_name = EXCLUDED.first_name, "
    $insertQuery += "last_name = EXCLUDED.last_name, "
    $insertQuery += "phone = EXCLUDED.phone, "
    $insertQuery += "is_active = EXCLUDED.is_active, "
    $insertQuery += "failed_login_attempts = EXCLUDED.failed_login_attempts, "
    $insertQuery += "locked_until = EXCLUDED.locked_until, "
    $insertQuery += "last_login_at = EXCLUDED.last_login_at, "
    $insertQuery += "updated_at = EXCLUDED.updated_at;"
    
    # Crear un archivo SQL local temporal y transferirlo al servidor
    $tempLocalFile = [System.IO.Path]::GetTempFileName()
    $insertQuery | Out-File -FilePath $tempLocalFile -Encoding UTF8 -NoNewline
    
    # Transferir archivo al servidor
    $pscp = "C:\Program Files\PuTTY\pscp.exe"
    $tempServerFile = "/tmp/insert_user_$userId.sql"
    $pscpCmd = "& `"$pscp`" -pw `"$password`" -hostkey `"$hostkey`" `"$tempLocalFile`" `"$hostname`:$tempServerFile`""
    Invoke-Expression $pscpCmd | Out-Null
    
    # Copiar archivo al contenedor y ejecutarlo
    $copyCmd = "docker cp $tempServerFile $serverContainer`:/tmp/insert_user.sql"
    $copyResult = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $copyCmd 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ⚠️  Error al copiar archivo al contenedor: $copyResult" -ForegroundColor Yellow
        $errorCount++
        continue
    }
    
    # Ejecutar SQL dentro del contenedor
    $insertCmd = "docker exec $serverContainer psql -U $serverUser -d $serverDatabase -f /tmp/insert_user.sql"
    $insertResult = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $insertCmd 2>&1
    
    # Limpiar archivo dentro del contenedor
    $cleanupContainerCmd = "docker exec $serverContainer rm -f /tmp/insert_user.sql"
    & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cleanupContainerCmd 2>&1 | Out-Null
    
    # Limpiar archivos temporales
    Remove-Item $tempLocalFile -ErrorAction SilentlyContinue
    $cleanupCmd = "rm -f $tempServerFile"
    & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cleanupCmd 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0 -and $insertResult -notmatch "ERROR") {
        Write-Host "  ✅ Usuario sincronizado: $email" -ForegroundColor Green
        $successCount++
    } else {
        Write-Host "  ❌ Error al sincronizar usuario: $email" -ForegroundColor Red
        Write-Host "    $insertResult" -ForegroundColor Red
        $errorCount++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  SINCRONIZACION COMPLETADA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Resumen:" -ForegroundColor Cyan
Write-Host "  - Usuarios sincronizados exitosamente: $successCount" -ForegroundColor Green
Write-Host "  - Errores: $errorCount" -ForegroundColor $(if ($errorCount -gt 0) { "Red" } else { "Green" })
Write-Host ""
Write-Host "✅ Los usuarios ahora deberian poder iniciar sesion en el servidor" -ForegroundColor Green
Write-Host ""
