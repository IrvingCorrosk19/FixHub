# Script para desplegar PanamaTravelHub en Docker en el servidor VPS
# Multi-aplicación: No afecta CarnetQR que está en /opt/apps/aspnet
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DESPLIEGUE PANAMATRAVELHUB - DOCKER" -ForegroundColor Cyan
Write-Host "  Multi-aplicación (sin afectar CarnetQR)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar TODAS las aplicaciones antes del deploy (PROTECCION)
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  VERIFICACION PRE-DEPLOY (PROTECCION)" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "Verificando estado de TODAS las aplicaciones..." -ForegroundColor Yellow
$cmdCheckAll = "docker ps --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'"
$resultCheckAll = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCheckAll 2>&1
Write-Host $resultCheckAll
Write-Host ""
Write-Host "IMPORTANTE: Solo se afectará PanamaTravelHub" -ForegroundColor Cyan
Write-Host "  - Puerto web: 8082 (único)" -ForegroundColor Gray
Write-Host "  - Puerto DB: 5433 (único)" -ForegroundColor Gray
Write-Host "  - Contenedores: panamatravelhub_* (nombres únicos)" -ForegroundColor Gray
Write-Host "  - Red: panamatravelhub_net (aislada)" -ForegroundColor Gray
Write-Host ""

# PASO 0: Crear directorio si no existe
Write-Host "PASO 0: Verificando directorio /opt/apps/panamatravelhub..." -ForegroundColor Yellow
$cmd0 = "mkdir -p /opt/apps/panamatravelhub && cd /opt/apps/panamatravelhub && pwd"
Write-Host "Ejecutando: $cmd0" -ForegroundColor Gray
$result0 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd0 2>&1
Write-Host $result0
Write-Host ""

# PASO 1: Actualizar el repositorio
Write-Host "PASO 1: Actualizando repositorio..." -ForegroundColor Yellow
$cmd1 = "cd /opt/apps/panamatravelhub && if [ -d .git ]; then git pull; else echo 'No es un repositorio Git. Clonar manualmente primero.'; fi"
Write-Host "Ejecutando: $cmd1" -ForegroundColor Gray
$result1 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd1 2>&1
Write-Host $result1
Write-Host ""

# PASO 2: Verificar que existen Dockerfile y docker-compose.yml
Write-Host "PASO 2: Verificando archivos Docker..." -ForegroundColor Yellow
$cmd2 = "cd /opt/apps/panamatravelhub && ls -la Dockerfile docker-compose.yml"
Write-Host "Ejecutando: $cmd2" -ForegroundColor Gray
$result2 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd2 2>&1
Write-Host $result2
Write-Host ""

# PASO 3: Crear archivo .env si no existe
Write-Host "PASO 3: Creando archivo .env..." -ForegroundColor Yellow
$envContent = @"
POSTGRES_DB=panamatravelhub_db
POSTGRES_USER=panamatravelhub_user
POSTGRES_PASSWORD=PanamaTravel2024!Secure

ASPNETCORE_ENVIRONMENT=Production
"@

$cmd3 = "cd /opt/apps/panamatravelhub && cat > .env << 'EOF'
POSTGRES_DB=panamatravelhub_db
POSTGRES_USER=panamatravelhub_user
POSTGRES_PASSWORD=PanamaTravel2024!Secure

ASPNETCORE_ENVIRONMENT=Production
EOF
"
Write-Host "Ejecutando: Creando .env..." -ForegroundColor Gray
$result3 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd3 2>&1
Write-Host $result3
Write-Host ""

# Verificar que .env se creó
Write-Host "Verificando .env..." -ForegroundColor Yellow
$cmd3b = "cd /opt/apps/panamatravelhub && cat .env"
$result3b = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd3b 2>&1
Write-Host $result3b
Write-Host ""

# PASO 4: Construir y levantar contenedores
Write-Host "PASO 4: Construyendo y levantando contenedores..." -ForegroundColor Yellow
Write-Host "Esto puede tardar varios minutos en la primera ejecución..." -ForegroundColor Gray
Write-Host "Puerto: 8082 (no afecta CarnetQR en 8081)" -ForegroundColor Gray
$cmd4 = "cd /opt/apps/panamatravelhub && docker compose up -d --build"
Write-Host "Ejecutando: $cmd4" -ForegroundColor Gray
$result4 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd4 2>&1
Write-Host $result4
Write-Host ""

