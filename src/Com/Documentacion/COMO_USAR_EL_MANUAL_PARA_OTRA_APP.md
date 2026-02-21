# 📘 Cómo Usar el Manual de Despliegue para Otra Aplicación

## ✅ Respuesta Corta

**SÍ, puedes usar el manual para desplegar cualquier aplicación ASP.NET Core**, pero necesitas hacer **5 ajustes simples** antes de comenzar.

---

## 🔄 Ajustes Necesarios (5 minutos)

### 1️⃣ Identificar la Estructura de tu Proyecto

**En el manual busca:** Sección 4.1 - "Identificar la Estructura del Proyecto"

**Reemplaza:**
- `TuProyecto.sln` → **Tu nombre real de solución** (ej: `Inventario.sln`)
- `TuProyecto.Web/` → **Tu proyecto web** (ej: `Inventario.Web/`)
- `TuProyecto.Application/` → **Tu proyecto Application** (si existe)
- `TuProyecto.Domain/` → **Tu proyecto Domain** (si existe)
- `TuProyecto.Infrastructure/` → **Tu proyecto Infrastructure** (si existe)

**Ejemplo:**
```dockerfile
# ANTES (del manual)
COPY TuProyecto.sln .
COPY TuProyecto.Web/ TuProyecto.Web/

# DESPUÉS (tu aplicación)
COPY Inventario.sln .
COPY Inventario.Web/ Inventario.Web/
COPY Inventario.Application/ Inventario.Application/
```

---

### 2️⃣ Cambiar Nombres en docker-compose.yml

**En el manual busca:** Sección 5.1 - "Crear docker-compose.yml"

**Reemplaza TODAS las ocurrencias de:**
- `tuapp_postgres` → `NOMBRE_APP_postgres`
- `tuapp_web` → `NOMBRE_APP_web`
- `tuapp_net` → `NOMBRE_APP_net`
- `postgres_data` → `NOMBRE_APP_postgres_data`
- `dataprotection_keys` → `NOMBRE_APP_dataprotection_keys`
- `uploads` → `NOMBRE_APP_uploads`

**Ejemplo:**
```yaml
# ANTES (del manual)
container_name: tuapp_postgres
volumes:
  - postgres_data:/var/lib/postgresql/data

# DESPUÉS (tu aplicación - ej: "inventario")
container_name: inventario_postgres
volumes:
  - inventario_postgres_data:/var/lib/postgresql/data
```

**📌 Regla:** Usa el nombre de tu aplicación en minúsculas como prefijo.

---

### 3️⃣ Cambiar Nombre en Program.cs

**En el manual busca:** Sección 7.2 - "Configuraciones Obligatorias para Docker"

**Reemplaza:**
- `SetApplicationName("TuAplicacion")` → `SetApplicationName("TuNombreApp")`
- `Cookie.Name = ".TuAplicacion.Auth"` → `Cookie.Name = ".TuNombreApp.Auth"`

**Ejemplo:**
```csharp
// ANTES (del manual)
.SetApplicationName("TuAplicacion")
options.Cookie.Name = ".TuAplicacion.Auth";

// DESPUÉS (tu aplicación)
.SetApplicationName("Inventario")
options.Cookie.Name = ".Inventario.Auth";
```

---

### 4️⃣ Ajustar Scripts PowerShell (si los usas)

**En el manual busca:** Sección 10.2 - "Script: deploy-docker.ps1"

**Reemplaza:**
- `cd /opt/apps/aspnet` → `cd /opt/apps/NOMBRE_APP`
- `docker logs tuapp_web` → `docker logs NOMBRE_APP_web`
- `docker logs tuapp_postgres` → `docker logs NOMBRE_APP_postgres`

**Ejemplo:**
```powershell
# ANTES (del manual)
$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "cd /opt/apps/aspnet && git pull" 2>&1
$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "docker logs tuapp_web --tail 30" 2>&1

# DESPUÉS (tu aplicación)
$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "cd /opt/apps/inventario && git pull" 2>&1
$result = & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname "docker logs inventario_web --tail 30" 2>&1
```

---

### 5️⃣ Ajustar Variables de Entorno (.env)

