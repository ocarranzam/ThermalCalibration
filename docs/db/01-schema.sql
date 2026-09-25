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

-- Tipos de equipo con límite MÁXIMO configurable.
-- Regla: una lectura está fuera de límite si TemperatureC > MaxTemperatureC.
-- MaxTemperatureC admite NULL mientras el límite no esté definido.
CREATE TABLE dbo.EquipmentType (
    EquipmentTypeId   INT IDENTITY(1, 1) NOT NULL,
    Name              NVARCHAR(100)      NOT NULL,
    MaxTemperatureC   DECIMAL(6, 2)      NULL,
    Description       NVARCHAR(500)      NULL,
    IsActive          BIT                NOT NULL CONSTRAINT DF_EquipmentType_IsActive DEFAULT (1),
    CreatedAt         DATETIMEOFFSET(0)  NOT NULL CONSTRAINT DF_EquipmentType_CreatedAt DEFAULT (SYSDATETIMEOFFSET()),
    UpdatedAt         DATETIMEOFFSET(0)  NULL,
    CONSTRAINT PK_EquipmentType PRIMARY KEY (EquipmentTypeId),
    CONSTRAINT UQ_EquipmentType_Name UNIQUE (Name)
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
    Brand            NVARCHAR(100)      NULL,
    Model            NVARCHAR(100)      NULL,
    SerialNumber     NVARCHAR(100)      NOT NULL,
    InternalCode     NVARCHAR(50)       NULL,   -- código patrimonial del cliente
    Notes            NVARCHAR(500)      NULL,
    IsActive         BIT                NOT NULL CONSTRAINT DF_Equipment_IsActive DEFAULT (1),
    CreatedAt        DATETIMEOFFSET(0)  NOT NULL CONSTRAINT DF_Equipment_CreatedAt DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT PK_Equipment PRIMARY KEY (EquipmentId),
    CONSTRAINT FK_Equipment_Company FOREIGN KEY (CompanyId) REFERENCES dbo.Company (CompanyId),
    CONSTRAINT FK_Equipment_EquipmentType FOREIGN KEY (EquipmentTypeId) REFERENCES dbo.EquipmentType (EquipmentTypeId),
    CONSTRAINT UQ_Equipment_Company_Serial UNIQUE (CompanyId, SerialNumber)
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
    CONSTRAINT CK_AcquisitionDevice_ChannelCount CHECK (ChannelCount BETWEEN 1 AND 10)
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
    SamplingIntervalSeconds     SMALLINT           NOT NULL CONSTRAINT DF_MeasurementSession_Interval DEFAULT (120),
    -- Copia del límite vigente al iniciar: editar el tipo de equipo no altera sesiones pasadas
    MaxTemperatureC             DECIMAL(6, 2)      NULL,
    -- Copia del umbral vigente al iniciar: una muestra queda afectada si el % de canales sin lectura OK es MAYOR que este valor
    SensorLossThresholdPct      DECIMAL(5, 2)      NOT NULL CONSTRAINT DF_MeasurementSession_SensorLoss DEFAULT (60.00),
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
    CONSTRAINT CK_MeasurementSession_Status CHECK (Status IN ('Configured', 'Running', 'Completed', 'Incomplete', 'Invalid', 'Cancelled')),
    CONSTRAINT CK_MeasurementSession_CloseReason CHECK (CloseReason IS NULL OR CloseReason IN ('Manual', 'MaxDuration', 'Cancelled', 'CommunicationLost', 'DeviceMismatch')),
    -- Otro adquisidor u otro grupo de sensores al reconectar: la sesión no es válida
    CONSTRAINT CK_MeasurementSession_Invalid CHECK ((Status = 'Invalid' AND CloseReason = 'DeviceMismatch') OR (Status <> 'Invalid' AND (CloseReason IS NULL OR CloseReason <> 'DeviceMismatch'))),
    CONSTRAINT CK_MeasurementSession_Interval CHECK (SamplingIntervalSeconds > 0),
    CONSTRAINT CK_MeasurementSession_SensorLoss CHECK (SensorLossThresholdPct > 0 AND SensorLossThresholdPct < 100),
    CONSTRAINT CK_MeasurementSession_TestScenario CHECK (IsSimulation = 1 OR TestScenarioCode IS NULL),
    CONSTRAINT CK_MeasurementSession_Dates CHECK (EndedAt IS NULL OR (StartedAt IS NOT NULL AND EndedAt >= StartedAt)),
    -- Duración máxima: 24 horas
    CONSTRAINT CK_MeasurementSession_MaxDuration CHECK (EndedAt IS NULL OR DATEDIFF(SECOND, StartedAt, EndedAt) <= 86400),
    -- Solo puede quedar como completa con al menos 1 hora de datos
    CONSTRAINT CK_MeasurementSession_MinDuration CHECK (Status <> 'Completed' OR DATEDIFF(SECOND, StartedAt, EndedAt) >= 3600),
    -- Si hay mezcla de tipos, la sesión no puede iniciar sin la confirmación del técnico
    CONSTRAINT CK_MeasurementSession_MixedAck CHECK (HasMixedThermocoupleTypes = 0 OR Status IN ('Configured', 'Cancelled') OR MixedTypesAcknowledgedAt IS NOT NULL)
);
GO

