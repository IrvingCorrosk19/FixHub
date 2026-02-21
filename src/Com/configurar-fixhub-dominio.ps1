# Configura fixhub.autonomousflow.lat con HTTPS (nginx + certbot)
# Requiere: DNS de fixhub.autonomousflow.lat apuntando a 164.68.99.83

$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FIXHUB - Dominio + HTTPS" -ForegroundColor Cyan
Write-Host "  fixhub.autonomousflow.lat" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "IMPORTANTE: fixhub.autonomousflow.lat debe apuntar a 164.68.99.83 (DNS)" -ForegroundColor Yellow
Write-Host ""

# PASO 1: Bloque temporal HTTP (para que certbot verifique el dominio)
Write-Host "PASO 1: Bloque temporal nginx para certbot..." -ForegroundColor Yellow
$cmd1 = "grep -q 'fixhub.autonomousflow.lat' /etc/nginx/sites-available/autonomousflow || (echo '' >> /etc/nginx/sites-available/autonomousflow && echo '# Temp FixHub - certbot' >> /etc/nginx/sites-available/autonomousflow && echo 'server {' >> /etc/nginx/sites-available/autonomousflow && echo '    listen 80;' >> /etc/nginx/sites-available/autonomousflow && echo '    server_name fixhub.autonomousflow.lat;' >> /etc/nginx/sites-available/autonomousflow && echo '    location / { return 200 ok; add_header Content-Type text/plain; }' >> /etc/nginx/sites-available/autonomousflow && echo '}' >> /etc/nginx/sites-available/autonomousflow && echo 'Bloque temporal añadido') && nginx -t && systemctl reload nginx && echo NginxOK"
$r1 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd1 2>&1
Write-Host $r1
Write-Host ""

# PASO 2: Ampliar certificado con fixhub
Write-Host "PASO 2: Ampliando certificado SSL..." -ForegroundColor Yellow
$cmd2 = "certbot certonly --nginx -d autonomousflow.lat -d carnet.autonomousflow.lat -d n8n.autonomousflow.lat -d travel.autonomousflow.lat -d fixhub.autonomousflow.lat --expand --non-interactive --agree-tos 2>&1"
$r2 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd2 2>&1
Write-Host $r2
Write-Host ""

# PASO 3: Reemplazar bloque temp por bloque HTTPS
Write-Host "PASO 3: Configurando nginx HTTPS para FixHub..." -ForegroundColor Yellow
$cmd3 = @"
# Quitar bloque temporal
sed -i '/# Temp FixHub - certbot/,/^}$/d' /etc/nginx/sites-available/autonomousflow
# Añadir bloque HTTPS si no existe
grep -q 'server_name fixhub.autonomousflow.lat' /etc/nginx/sites-available/autonomousflow || (
cat >> /etc/nginx/sites-available/autonomousflow << 'HTTPS'
# FixHub - https://fixhub.autonomousflow.lat
server {
    listen 443 ssl http2;
    server_name fixhub.autonomousflow.lat;
    ssl_certificate /etc/letsencrypt/live/autonomousflow.lat/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/autonomousflow.lat/privkey.pem;
    include /etc/letsencrypt/options-ssl-nginx.conf;
    ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem;
    location / {
        proxy_pass http://127.0.0.1:8081;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection upgrade;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }
}
HTTPS
echo 'Bloque HTTPS añadido'
)
nginx -t && systemctl reload nginx && echo 'Nginx recargado OK'
"@
$r3 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd3 2>&1
Write-Host $r3
Write-Host ""

# PASO 4: Actualizar .env y reiniciar FixHub
Write-Host "PASO 4: Actualizando FixHub (WEB_ORIGIN)..." -ForegroundColor Yellow
$cmd4 = "cd /opt/apps/fixhub && (grep -q '^WEB_ORIGIN=' .env && sed -i 's|^WEB_ORIGIN=.*|WEB_ORIGIN=https://fixhub.autonomousflow.lat|' .env || echo 'WEB_ORIGIN=https://fixhub.autonomousflow.lat' >> .env) && docker compose up -d api web 2>&1"
$r4 = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd4 2>&1
Write-Host $r4
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "  LISTO: https://fixhub.autonomousflow.lat" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
