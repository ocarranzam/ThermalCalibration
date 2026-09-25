# Diagrama 5 · Sesión simulada con el set de datos de prueba

Diagrama de secuencia de la fase 1. Participantes y convenciones en [04-sequence-diagrams.md](../../specs/functional/04-sequence-diagrams.md).

Cubre HU-14. Ver [06-test-data.md](../../specs/functional/06-test-data.md).

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
