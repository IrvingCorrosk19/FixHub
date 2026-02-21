$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "Habilitando firewall..." -ForegroundColor Yellow
$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "echo y | ufw enable && ufw status" 2>&1
Write-Host $result
