# Script para importar usuarios desde temp_sync/data.sql al servidor
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  IMPORTAR USUARIOS LOCAL -> SERVIDOR" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Leer archivo temp_sync/data.sql
$dataSqlPath = "C:\Proyectos\PanamaTravelHub\PanamaTravelHub\database\temp_sync\data.sql"

if (-not (Test-Path $dataSqlPath)) {
    Write-Host "Error: No se encontro el archivo $dataSqlPath" -ForegroundColor Red
    exit 1
}

Write-Host "Leyendo archivo: $dataSqlPath" -ForegroundColor Yellow
$content = Get-Content $dataSqlPath -Raw -Encoding UTF8

# Extraer solo las secciones de users y user_roles
Write-Host "Extrayendo datos de usuarios y roles..." -ForegroundColor Yellow

$importSQL = @"
-- Importar usuarios y user_roles desde BD local

"@

# Extraer usuarios (COPY statement)
if ($content -match "COPY public\.users.*?FROM stdin;(.*?)\.;") {
    $usersData = $matches[1].Trim()
    
    $importSQL += @"
-- Importar usuarios
INSERT INTO users (id, email, password_hash, first_name, last_name, phone, is_active, failed_login_attempts, locked_until, last_login_at, created_at, updated_at)
VALUES
$usersData
ON CONFLICT (id) DO UPDATE SET
    email = EXCLUDED.email,
    password_hash = EXCLUDED.password_hash,
    first_name = EXCLUDED.first_name,
    last_name = EXCLUDED.last_name,
    phone = EXCLUDED.phone,
    is_active = EXCLUDED.is_active,
    failed_login_attempts = EXCLUDED.failed_login_attempts,
    locked_until = EXCLUDED.locked_until,
    last_login_at = EXCLUDED.locked_until,
    updated_at = CURRENT_TIMESTAMP;

"@
    
    # Convertir COPY formato a INSERT formato
    $lines = $usersData -split "`n"
    $values = @()
    foreach ($line in $lines) {
        $line = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        
        # Parsear formato COPY: id\temail\tpassword_hash\t...
        $parts = $line -split "`t"
        if ($parts.Length -ge 6) {
            $id = $parts[0]
            $email = $parts[1] -replace "'", "''"
            $password_hash = $parts[2] -replace "'", "''"
            $first_name = ($parts[3] -replace "'", "''") -replace "`\N", "NULL"
            $last_name = ($parts[4] -replace "'", "''") -replace "`\N", "NULL"
            $phone = if ($parts.Length -gt 5 -and $parts[5] -ne "`\N") { "'$($parts[5] -replace "'", "''")'" } else { "NULL" }
            $is_active = if ($parts[6] -eq "t") { "true" } else { "false" }
            $failed_attempts = if ($parts.Length -gt 7 -and $parts[7] -ne "`\N") { $parts[7] } else { "0" }
            $locked_until = if ($parts.Length -gt 8 -and $parts[8] -ne "`\N") { "'$($parts[8])'" } else { "NULL" }
            $last_login = if ($parts.Length -gt 9 -and $parts[9] -ne "`\N") { "'$($parts[9])'" } else { "NULL" }
            $created_at = if ($parts.Length -gt 10) { "'$($parts[10])'" } else { "CURRENT_TIMESTAMP" }
            $updated_at = if ($parts.Length -gt 11 -and $parts[11] -ne "`\N") { "'$($parts[11])'" } else { "NULL" }
            
            $values += "('$id', '$email', '$password_hash', '$first_name', '$last_name', $phone, $is_active, $failed_attempts, $locked_until, $last_login, $created_at, $updated_at)"
        }
    }
    
    if ($values.Count -gt 0) {
        $importSQL = "INSERT INTO users (id, email, password_hash, first_name, last_name, phone, is_active, failed_login_attempts, locked_until, last_login_at, created_at, updated_at)`nVALUES`n" + ($values -join ",`n") + "`nON CONFLICT (id) DO UPDATE SET`n    email = EXCLUDED.email,`n    password_hash = EXCLUDED.password_hash,`n    first_name = EXCLUDED.first_name,`n    last_name = EXCLUDED.last_name,`n    phone = EXCLUDED.phone,`n    is_active = EXCLUDED.is_active,`n    updated_at = CURRENT_TIMESTAMP;`n`n"
    }
}

# Extraer user_roles
if ($content -match "COPY public\.user_roles.*?FROM stdin;(.*?)\.;") {
    $rolesData = $matches[1].Trim()
    
    $lines = $rolesData -split "`n"
    $roleValues = @()
    foreach ($line in $lines) {
        $line = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        
        $parts = $line -split "`t"
        if ($parts.Length -ge 4) {
            $id = $parts[0]
            $user_id = $parts[1]
            $role_id = $parts[2]
            $created_at = if ($parts.Length -gt 3) { "'$($parts[3])'" } else { "CURRENT_TIMESTAMP" }
            
            $roleValues += "('$id', '$user_id', '$role_id', $created_at)"
        }
    }
    
    if ($roleValues.Count -gt 0) {
        $importSQL += "INSERT INTO user_roles (id, user_id, role_id, created_at)`nVALUES`n" + ($roleValues -join ",`n") + "`nON CONFLICT (user_id, role_id) DO NOTHING;`n"
    }
}

if ([string]::IsNullOrWhiteSpace($importSQL) -or $importSQL -match "^INSERT INTO users.*VALUES") {
    Write-Host "Error: No se pudieron extraer usuarios del archivo" -ForegroundColor Red
    exit 1
}

Write-Host "Usuarios encontrados para importar:" -ForegroundColor Green
Write-Host "  - admin@panamatravelhub.com" -ForegroundColor Gray
Write-Host "  - cliente@panamatravelhub.com" -ForegroundColor Gray
Write-Host ""

# Copiar script al servidor usando base64
Write-Host "Copiando script al servidor..." -ForegroundColor Yellow
$bytes = [System.Text.Encoding]::UTF8.GetBytes($importSQL)
$base64 = [System.Convert]::ToBase64String($bytes)

$tempScript = "import_users_$(Get-Date -Format 'yyyyMMdd_HHmmss').sql"
$remoteScriptPath = "/tmp/$tempScript"

$cmdCopy = "echo '$base64' | base64 -d > $remoteScriptPath"
$resultCopy = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCopy 2>&1

# Importar en el servidor
Write-Host "Ejecutando importacion en servidor..." -ForegroundColor Yellow
$dbName = "panamatravelhub_db"
$dbUser = "panamatravelhub_user"

$cmdImport = "docker exec -i panamatravelhub_postgres psql -U $dbUser -d $dbName < $remoteScriptPath 2>&1"
$resultImport = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdImport 2>&1

Write-Host $resultImport
Write-Host ""

# Limpiar archivo temporal
$cmdCleanup = "rm -f $remoteScriptPath"
& $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCleanup 2>&1 | Out-Null

Write-Host ""
Write-Host "Verificando usuarios importados..." -ForegroundColor Yellow
$cmdVerify = "docker exec panamatravelhub_postgres psql -U $dbUser -d $dbName -c 'SELECT u.id, u.email, u.first_name, u.last_name, u.is_active, r.name as role FROM users u LEFT JOIN user_roles ur ON u.id = ur.user_id LEFT JOIN roles r ON ur.role_id = r.id ORDER BY u.created_at;' 2>&1"
$resultVerify = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdVerify 2>&1
Write-Host $resultVerify
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "  IMPORTACION COMPLETADA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
