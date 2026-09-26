# Checklist de los próximos sprints

Orden de trabajo del backend a partir del estado actual, con la **autenticación al final** (decisión del 2026-09-26). Cada sprint aplica la [definición de terminado](implementation-plan.md#4-definición-de-terminado-por-entidad) a sus entidades; aquí solo se detalla lo propio de cada uno.

| Campo | Valor |
|---|---|
| Fecha | 2026-09-26 |
| Estado de partida | Sprint 0 terminado: `EquipmentType` con D-05, D-06 y D-07. 94 pruebas, contrato v1.2.0 verificado 33/33, 12 de 152 escenarios completos |
| Estado actual | Sprints 1 y 2 terminados (2026-09-26): 179 pruebas, contrato v1.3.0 verificado 88/88, 19 de 152 escenarios completos |
| Relacionado | [implementation-plan.md](implementation-plan.md) (dependencias, lecciones aprendidas), [validation.md §5.3](validation.md#53-estado-de-implementación-por-historia-2026-09-26) (estado por historia) |

**Cómo usarlo:** marcar cada casilla al cumplirla; al cerrar un sprint, actualizar la tabla de estado de abajo, la §5.3 de validation.md y la §1 del plan de implementación.

## Resumen

| Sprint | Tema | Historias | Escenarios | Estado |
|---|---|---|---|---|
| 0 | Tipos de equipo | HU-02 (parcial) | 12 de 19 | ✅ Terminado |
| 1 | **Configuración**: parámetros del sistema y tipos de termopar | HU-02 (resto), HU-16 (parámetros), HU-07 (rango físico) | 3 de HU-02 + parte de HU-16 | ✅ Terminado (1 completo, 1 parcial: la copia en la sesión es del sprint 4) |
| 2 | Empresas y equipos | HU-01, HU-02 (desactivar con equipos) | 8 + 1 | ✅ Terminado (6 completos y 2 parciales de HU-01) |
| 3 | Adquisidor, protocolo serial y simulador | HU-03 (detección), HU-09 (identidad), HU-14 | parte de 12 + 5 | ⏳ **Siguiente** |
| 4 | Sesión: configurar e iniciar | HU-03, HU-04, HU-05, HU-16, HU-17 | 12 + 6 + 4 + 9 + 7 | ⏳ |
| 5 | Captura, lecturas y alertas | HU-06, HU-07, HU-08, HU-13, HU-15 | 10 + 7 + 12 + 11 + 4 | ⏳ |
| 6 | Comunicación, cierre y reanudación | HU-09, HU-10 | 10 + 9 | ⏳ |
| 7 | Historial y exportación a Excel | HU-11, HU-12 | 11 + 8 | ⏳ |
| 8 | Pruebas de extremo a extremo y datos reales | HU-14, todas | TD-01 … TD-22 + DATA-1 | ⏳ |
| 9 | Perfiles de eficacia por modelo | (historia nueva) | — | ⏳ Por especificar |
| 10 | **Autenticación y usuarios** (al final) | Todas (roles) | — | ⏳ |
| 11 | Despliegue y documentación de entrega | — | — | ⏳ |

**Mientras no haya autenticación definitiva** (sprints 1 a 9): se sigue usando el JWT de desarrollo (`dotnet user-jwts`) con los roles `Admin`, `Technician` y `Supervisor`. Donde haga falta un usuario (técnico responsable, quién reconoce una alerta), se usa un puerto `ICurrentUser` que la API resuelve desde el token, y usuarios de `AppUser` sembrados para desarrollo. En el sprint 10 solo cambia la implementación de ese puerto.

---

## Sprint 1 · Configuración: parámetros del sistema y tipos de termopar

**Objetivo:** el administrador mantiene los parámetros globales (`AppSetting`) que se copian en cada sesión, y el sistema conoce el rango físico de cada tipo de termopar.

Antes de empezar:

- [x] Confirmar los rangos de cada parámetro que aún no los tienen: descanso (`RestPeriodMinutes`), duración base y máxima, intervalo de muestreo **[PC-01]** (¿se permite cambiarlo desde la API o solo por script?). → Fijados en **D-08** (01 §10), confirmada el 2026-09-26; el intervalo sí se edita por la API y debe dividir exactamente la duración base.

Contrato y código:

- [x] Contrato: `GET /api/v1/settings` (cualquier rol) y `PUT /api/v1/settings` (Admin), con `ETag`/`If-Match`, schemas `SettingsResponse` / `UpdateSettingsRequest` con valores tipados (no cadenas).
- [x] Contrato: `GET /api/v1/thermocouple-types` (solo lectura).
- [x] Dominio: agregado `SystemSettings` sobre la tabla clave-valor, con value objects tipados (`SensorLossPolicy`, intervalo, duraciones, descanso, `AboveLimitCriticalMinutes`). → `SystemSettings` inmutable con propiedades tipadas y `MinValidSamples` derivado; `SensorLossPolicy` se extrae en el sprint 4, al copiarla en la sesión.
- [x] Reglas: umbral de pérdida > 0 y < 100 (HU-02); escalamiento 2 a 10 muestras; falla 10 a 240 min; fuera de límite sostenido 10 a 240 min; duración máxima ≤ 30 días; **sin literales 120 ni 31** (PC-01).
- [x] Mensajes iguales a los de HU-02 ("El umbral debe ser mayor que 0 y menor que 100").
- [x] Catálogo `ThermocoupleType` (T: -200 … 350 °C; K: -200 … 1260 °C) con consulta de solo lectura; sin edición en la fase 1.
- [x] Infraestructura: mapeo a `dbo.AppSetting` (clave-valor) y `dbo.ThermocoupleType`; `UpdatedAt`/`UpdatedById` al editar. → `UpdatedAt` sí; `UpdatedById` queda NULL hasta tener usuarios (sprint 10). El `ETag` es una huella de las `RowVersion` de todas las filas.

Pruebas y cierre:

- [x] Escenarios de HU-02: "Cambiar el umbral de pérdida de sensores" (la copia en la sesión queda para el sprint 4) y "Rechazar un umbral fuera de rango" (0 y 100).
- [x] Pruebas unitarias de cada parámetro en sus límites; integración contra el esquema real.
- [x] Casos nuevos en [tools/contract-check/](../tools/contract-check/README.md) (`cases/settings.mjs`, `cases/thermocouple-types.mjs`) y auditoría conforme. → Ambos recursos en `cases/settings.mjs`; 88/88 conformes.
- [x] Documentación: validation.md (cobertura), README (endpoints y cURL), manual de administrador §5 (tabla de parámetros), plan y este checklist.

**Criterio de salida:** los parámetros se leen y editan por la API con validación y concurrencia; los 3 escenarios de configuración de HU-02 tienen prueba.

## Sprint 2 · Empresas y equipos

**Objetivo:** registrar empresas cliente (RUC) y sus equipos, con marca y modelo obligatorios.

- [x] Contrato: `POST /api/v1/companies`, `GET /api/v1/companies/{id}`, `GET /api/v1/companies?taxId=&name=`, `POST /api/v1/companies/{id}/equipment`, `GET /api/v1/equipment/{id}`, `PUT` de ambos recursos.
- [x] Dominio `Company`: RUC de 11 dígitos, prefijo 10/15/17/20 y dígito verificador módulo 11; RUC único.
- [x] Dominio `Equipment`: serie única por empresa; tipo obligatorio y **activo**; **marca y modelo obligatorios**; `IsModelConfirmed` con `ConfirmModel`.
- [x] HU-02 "Desactivar un tipo con equipos asociados": un tipo con equipos no se borra (la API ya no tiene `DELETE`; comprobar que un tipo inactivo no se ofrece para equipos nuevos).
- [x] Mejora M-08 de 05: `CHECK` del RUC en la base (actualizar 01-schema.sql y 05, verificar en SQL Server).
- [x] Pruebas de los 8 escenarios de HU-01 (incluido el modelo inferido) y auditoría del contrato.
- [x] Registrar la cámara de DATA-1 como equipo de ejemplo en los datos de desarrollo (Memmert TTC256, modelo inferido). → [02-dev-data.sql](db/02-dev-data.sql), aparte del esquema (las pruebas solo usan 01) e idempotente; `docker compose` lo aplica al crear la base.

**Criterio de salida:** HU-01 completa (8/8) y el escenario de desactivación de HU-02 completo. → Alcanzado en lo que depende de este sprint: 6/8 completos; "historial vacío" (HU-12), el perfil de eficacia del modelo inferido y el aviso al intentar borrar un tipo (interfaz) dependen de sprints posteriores.

## Sprint 3 · Adquisidor, protocolo serial y simulador

**Objetivo:** detectar un adquisidor, hablar el protocolo v1.0 y reproducir los 22 escenarios sin hardware.

- [ ] `AcquisitionDevice` (1 a 27 canales, `DeviceId` único) y `DeviceIdentity` (value object, comparación para la reconexión).
- [ ] Parser del protocolo (V1–V6: formato, checksum XOR, número de muestra), máscara de `START` de 1 a 7 dígitos hexadecimales.
- [ ] `ISerialTransport` con `SerialTransport` (System.IO.Ports) y `SimulatedTransport` (SIM-01 a SIM-09), reloj `TimeProvider` acelerable.
- [ ] Prueba: cada línea `ADQ` de los 22 `transcript.log` se clasifica igual que su fila de `readings.csv`.
- [ ] Contrato: `GET /api/v1/ports`, `POST /api/v1/ports/{port}/detect`, `GET /api/v1/devices/{id}`.
- [ ] El modo simulación solo se registra fuera de `Production` (SIM-08).

**Criterio de salida:** el simulador reproduce las transcripciones de referencia (SIM-07) y la detección responde según HU-03.

## Sprint 4 · Sesión de medición: configurar e iniciar

**Objetivo:** configurar la sesión (equipo, puerto, adquisidor, canales) e iniciarla con todas las advertencias.

- [ ] Agregado `MeasurementSession` + `SessionChannel` según [domain-model.md](architecture/domain-model.md) (métodos por transición, sin setters públicos).
- [ ] Copias al iniciar (RN-08): criterio de límite (rango o banda y consigna), política de pérdida, intervalo, puntos mínimos del tipo.
- [ ] Consigna obligatoria en tipos con banda (D-05); hasta 27 canales (D-06); límite sugerido indicado en la sesión (D-07).
- [ ] Advertencias con confirmación: mezcla T/K (HU-04), menos puntos que el mínimo del tipo (HU-17), límite pendiente (HU-05).
- [ ] Duración planificada y descanso del adquisidor con autorización del supervisor (HU-16).
- [ ] `ICurrentUser` para el técnico responsable (con el JWT de desarrollo).
- [ ] Contrato: `POST /api/v1/sessions`, `PUT /api/v1/sessions/{id}/channels`, `POST /api/v1/sessions/{id}/start`, `…/acknowledge-mixed-types`, `…/acknowledge-below-minimum`, `…/authorize-early-start`, `GET /api/v1/devices/{id}/rest`.
- [ ] Completar los escenarios de HU-02 que dependían de la sesión (edición del límite sin afectar sesiones, sesión en curso, límite definido después).

**Criterio de salida:** HU-03, HU-04, HU-05, HU-16 y HU-17 completas; HU-02 sin escenarios parciales por falta de sesión.

## Sprint 5 · Captura, lecturas y alertas

**Objetivo:** registrar cada muestra con todas las reglas del dominio.

- [ ] `RecordSample`: clasificación V7–V10, evaluación del límite (`TemperatureLimit.Evaluate`: rango o banda, por arriba y por abajo), `IsAboveLimit`/`IsBelowLimit`.
- [ ] Episodios `SensorFault`/`TypeMismatch` (P-07: por episodio); pérdida de sensores con escalamiento y falla (RN-14, RN-15); fuera de límite sostenido por arriba y por abajo con causa probable (RN-19).
- [ ] Eventos de dominio → `Alert` en la misma transacción; severidades de RN-18.
- [ ] `CaptureWorker` (`BackgroundService`) y hub SignalR `MonitorHub` (`CriticalAlertRaised` solo para críticas).
- [ ] Oráculo: el `expected` de cada `scenario.json` como prueba de las reglas; prueba con 60 s y con 120 s (PC-01).
- [ ] Contrato: `GET /api/v1/sessions/{id}/coverage`, `GET /api/v1/sessions/{id}/alerts?severity=`, `POST /api/v1/alerts/{id}/acknowledge`.

**Criterio de salida:** los resultados de las reglas coinciden con el oráculo de los 22 escenarios.

## Sprint 6 · Comunicación, cierre y reanudación

- [ ] Huecos de comunicación, reintentos, reconexión en otro puerto y validación de identidad (HU-09, RN-15).
- [ ] Falla por 30 min sin datos (`DataLoss`) y por otro adquisidor o grupo (`DeviceMismatch`).
- [ ] Cierre automático, manual, durante un hueco y cancelación (HU-10); extensión de la duración.
- [ ] Reanudación de sesiones `Running` tras reiniciar el servicio.
- [ ] Contrato: `POST /api/v1/sessions/{id}/close`, `…/cancel`, `…/extend`.

**Criterio de salida:** HU-09 y HU-10 completas.

## Sprint 7 · Historial y exportación a Excel

- [ ] Consultas de historial y detalle sobre `vSessionSampleCoverage` y `vSessionReadingPivot` (27 columnas), sin pasar por el agregado (HU-12).
- [ ] Librería OpenXML con **licencia libre verificada** (p. ej. ClosedXML) antes de añadirla.
- [ ] Excel con el formato de 03: hojas Resumen, Lecturas, Alertas y Comunicación; límite aplicado como rango o banda, "sugerido por la norma", lecturas por debajo del límite en azul; `SessionExport` como bitácora (HU-11).
- [ ] Rendimiento: exportación de TD-17 (72 h) en menos de 15 s.

**Criterio de salida:** HU-11 y HU-12 completas.

## Sprint 8 · Pruebas de extremo a extremo y datos reales

- [ ] API + `SimulatedTransport` con los 22 escenarios: el validador SIM-09 informa "OK" en todos.
- [ ] Convertir DATA-1 en un escenario de datos reales (TD-23: 12 canales a 5 min, banda) y añadirlo al generador y al catálogo de 06.
- [ ] `Thermal.ArchitectureTests`: regla de dependencias del ADR-001 §5.
- [ ] Revisar la cobertura de los 152 escenarios: ninguno sin prueba ni marca de pendiente.

**Criterio de salida:** los 22 (o 23) escenarios pasan de extremo a extremo.

## Sprint 9 · Perfiles de eficacia por modelo

- [ ] Especificar la historia nueva (HU-18) con sus escenarios Gherkin a partir de [docs/data/](data/README.md).
- [ ] Métricas por sesión al cerrar (homogeneidad, fluctuación, variación estándar, deriva, disponibilidad), con [analyze_thermal_data.py](data/analyze_thermal_data.py) como implementación de referencia.
- [ ] Especificación del fabricante por modelo ([equipment-catalog](equipment-catalog/README.md)) y comparación; perfil por marca y modelo, distinguiendo modelos confirmados e inferidos.

**Criterio de salida:** el perfil de DATA-1 se reproduce desde una sesión del sistema.

## Sprint 10 · Autenticación y usuarios (al final)

Antes de empezar:

- [ ] **Decidir el mecanismo**: cuentas propias con JWT o Windows/AD (c4-containers §3.1).

Tareas:

- [ ] `AppUser` (rol `Admin`/`Technician`/`Supervisor`, correo único) y gestión de usuarios (manual de administrador §3).
- [ ] Implementación definitiva de `ICurrentUser` y de la configuración de autenticación; retirar la dependencia de `dotnet user-jwts` fuera de desarrollo.
- [ ] Matriz de permisos completa (RA-02) con pruebas por rol en cada endpoint.
- [ ] Actualizar el contrato (`securitySchemes`), la auditoría del contrato y el compose.

**Criterio de salida:** todos los endpoints protegidos con el mecanismo definitivo y la matriz de permisos probada.

## Sprint 11 · Despliegue y documentación de entrega

- [ ] Servicio de Windows, configuración de producción y secretos; verificación posterior al despliegue.
- [ ] Completar las plantillas de [docs/delivery/](delivery/README.md): despliegue (con plan de rollback), usuario, administrador y cierre técnico con el acta de aceptación.
- [ ] Confirmar con el laboratorio los límites sugeridos (P-03) y el resto de preguntas abiertas que sigan pendientes.

**Criterio de salida:** acta de aceptación lista para firmar.
