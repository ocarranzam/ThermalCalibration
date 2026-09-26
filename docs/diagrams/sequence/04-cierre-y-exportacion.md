# Diagrama 4 · Cierre, cancelación y exportación a Excel

Diagrama de secuencia de la fase 1. Participantes y convenciones en [04-sequence-diagrams.md](../../specs/functional/04-sequence-diagrams.md). Vista gráfica: [svg/04-cierre-y-exportacion.svg](svg/04-cierre-y-exportacion.svg) (se regenera con `node docs/diagrams/render-sequence-svg.mjs`).

Cubre HU-10 (cierre automático, manual y cancelación) y HU-11.

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
        Note over Ses,BD: Mínimo = 1 h de datos válidos: BaseSessionMinutes x 60 / intervalo + 1<br/>(31 muestras con 120 s, [PC-01])
        alt Alcanza el mínimo de muestras válidas
            Ses->>Ses: Status Completed, CloseReason PlannedDuration
        else No alcanza el mínimo
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
        UI->>Ses: Confirmar cierre
        Ses->>Ses: Status Incomplete, CloseReason Manual
    else Cancelación (datos descartables)
        Tec->>UI: Cancelar sesión con motivo
        UI->>Ses: Cancelar
        Ses->>Ses: Status Cancelled, CloseReason Cancelled, Notes = motivo
        Note over Ses: Las lecturas se conservan,<br/>pero la sesión no se puede exportar
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
        XLS->>XLS: Hoja Resumen con avisos: fallida, incompleta, menos de 9 puntos,<br/>mezcla T/K, pérdida de sensores, sin límite o simulación
        XLS->>XLS: Hoja Lecturas: estado de cada muestra<br/>y celdas resaltadas
        XLS->>XLS: Hojas Alertas y Comunicación:<br/>huecos y episodios de pérdida de sensores
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
