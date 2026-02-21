# Script para desplegar la aplicación en Docker en el servidor VPS
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DESPLIEGUE EN DOCKER - FASE 4" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# PASO 1: Actualizar el repositorio
Write-Host "PASO 1: Actualizando repositorio..." -ForegroundColor Yellow
$cmd1 = "cd /opt/apps/aspnet && git pull"
Write-Host "Ejecutando: $cmd1" -ForegroundColor Gray
$result1 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd1 2>&1
Write-Host $result1
Write-Host ""

# PASO 2: Verificar que existen Dockerfile y docker-compose.yml
Write-Host "PASO 2: Verificando archivos Docker..." -ForegroundColor Yellow
$cmd2 = "cd /opt/apps/aspnet && ls -la Dockerfile docker-compose.yml"
Write-Host "Ejecutando: $cmd2" -ForegroundColor Gray
$result2 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd2 2>&1
Write-Host $result2
Write-Host ""

# PASO 3: Crear archivo .env si no existe
Write-Host "PASO 3: Creando archivo .env..." -ForegroundColor Yellow
$envContent = @"
POSTGRES_DB=carnetqrdb
POSTGRES_USER=carnetqruser
POSTGRES_PASSWORD=superpasswordsegura

ASPNETCORE_ENVIRONMENT=Production
"@

# Escapar el contenido para pasarlo por SSH
$envContentEscaped = $envContent -replace '"', '\"' -replace "`n", "\n"
$cmd3 = "cd /opt/apps/aspnet && cat > .env << 'EOF'
POSTGRES_DB=carnetqrdb
POSTGRES_USER=carnetqruser
POSTGRES_PASSWORD=superpasswordsegura

ASPNETCORE_ENVIRONMENT=Production
EOF
"
Write-Host "Ejecutando: Creando .env..." -ForegroundColor Gray
$result3 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd3 2>&1
Write-Host $result3
Write-Host ""

# Verificar que .env se creó
Write-Host "Verificando .env..." -ForegroundColor Yellow
$cmd3b = "cd /opt/apps/aspnet && cat .env"
$result3b = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd3b 2>&1
Write-Host $result3b
Write-Host ""

# PASO 4: Construir y levantar contenedores
Write-Host "PASO 4: Construyendo y levantando contenedores..." -ForegroundColor Yellow
Write-Host "Esto puede tardar varios minutos en la primera ejecución..." -ForegroundColor Gray
$cmd4 = "cd /opt/apps/aspnet && docker compose up -d --build"
Write-Host "Ejecutando: $cmd4" -ForegroundColor Gray
$result4 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd4 2>&1
Write-Host $result4
Write-Host ""

# PASO 5: Verificar contenedores
Write-Host "PASO 5: Verificando contenedores..." -ForegroundColor Yellow
$cmd5 = "docker ps"
Write-Host "Ejecutando: $cmd5" -ForegroundColor Gray
$result5 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd5 2>&1
Write-Host $result5
Write-Host ""

# PASO 6: Ver logs de la aplicación
Write-Host "PASO 6: Últimas 20 líneas de logs de la aplicación..." -ForegroundColor Yellow
$cmd6 = "docker logs --tail 20 carnetqr_web"
Write-Host "Ejecutando: $cmd6" -ForegroundColor Gray
$result6 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd6 2>&1
Write-Host $result6
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DESPLIEGUE COMPLETADO" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Aplicación disponible en: http://164.68.99.83" -ForegroundColor Green
Write-Host ""
Write-Host "Para ver logs en tiempo real:" -ForegroundColor Yellow
Write-Host "  docker logs -f carnetqr_web" -ForegroundColor Gray
Write-Host ""
Write-Host "Para detener contenedores:" -ForegroundColor Yellow
Write-Host "  docker compose down" -ForegroundColor Gray
Write-Host ""
