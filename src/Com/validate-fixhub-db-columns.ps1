# Compara columnas (campos) por tabla: LOCAL vs VPS.
# Formato: tabla|campo|tipo
# Uso: $env:FIXHUB_VPS_PASSWORD='...'; .\validate-fixhub-db-columns.ps1

param([string]$Password)

$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"
$serverContainer = "fixhub_postgres"
$serverUser = "fixhubuser"
$serverDb = "FixHub"
$localHost = "localhost"
$localPort = "5432"
$localDb = "FixHub"
$localUser = "postgres"
$localPassword = "Panama2020$"

$pw = if ($Password) { $Password } else { $env:FIXHUB_VPS_PASSWORD }
if (-not $pw) {
    Write-Host "Indica FIXHUB_VPS_PASSWORD o -Password" -ForegroundColor Red
    exit 1
}

$pgDumpPath = Get-ChildItem "C:\Program Files\PostgreSQL" -Recurse -Filter "pg_dump.exe" -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
if (-not $pgDumpPath) {
    foreach ($v in @("18","17","16","15")) {
        $p = "C:\Program Files\PostgreSQL\$v\bin\pg_dump.exe"
        if (Test-Path $p) { $pgDumpPath = $p; break }
    }
}
$psqlPath = Join-Path (Split-Path $pgDumpPath) "psql.exe"
if (-not $psqlPath -or -not (Test-Path $psqlPath)) {
    Write-Host "ERROR: psql no encontrado" -ForegroundColor Red
    exit 1
}

# SQL: tabla, columna, tipo (ordenado)
$sqlColumns = "SELECT table_name, column_name, data_type FROM information_schema.columns WHERE table_schema = 'public' ORDER BY table_name, ordinal_position"

# --- LOCAL
$env:PGPASSWORD = $localPassword
$localRaw = & $psqlPath -h $localHost -p $localPort -U $localUser -d $localDb -t -A -c $sqlColumns 2>$null
Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
$localLines = $localRaw | Where-Object { $_ -match '\|' }

# --- VPS (vía base64 para evitar problemas de comillas)
$sqlB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($sqlColumns))
$vpsRaw = & $plink -ssh -pw $pw -batch -hostkey $hostkey $hostname "echo $sqlB64 | base64 -d | docker exec -i $serverContainer psql -U $serverUser -d $serverDb -t -A" 2>&1
$vpsLines = $vpsRaw | Where-Object { $_ -match '\|' -and $_ -notmatch 'psql:|warning' }

# --- Conjuntos "tabla|columna|tipo"
$setLocal = @{}
$localLines | ForEach-Object { $setLocal[$_] = $true }
$setVps = @{}
$vpsLines | ForEach-Object { $setVps[$_] = $true }

$soloLocal = $localLines | Where-Object { -not $setVps.ContainsKey($_) }
$soloVps = $vpsLines | Where-Object { -not $setLocal.ContainsKey($_) }

# --- Agrupar por tabla para reporte
function Group-ByTable($lines) {
    $byTable = @{}
    foreach ($line in $lines) {
        $t = ($line -split '\|')[0]
        if (-not $byTable[$t]) { $byTable[$t] = @() }
        $byTable[$t] += $line
    }
    $byTable
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CAMPOS POR TABLA: LOCAL vs VPS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  LOCAL: $($localLines.Count) columnas en $($localLines | ForEach-Object { ($_ -split '\|')[0] } | Sort-Object -Unique | Measure-Object | Select-Object -ExpandProperty Count) tablas" -ForegroundColor Gray
Write-Host "  VPS:   $($vpsLines.Count) columnas en $($vpsLines | ForEach-Object { ($_ -split '\|')[0] } | Sort-Object -Unique | Measure-Object | Select-Object -ExpandProperty Count) tablas" -ForegroundColor Gray
Write-Host ""

if ($soloLocal.Count -eq 0 -and $soloVps.Count -eq 0) {
    Write-Host "  RESULTADO: Iguales. Todas las tablas tienen los mismos campos y tipos." -ForegroundColor Green
    Write-Host ""
    Write-Host "  Resumen por tabla (LOCAL = VPS):" -ForegroundColor White
    $byTable = Group-ByTable $localLines
    foreach ($t in ($byTable.Keys | Sort-Object)) {
        $n = $byTable[$t].Count
        Write-Host "    $t : $n columnas" -ForegroundColor Gray
    }
    exit 0
}

Write-Host "  RESULTADO: Hay diferencias." -ForegroundColor Red
Write-Host ""

if ($soloLocal.Count -gt 0) {
    Write-Host "  Solo en LOCAL (tabla|columna|tipo):" -ForegroundColor Yellow
    $soloLocal | ForEach-Object { Write-Host "    $_" }
    Write-Host ""
}
if ($soloVps.Count -gt 0) {
    Write-Host "  Solo en VPS (tabla|columna|tipo):" -ForegroundColor Yellow
    $soloVps | ForEach-Object { Write-Host "    $_" }
}
Write-Host ""
exit 1
