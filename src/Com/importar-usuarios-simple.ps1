# Script simplificado para importar usuarios desde temp_sync/data.sql
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  IMPORTAR USUARIOS LOCAL -> SERVIDOR" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Datos de usuarios desde temp_sync/data.sql
# Usuario 1: admin@panamatravelhub.com
# Usuario 2: cliente@panamatravelhub.com

$importSQL = @"
-- Importar usuarios desde BD local

-- Usuario Admin
INSERT INTO users (id, email, password_hash, first_name, last_name, phone, is_active, failed_login_attempts, locked_until, last_login_at, created_at, updated_at)
VALUES (
    '24e8864d-7bbf-4fdf-b59a-0cfa3b882386',
    'admin@panamatravelhub.com',
    '$2a$12$gpmcPqtakrNDl29T9mDeqOjzeVjACvG/RRyjAdxH3.u58TZG6g8yS',
    'Administrador',
    'Sistema',
    NULL,
    true,
    0,
    NULL,
    '2025-12-26 13:49:37.074765',
    '2025-12-25 03:22:07.892489',
    '2025-12-26 06:24:40.899683'
)
ON CONFLICT (id) DO UPDATE SET
    email = EXCLUDED.email,
    password_hash = EXCLUDED.password_hash,
    first_name = EXCLUDED.first_name,
    last_name = EXCLUDED.last_name,
    is_active = EXCLUDED.is_active,
    updated_at = CURRENT_TIMESTAMP;

-- Usuario Cliente
INSERT INTO users (id, email, password_hash, first_name, last_name, phone, is_active, failed_login_attempts, locked_until, last_login_at, created_at, updated_at)
VALUES (
    '6093a936-f8b0-49da-bf2c-16e426df5e69',
    'cliente@panamatravelhub.com',
    '$2a$12$3c0sjSm98/xkoU7ZYo3GxO9t80ldYwoEmieY6qYrInr1eqw5la0Ea',
    'irving',
    'corro',
    NULL,
    true,
    0,
    NULL,
    NULL,
    '2025-12-26 06:28:13.867754',
    NULL
)
ON CONFLICT (id) DO UPDATE SET
    email = EXCLUDED.email,
    password_hash = EXCLUDED.password_hash,
    first_name = EXCLUDED.first_name,
    last_name = EXCLUDED.last_name,
    is_active = EXCLUDED.is_active,
    updated_at = CURRENT_TIMESTAMP;

-- Asignar rol Admin (el ID del rol Admin es 00000000-0000-0000-0000-000000000002 segun seed_data.sql)
-- Buscar el rol Admin primero
INSERT INTO user_roles (id, user_id, role_id, created_at, updated_at)
SELECT 
    gen_random_uuid(),
    '24e8864d-7bbf-4fdf-b59a-0cfa3b882386',
    r.id,
    CURRENT_TIMESTAMP,
    NULL
FROM roles r
WHERE r.name = 'Admin'
ON CONFLICT (user_id, role_id) DO NOTHING;

-- Asignar rol Customer al cliente
INSERT INTO user_roles (id, user_id, role_id, created_at, updated_at)
SELECT 
    gen_random_uuid(),
    '6093a936-f8b0-49da-bf2c-16e426df5e69',
    r.id,
    CURRENT_TIMESTAMP,
    NULL
FROM roles r
WHERE r.name = 'Customer'
ON CONFLICT (user_id, role_id) DO NOTHING;

"@

Write-Host "Usuarios a importar:" -ForegroundColor Green
Write-Host "  1. admin@panamatravelhub.com (Admin)" -ForegroundColor White
Write-Host "  2. cliente@panamatravelhub.com (Customer)" -ForegroundColor White
Write-Host ""

# Copiar script al servidor usando base64
Write-Host "Copiando script al servidor..." -ForegroundColor Yellow
$bytes = [System.Text.Encoding]::UTF8.GetBytes($importSQL)
$base64 = [System.Convert]::ToBase64String($bytes)

$tempScript = "import_users_$(Get-Date -Format 'yyyyMMdd_HHmmss').sql"
$remoteScriptPath = "/tmp/$tempScript"

$cmdCopy = "echo '$base64' | base64 -d > $remoteScriptPath"
$resultCopy = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCopy 2>&1

if ($resultCopy -match "error" -or $LASTEXITCODE -ne 0) {
    Write-Host "Error copiando script: $resultCopy" -ForegroundColor Red
    exit 1
}

Write-Host "Script copiado correctamente" -ForegroundColor Green
Write-Host ""

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
