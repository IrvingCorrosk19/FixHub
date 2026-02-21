# Script para actualizar docker-compose.yml en el servidor con la configuracion correcta
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ACTUALIZAR DOCKER-COMPOSE.YML" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$dockerComposeContent = @"
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
      test: ["CMD-SHELL", "pg_isready -U `${POSTGRES_USER:-postgres}"]
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
      ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=`${POSTGRES_DB};Username=`${POSTGRES_USER};Password=`${POSTGRES_PASSWORD}
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
"@

Write-Host "Actualizando docker-compose.yml en el servidor..." -ForegroundColor Yellow
$cmd = "cd /opt/apps/panamatravelhub && cat > docker-compose.yml << 'DOCKERCOMPOSE_EOF'
$dockerComposeContent
DOCKERCOMPOSE_EOF
"

$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd 2>&1
Write-Host $result
Write-Host ""

Write-Host "Verificando docker-compose.yml actualizado..." -ForegroundColor Yellow
$cmdVerify = "cd /opt/apps/panamatravelhub && cat docker-compose.yml | grep -E '(container_name|ports:|panamatravelhub_net)' | head -10"
$resultVerify = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdVerify 2>&1
Write-Host $resultVerify
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "  DOCKER-COMPOSE.YML ACTUALIZADO" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Ahora ejecuta: .\deploy-panamatravelhub.ps1" -ForegroundColor Yellow
Write-Host ""
