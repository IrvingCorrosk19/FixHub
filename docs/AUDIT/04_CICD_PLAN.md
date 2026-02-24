# FixHub — Plan DevSecOps y CI/CD (Fase 4)

**Branch:** `audit/fixhub-100`  
**Entregable:** Pipeline como código + recomendaciones de gobernanza.

---

## 1. Pipeline implementado

**Archivo:** `.github/workflows/ci.yml`

### 1.1 Jobs

| Job | Descripción | Herramienta |
|-----|-------------|-------------|
| **build-and-test** | Restore, build Release, tests de integración (Testcontainers) | dotnet build, dotnet test |
| **codeql** | Análisis estático de seguridad (SAST) | GitHub CodeQL (C#) |
| **dependency-scan** | Listado de paquetes vulnerables | dotnet list package --vulnerable |
| **secrets-scan** | Detección de secretos en repo | Gitleaks (gitleaks-action) |
| **container-scan** | Escaneo de imagen Docker API | Trivy (trivy-action), salida SARIF |

### 1.2 Disparadores

- Push y Pull Request a ramas: `main`, `master`, `audit/fixhub-100`.

### 1.3 Secretos

- **No se almacenan secretos en el workflow.**  
- Uso de `secrets.GITHUB_TOKEN` (automático) para upload de SARIF y Gitleaks.  
- Para despliegues futuros: usar GitHub Secrets (ej. `AZURE_CREDENTIALS`, `SSH_PRIVATE_KEY`) o variables de entorno del repositorio; nunca literales en YAML.

### 1.4 Notas de ejecución

- **Testcontainers:** Requiere Docker en el runner; GitHub-hosted runners (ubuntu-latest) lo incluyen.
- **CodeQL (C#):** Puede ejecutarse sin build en repos nuevos; si falla init, añadir paso de build antes de CodeQL.
- **Gitleaks:** En cuentas enterprise puede ser necesario `GITLEAKS_LICENSE` en Secrets.
- **Trivy:** Construye la imagen localmente sin push; el upload de SARIF es opcional (continue-on-error: true si falla).

---

## 2. Reglas de repositorio recomendadas (documentadas)

### 2.1 Branch protection

- **Ramas:** `main` (o `master`) como rama por defecto protegida.
- **Recomendaciones:**
  - Require pull request antes de merge (al menos 1 aprobación si hay varios mantenedores).
  - Require status checks: `Build & Test` (y opcionalmente `CodeQL`, `dependency-scan`, `secrets-scan`, `Container scan`) deben pasar.
  - No permitir force push ni borrado de la rama.
  - Restringir quién puede push (opcional): solo mantenedores o equipos definidos.

### 2.2 Convenciones de versionado y tags

- **Versionado semántico:** Recomendado para releases (ej. v1.0.0, v1.1.0). Definir en `Directory.Build.props` o en el .csproj de la API.
- **Tags:** Usar tags anotados para releases: `git tag -a v1.0.0 -m "Release 1.0.0"`. Los pipelines de release pueden dispararse por tag (ej. `v*`).
- **Changelog:** Mantener CHANGELOG.md o usar GitHub Releases para documentar cambios por versión.

### 2.3 Secretos y variables

- **Secretos (GitHub Secrets):** Para despliegue (SSH, Azure, etc.), JWT secret de prod nunca en repo.
- **Variables (GitHub Variables):** Para valores no sensibles (ej. nombre del entorno, URL base de QA).
- **Rotación:** Si Gitleaks o auditoría detectan credenciales expuestas: rotar inmediatamente y corregir el código (user secrets, env, secret store).

---

## 3. Separación de ambientes

| Ambiente | Uso | Configuración |
|----------|-----|----------------|
| **Development** | Local / dev | User Secrets, appsettings.Development.json (no versionado con valores reales). |
| **SIT / QA** | Integración / pruebas | Variables y secretos en GitHub (o en el runner de deploy). Connection string y JWT para BD de pruebas. |
| **Production** | Producción | Secretos en gestor seguro (GitHub Secrets, Azure Key Vault, etc.). Pipeline de deploy con aprobación manual o desde rama/tag protegido. |

No ejecutar el mismo pipeline con los mismos secretos contra PROD y no-PROD; usar secretos distintos por ambiente.

---

## 4. Próximos pasos (opcional)

- Añadir job de **deploy** (ej. a Azure Web App o a VPS vía SSH) disparado por tag o por rama `release/*`, usando secretos del repo.
- Integrar **Dependabot** (o Renovate) para actualizaciones de dependencias.
- Añadir **slack/email** en fallo de CI (webhooks o notificaciones de GitHub).

---

## 5. Entregables Fase 4

| Entregable | Ubicación |
|------------|-----------|
| Pipeline CI | `.github/workflows/ci.yml` |
| Este plan | `docs/AUDIT/04_CICD_PLAN.md` |