**En el manual busca:** Sección 6.2 - "Contenido del Archivo .env"

**Reemplaza:**
- `POSTGRES_DB=tuapp_db` → `POSTGRES_DB=NOMBRE_APP_db`
- `POSTGRES_USER=tuapp_user` → `POSTGRES_USER=NOMBRE_APP_user`
- `POSTGRES_PASSWORD=TuPasswordSuperSegura123!` → **Tu contraseña real**

**Ejemplo:**
```env
# ANTES (del manual)
POSTGRES_DB=tuapp_db
POSTGRES_USER=tuapp_user
POSTGRES_PASSWORD=TuPasswordSuperSegura123!

# DESPUÉS (tu aplicación)
POSTGRES_DB=inventario_db
POSTGRES_USER=inventario_user
POSTGRES_PASSWORD=MiPasswordSegura2024!
```

---

## 📋 Checklist Rápido para Nueva Aplicación

Antes de comenzar, prepara esta información:

- [ ] **Nombre de la aplicación** (ej: `inventario`, `crm`, `facturacion`)
- [ ] **Nombre del archivo .sln** (ej: `Inventario.sln`)
- [ ] **Nombres de los proyectos** (ej: `Inventario.Web`, `Inventario.Application`)
- [ ] **Puerto que usarás** (ej: `8081`, `8082`, `8083`)
- [ ] **IP del servidor** (si es diferente a `164.68.99.83`)
- [ ] **Contraseña del servidor SSH** (si es diferente)
- [ ] **Contraseña para PostgreSQL** (genera una segura)

---

## 🎯 Proceso Paso a Paso

### Paso 1: Leer el Manual

1. Abre `GUIA_DESPLIEGUE_DOCKER_ASPNET.md`
2. Lee desde el **Capítulo 1** hasta el **Capítulo 11**
3. **NO ejecutes nada aún**, solo lee y entiende

### Paso 2: Preparar tu Proyecto

1. Asegúrate de que tu proyecto compila localmente
2. Verifica que tienes `Program.cs` configurado
3. Verifica que tienes `appsettings.json`

### Paso 3: Crear Archivos de Configuración

1. **Dockerfile** (Sección 4.2)
   - Copia el ejemplo
   - Reemplaza nombres de proyectos
   - Guárdalo en la raíz de tu proyecto

2. **docker-compose.yml** (Sección 5.1)
   - Copia el ejemplo
   - Reemplaza `tuapp_*` con `NOMBRE_APP_*`
   - Ajusta el puerto si es necesario
   - Guárdalo en la raíz

3. **.env** (Sección 6.2)
   - Copia el ejemplo
   - Cambia nombres de DB y usuario
   - Genera contraseña segura
   - **NO lo subas a Git**

4. **Program.cs** (Sección 7.2)
   - Agrega las configuraciones de DataProtection
   - Agrega ForwardedHeaders
   - Configura Cookies con nombre único
   - Ajusta `SetApplicationName`

### Paso 4: Seguir el Manual

1. **Capítulo 1-2:** Preparar servidor (solo primera vez)
2. **Capítulo 3:** Clonar repositorio en `/opt/apps/NOMBRE_APP`
3. **Capítulo 4-9:** Ya tienes los archivos, solo verifica
4. **Capítulo 10:** Ajusta scripts si los usas
5. **Capítulo 11:** Desplegar

### Paso 5: Verificar

1. **Capítulo 12:** Verificar que todo funciona
2. **Capítulo 13:** Si hay problemas, consulta solución

---

## 🔍 Búsqueda y Reemplazo Rápido

Si quieres hacerlo rápido, usa **Buscar y Reemplazar** en tu editor:

### En Dockerfile:
```
Buscar: TuProyecto
Reemplazar: TuNombreReal
```

### En docker-compose.yml:
```
Buscar: tuapp
Reemplazar: NOMBRE_APP (en minúsculas)
```

### En Program.cs:
```
Buscar: TuAplicacion
Reemplazar: TuNombreApp
```

### En Scripts PowerShell:
```
Buscar: /opt/apps/aspnet
Reemplazar: /opt/apps/NOMBRE_APP

Buscar: tuapp_web
Reemplazar: NOMBRE_APP_web

Buscar: tuapp_postgres
Reemplazar: NOMBRE_APP_postgres
```

