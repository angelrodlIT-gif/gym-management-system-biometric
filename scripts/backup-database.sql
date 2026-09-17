/*
================================================================================
 Gym System - Script Seguro de Respaldo de Base de Datos
 Archivo: scripts/backup-database.sql
 Motor: Microsoft SQL Server 2019+ / SQL Server Express
 Base de Datos: SistemaGimnasio

 USO VÍA SQLCMD:
   sqlcmd -S .\SQLEXPRESS01 -E -v BackupFolder="<Ruta_Directorio_Backups>" -i scripts/backup-database.sql

 SEGURIDAD Y REGLAS OPERATIVAS:
 - ESTE SCRIPT NO SE EJECUTA POR DEFECTO NI TIENE RUTAS HARDCODEADAS.
 - Requiere explícitamente el parámetro $(BackupFolder) vía sqlcmd (-v BackupFolder="...").
 - Si no se especifica $(BackupFolder), el script falla cerrado inmediatamente sin realizar ninguna acción.
 - Emplea NOFORMAT, NOINIT con nombres únicos basados en fecha/hora/GUID para evitar cualquier riesgo de sobreescritura accidental de medios existentes.
 - Requiere permisos de rol 'db_backupoperator' o 'sysadmin'.
 - No ejecuta operaciones destructivas.
================================================================================
*/

SET NOCOUNT ON;

-- 1. Validar existencia de la base de datos
IF DB_ID(N'SistemaGimnasio') IS NULL
BEGIN
    RAISERROR(N'ERROR CRÍTICO: La base de datos [SistemaGimnasio] no existe en esta instancia.', 16, 1);
    RETURN;
END;

-- 2. Validar parámetro obligatorio de directorio de destino (falla cerrado, sin fallbacks hardcodeados)
DECLARE @DestDir NVARCHAR(4000) = N'$(BackupFolder)';

IF @DestDir IS NULL OR @DestDir = N'' OR @DestDir = N'$(BackupFolder)'
BEGIN
    RAISERROR(N'ERROR CRÍTICO: Debe especificar la variable obligatoria $(BackupFolder) con la ruta de destino. El script no se ejecuta por defecto sin una carpeta explícita definida por el operador.', 16, 1);
    RETURN;
END;

-- Asegurar barra diagonal final
IF RIGHT(RTRIM(@DestDir), 1) <> N'\'
    SET @DestDir = RTRIM(@DestDir) + N'\';

-- 3. Generar nombre de archivo único con marca de tiempo ISO (YYYYMMDD_HHMMSS) y sufijo único
DECLARE @Timestamp NVARCHAR(30) = CONVERT(NVARCHAR(8), GETDATE(), 112) + N'_' +
                                  REPLACE(CONVERT(NVARCHAR(8), GETDATE(), 108), N':', N'') + N'_' +
                                  RIGHT(REPLACE(CAST(NEWID() AS NVARCHAR(36)), N'-', N''), 8);
DECLARE @BackupFile NVARCHAR(4000) = @DestDir + N'SistemaGimnasio_Full_' + @Timestamp + N'.bak';
DECLARE @BackupName NVARCHAR(255)  = N'SistemaGimnasio-Copia Completa-' + @Timestamp;

PRINT N'========================================================================';
PRINT N'Iniciando Respaldo Completo de [SistemaGimnasio]';
PRINT N'Archivo destino: ' + @BackupFile;
PRINT N'Fecha y hora:    ' + CONVERT(NVARCHAR(30), GETDATE(), 120);
PRINT N'========================================================================';

-- 4. Ejecutar el respaldo de forma segura con NOFORMAT, NOINIT y CHECKSUM
BACKUP DATABASE [SistemaGimnasio]
TO DISK = @BackupFile
WITH
    NOFORMAT,               -- Preserva cabeceras existentes del medio; no formatea
    NOINIT,                 -- Previene sobreescritura accidental; no trunca el archivo
    CHECKSUM,               -- Valida checksums de página durante la lectura y escritura
    STATS = 10,             -- Reporta progreso cada 10%
    NAME = @BackupName,
    DESCRIPTION = N'Copia de seguridad completa segura y parametrizada de SistemaGimnasio';

-- 5. Verificar inmediatamente la integridad del archivo generado
PRINT N'Verificando integridad física del archivo de respaldo recién creado...';
RESTORE VERIFYONLY
FROM DISK = @BackupFile
WITH CHECKSUM;

IF @@ERROR = 0
BEGIN
    PRINT N'========================================================================';
    PRINT N'[ÉXITO] Respaldo completado y verificado correctamente.';
    PRINT N'Archivo: ' + @BackupFile;
    PRINT N'========================================================================';
END
ELSE
BEGIN
    RAISERROR(N'ADVERTENCIA: El respaldo concluyó con posibles inconsistencias en la verificación.', 16, 1);
END;
GO
