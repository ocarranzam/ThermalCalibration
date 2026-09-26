/* =====================================================================
   Sistema de Monitoreo Térmico para Calibración de Equipos de Refrigeración
   Fase 1: captura de lecturas y exportación a Excel
   Motor: SQL Server 2022
   Convención: PascalCase, tablas en singular, PK "<Tabla>Id"
   ===================================================================== */

CREATE DATABASE ThermalCalibration;
GO

USE ThermalCalibration;
GO

/* ---------------------------------------------------------------------
   Parámetros y tabla auxiliar
   --------------------------------------------------------------------- */

-- Parámetros del sistema que mantiene el administrador.
-- Los que afectan la evaluación de una sesión se COPIAN en MeasurementSession al iniciarla.
CREATE TABLE dbo.AppSetting (
    SettingKey    VARCHAR(50)        NOT NULL,
    SettingValue  NVARCHAR(100)      NOT NULL,
    Description   NVARCHAR(300)      NULL,
    UpdatedAt     DATETIMEOFFSET(0)  NOT NULL CONSTRAINT DF_AppSetting_UpdatedAt DEFAULT (SYSDATETIMEOFFSET()),
    UpdatedById   INT                NULL,
    CONSTRAINT PK_AppSetting PRIMARY KEY (SettingKey)
);
GO

-- Números 1..100 000 para generar las muestras programadas (hasta 30 días a 30 s)
CREATE TABLE dbo.Tally (
    N  INT  NOT NULL,
    CONSTRAINT PK_Tally PRIMARY KEY (N)
);
GO

WITH Digit AS (SELECT d FROM (VALUES (0), (1), (2), (3), (4), (5), (6), (7), (8), (9)) AS v (d))
INSERT INTO dbo.Tally (N)
SELECT a.d + 10 * b.d + 100 * c.d + 1000 * e.d + 10000 * f.d + 1
FROM Digit AS a CROSS JOIN Digit AS b CROSS JOIN Digit AS c CROSS JOIN Digit AS e CROSS JOIN Digit AS f;
GO

/* ---------------------------------------------------------------------
   Catálogos
   --------------------------------------------------------------------- */

-- Tipos de termopar y su rango físico de medición (para validar lecturas)
CREATE TABLE dbo.ThermocoupleType (
    ThermocoupleTypeCode CHAR(1)        NOT NULL,
    Name                 NVARCHAR(50)   NOT NULL,
    MinRangeC            DECIMAL(7, 2)  NOT NULL,
    MaxRangeC            DECIMAL(7, 2)  NOT NULL,
    CONSTRAINT PK_ThermocoupleType PRIMARY KEY (ThermocoupleTypeCode),
    CONSTRAINT CK_ThermocoupleType_Range CHECK (MinRangeC < MaxRangeC)
);
GO

-- Tipos de equipo con su criterio de límite (D-05 y D-07):
--   Range: límites absolutos. Fuera de límite si TemperatureC > MaxTemperatureC o < MinTemperatureC; cualquiera
--          de los dos puede faltar (refrigeradora 2..8 °C; congeladora de plasma o ultracongeladora: solo máximo).
--   Band:  relativo a la consigna. Fuera de límite si |TemperatureC - consigna| > ToleranceK (incubadoras,
--          cámaras ambientales); la consigna (SetpointC) se registra en cada sesión.
-- Sin valores, el límite está pendiente. IsLimitSuggested = 1: valores sugeridos por la norma, pendientes de
-- confirmar por el laboratorio (se pone en 0 al editar el límite).
-- MinMeasurementPoints: puntos mínimos que exige la norma del tipo (D-06, antes P-18): 9 según IEC 60068-3-5,
--   DKD-R 5-7 y USP <1079.4> para equipos de hasta 2000 L; 27 en incubadoras de más de 50 L (DIN 12880).
-- MinSessionDurationMinutes: duración mínima que exige el tipo por su forma de funcionar (base: 60 min).
-- RowVersion: versión para la concurrencia optimista; la API la expone como ETag / If-Match (docs/api/thermal-v1.yaml).
CREATE TABLE dbo.EquipmentType (
    EquipmentTypeId            INT IDENTITY(1, 1) NOT NULL,
    Name                       NVARCHAR(100)      NOT NULL,
    LimitMode                  VARCHAR(10)        NOT NULL CONSTRAINT DF_EquipmentType_LimitMode DEFAULT ('Range'),
    MinTemperatureC            DECIMAL(6, 2)      NULL,
    MaxTemperatureC            DECIMAL(6, 2)      NULL,
    ToleranceK                 DECIMAL(4, 2)      NULL,
    IsLimitSuggested           BIT                NOT NULL CONSTRAINT DF_EquipmentType_LimitSuggested DEFAULT (0),
    MinMeasurementPoints       TINYINT            NOT NULL CONSTRAINT DF_EquipmentType_MinPoints DEFAULT (9),
    MinSessionDurationMinutes  INT                NOT NULL CONSTRAINT DF_EquipmentType_MinDuration DEFAULT (60),
    Description                NVARCHAR(500)      NULL,
    IsActive                   BIT                NOT NULL CONSTRAINT DF_EquipmentType_IsActive DEFAULT (1),
    CreatedAt                  DATETIMEOFFSET(0)  NOT NULL CONSTRAINT DF_EquipmentType_CreatedAt DEFAULT (SYSDATETIMEOFFSET()),
    UpdatedAt                  DATETIMEOFFSET(0)  NULL,
    RowVersion                 ROWVERSION         NOT NULL,
    CONSTRAINT PK_EquipmentType PRIMARY KEY (EquipmentTypeId),
    CONSTRAINT UQ_EquipmentType_Name UNIQUE (Name),
    CONSTRAINT CK_EquipmentType_Name CHECK (LEN(Name) > 0 AND Name NOT LIKE N' %' AND DATALENGTH(Name) = DATALENGTH(RTRIM(Name))),
    CONSTRAINT CK_EquipmentType_MinDuration CHECK (MinSessionDurationMinutes BETWEEN 60 AND 43200),
    CONSTRAINT CK_EquipmentType_LimitMode CHECK (
        (LimitMode = 'Range' AND ToleranceK IS NULL
            AND (MinTemperatureC IS NULL OR MaxTemperatureC IS NULL OR MinTemperatureC < MaxTemperatureC))
        OR (LimitMode = 'Band' AND MinTemperatureC IS NULL AND MaxTemperatureC IS NULL
            AND (ToleranceK IS NULL OR ToleranceK > 0))),
    CONSTRAINT CK_EquipmentType_MinPoints CHECK (MinMeasurementPoints BETWEEN 1 AND 27)
);
GO

