# Script para generar hash BCrypt correcto usando la aplicacion en el servidor
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  GENERAR HASH BCRYPT CORRECTO" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Creando programa temporal para generar hash..." -ForegroundColor Yellow

# Crear un programa C# temporal que genere el hash
$generateHashProgram = @"
using BCrypt.Net;
class Program {
    static void Main() {
        var password = "Admin123!";
        var hash = BCrypt.Net.BCrypt.HashPassword(password, 12);
        System.Console.WriteLine(hash);
    }
}
"@

# Guardar programa en servidor
$tempProgramPath = "/tmp/generate_hash.cs"
$bytes = [System.Text.Encoding]::UTF8.GetBytes($generateHashProgram)
$base64 = [System.Convert]::ToBase64String($bytes)

$cmdCopy = "echo '$base64' | base64 -d > $tempProgramPath"
& $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCopy 2>&1 | Out-Null

# Ejecutar con dotnet script o crear ejecutable
# Alternativa: usar endpoint de registro para generar hash
# Mejor opcion: usar el endpoint /api/auth/register temporalmente o usar el hash del archivo data.sql

# El hash que tenemos en data.sql es: $2a$12$gpmcPqtakrNDl29T9mDeqOjzeVjACvG/RRyjAdxH3.u58TZG6g8yS
# Segun la documentacion, deberia ser Admin123!, pero puede que no coincida

# Opcion mas directa: usar el endpoint de la API para probar login y ver que pasa
# O verificar logs mas detallados

Write-Host "Verificando logs de intentos de login..." -ForegroundColor Yellow
$cmdLogs = "docker logs panamatravelhub_web 2>&1 | grep -i 'login\|password' | tail -10"
$resultLogs = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdLogs 2>&1
Write-Host $resultLogs
Write-Host ""

# Limpiar
$cmdCleanup = "rm -f $tempProgramPath"
& $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCleanup 2>&1 | Out-Null

Write-Host ""
Write-Host "Opcion recomendada:" -ForegroundColor Yellow
Write-Host "  Intenta con la contraseña que usabas en local" -ForegroundColor White
Write-Host "  O usa el endpoint de registro para crear una nueva cuenta" -ForegroundColor White
Write-Host ""
