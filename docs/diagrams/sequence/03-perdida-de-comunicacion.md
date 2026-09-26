# Diagrama 3 · Pérdida y recuperación de la comunicación serial

Diagrama de secuencia de la fase 1. Participantes y convenciones en [04-sequence-diagrams.md](../../specs/functional/04-sequence-diagrams.md). Vista gráfica: [svg/03-perdida-de-comunicacion.svg](svg/03-perdida-de-comunicacion.svg) (se regenera con `node docs/diagrams/render-sequence-svg.mjs`).

Cubre HU-09. Ver [02-serial-protocol.md §9](../../specs/functional/02-serial-protocol.md#9-pérdida-y-recuperación-de-la-comunicación).

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

    loop Cada 10 s hasta recuperar, invalidar, cerrar, cumplir SensorLossFailMinutes sin datos o llegar a la duración planificada
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
                Ses-->>UI: Sesión fallida: adquisidor o grupo de sensores distinto
            end
        else Puerto no disponible
            Ser->>Ser: Esperar 10 s
        end
    end

    opt El hueco llega a SensorLossFailMinutes (30 min: 16 muestras perdidas seguidas con 120 s)
        Ses->>BD: INSERT Alert SessionFailed, Critical
        Ses->>BD: UPDATE MeasurementSession Status Invalid, CloseReason DataLoss, EndedAt
        Ses-->>UI: Sesión fallida por pérdida sostenida de datos
    end

    opt El técnico cierra la sesión con el hueco abierto
        Ses->>BD: UPDATE CommunicationGap MissedSamples hasta EndedAt
        Ses->>BD: UPDATE MeasurementSession CloseReason CommunicationLost
    end
```
