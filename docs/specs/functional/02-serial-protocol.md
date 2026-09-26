# 02 · Protocolo serial PC ↔ adquisidor (módulo Adquisición Serial)

| Campo | Valor |
|---|---|
| Versión del protocolo | **1.0** (propuesta) |
| Versión del documento | 0.4. Hasta 27 canales (D-06): máscara de `START` de 1 a 7 dígitos hexadecimales (la de 3 dígitos sigue siendo válida), `ChannelCount` y `Canal` de 1 a 27, y bloque `READ` de hasta 10 s. Sin cambio de la versión del protocolo (compatible con 1.0). 0.3. Duración planificada en lugar del tope de 721 muestras y falla de sesión por 30 min sin datos. 0.2: modo `Stream`, el grupo de sensores en `IDN`, el inicio de la sesión con la primera muestra recibida, la invalidación por cambio de adquisidor o de grupo, y el simulador. |
| Estado | Borrador. El hardware está en diseño y este documento es el contrato que deben cumplir tanto el firmware como el adquisidor simulado. |
| Relacionado | [01-vision-document.md](01-vision-document.md), [04-sequence-diagrams.md](04-sequence-diagrams.md), [05-data-model.md](05-data-model.md), [06-test-data.md](06-test-data.md) |

---

## 1. Principios de diseño

1. **Independiente del hardware.** Cualquier dispositivo (Arduino, Raspberry Pi, ESP32, el adquisidor simulado u otro) que implemente este contrato sirve como adquisidor. El sistema no depende del modelo de amplificador (MAX31855, MAX31856, MCP9600, etc.).
2. **Texto ASCII, una trama por línea.** Legible en un monitor serial, fácil de depurar y de guardar como evidencia (`Reading.RawFrame`).
3. **La PC marca el tiempo.** La PC asigna el número de muestra y la marca de tiempo. El adquisidor no necesita reloj de tiempo real ni memoria del estado de la sesión.
4. **Dos modos de adquisición.** En el modo `POLL` la PC pide cada muestra con `READ`. En el modo `STREAM` el adquisidor transmite un bloque cada 120 s por su cuenta. Este segundo modo cubre adquisidores que empiezan a medir y transmitir antes de conectarse a la PC. El adquisidor declara su modo en `IDN`.
5. **Validación extremo a extremo.** Cada trama lleva un checksum. Una trama que no lo supera nunca se interpreta como una temperatura.
6. **Idempotencia (modo POLL).** Repetir `READ` con el mismo número de muestra es seguro: el adquisidor vuelve a medir y responde con ese mismo número.

## 2. Parámetros del puerto

