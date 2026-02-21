# 🚀 Guía Completa: Despliegue de Aplicación ASP.NET Core con Docker y PostgreSQL

## 📋 Tabla de Contenidos

1. [Preparación del Servidor VPS](#1-preparación-del-servidor-vps)
2. [Instalación de Docker](#2-instalación-de-docker)
3. [Preparación del Repositorio Git](#3-preparación-del-repositorio-git)
4. [Creación del Dockerfile](#4-creación-del-dockerfile)
5. [Configuración de docker-compose.yml](#5-configuración-de-docker-composeyml)
6. [Configuración de Variables de Entorno (.env)](#6-configuración-de-variables-de-entorno-env)
7. [Configuración de Program.cs (ASP.NET Core)](#7-configuración-de-programcs-aspnet-core)
8. [Configuración de DataProtection](#8-configuración-de-dataprotection)
9. [Configuración de Cookies y Autenticación](#9-configuración-de-cookies-y-autenticación)
10. [Scripts de Despliegue](#10-scripts-de-despliegue)
11. [Despliegue Inicial](#11-despliegue-inicial)
12. [Verificación y Pruebas](#12-verificación-y-pruebas)
13. [Solución de Problemas Comunes](#13-solución-de-problemas-comunes)
14. [Mantenimiento y Actualización](#14-mantenimiento-y-actualización)

---

## 1. Preparación del Servidor VPS

### 1.1 Información del Servidor

**Datos de ejemplo (ajusta según tu servidor):**
- **IP:** `164.68.99.83`
- **Sistema Operativo:** Ubuntu 22.04 LTS
- **Usuario:** `root`
- **Acceso:** SSH con PuTTY (Windows) o terminal SSH (Linux/Mac)

### 1.2 Conectarse al Servidor

**Desde Windows (PuTTY):**
```powershell
$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "TU_PASSWORD_AQUI"

& $plink -ssh -pw $password $hostname "comando_aqui"
```

**Desde Linux/Mac:**
```bash
ssh root@164.68.99.83
```

### 1.3 Actualizar el Sistema

```bash
# Actualizar lista de paquetes
sudo apt update

# Actualizar paquetes instalados
sudo apt upgrade -y

# Instalar herramientas básicas
sudo apt install -y curl wget git nano ufw
```

### 1.4 Configurar Firewall Básico

```bash
# Permitir SSH (para no perder conexión)
sudo ufw allow 22/tcp

# Permitir HTTP (puerto 80)
sudo ufw allow 80/tcp

# Permitir HTTPS (puerto 443) - para futuro
sudo ufw allow 443/tcp

# Opcional: Permitir PostgreSQL (solo si necesitas acceso externo)
sudo ufw allow 5432/tcp

# Habilitar firewall
sudo ufw --force enable

# Verificar estado
sudo ufw status
```

---

## 2. Instalación de Docker

### 2.1 Desinstalar Versiones Antiguas (si existen)

```bash
sudo apt-get remove docker docker-engine docker.io containerd runc
```

### 2.2 Instalar Docker

```bash
# Instalar dependencias
sudo apt-get update
sudo apt-get install -y \
    ca-certificates \
    curl \
    gnupg \
    lsb-release

# Agregar la clave GPG oficial de Docker
sudo mkdir -p /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg

# Configurar el repositorio
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Instalar Docker Engine
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

### 2.3 Verificar Instalación

```bash
# Verificar versión de Docker
docker --version
# Salida esperada: Docker version 24.x.x, build xxxxx

# Verificar versión de Docker Compose
docker compose version
# Salida esperada: Docker Compose version v2.x.x

# Probar Docker con contenedor de prueba
docker run hello-world
```

### 2.4 Configurar Docker para Usuario No Root (Opcional)

```bash
# Crear grupo docker
sudo groupadd docker

# Agregar usuario al grupo
sudo usermod -aG docker $USER

# Aplicar cambios
newgrp docker

# Probar sin sudo
docker run hello-world
```

---

## 3. Preparación del Repositorio Git

### 3.1 Crear Directorio para la Aplicación

```bash
# Crear directorio base para aplicaciones
sudo mkdir -p /opt/apps

# Navegar al directorio
cd /opt/apps
```

### 3.2 Clonar el Repositorio

```bash
# Clonar repositorio (reemplaza con tu URL)
git clone https://github.com/TU_USUARIO/TU_REPOSITORIO.git aspnet

# Navegar al directorio del proyecto
cd aspnet

# Verificar que se clonó correctamente
ls -la
```

### 3.3 Configurar Git (si vas a hacer commits desde el servidor)

```bash
git config --global user.name "Tu Nombre"
git config --global user.email "tu@email.com"
```

---

## 4. Creación del Dockerfile

### 4.1 Identificar la Estructura del Proyecto

**Estructura típica de un proyecto ASP.NET Core:**
```
TuProyecto/
├── TuProyecto.sln
├── TuProyecto.Web/
│   ├── TuProyecto.Web.csproj
│   ├── Program.cs
│   └── appsettings.json
├── TuProyecto.Application/
│   └── TuProyecto.Application.csproj
├── TuProyecto.Domain/
│   └── TuProyecto.Domain.csproj
└── TuProyecto.Infrastructure/
    └── TuProyecto.Infrastructure.csproj
```

### 4.2 Crear el Dockerfile en la Raíz del Proyecto

**Ubicación:** En la raíz del proyecto (donde está el `.sln`)

**Contenido del Dockerfile:**

```dockerfile
# ============================================
# Build stage - Compilar la aplicación
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de solución y proyectos
COPY TuProyecto.sln .
COPY TuProyecto.Application/ TuProyecto.Application/
COPY TuProyecto.Domain/ TuProyecto.Domain/
COPY TuProyecto.Infrastructure/ TuProyecto.Infrastructure/
COPY TuProyecto.Web/ TuProyecto.Web/

# Restaurar dependencias
RUN dotnet restore

# Publicar la aplicación
RUN dotnet publish TuProyecto.Web/TuProyecto.Web.csproj -c Release -o /app/publish

# ============================================
# Runtime stage - Ejecutar la aplicación
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Exponer puerto (ASP.NET Core usa 8080 por defecto en .NET 8)
EXPOSE 8080

# Copiar archivos publicados desde build stage
COPY --from=build /app/publish .

# Punto de entrada
ENTRYPOINT ["dotnet", "TuProyecto.Web.dll"]
```

**⚠️ IMPORTANTE:** Reemplaza:
- `TuProyecto.sln` con el nombre real de tu archivo `.sln`
- `TuProyecto.Application/`, `TuProyecto.Domain/`, etc. con los nombres reales de tus proyectos
- `TuProyecto.Web.csproj` con el nombre real del proyecto web
- `TuProyecto.Web.dll` con el nombre real del DLL de salida

### 4.3 Ejemplo Real (CarnetQR Platform)

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CarnetQRPlatform.sln .
COPY CarnetQRPlatform.Application/ CarnetQRPlatform.Application/
COPY CarnetQRPlatform.Domain/ CarnetQRPlatform.Domain/
COPY CarnetQRPlatform.Infrastructure/ CarnetQRPlatform.Infrastructure/
COPY CarnetQRPlatform.Web/ CarnetQRPlatform.Web/

RUN dotnet restore
RUN dotnet publish CarnetQRPlatform.Web/CarnetQRPlatform.Web.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CarnetQRPlatform.Web.dll"]
```

### 4.4 Verificar Sintaxis del Dockerfile

```bash
# Verificar que no hay errores de sintaxis
docker build --no-cache -t test-build . --progress=plain
```

---

## 5. Configuración de docker-compose.yml

### 5.1 Crear docker-compose.yml en la Raíz

**Ubicación:** En la raíz del proyecto (junto al `Dockerfile`)

**Contenido de docker-compose.yml:**

```yaml
services:
  postgres:
    image: postgres:15
    container_name: tuapp_postgres
    restart: always
    env_file:
      - .env
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"  # Opcional: para acceso externo (pgAdmin)
    networks:
      - tuapp_net
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 10s
      timeout: 5s
      retries: 5

  web:
    build: .
    container_name: tuapp_web
    restart: always
    depends_on:
      postgres:
        condition: service_healthy
    env_file:
      - .env
    environment:
      ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      ASPNETCORE_URLS: http://+:8080
      ASPNETCORE_DATAPROTECTION_PATH: /app/dataprotection-keys
    volumes:
      - dataprotection_keys:/app/dataprotection-keys
      - uploads:/app/wwwroot/uploads  # Para archivos subidos (fotos, etc.)
    ports:
      - "80:8080"  # Mapear puerto 80 del host al 8080 del contenedor
    networks:
      - tuapp_net

volumes:
  postgres_data:
    # Volumen persistente para datos de PostgreSQL
  dataprotection_keys:
    # Volumen persistente para claves de DataProtection
  uploads:
    # Volumen persistente para archivos subidos (opcional)

networks:
  tuapp_net:
    # Red interna para comunicación entre contenedores
```

**⚠️ IMPORTANTE:** Reemplaza:
- `tuapp_postgres` con un nombre descriptivo (ej: `carnetqr_postgres`)
- `tuapp_web` con un nombre descriptivo (ej: `carnetqr_web`)
- `tuapp_net` con un nombre descriptivo (ej: `carnetqr_net`)

### 5.2 Explicación de Componentes Clave

#### Servicio `postgres`

- **`image: postgres:15`**: Versión de PostgreSQL a usar
- **`restart: always`**: Reiniciar automáticamente si falla
- **`env_file: - .env`**: Cargar variables desde archivo `.env`
- **`volumes`**: Persistir datos de la base de datos
- **`ports`**: Exponer puerto 5432 (opcional, para herramientas externas)
- **`healthcheck`**: Verificar que PostgreSQL está listo antes de iniciar web

#### Servicio `web`

- **`build: .`**: Construir imagen desde Dockerfile en la raíz
- **`depends_on`**: Esperar a que PostgreSQL esté listo
- **`environment`**: Variables de entorno específicas
- **`ConnectionStrings__DefaultConnection`**: Cadena de conexión a PostgreSQL
  - Nota: Usa `postgres` (nombre del servicio) como host, NO `localhost`
- **`ASPNETCORE_URLS`**: URL de escucha (puerto 8080)
- **`ASPNETCORE_DATAPROTECTION_PATH`**: Ruta para claves de DataProtection
- **`volumes`**: Volúmenes persistentes
  - `dataprotection_keys`: Para claves de cifrado
  - `uploads`: Para archivos subidos por usuarios
- **`ports: - "80:8080"`**: Mapear puerto del host (80) al contenedor (8080)

#### Volúmenes

- **`postgres_data`**: Datos de PostgreSQL (persisten entre reinicios)
- **`dataprotection_keys`**: Claves de DataProtection (persisten entre reinicios)
- **`uploads`**: Archivos subidos (persisten entre reinicios)

#### Redes

- **`tuapp_net`**: Red interna para que los contenedores se comuniquen

---

## 6. Configuración de Variables de Entorno (.env)

### 6.1 Crear Archivo .env en la Raíz

**Ubicación:** En la raíz del proyecto (junto al `docker-compose.yml`)

**⚠️ MUY IMPORTANTE:**
- Este archivo contiene información sensible (contraseñas)
- **NUNCA** subir a Git/GitHub
- Agregar `.env` al `.gitignore`

### 6.2 Contenido del Archivo .env

```env
# ============================================
# Configuración de PostgreSQL
# ============================================
POSTGRES_DB=tuapp_db
POSTGRES_USER=tuapp_user
POSTGRES_PASSWORD=TuPasswordSuperSegura123!

# ============================================
# Configuración de ASP.NET Core
# ============================================
ASPNETCORE_ENVIRONMENT=Production

# ============================================
# Configuración de la Aplicación (opcional)
# ============================================
# JWT_SECRET=TuSecretoJWT_Si_Lo_Usas
# EMAIL_HOST=smtp.example.com
# EMAIL_PORT=587
# EMAIL_USER=noreply@tuapp.com
# EMAIL_PASSWORD=tu_password_email
```

**⚠️ IMPORTANTE:**
- Cambia `tuapp_db`, `tuapp_user` por nombres descriptivos
- Usa una contraseña fuerte para `POSTGRES_PASSWORD`
- Agrega otras variables según las necesidades de tu aplicación

### 6.3 Agregar .env al .gitignore

```bash
# Crear o editar .gitignore
nano .gitignore

# Agregar esta línea
.env
```

### 6.4 Crear .env.example (para documentación)

```bash
# Crear archivo de ejemplo
nano .env.example
```

**Contenido de .env.example:**

```env
# ============================================
# Configuración de PostgreSQL
# ============================================
POSTGRES_DB=nombre_base_datos
POSTGRES_USER=usuario_db
POSTGRES_PASSWORD=password_seguro_aqui

# ============================================
# Configuración de ASP.NET Core
# ============================================
ASPNETCORE_ENVIRONMENT=Production

# ============================================
# Configuración de la Aplicación
# ============================================
# JWT_SECRET=tu_secreto_aqui
# EMAIL_HOST=smtp.example.com
```

**Este archivo SÍ se sube a Git** como plantilla.

---

## 7. Configuración de Program.cs (ASP.NET Core)

### 7.1 Ubicación del Archivo

**Ruta:** `TuProyecto.Web/Program.cs`

### 7.2 Configuraciones Obligatorias para Docker

```csharp
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 1. DataProtection - Persistir claves
// ============================================
var dataProtectionPath = Environment.GetEnvironmentVariable("ASPNETCORE_DATAPROTECTION_PATH") 
    ?? "/app/dataprotection-keys";

if (!Directory.Exists(dataProtectionPath))
{
    Directory.CreateDirectory(dataProtectionPath);
}

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("TuAplicacion")  // ⚠️ Cambiar por el nombre de tu aplicación
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// ============================================
// 2. Forwarded Headers - Para Docker/Proxy
// ============================================
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ============================================
// 3. Base de Datos - PostgreSQL
// ============================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ============================================
// 4. Identity - Si usas autenticación
// ============================================
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ============================================
// 5. Cookies - Configuración para HTTP
// ============================================
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    
    // ⚠️ IMPORTANTE: Para HTTP sin HTTPS
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ... Otros servicios (MVC, Razor Pages, etc.)

var app = builder.Build();

// ============================================
// MIDDLEWARE PIPELINE
// ============================================

// ⚠️ 1. PRIMERO: Forwarded Headers
app.UseForwardedHeaders();

// 2. Manejo de errores
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // ⚠️ NO usar app.UseHsts() si no tienes HTTPS configurado
}

// ⚠️ 3. NO usar HTTPS redirection si solo tienes HTTP
// app.UseHttpsRedirection();  // ← COMENTAR o REMOVER

// 4. Archivos estáticos
app.UseStaticFiles();

// 5. Routing
app.UseRouting();

// 6. Autenticación y Autorización
app.UseAuthentication();
app.UseAuthorization();

// 7. Endpoints
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ============================================
// Inicialización de Base de Datos
// ============================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        await DbInitializer.InitializeAsync(context, userManager, roleManager, logger);
        
        var userCount = await context.Users.CountAsync();
        logger.LogInformation("Database initialized. Total users: {Count}", userCount);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

app.Run();
```

### 7.3 Puntos Clave del Program.cs

#### ✅ LO QUE DEBES HACER:

1. **Agregar `using` statements:**
   ```csharp
   using Microsoft.AspNetCore.HttpOverrides;
   using Microsoft.AspNetCore.DataProtection;
   ```

2. **Configurar DataProtection ANTES de `builder.Build()`**

3. **Configurar ForwardedHeaders ANTES de `builder.Build()`**

4. **Configurar Cookies con `SameSiteMode.Lax` y `CookieSecurePolicy.SameAsRequest`**

5. **Llamar `app.UseForwardedHeaders()` PRIMERO en el pipeline**

#### ❌ LO QUE NO DEBES HACER:

1. **NO usar `app.UseHttpsRedirection()`** si solo tienes HTTP
2. **NO usar `app.UseHsts()`** si no tienes HTTPS
3. **NO usar `CookieSecurePolicy.Always`** sin HTTPS
4. **NO usar `SameSiteMode.Strict`** sin HTTPS

---

## 8. Configuración de DataProtection

### 8.1 ¿Por Qué es Necesario?

ASP.NET Core usa DataProtection para:
- Cifrar cookies de autenticación
- Proteger tokens anti-falsificación (CSRF)
- Cifrar datos sensibles

**En Docker:** Si no configuras DataProtection con un volumen persistente, las claves se regeneran cada vez que reinicias el contenedor, causando:
- Errores 400 en login
- `AntiforgeryValidationException`
- `CryptographicException: The key was not found`

### 8.2 Configuración Completa

```csharp
// En Program.cs, ANTES de builder.Build()

var dataProtectionPath = Environment.GetEnvironmentVariable("ASPNETCORE_DATAPROTECTION_PATH") 
    ?? "/app/dataprotection-keys";

// Crear directorio si no existe
if (!Directory.Exists(dataProtectionPath))
{
    Directory.CreateDirectory(dataProtectionPath);
}

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("TuAplicacion")  // ⚠️ IMPORTANTE: Nombre consistente
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
```

### 8.3 Configuración en docker-compose.yml

```yaml
services:
  web:
    # ... otras configuraciones
    environment:
      ASPNETCORE_DATAPROTECTION_PATH: /app/dataprotection-keys
    volumes:
      - dataprotection_keys:/app/dataprotection-keys

volumes:
  dataprotection_keys:
```

### 8.4 Verificar que Funciona

```bash
# Verificar que el volumen existe
docker volume ls | grep dataprotection

# Inspeccionar el volumen
docker volume inspect NOMBRE_DEL_VOLUMEN

# Ver contenido del directorio en el contenedor
docker exec NOMBRE_CONTENEDOR ls -la /app/dataprotection-keys
```

---

## 9. Configuración de Cookies y Autenticación

### 9.1 Configuración de Cookies para HTTP (sin HTTPS)

**En `DependencyInjection.cs` o `Program.cs`:**

```csharp
services.ConfigureApplicationCookie(options =>
{
    // Rutas de autenticación
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    
    // Expiración
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    
    // ⚠️ CONFIGURACIÓN CLAVE PARA HTTP SIN HTTPS
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    
    // Opcional: Nombre de la cookie
    options.Cookie.Name = ".TuAplicacion.Auth";
    options.Cookie.HttpOnly = true;
});
```

### 9.2 Opciones de SameSite

| Opción | Cuándo Usar | Notas |
|--------|-------------|-------|
| `SameSiteMode.Lax` | ✅ HTTP sin HTTPS | Recomendado para Docker inicial |
| `SameSiteMode.Strict` | HTTPS configurado | Máxima seguridad |
| `SameSiteMode.None` | Cross-site requests | Requiere `Secure=true` |

### 9.3 Opciones de SecurePolicy

| Opción | Cuándo Usar | Notas |
|--------|-------------|-------|
| `CookieSecurePolicy.SameAsRequest` | ✅ HTTP y HTTPS | Flexible, se adapta al protocolo |
| `CookieSecurePolicy.Always` | Solo HTTPS | Requiere HTTPS configurado |
| `CookieSecurePolicy.None` | Desarrollo local | No recomendado para producción |

### 9.4 Configuración de ForwardedHeaders

```csharp
// En Program.cs, ANTES de builder.Build()

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
    
    // Limpiar redes y proxies conocidos para aceptar todos
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// En el pipeline, PRIMER middleware
app.UseForwardedHeaders();
```

**¿Por qué es necesario?**
- Docker/Proxy modifican los headers HTTP
- ASP.NET Core necesita saber el protocolo real (HTTP/HTTPS)
- Sin esto, las cookies no funcionan correctamente

---

## 10. Scripts de Despliegue

### 10.1 Crear Carpeta para Scripts

```bash
# En tu proyecto local (Windows)
mkdir Com
cd Com
```

### 10.2 Script: deploy-docker.ps1

**Propósito:** Desplegar la aplicación desde cero o después de cambios

**Contenido:**

```powershell
# ============================================
# deploy-docker.ps1
# Despliega la aplicación en el servidor VPS
# ============================================

$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "TU_PASSWORD_AQUI"
$hostkey = "ssh-ed25519 SHA256:TU_HOSTKEY_AQUI"

Write-Host "=== DESPLIEGUE EN SERVIDOR VPS ===" -ForegroundColor Cyan
Write-Host ""

# 1. Pull de cambios
Write-Host "1. Actualizando código desde Git..." -ForegroundColor Yellow
$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "cd /opt/apps/aspnet && git pull" 2>&1
Write-Host $result
Write-Host ""

# 2. Crear archivo .env si no existe
Write-Host "2. Verificando archivo .env..." -ForegroundColor Yellow
$envContent = @"
POSTGRES_DB=tuapp_db
POSTGRES_USER=tuapp_user
POSTGRES_PASSWORD=TuPasswordSegura123!
ASPNETCORE_ENVIRONMENT=Production
"@

$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "cd /opt/apps/aspnet && if [ ! -f .env ]; then echo '$envContent' > .env; echo 'Archivo .env creado'; else echo 'Archivo .env ya existe'; fi" 2>&1
Write-Host $result
Write-Host ""

# 3. Build y up
Write-Host "3. Construyendo y levantando contenedores..." -ForegroundColor Yellow
$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "cd /opt/apps/aspnet && docker compose up -d --build" 2>&1
Write-Host $result
Write-Host ""

# 4. Verificar contenedores
Write-Host "4. Verificando contenedores..." -ForegroundColor Yellow
Start-Sleep -Seconds 5
$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "docker ps" 2>&1
Write-Host $result
Write-Host ""

# 5. Ver logs
Write-Host "5. Últimos logs de la aplicación..." -ForegroundColor Yellow
$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "docker logs tuapp_web --tail 30" 2>&1
Write-Host $result
Write-Host ""

Write-Host "=== DESPLIEGUE COMPLETADO ===" -ForegroundColor Green
Write-Host "Accede a: http://164.68.99.83" -ForegroundColor Green
```

### 10.3 Script: rebuild-deploy.ps1

**Propósito:** Rebuild completo sin cache (para solucionar problemas)

**Contenido:**

```powershell
# ============================================
# rebuild-deploy.ps1
# Rebuild completo sin cache
# ============================================

$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "TU_PASSWORD_AQUI"
$hostkey = "ssh-ed25519 SHA256:TU_HOSTKEY_AQUI"

Write-Host "=== REBUILD COMPLETO SIN CACHE ===" -ForegroundColor Cyan
Write-Host ""

# 1. Pull
Write-Host "1. Actualizando código..." -ForegroundColor Yellow
$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "cd /opt/apps/aspnet && git pull" 2>&1
Write-Host $result
Write-Host ""

# 2. Down
Write-Host "2. Deteniendo contenedores..." -ForegroundColor Yellow
$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "cd /opt/apps/aspnet && docker compose down" 2>&1
Write-Host $result
Write-Host ""

# 3. Build sin cache
Write-Host "3. Construyendo sin cache (puede tardar varios minutos)..." -ForegroundColor Yellow
$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "cd /opt/apps/aspnet && docker compose build --no-cache" 2>&1
Write-Host $result
Write-Host ""

# 4. Up
Write-Host "4. Levantando contenedores..." -ForegroundColor Yellow
$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "cd /opt/apps/aspnet && docker compose up -d" 2>&1
Write-Host $result
Write-Host ""

# 5. Logs
Write-Host "5. Esperando 20 segundos..." -ForegroundColor Yellow
Start-Sleep -Seconds 20

$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "docker logs tuapp_web --tail 30" 2>&1
Write-Host $result
Write-Host ""

Write-Host "=== REBUILD COMPLETADO ===" -ForegroundColor Green
```

### 10.4 Script: verificar-db.ps1

**Propósito:** Verificar estado de PostgreSQL

**Contenido:**

```powershell
# ============================================
# verificar-db.ps1
# Verifica el estado de PostgreSQL
# ============================================

$plink = "C:\Program Files\PuTTY\plink.exe"
$hostname = "root@164.68.99.83"
$password = "TU_PASSWORD_AQUI"
$hostkey = "ssh-ed25519 SHA256:TU_HOSTKEY_AQUI"

Write-Host "=== VERIFICACION DE BASE DE DATOS ===" -ForegroundColor Cyan
Write-Host ""

# 1. Contenedores
Write-Host "1. Contenedores activos:" -ForegroundColor Yellow
$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "docker ps | grep postgres" 2>&1
Write-Host $result
Write-Host ""

# 2. Logs PostgreSQL
Write-Host "2. Logs de PostgreSQL:" -ForegroundColor Yellow
$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "docker logs tuapp_postgres --tail 20" 2>&1
Write-Host $result
Write-Host ""

# 3. Verificar conexión
Write-Host "3. Verificando conexión a base de datos:" -ForegroundColor Yellow
$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "docker exec tuapp_postgres psql -U tuapp_user -d tuapp_db -c '\dt'" 2>&1
Write-Host $result
Write-Host ""

Write-Host "=== VERIFICACION COMPLETADA ===" -ForegroundColor Green
```

---

## 11. Despliegue Inicial

### 11.1 Preparación Local

**1. Verificar que tienes todos los archivos:**
```
TuProyecto/
├── Dockerfile             ✅
├── docker-compose.yml     ✅
├── .env.example           ✅
├── .gitignore             ✅ (incluye .env)
├── Com/
│   ├── deploy-docker.ps1  ✅
│   ├── rebuild-deploy.ps1 ✅
│   └── verificar-db.ps1   ✅
└── TuProyecto.Web/
    └── Program.cs         ✅ (con configuraciones)
```

**2. Hacer commit y push:**
```bash
git add .
git commit -m "Configuración Docker completa"
git push origin main
```

### 11.2 Despliegue en el Servidor

**Opción 1: Usar script PowerShell (Windows)**

```powershell
cd "C:\Proyectos\TuProyecto\Com"
.\deploy-docker.ps1
```

**Opción 2: Comando manual (desde servidor)**

```bash
# Conectarse al servidor
ssh root@164.68.99.83

# Navegar al directorio
cd /opt/apps/aspnet

# Pull de cambios
git pull

# Crear archivo .env
nano .env
# (Pegar contenido y guardar con Ctrl+O, Enter, Ctrl+X)

# Levantar contenedores
docker compose up -d --build

# Ver logs
docker logs tuapp_web --tail 50
```

### 11.3 Verificar Despliegue

```bash
# Ver contenedores activos
docker ps

# Deberías ver algo como:
# CONTAINER ID   NAME          IMAGE              STATUS
# xxxxxxxxx      tuapp_web     tuapp-web:latest   Up X minutes
# xxxxxxxxx      tuapp_postgres postgres:15       Up X minutes

# Ver logs de web
docker logs tuapp_web --tail 100

# Buscar líneas clave:
# ✅ "Now listening on: http://0.0.0.0:8080"
# ✅ "Database migrations completed"
# ✅ "Database initialized"

# Ver logs de postgres
docker logs tuapp_postgres --tail 50

# Buscar líneas clave:
# ✅ "database system is ready to accept connections"
```

### 11.4 Probar la Aplicación

**Desde el navegador:**
```
http://164.68.99.83
```

**Deberías ver:**
- ✅ La página principal de tu aplicación
- ✅ Sin errores 500 o 404
- ✅ Login funciona correctamente

---

## 12. Verificación y Pruebas

### 12.1 Verificar Contenedores

```bash
# Ver todos los contenedores
docker ps -a

# Ver solo contenedores activos
docker ps

# Ver uso de recursos
docker stats

# Ver logs en tiempo real
docker logs -f tuapp_web
```

### 12.2 Verificar PostgreSQL

```bash
# Entrar al contenedor de PostgreSQL
docker exec -it tuapp_postgres psql -U tuapp_user -d tuapp_db

# Dentro de PostgreSQL:
# Listar tablas
\dt

# Ver usuarios
SELECT * FROM "AspNetUsers";

# Salir
\q
```

### 12.3 Verificar Volúmenes

```bash
# Listar volúmenes
docker volume ls

# Inspeccionar volumen de datos
docker volume inspect NOMBRE_postgres_data

# Inspeccionar volumen de DataProtection
docker volume inspect NOMBRE_dataprotection_keys

# Ver archivos en el volumen
docker exec tuapp_web ls -la /app/dataprotection-keys
```

### 12.4 Verificar Red

```bash
# Listar redes
docker network ls

# Inspeccionar red
docker network inspect NOMBRE_tuapp_net

# Ver qué contenedores están en la red
docker network inspect NOMBRE_tuapp_net | grep Name
```

### 12.5 Pruebas de Funcionalidad

**1. Login:**
- Acceder a `/Account/Login`
- Ingresar credenciales
- Verificar que NO hay error 400

**2. Cookies:**
- Abrir herramientas de desarrollador (F12)
- Ir a Application → Cookies
- Verificar que existe la cookie de autenticación

**3. Restart Test:**
```bash
# Reiniciar contenedor web
docker restart tuapp_web

# Esperar 10 segundos
sleep 10

# Intentar login nuevamente
# Debería funcionar sin error 400
```

---

## 13. Solución de Problemas Comunes

### 13.1 Error 400 en Login

**Síntomas:**
- `HTTP ERROR 400` al intentar login
- En logs: `AntiforgeryValidationException`

**Causas:**
1. DataProtection no configurado correctamente
2. Volumen de DataProtection no persistente
3. Cookies del navegador antiguas

**Soluciones:**

```bash
# 1. Verificar configuración de DataProtection en Program.cs
# 2. Verificar volumen en docker-compose.yml
# 3. Rebuild sin cache
docker compose down
docker compose build --no-cache
docker compose up -d

# 4. Limpiar cookies del navegador
# - Abrir ventana de incógnito
# - O limpiar cookies manualmente (F12 → Application → Cookies)
```

### 13.2 Error: "The key was not found in the key ring"

**Síntomas:**
- `CryptographicException`
- En logs: "The key {GUID} was not found"

**Solución:**

```bash
# 1. Verificar volumen de DataProtection
docker volume ls | grep dataprotection

# 2. Si no existe, agregar a docker-compose.yml
# 3. Rebuild
docker compose down -v  # ⚠️ Esto borra volúmenes
docker compose up -d --build

# 4. Limpiar cookies del navegador
```

### 13.3 Error: Connection Refused (PostgreSQL)

**Síntomas:**
- En logs: "Connection refused"
- En logs: "Could not connect to server"

**Solución:**

```bash
# 1. Verificar que PostgreSQL está corriendo
docker ps | grep postgres

# 2. Ver logs de PostgreSQL
docker logs tuapp_postgres

# 3. Verificar healthcheck
docker inspect tuapp_postgres | grep Health

# 4. Verificar cadena de conexión
# Asegúrate de usar "postgres" como host, NO "localhost"
# ConnectionStrings__DefaultConnection: Host=postgres;...
```

### 13.4 Error: "Could not find a part of the path"

**Síntomas:**
- Error al crear directorios
- Problemas con rutas de archivos

**Solución:**

```bash
# 1. Verificar volúmenes en docker-compose.yml
# 2. Verificar permisos de directorios en Program.cs

# Crear directorios en tiempo de ejecución:
if (!Directory.Exists(path))
{
    Directory.CreateDirectory(path);
}
```

### 13.5 Error: MissingMethodException

**Síntomas:**
- `MissingMethodException` en logs
- Problemas con DataProtection

**Causa:**
- Versiones incompatibles de paquetes NuGet

**Solución:**

```bash
# 1. Verificar versiones en .csproj
# Asegúrate de que todos los paquetes sean compatibles con .NET 8:
# Microsoft.Extensions.* → versión 8.0.x
# Microsoft.AspNetCore.* → versión 8.0.x

# 2. Limpiar y rebuild
dotnet clean
dotnet restore
docker compose build --no-cache
```

### 13.6 Aplicación No Carga (Error 502/504)

**Síntomas:**
- Página no carga
- Error 502 Bad Gateway
- Error 504 Gateway Timeout

**Solución:**

```bash
# 1. Ver logs de web
docker logs tuapp_web --tail 100

# 2. Ver si la aplicación está escuchando
docker exec tuapp_web netstat -tlnp

# 3. Verificar puerto en docker-compose.yml
# ports:
#   - "80:8080"  # Host:Contenedor

# 4. Verificar ASPNETCORE_URLS
# environment:
#   ASPNETCORE_URLS: http://+:8080

# 5. Verificar que no hay otro servicio en puerto 80
sudo lsof -i :80
```

### 13.7 Volúmenes No Persisten

**Síntomas:**
- Datos se pierden al reiniciar
- Claves de DataProtection se regeneran

**Solución:**

```bash
# 1. Verificar que los volúmenes existen
docker volume ls

# 2. Verificar montaje en docker-compose.yml
# volumes:
#   - dataprotection_keys:/app/dataprotection-keys

# 3. No usar "docker compose down -v" (borra volúmenes)
# Usar solo "docker compose down"

# 4. Inspeccionar volumen
docker volume inspect NOMBRE_VOLUMEN
```

---

## 14. Mantenimiento y Actualización

### 14.1 Actualizar Código

```bash
# Método 1: Con script (recomendado)
cd Com
.\deploy-docker.ps1

# Método 2: Manual
ssh root@164.68.99.83
cd /opt/apps/aspnet
git pull
docker compose up -d --build
```

### 14.2 Actualizar Solo Web (sin rebuild de DB)

```bash
# Detener solo web
docker compose stop web

# Rebuild solo web
docker compose build web

# Levantar web
docker compose up -d web
```

### 14.3 Backup de Base de Datos

```bash
# Crear backup
docker exec tuapp_postgres pg_dump -U tuapp_user tuapp_db > backup_$(date +%Y%m%d).sql

# Restaurar backup
cat backup_20260117.sql | docker exec -i tuapp_postgres psql -U tuapp_user -d tuapp_db
```

### 14.4 Backup de Volúmenes

```bash
# Backup de volumen de datos
docker run --rm \
  -v NOMBRE_postgres_data:/data \
  -v $(pwd):/backup \
  ubuntu tar czf /backup/postgres_backup.tar.gz /data

# Restaurar volumen
docker run --rm \
  -v NOMBRE_postgres_data:/data \
  -v $(pwd):/backup \
  ubuntu tar xzf /backup/postgres_backup.tar.gz -C /
```

### 14.5 Ver Logs de Producción

```bash
# Logs en tiempo real
docker logs -f tuapp_web

# Últimas 100 líneas
docker logs tuapp_web --tail 100

# Filtrar errores
docker logs tuapp_web 2>&1 | grep -i error

# Exportar logs a archivo
docker logs tuapp_web > logs_$(date +%Y%m%d_%H%M%S).txt
```

### 14.6 Reiniciar Servicios

```bash
# Reiniciar todo
docker compose restart

# Reiniciar solo web
docker compose restart web

# Reiniciar solo postgres
docker compose restart postgres
```

### 14.7 Limpiar Recursos

```bash
# Limpiar imágenes no usadas
docker image prune -a

# Limpiar contenedores detenidos
docker container prune

# Limpiar todo (⚠️ CUIDADO: no borra volúmenes)
docker system prune -a

# Ver uso de disco
docker system df
```

---

## 15. Checklist de Despliegue

### ✅ Pre-Despliegue

- [ ] Servidor VPS configurado y accesible por SSH
- [ ] Docker y Docker Compose instalados
- [ ] Firewall configurado (puertos 22, 80, 443)
- [ ] Repositorio Git clonado en `/opt/apps/aspnet`
- [ ] Estructura del proyecto identificada (`.sln`, proyectos)

### ✅ Archivos de Configuración

- [ ] `Dockerfile` creado en la raíz
- [ ] `docker-compose.yml` creado en la raíz
- [ ] `.env` creado con credenciales correctas
- [ ] `.env.example` creado para documentación
- [ ] `.gitignore` incluye `.env`

### ✅ Configuración de ASP.NET Core

- [ ] `Program.cs` configurado con DataProtection
- [ ] `Program.cs` configurado con ForwardedHeaders
- [ ] Cookies configuradas con `SameSiteMode.Lax`
- [ ] `UseHttpsRedirection()` comentado o removido
- [ ] `UseForwardedHeaders()` como primer middleware
- [ ] Inicialización de base de datos implementada

### ✅ Configuración de Docker

- [ ] Volumen `postgres_data` configurado
- [ ] Volumen `dataprotection_keys` configurado
- [ ] Volumen `uploads` configurado (si aplica)
- [ ] Puerto 80:8080 mapeado
- [ ] Red interna configurada
- [ ] Healthcheck de PostgreSQL configurado
- [ ] `depends_on` con `service_healthy` configurado

### ✅ Variables de Entorno

- [ ] `POSTGRES_DB` configurada
- [ ] `POSTGRES_USER` configurada
- [ ] `POSTGRES_PASSWORD` configurada
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `ASPNETCORE_DATAPROTECTION_PATH` configurada
- [ ] `ConnectionStrings__DefaultConnection` configurada

### ✅ Despliegue

- [ ] Código subido a Git
- [ ] `git pull` ejecutado en el servidor
- [ ] `docker compose up -d --build` ejecutado
- [ ] Contenedores corriendo: `docker ps`
- [ ] Logs sin errores: `docker logs tuapp_web`
- [ ] PostgreSQL listo: `docker logs tuapp_postgres`

### ✅ Verificación

- [ ] Aplicación accesible en `http://IP_DEL_SERVIDOR`
- [ ] Página principal carga correctamente
- [ ] Login funciona sin error 400
- [ ] Cookies se establecen correctamente
- [ ] Restart test exitoso (contenedor reiniciado, login sigue funcionando)
- [ ] Base de datos tiene tablas creadas
- [ ] Usuarios de prueba existen

### ✅ Scripts de Mantenimiento

- [ ] `deploy-docker.ps1` creado y probado
- [ ] `rebuild-deploy.ps1` creado y probado
- [ ] `verificar-db.ps1` creado y probado

---

## 16. Mejoras Futuras (Opcional)

### 16.1 Configurar HTTPS con Let's Encrypt

```bash
# Instalar Certbot
sudo apt install certbot python3-certbot-nginx

# Obtener certificado
sudo certbot --nginx -d tudominio.com

# Renovación automática
sudo certbot renew --dry-run
```

### 16.2 Configurar Nginx como Reverse Proxy

```nginx
# /etc/nginx/sites-available/tuapp
server {
    listen 80;
    server_name tudominio.com;

    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 16.3 Configurar Monitoreo

```yaml
# Agregar a docker-compose.yml
services:
  prometheus:
    image: prom/prometheus
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
    ports:
      - "9090:9090"

  grafana:
    image: grafana/grafana
    ports:
      - "3000:3000"
```

### 16.4 Configurar Backups Automáticos

```bash
# Crear script de backup
nano /opt/scripts/backup.sh

#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
docker exec tuapp_postgres pg_dump -U tuapp_user tuapp_db > /opt/backups/backup_$DATE.sql

# Agregar a crontab
crontab -e
# Backup diario a las 2 AM
0 2 * * * /opt/scripts/backup.sh
```

---

## 17. Notas Finales

### 🎯 Resumen de Configuraciones Clave

1. **DataProtection con Volumen Persistente** → Evita errores 400
2. **ForwardedHeaders** → Cookies funcionan correctamente
3. **SameSiteMode.Lax + SameAsRequest** → Compatibilidad HTTP
4. **Healthcheck en PostgreSQL** → Espera antes de iniciar web
5. **Volúmenes para datos críticos** → Persistencia entre reinicios

### ⚠️ Errores Comunes a Evitar

1. ❌ NO usar `localhost` para conectar a PostgreSQL → Usar `postgres`
2. ❌ NO usar `UseHttpsRedirection()` sin HTTPS → Comentar
3. ❌ NO olvidar `UseForwardedHeaders()` → Llamar PRIMERO
4. ❌ NO usar `docker compose down -v` → Borra volúmenes
5. ❌ NO subir `.env` a Git → Agregar a `.gitignore`

### 📝 Lista de Comandos Útiles

```bash
# Ver logs
docker logs tuapp_web --tail 100

# Reiniciar
docker compose restart

# Rebuild sin cache
docker compose build --no-cache

# Ver contenedores
docker ps

# Ver volúmenes
docker volume ls

# Entrar al contenedor
docker exec -it tuapp_web bash

# Ver recursos
docker stats
```

---

## 18. Despliegue de Múltiples Aplicaciones en un Solo VPS

### 18.1 Introducción

Esta guía ha sido diseñada para **una instalación limpia** de una aplicación ASP.NET Core. Sin embargo, en escenarios reales, es común necesitar **múltiples aplicaciones en el mismo servidor**.

**Escenarios comunes:**
- Varias aplicaciones ASP.NET Core diferentes
- Entornos staging + production en el mismo VPS
- Múltiples clientes con aplicaciones similares
- Microservicios o aplicaciones relacionadas

**✅ Buenas noticias:** Esta guía **SÍ permite múltiples instalaciones**, con ajustes estructurales mínimos para evitar conflictos.

---

### 18.2 Conflictos Potenciales (Si No Se Ajusta)

Si intentas usar esta guía tal cual para una segunda aplicación, tendrás **conflictos** en:

| Elemento | Conflicto | Impacto |
|----------|-----------|---------|
| **Puertos** | Todas usan `80:8080` | Solo una puede usar puerto 80 |
| **Volúmenes** | Nombres genéricos (`postgres_data`) | Datos se mezclan entre apps |
| **Contenedores** | Nombres genéricos (`tuapp_web`) | Docker no permite duplicados |
| **Cookies** | Mismo nombre (`.TuAplicacion.Auth`) | Sesiones cruzadas entre apps |
| **Redes** | Mismo nombre (`tuapp_net`) | Posible comunicación no deseada |

**👉 Solución:** Usar **prefijos únicos** y **puertos distintos** para cada aplicación.

---

### 18.3 Estructura Recomendada en el VPS

```
/opt/apps/
├── carnetqr/
│   ├── docker-compose.yml
│   ├── Dockerfile
│   ├── .env
│   ├── .git/
│   ├── Com/
│   │   ├── deploy-docker.ps1
│   │   ├── rebuild-deploy.ps1
│   │   └── verificar-db.ps1
│   └── [código fuente de CarnetQR]
│
├── inventario/
│   ├── docker-compose.yml
│   ├── Dockerfile
│   ├── .env
│   ├── .git/
│   ├── Com/
│   │   ├── deploy-docker.ps1
│   │   ├── rebuild-deploy.ps1
│   │   └── verificar-db.ps1
│   └── [código fuente de Inventario]
│
└── crm/
    ├── docker-compose.yml
    ├── Dockerfile
    ├── .env
    ├── .git/
    └── [código fuente de CRM]
```

**✅ Ventajas:**
- Cada aplicación completamente aislada
- Fácil identificación
- Backups independientes
- Rollback sin afectar otras apps
- Permisos granulares

---

### 18.4 Ajuste #1: Nombres Únicos (OBLIGATORIO)

#### docker-compose.yml - Aplicación 1 (CarnetQR)

```yaml
services:
  postgres:
    image: postgres:15
    container_name: carnetqr_postgres  # ⚠️ Prefijo único
    restart: always
    env_file:
      - .env
    volumes:
      - carnetqr_postgres_data:/var/lib/postgresql/data  # ⚠️ Prefijo único
    ports:
      - "5432:5432"  # ⚠️ Puerto único (o no exponer)
    networks:
      - carnetqr_net  # ⚠️ Prefijo único
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 10s
      timeout: 5s
      retries: 5

  web:
    build: .
    container_name: carnetqr_web  # ⚠️ Prefijo único
    restart: always
    depends_on:
      postgres:
        condition: service_healthy
    env_file:
      - .env
    environment:
      ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      ASPNETCORE_URLS: http://+:8080
      ASPNETCORE_DATAPROTECTION_PATH: /app/dataprotection-keys
    volumes:
      - carnetqr_dataprotection_keys:/app/dataprotection-keys  # ⚠️ Prefijo único
      - carnetqr_uploads:/app/wwwroot/uploads  # ⚠️ Prefijo único
    ports:
      - "8081:8080"  # ⚠️ Puerto único (8081, no 80)
    networks:
      - carnetqr_net  # ⚠️ Prefijo único

volumes:
  carnetqr_postgres_data:  # ⚠️ Prefijo único
  carnetqr_dataprotection_keys:  # ⚠️ Prefijo único
  carnetqr_uploads:  # ⚠️ Prefijo único

networks:
  carnetqr_net:  # ⚠️ Prefijo único
```

#### docker-compose.yml - Aplicación 2 (Inventario)

```yaml
services:
  postgres:
    image: postgres:15
    container_name: inventario_postgres  # ⚠️ Prefijo diferente
    restart: always
    env_file:
      - .env
    volumes:
      - inventario_postgres_data:/var/lib/postgresql/data  # ⚠️ Prefijo diferente
    ports:
      - "5433:5432"  # ⚠️ Puerto diferente (5433, no 5432)
    networks:
      - inventario_net  # ⚠️ Prefijo diferente
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 10s
      timeout: 5s
      retries: 5

  web:
    build: .
    container_name: inventario_web  # ⚠️ Prefijo diferente
    restart: always
    depends_on:
      postgres:
        condition: service_healthy
    env_file:
      - .env
    environment:
      ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      ASPNETCORE_URLS: http://+:8080
      ASPNETCORE_DATAPROTECTION_PATH: /app/dataprotection-keys
    volumes:
      - inventario_dataprotection_keys:/app/dataprotection-keys  # ⚠️ Prefijo diferente
      - inventario_uploads:/app/wwwroot/uploads  # ⚠️ Prefijo diferente
    ports:
      - "8082:8080"  # ⚠️ Puerto diferente (8082)
    networks:
      - inventario_net  # ⚠️ Prefijo diferente

volumes:
  inventario_postgres_data:  # ⚠️ Prefijo diferente
  inventario_dataprotection_keys:  # ⚠️ Prefijo diferente
  inventario_uploads:  # ⚠️ Prefijo diferente

networks:
  inventario_net:  # ⚠️ Prefijo diferente
```

**📌 Regla de Oro:**
> **TODO debe llevar prefijo del nombre del proyecto** (contenedores, volúmenes, redes)

---

### 18.5 Ajuste #2: Puertos Distintos

#### Opción A: Puertos Directos (Simple, para pruebas)

**Mapeo de puertos:**

| Aplicación | Puerto Host | Puerto Contenedor | URL |
|------------|-------------|-------------------|-----|
| CarnetQR | 8081 | 8080 | `http://164.68.99.83:8081` |
| Inventario | 8082 | 8080 | `http://164.68.99.83:8082` |
| CRM | 8083 | 8080 | `http://164.68.99.83:8083` |

**Configuración en docker-compose.yml:**

```yaml
# CarnetQR
ports:
  - "8081:8080"

# Inventario
ports:
  - "8082:8080"

# CRM
ports:
  - "8083:8080"
```

**✅ Ventajas:**
- Configuración simple
- No requiere componentes adicionales
- Ideal para desarrollo/staging

**❌ Desventajas:**
- URLs poco amigables (con puerto)
- No hay HTTPS fácil
- No recomendado para producción

---

#### Opción B: Reverse Proxy con Nginx (Recomendado para Producción)

**Arquitectura:**

```
Internet
    ↓
Nginx (puerto 80/443)
    ├─→ carnetqr.ejemplo.com → localhost:8081 (CarnetQR)
    ├─→ inventario.ejemplo.com → localhost:8082 (Inventario)
    └─→ crm.ejemplo.com → localhost:8083 (CRM)
```

**1. Instalar Nginx:**

```bash
sudo apt update
sudo apt install nginx -y
```

**2. Configurar sitio para CarnetQR:**

```bash
sudo nano /etc/nginx/sites-available/carnetqr
```

**Contenido:**

```nginx
server {
    listen 80;
    server_name carnetqr.ejemplo.com;

    location / {
        proxy_pass http://localhost:8081;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

**3. Habilitar el sitio:**

```bash
sudo ln -s /etc/nginx/sites-available/carnetqr /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

**4. Repetir para otras aplicaciones:**

```bash
# Inventario
sudo nano /etc/nginx/sites-available/inventario
# (cambiar server_name y proxy_pass a :8082)

# CRM
sudo nano /etc/nginx/sites-available/crm
# (cambiar server_name y proxy_pass a :8083)

# Habilitar
sudo ln -s /etc/nginx/sites-available/inventario /etc/nginx/sites-enabled/
sudo ln -s /etc/nginx/sites-available/crm /etc/nginx/sites-enabled/
sudo systemctl reload nginx
```

**5. Configurar DNS:**

En tu proveedor de dominios (GoDaddy, Namecheap, etc.):

| Tipo | Nombre | Valor |
|------|--------|-------|
| A | carnetqr | 164.68.99.83 |
| A | inventario | 164.68.99.83 |
| A | crm | 164.68.99.83 |

**6. Configurar HTTPS con Let's Encrypt:**

```bash
sudo apt install certbot python3-certbot-nginx -y

sudo certbot --nginx -d carnetqr.ejemplo.com
sudo certbot --nginx -d inventario.ejemplo.com
sudo certbot --nginx -d crm.ejemplo.com
```

**✅ Ventajas:**
- URLs amigables sin puertos
- HTTPS automático con Let's Encrypt
- Un solo punto de entrada
- Logs centralizados
- Configuración profesional

---

### 18.6 Ajuste #3: Cookies Únicas (MUY IMPORTANTE)

**Problema:** Si dos aplicaciones usan el mismo nombre de cookie, pueden ocurrir:
- Sesiones cruzadas (login en una app afecta a otra)
- Errores 400 al cambiar entre apps
- Logout inesperado

**Solución:** Nombre de cookie único por aplicación.

#### Program.cs - CarnetQR

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    
    // ⚠️ IMPORTANTE: Nombre único por aplicación
    options.Cookie.Name = ".CarnetQR.Auth";
    options.Cookie.HttpOnly = true;
});
```

#### Program.cs - Inventario

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    // ... misma configuración
    
    // ⚠️ Nombre diferente
    options.Cookie.Name = ".Inventario.Auth";
    options.Cookie.HttpOnly = true;
});
```

#### Program.cs - CRM

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    // ... misma configuración
    
    // ⚠️ Nombre diferente
    options.Cookie.Name = ".CRM.Auth";
    options.Cookie.HttpOnly = true;
});
```

**📌 Regla:**
> **Cada aplicación debe tener un nombre de cookie único** (usar el nombre del proyecto como prefijo)

---

### 18.7 Ajuste #4: DataProtection con Nombre de Aplicación

Además del volumen persistente, el **nombre de la aplicación** en DataProtection debe ser único:

#### Program.cs - CarnetQR

```csharp
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("CarnetQR")  // ⚠️ Único
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
```

#### Program.cs - Inventario

```csharp
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("Inventario")  // ⚠️ Único
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
```

**¿Por qué es importante?**
- Evita que las aplicaciones compartan claves de cifrado
- Previene errores de descifrado si los volúmenes se mezclan
- Mejora la seguridad

---

### 18.8 Script Maestro para Nueva Aplicación

Crear un script que automatice la creación de una nueva aplicación con configuración correcta:

#### create-new-app.sh

```bash
#!/bin/bash

# ============================================
# Script para crear nueva aplicación ASP.NET
# en el VPS con configuración multi-app
# ============================================

# Solicitar nombre de la aplicación
read -p "Nombre de la aplicación (ej: inventario): " APP_NAME
read -p "Puerto para la aplicación (ej: 8082): " APP_PORT
read -p "Puerto PostgreSQL (ej: 5433, o dejar vacío para no exponer): " PG_PORT
read -p "URL del repositorio Git: " GIT_URL

# Validar nombre
if [ -z "$APP_NAME" ]; then
    echo "Error: El nombre de la aplicación es obligatorio"
    exit 1
fi

# Crear directorio
APP_DIR="/opt/apps/$APP_NAME"
if [ -d "$APP_DIR" ]; then
    echo "Error: El directorio $APP_DIR ya existe"
    exit 1
fi

echo "Creando directorio $APP_DIR..."
sudo mkdir -p $APP_DIR
cd $APP_DIR

# Clonar repositorio
echo "Clonando repositorio..."
git clone $GIT_URL .

# Crear archivo .env
echo "Creando archivo .env..."
cat > .env << EOF
POSTGRES_DB=${APP_NAME}_db
POSTGRES_USER=${APP_NAME}_user
POSTGRES_PASSWORD=$(openssl rand -base64 20)
ASPNETCORE_ENVIRONMENT=Production
EOF

# Crear docker-compose.yml con nombres únicos
echo "Creando docker-compose.yml..."
PG_PORT_MAPPING=""
if [ ! -z "$PG_PORT" ]; then
    PG_PORT_MAPPING="ports:\n      - \"${PG_PORT}:5432\""
fi

cat > docker-compose.yml << EOF
services:
  postgres:
    image: postgres:15
    container_name: ${APP_NAME}_postgres
    restart: always
    env_file:
      - .env
    volumes:
      - ${APP_NAME}_postgres_data:/var/lib/postgresql/data
    ${PG_PORT_MAPPING}
    networks:
      - ${APP_NAME}_net
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U \${POSTGRES_USER}"]
      interval: 10s
      timeout: 5s
      retries: 5

  web:
    build: .
    container_name: ${APP_NAME}_web
    restart: always
    depends_on:
      postgres:
        condition: service_healthy
    env_file:
      - .env
    environment:
      ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=\${POSTGRES_DB};Username=\${POSTGRES_USER};Password=\${POSTGRES_PASSWORD}
      ASPNETCORE_URLS: http://+:8080
      ASPNETCORE_DATAPROTECTION_PATH: /app/dataprotection-keys
    volumes:
      - ${APP_NAME}_dataprotection_keys:/app/dataprotection-keys
      - ${APP_NAME}_uploads:/app/wwwroot/uploads
    ports:
      - "${APP_PORT}:8080"
    networks:
      - ${APP_NAME}_net

volumes:
  ${APP_NAME}_postgres_data:
  ${APP_NAME}_dataprotection_keys:
  ${APP_NAME}_uploads:

networks:
  ${APP_NAME}_net:
EOF

echo ""
echo "✅ Aplicación '$APP_NAME' creada exitosamente"
echo ""
echo "📂 Directorio: $APP_DIR"
echo "🔗 URL: http://IP_DEL_SERVIDOR:$APP_PORT"
echo ""
echo "📝 Próximos pasos:"
echo "1. Editar Program.cs para configurar:"
echo "   - Cookie.Name = \".$APP_NAME.Auth\""
echo "   - SetApplicationName(\"$APP_NAME\")"
echo ""
echo "2. Levantar contenedores:"
echo "   cd $APP_DIR"
echo "   docker compose up -d --build"
echo ""
echo "3. Ver logs:"
echo "   docker logs ${APP_NAME}_web --tail 50"
```

**Uso:**

```bash
chmod +x create-new-app.sh
sudo ./create-new-app.sh
```

---

### 18.9 Verificar Múltiples Aplicaciones

#### Ver todos los contenedores

```bash
docker ps

# Deberías ver algo como:
# carnetqr_web        Up 2 hours    0.0.0.0:8081->8080/tcp
# carnetqr_postgres   Up 2 hours    0.0.0.0:5432->5432/tcp
# inventario_web      Up 1 hour     0.0.0.0:8082->8080/tcp
# inventario_postgres Up 1 hour     0.0.0.0:5433->5432/tcp
# crm_web             Up 30 mins    0.0.0.0:8083->8080/tcp
# crm_postgres        Up 30 mins    5432/tcp
```

#### Ver todos los volúmenes

```bash
docker volume ls

# Deberías ver algo como:
# aspnet_carnetqr_postgres_data
# aspnet_carnetqr_dataprotection_keys
# aspnet_carnetqr_uploads
# aspnet_inventario_postgres_data
# aspnet_inventario_dataprotection_keys
# aspnet_inventario_uploads
```

#### Ver todas las redes

```bash
docker network ls

# Deberías ver algo como:
# aspnet_carnetqr_net
# aspnet_inventario_net
# aspnet_crm_net
```

#### Verificar puertos en uso

```bash
sudo netstat -tlnp | grep LISTEN

# O con ss:
sudo ss -tlnp | grep LISTEN

# Deberías ver:
# :8081  (CarnetQR web)
# :8082  (Inventario web)
# :8083  (CRM web)
# :5432  (CarnetQR postgres, si está expuesto)
# :5433  (Inventario postgres, si está expuesto)
```

---

### 18.10 Gestión Individual de Aplicaciones

#### Comandos específicos por aplicación

```bash
# CarnetQR
cd /opt/apps/carnetqr
docker compose restart        # Reiniciar
docker compose logs -f web   # Ver logs
docker compose down          # Detener
docker compose up -d --build # Rebuild

# Inventario
cd /opt/apps/inventario
docker compose restart
# ... etc

# CRM
cd /opt/apps/crm
docker compose restart
# ... etc
```

#### Backup por aplicación

```bash
# Backup de CarnetQR
cd /opt/apps/carnetqr
docker exec carnetqr_postgres pg_dump -U carnetqr_user carnetqr_db > backup_carnetqr_$(date +%Y%m%d).sql

# Backup de Inventario
cd /opt/apps/inventario
docker exec inventario_postgres pg_dump -U inventario_user inventario_db > backup_inventario_$(date +%Y%m%d).sql
```

#### Actualizar solo una aplicación

```bash
# Actualizar solo CarnetQR
cd /opt/apps/carnetqr
git pull
docker compose up -d --build

# Las otras aplicaciones NO se ven afectadas
```

---

### 18.11 Consideraciones de Recursos

#### Recursos por aplicación (aproximado)

| Recurso | PostgreSQL | ASP.NET Web | Total por App |
|---------|------------|-------------|---------------|
| RAM | 256-512 MB | 256-512 MB | 512 MB - 1 GB |
| CPU | 0.5 core | 0.5-1 core | 1-1.5 cores |
| Disco | 1-10 GB | 500 MB - 2 GB | 1.5 - 12 GB |

#### Recomendaciones por número de aplicaciones

| Aplicaciones | RAM Mínima | CPU Mínima | Disco Mínimo |
|--------------|------------|------------|--------------|
| 1-2 apps | 2 GB | 2 cores | 20 GB |
| 3-5 apps | 4 GB | 4 cores | 40 GB |
| 6-10 apps | 8 GB | 6 cores | 80 GB |

**⚠️ Importante:**
- Monitorear uso de recursos: `docker stats`
- Considerar swap si hay poca RAM
- Usar límites de recursos en docker-compose si es necesario

#### Limitar recursos por contenedor (opcional)

```yaml
services:
  web:
    # ... configuración normal
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 512M
        reservations:
          cpus: '0.5'
          memory: 256M
```

---

### 18.12 Monitoring y Logs Centralizados

#### Ver logs de todas las aplicaciones

```bash
# Script para ver logs de todas las apps
#!/bin/bash

echo "=== CarnetQR ==="
docker logs carnetqr_web --tail 10

echo ""
echo "=== Inventario ==="
docker logs inventario_web --tail 10

echo ""
echo "=== CRM ==="
docker logs crm_web --tail 10
```

#### Portainer para gestión visual

```bash
# Instalar Portainer (opcional)
docker volume create portainer_data

docker run -d -p 9000:9000 \
  --name portainer \
  --restart always \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -v portainer_data:/data \
  portainer/portainer-ce

# Acceder a: http://IP_DEL_SERVIDOR:9000
```

---

### 18.13 Checklist para Nueva Aplicación

Al agregar una nueva aplicación, verificar:

- [ ] **Directorio único** en `/opt/apps/NOMBRE_APP`
- [ ] **Nombres de contenedores únicos** con prefijo `NOMBRE_APP_`
- [ ] **Nombres de volúmenes únicos** con prefijo `NOMBRE_APP_`
- [ ] **Nombre de red único** con prefijo `NOMBRE_APP_`
- [ ] **Puerto único** (8081, 8082, 8083, etc.)
- [ ] **Puerto PostgreSQL único** (si se expone: 5432, 5433, 5434, etc.)
- [ ] **Cookie.Name único** en Program.cs: `".NOMBRE_APP.Auth"`
- [ ] **SetApplicationName único** en DataProtection
- [ ] **Archivo .env independiente** con credenciales únicas
- [ ] **Script de despliegue adaptado** con nombres correctos
- [ ] **Nginx configurado** (si usas reverse proxy)
- [ ] **DNS configurado** (si usas dominios)

---

### 18.14 Resumen de Buenas Prácticas Multi-App

#### ✅ HACER:

1. **Usar prefijos consistentes** en todo (contenedores, volúmenes, redes)
2. **Puertos únicos** para cada aplicación
3. **Cookies únicas** con nombre del proyecto
4. **DataProtection con nombre único**
5. **Estructura de directorios organizada** (`/opt/apps/NOMBRE`)
6. **Reverse proxy** para producción (Nginx)
7. **Backups independientes** por aplicación
8. **Monitoreo de recursos** con `docker stats`
9. **Documentar** qué puerto usa cada app
10. **Scripts de despliegue** independientes

#### ❌ NO HACER:

1. ❌ Usar nombres genéricos (`web`, `postgres`, `db`)
2. ❌ Compartir volúmenes entre aplicaciones
3. ❌ Usar el mismo puerto para múltiples apps
4. ❌ Compartir redes Docker (sin justificación)
5. ❌ Cookies con el mismo nombre
6. ❌ Mezclar archivos de diferentes apps en un directorio
7. ❌ Usar `docker compose down -v` sin backup
8. ❌ Olvidar documentar la configuración
9. ❌ No monitorear recursos
10. ❌ Exponer PostgreSQL sin necesidad

---

### 18.15 Migración de Aplicación Única a Multi-App

Si ya tienes una aplicación corriendo y quieres agregar otra:

#### Paso 1: Renombrar aplicación existente

```bash
# Detener aplicación actual
cd /opt/apps/aspnet
docker compose down

# Crear nuevo directorio con nombre específico
sudo mkdir -p /opt/apps/carnetqr
sudo mv /opt/apps/aspnet/* /opt/apps/carnetqr/
sudo rmdir /opt/apps/aspnet

# Actualizar docker-compose.yml con prefijos
cd /opt/apps/carnetqr
# Editar docker-compose.yml (agregar prefijos "carnetqr_")

# Actualizar Program.cs
# - Cookie.Name = ".CarnetQR.Auth"
# - SetApplicationName("CarnetQR")

# Levantar con nueva configuración
docker compose up -d --build
```

#### Paso 2: Agregar segunda aplicación

```bash
# Crear nueva aplicación con el script maestro
sudo ./create-new-app.sh

# O manualmente siguiendo la guía completa
```

---

## 🎯 Conclusión del Capítulo 18

### ✅ Evaluación de la Guía para Multi-App

| Criterio | Estado | Notas |
|----------|--------|-------|
| **Repetible** | ✅ | Plantilla reutilizable para N aplicaciones |
| **Escalable** | ✅ | Estructura soporta crecimiento |
| **Aislamiento** | ✅ | Cada app independiente |
| **Seguridad** | ✅ | Cookies y DataProtection únicos |
| **Mantenible** | ✅ | Updates sin afectar otras apps |
| **Profesional** | ✅ | Arquitectura nivel producción |

### 📊 Comparación: Antes vs Después

| Aspecto | Antes (Guía Original) | Después (Con Cap. 18) |
|---------|----------------------|----------------------|
| Apps soportadas | 1 | Ilimitadas |
| Conflictos | Posibles | Ninguno |
| Escalabilidad | Limitada | Total |
| Complejidad | Baja | Media |
| Producción real | Sí (1 app) | Sí (Multi-app) |

### 🚀 Nivel Alcanzado

Con esta guía completa (capítulos 1-18), ahora tienes:

✅ **Nivel Básico:** Despliegue de una aplicación ASP.NET Core  
✅ **Nivel Intermedio:** Multi-app en un VPS  
✅ **Nivel Avanzado:** Arquitectura escalable con Nginx  
✅ **Nivel Profesional:** Scripts de automatización  
✅ **Nivel Empresa:** Gestión de múltiples proyectos  

**👉 Este es el estándar oficial de despliegue para tu equipo.**

---

**Fecha de Creación:** 17 de Enero, 2026  
**Versión:** 2.0 - Multi-App Edition  
**Aplicación de Referencia:** CarnetQR Platform  
**Autor:** Documentación Generada para Despliegues Futuros

---

## 📚 Referencias

- [Documentación oficial de Docker](https://docs.docker.com/)
- [Documentación oficial de Docker Compose](https://docs.docker.com/compose/)
- [ASP.NET Core en Docker](https://docs.microsoft.com/es-es/aspnet/core/host-and-deploy/docker/)
- [DataProtection en ASP.NET Core](https://docs.microsoft.com/es-es/aspnet/core/security/data-protection/)
- [PostgreSQL Docker](https://hub.docker.com/_/postgres)
- [Nginx como Reverse Proxy](https://docs.nginx.com/nginx/admin-guide/web-server/reverse-proxy/)
- [Let's Encrypt y Certbot](https://certbot.eff.org/)