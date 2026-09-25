# 04 · Diagramas de secuencia

| Campo | Valor |
|---|---|
| Versión | 0.2 (borrador para revisión) |
| Cambios en 0.2 | Inicio con la primera muestra recibida, modo `STREAM`, evaluación de la pérdida de sensores, invalidación por cambio de adquisidor o de grupo, cierre según las muestras válidas y el nuevo diagrama 5 (sesión simulada). |
| Relacionado | [02-serial-protocol.md](02-serial-protocol.md), [03-user-stories.md](03-user-stories.md), [05-data-model.md](05-data-model.md), [06-test-data.md](06-test-data.md) |

Participantes comunes:

| Participante | Descripción |
|---|---|
| Técnico | Usuario con el rol `Technician`. |
| UI | Interfaz de la aplicación de captura. |
| Sesión | Servicio de sesión de medición: reglas de negocio, ciclo de vida, alertas, pérdida de sensores. |
| Serial | Servicio de adquisición serial: puerto, protocolo, validación de tramas, programador de muestreo. |
| ADQ | Adquisidor (Arduino, Raspberry Pi o simulado) conectado por el puerto COM. |
| BD | SQL Server 2022, base `ThermalCalibration`. |
| Excel | Generador del archivo .xlsx. |

Los checksums de las tramas se abrevian como `*CS`. Los valores reales están en [02-serial-protocol.md](02-serial-protocol.md).

---

## 1. Configuración, detección del adquisidor, advertencia de mezcla e inicio al llegar datos

Cubre HU-03, HU-04, HU-05 y el inicio de HU-06.

```mermaid
sequenceDiagram
    autonumber
    actor Tec as Técnico
    participant UI
    participant Ses as Sesión
    participant Ser as Serial
    participant ADQ as Adquisidor
    participant BD

    Tec->>UI: Selecciona empresa y equipo
    UI->>Ses: Obtener equipo y tipo de equipo
    Ses->>BD: SELECT Equipment + EquipmentType
    BD-->>Ses: Congeladora, límite vigente -5,00
    Tec->>UI: Selecciona puerto COM3
    UI->>Ser: Detectar adquisidor en COM3
    Ser->>Ses: ¿COM3 libre?
    Ses->>BD: Buscar sesión Running con ComPort = COM3
    BD-->>Ses: Ninguna
    Ser->>ADQ: Abrir puerto 115200 8N1 y descartar el búfer de entrada
    ADQ-->>Ser: $BOOT,ADQ-ARD-0001,1.2.0*CS
    Ser->>ADQ: $IDN*43
    alt Responde dentro de 2 s
        ADQ-->>Ser: $IDN,1.0,ADQ-ARD-0001,Arduino,1.2.0,10,POLL,GRP-A*CS
        Ser->>Ser: Validar checksum y versión 1.x
        Ser->>BD: Buscar AcquisitionDevice por DeviceIdentifier
        opt Adquisidor no registrado
            Ser->>BD: INSERT AcquisitionDevice con Platform y AcquisitionMode
        end
        Ser-->>UI: Adquisidor detectado, 10 canales, modo POLL, grupo GRP-A
    else Sin respuesta o versión incompatible
        Ser-->>UI: Error de detección
        UI-->>Tec: No se detectó un adquisidor compatible en COM3
    end
    opt Modo STREAM
        ADQ-->>Ser: Bloques que ya se estaban transmitiendo
        Ser-->>UI: Vista previa, sin almacenar
    end

    Tec->>UI: Asigna canales 1-5 con tipo y ubicación
    UI->>Ses: Guardar configuración
    Ses->>Ses: Validar 1 a 10 canales, sin duplicados, dentro de ChannelCount
    Ses->>BD: INSERT MeasurementSession Status Configured, SensorGroupId GRP-A
    Ses->>BD: INSERT SessionChannel x5

    Tec->>UI: Iniciar captura
    UI->>Ses: Solicitar inicio
    Ses->>Ses: ¿Canales con tipos T y K a la vez?
    alt Hay mezcla de tipos T y K
        Ses->>BD: UPDATE HasMixedThermocoupleTypes = 1
        Ses->>BD: INSERT Alert MixedThermocoupleTypes, Warning
        Ses-->>UI: Advertencia de mala práctica
        UI-->>Tec: Mezcla T y K no recomendada. ¿Confirma?
        break Técnico elige Volver y corregir
            UI-->>Tec: Sesión sigue en Configured, alerta sin reconocer
        end
        Tec->>UI: Confirmar e iniciar
        UI->>Ses: Confirmación de mezcla
        Ses->>BD: UPDATE Alert AcknowledgedById, AcknowledgedAt
        Ses->>BD: UPDATE MixedTypesAcknowledgedAt
    end
    opt Tipo de equipo sin límite definido
        Ses->>BD: INSERT Alert LimitNotDefined, Info
        UI-->>Tec: Se capturará sin evaluar el límite
    end
    Ses->>Ser: Esperar la primera muestra
    UI-->>Tec: Esperando datos
    Ser->>ADQ: $START,01F*CS
    ADQ-->>Ser: $ACK,START*CS
    loop Hasta recibir un bloque con al menos una trama RD válida
        alt Modo POLL
            Ser->>ADQ: $READ,1*CS
            ADQ-->>Ser: $RD,1,... y $EOS,1,5
        else Modo STREAM
            ADQ-->>Ser: Siguiente bloque completo $RD,Seq,... y $EOS
        end
    end
    Ser->>Ses: Primera muestra recibida en t0
    Ses->>BD: UPDATE Status Running, StartedAt = t0, MaxTemperatureC y SensorLossThresholdPct = copias vigentes
    Note over Ses,BD: CK_MeasurementSession_MixedAck rechaza Running<br/>si hay mezcla sin confirmación
    Ses->>BD: INSERT Reading de la muestra 1
    Ses->>Ses: Guardar la huella de tipos de la muestra 1
    Ser-->>UI: Captura en curso desde t0
```