---

## ⚠️ Elementos que NO Necesitas Cambiar

Estos elementos son genéricos y funcionan para cualquier aplicación:

✅ **Configuración de Docker** (instalación, comandos)  
✅ **Configuración de DataProtection** (código, no nombres)  
✅ **Configuración de ForwardedHeaders**  
✅ **Configuración de Cookies** (estructura, no nombres)  
✅ **Comandos de Docker** (`docker compose up`, `docker logs`, etc.)  
✅ **Estructura de volúmenes** (concepto, no nombres)  
✅ **Solución de problemas** (Capítulo 13)  
✅ **Mantenimiento** (Capítulo 14)  

---

## 📝 Ejemplo Completo: Aplicación "Inventario"

### 1. Dockerfile

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Inventario.sln .
COPY Inventario.Application/ Inventario.Application/
COPY Inventario.Domain/ Inventario.Domain/
COPY Inventario.Infrastructure/ Inventario.Infrastructure/
COPY Inventario.Web/ Inventario.Web/

RUN dotnet restore
RUN dotnet publish Inventario.Web/Inventario.Web.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Inventario.Web.dll"]
```

### 2. docker-compose.yml

```yaml
services:
  postgres:
    image: postgres:15
    container_name: inventario_postgres
    restart: always
    env_file:
      - .env
    volumes:
      - inventario_postgres_data:/var/lib/postgresql/data
    ports:
      - "5433:5432"  # Puerto diferente si hay otras apps
    networks:
      - inventario_net
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 10s
      timeout: 5s
      retries: 5

  web:
    build: .
    container_name: inventario_web
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
      - inventario_dataprotection_keys:/app/dataprotection-keys
      - inventario_uploads:/app/wwwroot/uploads
    ports:
      - "8082:8080"  # Puerto único
    networks:
      - inventario_net

volumes:
  inventario_postgres_data:
  inventario_dataprotection_keys:
  inventario_uploads:

networks:
  inventario_net:
```

### 3. .env

```env
POSTGRES_DB=inventario_db
POSTGRES_USER=inventario_user
POSTGRES_PASSWORD=Inventario2024!Seguro
ASPNETCORE_ENVIRONMENT=Production
```

### 4. Program.cs (solo las partes a agregar)

```csharp
// DataProtection
var dataProtectionPath = Environment.GetEnvironmentVariable("ASPNETCORE_DATAPROTECTION_PATH") 
    ?? "/app/dataprotection-keys";

if (!Directory.Exists(dataProtectionPath))
{
    Directory.CreateDirectory(dataProtectionPath);
}

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("Inventario")  // ⚠️ Nombre único
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// ForwardedHeaders
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ... otros servicios ...

// Cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.Name = ".Inventario.Auth";  // ⚠️ Nombre único
    options.Cookie.HttpOnly = true;
});

var app = builder.Build();

// ⚠️ PRIMERO: Forwarded Headers
app.UseForwardedHeaders();

// ... resto del pipeline ...
```

---

## ✅ Conclusión

**El manual es 95% reutilizable.** Solo necesitas:

1. ✅ Cambiar nombres de proyectos en Dockerfile
2. ✅ Cambiar prefijos en docker-compose.yml
3. ✅ Cambiar nombres en Program.cs
4. ✅ Ajustar scripts (si los usas)
5. ✅ Configurar .env con tus credenciales

**Tiempo estimado de adaptación:** 10-15 minutos

**Después de eso:** Puedes seguir el manual paso a paso sin más cambios.

---

## 🚀 ¿Quieres un Script Automático?

Si quieres, puedo crear un script que:
- Te pregunte el nombre de tu aplicación
- Te pregunte la estructura de proyectos
- Genere automáticamente el Dockerfile y docker-compose.yml
- Con todos los nombres correctos

**¿Te interesa?**

---

**Fecha de Creación:** 17 de Enero, 2026  
**Versión:** 1.0  
**Para:** Uso del Manual de Despliegue con Cualquier Aplicación ASP.NET Core
