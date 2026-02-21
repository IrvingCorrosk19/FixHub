# Script para copiar docker-compose.yml local al servidor usando base64
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "Copiando docker-compose.yml al servidor..." -ForegroundColor Yellow

# Leer archivo local
$dockerComposePath = "C:\Proyectos\PanamaTravelHub\PanamaTravelHub\docker-compose.yml"
$content = Get-Content $dockerComposePath -Raw -Encoding UTF8

# Convertir a base64
$bytes = [System.Text.Encoding]::UTF8.GetBytes($content)
$base64 = [System.Convert]::ToBase64String($bytes)

# Escribir al servidor usando base64 decode
$cmd = "cd /opt/apps/panamatravelhub && echo '$base64' | base64 -d > docker-compose.yml"
Write-Host "Ejecutando comando..." -ForegroundColor Gray
$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd 2>&1
Write-Host $result
Write-Host ""

# Verificar sintaxis
Write-Host "Verificando sintaxis YAML..." -ForegroundColor Yellow
$cmdVerify = "cd /opt/apps/panamatravelhub && docker compose config > /dev/null 2>&1 && echo 'OK' || (echo 'ERROR' && docker compose config 2>&1 | head -20)"
$resultVerify = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdVerify 2>&1
Write-Host $resultVerify
Write-Host ""

if ($resultVerify -match 'OK')
{
    Write-Host "docker-compose.yml copiado y verificado correctamente!" -ForegroundColor Green
}
Write-Host ""
