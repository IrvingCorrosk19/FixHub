# Script completo para desplegar la aplicación CarnetQR Platform en VPS
# Incluye: verificación de conflictos, backup de DB, migraciones y deployment
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DESPLIEGUE COMPLETO - CARNETQR PLATFORM" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# PASO 1: Verificar conflictos con otras aplicaciones
Write-Host "PASO 1: Verificando conflictos con otras aplicaciones..." -ForegroundColor Yellow
$cmd1 = "docker ps --format 'table {{.Names}}\t{{.Ports}}' | grep -E '(80|8001|5432)' || echo 'No hay conflictos detectados'"
Write-Host "Ejecutando: $cmd1" -ForegroundColor Gray
$result1 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd1 2>&1
Write-Host $result1
Write-Host ""

# Verificar si los contenedores de CarnetQR ya existen
Write-Host "Verificando contenedores existentes de CarnetQR..." -ForegroundColor Yellow
$cmd1b = "docker ps -a --filter 'name=carnetqr' --format '{{.Names}}'"
$result1b = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd1b 2>&1
if ($result1b -match "carnetqr") {
    Write-Host "Contenedores de CarnetQR encontrados: $result1b" -ForegroundColor Yellow
} else {
    Write-Host "No hay contenedores de CarnetQR existentes" -ForegroundColor Green
}
Write-Host ""

# PASO 2: Actualizar repositorio
Write-Host "PASO 2: Actualizando repositorio..." -ForegroundColor Yellow
$cmd2 = "cd /opt/apps/aspnet && git pull origin main"
Write-Host "Ejecutando: $cmd2" -ForegroundColor Gray
$result2 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd2 2>&1
Write-Host $result2
Write-Host ""

# PASO 3: Verificar archivos Docker
Write-Host "PASO 3: Verificando archivos Docker..." -ForegroundColor Yellow
$cmd3 = "cd /opt/apps/aspnet && ls -la Dockerfile docker-compose.yml .env 2>&1"
Write-Host "Ejecutando: $cmd3" -ForegroundColor Gray
$result3 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd3 2>&1
Write-Host $result3
Write-Host ""

# PASO 4: Crear/Actualizar archivo .env
Write-Host "PASO 4: Creando/Actualizando archivo .env..." -ForegroundColor Yellow
$envContent = @"
POSTGRES_DB=carnetqrdb
POSTGRES_USER=carnetqruser
POSTGRES_PASSWORD=superpasswordsegura

ASPNETCORE_ENVIRONMENT=Production
"@

$cmd4 = "cd /opt/apps/aspnet && cat > .env << 'EOF'
POSTGRES_DB=carnetqrdb
POSTGRES_USER=carnetqruser
POSTGRES_PASSWORD=superpasswordsegura

ASPNETCORE_ENVIRONMENT=Production
EOF
"
Write-Host "Ejecutando: Creando .env..." -ForegroundColor Gray
$result4 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd4 2>&1
Write-Host $result4
Write-Host ""

# PASO 5: Backup de base de datos si existe
Write-Host "PASO 5: Haciendo backup de base de datos (si existe)..." -ForegroundColor Yellow
$cmd5 = @"
if docker ps | grep -q carnetqr_postgres; then
    echo 'Contenedor PostgreSQL encontrado, haciendo backup...'
    BACKUP_DIR='/opt/apps/aspnet/backups'
    mkdir -p `$BACKUP_DIR
    BACKUP_FILE=`$BACKUP_DIR/carnetqrdb_backup_`$(date +%Y%m%d_%H%M%S).sql
    docker exec carnetqr_postgres pg_dump -U carnetqruser carnetqrdb > `$BACKUP_FILE
    echo "Backup creado: `$BACKUP_FILE"
    ls -lh `$BACKUP_FILE
else
    echo 'No hay contenedor PostgreSQL existente, no se requiere backup'
fi
"@
Write-Host "Ejecutando: Backup de DB..." -ForegroundColor Gray
$result5 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd5 2>&1
Write-Host $result5
Write-Host ""

# PASO 6: Detener contenedores existentes (si existen)
Write-Host "PASO 6: Deteniendo contenedores existentes (si existen)..." -ForegroundColor Yellow
$cmd6 = "cd /opt/apps/aspnet && docker compose down 2>&1 || echo 'No hay contenedores para detener'"
Write-Host "Ejecutando: $cmd6" -ForegroundColor Gray
$result6 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd6 2>&1
Write-Host $result6
Write-Host ""

# PASO 7: Construir y levantar contenedores
Write-Host "PASO 7: Construyendo y levantando contenedores..." -ForegroundColor Yellow
Write-Host "Esto puede tardar varios minutos..." -ForegroundColor Gray
$cmd7 = "cd /opt/apps/aspnet && docker compose up -d --build"
Write-Host "Ejecutando: $cmd7" -ForegroundColor Gray
$result7 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd7 2>&1
Write-Host $result7
Write-Host ""

# PASO 8: Esperar a que PostgreSQL esté listo
Write-Host "PASO 8: Esperando a que PostgreSQL esté listo..." -ForegroundColor Yellow
$cmd8 = @"
for i in {1..30}; do
    if docker exec carnetqr_postgres pg_isready -U carnetqruser -d carnetqrdb > /dev/null 2>&1; then
        echo 'PostgreSQL está listo'
        exit 0
    fi
    echo "Esperando PostgreSQL... (`$i/30)"
    sleep 2