/* ---------------------------------------------------------------------
   Usuarios, empresas y equipos
   --------------------------------------------------------------------- */

-- "User" es palabra reservada en SQL Server; por eso AppUser
CREATE TABLE dbo.AppUser (
    AppUserId  INT IDENTITY(1, 1) NOT NULL,
    FullName   NVARCHAR(150)      NOT NULL,
    Email      NVARCHAR(150)      NOT NULL,
    Role       VARCHAR(20)        NOT NULL,
    IsActive   BIT                NOT NULL CONSTRAINT DF_AppUser_IsActive DEFAULT (1),
    CreatedAt  DATETIMEOFFSET(0)  NOT NULL CONSTRAINT DF_AppUser_CreatedAt DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT PK_AppUser PRIMARY KEY (AppUserId),
    CONSTRAINT UQ_AppUser_Email UNIQUE (Email),
    CONSTRAINT CK_AppUser_Role CHECK (Role IN ('Admin', 'Technician', 'Supervisor'))
);
GO

CREATE TABLE dbo.Company (
    CompanyId    INT IDENTITY(1, 1) NOT NULL,
    TaxId        VARCHAR(20)        NOT NULL,   -- RUC
    Name         NVARCHAR(200)      NOT NULL,
    ContactName  NVARCHAR(150)      NULL,
    Phone        VARCHAR(30)        NULL,
    Email        NVARCHAR(150)      NULL,
    Address      NVARCHAR(300)      NULL,
    IsActive     BIT                NOT NULL CONSTRAINT DF_Company_IsActive DEFAULT (1),
    CreatedAt    DATETIMEOFFSET(0)  NOT NULL CONSTRAINT DF_Company_CreatedAt DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT PK_Company PRIMARY KEY (CompanyId),
    CONSTRAINT UQ_Company_TaxId UNIQUE (TaxId)
);
GO

CREATE TABLE dbo.Equipment (
    EquipmentId      INT IDENTITY(1, 1) NOT NULL,
    CompanyId        INT                NOT NULL,
    EquipmentTypeId  INT                NOT NULL,
    Brand            NVARCHAR(100)      NOT NULL,   -- obligatorios: clave de los perfiles de eficacia por marca y modelo
    Model            NVARCHAR(100)      NOT NULL,
    IsModelConfirmed BIT                NOT NULL CONSTRAINT DF_Equipment_ModelConfirmed DEFAULT (1),  -- 0 = inferido (sin placa), ver docs/data
    SerialNumber     NVARCHAR(100)      NOT NULL,
    InternalCode     NVARCHAR(50)       NULL,   -- código patrimonial del cliente
    Notes            NVARCHAR(500)      NULL,
    IsActive         BIT                NOT NULL CONSTRAINT DF_Equipment_IsActive DEFAULT (1),
    CreatedAt        DATETIMEOFFSET(0)  NOT NULL CONSTRAINT DF_Equipment_CreatedAt DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT PK_Equipment PRIMARY KEY (EquipmentId),
    CONSTRAINT FK_Equipment_Company FOREIGN KEY (CompanyId) REFERENCES dbo.Company (CompanyId),
    CONSTRAINT FK_Equipment_EquipmentType FOREIGN KEY (EquipmentTypeId) REFERENCES dbo.EquipmentType (EquipmentTypeId),
    CONSTRAINT UQ_Equipment_Company_Serial UNIQUE (CompanyId, SerialNumber),
    CONSTRAINT CK_Equipment_BrandModel CHECK (LEN(Brand) > 0 AND LEN(Model) > 0)
);
GO

CREATE INDEX IX_Equipment_EquipmentTypeId ON dbo.Equipment (EquipmentTypeId);
GO

/* ---------------------------------------------------------------------
   Adquisidor (Arduino / Raspberry Pi por puerto COM)
   --------------------------------------------------------------------- */

