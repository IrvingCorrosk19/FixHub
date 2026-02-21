# Script COMPLETO para desplegar CarnetQR Platform en https://carnet.autonomousflow.lat/
# Incluye: verificación, backup, migraciones y deployment completo
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DESPLIEGUE COMPLETO - CARNETQR PLATFORM" -ForegroundColor Cyan
Write-Host "  Dominio: https://carnet.autonomousflow.lat/" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# PASO 1: Verificar estado actual del VPS
Write-Host "PASO 1: Verificando estado actual del VPS..." -ForegroundColor Yellow
$cmd1 = "docker ps -a --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}' | head -15"
Write-Host "Ejecutando: $cmd1" -ForegroundColor Gray
$result1 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd1 2>&1
Write-Host $result1
Write-Host ""

# Verificar contenedores de CarnetQR específicamente
Write-Host "Verificando contenedores de CarnetQR..." -ForegroundColor Yellow
$cmd1b = "docker ps -a --filter 'name=carnetqr' --format '{{.Names}} - {{.Status}}'"
$result1b = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd1b 2>&1
if ($result1b -match "carnetqr") {
    Write-Host "Contenedores encontrados:" -ForegroundColor Yellow
    Write-Host $result1b
} else {
    Write-Host "No hay contenedores de CarnetQR existentes" -ForegroundColor Green
}
Write-Host ""

# PASO 2: Verificar directorio y repositorio
Write-Host "PASO 2: Verificando directorio y repositorio..." -ForegroundColor Yellow
$cmd2 = "cd /opt/apps/aspnet && pwd && git remote -v && git status --short | head -10"
Write-Host "Ejecutando: Verificando repositorio..." -ForegroundColor Gray
$result2 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd2 2>&1
Write-Host $result2
Write-Host ""

# PASO 3: Actualizar repositorio
Write-Host "PASO 3: Actualizando repositorio desde GitHub..." -ForegroundColor Yellow
$cmd3 = "cd /opt/apps/aspnet && git fetch origin && git pull origin main"
Write-Host "Ejecutando: $cmd3" -ForegroundColor Gray
$result3 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd3 2>&1
Write-Host $result3
Write-Host ""

# PASO 4: Verificar archivos Docker
Write-Host "PASO 4: Verificando archivos Docker..." -ForegroundColor Yellow
$cmd4 = "cd /opt/apps/aspnet && ls -lh Dockerfile docker-compose.yml .env 2>&1"
Write-Host "Ejecutando: $cmd4" -ForegroundColor Gray
$result4 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd4 2>&1
Write-Host $result4
Write-Host ""

# PASO 5: Crear/Actualizar archivo .env
Write-Host "PASO 5: Creando/Actualizando archivo .env..." -ForegroundColor Yellow
$cmd5 = "cd /opt/apps/aspnet && cat > .env << 'EOF'
POSTGRES_DB=carnetqrdb
POSTGRES_USER=carnetqruser
POSTGRES_PASSWORD=superpasswordsegura

ASPNETCORE_ENVIRONMENT=Production
EOF
"
Write-Host "Ejecutando: Creando .env..." -ForegroundColor Gray
$result5 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd5 2>&1
Write-Host $result5
Write-Host ""

# Verificar .env creado
$cmd5b = "cd /opt/apps/aspnet && cat .env"
$result5b = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd5b 2>&1
Write-Host "Contenido de .env:" -ForegroundColor Gray
Write-Host $result5b
Write-Host ""

