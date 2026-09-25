# 06 · Set de datos de prueba y adquisidor simulado

| Campo | Valor |
|---|---|
| Versión | 0.1 (borrador para revisión) |
| Fecha | 2026-09-25 |
| Relacionado | [01-vision-document.md](01-vision-document.md) (RN-14 a RN-16), [02-serial-protocol.md](02-serial-protocol.md) (F-13), [03-user-stories.md](03-user-stories.md) (HU-14), [04-sequence-diagrams.md](04-sequence-diagrams.md) (diagrama 5), [05-data-model.md](05-data-model.md) |
| Archivos | [test-data/](../../../test-data/) · generador [test-data/generate-test-data.mjs](../../../test-data/generate-test-data.mjs) |

---

## 1. Propósito

Al comienzo del desarrollo no habrá termopares ni adquisidor. Para no bloquear el desarrollo ni las pruebas, se define:

1. Un **set de datos de prueba** con 15 escenarios reproducibles. Cubren el funcionamiento óptimo, las variaciones de medida, la caída de sensores alrededor del umbral del 60 %, la pérdida de comunicación, el cambio de adquisidor o de grupo de sensores, las tramas corruptas, la mezcla T/K, los datos previos a la conexión, las duraciones límite y una combinación aleatoria.
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

Regenerar: `node test-data/generate-test-data.mjs`. Borra y vuelve a crear `test-data/scenarios/` e imprime la tabla resumen de la §5.

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
| `config.sensorLossThresholdPct` | número | 60. |
| `config.acquisitionMode` | `Poll` \| `Stream` | Modo del adquisidor simulado. |
| `config.device` | objeto | `deviceIdentifier`, `platform`, `firmwareVersion`, `channelCount`, `sensorGroupId`. |
| `config.channels[]` | lista | `channelNumber`, `thermocoupleType` (declarado), `position`. |
| `config.close` | objeto | `{ "type": "Manual", "atOffsetSeconds": n }` o `{ "type": "MaxDuration" }`. |
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
| `SampleNumber` | Número de muestra (1…721). |
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

Límite de -5,00 °C (congeladora) y umbral del 60 % salvo que se indique otra cosa. Duración en `hh:mm`.

| Código | Categoría | Canales | Modo | Duración | Muestras (prog./válidas/afect.) | Estado esperado | Motivo | Alertas esperadas |
|---|---|---|---|---|---|---|---|---|
| TD-01 | Funcionamiento óptimo | 10 | Poll | 02:00 | 61 / 61 / 0 | Completed | Manual | — |
| TD-02 | Variaciones de medida | 5 | Poll | 03:00 | 91 / 91 / 0 | Completed | Manual | AboveLimit ×4 |
| TD-03 | Caída de sensores | 10 | Poll | 02:00 | 61 / 61 / 0 | Completed | Manual | SensorFault ×6 |
| TD-04 | Caída de sensores | 10 | Poll | 02:00 | 61 / 51 / 10 | Completed | Manual | SensorFault ×7, SensorLoss ×1 |
| TD-05 | Caída de sensores | 5 | Poll | 01:10 | 36 / 26 / 10 | Incomplete | Manual | SensorFault ×4, SensorLoss ×1 |
| TD-06 | Comunicación | 5 | Poll | 02:00 | 61 / 56 / 5 | Completed | Manual | CommunicationLost ×1 |
| TD-07 | Comunicación | 5 | Poll | 01:47 | 54 / 49 / 5 | Invalid | DeviceMismatch | CommunicationLost ×1, DeviceMismatch ×1 |
| TD-08 | Comunicación | 5 | Poll | 01:03 | 32 / 29 / 3 | Invalid | DeviceMismatch | CommunicationLost ×1, DeviceMismatch ×1 |
| TD-09 | Tramas y tipos | 5 | Poll | 02:00 | 61 / 61 / 0 | Completed | Manual | SensorFault ×6, TypeMismatch ×1 |
| TD-10 | Configuración | 5 | Poll | 01:00 | 31 / 31 / 0 | Completed | Manual | MixedThermocoupleTypes ×1 |
| TD-11 | Inicio por llegada de datos | 5 | Stream | 01:05 | 33 / 33 / 0 | Completed | Manual | — |
| TD-12 | Funcionamiento óptimo | 10 | Poll | 24:00 | 721 / 721 / 0 | Completed | MaxDuration | — |
| TD-13 | Duración | 5 | Poll | 00:45 | 23 / 23 / 0 | Incomplete | Manual | — |
| TD-14 | Configuración | 5 | Poll | 02:00 | 61 / 61 / 0 | Completed | Manual | LimitNotDefined ×1 |
| TD-15 | Aleatorio combinado | 8 | Poll | 06:00 | 181 / 174 / 7 | Completed | Manual | SensorFault ×21, AboveLimit ×7, SensorLoss ×1, TypeMismatch ×1, CommunicationLost ×1 |