CREATE TABLE dbo.AcquisitionDevice (
    AcquisitionDeviceId  INT IDENTITY(1, 1) NOT NULL,
    DeviceIdentifier     VARCHAR(50)        NOT NULL,   -- respuesta al comando "identificar"
    Platform             VARCHAR(20)        NOT NULL,   -- 'Simulator' = adquisidor simulado con el set de datos de prueba
    FirmwareVersion      VARCHAR(20)        NULL,
    ChannelCount         TINYINT            NOT NULL CONSTRAINT DF_AcquisitionDevice_ChannelCount DEFAULT (10),
    AcquisitionMode      VARCHAR(10)        NOT NULL CONSTRAINT DF_AcquisitionDevice_Mode DEFAULT ('Poll'), -- Poll: la PC pide cada muestra; Stream: el adquisidor transmite solo
    Notes                NVARCHAR(500)      NULL,
    IsActive             BIT                NOT NULL CONSTRAINT DF_AcquisitionDevice_IsActive DEFAULT (1),
    CreatedAt            DATETIMEOFFSET(0)  NOT NULL CONSTRAINT DF_AcquisitionDevice_CreatedAt DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT PK_AcquisitionDevice PRIMARY KEY (AcquisitionDeviceId),
    CONSTRAINT UQ_AcquisitionDevice_Identifier UNIQUE (DeviceIdentifier),
    CONSTRAINT CK_AcquisitionDevice_Platform CHECK (Platform IN ('Arduino', 'RaspberryPi', 'Other', 'Simulator')),
    CONSTRAINT CK_AcquisitionDevice_Mode CHECK (AcquisitionMode IN ('Poll', 'Stream')),
    CONSTRAINT CK_AcquisitionDevice_ChannelCount CHECK (ChannelCount BETWEEN 1 AND 27)   -- D-06: hasta 27 canales
);
GO

/* ---------------------------------------------------------------------
   Sesión de medición
   --------------------------------------------------------------------- */

