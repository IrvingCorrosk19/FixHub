# Script para actualizar docker-compose.yml con formato correcto
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "Actualizando docker-compose.yml con formato correcto..." -ForegroundColor Yellow

# Crear docker-compose.yml usando cat con heredoc
$cmd = @"
cd /opt/apps/panamatravelhub && cat > docker-compose.yml << 'EOF'
services:
  postgres:
    image: postgres:16-alpine
    container_name: panamatravelhub_postgres
    restart: always
    env_file:
      - .env
    environment:
      PGDATA: /var/lib/postgresql/data/pgdata
    volumes:
      - panamatravelhub_postgres_data:/var/lib/postgresql/data
    ports:
      - "5433:5432"
    networks:
      - panamatravelhub_net
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U \`${POSTGRES_USER:-postgres}"]
      interval: 10s
      timeout: 5s
      retries: 5

  web:
    build: .
    container_name: panamatravelhub_web
    restart: always
    depends_on:
      postgres:
        condition: service_healthy
    env_file:
      - .env
    environment:
      ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=\`${POSTGRES_DB};Username=\`${POSTGRES_USER};Password=\`${POSTGRES_PASSWORD}
      ASPNETCORE_URLS: http://+:8080
      ASPNETCORE_DATAPROTECTION_PATH: /app/dataprotection-keys
    volumes:
      - panamatravelhub_dataprotection_keys:/app/dataprotection-keys
      - panamatravelhub_uploads:/app/wwwroot/uploads
    ports:
      - "8082:8080"
    networks:
      - panamatravelhub_net

volumes:
  panamatravelhub_postgres_data:
  panamatravelhub_dataprotection_keys:
  panamatravelhub_uploads:

networks:
  panamatravelhub_net:
    driver: bridge
EOF
"@

$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd 2>&1
Write-Host $result
Write-Host ""

Write-Host "Verificando sintaxis YAML..." -ForegroundColor Yellow
$cmdVerify = "cd /opt/apps/panamatravelhub && docker compose config > /dev/null 2>&1 && echo 'OK' || echo 'ERROR'"
$resultVerify = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdVerify 2>&1
Write-Host "Estado: $resultVerify" -ForegroundColor $(if ($resultVerify -match 'OK') { 'Green' } else { 'Red' })
Write-Host ""

if ($resultVerify -match 'OK')
{
    Write-Host "docker-compose.yml actualizado correctamente!" -ForegroundColor Green
}
else
{
    Write-Host "Error en docker-compose.yml. Mostrando contenido:" -ForegroundColor Red
    $cmdShow = "cd /opt/apps/panamatravelhub && cat docker-compose.yml"
    $resultShow = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdShow 2>&1
    Write-Host $resultShow
}
Write-Host ""