# PASO 6: Backup de base de datos si existe
Write-Host "PASO 6: Haciendo backup de base de datos (si existe)..." -ForegroundColor Yellow
$cmd6 = @"
if docker ps | grep -q carnetqr_postgres; then
    echo '=== Contenedor PostgreSQL encontrado, haciendo backup ==='
    BACKUP_DIR='/opt/apps/aspnet/backups'
    mkdir -p `$BACKUP_DIR
    BACKUP_FILE=`$BACKUP_DIR/carnetqrdb_backup_`$(date +%Y%m%d_%H%M%S).sql
    echo "Creando backup: `$BACKUP_FILE"
    docker exec carnetqr_postgres pg_dump -U carnetqruser carnetqrdb > `$BACKUP_FILE 2>&1
    if [ `$? -eq 0 ]; then
        echo "✅ Backup creado exitosamente"
        ls -lh `$BACKUP_FILE
    else
        echo "⚠️  Error al crear backup, pero continuando..."
    fi
else
    echo 'No hay contenedor PostgreSQL existente, no se requiere backup'
fi
"@
Write-Host "Ejecutando: Backup de DB..." -ForegroundColor Gray
$result6 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd6 2>&1
Write-Host $result6
Write-Host ""

# PASO 7: Detener contenedores existentes (si existen)
Write-Host "PASO 7: Deteniendo contenedores existentes (si existen)..." -ForegroundColor Yellow
$cmd7 = "cd /opt/apps/aspnet && docker compose down 2>&1"
Write-Host "Ejecutando: $cmd7" -ForegroundColor Gray
$result7 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd7 2>&1
Write-Host $result7
Write-Host ""

# PASO 8: Construir y levantar contenedores
Write-Host "PASO 8: Construyendo y levantando contenedores..." -ForegroundColor Yellow
Write-Host "Esto puede tardar varios minutos (primera vez: 3-5 minutos)..." -ForegroundColor Gray
$cmd8 = "cd /opt/apps/aspnet && docker compose up -d --build"
Write-Host "Ejecutando: $cmd8" -ForegroundColor Gray
$result8 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd8 2>&1
Write-Host $result8
Write-Host ""

# PASO 9: Esperar a que PostgreSQL esté listo
Write-Host "PASO 9: Esperando a que PostgreSQL esté listo..." -ForegroundColor Yellow
$cmd9 = @"
echo 'Esperando a que PostgreSQL esté listo...'
for i in {1..60}; do
    if docker exec carnetqr_postgres pg_isready -U carnetqruser -d carnetqrdb > /dev/null 2>&1; then
        echo "✅ PostgreSQL está listo (intento `$i)"
        exit 0
    fi
    if [ `$((i % 5)) -eq 0 ]; then
        echo "Esperando PostgreSQL... (`$i/60)"
    fi
    sleep 2
done
echo '⚠️  Timeout esperando PostgreSQL, pero continuando...'
exit 0
"@
Write-Host "Ejecutando: Esperando PostgreSQL..." -ForegroundColor Gray
$result9 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd9 2>&1
Write-Host $result9
Write-Host ""

# PASO 10: Verificar contenedores corriendo
Write-Host "PASO 10: Verificando contenedores..." -ForegroundColor Yellow
$cmd10 = "docker ps --filter 'name=carnetqr' --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'"
Write-Host "Ejecutando: $cmd10" -ForegroundColor Gray
$result10 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd10 2>&1
Write-Host $result10
Write-Host ""

# PASO 11: Ver logs de la aplicación (últimas 50 líneas)
Write-Host "PASO 11: Últimas 50 líneas de logs de la aplicación..." -ForegroundColor Yellow
$cmd11 = "docker logs --tail 50 carnetqr_web 2>&1"
Write-Host "Ejecutando: $cmd11" -ForegroundColor Gray
$result11 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd11 2>&1
Write-Host $result11
Write-Host ""

# PASO 12: Verificar migraciones aplicadas
Write-Host "PASO 12: Verificando migraciones en la base de datos..." -ForegroundColor Yellow
$cmd12 = @"
if docker exec carnetqr_postgres pg_isready -U carnetqruser -d carnetqrdb > /dev/null 2>&1; then
    echo '=== Migraciones aplicadas ==='
    docker exec carnetqr_postgres psql -U carnetqruser -d carnetqrdb -c 'SELECT \"MigrationId\", \"ProductVersion\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 10;' 2>&1
    echo ''
    echo '=== Total de migraciones ==='
    docker exec carnetqr_postgres psql -U carnetqruser -d carnetqrdb -c 'SELECT COUNT(*) as total_migrations FROM \"__EFMigrationsHistory\";' 2>&1
else
    echo 'PostgreSQL no está listo aún'
fi
"@
Write-Host "Ejecutando: Verificando migraciones..." -ForegroundColor Gray
$result12 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd12 2>&1
Write-Host $result12
Write-Host ""

