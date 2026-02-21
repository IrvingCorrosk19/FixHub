# Script para verificar usuarios en la base de datos
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  USUARIOS EN BASE DE DATOS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar usuarios en el servidor
Write-Host "Consultando usuarios en servidor..." -ForegroundColor Yellow
$dbName = "panamatravelhub_db"
$dbUser = "panamatravelhub_user"

$cmd = "docker exec panamatravelhub_postgres psql -U $dbUser -d $dbName -c 'SELECT u.id, u.email, u.first_name, u.last_name, u.is_active, r.name as role, u.created_at FROM users u LEFT JOIN user_roles ur ON u.id = ur.user_id LEFT JOIN roles r ON ur.role_id = r.id ORDER BY u.created_at;' 2>&1"

Write-Host "Ejecutando consulta..." -ForegroundColor Gray
$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd 2>&1
Write-Host $result
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  INFORMACION DE USUARIOS DEFAULT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Segun script 06_seed_data.sql:" -ForegroundColor White
Write-Host "  Email: admin@toursanama.com" -ForegroundColor Gray
Write-Host "  Password: Admin123! (hash generado en aplicacion)" -ForegroundColor Gray
Write-Host "  Rol: Admin" -ForegroundColor Gray
Write-Host ""
Write-Host "Segun docs/CONTRASENIAS_USUARIOS.md:" -ForegroundColor White
Write-Host "  1. admin@panamatravelhub.com / Admin123! (Admin)" -ForegroundColor Gray
Write-Host "  2. cliente@panamatravelhub.com / Cliente123! (Customer)" -ForegroundColor Gray
Write-Host "  3. test1@panamatravelhub.com / Test123! (Customer)" -ForegroundColor Gray
Write-Host "  4. test2@panamatravelhub.com / Prueba123! (Customer)" -ForegroundColor Gray
Write-Host ""