---

## 2. Ciclo de captura cada 2 minutos

Cubre HU-06, HU-07, HU-08 y HU-13. Validaciones V1–V10 de [02-serial-protocol.md §7](02-serial-protocol.md#7-validación-de-tramas-en-la-pc).

```mermaid
sequenceDiagram
    autonumber
    participant Ser as Serial
    participant ADQ as Adquisidor
    participant Ses as Sesión
    participant BD
    participant UI

    loop Cada muestra n de 2 a 721, en StartedAt + (n - 1) x 120 s
        alt Modo POLL
            Ser->>ADQ: $READ,n*CS
            Note right of Ser: ReadAt = hora de la PC al enviar
            loop Cada canal armado
                ADQ-->>Ser: $RD,n,canal,tipo,temp,estado*CS
                Ser->>Ser: V1-V6 formato, checksum y muestra
            end
            ADQ-->>Ser: $EOS,n,k*CS
            opt Faltan canales y quedan intentos, máximo 3
                Ser->>ADQ: $READ,n*CS de nuevo
                ADQ-->>Ser: $RD ... y $EOS
            end
        else Modo STREAM
            ADQ-->>Ser: $RD,Seq,... y $EOS,Seq,k sin petición
            Ser->>Ser: n = 1 + round((t - StartedAt) / 120)
            Note right of Ser: Sin reintentos. Canal sin trama válida = InvalidFrame
        end
        Ser->>Ses: Resultados de la muestra n por canal
        loop Cada canal activo
            alt Sin trama válida
                Ses->>BD: INSERT Reading InvalidFrame, TemperatureC NULL
            else Estado del adquisidor OC, SC u OR
                Ses->>BD: INSERT Reading OpenCircuit, ShortCircuit u OutOfRange
            else Tipo reportado distinto del declarado
                Ses->>BD: INSERT Reading TypeMismatch con valor informado
            else Valor fuera del rango del tipo declarado
                Ses->>BD: INSERT Reading OutOfRange, TemperatureC NULL
            else Lectura OK
                alt Límite definido y TempC mayor que MaxTemperatureC
                    Ses->>BD: INSERT Reading OK, IsAboveLimit = 1
                    Ses->>BD: INSERT Alert AboveLimit con ReadingId, valor y límite
                else Dentro del límite o sin límite
                    Ses->>BD: INSERT Reading OK, IsAboveLimit = 0
                end
            end
            opt Inicio de episodio de falla o de tipo no coincidente
                Ses->>BD: INSERT Alert SensorFault o TypeMismatch
            end
        end
        Ses->>Ses: sinDato = canales activos - lecturas OK
        alt sinDato x 100 mayor que umbral x canales activos
            Ses->>Ses: Muestra n afectada
            opt Primera muestra afectada del episodio
                Ses->>BD: INSERT Alert SensorLoss, Critical
            end
        else
            Ses->>Ses: Muestra n válida
        end
        Ses-->>UI: Actualizar monitor, contadores de muestras válidas y afectadas, alertas
        opt n = 721
            Ses->>Ses: Cierre automático, ver diagrama 4
        end
    end

    loop Cada 30 s entre muestras
        Ser->>ADQ: $PING*10
        ADQ-->>Ser: $ACK,PING*CS
    end
```

Ejemplos:

- Evaluación del límite con `MaxTemperatureC = -5,00`: una lectura de `-5,00` se inserta con `IsAboveLimit = 0`, y una de `-4,90` con `IsAboveLimit = 1`, más una alerta `AboveLimit`.
- Pérdida de sensores con 10 canales y umbral del 60 %: 6 canales sin dato (6 × 100 = 600, que no es mayor que 60 × 10 = 600) dejan la muestra válida. Con 7 canales sin dato (700 > 600) la muestra queda afectada.

---

## 3. Pérdida y recuperación de la comunicación serial

Cubre HU-09. Ver [02-serial-protocol.md §9](02-serial-protocol.md#9-pérdida-y-recuperación-de-la-comunicación).

```mermaid
sequenceDiagram
    autonumber
    participant Ser as Serial
    participant ADQ as Adquisidor
    participant Ses as Sesión
    participant BD
    participant UI

    Ser->>ADQ: $READ,64*CS
    ADQ--xSer: Sin respuesta o error del puerto
    alt Error del sistema operativo en el puerto
        Ser->>Ser: Pérdida inmediata
    else Timeout
        Ser->>ADQ: Reintentos hasta completar 3 fallos consecutivos
        ADQ--xSer: Sin respuesta
    end
    Ser->>Ses: Comunicación perdida desde LostAt
    Ses->>BD: INSERT CommunicationGap LostAt, RecoveredAt NULL
    Ses->>BD: INSERT Alert CommunicationLost, Critical
    Ses-->>UI: Sin comunicación, reintentando
    Note over Ses: La sesión sigue en Running.<br/>Las muestras programadas quedan perdidas y afectadas.

    loop Cada 10 s hasta recuperar, invalidar, cerrar o llegar a 24 h
        Ser->>Ser: Abrir COM3 o buscar el DeviceId en otros puertos
        alt Puerto disponible
            Ser->>ADQ: $IDN*43
            ADQ-->>Ser: $IDN,1.0,DeviceId,...,Mode,SensorGroupId*CS
            Ser->>Ses: Validar identidad
            Ses->>Ses: Comparar DeviceId, SensorGroupId, ChannelCount<br/>o huella de tipos si no hay grupo
            alt Mismo adquisidor y mismo grupo de sensores
                Ser->>ADQ: $START,01F*CS
                ADQ-->>Ser: $ACK,START*CS
                Ses->>Ses: MissedSamples = muestras programadas sin lecturas en el hueco
                Ses->>BD: UPDATE CommunicationGap RecoveredAt, MissedSamples, Notes
                Ses-->>UI: Comunicación restablecida
                Ser->>ADQ: $READ,67*CS en la siguiente hora programada
            else Otro adquisidor u otro grupo de sensores
                Ser->>ADQ: $STOP*18
                Ses->>BD: UPDATE CommunicationGap MissedSamples hasta EndedAt, Notes
                Ses->>BD: INSERT Alert DeviceMismatch, Critical
                Ses->>BD: UPDATE MeasurementSession Status Invalid, CloseReason DeviceMismatch, EndedAt
                Ses-->>UI: Sesión no válida: adquisidor o grupo de sensores distinto
            end
        else Puerto no disponible
            Ser->>Ser: Esperar 10 s
        end
    end

    opt El técnico cierra la sesión con el hueco abierto
        Ses->>BD: UPDATE CommunicationGap MissedSamples hasta EndedAt
        Ses->>BD: UPDATE MeasurementSession CloseReason CommunicationLost
    end
```

---

## 4. Cierre de sesión y exportación a Excel

Cubre HU-10 y HU-11.

```mermaid
sequenceDiagram
    autonumber
    actor Tec as Técnico
    participant UI
    participant Ses as Sesión
    participant Ser as Serial
    participant ADQ as Adquisidor
    participant BD
    participant XLS as Excel

    alt Cierre manual
        Tec->>UI: Finalizar sesión
        UI->>Ses: Solicitar cierre
        Ses->>BD: SELECT muestras válidas en vSessionSampleCoverage
        Ses->>Ses: Duración = ahora - StartedAt
        alt Duración de 1 h o más y al menos 31 muestras válidas
            Ses->>Ses: Status Completed, CloseReason Manual
        else Duración menor a 1 h o menos de 31 muestras válidas
            Ses-->>UI: Quedará INCOMPLETA, indicando el motivo
            UI-->>Tec: ¿Confirma?
            break Técnico no confirma
                UI-->>Tec: La sesión sigue en curso
            end
            Tec->>UI: Confirmar
            Ses->>Ses: Status Incomplete, CloseReason Manual
        end
    else Cierre automático al almacenar la muestra 721
        Ses->>BD: SELECT muestras válidas en vSessionSampleCoverage
        Ses->>Ses: Completed o Incomplete según las muestras válidas, CloseReason MaxDuration
    end
    Ses->>Ser: Detener muestreo
    Ser->>ADQ: $STOP*18
    ADQ-->>Ser: $ACK,STOP*CS
    Ser->>Ser: Cerrar puerto
    Ses->>BD: UPDATE MeasurementSession Status, CloseReason, EndedAt
    Note over BD: CK_MinDuration impide Completed con menos de 3600 s<br/>CK_MaxDuration impide más de 86400 s
    Ses-->>UI: Sesión cerrada
    UI-->>Tec: Resumen y botón Exportar a Excel

    Tec->>UI: Exportar a Excel y elegir carpeta
    UI->>XLS: Exportar sesión
    XLS->>BD: Consultar sesión, empresa, equipo, técnico, adquisidor, canales
    BD-->>XLS: Datos de la sesión
    alt Status Completed, Incomplete o Invalid
        XLS->>BD: SELECT vSessionSampleCoverage, vSessionReadingPivot + estados por lectura
        XLS->>BD: SELECT Alert, CommunicationGap
        BD-->>XLS: Muestras, lecturas, alertas y huecos
        XLS->>XLS: Hoja Resumen con avisos de no válida, incompleta, mezcla, pérdida de sensores, sin límite o simulación
        XLS->>XLS: Hoja Lecturas con estado de cada muestra y celdas resaltadas
        XLS->>XLS: Hojas Alertas y Comunicación con huecos y episodios de pérdida de sensores
        alt Archivo guardado
            XLS->>BD: INSERT SessionExport ExportedById, ExportedAt, FileName
            XLS-->>UI: Archivo generado
            UI-->>Tec: Abrir archivo o carpeta
        else Error de escritura
            XLS-->>UI: No se pudo guardar el archivo
        end
    else Configured, Running o Cancelled
        XLS-->>UI: La sesión no se puede exportar
    end
```

---

## 5. Sesión simulada con el set de datos de prueba

Cubre HU-14. Ver [06-test-data.md](06-test-data.md).

```mermaid
sequenceDiagram
    autonumber
    actor Tec as Técnico
    participant UI
    participant Ses as Sesión
    participant Ser as Serial
    participant SIM as Simulador
    participant FS as Escenario TD-xx
    participant BD

    Tec->>UI: Elegir puerto SIM y escenario TD-04
    UI->>SIM: Cargar escenario con aceleración x120
    SIM->>FS: Leer scenario.json y readings.csv
    FS-->>SIM: Configuración, lecturas, eventos y resultados esperados
    SIM-->>UI: Canales, tipos, ubicaciones, límite y umbral del escenario
    UI->>Ses: Crear sesión IsSimulation = 1, TestScenarioCode TD-04
    Ses->>BD: INSERT MeasurementSession y SessionChannel
    Ser->>SIM: $IDN*43
    SIM-->>Ser: $IDN,1.0,ADQ-SIM-0001,Simulator,1.0.0,10,POLL,GRP-A*CS
    Tec->>UI: Iniciar captura
    Note over Ser,SIM: Mismo protocolo y misma lógica que con el hardware.<br/>El reloj virtual avanza 120 veces más rápido.
    loop Cada muestra del escenario
        Ser->>SIM: $READ,n*CS
        SIM->>SIM: Tomar las filas de la muestra n y aplicar FrameFault y eventos
        alt Evento COMM_LOST activo
            SIM--xSer: Sin respuesta
        else Muestra normal
            SIM-->>Ser: $RD,n,... incluidas tramas corruptas o faltantes y $EOS
        end
        Ser->>Ses: Procesar como en el diagrama 2
        Ses->>BD: INSERT Reading, Alert
    end
    opt Evento COMM_RESTORED
        SIM-->>Ser: $BOOT y $IDN con DeviceId y SensorGroupId del evento
        Ser->>Ses: Validar identidad como en el diagrama 3
    end
    Ses->>BD: Cerrar sesión como en el diagrama 4
    Ses->>FS: Leer expected de scenario.json
    Ses->>BD: SELECT sesión, vSessionSampleCoverage, Reading, Alert, CommunicationGap
    Ses->>Ses: Comparar estado, muestras, lecturas por estado, alertas por tipo y huecos
    Ses-->>UI: Escenario TD-04 OK, o lista de diferencias
```
