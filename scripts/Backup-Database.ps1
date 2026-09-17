<#
.SYNOPSIS
    Script seguro de PowerShell para automatización de respaldos y gestión de retención.
.DESCRIPTION
    Ejecuta el respaldo de la base de datos de Sistema Gimnasio utilizando sqlcmd y el script
    scripts/backup-database.sql, aplicando una política de retención para depurar archivos antiguos.
.PARAMETER ServerInstance
    Instancia de SQL Server (por defecto '.\SQLEXPRESS01').
.PARAMETER DatabaseName
    Nombre de la base de datos (por defecto 'SistemaGimnasio').
.PARAMETER BackupDirectory
    Directorio obligatorio donde se almacenarán los archivos .bak.
.PARAMETER RetentionDays
    Número de días de retención para archivos de respaldo (por defecto 14 días).
.PARAMETER WhatIf
    Modo simulado; describe las acciones sin modificar el sistema de archivos ni la base de datos.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/Backup-Database.ps1 -BackupDirectory "D:\Backups\Gym" -WhatIf
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/Backup-Database.ps1 -BackupDirectory "D:\Backups\Gym" -RetentionDays 30
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$ServerInstance = ".\SQLEXPRESS01",
    [string]$DatabaseName = "SistemaGimnasio",
    [string]$BackupDirectory = "",
    [int]$RetentionDays = 14
)

$ErrorActionPreference = "Stop"

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host " Gym System - Operación Segura de Respaldo y Retención" -ForegroundColor Cyan
Write-Host " Instancia:  $ServerInstance" -ForegroundColor Cyan
Write-Host " Base Datos: $DatabaseName" -ForegroundColor Cyan
Write-Host " Retención:  $RetentionDays días" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

# 1. Validar parámetro obligatorio de directorio de destino (falla cerrado)
if ([string]::IsNullOrWhiteSpace($BackupDirectory)) {
    throw "Debe especificar un directorio de destino para el respaldo mediante el parámetro -BackupDirectory."
}

# Validar sintaxis y formato de la ruta de destino sin alterar el sistema de archivos
try {
    $null = [System.IO.Path]::GetFullPath($BackupDirectory)
} catch {
    throw "La ruta especificada en -BackupDirectory ('$BackupDirectory') no tiene un formato válido: $($_.Exception.Message)"
}

# 2. Resolver ruta del script SQL de respaldo
$scriptDir = $PSScriptRoot
if (-not $scriptDir) { $scriptDir = (Get-Location).Path }
$sqlScriptPath = Join-Path $scriptDir "backup-database.sql"

if (-not (Test-Path -LiteralPath $sqlScriptPath)) {
    throw "No se encontró el script SQL base en '$sqlScriptPath'."
}

# 3. Localizar sqlcmd en el sistema
$sqlcmdExe = Get-Command "sqlcmd.exe" -ErrorAction SilentlyContinue
if (-not $sqlcmdExe) {
    # Búsqueda en rutas estándar de SQL Server
    $standardPaths = @(
        "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\130\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles(x86)}\Microsoft SQL Server\Client SDK\ODBC\130\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles}\Microsoft SQL Server\110\Tools\Binn\sqlcmd.exe"
    )
    foreach ($p in $standardPaths) {
        if (Test-Path -LiteralPath $p) {
            $sqlcmdExe = $p
            break
        }
    }
}

if (-not $sqlcmdExe) {
    Write-Warning "No se encontró 'sqlcmd.exe' en el PATH. El script mostrará el comando equivalente a programar."
}

# 4. Preparar argumentos para sqlcmd (siempre con BackupFolder explícito)
$sqlcmdArgs = @("-S", $ServerInstance, "-E", "-i", $sqlScriptPath, "-v", "BackupFolder=`"$BackupDirectory`"")

Write-Host "`n[Paso 1] Comando de respaldo preparado:" -ForegroundColor Yellow
$cmdString = "sqlcmd " + ($sqlcmdArgs -join " ")
Write-Host "  $cmdString" -ForegroundColor White

# 5. Creación del directorio protegida bajo ShouldProcess (WhatIf no crea carpetas ni altera FS)
if (-not (Test-Path -LiteralPath $BackupDirectory)) {
    if ($PSCmdlet.ShouldProcess($BackupDirectory, "Crear directorio de respaldo")) {
        New-Item -ItemType Directory -Path $BackupDirectory -Force | Out-Null
        Write-Host "  Directorio de respaldo creado: $BackupDirectory" -ForegroundColor DarkGray
    }
}

# 6. Invocación de sqlcmd estrictamente condicionada a ShouldProcess
if ($PSCmdlet.ShouldProcess($DatabaseName, "Ejecutar respaldo completo con sqlcmd hacia '$BackupDirectory'")) {
    if ($sqlcmdExe) {
        Write-Host "`nEjecutando respaldo..." -ForegroundColor Green
        & $sqlcmdExe $sqlcmdArgs
        if ($LASTEXITCODE -ne 0) {
            throw "sqlcmd finalizó con código de error $LASTEXITCODE."
        }
    } else {
        Write-Host "Instrucción manual: Ejecute el comando anterior en su terminal con permisos de administrador." -ForegroundColor Yellow
    }
} else {
    Write-Host "[WhatIf] Operación de respaldo simulada con éxito (sin cambios en FS ni DB)." -ForegroundColor Gray
}

# 7. Política de retención: depuración de archivos .bak antiguos bajo ShouldProcess
if (Test-Path -LiteralPath $BackupDirectory) {
    Write-Host "`n[Paso 2] Evaluando política de retención en '$BackupDirectory'..." -ForegroundColor Yellow
    $cutoffDate = (Get-Date).AddDays(-$RetentionDays)
    $oldBackups = Get-ChildItem -LiteralPath $BackupDirectory -Filter "SistemaGimnasio_Full_*.bak" -File |
        Where-Object { $_.LastWriteTime -lt $cutoffDate }

    if ($oldBackups.Count -gt 0) {
        foreach ($oldFile in $oldBackups) {
            if ($PSCmdlet.ShouldProcess($oldFile.FullName, "Eliminar respaldo obsoleto (antigüedad > $RetentionDays días)")) {
                Remove-Item -LiteralPath $oldFile.FullName -Force
                Write-Host "  [ELIMINADO] $($oldFile.Name) (Fecha: $($oldFile.LastWriteTime))" -ForegroundColor DarkGray
            }
        }
    } else {
        Write-Host "  No se encontraron archivos de respaldo que superen los $RetentionDays días de antigüedad." -ForegroundColor White
    }
} else {
    Write-Host "`n[Paso 2] El directorio de respaldo '$BackupDirectory' no existe físicamente en disco; se omite evaluación de retención." -ForegroundColor Gray
}

Write-Host "`nOperación de gestión de respaldo completada." -ForegroundColor Cyan
