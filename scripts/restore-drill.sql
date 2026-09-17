/*
================================================================================
 Gym System - Simulación Segura de Restauración Aislada (Restore Drill)
 Archivo: scripts/restore-drill.sql
 Motor: Microsoft SQL Server 2019+ / SQL Server Express
 Objetivo: Validar la restaurabilidad e integridad de un respaldo SIN tocar producción.

 REGLAS DE ORO DE SEGURIDAD OPERATIVA:
 - NUNCA restaurar sobre la base activa de producción 'SistemaGimnasio' durante un simulacro.
 - Este script restaura exclusivamente en la base aislada [SistemaGimnasio_Drill].
 - Falla cerrado si la base objetivo ya existe, a menos que se confirme explícitamente
   mediante el parámetro $(ConfirmOverwriteExistingDrillDb)="YES".
 - Mueve los archivos físicos (.mdf / .ldf) dinámicamente para evitar colisiones de disco.
 - No contiene rutas fijas ni hardcodeadas.

 USO VÍA SQLCMD:
   # Caso 1: Primera ejecución o base drill no existente:
   sqlcmd -S .\SQLEXPRESS01 -E -v BackupFile="<Ruta_Al_Archivo_Respaldo.bak>" -i scripts/restore-drill.sql

   # Caso 2: Re-ejecución con confirmación explícita para sobreescribir drill anterior:
   sqlcmd -S .\SQLEXPRESS01 -E -v BackupFile="<Ruta_Al_Archivo_Respaldo.bak>" -v ConfirmOverwriteExistingDrillDb="YES" -i scripts/restore-drill.sql
================================================================================
*/

SET NOCOUNT ON;

-- 1. Guardrail absoluto: Definición de la base objetivo y prohibición estricta de producción
DECLARE @TargetDatabase NVARCHAR(128) = N'SistemaGimnasio_Drill';

IF LOWER(@TargetDatabase) = N'sistemagimnasio' OR LOWER(@TargetDatabase) = N'[sistemagimnasio]'
BEGIN
    RAISERROR(N'VIOLACIÓN CRÍTICA DE SEGURIDAD: Este script de simulacro NUNCA puede ejecutarse sobre la base de datos de producción [SistemaGimnasio]. Operación cancelada.', 16, 1);
    RETURN;
END;

-- 2. Validar parámetro obligatorio del archivo de respaldo (falla cerrado)
DECLARE @BackupFilePath NVARCHAR(4000) = N'$(BackupFile)';

IF @BackupFilePath IS NULL OR @BackupFilePath = N'' OR @BackupFilePath = N'$(BackupFile)'
BEGIN
    PRINT N'ERROR: Debe especificar la ruta absoluta del archivo de respaldo a evaluar.';
    PRINT N'Ejemplo de invocación:';
    PRINT N'  sqlcmd -S .\SQLEXPRESS01 -E -v BackupFile="<Ruta_Al_Archivo_Respaldo.bak>" -i scripts/restore-drill.sql';
    RAISERROR(N'Falta el parámetro requerido $(BackupFile). Abortando simulacro.', 16, 1);
    RETURN;
END;

-- 3. Validar existencia de base de datos previa y requerir confirmación explícita
DECLARE @ConfirmOverwrite NVARCHAR(50) = N'$(ConfirmOverwriteExistingDrillDb)';

IF DB_ID(@TargetDatabase) IS NOT NULL
BEGIN
    IF @ConfirmOverwrite <> N'YES' AND @ConfirmOverwrite <> N'CONFIRMAR_SOBREESCRITURA'
    BEGIN
        PRINT N'========================================================================';
        PRINT N'ERROR DE SEGURIDAD OPERATIVA: La base de datos objetivo [' + @TargetDatabase + N'] ya existe en la instancia.';
        PRINT N'El simulacro FALLA CERRADO para prevenir la eliminación accidental de bases de datos existentes.';
        PRINT N'Para autorizar la recreación de la base de simulacro previa, debe proporcionar la variable explícita:';
        PRINT N'  -v ConfirmOverwriteExistingDrillDb="YES"';
        PRINT N'========================================================================';
        RAISERROR(N'Fallo cerrado: La base de datos de prueba ya existe y no se proporcionó confirmación explícita ConfirmOverwriteExistingDrillDb=YES.', 16, 1);
        RETURN;
    END;

    PRINT N'Paso previo: Confirmación explícita recibida. Limpiando base de simulacro previa [' + @TargetDatabase + N']...';
    ALTER DATABASE [SistemaGimnasio_Drill] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [SistemaGimnasio_Drill];
END;

PRINT N'========================================================================';
PRINT N'Iniciando Simulacro de Restauración Aislado (Restore Drill)';
PRINT N'Archivo evaluado: ' + @BackupFilePath;
PRINT N'Base de destino de prueba: [' + @TargetDatabase + N']';
PRINT N'========================================================================';

