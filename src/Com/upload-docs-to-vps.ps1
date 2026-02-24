# Sube solo la carpeta docs/ al VPS en /opt/apps/fixhub/docs
# NO modifica .env, docker, ni ningun otro archivo. Solo actualiza documentacion.
# Uso: $env:FIXHUB_VPS_PASSWORD = "tu_password"; .\upload-docs-to-vps.ps1
#      o: .\upload-docs-to-vps.ps1 -Password "tu_password"

param(
    [string]$Password
)

$plink = "C:\Program Files\PuTTY\plink.exe"
$pscp  = "C:\Program Files\PuTTY\pscp.exe"
$hostname = "root@164.68.99.83"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"
$remoteDir = "/opt/apps/fixhub"
$docsRemote = "$remoteDir/docs"

$pw = if ($Password) { $Password } else { $env:FIXHUB_VPS_PASSWORD }
if (-not $pw) {
    Write-Host "ERROR: Indica la contraseña SSH." -ForegroundColor Red
    Write-Host "  Opcion 1: `$env:FIXHUB_VPS_PASSWORD = 'tu_password'; .\upload-docs-to-vps.ps1" -ForegroundColor Yellow
    Write-Host "  Opcion 2: .\upload-docs-to-vps.ps1 -Password 'tu_password'" -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $pscp)) {
    Write-Host "ERROR: No se encontro pscp.exe (PuTTY). Instala PuTTY o ajusta la ruta." -ForegroundColor Red
    exit 1
}

# Raiz del repo (desde src/Com subimos dos niveles)
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$docsLocal = Join-Path $repoRoot "docs"
if (-not (Test-Path $docsLocal)) {
    Write-Host "ERROR: No se encontro la carpeta docs en $docsLocal" -ForegroundColor Red
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SUBIR DOCS FIXHUB -> VPS" -ForegroundColor Cyan
Write-Host "  Solo docs/ -> ${hostname}:${docsRemote}" -ForegroundColor Gray
Write-Host "  No se modifica .env ni contenedores." -ForegroundColor Gray
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Subir carpeta docs completa (incluye AUDIT/, etc.) a /opt/apps/fixhub/docs
& $pscp -r -pw $pw -hostkey $hostkey $docsLocal "${hostname}:${remoteDir}/"
if ($LASTEXITCODE -ne 0) {
    Write-Host "Fallo la subida. Comprueba contraseña y conectividad (ping/ssh)." -ForegroundColor Red
    exit 1
}

Write-Host "Docs subidos a ${hostname}:${docsRemote}" -ForegroundColor Green
Write-Host "Verificar: ssh ${hostname} 'ls -la $docsRemote'" -ForegroundColor Gray