CREATE INDEX IX_MeasurementSession_EquipmentId_StartedAt ON dbo.MeasurementSession (EquipmentId, StartedAt);
CREATE INDEX IX_MeasurementSession_Status ON dbo.MeasurementSession (Status);
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
    CONSTRAINT CK_SessionChannel_ChannelNumber CHECK (ChannelNumber BETWEEN 1 AND 10)
);
GO

/* ---------------------------------------------------------------------
   Lecturas
   --------------------------------------------------------------------- */

-- Una fila por canal y por ciclo de muestreo (cada 2 minutos).
-- La muestra 1 es la primera recibida (t = 0) y la 721 cierra las 24 h.
-- Máximo esperado por sesión: 10 canales x 721 muestras = 7 210 filas.
CREATE TABLE dbo.Reading (
    ReadingId                 BIGINT IDENTITY(1, 1) NOT NULL,
    SessionChannelId          INT                   NOT NULL,
    SampleNumber              INT                   NOT NULL,   -- 1 en t = 0, n en t = (n - 1) x 2 min
    ReadAt                    DATETIMEOFFSET(0)     NOT NULL,
    TemperatureC              DECIMAL(7, 2)         NULL,       -- NULL si la lectura es inválida
    SensorStatus              VARCHAR(20)           NOT NULL,
    ReportedThermocoupleType  CHAR(1)               NULL,       -- tipo informado por el adquisidor
    IsAboveLimit              BIT                   NOT NULL CONSTRAINT DF_Reading_IsAboveLimit DEFAULT (0),
    RawFrame                  VARCHAR(200)          NULL,       -- trama serial original para auditoría
    ReceivedAt                DATETIMEOFFSET(3)     NOT NULL CONSTRAINT DF_Reading_ReceivedAt DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT PK_Reading PRIMARY KEY (ReadingId),
    CONSTRAINT FK_Reading_SessionChannel FOREIGN KEY (SessionChannelId) REFERENCES dbo.SessionChannel (SessionChannelId),
    CONSTRAINT UQ_Reading_Channel_Sample UNIQUE (SessionChannelId, SampleNumber),
    CONSTRAINT CK_Reading_SampleNumber CHECK (SampleNumber BETWEEN 1 AND 721),
    CONSTRAINT CK_Reading_SensorStatus CHECK (SensorStatus IN ('OK', 'OpenCircuit', 'ShortCircuit', 'OutOfRange', 'InvalidFrame', 'TypeMismatch')),
    CONSTRAINT CK_Reading_TemperatureWhenOk CHECK (SensorStatus <> 'OK' OR TemperatureC IS NOT NULL)
);
GO

