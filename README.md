# Sistema de Monitoreo Térmico para Calibración de Equipos de Refrigeración

Captura, almacena y exporta a Excel las lecturas de temperatura de equipos de refrigeración (refrigeradoras, congeladoras, conservadoras, incubadoras) de empresas cliente. Los datos llegan de hasta 10 termopares tipo T o K, conectados a un adquisidor de bajo costo (Arduino o Raspberry Pi) por puerto serial.

**Fase 1:** Sesión de Medición, Adquisición Serial y Exportación a Excel. Solo datos crudos: el análisis estadístico, los certificados y el inventario de sensores quedan para fases futuras.

> Este README es un resumen. Ante cualquier diferencia, prevalece la especificación en [docs/specs/functional/](docs/specs/functional/).

---

## Datos principales

| Tema | Valor | Regla |
|---|---|---|
| Canales por sesión | 1 a 10 termopares, tipo T o K, cada uno con su ubicación | RN-01 |
| Mínimo de puntos de medición | **9** (8 esquinas y el centro, según IEC 60068-3-5 y DKD-R 5-7). Con menos se advierte y se exige confirmación | RN-20 |
| Intervalo de muestreo **[PC-01]** | **120 s**, una lectura por canal cada 2 min | RN-02 |
| Inicio de la sesión | Con la **primera muestra recibida** después de pulsar "Iniciar" (muestra 1, t = 0) | RN-02 |
| Duración de la sesión | Base de **1 h**. Mayor si lo exige el tipo de equipo o lo pide el cliente (con referencia), hasta varios días (máximo 7 días por defecto). Se cierra sola al cumplirse | RN-04 |
| Sesión completa | Cumplió la duración planificada **y** tiene 1 h de datos válidos (31 muestras con 120 s) | RN-03 |
| Descanso del kit de medición | **15 min** entre sesiones con el mismo adquisidor. Un supervisor puede autorizar un inicio anticipado | RN-17 |
| Límite máximo | Por tipo de equipo. Fuera de límite si la lectura es **estrictamente mayor**: con -5,0 °C, -5,0 cumple y -4,9 no. Se copia en la sesión al iniciar | RN-06 a RN-08 |
| Mezcla de termopares T y K | Se permite, con advertencia y confirmación | RN-05 |
| Muestra afectada | **Más del 60 %** de los canales sin lectura válida (6 de 10 no la afecta, 7 de 10 sí) | RN-14 |
| Pérdida de sensores | Advertencia en la 1.ª muestra afectada, **crítica en la 3.ª** seguida y **sesión fallida a los 30 min** seguidos. Un corte de comunicación de 30 min también hace fallar la sesión | RN-14, RN-15 |
| Fuera de límite sostenido | Un canal fuera de límite **30 min** seguidos genera una alerta crítica, con la causa probable: **sensor** (solo una minoría de canales fuera de límite) o **equipo** (la mayoría) | RN-19 |
| Reconexión con otro adquisidor u otro grupo de sensores | Sesión fallida | RN-15 |
| Alertas | **Críticas:** aviso destacado y reconocimiento obligatorio. **Advertencias:** solo se registran, no interrumpen | RN-18 |
| Estados de la sesión | Configurada → En curso → Completa / Incompleta / Fallida / Cancelada | 05 §2 |
| Excel exportado | Hojas Resumen, Lecturas, Alertas y Comunicación | HU-11 |

Todos los parámetros se guardan en la tabla `AppSetting`, los mantiene el administrador y se **copian en cada sesión al iniciarla**: cambiarlos no altera las sesiones ya registradas.

## Puntos de cambio previstos

Decisiones que el Product Owner puede cambiar a futuro. Cada una lleva una marca **`[PC-nn]`** en todos los lugares donde se define, para ubicarla con una sola búsqueda:

```bash
grep -rn "PC-01" docs test-data
```

### PC-01 · Intervalo de muestreo

