# 06 · Set de datos de prueba y adquisidor simulado

| Campo | Valor |
|---|---|
| Versión | 0.5 (borrador para revisión) |
| Cambios en 0.5 | Columna "Historias" de la §5.1 alineada con el índice de 03 (TD-10, TD-11, TD-12, TD-14 y TD-16). Diagrama de la simulación movido a [docs/diagrams/sequence/](../../diagrams/sequence/05-sesion-simulada.md). |
| Cambios en 0.4 | Mínimo de 9 puntos: los escenarios con menos canales esperan la advertencia `BelowMinimumPoints`. Escenario TD-22 (exactamente 9 puntos). |
| Cambios en 0.3 | Escenarios TD-20 y TD-21 (fuera de límite sostenido con causa probable sensor o equipo). |
| Cambios en 0.2 | Duración planificada por escenario y cierre automático (`PlannedDuration`). Escalamiento y falla por pérdida de sensores. Escenarios TD-16 (falla), TD-17 (72 h), TD-18 (límites del escalamiento) y TD-19 (duración por tipo de equipo). |
| Fecha | 2026-09-25 |
| Relacionado | [01-vision-document.md](01-vision-document.md) (RN-14 a RN-16), [02-serial-protocol.md](02-serial-protocol.md) (F-13), [03-user-stories.md](03-user-stories.md) (HU-14), [04-sequence-diagrams.md](04-sequence-diagrams.md) ([diagrama 5](../../diagrams/sequence/05-sesion-simulada.md)), [05-data-model.md](05-data-model.md) |
| Archivos | [test-data/](../../../test-data/) · generador [test-data/generate-test-data.mjs](../../../test-data/generate-test-data.mjs) |

---

## 1. Propósito

Al comienzo del desarrollo no habrá termopares ni adquisidor. Para no bloquear el desarrollo ni las pruebas, se define:

1. Un **set de datos de prueba** con 22 escenarios reproducibles. Cubren el funcionamiento óptimo, las variaciones de medida, la caída de sensores alrededor del umbral del 60 %, el escalamiento a crítica y la falla de sesión, la pérdida de comunicación, el cambio de adquisidor o de grupo de sensores, las tramas corruptas, la mezcla T/K, los datos previos a la conexión, la duración planificada (base, por tipo de equipo, 24 h y 72 h) y una combinación aleatoria.
2. Un **adquisidor simulado** que implementa el mismo protocolo que el firmware y reproduce cualquier escenario.
3. Los **resultados esperados** de cada escenario (oráculo): estado final, muestras válidas y afectadas, lecturas por estado, alertas y huecos. Con ellos se comprueba automáticamente que la aplicación aplica bien las reglas.

Los datos son **pseudoaleatorios con semilla fija**: el generador produce siempre exactamente los mismos archivos.

## 2. Estructura de archivos

```text
test-data/
├── README.md
├── generate-test-data.mjs          # generador y oráculo (Node.js ≥ 18, sin dependencias)
└── scenarios/
    ├── index.json                  # resumen de todos los escenarios
    ├── TD-01-optimo-2h/
    │   ├── scenario.json           # configuración, eventos y resultados esperados
    │   ├── readings.csv            # una fila por canal y muestra, con lo que transmite el adquisidor y lo esperado
    │   └── transcript.log          # transcripción serial de referencia PC <-> adquisidor
    ├── TD-02-variaciones-medida/
    └── ...
```

