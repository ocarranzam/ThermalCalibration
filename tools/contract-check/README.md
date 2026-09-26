# Auditoría del contrato OpenAPI

Contrasta las respuestas **reales** de la API con [docs/api/thermal-v1.yaml](../../docs/api/thermal-v1.yaml). Para cada solicitud comprueba:

- que el código de estado esté documentado en la operación y sea el esperado;
- que el `Content-Type` sea el documentado;
- que el cuerpo cumpla el schema, validado con Ajv;
- que estén las cabeceras declaradas (`ETag`, `Location`).

Es el paso 7 de la definición de terminado de [implementation-plan.md](../../docs/implementation-plan.md#4-definición-de-terminado-por-entidad). El resultado se registra en [validation.md](../../docs/validation.md).

## Uso

```bash
# 1. API en ejecución (README §1, opción A)
docker compose up -d

# 2. Tokens de desarrollo (con las tres audiencias)
AUD="--audience https://localhost:5001 --audience http://localhost:5000 --audience http://localhost:8080"
export ADMIN_TOKEN=$(dotnet user-jwts create --project src/Thermal.Api --role Admin $AUD --output token)
export TECHNICIAN_TOKEN=$(dotnet user-jwts create --project src/Thermal.Api --role Technician $AUD --output token)

# 3. Auditoría
cd tools/contract-check && npm install && node contract-check.mjs http://localhost:8080
```

Termina con código 0 si todos los casos son conformes y con 1 si hay alguna desviación. Crea datos de prueba en la base del compose (tipos con nombre `Auditoria …`).

## Añadir una entidad

1. Crear `cases/<recurso>.mjs`, con la forma de [cases/equipment-types.mjs](cases/equipment-types.mjs): cada llamada `check(etiqueta, solicitud, estadoEsperado)` declara el método, la ruta real, la ruta del contrato (`operation`), el token y el cuerpo.
2. Importarlo y añadirlo a `SUITES` en [contract-check.mjs](contract-check.mjs).
3. Cubrir al menos: el caso válido, cada 4xx documentado, y las cabeceras de concurrencia si el recurso es editable.
