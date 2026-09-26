# Documentación técnica de cierre

| Campo | Valor |
|---|---|
| Proyecto | ThermalCalibration, fase 1 |
| Versión entregada | [COMPLETAR: versión y etiqueta de git] |
| Lectores | Equipo técnico receptor, Product Owner y representante del cliente |
| Estado | **Plantilla** |

## Índice

**Parte A · Documentación técnica**

1. Resumen del sistema entregado
2. Arquitectura y decisiones
3. Documentación de la API (endpoints)
4. Modelo de datos
5. Calidad y pruebas
6. Trazabilidad requisitos → implementación → pruebas
7. Deuda técnica, limitaciones conocidas y pendientes
8. Guía para el equipo que mantendrá el sistema

**Parte B · Acta de aceptación final**

9. Acta de aceptación final

---

# Parte A · Documentación técnica

## 1. Resumen del sistema entregado

**Contenido:** alcance entregado frente al comprometido (historias HU-01 a HU-17), versión, repositorio y fecha. Una tabla por módulo (Sesión de Medición, Adquisición Serial, Exportación) con su estado.
**Fuente:** [validation.md §5.3](../validation.md#53-estado-de-implementación-por-historia-2026-09-25). **Estado:** ⏳

## 2. Arquitectura y decisiones

**Contenido:** resumen de la arquitectura con el diagrama de capas y remisión a los documentos vigentes; registro de decisiones (ADR) con fecha y motivo: Clean Architecture + CQRS, controladores en lugar de Minimal APIs, sin MediatR, base primero sin migraciones, licencias de dependencias.
**Fuente:** [ADR-001](../architecture/adr/ADR-001-clean-architecture-cqrs-ddd.md), [c4-containers.md](../architecture/c4-containers.md), [domain-model.md](../architecture/domain-model.md). **Estado:** ✅

## 3. Documentación de la API (endpoints)

### 3.1 Información general

**Contenido:** URL base por entorno, versionado (`/api/v1`), autenticación y roles, formato (JSON, camelCase), fechas (America/Lima), errores RFC 7807 (estructura, `traceId`), concurrencia (`ETag` / `If-Match`), códigos de estado comunes.
**Fuente:** [thermal-v1.yaml](../api/thermal-v1.yaml) (fuente de verdad; esta sección la resume, no la sustituye). **Estado:** ✅

### 3.2 Catálogo de endpoints

**Contenido:** una tabla por recurso, generada o revisada contra el contrato:

| Método | Ruta | Descripción | Rol | Respuestas | Historia |
|---|---|---|---|---|---|
| `POST` | `/api/v1/equipment-types` | Registrar un tipo de equipo | Admin | 201, 400, 401, 403, 409, 415 | HU-02 |
| `GET` | `/api/v1/equipment-types/{id}` | Obtener un tipo de equipo | Autenticado | 200, 401, 404 | HU-02 |
| `PUT` | `/api/v1/equipment-types/{id}` | Reemplazar o desactivar un tipo de equipo | Admin | 200, 400, 401, 403, 404, 409, 412, 415 | HU-02 |
| [COMPLETAR: un bloque por cada recurso nuevo] | | | | | |

**Estado:** ⚠️ Parcial (tipos de equipo)

### 3.3 Ficha por endpoint

**Contenido:** plantilla que se repite por endpoint:

- **Propósito** y reglas de negocio aplicadas (RN, HU).
- **Solicitud:** parámetros, cabeceras, cuerpo (schema y ejemplo).
- **Respuestas:** por código, con schema y ejemplo real.
- **Ejemplo cURL** verificado.
- **Errores frecuentes** y su causa.

### 3.4 Tiempo real (SignalR)

**Contenido:** hub `/hubs/monitor`, eventos (`SampleRecorded`, `CriticalAlertRaised`, `SessionStatusChanged`, `CommunicationChanged`), carga útil y cuándo se emite cada uno.
**Estado:** ⏳ (ola 4)

### 3.5 Verificación del contrato

**Contenido:** resultado de la auditoría automática de todos los endpoints contra el contrato y del `redocly lint`.
**Fuente:** [tools/contract-check/](../../tools/contract-check/README.md), [validation.md §5.2](../validation.md#52-auditoría-código--contrato--gherkin-2026-09-25). **Estado:** ⚠️ Parcial

## 4. Modelo de datos

**Contenido:** diagrama entidad-relación, tablas y vistas, restricciones como segunda línea de defensa, scripts entregados y versión del esquema.
**Fuente:** [05-data-model.md](../specs/functional/05-data-model.md), [01-schema.sql](../db/01-schema.sql). **Estado:** ✅

## 5. Calidad y pruebas

**Contenido:** estrategia (unitarias, integración con Testcontainers, extremo a extremo con los 22 escenarios TD), resultados finales (número de pruebas, todas correctas), cobertura de código [COMPLETAR], análisis estático (compilación sin advertencias, `dotnet format`), pruebas de rendimiento (TD-17: 72 h, exportación en menos de 15 s).
**Fuente:** [validation.md §5](../validation.md#5-niveles-de-validación-de-la-aplicación). **Estado:** ⚠️ Parcial

## 6. Trazabilidad requisitos → implementación → pruebas

**Contenido:** matriz historia → escenario Gherkin → código → pruebas → estado, y regla de negocio → historias → escenarios TD. Remite a las tablas de cobertura de validation.md y resume los totales.
**Fuente:** [validation.md §3 y §5](../validation.md). **Estado:** ⚠️ Parcial

## 7. Deuda técnica, limitaciones conocidas y pendientes

**Contenido:** tabla de elemento, impacto, prioridad y propuesta. Punto de partida:

| Elemento | Impacto | Propuesta |
|---|---|---|
| Autenticación provisional (JWT de desarrollo) | Alto | Decidir entre cuentas propias y Windows/AD |
| Intervalo de 120 s frente a los 60 s de IEC 60068-3-5 y DKD-R 5-7 **[PC-01]** | Normativo | Decisión del Product Owner (P-11) |
| Mensajes de validación de formato en inglés (ASP.NET Core) | Bajo | Traducir con un `InvalidModelStateResponseFactory` |
| Preguntas abiertas P-03, P-04, P-06 a P-10, P-12 y P-14 | Variable | Ver 01 §11 |
| Edición 2025-01 de DKD-R 5-7 sin revisar | Normativo | Revisar los apartados citados |
| [COMPLETAR al cierre] | | |

## 8. Guía para el equipo que mantendrá el sistema

**Contenido:** cómo compilar, probar y desplegar; convenciones (nombres en inglés, contrato primero, definición de terminado); dónde está cada documento; cómo añadir una entidad.
**Fuente:** [README](../../README.md), [CLAUDE.md](../../CLAUDE.md), [implementation-plan.md](../implementation-plan.md). **Estado:** ✅

---

# Parte B · Acta de aceptación final

## 9. Acta de aceptación final

**Acta N.º:** [COMPLETAR] · **Fecha:** [COMPLETAR] · **Lugar:** [COMPLETAR]

### 9.1 Datos del proyecto

| Campo | Valor |
|---|---|
| Proyecto | ThermalCalibration: Sistema de Monitoreo Térmico para Calibración de Equipos de Refrigeración, fase 1 |
| Cliente | [COMPLETAR: laboratorio] |
| Proveedor / equipo de desarrollo | [COMPLETAR] |
| Versión entregada | [COMPLETAR: versión, etiqueta de git y fecha] |
| Periodo | [COMPLETAR: inicio – fin] |

### 9.2 Alcance aceptado

**Contenido:** lista de módulos e historias entregadas, con referencia a la matriz de trazabilidad (sección 6).

### 9.3 Entregables

| Entregable | Ubicación | Entregado |
|---|---|---|
| Código fuente y repositorio | [COMPLETAR] | ☐ |
| Scripts de base de datos | `docs/db/` | ☐ |
| Contrato OpenAPI | `docs/api/thermal-v1.yaml` | ☐ |
| Manual de despliegue e implementación | `docs/delivery/01-manual-despliegue.md` | ☐ |
| Manual de usuario | `docs/delivery/02-manual-usuario.md` | ☐ |
| Manual de administrador | `docs/delivery/03-manual-administrador.md` | ☐ |
| Documentación técnica de cierre | Este documento | ☐ |
| Informe de pruebas | `docs/validation.md` | ☐ |
| Binarios / paquete de instalación | [COMPLETAR] | ☐ |

### 9.4 Criterios de aceptación y resultado

| # | Criterio | Evidencia | Resultado |
|---|---|---|---|
| 1 | Todas las historias comprometidas implementadas con sus escenarios Gherkin | validation.md §5 | ☐ Cumple ☐ No cumple |
| 2 | Pruebas automatizadas en verde | Salida de `dotnet test` | ☐ / ☐ |
| 3 | Los 22 escenarios TD con el validador SIM-09 en "OK" | Informe de pruebas | ☐ / ☐ |
| 4 | Contrato de la API verificado sin desviaciones | contract-check y `redocly lint` | ☐ / ☐ |
| 5 | Despliegue en el entorno del laboratorio verificado | Manual de despliegue, sección 8 | ☐ / ☐ |
| 6 | Manuales entregados y revisados | Sección 9.3 | ☐ / ☐ |
| [COMPLETAR] | | | |

### 9.5 Observaciones y pendientes aceptados

**Contenido:** pendientes que el cliente acepta recibir después (con fecha comprometida y responsable), y observaciones que no impiden la aceptación.

| # | Observación o pendiente | Responsable | Fecha comprometida |
|---|---|---|---|
| | | | |

### 9.6 Garantía y soporte

**Contenido:** periodo de garantía, alcance (corrección de defectos), canal de soporte y tiempos de respuesta [COMPLETAR].

### 9.7 Declaración de aceptación

Las partes declaran que el sistema descrito se entrega y se recibe **☐ a conformidad ☐ con las observaciones de la sección 9.5**, de acuerdo con los criterios de la sección 9.4.

### 9.8 Firmas

| Rol | Nombre | Firma | Fecha |
|---|---|---|---|
| Representante del cliente | | | |
| Product Owner | | | |
| Líder técnico / arquitecto | | | |
| Responsable de calidad | | | |