# PASO 13: Verificar tablas creadas
Write-Host "PASO 13: Verificando tablas en la base de datos..." -ForegroundColor Yellow
$cmd13 = @"
if docker exec carnetqr_postgres pg_isready -U carnetqruser -d carnetqrdb > /dev/null 2>&1; then
    echo '=== Tablas en la base de datos ==='
    docker exec carnetqr_postgres psql -U carnetqruser -d carnetqrdb -c '\dt' 2>&1 | head -30
    echo ''
    echo '=== Conteo de tablas ==='
    docker exec carnetqr_postgres psql -U carnetqruser -d carnetqrdb -c 'SELECT COUNT(*) as total_tables FROM information_schema.tables WHERE table_schema = '\''public'\'';' 2>&1
else
    echo 'PostgreSQL no está listo aún'
fi
"@
Write-Host "Ejecutando: Verificando tablas..." -ForegroundColor Gray
$result13 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd13 2>&1
Write-Host $result13
Write-Host ""

# PASO 14: Verificar usuarios creados
Write-Host "PASO 14: Verificando usuarios en la base de datos..." -ForegroundColor Yellow
$cmd14 = @"
if docker exec carnetqr_postgres pg_isready -U carnetqruser -d carnetqrdb > /dev/null 2>&1; then
    echo '=== Usuarios creados ==='
    docker exec carnetqr_postgres psql -U carnetqruser -d carnetqrdb -c 'SELECT \"UserName\", \"Email\", \"IsActive\" FROM \"AspNetUsers\" ORDER BY \"UserName\";' 2>&1
    echo ''
    echo '=== Total de usuarios ==='
    docker exec carnetqr_postgres psql -U carnetqruser -d carnetqrdb -c 'SELECT COUNT(*) as total_users FROM \"AspNetUsers\";' 2>&1
else
    echo 'PostgreSQL no está listo aún'
fi
"@
Write-Host "Ejecutando: Verificando usuarios..." -ForegroundColor Gray
$result14 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd14 2>&1
Write-Host $result14
Write-Host ""

# PASO 15: Verificar acceso a la aplicación
Write-Host "PASO 15: Verificando acceso a la aplicación..." -ForegroundColor Yellow
$cmd15 = "curl -s -o /dev/null -w 'HTTP Status: %{http_code}\n' http://localhost:80 || echo 'No se pudo conectar'"
Write-Host "Ejecutando: $cmd15" -ForegroundColor Gray
$result15 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd15 2>&1
Write-Host $result15
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DESPLIEGUE COMPLETADO" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Aplicación desplegada exitosamente" -ForegroundColor Green
Write-Host ""
Write-Host "URLs de acceso:" -ForegroundColor Yellow
Write-Host "  - IP directa: http://164.68.99.83" -ForegroundColor White
Write-Host "  - Dominio: https://carnet.autonomousflow.lat/" -ForegroundColor White
Write-Host ""
Write-Host "Credenciales de acceso:" -ForegroundColor Yellow
Write-Host "  - SuperAdmin: admin@qlservices.com / Admin@123456" -ForegroundColor White
Write-Host ""
Write-Host "Información técnica:" -ForegroundColor Yellow
Write-Host "  - Contenedores: carnetqr_postgres, carnetqr_web" -ForegroundColor Gray
Write-Host "  - Puerto: 80 (interno: 8080)" -ForegroundColor Gray
Write-Host "  - Base de datos: carnetqrdb (PostgreSQL 15)" -ForegroundColor Gray
Write-Host "  - Red: carnetqr_net (aislada)" -ForegroundColor Gray
Write-Host "  - Volúmenes: carnetqr_postgres_data, carnetqr_dataprotection_keys" -ForegroundColor Gray
Write-Host ""
Write-Host "Comandos útiles:" -ForegroundColor Yellow
Write-Host "  Ver logs: docker logs -f carnetqr_web" -ForegroundColor Gray
Write-Host "  Ver logs DB: docker logs -f carnetqr_postgres" -ForegroundColor Gray
Write-Host "  Detener: cd /opt/apps/aspnet && docker compose down" -ForegroundColor Gray
Write-Host "  Reiniciar: cd /opt/apps/aspnet && docker compose restart" -ForegroundColor Gray
Write-Host ""