CREATE TABLE dbo.MeasurementSession (
    MeasurementSessionId        INT IDENTITY(1, 1) NOT NULL,
    EquipmentId                 INT                NOT NULL,
    TechnicianId                INT                NOT NULL,
    AcquisitionDeviceId         INT                NULL,
    ComPort                     VARCHAR(20)        NOT NULL,   -- p. ej. COM3, /dev/ttyUSB0 o SIM (simulador)
    SensorGroupId               VARCHAR(50)        NULL,       -- grupo de sensores informado por el adquisidor (IDN)
    -- [PC-01] Punto de cambio: intervalo de muestreo. Ver docs/specs/functional/01-vision-document.md §12
    SamplingIntervalSeconds     SMALLINT           NOT NULL CONSTRAINT DF_MeasurementSession_Interval DEFAULT (120),
    -- Duración planificada: base 60 min, o la exigida por el tipo de equipo, o la solicitada por el cliente (hasta varios días)
    PlannedDurationMinutes      INT                NOT NULL CONSTRAINT DF_MeasurementSession_Planned DEFAULT (60),
    DurationSource              VARCHAR(20)        NOT NULL CONSTRAINT DF_MeasurementSession_DurationSource DEFAULT ('Base'),
    ClientRequestReference      NVARCHAR(100)      NULL,       -- p. ej. orden de servicio del cliente
    -- Copia del criterio de límite vigente al iniciar: editar el tipo de equipo no altera sesiones pasadas (RN-08).
    -- Range: MinTemperatureC y/o MaxTemperatureC. Band: SetpointC (consigna de esta sesión, la indica el técnico) ± ToleranceK.
    LimitMode                   VARCHAR(10)        NOT NULL CONSTRAINT DF_MeasurementSession_LimitMode DEFAULT ('Range'),
    MinTemperatureC             DECIMAL(6, 2)      NULL,
    MaxTemperatureC             DECIMAL(6, 2)      NULL,
    SetpointC                   DECIMAL(6, 2)      NULL,
    ToleranceK                  DECIMAL(4, 2)      NULL,
    -- Copias de la política de pérdida de sensores vigente al iniciar:
    -- muestra afectada si el % de canales sin lectura OK es MAYOR que el umbral;
    -- alerta crítica si sigue afectada en la N-ésima muestra consecutiva;
    -- sesión fallida si lleva FailMinutes consecutivos afectada.
    SensorLossThresholdPct      DECIMAL(5, 2)      NOT NULL CONSTRAINT DF_MeasurementSession_SensorLoss DEFAULT (60.00),
    SensorLossCriticalAfterSamples TINYINT         NOT NULL CONSTRAINT DF_MeasurementSession_LossCritical DEFAULT (3),
    SensorLossFailMinutes       SMALLINT           NOT NULL CONSTRAINT DF_MeasurementSession_LossFail DEFAULT (30),
    -- Copia al iniciar: un canal fuera de límite estos minutos seguidos genera una alerta crítica
    AboveLimitCriticalMinutes   SMALLINT           NOT NULL CONSTRAINT DF_MeasurementSession_AboveCritical DEFAULT (30),
    -- Inicio antes de terminar el descanso del adquisidor: solo con autorización registrada
    RestOverrideById            INT                NULL,
    RestOverrideReason          NVARCHAR(300)      NULL,
    -- Copia al iniciar del mínimo de puntos de medición del tipo de equipo (9: 8 esquinas + centro, IEC 60068-3-5 /
    -- DKD-R 5-7; 27 en incubadoras de más de 50 L, DIN 12880).
    -- Con menos canales se permite iniciar, pero con advertencia y confirmación del técnico.
    MinMeasurementPoints        TINYINT            NOT NULL CONSTRAINT DF_MeasurementSession_MinPoints DEFAULT (9),
    IsBelowMinimumPoints        BIT                NOT NULL CONSTRAINT DF_MeasurementSession_BelowMin DEFAULT (0),
    BelowMinimumAcknowledgedAt  DATETIMEOFFSET(0)  NULL,
    HasMixedThermocoupleTypes   BIT                NOT NULL CONSTRAINT DF_MeasurementSession_Mixed DEFAULT (0),
    MixedTypesAcknowledgedAt    DATETIMEOFFSET(0)  NULL,       -- confirmación del técnico ante la advertencia
    -- Sesiones con datos simulados (set de datos de prueba): nunca válidas para calibración
    IsSimulation                BIT                NOT NULL CONSTRAINT DF_MeasurementSession_IsSimulation DEFAULT (0),
    TestScenarioCode            VARCHAR(20)        NULL,       -- p. ej. TD-04
    Status                      VARCHAR(20)        NOT NULL CONSTRAINT DF_MeasurementSession_Status DEFAULT ('Configured'),
    CloseReason                 VARCHAR(20)        NULL,
    StartedAt                   DATETIMEOFFSET(0)  NULL,       -- llegada de la primera muestra válida, no el clic en "Iniciar"
    EndedAt                     DATETIMEOFFSET(0)  NULL,
    Notes                       NVARCHAR(1000)     NULL,
    CreatedAt                   DATETIMEOFFSET(0)  NOT NULL CONSTRAINT DF_MeasurementSession_CreatedAt DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT PK_MeasurementSession PRIMARY KEY (MeasurementSessionId),
    CONSTRAINT FK_MeasurementSession_Equipment FOREIGN KEY (EquipmentId) REFERENCES dbo.Equipment (EquipmentId),
    CONSTRAINT FK_MeasurementSession_Technician FOREIGN KEY (TechnicianId) REFERENCES dbo.AppUser (AppUserId),
    CONSTRAINT FK_MeasurementSession_Device FOREIGN KEY (AcquisitionDeviceId) REFERENCES dbo.AcquisitionDevice (AcquisitionDeviceId),
    CONSTRAINT FK_MeasurementSession_RestOverrideBy FOREIGN KEY (RestOverrideById) REFERENCES dbo.AppUser (AppUserId),
    -- Invalid = sesión fallida / no válida
    CONSTRAINT CK_MeasurementSession_Status CHECK (Status IN ('Configured', 'Running', 'Completed', 'Incomplete', 'Invalid', 'Cancelled')),
    CONSTRAINT CK_MeasurementSession_CloseReason CHECK (CloseReason IS NULL OR CloseReason IN ('Manual', 'PlannedDuration', 'Cancelled', 'CommunicationLost', 'DeviceMismatch', 'DataLoss')),
    -- Sesión fallida: otro adquisidor u otro grupo de sensores al reconectar, o pérdida sostenida de datos
    CONSTRAINT CK_MeasurementSession_Invalid CHECK (
        (Status = 'Invalid' AND CloseReason IN ('DeviceMismatch', 'DataLoss'))
        OR (Status <> 'Invalid' AND (CloseReason IS NULL OR CloseReason NOT IN ('DeviceMismatch', 'DataLoss')))),
    CONSTRAINT CK_MeasurementSession_Interval CHECK (SamplingIntervalSeconds > 0),
    CONSTRAINT CK_MeasurementSession_Planned CHECK (PlannedDurationMinutes BETWEEN 60 AND 43200),
    CONSTRAINT CK_MeasurementSession_DurationSource CHECK (DurationSource IN ('Base', 'EquipmentType', 'ClientRequest')),
    CONSTRAINT CK_MeasurementSession_ClientRequest CHECK (DurationSource <> 'ClientRequest' OR ClientRequestReference IS NOT NULL),
    CONSTRAINT CK_MeasurementSession_SensorLoss CHECK (SensorLossThresholdPct > 0 AND SensorLossThresholdPct < 100),
    CONSTRAINT CK_MeasurementSession_LossEscalation CHECK (SensorLossCriticalAfterSamples BETWEEN 2 AND 10 AND SensorLossFailMinutes BETWEEN 10 AND 240),
    CONSTRAINT CK_MeasurementSession_AboveCritical CHECK (AboveLimitCriticalMinutes BETWEEN 10 AND 240),
    CONSTRAINT CK_MeasurementSession_RestOverride CHECK ((RestOverrideById IS NULL AND RestOverrideReason IS NULL) OR (RestOverrideById IS NOT NULL AND RestOverrideReason IS NOT NULL)),
    CONSTRAINT CK_MeasurementSession_TestScenario CHECK (IsSimulation = 1 OR TestScenarioCode IS NULL),
    CONSTRAINT CK_MeasurementSession_Dates CHECK (EndedAt IS NULL OR (StartedAt IS NOT NULL AND EndedAt >= StartedAt)),
    -- Duración máxima: la planificada (se puede extender durante la sesión, hasta 30 días)
    CONSTRAINT CK_MeasurementSession_MaxDuration CHECK (EndedAt IS NULL OR DATEDIFF(SECOND, StartedAt, EndedAt) <= PlannedDurationMinutes * 60),
    -- Solo puede quedar como completa si cumplió la duración planificada (la exigencia de 31 muestras válidas la aplica el dominio)
    CONSTRAINT CK_MeasurementSession_MinDuration CHECK (Status <> 'Completed' OR DATEDIFF(SECOND, StartedAt, EndedAt) >= PlannedDurationMinutes * 60),
    CONSTRAINT CK_MeasurementSession_MinPoints CHECK (MinMeasurementPoints BETWEEN 1 AND 27),
    -- Banda: la consigna es obligatoria para iniciar; sin tolerancia definida, el límite queda pendiente
    CONSTRAINT CK_MeasurementSession_LimitMode CHECK (
        (LimitMode = 'Range' AND SetpointC IS NULL AND ToleranceK IS NULL
            AND (MinTemperatureC IS NULL OR MaxTemperatureC IS NULL OR MinTemperatureC < MaxTemperatureC))
        OR (LimitMode = 'Band' AND MinTemperatureC IS NULL AND MaxTemperatureC IS NULL AND (ToleranceK IS NULL OR ToleranceK > 0)
            AND (Status IN ('Configured', 'Cancelled') OR SetpointC IS NOT NULL))),
    -- Con menos puntos que el mínimo, la sesión no puede iniciar sin la confirmación del técnico
    CONSTRAINT CK_MeasurementSession_BelowMinAck CHECK (IsBelowMinimumPoints = 0 OR Status IN ('Configured', 'Cancelled') OR BelowMinimumAcknowledgedAt IS NOT NULL),
    -- Si hay mezcla de tipos, la sesión no puede iniciar sin la confirmación del técnico
    CONSTRAINT CK_MeasurementSession_MixedAck CHECK (HasMixedThermocoupleTypes = 0 OR Status IN ('Configured', 'Cancelled') OR MixedTypesAcknowledgedAt IS NOT NULL)
);
GO