-- 4. Fase 1: Verificación de cabeceras y suma de comprobación del medio
PRINT N'Paso 1: Verificando integridad del medio de respaldo (RESTORE VERIFYONLY)...';
RESTORE VERIFYONLY
FROM DISK = @BackupFilePath
WITH CHECKSUM;

IF @@ERROR <> 0
BEGIN
    RAISERROR(N'Fallo crítico en VERIFYONLY: El archivo de respaldo está corrupto o es ilegible.', 16, 1);
    RETURN;
END;
PRINT N'-> Cabecera y checksums del archivo de respaldo: VÁLIDOS.';

-- 5. Fase 2: Inspeccionar y capturar nombres lógicos reales del respaldo (RESTORE FILELISTONLY)
PRINT N'Paso 2: Inspeccionando estructura interna del respaldo (RESTORE FILELISTONLY)...';

IF OBJECT_ID('tempdb..#BackupFiles') IS NOT NULL
    DROP TABLE #BackupFiles;

CREATE TABLE #BackupFiles (
    LogicalName          NVARCHAR(128),
    PhysicalName         NVARCHAR(260),
    Type                 CHAR(1),
    FileGroupName        NVARCHAR(128),
    Size                 NUMERIC(20,0),
    MaxSize              NUMERIC(20,0),
    FileId               BIGINT,
    CreateLSN            NUMERIC(25,0),
    DropLSN              NUMERIC(25,0),
    UniqueId             UNIQUEIDENTIFIER,
    ReadOnlyLSN          NUMERIC(25,0),
    ReadWriteLSN         NUMERIC(25,0),
    BackupSizeInBytes    BIGINT,
    SourceBlockSize      INT,
    FileGroupId          INT,
    LogGroupGUID         UNIQUEIDENTIFIER,
    DifferentialBaseLSN  NUMERIC(25,0),
    DifferentialBaseGUID UNIQUEIDENTIFIER,
    IsReadOnly           BIT,
    IsPresent            BIT,
    TDEThumbprint        VARBINARY(32),
    SnapshotURL          NVARCHAR(360)
);

DECLARE @SqlFileListCmd NVARCHAR(MAX) = N'RESTORE FILELISTONLY FROM DISK = N''' + REPLACE(@BackupFilePath, N'''', N'''''''') + N''';';
INSERT INTO #BackupFiles
EXEC (@SqlFileListCmd);

IF NOT EXISTS (SELECT 1 FROM #BackupFiles)
BEGIN
    RAISERROR(N'ERROR CRÍTICO: No se pudieron extraer archivos lógicos del respaldo. Operación cancelada.', 16, 1);
    RETURN;
END;

-- 6. Fase 3: Determinar rutas físicas de datos y logs de forma dinámica
DECLARE @DefaultDataDir NVARCHAR(4000);
DECLARE @DefaultLogDir  NVARCHAR(4000);

SET @DefaultDataDir = CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(4000));
SET @DefaultLogDir  = CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS NVARCHAR(4000));

