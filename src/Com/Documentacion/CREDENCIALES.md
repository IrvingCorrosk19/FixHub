# 🔐 Contraseñas y Credenciales del Sistema

## 🖥️ SERVIDOR VPS

### SSH - Acceso al Servidor
- **IP:** `164.68.99.83`
- **Usuario:** `root`
- **Contraseña:** `DC26Y0U5ER6sWj`
- **Propósito:** Acceso remoto al servidor Ubuntu para administración

---

## 🐘 POSTGRESQL (Base de Datos)

### Configuración en .env
- **Base de Datos:** `carnetqrdb`
- **Usuario:** `carnetqruser`
- **Contraseña:** `superpasswordsegura`
- **Propósito:** Conexión de la aplicación ASP.NET Core a PostgreSQL

### Acceso desde pgAdmin o herramientas externas
- **Host:** `164.68.99.83`
- **Puerto:** `5432`
- **Base de Datos:** `carnetqrdb`
- **Usuario:** `carnetqruser`
- **Contraseña:** `superpasswordsegura`
- **Propósito:** Administración de base de datos con herramientas gráficas

---

## 👤 USUARIOS DE LA APLICACIÓN

### SuperAdmin (Administrador del Sistema)
- **Email:** `admin@qlservices.com`
- **Contraseña:** `Admin@123456`
- **Rol:** SuperAdmin
- **Propósito:** Acceso completo al sistema, puede ver y gestionar todas las instituciones

### InstitutionAdmin (Administrador de "Empresa Demo")
- **Email:** `admin@demo.com`
- **Contraseña:** `Admin@123456`
- **Rol:** InstitutionAdmin
- **Propósito:** Administrador de la institución "Empresa Demo", solo ve su institución

---

## 📝 USUARIOS DE PRUEBA (Creados durante testing)

### Usuario Staff
- **Email:** `staff@hospital.com`
- **Contraseña:** `Staff@123456`
- **Rol:** Staff
- **Institución:** Hospital San José
- **Propósito:** Probar funcionalidad de rol Staff

### Usuario AdministrativeOperator
- **Email:** `operador@hospital.com`
- **Contraseña:** `Operador@123456`
- **Rol:** AdministrativeOperator
- **Institución:** Hospital San José
- **Propósito:** Probar funcionalidad de rol operador administrativo

### Usuario Staff Demo
- **Email:** `staff@demo.com`
- **Contraseña:** `Staff@123456`
- **Rol:** Staff
- **Institución:** Empresa Demo
- **Propósito:** Probar funcionalidad de Staff en institución demo

---

## 🔑 RESUMEN RÁPIDO

| Servicio | Usuario/Email | Contraseña | Propósito |
|----------|---------------|------------|-----------|
| **SSH VPS** | root@164.68.99.83 | `DC26Y0U5ER6sWj` | Acceso al servidor |
| **PostgreSQL** | carnetqruser | `superpasswordsegura` | Base de datos |
| **SuperAdmin** | admin@qlservices.com | `Admin@123456` | Administrador total |
| **InstitutionAdmin** | admin@demo.com | `Admin@123456` | Admin de Empresa Demo |
| **Staff** | staff@hospital.com | `Staff@123456` | Personal Hospital San José |
| **Operador** | operador@hospital.com | `Operador@123456` | Operador Hospital San José |
| **Staff Demo** | staff@demo.com | `Staff@123456` | Personal Empresa Demo |

---

## ⚠️ NOTAS DE SEGURIDAD

### Contraseñas que DEBES cambiar en producción real:
1. ✅ **SSH (root):** Cambiar inmediatamente
2. ✅ **PostgreSQL:** Usar contraseña más compleja
3. ✅ **SuperAdmin:** Cambiar después del primer login
4. ✅ **InstitutionAdmin:** Cambiar después del primer login

### Recomendaciones:
- **SSH:** Considerar usar autenticación por clave SSH en lugar de contraseña
- **PostgreSQL:** Generar contraseña de 20+ caracteres con símbolos
- **Usuarios:** Implementar cambio de contraseña obligatorio en primer login
- **Backup:** Guardar credenciales en gestor de contraseñas (LastPass, 1Password, Bitwarden)

---

**⚠️ IMPORTANTE:** Este archivo contiene información sensible. NO subir a Git/GitHub.

**Fecha de Creación:** 17 de Enero, 2026  
**Sistema:** CarnetQR Platform  
**Servidor:** 164.68.99.83
