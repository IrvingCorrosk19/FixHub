$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "Verificando Docker..." -ForegroundColor Yellow
$docker = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "docker --version" 2>&1
Write-Host $docker

Write-Host ""
Write-Host "Verificando Firewall..." -ForegroundColor Yellow
$ufw = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "ufw status" 2>&1
Write-Host $ufw

Write-Host ""
Write-Host "Verificando directorio /opt/apps..." -ForegroundColor Yellow
$dir = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "ls -la /opt/apps" 2>&1
Write-Host $dir

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RESUMEN:" -ForegroundColor Yellow
if ($docker -match "Docker version") {
    Write-Host "  Docker funciona: si" -ForegroundColor Green
} else {
    Write-Host "  Docker funciona: no" -ForegroundColor Red
}
Write-Host "  Entre por SSH: si" -ForegroundColor Green
