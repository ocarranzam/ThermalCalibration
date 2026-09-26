/* =====================================================================
   Datos de DESARROLLO y demostración (no se usan en producción ni en las pruebas automatizadas)
   Se ejecuta después de 01-schema.sql. Es idempotente: se puede repetir sin duplicar filas.
   docker compose lo aplica al crear la base (servicio db-init).
   ===================================================================== */

USE ThermalCalibration;
GO

SET NOCOUNT ON;
GO

-- Empresa de ejemplo de HU-01 (nombre ficticio; RUC con dígito verificador válido)
IF NOT EXISTS (SELECT 1 FROM dbo.Company WHERE TaxId = '20100070970')
    INSERT INTO dbo.Company (TaxId, Name, ContactName, Email)
    VALUES ('20100070970', N'Laboratorios Andinos S.A.C.', N'Ana Torres', N'calidad@andinos.pe');
GO

-- Equipos de ejemplo:
--   - La cámara ambiental del primer registro real (DATA-1): marca Memmert, modelo INFERIDO con la ficha del
--     fabricante (IsModelConfirmed = 0, pendiente de confirmar en la placa). Ver docs/data/DATA-1-analisis.md.
--   - Una congeladora como la de los escenarios de HU-01.
DECLARE @CompanyId INT = (SELECT CompanyId FROM dbo.Company WHERE TaxId = '20100070970');
DECLARE @ChamberTypeId INT = (SELECT EquipmentTypeId FROM dbo.EquipmentType WHERE Name = N'Cámara ambiental');
DECLARE @FreezerTypeId INT = (SELECT EquipmentTypeId FROM dbo.EquipmentType WHERE Name = N'Congeladora');

IF NOT EXISTS (SELECT 1 FROM dbo.Equipment WHERE CompanyId = @CompanyId AND SerialNumber = N'DATA-1')
    INSERT INTO dbo.Equipment (CompanyId, EquipmentTypeId, Brand, Model, IsModelConfirmed, SerialNumber, InternalCode, Notes)
    VALUES (@CompanyId, @ChamberTypeId, N'Memmert', N'TTC256', 0, N'DATA-1', NULL,
            N'Primer registro real: 72 h, 12 termopares tipo T, cada 5 min. Serie real desconocida; modelo inferido.');

IF NOT EXISTS (SELECT 1 FROM dbo.Equipment WHERE CompanyId = @CompanyId AND SerialNumber = N'SN-88231')
    INSERT INTO dbo.Equipment (CompanyId, EquipmentTypeId, Brand, Model, IsModelConfirmed, SerialNumber, InternalCode)
    VALUES (@CompanyId, @FreezerTypeId, N'Haier', N'HBF-205', 1, N'SN-88231', N'CONG-03');
GO