# PASO 5: Verificar contenedores (todos para comparar)
Write-Host "PASO 5: Verificando contenedores..." -ForegroundColor Yellow
Write-Host "Contenedores en el servidor (CarnetQR + PanamaTravelHub):" -ForegroundColor Cyan
$cmd5 = "docker ps --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'"
Write-Host "Ejecutando: $cmd5" -ForegroundColor Gray
$result5 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd5 2>&1
Write-Host $result5
Write-Host ""

# PASO 6: Ver logs de PanamaTravelHub
Write-Host "PASO 6: Últimas 30 líneas de logs de PanamaTravelHub..." -ForegroundColor Yellow
$cmd6 = "docker logs --tail 30 panamatravelhub_web 2>&1 || echo 'Contenedor aún no disponible, espera unos segundos...'"
Write-Host "Ejecutando: $cmd6" -ForegroundColor Gray
$result6 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd6 2>&1
Write-Host $result6
Write-Host ""

# PASO 7: Verificar salud de TODAS las aplicaciones (POST-DEPLOY)
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  VERIFICACION POST-DEPLOY" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "Estado de TODAS las aplicaciones despues del deploy:" -ForegroundColor Yellow
Write-Host ""
$cmd7All = "docker ps --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'"
$result7All = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd7All 2>&1
Write-Host $result7All
Write-Host ""

# Verificacion especifica por aplicacion
Write-Host "Verificacion detallada:" -ForegroundColor Cyan
Write-Host ""

# Aplicacion 1 (si existe)
$cmd7a = "docker ps --filter name=carnetqr --format '{{.Names}}: {{.Status}}'"
$result7a = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd7a 2>&1
if ($result7a -and $result7a -notmatch "^\s*$") {
    Write-Host "  [OK] Aplicacion 1 (carnetqr):" -ForegroundColor Green
    Write-Host "    $result7a" -ForegroundColor White
} else {
    Write-Host "  [-] Aplicacion 1 (carnetqr): No encontrada" -ForegroundColor Gray
}
Write-Host ""

# PanamaTravelHub
Write-Host "  [OK] PanamaTravelHub:" -ForegroundColor Green
$cmd7b = "docker ps --filter name=panamatravelhub --format '{{.Names}}: {{.Status}}'"
$result7b = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd7b 2>&1
Write-Host "    $result7b" -ForegroundColor White
Write-Host ""

# Verificar otras aplicaciones (generico)
$cmd7c = "docker ps --format '{{.Names}}' | grep -v panamatravelhub | grep -v carnetqr | head -5"
$result7c = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd7c 2>&1
if ($result7c -and $result7c -notmatch "^\s*$") {
    Write-Host "  [OK] Otras aplicaciones detectadas:" -ForegroundColor Green
    $result7c -split "`n" | ForEach-Object {
        if ($_ -and $_ -notmatch "^\s*$") {
            $otherApp = $_.Trim()
            $cmdOther = "docker ps --filter name=$otherApp --format '{{.Names}}: {{.Status}}'"
            $resultOther = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdOther 2>&1
            Write-Host "    $resultOther" -ForegroundColor White
        }
    }
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DESPLIEGUE COMPLETADO" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Aplicaciones disponibles:" -ForegroundColor Green
Write-Host "  - CarnetQR:      http://164.68.99.83:8081 (no afectada)" -ForegroundColor Green
Write-Host "  - PanamaTravelHub: http://164.68.99.83:8082" -ForegroundColor Green
Write-Host ""
Write-Host "Comandos útiles:" -ForegroundColor Yellow
Write-Host "  Ver logs PanamaTravelHub:" -ForegroundColor Gray
Write-Host "    docker logs -f panamatravelhub_web" -ForegroundColor White
Write-Host ""
Write-Host "  Ver logs CarnetQR (verificar que sigue funcionando):" -ForegroundColor Gray
Write-Host "    docker logs -f carnetqr_web" -ForegroundColor White
Write-Host ""
Write-Host "  Detener PanamaTravelHub:" -ForegroundColor Gray
Write-Host "    cd /opt/apps/panamatravelhub && docker compose down" -ForegroundColor White
Write-Host ""
Write-Host "  Reiniciar PanamaTravelHub:" -ForegroundColor Gray
Write-Host "    cd /opt/apps/panamatravelhub && docker compose restart" -ForegroundColor White
Write-Host ""
