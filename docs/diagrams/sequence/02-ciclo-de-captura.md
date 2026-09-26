# Diagrama 2 · Ciclo de captura cada 2 minutos

Diagrama de secuencia de la fase 1. Participantes y convenciones en [04-sequence-diagrams.md](../../specs/functional/04-sequence-diagrams.md). Vista gráfica: [svg/02-ciclo-de-captura.svg](svg/02-ciclo-de-captura.svg) (se regenera con `node docs/diagrams/render-sequence-svg.mjs`).

Cubre HU-06, HU-07, HU-08 y HU-13. Validaciones V1–V10 de [02-serial-protocol.md §7](../../specs/functional/02-serial-protocol.md#7-validación-de-tramas-en-la-pc).

```mermaid
sequenceDiagram
    autonumber
    participant Ser as Serial
    participant ADQ as Adquisidor
    participant Ses as Sesión
    participant BD
    participant UI

    Note over Ser,UI: intervalo = SamplingIntervalSeconds copiado en la sesión (120 s por defecto, [PC-01])<br/>Umbrales copiados al iniciar: SensorLossCriticalAfterSamples (3), SensorLossFailMinutes (30), AboveLimitCriticalMinutes (30)
    loop Cada muestra n desde 2 hasta la última de la duración planificada, en StartedAt + (n - 1) x intervalo
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
            Ser->>Ser: n = 1 + round((t - StartedAt) / intervalo)
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
                    Ses->>BD: INSERT Alert AboveLimit, Warning, con ReadingId, valor y límite
                    opt Canal fuera de límite AboveLimitCriticalMinutes seguidos, primera vez en la racha
                        Ses->>Ses: Causa probable = Equipment si la mitad o más<br/>de los canales OK están fuera de límite. Si no, Sensor
                        Ses->>BD: INSERT Alert AboveLimitSustained, Critical, SuspectedCause
                        Ses-->>UI: Aviso crítico con la causa probable y la acción sugerida
                    end
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
            Ses->>Ses: Muestra n afectada, consecutivas = consecutivas + 1
            opt Primera muestra afectada del episodio
                Ses->>BD: INSERT Alert SensorLoss, Warning
            end
            opt consecutivas = SensorLossCriticalAfterSamples (no se restableció en la 3.ª medición)
                Ses->>BD: INSERT Alert SensorLossPersistent, Critical
                Ses-->>UI: Aviso crítico destacado, requiere reconocimiento
            end
            opt (consecutivas - 1) x intervalo mayor o igual que SensorLossFailMinutes
                Ses->>BD: INSERT Alert SessionFailed, Critical
                Ses->>BD: UPDATE Status Invalid, CloseReason DataLoss, EndedAt
                Ser->>ADQ: $STOP*18
                Ses-->>UI: Sesión fallida por pérdida sostenida de datos
            end
        else No supera el umbral
            Ses->>Ses: Muestra n válida, consecutivas = 0
        end
        Ses-->>UI: Actualizar monitor y contadores, advertencias sin aviso emergente
        opt n = última muestra planificada
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
- Escalamiento (TD-16, con el intervalo de 120 s): afectadas desde la muestra 20 → advertencia en la 20, crítica en la 22, sesión fallida en la 35 (30 min).
