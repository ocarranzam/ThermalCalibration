# 05 · Modelo de datos

| Campo | Valor |
|---|---|
| Versión | 0.6 (borrador para revisión) |
| Cambios en 0.6 | `EquipmentType.RowVersion` (concurrencia optimista, `ETag` de la API) y `CK_EquipmentType_Name` (nombre no vacío y sin espacios en los extremos), alineados con el contrato [thermal-v1.yaml](../../api/thermal-v1.yaml). |
| Cambios en 0.5 | Mínimo de puntos de medición: `MinMeasurementPoints`, `IsBelowMinimumPoints`, `BelowMinimumAcknowledgedAt`, alerta `BelowMinimumPoints` y `CK_MeasurementSession_BelowMinAck`. |
| Cambios en 0.4 | `MeasurementSession.AboveLimitCriticalMinutes`, alerta `AboveLimitSustained` y columna `Alert.SuspectedCause` (`Sensor` / `Equipment`). |
| Cambios en 0.3 | Duración planificada (`PlannedDurationMinutes`, `DurationSource`, `ClientRequestReference`) y mínimo por tipo de equipo. Política de pérdida de sensores copiada en la sesión (escalamiento y falla). Motivos de cierre `PlannedDuration` (reemplaza a `MaxDuration`) y `DataLoss`. Alertas `SensorLossPersistent` y `SessionFailed`. Autorización de inicio durante el descanso. Tablas `AppSetting` y `Tally`. Sin tope fijo de 721 muestras. |
| Cambios en 0.2 | Estado `Invalid` y motivo `DeviceMismatch`. Umbral de pérdida de sensores copiado en la sesión. Grupo de sensores. Marca de simulación. Modo de adquisición y plataforma `Simulator`. Alertas `SensorLoss` y `DeviceMismatch`. Vista `vSessionSampleCoverage`. Muestra 1 en t = 0 (hasta la 721). |
| Motor | SQL Server 2022. Base de datos `ThermalCalibration`, esquema `dbo`. |
| Fuente de verdad | [../../db/01-schema.sql](../../db/01-schema.sql). Validado el 2026-09-25: el script se ejecuta sin errores y los 22 escenarios de [06-test-data.md](06-test-data.md) cargan sin violar ninguna restricción. `vSessionSampleCoverage` reproduce las muestras programadas y afectadas esperadas en todos ellos. |
| Convenciones | PascalCase. Tablas en singular. PK `<Tabla>Id` (salvo `ThermocoupleType`, cuya PK es el código natural). Restricciones con prefijos `PK_`, `FK_`, `UQ_`, `CK_`, `DF_` e índices con `IX_`. Fechas en `DATETIMEOFFSET` (con desfase horario). Temperaturas en `DECIMAL` y en °C. |

