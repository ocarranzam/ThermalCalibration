# Diagrama 1 · Configuración, detección del adquisidor, advertencia de mezcla e inicio al llegar datos

Diagrama de secuencia de la fase 1. Participantes y convenciones en [04-sequence-diagrams.md](../../specs/functional/04-sequence-diagrams.md).

Cubre HU-03, HU-04, HU-05, HU-16 (duración y descanso), HU-17 (menos de 9 puntos) y el inicio de HU-06.

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
    Tec->>UI: Duración: base, o la del tipo de equipo, o la pedida por el cliente con referencia
    UI->>Ses: Guardar configuración
    Ses->>Ses: Validar 1 a 10 canales, sin duplicados, dentro de ChannelCount
    Ses->>Ses: PlannedDuration = max(base 60, mínimo del tipo, pedido del cliente), hasta el máximo
    Ses->>BD: INSERT MeasurementSession Status Configured, SensorGroupId GRP-A, PlannedDurationMinutes
    Ses->>BD: INSERT SessionChannel x5

    Tec->>UI: Iniciar captura
    UI->>Ses: Solicitar inicio
    Ses->>BD: Última EndedAt del adquisidor
    alt Adquisidor en descanso (EndedAt + RestPeriodMinutes mayor que ahora)
        Ses-->>UI: El adquisidor descansa hasta HH:MM
        break Sin autorización de un supervisor
            UI-->>Tec: Iniciar captura deshabilitado, cuenta regresiva
        end
        Tec->>UI: Supervisor autoriza el inicio anticipado con motivo
        Ses->>BD: UPDATE RestOverrideById, RestOverrideReason
    end
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
    opt Menos canales activos que MinMeasurementPoints (9)
        Ses->>BD: UPDATE IsBelowMinimumPoints = 1
        Ses->>BD: INSERT Alert BelowMinimumPoints, Warning
        UI-->>Tec: N puntos de 9 que exigen IEC 60068-3-5 y DKD-R 5-7. ¿Confirma?
        Tec->>UI: Confirmar
        Ses->>BD: UPDATE Alert reconocida y BelowMinimumAcknowledgedAt
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
    Ses->>BD: UPDATE Status Running, StartedAt = t0, copias del límite y de la política de pérdida de sensores
    Note over Ses,BD: CK_MixedAck y CK_BelowMinAck rechazan Running<br/>si hay mezcla o menos de 9 puntos sin confirmación
    Ses->>BD: INSERT Reading de la muestra 1
    Ses->>Ses: Guardar la huella de tipos de la muestra 1
    Ser-->>UI: Captura en curso desde t0
```
