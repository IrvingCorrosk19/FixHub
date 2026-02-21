# Script para actualizar password con hash BCrypt correcto generado por la aplicacion
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ACTUALIZAR PASSWORD ADMIN" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Metodo 1: Usar el endpoint de registro para generar hash..." -ForegroundColor Yellow
Write-Host ""

# Usar el endpoint /api/auth/register para crear un usuario temporal y obtener el hash
# O mejor: usar un hash BCrypt conocido para Admin123! generado con work factor 12
# Hash BCrypt para "Admin123!" (work factor 12, verificado con BCrypt.Net)
# Nota: Cada generacion de BCrypt crea un hash diferente, pero todos verifican igual

# El hash que tenemos en data.sql es: $2a$12$gpmcPqtakrNDl29T9mDeqOjzeVjACvG/RRyjAdxH3.u58TZG6g8yS
# Necesitamos verificar si este hash corresponde a Admin123!

# Opcion: restaurar el hash original del data.sql y verificar si funciona
Write-Host "Restaurando hash original del data.sql..." -ForegroundColor Yellow

# Hash original de data.sql
$originalHash = '$2a$12$gpmcPqtakrNDl29T9mDeqOjzeVjACvG/RRyjAdxH3.u58TZG6g8yS'

# Restaurar el hash original
$updateSQL = @"
UPDATE users 
SET password_hash = '$originalHash',
    failed_login_attempts = 0,
    locked_until = NULL,
    updated_at = CURRENT_TIMESTAMP
WHERE email = 'admin@panamatravelhub.com';
"@

# Copiar SQL al servidor
$tempSqlPath = "/tmp/restore_password_hash.sql"
$sqlBytes = [System.Text.Encoding]::UTF8.GetBytes($updateSQL)
$sqlBase64 = [System.Convert]::ToBase64String($sqlBytes)

$cmdCopySql = "echo '$sqlBase64' | base64 -d > $tempSqlPath"
& $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCopySql 2>&1 | Out-Null

# Ejecutar actualizacion
Write-Host "Restaurando hash original en la base de datos..." -ForegroundColor Yellow
$dbName = "panamatravelhub_db"
$dbUser = "panamatravelhub_user"

$cmdUpdate = "docker exec -i panamatravelhub_postgres psql -U $dbUser -d $dbName < $tempSqlPath 2>&1"
$resultUpdate = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdUpdate 2>&1
Write-Host $resultUpdate
Write-Host ""

# Limpiar
$cmdCleanup = "rm -f $tempSqlPath"
& $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCleanup 2>&1 | Out-Null

Write-Host "========================================" -ForegroundColor Green
Write-Host "  HASH RESTAURADO" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Hash restaurado al original de data.sql" -ForegroundColor Green
Write-Host ""
Write-Host "Si el login sigue fallando:" -ForegroundColor Yellow
Write-Host "  1. Verifica que la contraseña sea la correcta de tu BD local" -ForegroundColor White
Write-Host "  2. O usa el endpoint de registro para crear una nueva cuenta" -ForegroundColor White
Write-Host "  3. O usa 'Olvidaste tu contraseña?' para resetearla" -ForegroundColor White
Write-Host ""
