# FixHub — Suite E2E de auditoría (AUDIT)

Suite de pruebas funcionales E2E por API para auditoría production-grade.  
Datos de prueba usan prefijo **AUDIT_** cuando se documenta en `01_FUNCTIONAL_TESTS.md`.

## Requisitos

- **Entorno:** Local con stack levantado (docker-compose o API + BD por separado).
- **API base URL:** 
  - Con docker-compose (acceso desde host a Web): `http://localhost:8084` (Web redirige a API interna) **o** exponer API en puerto host (ej. 5100) y usar `http://localhost:5100`.
  - Para Postman/scripts contra API directo: `http://localhost:5100` (desarrollo) o la URL donde esté escuchando la API.

## Opción A: Postman / Newman

1. Importar en Postman:
   - `FixHub-AUDIT-E2E.postman_collection.json`
   - `FixHub-AUDIT-E2E.postman_environment.json`
2. En el environment, setear `baseUrl` (ej. `http://localhost:5100`).
3. Ejecutar la colección (Runner o Newman):
   ```bash
   newman run FixHub-AUDIT-E2E.postman_collection.json -e FixHub-AUDIT-E2E.postman_environment.json
   ```
4. Las requests que requieren token usan la variable `token` (seteada por el script de pre-request o por un request previo de login guardado en variables).

## Opción B: Tests de integración existentes (Testcontainers)

Los tests en `FixHub.IntegrationTests` ya cubren flujos E2E contra una API levantada con WebApplicationFactory + Testcontainers (PostgreSQL efímero). No usan docker-compose ni prefijo AUDIT_; son repetibles y aislados.

```bash
cd c:\Proyectos\FixHub
dotnet test tests/FixHub.IntegrationTests/FixHub.IntegrationTests.csproj
```

## Limpieza de datos

- **Testcontainers:** La BD se destruye al finalizar; no hay persistencia.
- **docker-compose local:** Si se crean datos con prefijo AUDIT_, limpiar solo en BD de pruebas (no PROD). Ejemplo seguro: usar una base de datos dedicada para auditoría que se pueda dropear o truncar (tablas de auditoría documentadas en 01_FUNCTIONAL_TESTS.md). **No ejecutar deletes destructivos en datos reales.**

## Referencia

- Procedimiento detallado y resultados esperados: `docs/AUDIT/01_FUNCTIONAL_TESTS.md`.
