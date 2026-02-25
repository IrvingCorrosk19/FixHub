# FixHub — Pruebas funcionales E2E por API

**Prefijo de datos:** `TEST_<timestamp>`  
**Entorno:** Solo local / SIT / QA. Prohibido producción.

## Requisitos

- API en ejecución (ej. `http://localhost:5100` o URL del entorno).
- Variables de entorno o Postman: `baseUrl`, y para Admin: `adminEmail`, `adminPassword` (usuario seed).

## Ejecución

### Postman

1. Importar `FixHub-Functional-E2E.postman_collection.json` y `FixHub-Functional-E2E.postman_environment.json`.
2. En el environment, setear `baseUrl` y (para Login Admin) `adminEmail`, `adminPassword`.
3. Ejecutar la colección en orden: carpeta 0 → 1 → 2 → 3 → 4 → 5 → 6. Las variables `customerToken`, `technicianToken`, `adminToken`, `jobId`, `proposalId` se rellenan por scripts.

### Newman

```bash
newman run tests/FUNCTIONAL_E2E/FixHub-Functional-E2E.postman_collection.json -e tests/FUNCTIONAL_E2E/FixHub-Functional-E2E.postman_environment.json
```

## Resultados y evidencia

Ver **docs/AUDIT/06_FUNCTIONAL_RESULTS.md** (cómo ejecutar, evidencia request/response, tabla Pass/Fail, bugs encontrados).