Regenerar: `node test-data/generate-test-data.mjs`. Si cambia el intervalo de muestreo (**[PC-01]**, [01 §12](01-vision-document.md#pc-01--intervalo-de-muestreo)), se ajusta la constante `INTERVAL_S` y se regenera: los resultados esperados se recalculan solos. Borra y vuelve a crear `test-data/scenarios/` e imprime la tabla resumen de la §5.

## 3. Adquisidor simulado

| Id | Requisito |
|---|---|
| SIM-01 | Implementa el protocolo v1.0 completo ([02 §10](02-serial-protocol.md#10-requisitos-para-el-firmware-y-el-simulador-resumen-de-conformidad)) con `Platform` = `Simulator` y `DeviceId` `ADQ-SIM-nnnn`. |
| SIM-02 | **Transporte:** (a) en memoria, dentro de la aplicación, seleccionable como puerto `SIM` para pruebas rápidas y automatizadas; (b) par de puertos COM virtuales (p. ej. com0com en Windows, `socat` en Linux), para probar la pila serial real sin hardware. |
| SIM-03 | Lee `scenario.json` y `readings.csv`. Responde a `IDN` con los datos de `config.device` y el modo de `config.acquisitionMode`. |
| SIM-04 | Modo `POLL`: responde a `READ,n` con las filas de la muestra *n* y aplica `FrameFault` (§4.2) según el número de intento. Modo `STREAM`: emite los bloques por su cuenta, incluidos los bloques previos al inicio y la línea parcial. |
| SIM-05 | Aplica los eventos: `COMM_LOST` (deja de responder), `COMM_RESTORED` (emite `BOOT` y responde `IDN` con el `DeviceId` y el `SensorGroupId` del evento) y `DEVICE_REBOOT` (emite `BOOT` y responde `NAK,READ,E04` hasta recibir `START`). |
| SIM-06 | **Reloj virtual acelerable** (x1, x60, x120, x720). La aplicación y el simulador comparten el mismo reloj inyectable. Las marcas de tiempo almacenadas son las **simuladas**, de modo que se aplican las reglas de duración (1 h, 24 h) igual que en una sesión real. Con x720, el escenario de 24 h dura unos 2 minutos. |
| SIM-07 | Con el mismo escenario y el mismo intervalo, el simulador produce, línea a línea, las mismas tramas `ADQ` que `transcript.log` (salvo `PING`/`ACK,PING`). |
| SIM-08 | Solo se habilita si la configuración de la aplicación no es "Producción". Las sesiones quedan con `IsSimulation` = 1, `TestScenarioCode` = código del escenario, la empresa de prueba y un equipo `SIM-<tipo>` (RN-16). |
| SIM-09 | Al cerrar la sesión, un validador compara lo almacenado con `expected` (§6) y muestra "OK" o la lista de diferencias. |

## 4. Formato de los archivos

### 4.1 `scenario.json`

| Ruta | Tipo | Descripción |
|---|---|---|
| `code`, `title`, `category`, `description` | texto | Identificación del escenario. |
| `protocolVersion` | texto | Versión del protocolo (`1.0`). |
| `seed` | entero | Semilla del generador pseudoaleatorio. |
| `config.isSimulation` | bool | Siempre `true`. |
| `config.equipmentType` | texto | `Congeladora`, `Refrigeradora`… |
| `config.maxTemperatureC` | número o `null` | Límite aplicado. `null` = límite pendiente. |
| `config.samplingIntervalSeconds` | entero | 120. |
| `config.equipmentTypeMinSessionMinutes` | entero | Duración mínima que exige el tipo de equipo (60 salvo TD-19). |
| `config.plannedDurationMinutes`, `config.durationSource`, `config.clientRequestReference` | entero, texto, texto | Duración planificada, su origen (`Base`, `EquipmentType`, `ClientRequest`) y la referencia del pedido. |
| `config.sensorLossThresholdPct` | número | 60. |
| `config.sensorLossCriticalAfterSamples`, `config.sensorLossFailMinutes`, `config.aboveLimitCriticalMinutes`, `config.minMeasurementPoints` | entero | 3, 30, 30 y 9. |
| `config.acquisitionMode` | `Poll` \| `Stream` | Modo del adquisidor simulado. |
| `config.device` | objeto | `deviceIdentifier`, `platform`, `firmwareVersion`, `channelCount`, `sensorGroupId`. |
| `config.channels[]` | lista | `channelNumber`, `thermocoupleType` (declarado), `position`. |
| `config.close` | objeto | `{ "type": "PlannedDuration" }` (cierre automático) o `{ "type": "Manual", "atOffsetSeconds": n }`. |
| `events[]` | lista | `COMM_LOST` (`sample`), `COMM_RESTORED` (`sample`, `deviceIdentifier`, `sensorGroupId`) y `DEVICE_REBOOT` (`sample`). `sample` es la muestra afectada por el evento. La reconexión ocurre 20 s antes de la muestra `sample` y la comprobación de `IDN` termina 3 s después. |
| `expected.status`, `expected.closeReason` | texto | Estado y motivo de cierre esperados. |
| `expected.startedAtOffsetSeconds`, `expected.endedAtOffsetSeconds` | entero | Siempre 0 y la duración en segundos. |
| `expected.hasMixedThermocoupleTypes` | bool | |
| `expected.programmedSamples`, `samplesWithData`, `validSamples` | entero | Conteos de muestras. |
| `expected.affectedSamples[]` | lista | Números de las muestras afectadas (incluidas las perdidas). |
| `expected.readingRows`, `expected.readingsByStatus` | entero, objeto | Filas esperadas en `Reading`, en total y por `SensorStatus`. |
| `expected.aboveLimitReadings` | entero | Lecturas con `IsAboveLimit` = 1. |
| `expected.alertsByType`, `expected.alerts[]` | objeto, lista | Alertas esperadas, por tipo y en detalle (`alertType`, `severity`, `sampleNumber`, `channel` y datos específicos). |
| `expected.communicationGaps[]` | lista | `lostAtOffsetSeconds`, `recoveredAtOffsetSeconds` (o `null`), `firstMissedSample`, `lastMissedSample`, `missedSamples`, `notes`. |
| `expected.discardedPreStartBlocks` | entero | Bloques descartados antes del inicio (modo `STREAM`). |

Todos los desfases (`OffsetSeconds`) se miden en segundos desde `StartedAt` (muestra 1).

### 4.2 `readings.csv`

Separador `,`, punto decimal, UTF-8, una fila por canal y muestra con datos. Las muestras perdidas por un hueco **no** tienen filas.

| Columna | Descripción |
|---|---|
| `SampleNumber` | Número de muestra (1, 2, 3…). |
| `OffsetSeconds` | Segundos desde `StartedAt` = `ReadAt` esperado. En `STREAM` incluye la variación de llegada (0–2 s). |
| `Channel` | Canal 1…10. |
| `DeclaredType` | Tipo declarado en la sesión. |
| `ReportedType` | Tipo que informa el adquisidor en la trama. |
| `TransmittedValue` | Valor transmitido (1 o 2 decimales), vacío si `DeviceStatus` ≠ `OK`. |
| `DeviceStatus` | `OK`, `OC`, `SC` u `OR` (lo que envía el adquisidor). |
| `FrameFault` | Falla de transmisión inyectada: `NONE`; `BAD_CHECKSUM_RECOVERED` (corrupta en el 1.er intento y correcta en el 2.º); `BAD_CHECKSUM` (corrupta en los 3 intentos); `TRUNCATED` (línea cortada, sin checksum, en los 3 intentos); `MISSING` (no se envía en ningún intento). |
| `ExpectedSensorStatus` | `SensorStatus` que debe almacenar la aplicación. |
| `ExpectedTemperatureC` | `TemperatureC` esperada (2 decimales) o vacío. |
| `ExpectedIsAboveLimit` | 0 o 1. |

### 4.3 `transcript.log`

Transcripción de referencia del intercambio serial, útil para las pruebas unitarias del parser y para verificar el simulador (SIM-07).

```text
# comentarios
<offset_s>\t<origen>\t<línea>
-4.900	PC	$IDN*43
-4.850	ADQ	$IDN,1.0,ADQ-SIM-0001,Simulator,1.0.0,10,POLL,GRP-A*02
0.000	PC	$READ,1*0F
0.120	ADQ	$RD,1,1,T,-17.31,OK*6D
```

- `origen` = `PC`, `ADQ` o `#` (comentario que describe el evento).
- Desfases negativos: antes de la muestra 1.
- Se omiten `PING`/`ACK,PING`.
- Las tramas corruptas aparecen tal como las recibiría la PC (checksum que no coincide o línea truncada).

### 4.4 `index.json`

Lista con el resumen de cada escenario (código, carpeta, categoría, canales, modo, estado y motivo esperados, duración, muestras programadas, válidas y afectadas, y alertas por tipo).

## 5. Catálogo de escenarios

Los escenarios con menos de 9 canales esperan además la advertencia `BelowMinimumPoints` (RN-20): son casos de prueba enfocados en otras reglas, no sesiones de calibración conformes. Límite de -5,00 °C (congeladora), umbral del 60 %, escalamiento en la 3.ª muestra consecutiva y falla a los 30 min, salvo que se indique otra cosa. "Duración" es la duración real de la sesión (`hh:mm`). La planificada figura en la §5.1.

| Código | Categoría | Canales | Modo | Duración | Muestras (prog./válidas/afect.) | Estado esperado | Motivo | Alertas esperadas |
|---|---|---|---|---|---|---|---|---|
| TD-01 | Funcionamiento óptimo | 10 | Poll | 02:00 | 61 / 61 / 0 | Completed | PlannedDuration | — |
| TD-02 | Variaciones de medida | 5 | Poll | 03:00 | 91 / 91 / 0 | Completed | PlannedDuration | BelowMinimumPoints ×1, AboveLimit ×4 |
| TD-03 | Caída de sensores | 10 | Poll | 02:00 | 61 / 61 / 0 | Completed | PlannedDuration | SensorFault ×6 |
| TD-04 | Caída de sensores | 10 | Poll | 02:00 | 61 / 51 / 10 | Completed | PlannedDuration | SensorFault ×7, SensorLoss ×1, SensorLossPersistent ×1 |
| TD-05 | Caída de sensores | 5 | Poll | 01:00 | 31 / 21 / 10 | Incomplete | PlannedDuration | BelowMinimumPoints ×1, SensorFault ×4, SensorLoss ×1, SensorLossPersistent ×1 |
| TD-06 | Comunicación | 5 | Poll | 02:00 | 61 / 56 / 5 | Completed | PlannedDuration | BelowMinimumPoints ×1, CommunicationLost ×1 |
| TD-07 | Comunicación | 5 | Poll | 01:47 | 54 / 49 / 5 | Invalid | DeviceMismatch | BelowMinimumPoints ×1, CommunicationLost ×1, DeviceMismatch ×1 |
| TD-08 | Comunicación | 5 | Poll | 01:03 | 32 / 29 / 3 | Invalid | DeviceMismatch | BelowMinimumPoints ×1, CommunicationLost ×1, DeviceMismatch ×1 |
| TD-09 | Tramas y tipos | 5 | Poll | 02:00 | 61 / 61 / 0 | Completed | PlannedDuration | BelowMinimumPoints ×1, SensorFault ×6, TypeMismatch ×1 |
| TD-10 | Configuración | 5 | Poll | 01:00 | 31 / 31 / 0 | Completed | PlannedDuration | MixedThermocoupleTypes ×1, BelowMinimumPoints ×1 |
| TD-11 | Inicio por llegada de datos | 5 | Stream | 01:00 | 31 / 31 / 0 | Completed | PlannedDuration | BelowMinimumPoints ×1 |
| TD-12 | Duración | 10 | Poll | 24:00 | 721 / 721 / 0 | Completed | PlannedDuration | — |
| TD-13 | Duración | 5 | Poll | 00:45 | 23 / 23 / 0 | Incomplete | Manual | BelowMinimumPoints ×1 |
| TD-14 | Configuración | 5 | Poll | 02:00 | 61 / 61 / 0 | Completed | PlannedDuration | BelowMinimumPoints ×1, LimitNotDefined ×1 |
| TD-15 | Aleatorio combinado | 8 | Poll | 06:00 | 181 / 174 / 7 | Completed | PlannedDuration | BelowMinimumPoints ×1, SensorFault ×21, AboveLimit ×7, SensorLoss ×1, SensorLossPersistent ×1, TypeMismatch ×1, CommunicationLost ×1 |
| TD-16 | Caída de sensores | 5 | Poll | 01:08 | 35 / 19 / 16 | Invalid | DataLoss | BelowMinimumPoints ×1, SensorFault ×4, SensorLoss ×1, SensorLossPersistent ×1, SessionFailed ×1 |
| TD-17 | Duración | 5 | Poll | 72:00 | 2161 / 2161 / 0 | Completed | PlannedDuration | BelowMinimumPoints ×1 |
| TD-18 | Caída de sensores | 5 | Poll | 03:00 | 91 / 71 / 20 | Completed | PlannedDuration | BelowMinimumPoints ×1, SensorFault ×12, SensorLoss ×3, SensorLossPersistent ×2 |
| TD-19 | Duración | 5 | Poll | 01:30 | 46 / 46 / 0 | Incomplete | Manual | BelowMinimumPoints ×1, LimitNotDefined ×1 |
| TD-20 | Variaciones de medida | 5 | Poll | 02:00 | 61 / 61 / 0 | Completed | PlannedDuration | BelowMinimumPoints ×1, AboveLimit ×26, AboveLimitSustained ×1 |
| TD-21 | Variaciones de medida | 5 | Poll | 02:00 | 61 / 61 / 0 | Completed | PlannedDuration | BelowMinimumPoints ×1, AboveLimit ×141, AboveLimitSustained ×5 |
| TD-22 | Configuración | 9 | Poll | 01:00 | 31 / 31 / 0 | Completed | PlannedDuration | — |

Severidad de las alertas esperadas: `AboveLimitSustained`, `SensorLossPersistent`, `SessionFailed`, `CommunicationLost` y `DeviceMismatch` son **críticas**; `LimitNotDefined` es **informativa**; el resto son **advertencias** (01 §6.3).

### 5.1 Detalle de cada escenario

**Modelo de temperatura común.** Valor = consigna + desfase por zona + ciclo del compresor (0,8 °C de amplitud, periodo de 40 min, fase distinta por canal) + ruido gaussiano (σ = 0,15 °C). La consigna es -18 °C en la congeladora, +5 °C en la refrigeradora y +37 °C en la incubadora. Desfases por zona (canales 1 a 10): Superior +1,2, Centro 0, Inferior -0,8, Puerta +2,2, Fondo -0,5, Lateral izq. +0,4, Lateral der. +0,3, Esquina sup. izq. +1,0, Esquina inf. der. -1,0, Bandeja media +0,1. Así se reproduce la variación normal entre zonas.

| Código | Duración planificada | Qué prueba | Detalle | Historias |
|---|---|---|---|---|
| TD-01 | 120 min, cliente | Funcionamiento óptimo | 10 canales T, sin fallas ni alertas. Cierre automático. Es la línea base. | HU-03, HU-06 |
| TD-02 | 180 min, cliente | Variaciones de medida y límite estricto | S4 (Puerta): muestra 20 = -5,00 (**cumple**), 21 = -4,99 y 22 = -4,90 (**fuera**), 50 = +2,10 y 51 = -3,50 (**fuera**), 52 = -6,20 (cumple). S2 transmite con 1 decimal. S1 tiene un pico de -15,80 en la muestra 70, dentro del límite. Las 4 alertas son **advertencias**: no interrumpen. | HU-08, HU-15 |
| TD-03 | 120 min, cliente | Umbral exacto: 60 % no afecta | Muestras 20–25: canales 1–6 de 10 en `OC` (60 %). Ninguna muestra afectada. 6 alertas `SensorFault`. | HU-13 |
| TD-04 | 120 min, cliente | Sobre el umbral (70 %) con escalamiento | Muestras 30–39: S1–S3 `OC`, S4–S5 `SC`, S6 trama ausente y S7 trama truncada (7 de 10). Advertencia en la 30 y **crítica en la 32**. El episodio dura 18 min, menos de 30: no falla. Sigue **completa** (51 válidas ≥ 31). | HU-13, HU-15 |
| TD-05 | 60 min, base | Sesión base incompleta por pérdida | Muestras 5–14: S1–S4 `OC` (80 %), con crítica en la 7. Al cumplir la hora hay 21 válidas < 31 → **Incomplete**. | HU-13, HU-10 |
| TD-06 | 120 min, cliente | Hueco recuperado | Sin respuesta desde la muestra 40. Reconecta el mismo adquisidor y grupo 20 s antes de la muestra 45. 5 muestras perdidas (8 min), sin llegar a los 30 min: sesión completa. | HU-09 |
| TD-07 | 120 min, cliente | Otro adquisidor | Hueco desde la muestra 50. Al reconectar responde `ADQ-SIM-0002` → **fallida** (`Invalid`/`DeviceMismatch`) a los 6463 s (01:47:43). | HU-09 |
| TD-08 | 120 min, cliente | Otro grupo de sensores | Hueco desde la muestra 30. Reconecta `ADQ-SIM-0001` con el grupo `GRP-B` en lugar de `GRP-A` → **fallida**. | HU-09 |
| TD-09 | 120 min, cliente | Tramas y tipos | Checksum recuperado en el reintento (m8 S2 → `OK`), checksum persistente (m12 S2), trama truncada (m15 S4), trama ausente (m18 S5): `InvalidFrame`. Tipo K informado en S3 declarado T (m20–25): 6 lecturas `TypeMismatch` y 1 alerta. Valor de 400,00 °C con tipo T (m40 S1): `OutOfRange` de la PC. `OR` del adquisidor (m45 S4). `SC` (m50 S5). Reinicio del adquisidor antes de la muestra 30 (`BOOT` → `NAK,READ,E04` → `START`). | HU-07 |
| TD-10 | 60 min, base | Mezcla T/K en la sesión base | S1–S4 T y S5 K. Cierre automático a los 3600 s con 31 muestras válidas: el caso límite de sesión completa. | HU-04, HU-10, HU-16 |
| TD-11 | 60 min, base | Datos previos a la conexión | Modo `STREAM`. Al abrir el puerto llega una línea parcial y luego dos bloques completos (`Seq` 1038 y 1039) antes de pulsar Iniciar: se descartan. La sesión inicia con el bloque `Seq` 1040 (muestra 1). Los bloques siguientes llegan con 0–2 s de variación. | HU-03, HU-06 |
| TD-12 | 1440 min, cliente | Sesión ocasional de 24 h | 10 canales, variación diaria de ±0,6 °C. 721 muestras y 7210 lecturas. Cierre automático. | HU-06, HU-10, HU-16 |
| TD-13 | 60 min, base | Cierre antes de lo planificado | Cierre manual a los 45 min (23 muestras) → **Incomplete** (`Manual`). | HU-10 |
| TD-14 | 120 min, cliente | Límite no definido | Refrigeradora (consigna +5 °C), límite NULL. S4 llega a +9,50 °C en las muestras 30–32 sin generar `AboveLimit`. Solo la alerta `LimitNotDefined`. | HU-02, HU-05 |
| TD-15 | 360 min, cliente | Estrés aleatorio combinado | Semilla 20260925, 8 canales. 6 aperturas de puerta en S4, con 7 lecturas fuera de límite. 4 episodios `OC` al azar. Tramas corruptas al azar (≈ 0,9 % de las lecturas). Tipo no coincidente en S2 (m133–135). Caída de 6 de 8 sensores (75 %) en las muestras 65–68 → advertencia en la 65 y crítica en la 67. Hueco de comunicación en las muestras 150–152. | Todas |
| TD-16 | 120 min, cliente | **Falla por pérdida sostenida** | Desde la muestra 20, S1–S4 `OC` sin recuperarse. Advertencia en la 20, crítica en la 22 y, a los 30 min (muestra 35, 01:08), `SessionFailed` → **fallida** (`Invalid`/`DataLoss`). No hay lecturas después de la 35. | HU-13, HU-15 |
| TD-17 | 4320 min, cliente `OS-2026-0142` | Varios días (72 h) | Caso extremo a pedido del cliente: 2161 muestras y 10 805 lecturas. Prueba que no hay un tope fijo de 24 h ni de 721 muestras. | HU-16 |
| TD-18 | 180 min, cliente | Límites del escalamiento | Tres episodios con 4 de 5 canales abiertos. Muestras 10–11 (2 seguidas): solo advertencia. Muestras 30–32 (3): crítica en la 32. Muestras 50–64 (15, 28 min): crítica en la 52, sin falla. | HU-13 |
| TD-19 | 120 min, tipo de equipo | Duración mínima por tipo | Incubadora con mínimo de 120 min (configuración de prueba). Cierre manual a los 90 min: tiene 46 muestras válidas (≥ 31), pero no cumplió lo planificado → **Incomplete**. | HU-02, HU-16 |
| TD-20 | 120 min, cliente | **Posible falla del sensor** | Solo S3 (Inferior) marca alrededor de -2 °C entre las muestras 20 y 45, mientras los demás siguen cerca de -18 °C. 26 advertencias `AboveLimit` y, a los 30 min (muestra 35), una crítica `AboveLimitSustained` con causa probable `Sensor`. | HU-08, HU-15 |
| TD-21 | 120 min, cliente | **Posible falla del equipo** | Desde la muestra 30 todos los canales suben 3 °C por muestra hasta +15 °C sobre lo normal. Una crítica `AboveLimitSustained` por canal (S4 en la muestra 48, los demás en la 49) con causa probable `Equipment`. La sesión sigue completa: los datos son válidos. | HU-08, HU-15 |
| TD-22 | 60 min, base | Mínimo normativo exacto | 9 canales en las 8 esquinas y el centro. No genera `BelowMinimumPoints`. Con 8 canales (TD-15) sí se genera. | HU-17 |

## 6. Reglas del oráculo

El generador calcula `expected` con las mismas reglas de la especificación. Es la **implementación de referencia**: si la aplicación y el oráculo difieren, hay que revisar ambas contra el documento funcional.

| Paso | Regla | Ref. |
|---|---|---|
| 1 | Clasificación de cada lectura, en este orden: `FrameFault` persistente → `InvalidFrame`; estado `OC`/`SC`/`OR` → `OpenCircuit`/`ShortCircuit`/`OutOfRange`; tipo informado ≠ declarado → `TypeMismatch`; valor fuera del rango del tipo declarado → `OutOfRange`; si no → `OK`. | 02 §7 |
| 2 | `IsAboveLimit` = `OK` y valor > límite, comparando en centésimas enteras (sin errores de coma flotante). | RN-07 |
| 3 | Muestra afectada: `(activos − OK) × 100 > umbral × activos`. Las muestras perdidas por un hueco están afectadas. | RN-14 |
| 4 | Alertas `SensorFault` y `TypeMismatch` por episodio y canal. `SensorLoss` (Warning) por episodio de muestras afectadas con lecturas. `AboveLimit` por lectura. `CommunicationLost` por hueco. `MixedThermocoupleTypes`, `LimitNotDefined` y `DeviceMismatch` una vez por sesión. El estado de episodio de cada canal se conserva a través de los huecos. | 02 §8, RN-14 |
| 4a | `BelowMinimumPoints` (Warning) una vez por sesión si hay menos de 9 canales activos. | RN-20 |
| 4b | Fuera de límite sostenido: por canal, `j` = lecturas `OK` fuera de límite seguidas (cualquier otra lectura o una muestra perdida lo reinicia). `AboveLimitSustained` (Critical) una vez por racha cuando `(j − 1) × 120 ≥ 1800`. `suspectedCause` = `Equipment` si en esa muestra al menos la mitad de las lecturas `OK` está fuera de límite; si no, `Sensor`. | RN-19, 01 §6.4 |
| 5 | Escalamiento: `k` = muestras afectadas consecutivas (incluidas las perdidas). `SensorLossPersistent` (Critical) una vez por racha, cuando `k` ≥ 3 en una muestra con lecturas. Falla cuando `(k − 1) × 120 ≥ 1800`: `SessionFailed` (Critical), `EndedAt` = instante de esa muestra y se descarta todo lo posterior. | 01 §6.2 |
| 6 | Muestras programadas = `floor(EndedAt / 120) + 1`. Muestras válidas = programadas − afectadas. `EndedAt` = duración planificada (cierre automático) o el cierre manual, la reconexión con otro equipo o la falla. | RN-02, RN-04 |
| 7 | Estado: `Invalid` si hay falla o si reconecta otro adquisidor o grupo. Si no, `Completed` si `EndedAt` ≥ duración planificada y hay ≥ 31 muestras válidas. Si no, `Incomplete`. | RN-03, RN-15 |
| 8 | Motivo: `DataLoss`, `DeviceMismatch`, `PlannedDuration`, `CommunicationLost` (hueco abierto al cerrar) o `Manual`. | HU-10 |

## 7. Uso en las pruebas

| Nivel | Qué se prueba | Archivos | Criterio de aceptación |
|---|---|---|---|
| Unitario: parser y validación de tramas | V1–V10, checksum, tramas truncadas, tardías y duplicadas | `transcript.log` | Cada línea `ADQ` se clasifica igual que su fila en `readings.csv`. |
| Unitario: reglas de negocio | Límite, umbral, escalamiento, falla, episodios de alertas, duración planificada y cierre | `readings.csv`, `scenario.json` | Los resultados coinciden con `expected`, incluida la severidad de cada alerta. |
| Integración: aplicación + simulador | Protocolo completo, reintentos, huecos, reconexión, invalidación, inicio al llegar datos, cierre automático | Carpeta del escenario | El validador SIM-09 informa "OK" en los 22 escenarios. |
| Base de datos | Restricciones `CHECK` y `vSessionSampleCoverage` | `scenario.json`, `readings.csv` | Los 22 escenarios cargan sin violar restricciones, y la vista devuelve las muestras programadas y afectadas esperadas. La base rechaza `Completed` en TD-19 (no cumplió lo planificado). **Verificado** el 2026-09-25 con el script [01-schema.sql](../../db/01-schema.sql). |
| Presentación de alertas | Críticas con aviso y reconocimiento; advertencias sin aviso | TD-02, TD-04, TD-16, TD-20, TD-21 | TD-02 no muestra ningún aviso emergente. TD-04 muestra 1 aviso crítico (muestra 32). TD-16 muestra 2 avisos críticos (muestras 22 y 35). TD-20 muestra 1 aviso crítico con causa "Sensor" (muestra 35). TD-21 muestra 2 avisos agrupados con causa "Equipo" (muestras 48 y 49). |
| Exportación a Excel | Avisos, estados de muestra, formatos de celda y hojas | Sesiones simuladas | TD-02 muestra 4 celdas rojas. TD-04 muestra 10 filas "Afectada". TD-06 muestra 5 filas "Perdida". TD-07, TD-08 y TD-16 muestran el aviso de sesión fallida. TD-10 muestra el aviso de mezcla. TD-14 no tiene celdas rojas. TD-17 muestra la referencia `OS-2026-0142`. Todas llevan el aviso de datos simulados. |
| Rendimiento | 72 h, 5 canales | TD-17 | Captura, cierre y exportación sin errores. Exportación en menos de 15 s. |

## 8. Cómo añadir un escenario

1. Agregar una entrada a `SCENARIOS` en [generate-test-data.mjs](../../../test-data/generate-test-data.mjs), con un código `TD-nn` nuevo, su `slug`, una semilla propia y, si hace falta, una función `mutate(row)` que modifique `value`, `deviceStatus`, `reportedType` o `frameFault`.
2. Ejecutar `node test-data/generate-test-data.mjs` y revisar el `expected` generado contra la especificación.
3. Actualizar la tabla de la §5 y, si corresponde, los ejemplos de HU-14.
4. No modificar a mano los archivos generados: se sobrescriben en cada ejecución.