done
echo 'Timeout esperando PostgreSQL'
exit 1
"@
Write-Host "Ejecutando: Esperando PostgreSQL..." -ForegroundColor Gray
$result8 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd8 2>&1
Write-Host $result8
Write-Host ""

# PASO 9: Verificar contenedores
Write-Host "PASO 9: Verificando contenedores..." -ForegroundColor Yellow
$cmd9 = "docker ps --filter 'name=carnetqr' --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'"
Write-Host "Ejecutando: $cmd9" -ForegroundColor Gray
$result9 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd9 2>&1
Write-Host $result9
Write-Host ""

# PASO 10: Ver logs de la aplicación (últimas 50 líneas)
Write-Host "PASO 10: Últimas 50 líneas de logs de la aplicación..." -ForegroundColor Yellow
$cmd10 = "docker logs --tail 50 carnetqr_web 2>&1"
Write-Host "Ejecutando: $cmd10" -ForegroundColor Gray
$result10 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd10 2>&1
Write-Host $result10
Write-Host ""

# PASO 11: Verificar migraciones aplicadas
Write-Host "PASO 11: Verificando migraciones en la base de datos..." -ForegroundColor Yellow
$cmd11 = @"
docker exec carnetqr_postgres psql -U carnetqruser -d carnetqrdb -c "SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 10;" 2>&1
"@
Write-Host "Ejecutando: Verificando migraciones..." -ForegroundColor Gray
$result11 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd11 2>&1
Write-Host $result11
Write-Host ""

# PASO 12: Verificar tablas creadas
Write-Host "PASO 12: Verificando tablas en la base de datos..." -ForegroundColor Yellow
$cmd12 = "docker exec carnetqr_postgres psql -U carnetqruser -d carnetqrdb -c '\dt' 2>&1"
Write-Host "Ejecutando: $cmd12" -ForegroundColor Gray
$result12 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd12 2>&1
Write-Host $result12
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DESPLIEGUE COMPLETADO" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Aplicación disponible en: http://164.68.99.83:8001" -ForegroundColor Green
Write-Host ""
Write-Host "Información importante:" -ForegroundColor Yellow
Write-Host "  - Puerto de la aplicación: 8001 (para evitar conflictos)" -ForegroundColor Gray
Write-Host "  - PostgreSQL NO está expuesto externamente (solo red interna)" -ForegroundColor Gray
Write-Host "  - Contenedores: carnetqr_postgres, carnetqr_web" -ForegroundColor Gray
Write-Host "  - Red: carnetqr_net (aislada)" -ForegroundColor Gray
Write-Host "  - Volúmenes: carnetqr_postgres_data, carnetqr_dataprotection_keys" -ForegroundColor Gray
Write-Host ""
Write-Host "Comandos útiles:" -ForegroundColor Yellow
Write-Host "  Ver logs en tiempo real: docker logs -f carnetqr_web" -ForegroundColor Gray
Write-Host "  Ver logs de PostgreSQL: docker logs -f carnetqr_postgres" -ForegroundColor Gray
Write-Host "  Detener contenedores: cd /opt/apps/aspnet && docker compose down" -ForegroundColor Gray
Write-Host "  Reiniciar contenedores: cd /opt/apps/aspnet && docker compose restart" -ForegroundColor Gray
Write-Host ""
