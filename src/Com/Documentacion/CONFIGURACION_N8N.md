# 🚀 Configuración de n8n en Servidor Multi-Aplicación

## 📋 Resumen de Aplicaciones en el Servidor

| Aplicación | Puerto Web | Puerto PostgreSQL | Prefijo | Estado |
|------------|-----------|-------------------|---------|--------|
| **CarnetQR Platform** | 8081 | 5432 | `carnetqr_` | ✅ Activo |
| **PanamaTravelHub** | 8082 | 5433 | `panamatravelhub_` | ✅ Activo |
| **n8n** | **8083** | **5434** | **`n8n_`** | 🔄 Nuevo |

---

## 🎯 Configuración de n8n

### Puerto y Acceso
- **Puerto Web:** `8083:5678` (host:contenedor)
- **URL Acceso:** `http://164.68.99.83:8083`
- **Puerto PostgreSQL (opcional):** `5434:5432` (solo si necesitas acceso externo)

### Estructura de Directorios
```
/opt/apps/
├── carnetqr/              # CarnetQR Platform
├── panamatravelhub/       # PanamaTravelHub
└── n8n/                   # n8n (nuevo)
    ├── docker-compose.yml
    ├── .env
    └── [datos persistentes]
```

### Prefijos Únicos (OBLIGATORIO)
- **Contenedores:** `n8n_n8n`, `n8n_postgres`
- **Volúmenes:** `n8n_data`, `n8n_postgres_data`
- **Red:** `n8n_net`

---

## 🔐 Variables de Entorno (.env)

```env
# PostgreSQL
POSTGRES_USER=n8nuser
POSTGRES_PASSWORD=superpasswordsegura_n8n
POSTGRES_DB=n8n

# n8n Configuration
N8N_HOST=164.68.99.83
N8N_PROTOCOL=http
N8N_PORT=5678
WEBHOOK_URL=http://164.68.99.83:8083/

# Timezone
TZ=America/Panama
GENERIC_TIMEZONE=America/Panama

# Database (n8n)
DB_TYPE=postgresdb
DB_POSTGRESDB_HOST=postgres
DB_POSTGRESDB_PORT=5432
DB_POSTGRESDB_DATABASE=n8n
DB_POSTGRESDB_USER=n8nuser
DB_POSTGRESDB_PASSWORD=superpasswordsegura_n8n

# Security
N8N_ENCRYPTION_KEY=CHANGE_THIS_TO_A_STRONG_SECRET_KEY_MIN_32_CHARACTERS

# Executions
EXECUTIONS_DATA_PRUNE=true
EXECUTIONS_DATA_MAX_AGE=168
```

---

## 📦 Docker Compose

### Características
- ✅ PostgreSQL 15 para base de datos (producción-ready)
- ✅ Volúmenes persistentes para datos y workflows
- ✅ Healthchecks configurados
- ✅ Restart policy: always
- ✅ Red aislada con prefijo `n8n_`

### Recursos Estimados
- **RAM:** ~512 MB (n8n) + ~256 MB (PostgreSQL) = ~768 MB
- **CPU:** 0.5-1 core
- **Disco:** ~1-2 GB (inicial), crece con workflows

---

## 🛠️ Scripts de Despliegue

### 1. `Com/clonar-n8n.ps1`
Crea el directorio y prepara el entorno.

### 2. `Com/deploy-n8n.ps1`
Despliega n8n en el servidor.

### 3. `Com/verificar-n8n.ps1`
Verifica que n8n esté funcionando correctamente.

---

## ✅ Checklist de Despliegue

- [ ] Directorio `/opt/apps/n8n` creado
- [ ] `docker-compose.yml` con prefijos `n8n_`
- [ ] `.env` configurado con credenciales únicas
- [ ] Puerto `8083` disponible (no usado por otras apps)
- [ ] Puerto `5434` disponible (si se expone PostgreSQL)
- [ ] Volúmenes con nombres únicos (`n8n_*`)
- [ ] Red con nombre único (`n8n_net`)
- [ ] Firewall: puerto `8083` abierto (si es necesario)
- [ ] n8n accesible en `http://164.68.99.83:8083`
- [ ] Base de datos PostgreSQL funcionando

---

## 🔍 Verificación Post-Despliegue

```bash
# Ver contenedores
docker ps | grep n8n

# Ver logs
docker logs n8n_n8n

# Verificar acceso
curl http://localhost:8083/healthz
```

---

## 📝 Notas Importantes

1. **No afecta aplicaciones existentes:** Usa prefijos únicos y puertos distintos
2. **PostgreSQL:** Recomendado para producción (no SQLite)
3. **Encryption Key:** CAMBIAR en producción (`N8N_ENCRYPTION_KEY`)
4. **Backups:** Hacer backup de volúmenes `n8n_data` y `n8n_postgres_data`
5. **Firewall:** Abrir puerto 8083 si quieres acceso externo

---

## 🚨 Troubleshooting

### n8n no inicia
```bash
docker logs n8n_n8n
docker compose -f /opt/apps/n8n/docker-compose.yml logs
```

### Puerto ocupado
```bash
netstat -tulpn | grep 8083
```

### Base de datos no conecta
```bash
docker exec -it n8n_postgres psql -U n8nuser -d n8n
```

---

**Fecha de Creación:** 6 de Enero, 2026  
**Servidor:** 164.68.99.83  
**Versión n8n:** latest (stable)
