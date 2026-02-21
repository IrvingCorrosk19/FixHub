# Script para verificar usuario admin@panamatravelhub.com
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  VERIFICAR USUARIO admin@panamatravelhub.com" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$dbName = "panamatravelhub_db"
$dbUser = "panamatravelhub_user"

# Listar todos los usuarios y buscar el admin
Write-Host "Consultando usuarios en el servidor..." -ForegroundColor Yellow
Write-Host ""

$cmdAll = "docker exec panamatravelhub_postgres psql -U $dbUser -d $dbName -c 'SELECT u.email, u.first_name, u.last_name, u.is_active, r.name as role FROM users u LEFT JOIN user_roles ur ON u.id = ur.user_id LEFT JOIN roles r ON ur.role_id = r.id ORDER BY u.created_at;' 2>&1"
$resultAll = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdAll 2>&1
Write-Host $resultAll
Write-Host ""

# Verificar si existe el usuario admin@panamatravelhub.com
if ($resultAll -match "admin@panamatravelhub.com") {
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  USUARIO ENCONTRADO" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "El usuario admin@panamatravelhub.com EXISTE en el servidor" -ForegroundColor Green
    Write-Host ""
    
    # Extraer detalles del usuario
    $lines = $resultAll -split "`n"
    foreach ($line in $lines) {
        if ($line -match "admin@panamatravelhub.com") {
            Write-Host "Detalles del usuario:" -ForegroundColor Cyan
            Write-Host "  $line" -ForegroundColor White
            Write-Host ""
        }
    }
} else {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  USUARIO NO ENCONTRADO" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "El usuario admin@panamatravelhub.com NO EXISTE en el servidor" -ForegroundColor Yellow
    Write-Host "Ejecuta: .\importar-usuarios-simple.ps1 para importarlo" -ForegroundColor Yellow
    Write-Host ""
}
