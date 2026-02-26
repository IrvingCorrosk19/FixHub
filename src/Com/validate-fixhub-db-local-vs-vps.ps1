# Compara la BD FixHub local con la del VPS (esquema + migraciones).
# Uso: $env:FIXHUB_VPS_PASSWORD='...'; .\validate-fixhub-db-local-vs-vps.ps1

param([string]$Password)

$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"
$serverContainer = "fixhub_postgres"
$serverUser = "fixhubuser"
$serverDb = "FixHub"

# Local (mismo que sync-fixhub-db.ps1)
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

# Buscar psql (mismo directorio que pg_dump)
$pgDumpPath = Get-ChildItem "C:\Program Files\PostgreSQL" -Recurse -Filter "pg_dump.exe" -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
if (-not $pgDumpPath) {
    foreach ($v in @("18","17","16","15")) {
        $p = "C:\Program Files\PostgreSQL\$v\bin\pg_dump.exe"
        if (Test-Path $p) { $pgDumpPath = $p; break }
    }
}
if (-not $pgDumpPath -or -not (Test-Path $pgDumpPath)) {
    Write-Host "ERROR: No se encontro PostgreSQL (pg_dump). Instala PostgreSQL o ajusta la ruta." -ForegroundColor Red
    exit 1
}
$psqlPath = Join-Path (Split-Path $pgDumpPath) "psql.exe"
if (-not (Test-Path $psqlPath)) {
    Write-Host "ERROR: No se encontro psql en $psqlPath" -ForegroundColor Red
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  VALIDACION: FixHub DB LOCAL vs VPS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# --- LOCAL: tablas y migraciones
$env:PGPASSWORD = $localPassword
$localTablesCmd = "& `"$psqlPath`" -h $localHost -p $localPort -U $localUser -d $localDb -t -A -c `"SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY 1`""
$localTables = Invoke-Expression $localTablesCmd 2>$null
$localMigrationsCmd = "& `"$psqlPath`" -h $localHost -p $localPort -U $localUser -d $localDb -t -A -c `"SELECT \`"MigrationId\`" FROM \`"__EFMigrationsHistory\`" ORDER BY 1`""
$localMigrations = @()
try {
    $localMigrations = (Invoke-Expression $localMigrationsCmd 2>$null) | Where-Object { $_ -match '\S' }
} catch { }
Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue

if (-not $localTables) {
    Write-Host "ADVERTENCIA: No se pudo conectar a la BD local (${localHost}:${localPort}/${localDb}). ¿PostgreSQL en ejecucion?" -ForegroundColor Yellow
    $localTables = @()
}
$localTables = $localTables | Where-Object { $_ -match '\S' }

# --- VPS: tablas y migraciones (vía docker exec); comillas escapadas para el shell remoto
$cmdTables = "docker exec $serverContainer psql -U $serverUser -d $serverDb -t -A -c \`"SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY 1\`""
$vpsTablesRaw = & $plink -ssh -pw $pw -batch -hostkey $hostkey $hostname $cmdTables 2>&1
$vpsTables = $vpsTablesRaw | Where-Object { $_ -match '^\w' -and $_ -notmatch 'psql:|warning' }

$cmdMigrations = "docker exec $serverContainer psql -U $serverUser -d $serverDb -t -A -c 'SELECT `"MigrationId`" FROM `"__EFMigrationsHistory`" ORDER BY 1'"
$vpsMigrationsRaw = & $plink -ssh -pw $pw -batch -hostkey $hostkey $hostname $cmdMigrations 2>&1
$vpsMigrations = $vpsMigrationsRaw | Where-Object { $_ -match '^\d{14}_' }

# --- Comparar tablas
$setLocal = @{}; $localTables | ForEach-Object { $setLocal[$_] = $true }
$setVps = @{}; $vpsTables | ForEach-Object { $setVps[$_] = $true }
$soloLocal = $localTables | Where-Object { -not $setVps.ContainsKey($_) }
$soloVps = $vpsTables | Where-Object { -not $setLocal.ContainsKey($_) }

# --- Comparar migraciones
$migLocalSet = @{}; $localMigrations | ForEach-Object { $migLocalSet[$_] = $true }
$migVpsSet = @{}; $vpsMigrations | ForEach-Object { $migVpsSet[$_] = $true }
$migSoloLocal = $localMigrations | Where-Object { -not $migVpsSet.ContainsKey($_) }
$migSoloVps = $vpsMigrations | Where-Object { -not $migLocalSet.ContainsKey($_) }

# --- Resultado
Write-Host "  LOCAL (localhost:$localPort/$localDb)" -ForegroundColor White
Write-Host "    Tablas: $($localTables.Count)  |  Migraciones: $($localMigrations.Count)" -ForegroundColor Gray
Write-Host "  VPS (fixhub_postgres/$serverDb)" -ForegroundColor White
Write-Host "    Tablas: $($vpsTables.Count)  |  Migraciones: $($vpsMigrations.Count)" -ForegroundColor Gray
Write-Host ""

$ok = $true
if ($soloLocal.Count -gt 0) {
    Write-Host "  Tablas solo en LOCAL: $($soloLocal -join ', ')" -ForegroundColor Yellow
    $ok = $false
}
if ($soloVps.Count -gt 0) {
    Write-Host "  Tablas solo en VPS: $($soloVps -join ', ')" -ForegroundColor Yellow
    $ok = $false
}
if ($migSoloLocal.Count -gt 0) {
    Write-Host "  Migraciones solo en LOCAL: $($migSoloLocal -join ', ')" -ForegroundColor Yellow
    $ok = $false
}
if ($migSoloVps.Count -gt 0) {
    Write-Host "  Migraciones solo en VPS: $($migSoloVps -join ', ')" -ForegroundColor Yellow
    $ok = $false
}

if ($localTables.Count -eq 0 -and $vpsTables.Count -gt 0) {
    Write-Host "  No se pudo leer LOCAL; solo se muestra estado del VPS." -ForegroundColor Gray
}

if ($ok -and $localTables.Count -gt 0 -and $vpsTables.Count -gt 0) {
    if ($soloLocal.Count -eq 0 -and $soloVps.Count -eq 0 -and $migSoloLocal.Count -eq 0 -and $migSoloVps.Count -eq 0) {
        Write-Host "  RESULTADO: Iguales (mismas tablas y mismas migraciones aplicadas)." -ForegroundColor Green
    } else {
        Write-Host "  RESULTADO: Diferencias detectadas (ver arriba)." -ForegroundColor Red
    }
} elseif (-not $ok) {
    Write-Host "  RESULTADO: Las bases NO son iguales." -ForegroundColor Red
} else {
    Write-Host "  RESULTADO: No se pudo comparar (revisa conexion local o VPS)." -ForegroundColor Yellow
}
Write-Host ""
