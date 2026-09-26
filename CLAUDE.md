# ThermalCalibration: guía para trabajar en este repositorio

Sistema de monitoreo térmico para calibración de equipos de refrigeración. Web API .NET 10, Clean Architecture + CQRS + DDD, SQL Server 2022. Documentación y mensajes al usuario en **español**; código y contrato en **inglés**.

## Dónde está cada cosa

- **Qué construir y en qué orden:** [docs/implementation-plan.md](docs/implementation-plan.md) (estado por entidad, olas, definición de terminado, guía paso a paso y lecciones aprendidas). Empezar siempre por aquí y actualizar su §1 al terminar una entidad.
- Especificación, que prevalece ante cualquier diferencia: [docs/specs/functional/](docs/specs/functional/). Historias en `03-user-stories.md` (IDs `HU-xx`, reglas `RN-xx`, escenarios de prueba `TD-xx`).
- Contrato: [docs/api/thermal-v1.yaml](docs/api/thermal-v1.yaml). Primero el contrato, después el código.
- Arquitectura: [ADR-001](docs/architecture/adr/ADR-001-clean-architecture-cqrs-ddd.md), [domain-model.md](docs/architecture/domain-model.md), [c4-containers.md](docs/architecture/c4-containers.md).
- Base de datos (base primero, sin migraciones de EF): [docs/db/01-schema.sql](docs/db/01-schema.sql) y [05-data-model.md](docs/specs/functional/05-data-model.md).
- Trazabilidad, cobertura de pruebas y auditorías: [docs/validation.md](docs/validation.md).
- Plantillas de la documentación de entrega: [docs/delivery/](docs/delivery/README.md).
- Registros reales de campo y su análisis (base de los perfiles de eficacia por modelo): [docs/data/](docs/data/README.md). Marca y modelo del equipo son **obligatorios**; un modelo deducido se marca como inferido.

## Convenciones obligatorias

- Nombres técnicos en inglés en todas las capas (`EquipmentType`, `CreateEquipmentTypeCommand`, `/api/v1/equipment-types`). Nunca `TipoEquipo` ni nombres en español en código, rutas o schemas.
- Rutas REST en plural, minúsculas y kebab-case, con el prefijo `/api/v1`. Controladores `[ApiController]` (no Minimal APIs).
- CQRS con `ICommandHandler`/`IQueryHandler` propios. **No usar MediatR** (licencia comercial). FluentAssertions fijado en **7.2.2** (la 8+ es comercial). Revisar la licencia de toda dependencia nueva.
- Dominio: constructor primario, setters privados con validación (`field`), `DomainValidationException(nameof(Propiedad), "mensaje igual al de la historia")`.
- Errores RFC 7807 con títulos en español. `ETag`/`If-Match` en recursos editables.
- **[PC-01]:** nunca usar los literales `120` ni `31`; derivar todo de `SamplingIntervalSeconds`.
- Alcance: equipos de hasta 2000 L (uso individual, pequeña y mediana escala, hospitales y clínicas); nada industrial. Hasta **27 canales** (`MeasurementPoints.MaxChannels`); puntos mínimos y criterio de límite (`Range` con mínimo y/o máximo, o `Band` con consigna ± tolerancia) **por tipo de equipo** (D-05, D-06, D-07); los límites del catálogo inicial son sugeridos (`IsLimitSuggested`).
- Fuentes públicas descargadas: fichas de fabricantes en [docs/equipment-catalog/](docs/equipment-catalog/README.md) y normas en [docs/standards/](docs/standards/README.md) (solo si la licencia lo permite, con su SHA-256). Los `.xlsx` de clientes en `docs/data/` **no se versionan**.
- Cada prueba lleva `[Trait("Story", "HU-xx")]` y el comentario `// HU-xx · Scenario: <nombre del escenario Gherkin>`.
- Archivos en UTF-8 con LF ([.editorconfig](.editorconfig)). Compilación con advertencias como errores.
- Diagramas Mermaid: sin `;` dentro de los mensajes (separa instrucciones) y sin literales derivados del intervalo (usar el nombre del parámetro).

## Comandos

```bash
dotnet build Thermal.slnx
dotnet format Thermal.slnx --verify-no-changes --severity info
dotnet test --solution Thermal.slnx                   # unitarias + integración (requiere Docker)
dotnet test --project tests/Thermal.UnitTests -- --filter-trait "Story=HU-02"
npx @redocly/cli lint docs/api/thermal-v1.yaml        # sin errores ni advertencias
node docs/diagrams/generate-features.mjs              # tras cambiar 03 o 06
node docs/diagrams/render-sequence-svg.mjs            # tras cambiar un diagrama de secuencia (valida y genera SVG)
node test-data/generate-test-data.mjs                 # tras cambiar el generador de escenarios
```

Tokens de desarrollo: siempre con las **tres audiencias** (si no, `user-jwts` reescribe `appsettings.Development.json`):

```bash
dotnet user-jwts create --project src/Thermal.Api --role Admin \
  --audience https://localhost:5001 --audience http://localhost:5000 --audience http://localhost:8080 --output token
```

Auditoría del contrato contra la API en Docker: [tools/contract-check/](tools/contract-check/README.md).

## Docker (la PC tiene poca memoria)

- El compose (`thermal`: SQL Server a 2 GB y API a 256 MB) se **conserva** entre sesiones. Tras usarlo: `SHUTDOWN` por sqlcmd y `docker compose stop`. No borrarlo salvo que dé problemas.
- Detener el compose **antes** de `dotnet test`: Testcontainers crea otro SQL Server (2 GB) y lo elimina al terminar.
- Comprobar con `docker ps -a` que no queden contenedores de pruebas. No tocar los contenedores ajenos al proyecto.
- En Git Bash, anteponer `MSYS_NO_PATHCONV=1` a `docker compose exec … /opt/...`.

## Al terminar una entidad

Seguir la definición de terminado de [implementation-plan.md §4](docs/implementation-plan.md#4-definición-de-terminado-por-entidad): contrato → dominio → aplicación → infraestructura → API → pruebas → verificación → documentación (validation.md, README, c4, domain-model, plan) → commit y push.