| Parámetro | Valor | Observación |
|---|---|---|
| Interfaz | Serial sobre USB (CDC/ACM) o UART-USB (CH340, FTDI, CP210x) | En Windows aparece como `COMn`. En Linux, como `/dev/ttyUSBn` o `/dev/ttyACMn`. El simulador usa un par de COM virtuales o un transporte en memoria ([06-test-data.md §3](06-test-data.md#3-adquisidor-simulado)). |
| Velocidad | **115 200 baudios** | Obligatoria en la v1.0. El tráfico real es muy bajo (< 1 kB cada 2 min). |
| Bits de datos | 8 | |
| Paridad | Ninguna | |
| Bits de parada | 1 | Configuración "8N1". |
| Control de flujo | Ninguno | |
| Codificación | ASCII de 7 bits (0x20–0x7E) más el fin de línea | Sin acentos ni caracteres extendidos. |
| Fin de línea | El adquisidor envía **CR LF** (`\r\n`). La PC envía **LF** (`\n`). | Ambos extremos **deben aceptar** `\n` o `\r\n` al recibir, e ignorar un `\r` suelto. |
| Longitud máxima de trama | **200 caracteres** sin contar el fin de línea | Coincide con `Reading.RawFrame VARCHAR(200)`. Las líneas más largas se descartan como corruptas. |
| Separador decimal | Punto (`.`) | En la interfaz y en el Excel se muestra según la configuración regional (p. ej. `-4,9`). |
| Separador de campos | Coma (`,`) | Los campos no pueden contener comas. |

> **Nota (Arduino):** muchas placas se reinician cuando la PC abre el puerto (señal DTR). Por eso la PC, después de abrir el puerto, espera la trama `BOOT` o un máximo de **3 s** antes de enviar `IDN`.
>
> **Datos previos a la conexión:** al abrir el puerto, la PC **descarta** el búfer de entrada y toda línea incompleta, y no almacena como lectura nada de lo recibido antes de que el técnico pulse "Iniciar captura" (ver §6.4).

## 3. Estructura de la trama

```
$<CMD>[,<campo1>,<campo2>,...]*<CS><fin de línea>
```

| Elemento | Descripción |
|---|---|
| `$` | Inicio de trama. Todo lo recibido antes de `$` en la misma línea se descarta. |
| `<CMD>` | Identificador del mensaje en mayúsculas (`IDN`, `RD`, `EOS`...). |
| `,<campo>` | Campos posicionales. Un campo vacío (`,,`) indica que no hay valor. |
| `*` | Separador del checksum. |
| `<CS>` | **Checksum**: XOR de todos los bytes entre `$` y `*` (sin incluirlos), expresado en 2 dígitos hexadecimales **en mayúsculas**. Es el mismo esquema que NMEA-0183. |

Cálculo de referencia (pseudocódigo):

```text
cs = 0
para cada byte b entre '$' y '*' (sin incluirlos):
    cs = cs XOR b
checksum = hex2(cs)   // p. ej. 0x51 -> "51"
```

Ejemplo: `$RD,16,1,T,-18.25,OK*51` (el XOR de `RD,16,1,T,-18.25,OK` es 0x51).

> Se eligió XOR de 8 bits por ser trivial de implementar en cualquier microcontrolador. Detecta todos los errores de un solo bit y la mayoría de los errores de ráfaga típicos de una línea serial corta. Si en pruebas de campo se observan tramas corruptas no detectadas, la v2.0 pasará a CRC-16/CCITT con 4 dígitos hexadecimales. El campo `ProtocolVersion` de `IDN` permite negociar ese cambio.

## 4. Mensajes

### 4.1 Comandos PC → adquisidor

| Comando | Formato | Respuesta esperada | Modos | Descripción |
|---|---|---|---|---|
| **Identificar** | `$IDN*43` | `IDN` | Ambos | Solicita la identidad, las capacidades y el grupo de sensores. Se puede enviar en cualquier estado. |
| **Iniciar** | `$START,<Mascara>*CS` | `ACK,START` o `NAK,START,<Err>` | Ambos | Arma el adquisidor para los canales indicados. `Mascara`: de 1 a 7 dígitos hexadecimales (27 bits como máximo; los ceros a la izquierda son opcionales). El bit 0 corresponde al canal 1 y el bit 26 al canal 27. Ej.: canales 1–5 → `01F`; canales 1–12 → `FFF`; canales 1–27 → `7FFFFFF`. En `STREAM` limita los canales transmitidos. Si no se recibe, el adquisidor transmite todos sus canales. |
| **Leer muestra** | `$READ,<Muestra>*CS` | *k* tramas `RD` + 1 trama `EOS`, o `NAK,READ,<Err>` | Solo `POLL` | Pide medir ahora todos los canales armados. `Muestra`: entero desde 1, asignado por la PC (31 en la sesión base de 1 h; el máximo depende de la duración planificada). En `STREAM` responde `NAK,READ,E02`. |
| **Latido** | `$PING*10` | `ACK,PING` | Ambos | Comprueba el enlace entre muestras. |
| **Detener** | `$STOP*18` | `ACK,STOP` | Ambos | Desarma el adquisidor. En `POLL`, después `READ` responde `NAK,READ,E04`. En `STREAM`, el adquisidor puede seguir transmitiendo, pero la PC ya no almacena nada. |

### 4.2 Mensajes adquisidor → PC

| Mensaje | Formato | Descripción |
|---|---|---|
| **Arranque** | `$BOOT,<DeviceId>,<Firmware>*CS` | Espontáneo, al encender o reiniciarse. Si llega durante una sesión en curso, indica que el adquisidor se reinició y perdió el armado: la PC reenvía `START`. |
| **Identidad** | `$IDN,<ProtocolVersion>,<DeviceId>,<Platform>,<Firmware>,<ChannelCount>,<Mode>,<SensorGroupId>*CS` | Respuesta a `IDN`. |
| **Lectura** | `$RD,<Muestra\|Seq>,<Canal>,<Tipo>,<TempC>,<Estado>*CS` | Una trama por canal. En `POLL` responde a `READ` y el primer campo repite el número de muestra. En `STREAM` se emite sola y el primer campo es el contador de bloques del adquisidor (`Seq`). |
| **Fin de bloque** | `$EOS,<Muestra\|Seq>,<NumTramas>*CS` | Cierra el bloque. `NumTramas` indica cuántas tramas `RD` se enviaron. |
| **Confirmación** | `$ACK,<Comando>*CS` | El comando se aceptó. |
| **Rechazo** | `$NAK,<Comando>,<Err>*CS` | El comando se rechazó. Ver §4.5. |

### 4.3 Campos

| Campo | Formato | Reglas |
|---|---|---|
| `ProtocolVersion` | `\d+\.\d+` | La PC acepta una versión mayor `1`. Con otra versión mayor rechaza el adquisidor (incompatible). |
| `DeviceId` | `[A-Z0-9-]{1,50}` | Único y persistente (grabado en la EEPROM o en un archivo). Se corresponde con `AcquisitionDevice.DeviceIdentifier`. Ej.: `ADQ-ARD-0001`. El simulador usa `ADQ-SIM-nnnn`. |
| `Platform` | `Arduino` \| `RaspberryPi` \| `Other` \| `Simulator` | Se corresponde con `AcquisitionDevice.Platform`. `Simulator` marca la sesión como simulación. |
| `Firmware` | `[0-9A-Za-z.\-]{1,20}` | Se corresponde con `AcquisitionDevice.FirmwareVersion`. |
| `ChannelCount` | `1`…`27` | Canales físicos disponibles. Se corresponde con `AcquisitionDevice.ChannelCount`. |
| `Mode` | `POLL` \| `STREAM` | Modo de adquisición. Se corresponde con `AcquisitionDevice.AcquisitionMode` (`Poll`/`Stream`). |
| `SensorGroupId` | `[A-Z0-9-]{0,50}` | Identificador del grupo de sensores conectado (arnés o placa de termopares). **Puede ir vacío** si el hardware no lo soporta. Se guarda en `MeasurementSession.SensorGroupId`. |
| `Muestra` | `1`…`99999` | Modo `POLL`: eco del número recibido en `READ`. |
| `Seq` | `1`…`65535` | Modo `STREAM`: contador de bloques del adquisidor. Da la vuelta a 1 después de 65535. Solo se usa para detectar bloques repetidos o saltados. **No** es el número de muestra. |
| `Canal` | `1`…`27` | Número de canal físico. |
| `Tipo` | `T` \| `K` | Tipo de termopar **configurado en el adquisidor** para ese canal (jumper, configuración o módulo). Se corresponde con `Reading.ReportedThermocoupleType`. |
| `TempC` | `-?\d{1,4}\.\d{1,2}` | Temperatura en °C con **1 o 2 decimales**, punto decimal y signo solo si es negativa. **Vacío** cuando `Estado` ≠ `OK`. |
| `Estado` | `OK` \| `OC` \| `SC` \| `OR` | Diagnóstico del sensor. Ver §4.4. |

### 4.4 Códigos de estado del sensor

| Código en la trama | Significado | `Reading.SensorStatus` | Origen |
|---|---|---|---|
| `OK` | Lectura válida | `OK` | Adquisidor |
| `OC` | Termopar abierto (desconectado o roto) | `OpenCircuit` | Adquisidor |
| `SC` | Cortocircuito a GND o VCC | `ShortCircuit` | Adquisidor |
| `OR` | Fuera del rango que puede medir el amplificador | `OutOfRange` | Adquisidor |
| — | Valor fuera del rango físico del tipo **declarado** (`ThermocoupleType.MinRangeC`–`MaxRangeC`) | `OutOfRange` | PC |
| — | Trama corrupta, incompleta o ausente para un canal armado | `InvalidFrame` | PC |
| — | Tipo reportado distinto del tipo declarado en la sesión | `TypeMismatch` | PC |

Para el umbral de pérdida de sensores ([01 RN-14](01-vision-document.md#6-reglas-de-negocio-principales)) solo cuenta como dato válido una lectura `OK`.

### 4.5 Códigos de error (`NAK`)

| Código | Significado |
|---|---|
| `E01` | El checksum del comando recibido no es válido. |
| `E02` | Comando desconocido o no disponible en el modo actual (p. ej. `READ` en `STREAM`). |
| `E03` | Parámetro inválido (formato o rango). |
| `E04` | El adquisidor no está armado (se recibió `READ` sin `START` previo). |
| `E05` | La máscara incluye canales que no existen físicamente. |
| `E06` | Fallo interno del adquisidor (bus SPI/I²C, amplificador sin respuesta). |

## 5. Temporización y numeración de muestras

| Parámetro | Valor | Uso |
|---|---|---|
| Intervalo de muestreo **[PC-01]** | 120 s (`MeasurementSession.SamplingIntervalSeconds`, copiado del parámetro del sistema al crear la sesión) | |
| Inicio de la sesión | Primera muestra con al menos una trama `RD` válida de un canal activo, recibida **después** de pulsar "Iniciar captura" | Esa muestra es la número 1 y su instante es `StartedAt` (t = 0). Ver §6.4. |
| Programación (modo `POLL`) | La muestra *n* se solicita en `StartedAt + (n − 1) × 120 s` | Se calcula desde el inicio para no acumular deriva. |
| Asignación (modo `STREAM`) | Un bloque recibido en el instante *t* se asigna a la muestra `n = 1 + round((t − StartedAt) / 120 s)` | Si llegan dos bloques para la misma muestra se conserva el primero y el otro se anota en el log. |
| Tolerancia de llegada (`STREAM`) | ± intervalo / 4 (± 30 s con 120 s) respecto del instante programado | Si no llega ningún bloque en la ventana y el enlace sigue vivo (responde `PING`), todos los canales de esa muestra se registran como `InvalidFrame`. |
| Espera tras abrir el puerto | hasta 3 s o la llegada de `BOOT` | §2, nota Arduino. |
| Tiempo máximo de respuesta a `IDN`, `START`, `STOP` y `PING` | 2 s | |
| Tiempo máximo del bloque `READ` (hasta `EOS`) | 10 s | Con 27 canales y conversiones de ≈ 100–250 ms por canal (≈ 6,8 s en el peor caso). |
| Intentos por muestra (`POLL`) | 3 (1 + 2 reintentos, cada 10 s) | Ver §7.1. En `STREAM` no hay reintentos. |
| Latido | `PING` cada 30 s entre muestras | Detecta la pérdida del enlace antes de la siguiente muestra. |
| Umbral de pérdida de comunicación | 3 fallos consecutivos (sin respuesta válida a `READ` o `PING`) **o** error del sistema operativo en el puerto (dispositivo retirado o puerto cerrado) | Abre un `CommunicationGap`. |
| Reintento de reconexión | cada 10 s | Hasta que se recupera la comunicación, el técnico cierra la sesión, se cumplen 30 min sin datos (sesión fallida, 01 RN-15) o se llega a la duración planificada. |
| Última muestra | La que corresponde a la duración planificada: 31 en la sesión base de 1 h, 721 en 24 h | Tras almacenarla, la sesión se cierra automáticamente (`CloseReason` = `PlannedDuration`). |

**Marcas de tiempo:** `Reading.ReadAt` es el instante programado de la muestra. En `POLL` es el envío del primer `READ` de la muestra, y en `STREAM` la recepción de la primera trama del bloque, en ambos casos truncado al segundo. Todas las lecturas de una muestra comparten el mismo `ReadAt`. `Reading.ReceivedAt` guarda el instante de recepción de cada trama, con milisegundos.

## 6. Flujos del protocolo

### 6.1 Detección e identificación

```text
PC  -> (abre COM3, 115200 8N1, descarta el búfer de entrada)
ADQ -> $BOOT,ADQ-ARD-0001,1.2.0*27
PC  -> $IDN*43
ADQ -> $IDN,1.0,ADQ-ARD-0001,Arduino,1.2.0,10,POLL,GRP-A*0C
```

La PC busca `DeviceId` en `AcquisitionDevice`. Si no existe, lo registra con `Platform`, `FirmwareVersion`, `ChannelCount` y `AcquisitionMode`. Si existe y el firmware cambió, actualiza `FirmwareVersion`. Además comprueba que todos los canales configurados en la sesión sean ≤ `ChannelCount`, y guarda `SensorGroupId` para compararlo en una reconexión.

Ejemplo de un adquisidor en modo `STREAM` que no informa grupo de sensores: `$IDN,1.0,ADQ-RPI-0007,RaspberryPi,0.9.1,8,STREAM,*33`.

### 6.2 Ciclo de muestra en modo POLL (5 canales, límite -5,00 °C)

Muestra 16, programada en t = 15 × 120 s = 30 min:

```text
PC  -> $READ,16*39
ADQ -> $RD,16,1,T,-18.25,OK*51     -> OK, -18,25 <= -5,00: cumple
ADQ -> $RD,16,2,T,-17.9,OK*63      -> OK, -17,90: cumple
ADQ -> $RD,16,3,K,-18.40,OK*4F     -> el canal 3 se declaró T: TypeMismatch + alerta
ADQ -> $RD,16,4,T,,OC*51           -> OpenCircuit, TempC nula + alerta SensorFault
ADQ -> $RD,16,5,T,-4.90,OK*66      -> OK, -4,90 > -5,00: IsAboveLimit = 1 + alerta AboveLimit
ADQ -> $EOS,16,5*6B
```

Solo 2 de los 5 canales (40 %) quedan sin lectura `OK`. No se supera el umbral del 60 %, así que la muestra 16 es **válida**.

Caso límite exacto: `$RD,32,5,T,-5.00,OK*68` cumple, porque -5,00 no es > -5,00. `$RD,32,5,T,-4.90,OK*60` está fuera de límite.

### 6.3 Ciclo de muestra en modo STREAM

```text
ADQ -> $RD,1040,1,T,-18.25,OK*53      (sin petición de la PC)
ADQ -> ...
ADQ -> $EOS,1040,5*69
```

La PC asigna el bloque a la muestra según el instante de llegada (§5). El `Seq` 1040 no se almacena como número de muestra. Solo queda en `RawFrame`.

### 6.4 Inicio de la sesión cuando llegan datos

1. El técnico configura la sesión y la aplicación detecta el adquisidor (§6.1). En modo `STREAM` los bloques que ya llegan se muestran como **vista previa** y no se almacenan.
2. El técnico pulsa "Iniciar captura" y confirma las advertencias (mezcla T/K, límite no definido). La sesión pasa a **"Esperando datos"**, aunque sigue en estado `Configured` en la base.
3. La PC envía `START,<Mascara>`. Luego:
   - En `POLL` envía de inmediato `READ,1`. Si no llega ninguna trama `RD` válida de un canal activo, reintenta cada 10 s **sin** consumir números de muestra.
   - En `STREAM` espera el siguiente bloque completo. Descarta cualquier bloque que haya empezado antes de pulsar "Iniciar".
4. Con el primer bloque que tenga al menos una trama `RD` válida de un canal activo, la sesión pasa a `Running`, se fija `StartedAt` = instante de esa muestra, y se almacenan sus lecturas como muestra 1.
5. Si en 5 min no llegan datos, la aplicación avisa al técnico, que puede seguir esperando o cancelar.

### 6.5 Detención

```text
PC  -> $STOP*18
ADQ -> $ACK,STOP*7D
```

## 7. Validación de tramas en la PC

Cada línea recibida pasa por las siguientes validaciones, en este orden. La primera que falla determina el resultado.

| Paso | Validación | Si falla |
|---|---|---|
| V1 | Longitud ≤ 200, solo caracteres ASCII imprimibles, empieza con `$` y contiene `*` seguido de 2 dígitos hexadecimales | Trama corrupta: se registra en el log de comunicación y se aplica §7.1. |
| V2 | El checksum calculado es igual a `<CS>` | Trama corrupta (§7.1). |
| V3 | `CMD` conocido y número de campos correcto para ese `CMD` | Trama corrupta (§7.1). |
| V4 | Formato de cada campo según §4.3 | Trama corrupta (§7.1). |
| V5 | `RD`/`EOS` en `POLL`: `Muestra` igual a la muestra en curso. En `STREAM`: el bloque cae en la ventana de una muestra aún no recibida. | Trama **tardía o duplicada**: se descarta sin registrar lectura. |
| V6 | `RD`: `Canal` pertenece a los canales activos de la sesión | Se descarta y se anota en el log. No es una lectura de la sesión. |
| V7 | `RD`: `Estado` ≠ `OK` | Lectura con `SensorStatus` = `OpenCircuit`/`ShortCircuit`/`OutOfRange`, `TemperatureC` = NULL. |
| V8 | `RD`: `Tipo` igual a `SessionChannel.ThermocoupleTypeCode` | Lectura `TypeMismatch` (§8). |
| V9 | `RD`: `TempC` dentro de [`MinRangeC`, `MaxRangeC`] del tipo declarado | Lectura `OutOfRange`, `TemperatureC` = NULL. |
| V10 | `RD` válida | Lectura `OK`. Si `MeasurementSession.MaxTemperatureC` no es NULL y `TempC > MaxTemperatureC`: `IsAboveLimit` = 1 y alerta `AboveLimit`. |

En todos los casos en que se registra una lectura, `Reading.RawFrame` guarda la línea tal como se recibió, sin el fin de línea.

Después de registrar todas las lecturas de la muestra, la PC evalúa la **pérdida de sensores**: si `(canales activos − lecturas OK) × 100 > SensorLossThresholdPct × canales activos`, la muestra queda afectada. Si además es la primera de un episodio, se genera la advertencia `SensorLoss` (01 RN-14). Si sigue afectada en la 3.ª muestra consecutiva, se genera la alerta crítica `SensorLossPersistent`. Si pasan 30 min consecutivos, la sesión falla (01 §6.2).

### 7.1 Manejo de tramas corruptas y faltantes

1. Una trama corrupta **nunca** produce un valor de temperatura.
2. Al recibir `EOS` (o al vencer los 5 s), la PC comprueba si todos los canales activos tienen una trama `RD` válida para la muestra en curso.
3. **Modo POLL:** si falta algún canal (por trama corrupta, `EOS` ausente o `NumTramas` distinto de lo recibido), la PC **reintenta** `READ` con el **mismo** número de muestra, hasta 3 intentos en total. Para cada canal se conserva la primera trama válida que se reciba, sea del intento que sea.
4. **Modo STREAM:** no hay reintentos. Un canal sin trama válida en el bloque se registra directamente como `InvalidFrame`.
5. Si después del último intento un canal sigue sin trama válida, pero el adquisidor respondió en algún intento (el enlace está vivo), se registra una lectura `InvalidFrame` con `TemperatureC` = NULL. `RawFrame` guarda la última trama corrupta atribuible a ese canal, si se pudo leer su campo `Canal`, o queda NULL.
6. Si en los 3 intentos no llegó **ninguna** trama válida de ningún tipo, no se registran lecturas: la situación se trata como **pérdida de comunicación** (§9).
7. Si llega `NAK,READ,E04` (el adquisidor no está armado, p. ej. después de un reinicio), la PC reenvía `START` y repite `READ` dentro de los mismos 3 intentos.
8. Si llega `NAK,<cmd>,E01`, la PC reenvía el comando. Cuenta como un intento.

Un canal puede tener como máximo **una** lectura por muestra (`UQ_Reading_Channel_Sample`).

## 8. Verificación del tipo de termopar

- Cada trama `RD` informa el tipo configurado en el adquisidor para ese canal. La PC lo compara con el tipo declarado por el técnico en `SessionChannel.ThermocoupleTypeCode`.
- **Si no coincide:**
  - La lectura se registra con `SensorStatus` = `TypeMismatch`, `ReportedThermocoupleType` = tipo informado y `TemperatureC` = valor informado. El valor se conserva como evidencia, pero **no es confiable**: el amplificador linealizó con la curva de otro tipo.
  - **No** se evalúa el límite (`IsAboveLimit` = 0), y la lectura cuenta como "sin dato válido" para el umbral de pérdida de sensores.
  - Se genera una alerta `TypeMismatch` (severidad `Warning`) con el canal, la lectura y el mensaje: *"Canal 3: el adquisidor reporta termopar tipo K pero la sesión declara tipo T."*
  - La alerta se genera **por episodio**: con la primera lectura no coincidente y otra vez si el canal vuelve a no coincidir después de haber coincidido.
- Durante la configuración (antes de iniciar) la aplicación puede hacer una lectura de prueba (en `POLL`, `READ` sin almacenar; en `STREAM`, la vista previa) para mostrar al técnico el tipo informado por canal y advertir las diferencias antes de empezar. Es recomendable, pero no bloquea el inicio.
- La **huella de tipos** de la sesión es la secuencia de tipos informados por canal en la muestra 1 (p. ej. `T,T,T,T,K`). Se usa en la reconexión cuando el adquisidor no informa `SensorGroupId` (§9).

## 9. Pérdida y recuperación de la comunicación

1. **Detección:** error del sistema operativo en el puerto (dispositivo USB retirado, `IOException`) **o** 3 fallos consecutivos (sin respuesta válida a `READ` o `PING`).
2. **Registro:** se crea `CommunicationGap` con `LostAt` = instante del primer fallo del episodio y una alerta `CommunicationLost` (severidad `Critical`). La sesión **sigue** en estado `Running`. Las muestras programadas mientras dura el hueco quedan **perdidas** y cuentan como afectadas (01 RN-14).
3. **Reconexión:** cada 10 s la PC intenta abrir el puerto. Si el puerto ya no existe (Windows reenumeró el dispositivo), busca en los puertos disponibles uno cuya respuesta a `IDN` tenga el mismo `DeviceId`.
4. **Validación de identidad:** al reconectar, la PC envía `IDN` y compara:

   | Comprobación | Si no coincide |
   |---|---|
   | `DeviceId` igual al de la sesión | **Otro adquisidor** |
   | `SensorGroupId` igual al de la sesión (si la sesión tiene uno) | **Otro grupo de sensores** |
   | `ChannelCount` ≥ canal activo más alto | **Otro adquisidor** |
   | Si la sesión no tiene `SensorGroupId`: la huella de tipos del primer bloque tras reconectar coincide con la de la muestra 1 | **Otro grupo de sensores** |

   Si todo coincide, la PC reenvía `START` con la misma máscara y **reanuda**.

   Si algo no coincide, la sesión se declara **fallida** (no válida, 01 RN-15):
   - `Status` = `Invalid`, `CloseReason` = `DeviceMismatch`, `EndedAt` = instante de la comprobación.
   - Alerta `DeviceMismatch` (severidad `Critical`) con el identificador y el grupo encontrados.
   - El hueco se cierra con `RecoveredAt` = NULL, `MissedSamples` contadas hasta `EndedAt`, y una nota con lo que respondió.
   - La PC envía `STOP` al dispositivo conectado.
5. **Reanudación:** se completa `CommunicationGap.RecoveredAt` y `MissedSamples` (número de muestras programadas entre `LostAt` y `RecoveredAt` sin lecturas). La captura sigue en la **siguiente muestra programada**, sin recuperar ni renumerar las perdidas. La numeración de muestras refleja siempre el tiempo transcurrido desde el inicio.
6. Las muestras perdidas durante el hueco cuentan como afectadas consecutivas (01 §6.2). Si el hueco dura **30 min**, la sesión se declara **fallida** (Invalid, CloseReason = DataLoss, alerta SessionFailed) y se deja de intentar la reconexión.
7. Si se llega a la duración planificada con el hueco abierto, la sesión se cierra (CloseReason = PlannedDuration) y el hueco se cierra con RecoveredAt = NULL y MissedSamples calculado hasta el cierre.

## 10. Requisitos para el firmware y el simulador (resumen de conformidad)

| # | Requisito |
|---|---|
| F-01 | Implementar `IDN`, `START`, `PING` y `STOP` con los formatos y tiempos de este documento. En modo `POLL`, también `READ`. |
| F-02 | Emitir `BOOT` al arrancar. |
| F-03 | Calcular y verificar el checksum XOR. Responder `NAK,<cmd>,E01` ante un comando con checksum inválido. |
| F-04 | `DeviceId` único y persistente entre reinicios. |
| F-05 | Informar en cada `RD` el tipo de termopar configurado para el canal. |
| F-06 | Hacer la compensación de unión fría y la linealización, y entregar °C con 1 o 2 decimales. |
| F-07 | Diagnosticar `OC`, `SC` y `OR` usando los bits de falla del amplificador. |
| F-08 | En modo `POLL`, no enviar tramas `RD` espontáneas: solo en respuesta a `READ`. |
| F-09 | Enviar el bloque `RD`…`EOS` completo en menos de 10 s, con hasta 27 canales. |
| F-10 | No usar caracteres fuera de ASCII imprimible ni líneas de más de 200 caracteres. |
| F-11 | Informar en `IDN` el modo (`POLL`/`STREAM`) y, si el hardware lo permite, el `SensorGroupId` del grupo conectado. |
| F-12 | En modo `STREAM`, emitir un bloque cada 120 s ± 2 s con un `Seq` creciente, y seguir respondiendo `IDN`, `START`, `PING` y `STOP` entre bloques. |
| F-13 | El adquisidor simulado cumple F-01 a F-12 con `Platform` = `Simulator` y reproduce exactamente la transcripción de referencia de cada escenario ([06-test-data.md](06-test-data.md)). |

## 11. Evolución prevista

- v1.1: comando opcional `CFG?` para consultar el tipo configurado por canal sin medir.
- v2.0: CRC-16 en lugar de XOR si las pruebas de campo lo justifican; comando `TIME` y almacenamiento local en el adquisidor para rellenar huecos de comunicación con datos marcados en el adquisidor.