CREATE INDEX IX_MeasurementSession_EquipmentId_StartedAt ON dbo.MeasurementSession (EquipmentId, StartedAt);
CREATE INDEX IX_MeasurementSession_Status ON dbo.MeasurementSession (Status);
-- Última sesión de cada adquisidor, para controlar el periodo de descanso
CREATE INDEX IX_MeasurementSession_Device_EndedAt ON dbo.MeasurementSession (AcquisitionDeviceId, EndedAt);
GO

-- Canales (sensores) usados en la sesión. El tipo se guarda por canal para permitir mezcla.
CREATE TABLE dbo.SessionChannel (
    SessionChannelId      INT IDENTITY(1, 1) NOT NULL,
    MeasurementSessionId  INT                NOT NULL,
    ChannelNumber         TINYINT            NOT NULL,
    ThermocoupleTypeCode  CHAR(1)            NOT NULL,
    SensorLabel           NVARCHAR(50)       NULL,   -- etiqueta física del sensor, si existe
    Position              NVARCHAR(100)      NOT NULL, -- superior, centro, inferior, puerta, fondo...
    IsActive              BIT                NOT NULL CONSTRAINT DF_SessionChannel_IsActive DEFAULT (1),
    CONSTRAINT PK_SessionChannel PRIMARY KEY (SessionChannelId),
    CONSTRAINT FK_SessionChannel_Session FOREIGN KEY (MeasurementSessionId) REFERENCES dbo.MeasurementSession (MeasurementSessionId),
    CONSTRAINT FK_SessionChannel_ThermocoupleType FOREIGN KEY (ThermocoupleTypeCode) REFERENCES dbo.ThermocoupleType (ThermocoupleTypeCode),
    CONSTRAINT UQ_SessionChannel_Session_Channel UNIQUE (MeasurementSessionId, ChannelNumber),
    CONSTRAINT CK_SessionChannel_ChannelNumber CHECK (ChannelNumber BETWEEN 1 AND 27)   -- D-06: hasta 27 canales (DIN 12880)
);
GO

/* ---------------------------------------------------------------------
   Lecturas
   --------------------------------------------------------------------- */