| Campo | Valor |
|---|---|
| Valor actual | **120 s** |
| Decisión | Se mantiene en la fase 1 (P-11, 2026-09-25). |
| Por qué podría cambiar | DKD-R 5-7 §7.3 (30 valores en 30 min) e IEC 60068-3-5 §4.4 (1 registro por minuto) piden **60 s o menos**. 120 s cumple la OMS (1 a 15 min). |
| Dónde se cambia | 1) Parámetro `SamplingIntervalSeconds` en `AppSetting`: es el único cambio necesario en producción. 2) `INSERT` inicial y `DF_MeasurementSession_Interval` en [docs/db/01-schema.sql](docs/db/01-schema.sql). 3) Constante `INTERVAL_S` en [test-data/generate-test-data.mjs](test-data/generate-test-data.mjs), y después regenerar los escenarios. |
| Qué se recalcula solo | Muestras mínimas (31 → 61 con 60 s), última muestra de la sesión, y los 30 min de falla y de fuera de límite sostenido. |
| Condición para el desarrollo | El código no usa los literales `120` ni `31`: lee el intervalo de la sesión y deriva el resto. |

Detalle completo en [01-vision-document.md §12](docs/specs/functional/01-vision-document.md#pc-01--intervalo-de-muestreo).

## Arquitectura

- **Web API en .NET 10** (ASP.NET Core) con **Clean Architecture + CQRS + dominio enriquecido**. La captura serial corre en segundo plano dentro de la misma API, en la PC del laboratorio. Ver [ADR-001](docs/architecture/adr/ADR-001-clean-architecture-cqrs-ddd.md).
- **SQL Server 2022**: las restricciones `CHECK` son la segunda línea de defensa detrás del dominio. Ver [docs/db/01-schema.sql](docs/db/01-schema.sql).
- Protocolo serial propio v1.0: texto ASCII con checksum, modos `POLL` y `STREAM`, independiente del hardware.

## Convención de nombres: inglés en todo lo técnico

**Decisión (2026-09-25):** todo lo que es código o contrato va en **inglés**, con los mismos nombres en todas las capas. El **español** queda para la documentación, los mensajes al usuario y los textos del Excel.

| Capa | Nombre del tipo de equipo | Ejemplos |
|---|---|---|
| Base de datos ([01-schema.sql](docs/db/01-schema.sql)) | `dbo.EquipmentType` | `MaxTemperatureC`, `MinSessionDurationMinutes`, `RowVersion` |
| Dominio ([domain-model.md](docs/architecture/domain-model.md)) | `EquipmentType` | `TemperatureLimit`, `ChangeLimit`, `Deactivate` |
| Aplicación (CQRS) | `CreateEquipmentTypeCommand` | `UpdateEquipmentTypeCommand`, `GetEquipmentTypeByIdQuery`, `EquipmentTypeDto` |
| API ([thermal-v1.yaml](docs/api/thermal-v1.yaml)) | `EquipmentTypesController` | Rutas `/api/v1/equipment-types`, schemas `CreateEquipmentTypeRequest`, `CreateEquipmentTypeResponse`, `UpdateEquipmentTypeRequest`, `EquipmentTypeResponse`, `ErrorResponse` |
| JSON | camelCase | `maxTemperatureC`, `minSessionDurationMinutes`, `isActive` |
| Datos de prueba | camelCase | `config.equipmentTypeMinSessionDurationMinutes` |

Reglas para lo nuevo:

- **No** usar nombres en español en clases, propiedades, rutas ni schemas (`TipoEquipo`, `CreateTipoEquipamientoCommand`… no existen).
- Rutas REST en plural, minúsculas y kebab-case: `/api/v1/equipment-types`, `/api/v1/sessions/{id}/alerts`.
- Comandos y consultas: `<Verbo><Entidad>Command` / `Get<Entidad>…Query`, con su handler `…Handler` al lado.
- Mensajes de error y de validación en español, iguales a los de las historias (p. ej. "El límite admite como máximo 2 decimales").

## Backend (.NET 10)

```text
Thermal.slnx
src/
  Thermal.Domain/          # Entidades y value objects, sin dependencias externas
  Thermal.Application/     # Comandos, consultas, handlers y puertos (CQRS con interfaces propias)
  Thermal.Infrastructure/  # EF Core 10 sobre SQL Server (esquema de docs/db/01-schema.sql)
  Thermal.Api/             # Controladores, Problem Details (RFC 7807), JWT y composición
```

| Tema | Decisión |
|---|---|
| Endpoints | Controladores `[ApiController]` según [thermal-v1.yaml](docs/api/thermal-v1.yaml) |
| CQRS | `ICommandHandler` / `IQueryHandler` propios, **sin MediatR** (licencia comercial desde 2025; [ADR-001 §2.2](docs/architecture/adr/ADR-001-clean-architecture-cqrs-ddd.md#22-cqrs-lógico)) |
| Paquetes NuGet | Versiones centralizadas en [Directory.Packages.props](Directory.Packages.props): `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.AspNetCore.OpenApi` y `Microsoft.Extensions.DependencyInjection.Abstractions` (10.0.12) |
| Compilación | C# 14, nulabilidad activa y advertencias tratadas como errores ([Directory.Build.props](Directory.Build.props)) |
| Base de datos | Base primero: se crea con [01-schema.sql](docs/db/01-schema.sql), sin migraciones de EF |
| Autenticación | JWT provisional hasta decidir el mecanismo definitivo |

Compilar, preparar la base y ejecutar en desarrollo (SQL Server LocalDB):

```bash
dotnet build Thermal.slnx
sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i docs/db/01-schema.sql
dotnet user-jwts create --project src/Thermal.Api --role Admin --output token
dotnet run --project src/Thermal.Api
```

La API queda en `https://localhost:5001`. Hay solicitudes de ejemplo en [Thermal.Api.http](src/Thermal.Api/Thermal.Api.http); se pega el token del paso anterior.

Implementado: `POST`, `GET` y `PUT` de `/api/v1/equipment-types` (HU-02).

## Documentación

| Documento | Contenido |
|---|---|
| [01-vision-document.md](docs/specs/functional/01-vision-document.md) | Problema, alcance, reglas de negocio, glosario, base normativa, decisiones, preguntas abiertas y puntos de cambio |
| [02-serial-protocol.md](docs/specs/functional/02-serial-protocol.md) | Protocolo PC ↔ adquisidor |
| [03-user-stories.md](docs/specs/functional/03-user-stories.md) | Historias HU-01 a HU-17 en Gherkin y formato del Excel |
| [04-sequence-diagrams.md](docs/specs/functional/04-sequence-diagrams.md) | Índice de los diagramas de secuencia y sus participantes |
| [05-data-model.md](docs/specs/functional/05-data-model.md) | Modelo de datos y reglas de la base |
| [06-test-data.md](docs/specs/functional/06-test-data.md) | Set de datos de prueba y adquisidor simulado |
| [docs/diagrams/gherkin/](docs/diagrams/gherkin/) | Criterios de aceptación en Gherkin: un `.feature` por historia, generado desde 03 con `node docs/diagrams/generate-features.mjs` |
| [docs/diagrams/sequence/](docs/diagrams/sequence/) | Diagramas de secuencia en Mermaid (en VS Code: vista previa de Markdown con la extensión recomendada `bierner.markdown-mermaid`) |
| [docs/validation.md](docs/validation.md) | Validación y trazabilidad HU ↔ RN ↔ TD, oráculo de los escenarios y hallazgos pendientes |
| [docs/api/thermal-v1.yaml](docs/api/thermal-v1.yaml) | Contrato OpenAPI 3.0 de la Web API (tipos de equipo). Validar con `npx @redocly/cli lint docs/api/thermal-v1.yaml` |
| [docs/architecture/](docs/architecture/) | Modelo de dominio, C4 de contenedores y ADR-001 |
| [docs/standards/](docs/standards/README.md) | Fichas de OMS TRS 961, IEC 60068-3-5, EURAMET cg-20 y DKD-R 5-7, y matriz norma → especificación |

## Pruebas sin hardware

22 escenarios reproducibles (TD-01 a TD-22), con sus resultados esperados, en [test-data/](test-data/README.md). Se regeneran con:

```bash
node test-data/generate-test-data.mjs
```

## Estado

| | |
|---|---|
| Fase | Especificación y arquitectura completas. Backend iniciado: tipos de equipo (HU-02) |
| Decisiones tomadas | P-01, P-02, P-05, P-11, P-13, P-15, P-16, P-17 y D-01 a D-04 ([01 §10](docs/specs/functional/01-vision-document.md#10-decisiones-tomadas)) |
| Preguntas abiertas | P-03, P-04, P-06 a P-10, P-12 y P-14, con propuesta provisional ([01 §11](docs/specs/functional/01-vision-document.md#11-preguntas-abiertas)) |
| Pendiente | Autenticación (cuentas propias o Windows/AD); edición 2025-01 de DKD-R 5-7 |
