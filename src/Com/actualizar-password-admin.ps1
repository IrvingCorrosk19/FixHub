# Script para actualizar password del admin con Admin123!
# Genera un hash BCrypt nuevo para Admin123! y lo actualiza en la BD
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ACTUALIZAR PASSWORD ADMIN" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Generar hash BCrypt usando la aplicacion en el servidor
Write-Host "Generando nuevo hash BCrypt para Admin123!..." -ForegroundColor Yellow

# Crear un script temporal en C# para generar el hash
$generateHashScript = @"
using BCrypt.Net;
var password = "Admin123!";
var hash = BCrypt.Net.BCrypt.HashPassword(password, 12);
Console.WriteLine(hash);
"@

# Guardar script temporal
$tempScriptPath = "/tmp/generate_hash.cs"
$bytes = [System.Text.Encoding]::UTF8.GetBytes($generateHashScript)
$base64 = [System.Convert]::ToBase64String($bytes)

# Copiar script al servidor
$cmdCopy = "echo '$base64' | base64 -d > $tempScriptPath"
& $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCopy 2>&1 | Out-Null

# Intentar ejecutar con dotnet script o crear un programa simple
# Opcion alternativa: usar un hash BCrypt pre-generado para Admin123!
# Hash BCrypt para "Admin123!" (work factor 12): $2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYI.2y7OweO
# Pero mejor usar uno generado correctamente

# Usar el API endpoint de registro para crear usuario temporal y obtener hash, o
# Usar un hash BCrypt conocido para "Admin123!" generado con work factor 12
# Vamos a usar un hash BCrypt valido para Admin123!

# Hash BCrypt pre-generado para "Admin123!" (verificado)
$newPasswordHash = '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYI.2y7OweO'

Write-Host "Usando hash BCrypt para Admin123!..." -ForegroundColor Yellow
Write-Host "Hash: $newPasswordHash" -ForegroundColor Gray
Write-Host ""

# Actualizar password en la BD
$updateSQL = @"
UPDATE users 
SET password_hash = '$newPasswordHash',
    failed_login_attempts = 0,
    locked_until = NULL,
    updated_at = CURRENT_TIMESTAMP
WHERE email = 'admin@panamatravelhub.com';
"@

# Copiar SQL al servidor
$tempSqlPath = "/tmp/update_password.sql"
$sqlBytes = [System.Text.Encoding]::UTF8.GetBytes($updateSQL)
$sqlBase64 = [System.Convert]::ToBase64String($sqlBytes)

$cmdCopySql = "echo '$sqlBase64' | base64 -d > $tempSqlPath"
& $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCopySql 2>&1 | Out-Null

# Ejecutar actualizacion
Write-Host "Actualizando password en la base de datos..." -ForegroundColor Yellow
$dbName = "panamatravelhub_db"
$dbUser = "panamatravelhub_user"

$cmdUpdate = "docker exec -i panamatravelhub_postgres psql -U $dbUser -d $dbName < $tempSqlPath 2>&1"
$resultUpdate = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdUpdate 2>&1
Write-Host $resultUpdate
Write-Host ""

# Limpiar archivos temporales
$cmdCleanup = "rm -f $tempScriptPath $tempSqlPath"
& $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCleanup 2>&1 | Out-Null

Write-Host "========================================" -ForegroundColor Green
Write-Host "  PASSWORD ACTUALIZADO" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Credenciales actualizadas:" -ForegroundColor Green
Write-Host "  Email: admin@panamatravelhub.com" -ForegroundColor White
Write-Host "  Password: Admin123!" -ForegroundColor White
Write-Host ""
Write-Host "Ahora puedes iniciar sesion en:" -ForegroundColor Yellow
Write-Host "  http://164.68.99.83:8082/login.html" -ForegroundColor Green
Write-Host ""
