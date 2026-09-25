# Diagrama 4 · Cierre de sesión y exportación a Excel

Diagrama de secuencia de la fase 1. Participantes y convenciones en [04-sequence-diagrams.md](../../specs/functional/04-sequence-diagrams.md).

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

    alt Cierre automático al almacenar la última muestra de la duración planificada
        Ses->>BD: SELECT muestras válidas en vSessionSampleCoverage
        alt Al menos 31 muestras válidas
            Ses->>Ses: Status Completed, CloseReason PlannedDuration
        else Menos de 31 muestras válidas
            Ses->>Ses: Status Incomplete, CloseReason PlannedDuration
        end
    else Cierre manual antes de la duración planificada
        Tec->>UI: Finalizar sesión
        UI->>Ses: Solicitar cierre
        Ses-->>UI: Quedará INCOMPLETA: M de P minutos planificados
        UI-->>Tec: ¿Confirma? (alternativa: extender la sesión)
        break Técnico no confirma
            UI-->>Tec: La sesión sigue en curso
        end
        Tec->>UI: Confirmar
        Ses->>Ses: Status Incomplete, CloseReason Manual
    end
    Ses->>Ser: Detener muestreo
    Ser->>ADQ: $STOP*18
    ADQ-->>Ser: $ACK,STOP*CS
    Ser->>Ser: Cerrar puerto
    Ses->>BD: UPDATE MeasurementSession Status, CloseReason, EndedAt
    Note over BD: CK_MinDuration impide Completed antes de PlannedDurationMinutes<br/>CK_MaxDuration impide superar PlannedDurationMinutes
    Ses-->>UI: Sesión cerrada, empieza el descanso del adquisidor
    UI-->>Tec: Resumen y botón Exportar a Excel

    Tec->>UI: Exportar a Excel y elegir carpeta
    UI->>XLS: Exportar sesión
    XLS->>BD: Consultar sesión, empresa, equipo, técnico, adquisidor, canales
    BD-->>XLS: Datos de la sesión
    alt Status Completed, Incomplete o Invalid
        XLS->>BD: SELECT vSessionSampleCoverage, vSessionReadingPivot + estados por lectura
        XLS->>BD: SELECT Alert, CommunicationGap
        BD-->>XLS: Muestras, lecturas, alertas y huecos
        XLS->>XLS: Hoja Resumen con avisos de fallida, incompleta, mezcla, pérdida de sensores, sin límite o simulación
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