-- Una fila por canal y por ciclo de muestreo (cada 2 minutos).
-- La muestra 1 es la primera recibida (t = 0). Sesión base de 1 h = 31 muestras;
-- la última muestra depende de la duración planificada (24 h = 721, 72 h = 2 161).
-- Volumen típico por sesión base: 10 canales x 31 muestras = 310 filas.
CREATE TABLE dbo.Reading (
    ReadingId                 BIGINT IDENTITY(1, 1) NOT NULL,
    SessionChannelId          INT                   NOT NULL,
    SampleNumber              INT                   NOT NULL,   -- 1 en t = 0, n en t = (n - 1) x 2 min
    ReadAt                    DATETIMEOFFSET(0)     NOT NULL,
    TemperatureC              DECIMAL(7, 2)         NULL,       -- NULL si la lectura es inválida
    SensorStatus              VARCHAR(20)           NOT NULL,
    ReportedThermocoupleType  CHAR(1)               NULL,       -- tipo informado por el adquisidor
    IsAboveLimit              BIT                   NOT NULL CONSTRAINT DF_Reading_IsAboveLimit DEFAULT (0),   -- > máximo, o > consigna + tolerancia
    IsBelowLimit              BIT                   NOT NULL CONSTRAINT DF_Reading_IsBelowLimit DEFAULT (0),   -- < mínimo, o < consigna - tolerancia
    RawFrame                  VARCHAR(200)          NULL,       -- trama serial original para auditoría
    ReceivedAt                DATETIMEOFFSET(3)     NOT NULL CONSTRAINT DF_Reading_ReceivedAt DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT PK_Reading PRIMARY KEY (ReadingId),
    CONSTRAINT FK_Reading_SessionChannel FOREIGN KEY (SessionChannelId) REFERENCES dbo.SessionChannel (SessionChannelId),
    CONSTRAINT UQ_Reading_Channel_Sample UNIQUE (SessionChannelId, SampleNumber),
    CONSTRAINT CK_Reading_SampleNumber CHECK (SampleNumber >= 1),   -- el tope lo fija la duración planificada de la sesión
    CONSTRAINT CK_Reading_SensorStatus CHECK (SensorStatus IN ('OK', 'OpenCircuit', 'ShortCircuit', 'OutOfRange', 'InvalidFrame', 'TypeMismatch')),
    CONSTRAINT CK_Reading_TemperatureWhenOk CHECK (SensorStatus <> 'OK' OR TemperatureC IS NOT NULL),
    CONSTRAINT CK_Reading_LimitSide CHECK (IsAboveLimit = 0 OR IsBelowLimit = 0)
);
GO

CREATE INDEX IX_Reading_SessionChannelId_ReadAt ON dbo.Reading (SessionChannelId, ReadAt) INCLUDE (TemperatureC, SensorStatus, IsAboveLimit, IsBelowLimit);
GO

-- Pérdidas de comunicación con el puerto COM (huecos de datos)
CREATE TABLE dbo.CommunicationGap (
    CommunicationGapId    INT IDENTITY(1, 1) NOT NULL,
    MeasurementSessionId  INT                NOT NULL,
    LostAt                DATETIMEOFFSET(0)  NOT NULL,
    RecoveredAt           DATETIMEOFFSET(0)  NULL,
    MissedSamples         INT                NULL,
    Notes                 NVARCHAR(500)      NULL,
    CONSTRAINT PK_CommunicationGap PRIMARY KEY (CommunicationGapId),
    CONSTRAINT FK_CommunicationGap_Session FOREIGN KEY (MeasurementSessionId) REFERENCES dbo.MeasurementSession (MeasurementSessionId),
    CONSTRAINT CK_CommunicationGap_Dates CHECK (RecoveredAt IS NULL OR RecoveredAt >= LostAt)
);
GO

/* ---------------------------------------------------------------------
   Alertas
   --------------------------------------------------------------------- */

CREATE TABLE dbo.Alert (
    AlertId               BIGINT IDENTITY(1, 1) NOT NULL,
    MeasurementSessionId  INT                   NOT NULL,
    SessionChannelId      INT                   NULL,
    ReadingId             BIGINT                NULL,
    AlertType             VARCHAR(30)           NOT NULL,
    Severity              VARCHAR(10)           NOT NULL,
    OccurredAt            DATETIMEOFFSET(0)     NOT NULL CONSTRAINT DF_Alert_OccurredAt DEFAULT (SYSDATETIMEOFFSET()),
    ValueC                DECIMAL(7, 2)         NULL,
    LimitC                DECIMAL(6, 2)         NULL,
    Message               NVARCHAR(500)         NOT NULL,
    -- Solo en AboveLimitSustained / BelowLimitSustained: Sensor = solo una minoría de canales fuera de límite (revisar el termopar);
    -- Equipment = la mayoría de canales válidos fuera de límite (revisar el equipo)
    SuspectedCause        VARCHAR(20)           NULL,
    AcknowledgedById      INT                   NULL,
    AcknowledgedAt        DATETIMEOFFSET(0)     NULL,
    CONSTRAINT PK_Alert PRIMARY KEY (AlertId),
    CONSTRAINT FK_Alert_Session FOREIGN KEY (MeasurementSessionId) REFERENCES dbo.MeasurementSession (MeasurementSessionId),
    CONSTRAINT FK_Alert_SessionChannel FOREIGN KEY (SessionChannelId) REFERENCES dbo.SessionChannel (SessionChannelId),
    CONSTRAINT FK_Alert_Reading FOREIGN KEY (ReadingId) REFERENCES dbo.Reading (ReadingId),
    CONSTRAINT FK_Alert_AcknowledgedBy FOREIGN KEY (AcknowledgedById) REFERENCES dbo.AppUser (AppUserId),
    CONSTRAINT CK_Alert_SuspectedCause CHECK (
        (AlertType IN ('AboveLimitSustained', 'BelowLimitSustained') AND SuspectedCause IS NOT NULL AND SuspectedCause IN ('Sensor', 'Equipment'))
        OR (AlertType NOT IN ('AboveLimitSustained', 'BelowLimitSustained') AND SuspectedCause IS NULL)),
    CONSTRAINT CK_Alert_AlertType CHECK (AlertType IN ('AboveLimit', 'AboveLimitSustained', 'BelowLimit', 'BelowLimitSustained', 'MixedThermocoupleTypes', 'BelowMinimumPoints', 'TypeMismatch', 'SensorFault',
                                                       'SensorLoss', 'SensorLossPersistent', 'SessionFailed',
                                                       'CommunicationLost', 'DeviceMismatch', 'LimitNotDefined')),
    -- Critical: se notifica visualmente y exige reconocimiento. Warning e Info: solo se registran y se listan.
    CONSTRAINT CK_Alert_Severity CHECK (Severity IN ('Info', 'Warning', 'Critical')),
    CONSTRAINT CK_Alert_Ack CHECK ((AcknowledgedById IS NULL AND AcknowledgedAt IS NULL) OR (AcknowledgedById IS NOT NULL AND AcknowledgedAt IS NOT NULL))
);
GO

