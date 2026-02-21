# Script para crear tablas faltantes: refresh_tokens y password_reset_tokens
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "DC26Y0U5ER6sWj"
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CREAR TABLAS FALTANTES" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Leer estructura de refresh_tokens y password_reset_tokens desde structure.sql
$structurePath = "C:\Proyectos\PanamaTravelHub\PanamaTravelHub\database\temp_sync\structure.sql"

if (-not (Test-Path $structurePath)) {
    Write-Host "Error: No se encontro el archivo $structurePath" -ForegroundColor Red
    exit 1
}

Write-Host "Leyendo estructura de tablas..." -ForegroundColor Yellow
$content = Get-Content $structurePath -Raw -Encoding UTF8

# Extraer CREATE TABLE para password_reset_tokens
$passwordResetTableSQL = ""
if ($content -match "(CREATE TABLE public\.password_reset_tokens.*?);") {
    $passwordResetTableSQL = $matches[1].Trim()
    Write-Host "Estructura de password_reset_tokens encontrada" -ForegroundColor Green
}

# Extraer CREATE TABLE para refresh_tokens
$refreshTokenTableSQL = ""
if ($content -match "(CREATE TABLE public\.refresh_tokens.*?);") {
    $refreshTokenTableSQL = $matches[1].Trim()
    Write-Host "Estructura de refresh_tokens encontrada" -ForegroundColor Green
}

# Si no se encontraron, crear manualmente
if ([string]::IsNullOrWhiteSpace($passwordResetTableSQL)) {
    $passwordResetTableSQL = @"
CREATE TABLE public.password_reset_tokens (
    id uuid NOT NULL DEFAULT uuid_generate_v4(),
    user_id uuid NOT NULL,
    token character varying(255) NOT NULL,
    expires_at timestamp without time zone NOT NULL,
    is_used boolean NOT NULL DEFAULT false,
    used_at timestamp without time zone,
    ip_address character varying(45),
    user_agent text,
    created_at timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone,
    CONSTRAINT pk_password_reset_tokens PRIMARY KEY (id),
    CONSTRAINT fk_password_reset_tokens_user_id FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX uq_password_reset_tokens_token ON public.password_reset_tokens(token);
CREATE INDEX idx_password_reset_tokens_user_id ON public.password_reset_tokens(user_id);
CREATE INDEX idx_password_reset_tokens_expires_at ON public.password_reset_tokens(expires_at);
"@
}

if ([string]::IsNullOrWhiteSpace($refreshTokenTableSQL)) {
    $refreshTokenTableSQL = @"
CREATE TABLE public.refresh_tokens (
    id uuid NOT NULL DEFAULT uuid_generate_v4(),
    user_id uuid NOT NULL,
    token character varying(500) NOT NULL,
    expires_at timestamp without time zone NOT NULL,
    is_revoked boolean NOT NULL DEFAULT false,
    revoked_at timestamp without time zone,
    replaced_by_token character varying(500),
    ip_address character varying(45),
    user_agent text,
    created_at timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone,
    CONSTRAINT pk_refresh_tokens PRIMARY KEY (id),
    CONSTRAINT fk_refresh_tokens_user_id FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX uq_refresh_tokens_token ON public.refresh_tokens(token);
CREATE INDEX idx_refresh_tokens_user_id ON public.refresh_tokens(user_id);
CREATE INDEX idx_refresh_tokens_expires_at ON public.refresh_tokens(expires_at);
"@
}

$createTablesSQL = @"
-- Crear tablas faltantes para autenticacion

-- Tabla password_reset_tokens
$passwordResetTableSQL

-- Tabla refresh_tokens
$refreshTokenTableSQL
"@

# Copiar SQL al servidor usando base64
Write-Host "Creando tablas en el servidor..." -ForegroundColor Yellow
$bytes = [System.Text.Encoding]::UTF8.GetBytes($createTablesSQL)
$base64 = [System.Convert]::ToBase64String($bytes)

$tempScript = "create_missing_tables_$(Get-Date -Format 'yyyyMMdd_HHmmss').sql"
$remoteScriptPath = "/tmp/$tempScript"

$cmdCopy = "echo '$base64' | base64 -d > $remoteScriptPath"
& $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCopy 2>&1 | Out-Null

# Ejecutar creacion de tablas
$dbName = "panamatravelhub_db"
$dbUser = "panamatravelhub_user"

$cmdCreate = "docker exec -i panamatravelhub_postgres psql -U $dbUser -d $dbName < $remoteScriptPath 2>&1"
$resultCreate = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCreate 2>&1
Write-Host $resultCreate
Write-Host ""

# Limpiar archivo temporal
$cmdCleanup = "rm -f $remoteScriptPath"
& $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdCleanup 2>&1 | Out-Null

Write-Host ""
Write-Host "Verificando tablas creadas..." -ForegroundColor Yellow
$cmdVerify = "docker exec panamatravelhub_postgres psql -U $dbUser -d $dbName -c '\dt' 2>&1 | grep -E '(refresh|password_reset)' || echo 'Verificando manualmente...'"
$resultVerify = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmdVerify 2>&1
Write-Host $resultVerify
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "  TABLAS CREADAS" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Ahora intenta hacer login nuevamente en:" -ForegroundColor Yellow
Write-Host "  http://164.68.99.83:8082/login.html" -ForegroundColor Green
Write-Host ""
Write-Host "Credenciales:" -ForegroundColor Cyan
Write-Host "  Email: admin@panamatravelhub.com" -ForegroundColor White
Write-Host "  Password: Admin123! (o la que uses en local)" -ForegroundColor White
Write-Host ""