CREATE INDEX IX_Reading_SessionChannelId_ReadAt ON dbo.Reading (SessionChannelId, ReadAt) INCLUDE (TemperatureC, SensorStatus, IsAboveLimit);
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
    AcknowledgedById      INT                   NULL,
    AcknowledgedAt        DATETIMEOFFSET(0)     NULL,
    CONSTRAINT PK_Alert PRIMARY KEY (AlertId),
    CONSTRAINT FK_Alert_Session FOREIGN KEY (MeasurementSessionId) REFERENCES dbo.MeasurementSession (MeasurementSessionId),
    CONSTRAINT FK_Alert_SessionChannel FOREIGN KEY (SessionChannelId) REFERENCES dbo.SessionChannel (SessionChannelId),
    CONSTRAINT FK_Alert_Reading FOREIGN KEY (ReadingId) REFERENCES dbo.Reading (ReadingId),
    CONSTRAINT FK_Alert_AcknowledgedBy FOREIGN KEY (AcknowledgedById) REFERENCES dbo.AppUser (AppUserId),
    CONSTRAINT CK_Alert_AlertType CHECK (AlertType IN ('AboveLimit', 'MixedThermocoupleTypes', 'TypeMismatch', 'SensorFault', 'SensorLoss', 'CommunicationLost', 'DeviceMismatch', 'LimitNotDefined')),
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
    p.[1] AS S1, p.[2] AS S2, p.[3] AS S3, p.[4] AS S4, p.[5] AS S5,
    p.[6] AS S6, p.[7] AS S7, p.[8] AS S8, p.[9] AS S9, p.[10] AS S10
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
PIVOT (MAX(TemperatureC) FOR ChannelNumber IN ([1], [2], [3], [4], [5], [6], [7], [8], [9], [10])) AS p;
GO

-- Cobertura por muestra programada, incluidas las perdidas por huecos de comunicación.
-- Una muestra está afectada si el % de canales activos sin lectura OK es MAYOR que SensorLossThresholdPct.
-- Una sesión solo puede quedar completa con al menos 31 muestras no afectadas (1 h de datos).
CREATE VIEW dbo.vSessionSampleCoverage
AS
WITH Digit AS (
    SELECT d FROM (VALUES (0), (1), (2), (3), (4), (5), (6), (7), (8), (9)) AS v (d)
),
Numbers AS (
    SELECT a.d + 10 * b.d + 100 * c.d + 1 AS n
    FROM Digit AS a CROSS JOIN Digit AS b CROSS JOIN Digit AS c
)
SELECT
    s.MeasurementSessionId,
    num.n AS SampleNumber,
    DATEADD(SECOND, (num.n - 1) * s.SamplingIntervalSeconds, s.StartedAt) AS ScheduledAt,
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
INNER JOIN Numbers AS num
    ON num.n <= DATEDIFF(SECOND, s.StartedAt, COALESCE(s.EndedAt, SYSDATETIMEOFFSET())) / s.SamplingIntervalSeconds + 1
   AND num.n <= 721
OUTER APPLY (
    SELECT
        COUNT(*) AS ReadingRows,
        SUM(CASE WHEN rd.SensorStatus = 'OK' THEN 1 ELSE 0 END) AS ValidReadings
    FROM dbo.Reading AS rd
    INNER JOIN dbo.SessionChannel AS sc2 ON sc2.SessionChannelId = rd.SessionChannelId
    WHERE sc2.MeasurementSessionId = s.MeasurementSessionId
      AND sc2.IsActive = 1
      AND rd.SampleNumber = num.n
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
INSERT INTO dbo.EquipmentType (Name, MaxTemperatureC, Description)
VALUES
    (N'Refrigeradora', NULL,  N'Límite máximo pendiente de definir'),
    (N'Congeladora',   -5.00, N'No debe superar -5,0 °C; -4,9 °C ya está fuera de límite'),
    (N'Conservadora',  NULL,  N'Límite máximo pendiente de definir'),
    (N'Incubadora',    NULL,  N'Límite máximo pendiente de definir');
GO