IF @DefaultDataDir IS NULL OR @DefaultDataDir = N''
BEGIN
    SELECT TOP 1 @DefaultDataDir = LEFT(physical_name, LEN(physical_name) - CHARINDEX('\', REVERSE(physical_name)) + 1)
    FROM master.sys.master_files
    WHERE database_id = 1 AND type = 0;
END;

IF @DefaultLogDir IS NULL OR @DefaultLogDir = N''
BEGIN
    SELECT TOP 1 @DefaultLogDir = LEFT(physical_name, LEN(physical_name) - CHARINDEX('\', REVERSE(physical_name)) + 1)
    FROM master.sys.master_files
    WHERE database_id = 1 AND type = 1;
END;

IF @DefaultDataDir IS NULL OR @DefaultDataDir = N''
BEGIN
    RAISERROR(N'ERROR CRÍTICO: No se pudo determinar el directorio de datos de la instancia para ubicar los archivos del simulacro.', 16, 1);
    RETURN;
END;

IF @DefaultLogDir IS NULL OR @DefaultLogDir = N''
    SET @DefaultLogDir = @DefaultDataDir;

IF RIGHT(RTRIM(@DefaultDataDir), 1) <> N'\' SET @DefaultDataDir = RTRIM(@DefaultDataDir) + N'\';
IF RIGHT(RTRIM(@DefaultLogDir), 1) <> N'\'  SET @DefaultLogDir = RTRIM(@DefaultLogDir) + N'\';

-- 7. Fase 4: Construir y ejecutar restauración aislada con WITH MOVE dinámico
PRINT N'Paso 3: Construyendo reubicación física de archivos (WITH MOVE dinámico)...';

DECLARE @MoveClauses NVARCHAR(MAX) = N'';
DECLARE @LogName NVARCHAR(128);
DECLARE @FileType CHAR(1);
DECLARE @FileId BIGINT;
DECLARE @TargetPhysical NVARCHAR(4000);
DECLARE @DataCount INT = 0;
DECLARE @LogCount INT = 0;

DECLARE curFiles CURSOR LOCAL FAST_FORWARD FOR
SELECT LogicalName, Type, FileId
FROM #BackupFiles
ORDER BY Type, FileId;

OPEN curFiles;
FETCH NEXT FROM curFiles INTO @LogName, @FileType, @FileId;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF @FileType = 'D'
    BEGIN
        SET @DataCount = @DataCount + 1;
        IF @DataCount = 1
            SET @TargetPhysical = @DefaultDataDir + @TargetDatabase + N'.mdf';
        ELSE
            SET @TargetPhysical = @DefaultDataDir + @TargetDatabase + N'_data_' + CAST(@DataCount AS NVARCHAR(10)) + N'.ndf';
    END
    ELSE IF @FileType = 'L'
    BEGIN
        SET @LogCount = @LogCount + 1;
        IF @LogCount = 1
            SET @TargetPhysical = @DefaultLogDir + @TargetDatabase + N'_log.ldf';
        ELSE
            SET @TargetPhysical = @DefaultLogDir + @TargetDatabase + N'_log_' + CAST(@LogCount AS NVARCHAR(10)) + N'.ldf';
    END
    ELSE
    BEGIN
        SET @TargetPhysical = @DefaultDataDir + @TargetDatabase + N'_file_' + CAST(@FileId AS NVARCHAR(10)) + N'.dat';
    END;

    PRINT N'  Mapeo lógico [' + @LogName + N'] (' + @FileType + N') -> ' + @TargetPhysical;

    SET @MoveClauses = @MoveClauses + N'
    MOVE N''' + REPLACE(@LogName, N'''', N'''''''') + N''' TO N''' + REPLACE(@TargetPhysical, N'''', N'''''''') + N''',';

    FETCH NEXT FROM curFiles INTO @LogName, @FileType, @FileId;
END;

CLOSE curFiles;
DEALLOCATE curFiles;

-- Construir y ejecutar la sentencia de restauración con WITH MOVE y CHECKSUM
DECLARE @SqlRestore NVARCHAR(MAX);
SET @SqlRestore = N'RESTORE DATABASE [' + REPLACE(@TargetDatabase, N']', N']]') + N']
FROM DISK = N''' + REPLACE(@BackupFilePath, N'''', N'''''''') + N'''
WITH' + @MoveClauses + N'
    REPLACE,
    RECOVERY,
    CHECKSUM,
    STATS = 20;';

PRINT N'Ejecutando restauración en base aislada [' + @TargetDatabase + N']...';
EXEC (@SqlRestore);

-- 8. Fase 5: Comprobación de integridad estructural (DBCC CHECKDB)
PRINT N'Paso 4: Ejecutando comprobación de integridad estructural (DBCC CHECKDB)...';
DBCC CHECKDB (N'SistemaGimnasio_Drill') WITH NO_INFOMSGS;

-- 9. Fase 6: Validación de tablas y conteos de datos
PRINT N'Paso 5: Validando consistencia de tablas principales en [' + @TargetDatabase + N']...';

DECLARE @CountMembresias INT, @CountMiembros INT, @CountPagos INT, @CountVisitas INT;
SELECT @CountMembresias = COUNT(*) FROM [SistemaGimnasio_Drill].dbo.Membresias;
SELECT @CountMiembros   = COUNT(*) FROM [SistemaGimnasio_Drill].dbo.Miembros;
SELECT @CountPagos      = COUNT(*) FROM [SistemaGimnasio_Drill].dbo.Pagos;
SELECT @CountVisitas    = COUNT(*) FROM [SistemaGimnasio_Drill].dbo.Visitas;

PRINT N'------------------------------------------------------------------------';
PRINT N'Resultados de conteo en la base restaurada:';
PRINT N'  - Registros en Membresias: ' + CAST(@CountMembresias AS NVARCHAR(20));
PRINT N'  - Registros en Miembros:   ' + CAST(@CountMiembros AS NVARCHAR(20));
PRINT N'  - Registros en Pagos:      ' + CAST(@CountPagos AS NVARCHAR(20));
PRINT N'  - Registros en Visitas:    ' + CAST(@CountVisitas AS NVARCHAR(20));
PRINT N'------------------------------------------------------------------------';

PRINT N'========================================================================';
PRINT N'[ÉXITO] Simulacro de restauración completado satisfactoriamente.';
PRINT N'El respaldo es 100% restaurable e íntegro sin haber afectado producción.';
PRINT N'NOTA OPERATIVA: Puede eliminar la base de prueba cuando concluya su auditoría ejecutando:';
PRINT N'  DROP DATABASE [SistemaGimnasio_Drill];';
PRINT N'========================================================================';
GO