CREATE INDEX IX_Alert_Session_OccurredAt ON dbo.Alert (MeasurementSessionId, OccurredAt);
GO

/* ---------------------------------------------------------------------
   Exportaciones a Excel
   --------------------------------------------------------------------- */

CREATE TABLE dbo.SessionExport (
    SessionExportId       INT IDENTITY(1, 1) NOT NULL,
    MeasurementSessionId  INT                NOT NULL,
    ExportedById          INT                NOT NULL,
    ExportedAt            DATETIMEOFFSET(0)  NOT NULL CONSTRAINT DF_SessionExport_ExportedAt DEFAULT (SYSDATETIMEOFFSET()),
    FileName              NVARCHAR(260)      NOT NULL,
    CONSTRAINT PK_SessionExport PRIMARY KEY (SessionExportId),
    CONSTRAINT FK_SessionExport_Session FOREIGN KEY (MeasurementSessionId) REFERENCES dbo.MeasurementSession (MeasurementSessionId),
    CONSTRAINT FK_SessionExport_ExportedBy FOREIGN KEY (ExportedById) REFERENCES dbo.AppUser (AppUserId)
);
GO

/* ---------------------------------------------------------------------
   Vistas de apoyo
   --------------------------------------------------------------------- */

-- Detecta sesiones con mezcla de tipos de termopar (para validar el flag de la sesión)
CREATE VIEW dbo.vSessionThermocoupleMix
AS
SELECT
    sc.MeasurementSessionId,
    COUNT(DISTINCT sc.ThermocoupleTypeCode) AS ThermocoupleTypeCount,
    CAST(CASE WHEN COUNT(DISTINCT sc.ThermocoupleTypeCode) > 1 THEN 1 ELSE 0 END AS BIT) AS IsMixed
FROM dbo.SessionChannel AS sc
WHERE sc.IsActive = 1
GROUP BY sc.MeasurementSessionId;
GO

-- Base para la hoja "Lecturas" del Excel: una fila por muestra, una columna por canal
CREATE VIEW dbo.vSessionReadingPivot
AS
SELECT
    p.MeasurementSessionId,
    p.SampleNumber,
    p.ReadAt,
    p.[1] AS S1, p.[2] AS S2, p.[3] AS S3, p.[4] AS S4, p.[5] AS S5, p.[6] AS S6, p.[7] AS S7,
    p.[8] AS S8, p.[9] AS S9, p.[10] AS S10, p.[11] AS S11, p.[12] AS S12, p.[13] AS S13, p.[14] AS S14,
    p.[15] AS S15, p.[16] AS S16, p.[17] AS S17, p.[18] AS S18, p.[19] AS S19, p.[20] AS S20, p.[21] AS S21,
    p.[22] AS S22, p.[23] AS S23, p.[24] AS S24, p.[25] AS S25, p.[26] AS S26, p.[27] AS S27
FROM (
    SELECT
        sc.MeasurementSessionId,
        r.SampleNumber,
        MIN(r.ReadAt) OVER (PARTITION BY sc.MeasurementSessionId, r.SampleNumber) AS ReadAt,
        sc.ChannelNumber,
        r.TemperatureC
    FROM dbo.Reading AS r
    INNER JOIN dbo.SessionChannel AS sc ON sc.SessionChannelId = r.SessionChannelId
) AS src
PIVOT (MAX(TemperatureC) FOR ChannelNumber IN ([1], [2], [3], [4], [5], [6], [7], [8], [9], [10], [11], [12], [13], [14], [15], [16], [17], [18], [19], [20], [21], [22], [23], [24], [25], [26], [27])) AS p;
GO

-- Cobertura por muestra programada, incluidas las perdidas por huecos de comunicación.
-- Una muestra está afectada si el % de canales activos sin lectura OK es MAYOR que SensorLossThresholdPct.
-- Una sesión solo puede quedar completa con al menos 31 muestras no afectadas (1 h de datos).
CREATE VIEW dbo.vSessionSampleCoverage
AS
SELECT
    s.MeasurementSessionId,
    num.N AS SampleNumber,
    DATEADD(SECOND, (num.N - 1) * s.SamplingIntervalSeconds, s.StartedAt) AS ScheduledAt,
    ch.ActiveChannels,
    COALESCE(r.ReadingRows, 0) AS ReadingRows,
    COALESCE(r.ValidReadings, 0) AS ValidReadings,
    CAST(CASE WHEN (ch.ActiveChannels - COALESCE(r.ValidReadings, 0)) * 100.0 > s.SensorLossThresholdPct * ch.ActiveChannels
              THEN 1 ELSE 0 END AS BIT) AS IsAffected