### 5.1 Detalle de cada escenario

**Modelo de temperatura común.** Valor = consigna + desfase por zona + ciclo del compresor (0,8 °C de amplitud, periodo de 40 min, fase distinta por canal) + ruido gaussiano (σ = 0,15 °C). La consigna es -18 °C en la congeladora y +5 °C en la refrigeradora. Desfases por zona (canales 1 a 10): Superior +1,2, Centro 0, Inferior -0,8, Puerta +2,2, Fondo -0,5, Lateral izq. +0,4, Lateral der. +0,3, Esquina sup. izq. +1,0, Esquina inf. der. -1,0, Bandeja media +0,1. Así se reproduce la variación normal entre zonas.

| Código | Qué prueba | Detalle | Historias |
|---|---|---|---|
| TD-01 | Funcionamiento óptimo | 10 canales T, 2 h, sin fallas ni alertas. Es la línea base. | HU-03, HU-06 |
| TD-02 | Variaciones de medida y límite estricto | S4 (Puerta): muestra 20 = -5,00 (**cumple**), 21 = -4,99 y 22 = -4,90 (**fuera**), 50 = +2,10 y 51 = -3,50 (**fuera**), 52 = -6,20 (cumple). S2 transmite con 1 decimal. S1 tiene un pico de -15,80 en la muestra 70, dentro del límite. | HU-08 |
| TD-03 | Umbral exacto: 60 % no afecta | Muestras 20–25: canales 1–6 de 10 en `OC` (60 %). Ninguna muestra afectada. 6 alertas `SensorFault`. | HU-13 |
| TD-04 | Sobre el umbral: 70 % | Muestras 30–39: S1–S3 `OC`, S4–S5 `SC`, S6 trama ausente y S7 trama truncada (7 de 10). 10 muestras afectadas, 1 alerta `SensorLoss`. Sigue **completa** (51 válidas ≥ 31). | HU-13, HU-07 |
| TD-05 | Pérdida que deja la sesión incompleta | 5 canales, 1 h 10 min. Muestras 5–14: S1–S4 `OC` (80 %). 26 válidas < 31 → **Incomplete** aunque dure más de 1 h. | HU-13, HU-10 |
| TD-06 | Hueco recuperado | Sin respuesta desde la muestra 40. Reconecta el mismo adquisidor y grupo 20 s antes de la muestra 45. 5 muestras perdidas y afectadas, sesión completa. | HU-09 |
| TD-07 | Otro adquisidor | Hueco desde la muestra 50. Al reconectar responde `ADQ-SIM-0002` → **Invalid**/`DeviceMismatch` a los 6463 s (01:47:43). | HU-09 |
| TD-08 | Otro grupo de sensores | Hueco desde la muestra 30. Reconecta `ADQ-SIM-0001` con el grupo `GRP-B` en lugar de `GRP-A` → **Invalid**. | HU-09 |
| TD-09 | Tramas y tipos | Checksum recuperado en el reintento (m8 S2 → `OK`), checksum persistente (m12 S2), trama truncada (m15 S4), trama ausente (m18 S5): `InvalidFrame`. Tipo K informado en S3 declarado T (m20–25): 6 lecturas `TypeMismatch` y 1 alerta. Valor de 400,00 °C con tipo T (m40 S1): `OutOfRange` de la PC. `OR` del adquisidor (m45 S4). `SC` (m50 S5). Reinicio del adquisidor antes de la muestra 30 (`BOOT` → `NAK,READ,E04` → `START`). | HU-07 |
| TD-10 | Mezcla T/K y duración mínima exacta | S1–S4 T y S5 K. Cierre a los 3600 s con 31 muestras válidas: es el caso límite de sesión completa. | HU-04, HU-10 |
| TD-11 | Datos previos a la conexión | Modo `STREAM`. Al abrir el puerto llega una línea parcial y luego dos bloques completos (`Seq` 1038 y 1039) antes de pulsar Iniciar: se descartan. La sesión inicia con el bloque `Seq` 1040 (muestra 1). Los bloques siguientes llegan con 0–2 s de variación. | HU-06 |
| TD-12 | 24 h y cierre automático | 10 canales, variación diaria de ±0,6 °C. 721 muestras, cierre `MaxDuration`. 7210 lecturas. | HU-10 |
| TD-13 | Sesión corta | Cierre manual a los 45 min (23 muestras) → **Incomplete**. | HU-10 |
| TD-14 | Límite no definido | Refrigeradora (consigna +5 °C), límite NULL. S4 llega a +9,50 °C en las muestras 30–32 sin generar `AboveLimit`. Solo la alerta `LimitNotDefined`. | HU-05 |
| TD-15 | Estrés aleatorio combinado | Semilla 20260925, 8 canales, 6 h. 6 aperturas de puerta en S4, con 7 lecturas fuera de límite. 4 episodios `OC` al azar. Tramas corruptas al azar (≈ 0,9 % de las lecturas). Tipo no coincidente en S2 (m133–135). Caída de 6 de 8 sensores (75 %) en las muestras 65–68 → 4 muestras afectadas y 1 `SensorLoss`. Hueco de comunicación en las muestras 150–152. | Todas |

