# Script para crear tabla 'home_page_content'
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CREAR TABLA 'home_page_content'" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# SQL para crear tabla 'home_page_content'
$createTableSQL = @"
-- Crear tabla home_page_content
CREATE TABLE IF NOT EXISTS public.home_page_content (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    hero_title TEXT,
    hero_subtitle TEXT,
    hero_search_placeholder TEXT,
    hero_search_button TEXT,
    tours_section_title TEXT,
    tours_section_subtitle TEXT,
    loading_tours_text TEXT,
    error_loading_tours_text TEXT,
    no_tours_found_text TEXT,
    footer_brand_text TEXT,
    footer_description TEXT,
    footer_copyright TEXT,
    nav_brand_text TEXT,
    nav_tours_link TEXT,
    nav_bookings_link TEXT,
    nav_login_link TEXT,
    nav_logout_button TEXT,
    page_title TEXT,
    meta_description TEXT,
    logo_url TEXT,
    favicon_url TEXT,
    logo_url_social TEXT,
    hero_image_url TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

-- Crear índice si no existe
CREATE INDEX IF NOT EXISTS idx_home_page_content_created_at ON public.home_page_content(created_at);
"@

# Copiar SQL al servidor usando base64
Write-Host "Creando tabla 'home_page_content'..." -ForegroundColor Yellow
$bytes = [System.Text.Encoding]::UTF8.GetBytes($createTableSQL)
$base64 = [System.Convert]::ToBase64String($bytes)

$tempScript = "create_homepage_content_$(Get-Date -Format 'yyyyMMdd_HHmmss').sql"
$remoteScriptPath = "/tmp/$tempScript"

$cmdCopy = "echo '$base64' | base64 -d > $remoteScriptPath"
& $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCopy 2>&1 | Out-Null

# Ejecutar CREATE TABLE
$dbName = "panamatravelhub_db"
$dbUser = "panamatravelhub_user"

$cmdCreate = "docker exec -i panamatravelhub_postgres psql -U $dbUser -d $dbName < $remoteScriptPath 2>&1"
$resultCreate = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCreate 2>&1
Write-Host $resultCreate
Write-Host ""

# Limpiar archivo temporal
$cmdCleanup = "rm -f $remoteScriptPath"
& $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCleanup 2>&1 | Out-Null

# Verificar que la tabla se creó
Write-Host "Verificando que la tabla se creó correctamente..." -ForegroundColor Yellow
$cmdVerify = "docker exec panamatravelhub_postgres psql -U $dbUser -d $dbName -c '\d home_page_content' 2>&1 | head -30"
$resultVerify = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdVerify 2>&1
Write-Host $resultVerify
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "  TABLA 'home_page_content' CREADA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Ahora intenta cargar el CMS nuevamente en:" -ForegroundColor Yellow
Write-Host "  http://164.68.99.83:8082/Admin (Tab CMS)" -ForegroundColor Green
Write-Host ""