FROM dbo.MeasurementSession AS s
CROSS APPLY (
    SELECT COUNT(*) AS ActiveChannels
    FROM dbo.SessionChannel AS sc
    WHERE sc.MeasurementSessionId = s.MeasurementSessionId AND sc.IsActive = 1
) AS ch
INNER JOIN dbo.Tally AS num
    ON num.N <= DATEDIFF(SECOND, s.StartedAt, COALESCE(s.EndedAt, SYSDATETIMEOFFSET())) / s.SamplingIntervalSeconds + 1
   AND num.N <= s.PlannedDurationMinutes * 60 / s.SamplingIntervalSeconds + 1
OUTER APPLY (
    SELECT
        COUNT(*) AS ReadingRows,
        SUM(CASE WHEN rd.SensorStatus = 'OK' THEN 1 ELSE 0 END) AS ValidReadings
    FROM dbo.Reading AS rd
    INNER JOIN dbo.SessionChannel AS sc2 ON sc2.SessionChannelId = rd.SessionChannelId
    WHERE sc2.MeasurementSessionId = s.MeasurementSessionId
      AND sc2.IsActive = 1
      AND rd.SampleNumber = num.N
) AS r
WHERE s.StartedAt IS NOT NULL
  AND ch.ActiveChannels > 0;
GO

/* ---------------------------------------------------------------------
   Datos iniciales
   --------------------------------------------------------------------- */

INSERT INTO dbo.ThermocoupleType (ThermocoupleTypeCode, Name, MinRangeC, MaxRangeC)
VALUES
    ('T', N'Termopar tipo T (cobre-constantán)', -200.00, 350.00),
    ('K', N'Termopar tipo K (cromel-alumel)',    -200.00, 1260.00);
GO

-- Solo la congeladora tiene límite definido; el resto queda pendiente (NULL)
-- Límites SUGERIDOS por la normativa (IsLimitSuggested = 1): el laboratorio debe confirmarlos (D-07, P-03).
INSERT INTO dbo.EquipmentType (Name, LimitMode, MinTemperatureC, MaxTemperatureC, ToleranceK, IsLimitSuggested, MinMeasurementPoints, Description)
VALUES
    (N'Refrigeradora',    'Range', 2.00, 8.00,  NULL, 1, 9,  N'Sugerido: +2 a +8 °C (OMS PQS E003, vacunas y medicamentos). Banco de sangre: +1 a +6 °C (AABB). 9 puntos (USP <1079.4>)'),
    (N'Congeladora',      'Range', NULL, -5.00, NULL, 0, 9,  N'No debe superar -5,0 °C; -4,9 °C ya está fuera de límite. 9 puntos'),
    (N'Conservadora',     'Range', 2.00, 8.00,  NULL, 1, 9,  N'Sugerido: +2 a +8 °C (cadena de frío de vacunas, OMS). 9 puntos'),
    (N'Incubadora',       'Band',  NULL, NULL,  1.00, 1, 27, N'Sugerido: consigna ± 1,0 K (p. ej. 37 ± 1 °C). 27 puntos (DIN 12880, más de 50 L; 9 si es de 50 L o menos)'),
    (N'Cámara ambiental', 'Band',  NULL, NULL,  2.00, 1, 9,  N'Sugerido: consigna ± 2,0 K (homogeneidad máxima declarada por Memmert CTC/TTC). 9 puntos (IEC 60068-3-5, hasta 2000 L). Ver docs/data/DATA-1-analisis.md');
GO

-- Parámetros iniciales (confirmados por el laboratorio el 2026-09-25)
INSERT INTO dbo.AppSetting (SettingKey, SettingValue, Description)
VALUES
    ('SamplingIntervalSeconds',        N'120',   N'[PC-01] Intervalo de muestreo (s). 60 o menos para DKD-R 5-7 / IEC 60068-3-5'),   -- [PC-01] punto de cambio
    ('BaseSessionMinutes',             N'60',    N'Duración base de una sesión (min)'),
    ('MaxSessionMinutes',              N'10080', N'Duración máxima que se puede planificar (min); 7 días. La base admite hasta 30 días'),
    ('RestPeriodMinutes',              N'15',    N'Descanso mínimo del kit de medición (adquisidor y sensores) entre sesiones (min)'),
    ('AboveLimitCriticalMinutes',      N'30',    N'Un canal fuera de límite estos minutos seguidos genera una alerta crítica con causa probable'),
    ('SensorLossThresholdPct',         N'60',    N'Muestra afectada si MÁS de este % de canales no tiene lectura válida'),
    ('SensorLossCriticalAfterSamples', N'3',     N'La pérdida pasa a alerta crítica si sigue en la N-ésima muestra consecutiva'),
    ('SensorLossFailMinutes',          N'30',    N'La sesión falla tras estos minutos consecutivos de muestras afectadas');
GO