## 6. Reglas del oráculo

El generador calcula `expected` con las mismas reglas de la especificación. Es la **implementación de referencia**: si la aplicación y el oráculo difieren, hay que revisar ambas contra el documento funcional.

| Paso | Regla | Ref. |
|---|---|---|
| 1 | Clasificación de cada lectura, en este orden: `FrameFault` persistente → `InvalidFrame`; estado `OC`/`SC`/`OR` → `OpenCircuit`/`ShortCircuit`/`OutOfRange`; tipo informado ≠ declarado → `TypeMismatch`; valor fuera del rango del tipo declarado → `OutOfRange`; si no → `OK`. | 02 §7 |
| 2 | `IsAboveLimit` = `OK` y valor > límite, comparando en centésimas enteras (sin errores de coma flotante). | RN-07 |
| 3 | Muestra afectada: `(activos − OK) × 100 > umbral × activos`. Las muestras perdidas por un hueco están afectadas. | RN-14 |
| 4 | Alertas `SensorFault` y `TypeMismatch` por episodio y canal. `SensorLoss` por episodio de muestras afectadas con lecturas. `AboveLimit` por lectura. `CommunicationLost` por hueco. `MixedThermocoupleTypes`, `LimitNotDefined` y `DeviceMismatch` una vez por sesión. El estado de episodio de cada canal se conserva a través de los huecos. | 02 §8, RN-14 |
| 5 | Muestras programadas = `floor(EndedAt / 120) + 1`. Muestras válidas = programadas − afectadas. | RN-02 |
| 6 | Estado: `Invalid` si reconecta otro adquisidor o grupo. Si no, `Completed` si la duración es ≥ 3600 s y hay ≥ 31 muestras válidas. Si no, `Incomplete`. | RN-03, RN-15 |
| 7 | Motivo: `DeviceMismatch`, `MaxDuration`, `CommunicationLost` (hueco abierto al cerrar) o `Manual`. | HU-10 |

## 7. Uso en las pruebas

| Nivel | Qué se prueba | Archivos | Criterio de aceptación |
|---|---|---|---|
| Unitario: parser y validación de tramas | V1–V10, checksum, tramas truncadas, tardías y duplicadas | `transcript.log` | Cada línea `ADQ` se clasifica igual que su fila en `readings.csv`. |
| Unitario: reglas de negocio | Límite, umbral de pérdida, episodios de alertas, cierre | `readings.csv`, `scenario.json` | Los resultados coinciden con `expected`. |
| Integración: aplicación + simulador | Protocolo completo, reintentos, huecos, reconexión, invalidación, inicio al llegar datos | Carpeta del escenario | El validador SIM-09 informa "OK" en los 15 escenarios. |
| Base de datos | Restricciones `CHECK` y `vSessionSampleCoverage` | `scenario.json`, `readings.csv` | Los 15 escenarios cargan sin violar restricciones, y la vista devuelve las muestras programadas y afectadas esperadas. **Verificado** el 2026-09-25 con el script [01-schema.sql](../../db/01-schema.sql). |
| Exportación a Excel | Avisos, estados de muestra, formatos de celda y hojas | Sesiones simuladas | TD-02 muestra 4 celdas rojas. TD-04 muestra 10 filas "Afectada". TD-06 muestra 5 filas "Perdida". TD-07 y TD-08 muestran el aviso de no válida. TD-10 muestra el aviso de mezcla. TD-14 no tiene celdas rojas. Todas llevan el aviso de datos simulados. |
| Rendimiento | 24 h, 10 canales | TD-12 | Captura, cierre y exportación sin errores. Exportación en menos de 10 s. |

## 8. Cómo añadir un escenario

1. Agregar una entrada a `SCENARIOS` en [generate-test-data.mjs](../../../test-data/generate-test-data.mjs), con un código `TD-nn` nuevo, su `slug`, una semilla propia y, si hace falta, una función `mutate(row)` que modifique `value`, `deviceStatus`, `reportedType` o `frameFault`.
2. Ejecutar `node test-data/generate-test-data.mjs` y revisar el `expected` generado contra la especificación.
3. Actualizar la tabla de la §5 y, si corresponde, los ejemplos de HU-14.
4. No modificar a mano los archivos generados: se sobrescriben en cada ejecución.
