# Script para sincronizar usuarios de BD local al servidor
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SINCRONIZAR USUARIOS LOCAL -> SERVIDOR" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Paso 1: Verificar usuarios en BD local
Write-Host "PASO 1: Verificando usuarios en BD LOCAL..." -ForegroundColor Yellow

# Intentar con Docker primero
$localDockerPostgres = docker ps --filter name=postgres --format "{{.Names}}" 2>&1 | Select-Object -First 1

if ($localDockerPostgres -and $localDockerPostgres -notmatch "error") {
    Write-Host "PostgreSQL encontrado en Docker: $localDockerPostgres" -ForegroundColor Green
    
    # Consultar usuarios en Docker local
    Write-Host "Consultando usuarios..." -ForegroundColor Gray
    $localUsersQuery = "docker exec -i $localDockerPostgres psql -U postgres -d PanamaTravelHub -c `"SELECT id, email, password_hash, first_name, last_name, is_active, created_at FROM users ORDER BY created_at;`" -t -A 2>&1"
    
    $localUsers = Invoke-Expression $localUsersQuery
    
    if ($localUsers -and $localUsers -notmatch "error" -and $localUsers -notmatch "No such") {
        Write-Host "Usuarios encontrados en local (Docker):" -ForegroundColor Green
        Write-Host $localUsers
        Write-Host ""
        
        # Exportar datos completos de usuarios
        Write-Host "Exportando datos completos de usuarios..." -ForegroundColor Yellow
        $exportQuery = "docker exec -i $localDockerPostgres pg_dump -U postgres -d PanamaTravelHub -t users -t user_roles -a --column-inserts 2>&1"
        $exportData = Invoke-Expression $exportQuery
        
        if ($exportData -and $exportData -notmatch "error") {
            Write-Host "Datos exportados correctamente" -ForegroundColor Green
            $useLocalDocker = $true
        } else {
            Write-Host "Error exportando datos" -ForegroundColor Red
            $useLocalDocker = $false
        }
    } else {
        Write-Host "No se pudieron consultar usuarios en Docker local" -ForegroundColor Yellow
        $useLocalDocker = $false
    }
} else {
    Write-Host "PostgreSQL no encontrado en Docker local" -ForegroundColor Yellow
    $useLocalDocker = $false
}

# Si Docker no funcionó, intentar con archivos SQL de exportación
if (-not $useLocalDocker) {
    Write-Host ""
    Write-Host "Buscando archivos de exportación de datos..." -ForegroundColor Yellow
    
    $exportFiles = @(
        "database\export_business_data.sql",
        "database\13_sync_render_data.sql",
        "database\temp_sync\data.sql"
    )
    
    $foundExportFile = $null
    foreach ($file in $exportFiles) {
        if (Test-Path $file) {
            Write-Host "Archivo encontrado: $file" -ForegroundColor Green
            $foundExportFile = $file
            break
        }
    }
    
    if ($foundExportFile) {
        Write-Host "Usando archivo: $foundExportFile" -ForegroundColor Green
        $exportData = Get-Content $foundExportFile -Raw -Encoding UTF8
    } else {
        Write-Host "No se encontraron archivos de exportación" -ForegroundColor Red
        Write-Host "Por favor, exporta manualmente los usuarios desde tu BD local" -ForegroundColor Yellow
        exit 1
    }
}

Write-Host ""
Write-Host "PASO 2: Importando usuarios al servidor..." -ForegroundColor Yellow

# Preparar script SQL para importar
$tempScript = "temp_import_users_$(Get-Date -Format 'yyyyMMdd_HHmmss').sql"
$tempScriptPath = Join-Path $PSScriptRoot $tempScript

# Filtrar solo INSERT de users y user_roles
$importSQL = ""
if ($exportData) {
    # Extraer solo INSERTs de users y user_roles
    $lines = $exportData -split "`n"
    $inUserTable = $false
    $inUserRolesTable = $false
    
    foreach ($line in $lines) {
        if ($line -match "INSERT INTO.*users.*VALUES") {
            $inUserTable = $true
            $importSQL += "$line`n"
        }
        elseif ($line -match "INSERT INTO.*user_roles.*VALUES") {
            $inUserRolesTable = $true
            $importSQL += "$line`n"
        }
        elseif ($line -match "^\s*\);?\s*$" -or $line -match "^INSERT INTO") {
            if ($inUserTable -or $inUserRolesTable) {
                $importSQL += "$line`n"
            }
            $inUserTable = $false
            $inUserRolesTable = $false
        }
        elseif (($inUserTable -or $inUserRolesTable) -and $line -notmatch "^--") {
            $importSQL += "$line`n"
        }
    }
    
    # Agregar ON CONFLICT DO NOTHING si no está presente
    if ($importSQL -notmatch "ON CONFLICT") {
        $importSQL = $importSQL -replace "\);", ") ON CONFLICT DO NOTHING;"
    }
}

if ([string]::IsNullOrWhiteSpace($importSQL)) {
    Write-Host "Error: No se encontraron datos de usuarios para importar" -ForegroundColor Red
    exit 1
}

# Guardar script temporal
$importSQL | Out-File -FilePath $tempScriptPath -Encoding UTF8 -NoNewline
Write-Host "Script temporal creado: $tempScript" -ForegroundColor Gray

# Copiar script al servidor usando base64
$bytes = [System.Text.Encoding]::UTF8.GetBytes($importSQL)
$base64 = [System.Convert]::ToBase64String($bytes)

Write-Host "Copiando script al servidor..." -ForegroundColor Gray
$remoteScriptPath = "/tmp/$tempScript"
$cmdCopy = "echo '$base64' | base64 -d > $remoteScriptPath"
$resultCopy = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCopy 2>&1

# Importar en el servidor
Write-Host "Ejecutando importación en servidor..." -ForegroundColor Gray
$dbName = "panamatravelhub_db"
$dbUser = "panamatravelhub_user"

$cmdImport = "docker exec -i panamatravelhub_postgres psql -U $dbUser -d $dbName < $remoteScriptPath 2>&1"
$resultImport = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdImport 2>&1

Write-Host $resultImport
Write-Host ""

# Limpiar archivo temporal
$cmdCleanup = "rm -f $remoteScriptPath"
& $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCleanup 2>&1 | Out-Null
Remove-Item -Path $tempScriptPath -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "PASO 3: Verificando usuarios en servidor..." -ForegroundColor Yellow
$cmdVerify = "docker exec panamatravelhub_postgres psql -U $dbUser -d $dbName -c 'SELECT u.id, u.email, u.first_name, u.last_name, u.is_active, r.name as role FROM users u LEFT JOIN user_roles ur ON u.id = ur.user_id LEFT JOIN roles r ON ur.role_id = r.id ORDER BY u.created_at;' 2>&1"
$resultVerify = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdVerify 2>&1
Write-Host $resultVerify
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "  SINCRONIZACION COMPLETADA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