Este documento describe el modelo. Si hay alguna discrepancia con el script, prevalece el script. Las mejoras propuestas se listan en la [§5](#5-observaciones-y-mejoras-propuestas-al-script) y **no** están aplicadas.

---

## 1. Diagrama entidad-relación

```mermaid
erDiagram
    ThermocoupleType ||--o{ SessionChannel : "tipo declarado"
    EquipmentType ||--o{ Equipment : "clasifica"
    Company ||--o{ Equipment : "posee"
    Equipment ||--o{ MeasurementSession : "se mide en"
    AppUser ||--o{ MeasurementSession : "técnico responsable"
    AcquisitionDevice |o--o{ MeasurementSession : "adquiere"
    MeasurementSession ||--o{ SessionChannel : "usa canales"
    SessionChannel ||--o{ Reading : "produce"
    MeasurementSession ||--o{ CommunicationGap : "registra huecos"
    MeasurementSession ||--o{ Alert : "genera"
    SessionChannel |o--o{ Alert : "sensor afectado"
    Reading |o--o{ Alert : "lectura origen"
    AppUser |o--o{ Alert : "reconoce"
    MeasurementSession ||--o{ SessionExport : "se exporta"
    AppUser ||--o{ SessionExport : "exporta"
    AppUser |o--o{ MeasurementSession : "autoriza inicio en descanso"

    ThermocoupleType {
        char ThermocoupleTypeCode PK "T o K"
        nvarchar Name
        decimal MinRangeC "rango físico mínimo"
        decimal MaxRangeC "rango físico máximo"
    }
    EquipmentType {
        int EquipmentTypeId PK
        nvarchar Name UK
        decimal MaxTemperatureC "NULL = límite pendiente"
        int MinSessionDurationMinutes "60 por defecto"
        nvarchar Description
        bit IsActive
        datetimeoffset CreatedAt
        datetimeoffset UpdatedAt
        rowversion RowVersion "ETag de la API"
    }
    AppUser {
        int AppUserId PK
        nvarchar FullName
        nvarchar Email UK
        varchar Role "Admin, Technician, Supervisor"
        bit IsActive
        datetimeoffset CreatedAt
    }
    Company {
        int CompanyId PK
        varchar TaxId UK "RUC"
        nvarchar Name
        nvarchar ContactName
        varchar Phone
        nvarchar Email
        nvarchar Address
        bit IsActive
        datetimeoffset CreatedAt
    }
    Equipment {
        int EquipmentId PK
        int CompanyId FK
        int EquipmentTypeId FK
        nvarchar Brand
        nvarchar Model
        nvarchar SerialNumber "UK con CompanyId"
        nvarchar InternalCode
        nvarchar Notes
        bit IsActive
        datetimeoffset CreatedAt
    }
    AcquisitionDevice {
        int AcquisitionDeviceId PK
        varchar DeviceIdentifier UK "respuesta a IDN"
        varchar Platform "Arduino, RaspberryPi, Other, Simulator"
        varchar FirmwareVersion
        tinyint ChannelCount "1 a 10"
        varchar AcquisitionMode "Poll o Stream"
        nvarchar Notes
        bit IsActive
        datetimeoffset CreatedAt
    }
    MeasurementSession {
        int MeasurementSessionId PK
        int EquipmentId FK
        int TechnicianId FK
        int AcquisitionDeviceId FK "NULL mientras se configura"
        varchar ComPort
        varchar SensorGroupId "grupo informado en IDN"
        smallint SamplingIntervalSeconds "120"
        int PlannedDurationMinutes "60 base, hasta días"
        varchar DurationSource "Base, EquipmentType, ClientRequest"
        nvarchar ClientRequestReference
        decimal MaxTemperatureC "copia del límite al iniciar"
        decimal SensorLossThresholdPct "copia del umbral, 60"
        tinyint SensorLossCriticalAfterSamples "copia, 3"
        smallint SensorLossFailMinutes "copia, 30"
        smallint AboveLimitCriticalMinutes "copia, 30"
        int RestOverrideById FK
        nvarchar RestOverrideReason
        tinyint MinMeasurementPoints "copia, 9"
        bit IsBelowMinimumPoints
        datetimeoffset BelowMinimumAcknowledgedAt
        bit HasMixedThermocoupleTypes
        datetimeoffset MixedTypesAcknowledgedAt
        bit IsSimulation
        varchar TestScenarioCode "TD-xx si es simulación"
        varchar Status
        varchar CloseReason
        datetimeoffset StartedAt "primera muestra recibida"
        datetimeoffset EndedAt
        nvarchar Notes
        datetimeoffset CreatedAt
    }
    SessionChannel {
        int SessionChannelId PK
        int MeasurementSessionId FK "UK con ChannelNumber"
        tinyint ChannelNumber "1 a 10"
        char ThermocoupleTypeCode FK
        nvarchar SensorLabel
        nvarchar Position "ubicación"
        bit IsActive
    }
    Reading {
        bigint ReadingId PK
        int SessionChannelId FK "UK con SampleNumber"
        int SampleNumber "1 = t0"
        datetimeoffset ReadAt
        decimal TemperatureC "NULL si es inválida"
        varchar SensorStatus
        char ReportedThermocoupleType
        bit IsAboveLimit
        varchar RawFrame "trama original"
        datetimeoffset ReceivedAt
    }
    CommunicationGap {
        int CommunicationGapId PK
        int MeasurementSessionId FK
        datetimeoffset LostAt
        datetimeoffset RecoveredAt "NULL si no se recuperó"
        int MissedSamples
        nvarchar Notes
    }
    Alert {
        bigint AlertId PK
        int MeasurementSessionId FK
        int SessionChannelId FK "NULL si es de la sesión"
        bigint ReadingId FK
        varchar AlertType
        varchar Severity "Info, Warning, Critical"
        datetimeoffset OccurredAt
        decimal ValueC
        decimal LimitC
        nvarchar Message
        varchar SuspectedCause "Sensor o Equipment"
        int AcknowledgedById FK
        datetimeoffset AcknowledgedAt
    }
    SessionExport {
        int SessionExportId PK
        int MeasurementSessionId FK
        int ExportedById FK
        datetimeoffset ExportedAt
        nvarchar FileName
    }
```

## 2. Ciclo de vida de la sesión

```mermaid
stateDiagram-v2
    [*] --> Configured : guardar configuración
    Configured --> Configured : editar canales, volver tras la advertencia de mezcla o esperar datos
    Configured --> Running : primera muestra recibida tras Iniciar (con confirmación si hay mezcla)
    Configured --> Cancelled : cancelar
    Running --> Completed : duración planificada cumplida y 31 o más muestras válidas
    Running --> Incomplete : cierre antes de lo planificado o menos de 31 muestras válidas
    Running --> Invalid : otro adquisidor o grupo al reconectar, o 30 min sin datos suficientes
    Running --> Cancelled : cancelar
    Completed --> [*]
    Incomplete --> [*]
    Invalid --> [*]
    Cancelled --> [*]
```

| `Status` | Significado | `StartedAt` | `EndedAt` | `CloseReason` | ¿Exportable? |
|---|---|---|---|---|---|
| `Configured` | Configurada, aún sin captura. Incluye el subestado de interfaz "Esperando datos" (después de pulsar Iniciar y antes de la primera muestra). | NULL | NULL | NULL | No |
| `Running` | Captura en curso (también durante un hueco de comunicación) | Sí | NULL | NULL | No |
| `Completed` | Alcanzó la duración planificada con al menos 31 muestras válidas | Sí | Sí | `PlannedDuration` | Sí |
| `Incomplete` | Cerrada antes de la duración planificada, o con menos de 31 muestras válidas | Sí | Sí | `PlannedDuration`, `Manual` o `CommunicationLost` | Sí |
| `Invalid` | **Fallida**: al reconectar respondió otro adquisidor u otro grupo de sensores, o hubo 30 min consecutivos de muestras afectadas | Sí | Sí | `DeviceMismatch` o `DataLoss` | Sí, con aviso |
| `Cancelled` | Descartada por el técnico | NULL o sí | NULL o sí | `Cancelled` | No |

## 3. Descripción de las entidades

### 3.1 Catálogos

**`ThermocoupleType`**: tipos de termopar admitidos y su rango físico. Datos iniciales: `T` (-200 a 350 °C) y `K` (-200 a 1260 °C). Se usa para validar el tipo declarado de cada canal (FK) y para marcar como `OutOfRange` los valores imposibles para el tipo declarado. Solo lo modifica el administrador. En la fase 1 es de solo lectura.

**`EquipmentType`**: catálogo ampliable de tipos de equipo (Refrigeradora, Congeladora, Conservadora, Incubadora…) con su **límite máximo** `MaxTemperatureC` y la **duración mínima de sesión** que exige por su forma de funcionar (`MinSessionDurationMinutes`, 60 min por defecto). NULL en el límite significa *límite pendiente*. Nombre único. Solo el administrador lo crea o edita, y registra `UpdatedAt` al editar. Se desactiva en lugar de borrarse cuando tiene equipos. `RowVersion` cambia en cada edición y la API lo usa como `ETag` para rechazar ediciones simultáneas (412). La base redondea un límite con más de 2 decimales, así que el rechazo de "-5,123" lo hace el dominio (`TemperatureLimit`).

**`AppSetting`**: parámetros del sistema que mantiene el administrador (clave y valor). Incluye el intervalo de muestreo, la duración base (60 min), la duración máxima planificable (7 días), el descanso del adquisidor (15 min, propuesta) y la política de pérdida de sensores (umbral del 60 %, escalamiento en la 3.ª muestra y falla a los 30 min). Los que afectan la evaluación de una sesión se **copian** en `MeasurementSession` al iniciarla.

**`Tally`**: tabla auxiliar con los números 1 a 100 000. La usa `vSessionSampleCoverage` para generar las muestras programadas de sesiones de cualquier duración.

### 3.2 Usuarios, empresas y equipos

**`AppUser`**: usuarios de la aplicación con un rol (`Admin`, `Technician`, `Supervisor`). Se llama `AppUser` porque `User` es palabra reservada en SQL Server. El correo es único.

**`Company`**: empresa cliente, identificada por su RUC (`TaxId`, único). Guarda los datos de contacto. Las simulaciones usan una empresa de prueba dedicada.

**`Equipment`**: equipo bajo prueba de una empresa. Tiene tipo, marca, modelo, número de serie (único por empresa) y código interno del cliente. Su historial de sesiones se obtiene con `MeasurementSession.EquipmentId` (índice `IX_MeasurementSession_EquipmentId_StartedAt`).

### 3.3 Adquisición

**`AcquisitionDevice`**: adquisidor identificado por el `DeviceIdentifier` que devuelve el comando `IDN` del protocolo. La aplicación lo registra al detectarlo por primera vez, con su `Platform` y su `AcquisitionMode` (`Poll` o `Stream`), y actualiza `FirmwareVersion` si cambia. `ChannelCount` (1–10) limita los canales que se pueden asignar. Los adquisidores simulados usan `Platform` = `Simulator`.

### 3.4 Sesión de medición

**`MeasurementSession`**: cabecera de la sesión. Registra:

- el equipo, el técnico, el adquisidor, el puerto COM y el **grupo de sensores** (`SensorGroupId`, informado por el adquisidor);
- el intervalo de muestreo (120 s por defecto);
- la **duración planificada** (`PlannedDurationMinutes`), su origen (`DurationSource`: `Base`, `EquipmentType` o `ClientRequest`) y la referencia del pedido del cliente (`ClientRequestReference`). La duración se puede extender mientras la sesión está en curso;
- el **límite aplicado** `MaxTemperatureC`, los minutos para escalar un fuera de límite sostenido (`AboveLimitCriticalMinutes`) y la **política de pérdida de sensores** (`SensorLossThresholdPct`, `SensorLossCriticalAfterSamples`, `SensorLossFailMinutes`), copiados al iniciar;
- la autorización para iniciar durante el descanso del adquisidor (`RestOverrideById`, `RestOverrideReason`);
- el indicador de mezcla con la fecha de confirmación, y el mínimo de puntos de medición (`MinMeasurementPoints`, 9) con el indicador `IsBelowMinimumPoints` y su fecha de confirmación;
- la **marca de simulación** (`IsSimulation`, `TestScenarioCode`);
- el estado, el motivo de cierre, las fechas de inicio (primera muestra recibida) y fin, y las notas.

**`SessionChannel`**: canal activo de la sesión, con su número (1–10, único en la sesión), su **tipo declarado** (FK a `ThermocoupleType`), su ubicación (`Position`) y una etiqueta física opcional (`SensorLabel`). El tipo se guarda por canal para poder representar la mezcla T/K.

### 3.5 Lecturas y eventos

**`Reading`**: una fila por canal y por muestra. Guarda:

- `SampleNumber`: 1 en t = 0, *n* en t = (n − 1) × 2 min. La última depende de la duración planificada: 31 en la sesión base, 721 en 24 h.
- `ReadAt`: instante programado de la muestra (hora de la PC).
- `TemperatureC` (NULL si la lectura es inválida), `SensorStatus`, `ReportedThermocoupleType` (tipo informado por el adquisidor) e `IsAboveLimit`.
- `RawFrame`: trama serial original, hasta 200 caracteres.
- `ReceivedAt`: hora de recepción con milisegundos.

Volumen por sesión: 10 × 31 = 310 filas en la sesión base; 7 210 en 24 h; 50 410 en 7 días.

| `SensorStatus` | Origen | `TemperatureC` | ¿Se evalúa el límite? | ¿Cuenta como dato válido para el umbral? |
|---|---|---|---|---|
| `OK` | Adquisidor, validado por la PC | Obligatoria | Sí, si `MeasurementSession.MaxTemperatureC` no es NULL | **Sí** |
| `OpenCircuit` | Adquisidor (`OC`) | NULL | No | No |
| `ShortCircuit` | Adquisidor (`SC`) | NULL | No | No |
| `OutOfRange` | Adquisidor (`OR`) o PC (valor fuera del rango del tipo) | NULL | No | No |
| `InvalidFrame` | PC (trama corrupta o faltante) | NULL | No | No |
| `TypeMismatch` | PC (tipo informado ≠ declarado) | Valor informado, no confiable | No | No |

**`CommunicationGap`**: intervalo sin comunicación con el adquisidor, con `LostAt`, `RecoveredAt` (NULL si no se recuperó antes del cierre o si la sesión se invalidó), `MissedSamples` y notas (p. ej. reconexión en otro puerto, o el adquisidor distinto que respondió).

**`Alert`**: evento que requiere atención. Siempre pertenece a una sesión. Opcionalmente se asocia a un canal y a una lectura.

| `AlertType` | Severidad | Canal | Lectura | `ValueC` / `LimitC` | Cuándo se genera |
|---|---|---|---|---|---|
| `AboveLimit` | `Warning` | Sí | Sí | Sí / Sí | En **cada** lectura `OK` con `TemperatureC > MaxTemperatureC` |
| `AboveLimitSustained` | `Critical` | Sí | Sí | Sí / Sí | Cuando un canal acumula `AboveLimitCriticalMinutes` (30) seguidos de lecturas fuera de límite. Una vez por racha. Lleva `SuspectedCause`: `Sensor` si menos de la mitad de los canales `OK` están fuera de límite, y `Equipment` si lo está la mitad o más. |
| `MixedThermocoupleTypes` | `Warning` | No | No | No / No | Al solicitar el inicio de una sesión con canales T y K |
| `BelowMinimumPoints` | `Warning` | No | No | No / No | Al solicitar el inicio de una sesión con menos canales activos que `MinMeasurementPoints` (9). Su reconocimiento es la confirmación obligatoria. |
| `TypeMismatch` | `Warning` | Sí | Sí | Sí / No | Al iniciar un episodio de tipo no coincidente en un canal |
| `SensorFault` | `Warning` | Sí | Sí | No / No | Al iniciar un episodio `OpenCircuit`, `ShortCircuit`, `OutOfRange` o `InvalidFrame` en un canal |
| `SensorLoss` | `Warning` | No | No | No / No | Al iniciar un episodio de muestras afectadas (más del umbral de canales sin dato válido) con lecturas |
| `SensorLossPersistent` | `Critical` | No | No | No / No | Cuando la pérdida no se restablece en la 3.ª muestra afectada consecutiva (`SensorLossCriticalAfterSamples`). Una vez por episodio. |
| `SessionFailed` | `Critical` | No | No | No / No | Cuando se cumplen `SensorLossFailMinutes` (30) consecutivos de muestras afectadas o perdidas. La sesión pasa a `Invalid` con `DataLoss`. |
| `CommunicationLost` | `Critical` | No | No | No / No | Al abrir un `CommunicationGap` |
| `DeviceMismatch` | `Critical` | No | No | No / No | Al reconectar otro adquisidor u otro grupo de sensores. La sesión pasa a `Invalid`. |
| `LimitNotDefined` | `Info` | No | No | No / No | Al iniciar una sesión cuyo tipo de equipo no tiene límite |

Las alertas `Critical` se notifican visualmente y exigen reconocimiento. Las `Warning` e `Info` solo se registran (01 RN-18). El reconocimiento (`AcknowledgedById` + `AcknowledgedAt`) es la única modificación permitida sobre una alerta. En `MixedThermocoupleTypes` el reconocimiento equivale a la confirmación obligatoria del técnico.

### 3.6 Exportación

**`SessionExport`**: bitácora de las exportaciones a Excel (quién, cuándo, nombre del archivo). No guarda el archivo.

### 3.7 Vistas

| Vista | Uso |
|---|---|
| `vSessionThermocoupleMix` | Número de tipos distintos entre los canales activos y el indicador `IsMixed`. Sirve para calcular y verificar `HasMixedThermocoupleTypes`. |
| `vSessionReadingPivot` | Una fila por muestra con las columnas `S1`…`S10` (`TemperatureC`). Es la base de los valores de la hoja "Lecturas". |
| `vSessionSampleCoverage` | Una fila por **muestra programada** (de 1 hasta la muestra correspondiente a `EndedAt`, o a la hora actual si la sesión está en curso, sin superar la duración planificada), **incluidas las perdidas**. Columnas: `ScheduledAt`, `ActiveChannels`, `ReadingRows`, `ValidReadings` (lecturas `OK`) e `IsAffected`. Es la referencia para contar las muestras válidas al cerrar (RN-03), para la columna "Estado de la muestra" del Excel y para validar los escenarios de prueba. Genera los números de muestra con la tabla `Tally`, sin depender de `GENERATE_SERIES`. |

## 4. Reglas de negocio

### 4.1 Reglas aplicadas en la base de datos

| Restricción | Regla | Ref. |
|---|---|---|
| `CK_ThermocoupleType_Range` | `MinRangeC < MaxRangeC`. | — |
| `UQ_EquipmentType_Name` | Nombre de tipo de equipo único (sin distinguir mayúsculas, según la intercalación por defecto). | HU-02 |
| `CK_EquipmentType_Name` | Nombre no vacío y sin espacios al inicio ni al final. | HU-02 |
| `CK_EquipmentType_MinDuration` | Duración mínima de sesión entre 60 y 43 200 min. | RN-04 |
| `CK_AppUser_Role` | Rol ∈ {`Admin`, `Technician`, `Supervisor`}. | Visión §4 |
| `UQ_AppUser_Email` | Correo único. | — |
| `UQ_Company_TaxId` | RUC único. | HU-01 |
| `UQ_Equipment_Company_Serial` | Número de serie único por empresa. | HU-01 |
| `UQ_AcquisitionDevice_Identifier` | Identificador de adquisidor único. | HU-03 |
| `CK_AcquisitionDevice_Platform` | Plataforma ∈ {`Arduino`, `RaspberryPi`, `Other`, `Simulator`}. | Protocolo §4.3 |
| `CK_AcquisitionDevice_Mode` | Modo ∈ {`Poll`, `Stream`}. | Protocolo §4.3 |
| `CK_AcquisitionDevice_ChannelCount` | De 1 a 10 canales. | RN-01 |
| `CK_MeasurementSession_Status` | Estado ∈ {`Configured`, `Running`, `Completed`, `Incomplete`, `Invalid`, `Cancelled`}. | §2 |
| `CK_MeasurementSession_CloseReason` | Motivo NULL o ∈ {`Manual`, `PlannedDuration`, `Cancelled`, `CommunicationLost`, `DeviceMismatch`, `DataLoss`}. | HU-10 |
| `CK_MeasurementSession_Invalid` | `Invalid` (fallida) si y solo si `CloseReason` ∈ {`DeviceMismatch`, `DataLoss`}. | RN-15 |
| `CK_MeasurementSession_Planned` | Duración planificada entre 60 y 43 200 min (30 días). El máximo operativo (7 días) está en `AppSetting`. | RN-04 |
| `CK_MeasurementSession_DurationSource` | Origen ∈ {`Base`, `EquipmentType`, `ClientRequest`}. | RN-04 |
| `CK_MeasurementSession_ClientRequest` | Si el origen es `ClientRequest`, la referencia del pedido es obligatoria. | RN-04 |
| `CK_MeasurementSession_LossEscalation` | Escalamiento entre 2 y 10 muestras; falla entre 10 y 240 min. | RN-14, RN-15 |
| `CK_MeasurementSession_RestOverride` | Quién autorizó y el motivo se rellenan juntos o ninguno. | RN-17 |
| `CK_MeasurementSession_Interval` | Intervalo > 0 (120 s por defecto, `DF_MeasurementSession_Interval`, **[PC-01]**). | RN-02 |
| `CK_MeasurementSession_SensorLoss` | 0 < `SensorLossThresholdPct` < 100 (60 por defecto, `DF_MeasurementSession_SensorLoss`). | RN-14 |
| `CK_MeasurementSession_TestScenario` | `TestScenarioCode` solo en sesiones con `IsSimulation` = 1. | RN-16 |
| `CK_MeasurementSession_Dates` | `EndedAt` exige `StartedAt` y `EndedAt >= StartedAt`. | — |
| `CK_MeasurementSession_MaxDuration` | Duración ≤ `PlannedDurationMinutes` × 60. | RN-04 |
| `CK_MeasurementSession_MinDuration` | `Completed` exige una duración ≥ `PlannedDurationMinutes` × 60. | RN-03 |
| `CK_MeasurementSession_MinPoints` | Mínimo de puntos entre 1 y 10. | RN-20 |
| `CK_MeasurementSession_BelowMinAck` | Con menos puntos que el mínimo, la sesión solo puede salir de `Configured`/`Cancelled` si tiene `BelowMinimumAcknowledgedAt`. | RN-20 |
| `CK_MeasurementSession_MixedAck` | Con mezcla, la sesión solo puede salir de `Configured`/`Cancelled` si tiene `MixedTypesAcknowledgedAt`. | RN-05 |
| `UQ_SessionChannel_Session_Channel` | Un número de canal por sesión. | RN-01 |
| `CK_SessionChannel_ChannelNumber` | Canal de 1 a 10. | RN-01 |
| `FK_SessionChannel_ThermocoupleType` | El tipo declarado debe existir (T o K). | RN-01 |
| `UQ_Reading_Channel_Sample` | Una lectura por canal y muestra. | HU-06 |
| `CK_Reading_SampleNumber` | Muestra ≥ 1. El tope lo fija la duración planificada. | RN-02, RN-04 |
| `CK_Reading_SensorStatus` | Estado ∈ {`OK`, `OpenCircuit`, `ShortCircuit`, `OutOfRange`, `InvalidFrame`, `TypeMismatch`}. | HU-07 |
| `CK_Reading_TemperatureWhenOk` | Una lectura `OK` debe tener temperatura. | HU-07 |
| `CK_CommunicationGap_Dates` | `RecoveredAt >= LostAt`. | HU-09 |
| `CK_Alert_AlertType` | Tipo ∈ {`AboveLimit`, `AboveLimitSustained`, `MixedThermocoupleTypes`, `BelowMinimumPoints`, `TypeMismatch`, `SensorFault`, `SensorLoss`, `SensorLossPersistent`, `SessionFailed`, `CommunicationLost`, `DeviceMismatch`, `LimitNotDefined`}. | §3.5 |
| `CK_Alert_Severity` | Severidad ∈ {`Info`, `Warning`, `Critical`}. | §3.5 |
| `CK_Alert_SuspectedCause` | `SuspectedCause` ∈ {`Sensor`, `Equipment`} y obligatorio en `AboveLimitSustained`; NULL en las demás alertas. | RN-19 |
| `CK_MeasurementSession_AboveCritical` | `AboveLimitCriticalMinutes` entre 10 y 240. | RN-19 |
| `CK_Alert_Ack` | `AcknowledgedById` y `AcknowledgedAt` se rellenan juntos o ninguno. | HU-04, HU-08 |

### 4.2 Reglas aplicadas en la aplicación

| Id | Regla | Motivo por el que no está en la BD |
|---|---|---|
| RA-01 | Validación del RUC (11 dígitos, prefijo 10/15/17/20, dígito verificador módulo 11). | Algoritmo. Se podría añadir un `CHECK` de formato (ver §5). |
| RA-02 | Solo `Admin` edita `EquipmentType`, `AppUser` y `AppSetting`. Solo `Supervisor` o `Admin` autorizan un inicio durante el descanso. Solo el técnico de la sesión, un `Admin` o un `Supervisor` pueden cerrarla o cancelarla. | Autorización. |
| RA-03 | Al pasar a `Running` se copian `EquipmentType.MaxTemperatureC` y la política de pérdida de sensores vigente (`AppSetting`) en la sesión, y después ya no se modifican. | Requiere la lectura de otra tabla o de la configuración en el momento de la transición. |
| RA-04 | Para solicitar el inicio hacen falta: al menos 1 canal activo, un adquisidor detectado (`AcquisitionDeviceId` no NULL), todos los canales ≤ `ChannelCount` y el puerto COM sin otra sesión `Running`. | Reglas entre tablas y de estado. |
| RA-23 | `IsBelowMinimumPoints` = canales activos < `MinMeasurementPoints` (copiado de `AppSetting` al solicitar el inicio). Si es verdadero, se crea la alerta `BelowMinimumPoints` y se exige la confirmación. | Regla entre tablas y parámetros. |
| RA-05 | `HasMixedThermocoupleTypes` se calcula con los canales activos (`vSessionThermocoupleMix`) al solicitar el inicio. Si hay mezcla, se crea la alerta `MixedThermocoupleTypes`. | Regla entre tablas. |
| RA-06 | Si `MaxTemperatureC` es NULL al iniciar, se crea la alerta `LimitNotDefined` y no se evalúa el límite. | Evento de negocio. |
| RA-07 | `IsAboveLimit = 1` si y solo si `SensorStatus = 'OK'` y `TemperatureC > MeasurementSession.MaxTemperatureC`. Cada caso genera una alerta `AboveLimit` con `ValueC` y `LimitC`. | Regla entre tablas. |
| RA-08 | `TemperatureC` es NULL para `OpenCircuit`, `ShortCircuit`, `OutOfRange` e `InvalidFrame`. En `TypeMismatch` guarda el valor informado. | Parcialmente expresable (ver §5). |
| RA-09 | Validación de tramas V1–V10, reintentos y verificación de tipo según [02-serial-protocol.md](02-serial-protocol.md). | Lógica de protocolo. |
| RA-10 | Alertas `SensorFault`, `TypeMismatch`, `SensorLoss` y `SensorLossPersistent` por episodio. `AboveLimit` por lectura. | Requiere el estado previo. |
| RA-11 | `StartedAt` = instante de la primera muestra con al menos una trama `RD` válida recibida después de "Iniciar". Esa muestra es la número 1. La muestra *n* se programa (o asigna, en `STREAM`) en `StartedAt + (n − 1) × SamplingIntervalSeconds`. | Temporización y protocolo. |
| RA-12 | Cierre automático al almacenar la última muestra de la duración planificada, con `PlannedDuration`: `Completed` si hay ≥ `BaseSessionMinutes × 60 / SamplingIntervalSeconds + 1` muestras no afectadas (31 con 120 s, **[PC-01]**) en `vSessionSampleCoverage`, y si no `Incomplete`. El cierre manual antes de lo planificado deja la sesión `Incomplete` (`Manual`). | La BD valida la duración (`CK_MinDuration`, `CK_MaxDuration`), pero el conteo de muestras válidas depende de la vista y de la transición. |
| RA-13 | `CommunicationGap.MissedSamples` = número de muestras programadas entre `LostAt` y `RecoveredAt` (o `EndedAt`) sin lecturas. | Cálculo. |
| RA-14 | Las lecturas, los huecos y las alertas no se editan ni se borran, salvo el reconocimiento de alertas y el cierre del hueco (`RecoveredAt`, `MissedSamples`, `Notes`). | Se recomienda reforzarlo con permisos (ver §5). |
| RA-15 | Se exportan sesiones `Completed`, `Incomplete` e `Invalid`. Cada exportación inserta un `SessionExport`. | Regla de proceso. |
| RA-16 | Al reconectar se comparan `DeviceId`, `SensorGroupId`, `ChannelCount` y, si no hay grupo, la huella de tipos de la muestra 1. Cualquier diferencia pasa la sesión a `Invalid`/`DeviceMismatch` con alerta `DeviceMismatch`. | Lógica de protocolo. |
| RA-17 | Una muestra está afectada si `(ActiveChannels − ValidReadings) × 100 > SensorLossThresholdPct × ActiveChannels`. Las muestras perdidas por un hueco tienen `ValidReadings` = 0. Coincide con `vSessionSampleCoverage.IsAffected`. | La vista lo calcula. La aplicación lo evalúa en línea para generar la alerta. |
| RA-19 | `PlannedDurationMinutes` = máx(`BaseSessionMinutes`, `EquipmentType.MinSessionDurationMinutes`, pedido del cliente) ≤ `MaxSessionMinutes`. Se puede extender durante la sesión (el origen pasa a `ClientRequest`, con referencia). | Regla entre tablas y parámetros. |
| RA-20 | Descanso: no se inicia una sesión con un adquisidor cuya última `EndedAt` + `RestPeriodMinutes` sea posterior al momento actual, salvo con autorización registrada. | Consulta sobre otras sesiones (`IX_MeasurementSession_Device_EndedAt`). |
| RA-21 | Escalamiento: con `k` muestras afectadas consecutivas (incluidas las perdidas por un hueco), se genera `SensorLossPersistent` cuando `k` = `SensorLossCriticalAfterSamples` y la muestra tiene lecturas. Se declara la falla (`SessionFailed`, `Invalid`/`DataLoss`) cuando `(k − 1) × SamplingIntervalSeconds ≥ SensorLossFailMinutes × 60`. | Requiere el estado previo. |
| RA-22 | Fuera de límite sostenido: por canal, cuenta las lecturas `OK` fuera de límite seguidas (`k`). Cuando `(k − 1) × SamplingIntervalSeconds ≥ AboveLimitCriticalMinutes × 60`, crea una `AboveLimitSustained` con la causa probable calculada en esa muestra. Cualquier otra lectura, o una muestra perdida, reinicia `k`. | Requiere el estado previo. |
| RA-18 | Un adquisidor con `Platform` = `Simulator` solo se usa en sesiones con `IsSimulation` = 1 y con la empresa de prueba. El modo simulación se deshabilita en producción. | Configuración y regla entre tablas (ver §5, M-13). |

## 5. Observaciones y mejoras propuestas al script

Hallazgos de la revisión de [01-schema.sql](../../db/01-schema.sql) contra esta especificación. **No están aplicados**: requieren aprobación.

| # | Observación | Propuesta |
|---|---|---|
| M-01 | `CK_MeasurementSession_MinDuration` se cumple si `StartedAt` o `EndedAt` son NULL, porque `DATEDIFF` devuelve NULL y un `CHECK` con resultado desconocido pasa. Una sesión `Completed` sin fechas sería aceptada. | Añadir `CHECK (Status NOT IN ('Running','Completed','Incomplete','Invalid') OR StartedAt IS NOT NULL)` y `CHECK (Status NOT IN ('Completed','Incomplete','Invalid') OR EndedAt IS NOT NULL)`. |
| M-02 | No se exige `CloseReason` en las sesiones cerradas ni se impide en las abiertas. | `CHECK ((Status IN ('Completed','Incomplete','Invalid','Cancelled') AND CloseReason IS NOT NULL) OR (Status IN ('Configured','Running') AND CloseReason IS NULL))`. |
| M-03 | Nada impide dos sesiones `Running` en el mismo puerto COM ni en el mismo equipo. | Índices únicos filtrados: `CREATE UNIQUE INDEX UX_MeasurementSession_ComPort_Running ON dbo.MeasurementSession (ComPort) WHERE Status = 'Running'`, y lo mismo para `EquipmentId`. |
| M-04 | `IsAboveLimit` podría valer 1 en una lectura no `OK`. | `CHECK (IsAboveLimit = 0 OR SensorStatus = 'OK')`. |
| M-05 | `TemperatureC` puede tener valor en los estados de falla. | `CHECK (SensorStatus IN ('OK','TypeMismatch') OR TemperatureC IS NULL)`. |
| M-06 | `ReportedThermocoupleType` no tiene FK. | FK a `ThermocoupleType`, o `CHECK (ReportedThermocoupleType IN ('T','K'))`. |
| M-07 | La inmutabilidad de `Reading`, `Alert` y `CommunicationGap` depende solo de la aplicación. | Rol de base de datos de la aplicación con `DENY DELETE` sobre esas tablas y `DENY UPDATE` sobre `Reading`. Alternativa: triggers de auditoría. |
| M-08 | `Company.TaxId` acepta cualquier texto. | `CHECK (TaxId NOT LIKE '%[^0-9]%' AND LEN(TaxId) = 11)`, si solo habrá clientes con RUC peruano. |
| M-09 | `Alert` para `AboveLimit` no exige `SessionChannelId`, `ReadingId`, `ValueC` ni `LimitC`. | `CHECK (AlertType <> 'AboveLimit' OR (SessionChannelId IS NOT NULL AND ReadingId IS NOT NULL AND ValueC IS NOT NULL AND LimitC IS NOT NULL))`. |
| M-10 | `vSessionReadingPivot` solo expone `TemperatureC`. Para el Excel también hacen falta `SensorStatus` e `IsAboveLimit` por celda. | Vista complementaria en formato largo, o dejar el pivotado en el generador. |
| M-11 | `CK_Reading_SampleNumber` ya no tiene tope fijo. Una lectura con un número mayor que el de la duración planificada no se detecta en la base. | Opcional: trigger que compare `SampleNumber` con `PlannedDurationMinutes × 60 / SamplingIntervalSeconds + 1`. |
| M-12 | `CK_MeasurementSession_MinDuration` solo controla la duración de reloj. La exigencia de 31 muestras válidas (P-02) es una regla de aplicación (RA-12). | Opcional: trigger `AFTER UPDATE` que rechace `Completed` si `vSessionSampleCoverage` tiene menos de 31 muestras no afectadas. |
| M-13 | Nada impide usar un adquisidor `Simulator` en una sesión real (`IsSimulation` = 0). | Trigger o validación en el procedimiento de inicio. No se puede expresar con un `CHECK` porque cruza tablas. |
| M-14 | `vSessionSampleCoverage` recalcula la cobertura en cada consulta (hasta 5 041 muestras × 10 canales en una sesión de 7 días). | Suficiente para la fase 1. Si el historial crece, materializar la cobertura al cerrar la sesión (columnas `ValidSamples` y `AffectedSamples` en `MeasurementSession`). |
| M-15 | `AppSetting.SettingValue` es texto: la base no valida el tipo ni el rango de cada parámetro. | La aplicación valida al guardar. Alternativa: una tabla con columnas tipadas y `CHECK` por parámetro. |
