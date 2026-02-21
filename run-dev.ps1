# Ejecuta API (puerto 5100) y Web (puerto 5200/7200) para desarrollo local.
# Requiere: PostgreSQL en localhost:5432, migraciones aplicadas.
#
# Uso: .\run-dev.ps1
# Abre dos ventanas: una para la API y otra para la Web.

$apiPath = Join-Path $PSScriptRoot "src\FixHub.API\FixHub.API.csproj"
$webPath = Join-Path $PSScriptRoot "src\FixHub.Web\FixHub.Web.csproj"

Write-Host "Iniciando FixHub.API (http://localhost:5100)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot'; dotnet run --project '$apiPath'"

Start-Sleep -Seconds 4

Write-Host "Iniciando FixHub.Web (http://localhost:5200)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot'; dotnet run --project '$webPath' --launch-profile http"

Write-Host "`nAPI: http://localhost:5100/swagger" -ForegroundColor Green
Write-Host "Web: http://localhost:5200" -ForegroundColor Green
Write-Host "`nCierra las ventanas de PowerShell para detener los servidores." -ForegroundColor Yellow
