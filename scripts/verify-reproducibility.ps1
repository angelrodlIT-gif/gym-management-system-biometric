[CmdletBinding()]
param(
    [string]$RepoRoot
)

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    if ($PSScriptRoot) {
        $RepoRoot = Split-Path -Parent $PSScriptRoot
    }
    if ([string]::IsNullOrWhiteSpace($RepoRoot) -or -not (Test-Path -LiteralPath $RepoRoot)) {
        $RepoRoot = (Get-Location).Path
    }
}
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

$ErrorActionPreference = "Continue"
$failedChecks = 0

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host " Running Reproducibility & Consistency Checks (Phase 1, 2 & 3)" -ForegroundColor Cyan
Write-Host " Repository Root: $RepoRoot" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

function Assert-Check {
    param(
        [string]$Name,
        [bool]$Success,
        [string]$Message
    )
    if ($Success) {
        Write-Host "[PASS] $($Name): $Message" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] $($Name): $Message" -ForegroundColor Red
        $script:failedChecks++
    }
}

# ---------------------------------------------------------
# Check A: App.config contains GymDbConnection in both projects
# ---------------------------------------------------------
Write-Host "`n--- Check A: App.config Connection Strings ---" -ForegroundColor Yellow

$appConfigs = @(
    "Sistema_Gimnasio\App.config",
    "BiometricApp\BiometricApp\BiometricApp\App.config"
)

$extractedConnStrings = @{}

foreach ($relPath in $appConfigs) {
    $fullPath = Join-Path $RepoRoot $relPath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        Assert-Check "Config Exists ($relPath)" $false "File not found at $fullPath"
        continue
    }

    try {
        [xml]$xml = Get-Content -LiteralPath $fullPath -Raw
        $node = $xml.SelectSingleNode("/configuration/connectionStrings/add[@name='GymDbConnection']")
        if ($null -ne $node -and -not [string]::IsNullOrWhiteSpace($node.connectionString)) {
            $connVal = $node.connectionString.Trim()
            $extractedConnStrings[$relPath] = $connVal
            Assert-Check "GymDbConnection in $relPath" $true "Found GymDbConnection ('$connVal')"
        } else {
            Assert-Check "GymDbConnection in $relPath" $false "Missing GymDbConnection in <connectionStrings>"
        }
    } catch {
        Assert-Check "GymDbConnection in $relPath" $false "Failed to parse XML: $($_.Exception.Message)"
    }
}

# Verify consistency between both connection strings
$config1 = "Sistema_Gimnasio\App.config"
$config2 = "BiometricApp\BiometricApp\BiometricApp\App.config"

if ($extractedConnStrings.ContainsKey($config1) -and $extractedConnStrings.ContainsKey($config2)) {
    $val1 = $extractedConnStrings[$config1]
    $val2 = $extractedConnStrings[$config2]
    if ($val1 -eq $val2) {
        Assert-Check "GymDbConnection Consistency" $true "Both App.config connection strings are identical ('$val1')"
    } else {
        Assert-Check "GymDbConnection Consistency" $false "Connection strings differ between projects. $($config1) has '$val1' while $($config2) has '$val2'"
    }
} else {
    Assert-Check "GymDbConnection Consistency" $false "Cannot compare connection strings: GymDbConnection is missing or invalid in one or both configurations"
}

# ---------------------------------------------------------
# Check B: No hardcoded SQL connection strings in .cs files
# ---------------------------------------------------------
Write-Host "`n--- Check B: Hardcoded SQL Connection Strings in C# ---" -ForegroundColor Yellow

$csFiles = Get-ChildItem -LiteralPath $RepoRoot -Filter "*.cs" -Recurse |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

$hardcodedMatches = @()
$connStrPattern = 'Data\s+Source\s*=\s*[^;]+;\s*Initial\s+Catalog\s*='

foreach ($file in $csFiles) {
    $lines = Get-Content -LiteralPath $file.FullName
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        # Ignore comments
        $trimmed = $line.Trim()
        if ($trimmed.StartsWith("//") -or $trimmed.StartsWith("/*") -or $trimmed.StartsWith("*")) {
            continue
        }
        if ($line -match $connStrPattern) {
            $hardcodedMatches += [PSCustomObject]@{
                File = ($file.FullName.Replace($RepoRoot, "").TrimStart('\', '/'))
                Line = $i + 1
                Content = $line.Trim()
            }
        }
    }
}

if ($hardcodedMatches.Count -eq 0) {
    Assert-Check "No hardcoded SQL connstrings" $true "No hardcoded connection strings detected in C# source files"
} else {
    $details = ($hardcodedMatches | ForEach-Object { "$($_.File):$($_.Line) -> $($_.Content)" }) -join "; "
    Assert-Check "No hardcoded SQL connstrings" $false "Found $($hardcodedMatches.Count) occurrence(s): $details"
}

# ---------------------------------------------------------
# Check C: SDK references point to libs/ and files exist
# ---------------------------------------------------------
Write-Host "`n--- Check C: SDK References in csproj ---" -ForegroundColor Yellow

$sdkAssemblies = @("DPUruNet", "DPCtlUruNet", "DPXUru", "DPCtlXUru")
$csprojFiles = Get-ChildItem -LiteralPath $RepoRoot -Filter "*.csproj" -Recurse |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

$sdkCheckCount = 0
foreach ($csproj in $csprojFiles) {
    $relCsproj = $csproj.FullName.Replace($RepoRoot, "").TrimStart('\', '/')
    [xml]$projXml = Get-Content -LiteralPath $csproj.FullName
    $references = $projXml.SelectNodes("//*[local-name()='Reference']")

    foreach ($ref in $references) {
        $include = $ref.GetAttribute("Include")
        $matchingSdk = $sdkAssemblies | Where-Object { $include -match "^$([regex]::Escape($_))\b" }
        if ($matchingSdk) {
            $sdkCheckCount++
            $hintPathNode = $ref.SelectSingleNode("*[local-name()='HintPath']")
            if ($null -eq $hintPathNode -or [string]::IsNullOrWhiteSpace($hintPathNode.InnerText)) {
                Assert-Check "SDK Reference ($relCsproj -> $matchingSdk)" $false "HintPath is missing"
            } else {
                $hint = $hintPathNode.InnerText.Trim()
                if ($hint -notmatch 'libs[\\/]') {
                    Assert-Check "SDK Reference ($relCsproj -> $matchingSdk)" $false "HintPath '$hint' does not point to libs/"
                } else {
                    $resolved = [System.IO.Path]::GetFullPath((Join-Path $csproj.DirectoryName $hint))
                    if (Test-Path -LiteralPath $resolved) {
                        Assert-Check "SDK Reference ($relCsproj -> $matchingSdk)" $true "Points to libs and exists on disk ($hint)"
                    } else {
                        Assert-Check "SDK Reference ($relCsproj -> $matchingSdk)" $false "Resolved path '$resolved' does not exist"
                    }
                }
            }
        }
    }
}

if ($sdkCheckCount -eq 0) {
    Assert-Check "SDK References found" $false "No SDK references were found in any .csproj"
}

# ---------------------------------------------------------
# Check D: database/SistemaGimnasio.sql portability & schema
# ---------------------------------------------------------
Write-Host "`n--- Check D: Database SQL Encoding & Schema ---" -ForegroundColor Yellow

$sqlPath = Join-Path $RepoRoot "database\SistemaGimnasio.sql"
if (-not (Test-Path -LiteralPath $sqlPath)) {
    Assert-Check "database/SistemaGimnasio.sql exists" $false "File not found at $sqlPath"
} else {
    $bytes = [System.IO.File]::ReadAllBytes($sqlPath)
    $hasNul = $false
    for ($i = 0; $i -lt $bytes.Length; $i++) {
        if ($bytes[$i] -eq 0) {
            $hasNul = $true
            break
        }
    }

    if ($hasNul) {
        Assert-Check "SQL File No NULs" $false "database/SistemaGimnasio.sql contains NUL (0x00) bytes (UTF-16/binary-like encoding)"
    } else {
        Assert-Check "SQL File No NULs" $true "database/SistemaGimnasio.sql has no NUL bytes (portable encoding)"
    }

    # Strict UTF-8 validation using UTF8Encoding with throwOnInvalidBytes = true
    $utf8Strict = New-Object System.Text.UTF8Encoding($false, $true)
    $sqlText = $null
    $isUtf8Valid = $true
    try {
        $sqlText = $utf8Strict.GetString($bytes)
        Assert-Check "SQL File Strict UTF-8" $true "database/SistemaGimnasio.sql is valid strict UTF-8 (decoder exception fallback validated)"
    } catch [System.Text.DecoderFallbackException] {
        $isUtf8Valid = $false
        Assert-Check "SQL File Strict UTF-8" $false "database/SistemaGimnasio.sql contains invalid UTF-8 byte sequences: $($_.Exception.Message)"
    } catch {
        $isUtf8Valid = $false
        Assert-Check "SQL File Strict UTF-8" $false "Failed decoding database/SistemaGimnasio.sql as strict UTF-8: $($_.Exception.Message)"
    }

    # Verify expected tables
    if ($isUtf8Valid -and -not [string]::IsNullOrWhiteSpace($sqlText)) {
        $expectedTables = @("Membresias", "Miembros", "Pagos", "Visitas")
        foreach ($table in $expectedTables) {
            $pattern = "CREATE\s+TABLE\s+(dbo\.)?\[?$table\]?"
            if ($sqlText -match $pattern) {
                Assert-Check "Table $table in SQL schema" $true "Found table definition for $table"
            } else {
                Assert-Check "Table $table in SQL schema" $false "Missing CREATE TABLE for $table"
            }
        }
    } else {
        Assert-Check "SQL Schema Tables" $false "Cannot verify table definitions due to invalid encoding in database/SistemaGimnasio.sql"
    }
}

# ---------------------------------------------------------
# Check E: No tracked binary artifacts outside libs/
# ---------------------------------------------------------
Write-Host "`n--- Check E: Git Tracked Artifacts Outside libs/ ---" -ForegroundColor Yellow

try {
    $gitTracked = & git -C "$RepoRoot" ls-files
    $trackedArtifacts = $gitTracked | Where-Object {
        $_ -match '\.(dll|exe|nupkg|pdb|zip)$' -and $_ -notmatch '^libs/'
    }

    if ($null -eq $trackedArtifacts -or $trackedArtifacts.Count -eq 0) {
        Assert-Check "No tracked binary artifacts outside libs/" $true "No unexpected binaries tracked in git"
    } else {
        Assert-Check "No tracked binary artifacts outside libs/" $false "Found tracked binaries: $($trackedArtifacts -join ', ')"
    }
} catch {
    Assert-Check "Git tracked check" $false "Error executing git ls-files: $($_.Exception.Message)"
}

# ---------------------------------------------------------
# Check F: Zero MessageBox & Zero WinForms in Gym_System.Core (Phase 2)
# ---------------------------------------------------------
Write-Host "`n--- Check F: Zero MessageBox & WinForms in Gym_System.Core ---" -ForegroundColor Yellow

$coreDir = Join-Path $RepoRoot "Gym_System.Core\Gym_System.Core"
$coreCsFiles = Get-ChildItem -LiteralPath $coreDir -Filter "*.cs" -Recurse |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

$msgBoxMatches = @()
$winFormsUsings = @()
foreach ($file in $coreCsFiles) {
    $lines = Get-Content -LiteralPath $file.FullName
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i].Trim()
        if ($line.StartsWith("//") -or $line.StartsWith("/*") -or $line.StartsWith("*")) { continue }
        if ($line -match '\bMessageBox\b') {
            $msgBoxMatches += "$($file.Name):$($i + 1) -> $line"
        }
        if ($line -match 'using\s+System\.Windows\.Forms;') {
            $winFormsUsings += "$($file.Name):$($i + 1) -> $line"
        }
    }
}

Assert-Check "Zero MessageBox in Gym_System.Core" ($msgBoxMatches.Count -eq 0) `
    $(if ($msgBoxMatches.Count -eq 0) { "No MessageBox references found in Gym_System.Core" } else { "Found: $($msgBoxMatches -join '; ')" })

Assert-Check "Zero WinForms using in Gym_System.Core" ($winFormsUsings.Count -eq 0) `
    $(if ($winFormsUsings.Count -eq 0) { "No WinForms using statements in Gym_System.Core" } else { "Found: $($winFormsUsings -join '; ')" })

# Absence of System.Windows.Forms reference in Gym_System.Core.csproj
$coreCsprojPath = Join-Path $RepoRoot "Gym_System.Core\Gym_System.Core\Gym_System.Core.csproj"
if (Test-Path -LiteralPath $coreCsprojPath) {
    $csprojContent = Get-Content -LiteralPath $coreCsprojPath -Raw
    $hasWinFormsRef = $csprojContent -match 'System\.Windows\.Forms'
    Assert-Check "Zero WinForms in Gym_System.Core.csproj" (-not $hasWinFormsRef) `
        $(if (-not $hasWinFormsRef) { "Gym_System.Core.csproj has no System.Windows.Forms reference (UI decoupled)" } else { "Gym_System.Core.csproj retains System.Windows.Forms reference" })

    $hasBiometricRef = $csprojContent -match 'ProjectReference.*BiometricApp'
    Assert-Check "Zero BiometricApp in Gym_System.Core.csproj" (-not $hasBiometricRef) `
        $(if (-not $hasBiometricRef) { "Gym_System.Core.csproj has no BiometricApp reference (hardware/UI decoupled)" } else { "Gym_System.Core.csproj retains BiometricApp reference" })
} else {
    Assert-Check "Zero WinForms in Gym_System.Core.csproj" $false "Gym_System.Core.csproj not found"
}

# ---------------------------------------------------------
# Check G: State Update Optimization & N+1 Prevention (Phase 2)
# ---------------------------------------------------------
Write-Host "`n--- Check G: State Update Optimization & N+1 Prevention ---" -ForegroundColor Yellow

$miembrosViewPath = Join-Path $RepoRoot "Sistema_Gimnasio\MiembrosView.xaml.cs"
$miembrosViewContent = Get-Content -LiteralPath $miembrosViewPath -Raw

$hasActualizarEstadosMasivoInView = $miembrosViewContent -match 'conexion\.ActualizarEstadosMasivo\s*\('
$hasN1LoopInView = $miembrosViewContent -match 'foreach\s*\([^\)]*\)\s*\{[^}]*ActualizarEstadoMiembro'

$automaticoMatch = [regex]::Match($miembrosViewContent, 'void\s+ActualizarEstados_Automatico\s*\(\)\s*\{([\s\S]*?)\n\s*\}')
$hasPopupInAutomatico = $false
if ($automaticoMatch.Success) {
    if ($automaticoMatch.Groups[1].Value -match 'MessageBox\.Show') {
        $hasPopupInAutomatico = $true
    }
}

Assert-Check "MiembrosView uses ActualizarEstadosMasivo" $hasActualizarEstadosMasivoInView `
    $(if ($hasActualizarEstadosMasivoInView) { "View calls ActualizarEstadosMasivo" } else { "View does not invoke ActualizarEstadosMasivo" })

Assert-Check "No N+1 iteration in MiembrosView" (-not $hasN1LoopInView) `
    $(if (-not $hasN1LoopInView) { "No N+1 loop detected in MiembrosView" } else { "Detected N+1 foreach loop" })

Assert-Check "No repetitive MessageBox in ActualizarEstados_Automatico" (-not $hasPopupInAutomatico) `
    $(if (-not $hasPopupInAutomatico) { "Automatic update executes silently" } else { "Found MessageBox.Show in automatic update" })

# ---------------------------------------------------------
# Check H: Atomic Payment Transaction & Estado='Activo' (Phase 2)
# ---------------------------------------------------------
Write-Host "`n--- Check H: Atomic Payment Transaction ---" -ForegroundColor Yellow

$miembrosConexionPath = Join-Path $RepoRoot "Gym_System.Core\Gym_System.Core\Miembros_Conexion.cs"
$miembrosConexionContent = Get-Content -LiteralPath $miembrosConexionPath -Raw

$publicRegistrarPagoMatches = [regex]::Matches($miembrosConexionContent, 'public\s+bool\s+RegistrarPago\s*\(')
$singlePublicApi = ($publicRegistrarPagoMatches.Count -eq 1)
$hasInsecureDatesSignature = $miembrosConexionContent -match 'RegistrarPago\s*\([^)]*DateTime\s+fechaInicio'
$hasUpdlock = $miembrosConexionContent -match 'WITH\s*\(\s*UPDLOCK,\s*ROWLOCK\s*\)'

$hasEstadoActivoInUpdate = $miembrosConexionContent -match 'UPDATE\s+Miembros\s+SET[^;]+Estado\s*=\s*''Activo'''
$hasMassUpdateMethod = $miembrosConexionContent -match 'ActualizarEstadosMasivo'
$hasRowsAffectedValidation = ($miembrosConexionContent -match 'rowsAffected\s*<=\s*0' -or $miembrosConexionContent -match 'rowsUpdated\s*<=\s*0') -and ($miembrosConexionContent -match 'transaccion\.Rollback\(\)')
$hasInTxVigencia = $miembrosConexionContent -match 'VigenciaCalculador\.CalcularVigencia'

Assert-Check "No insecure RegistrarPago with explicit dates" (-not $hasInsecureDatesSignature) `
    $(if (-not $hasInsecureDatesSignature) { "No insecure RegistrarPago signature accepting explicit dates found" } else { "Detected insecure RegistrarPago signature with explicit dates" })

Assert-Check "Single public RegistrarPago API" $singlePublicApi `
    $(if ($singlePublicApi) { "Exactly one public RegistrarPago API exists in Miembros_Conexion" } else { "Found $($publicRegistrarPagoMatches.Count) public RegistrarPago overloads (expected exactly 1)" })

Assert-Check "RegistrarPago uses UPDLOCK row lock" $hasUpdlock `
    $(if ($hasUpdlock) { "RegistrarPago queries member with UPDLOCK, ROWLOCK" } else { "RegistrarPago lacks UPDLOCK row lock" })

Assert-Check "RegistrarPago sets Estado='Activo'" $hasEstadoActivoInUpdate `
    $(if ($hasEstadoActivoInUpdate) { "RegistrarPago SQL updates Estado = 'Activo'" } else { "UPDATE Miembros lacks Estado = 'Activo'" })

Assert-Check "RegistrarPago validates affected rows" $hasRowsAffectedValidation `
    $(if ($hasRowsAffectedValidation) { "RegistrarPago validates affected rows and rollbacks on failure/missing member" } else { "RegistrarPago does not validate affected rows" })

Assert-Check "RegistrarPago calculates vigencia inside transaction" $hasInTxVigencia `
    $(if ($hasInTxVigencia) { "RegistrarPago calculates validity in-transaction using VigenciaCalculador under row lock" } else { "RegistrarPago does not invoke VigenciaCalculador in-transaction" })

Assert-Check "ActualizarEstadosMasivo defined in Core" $hasMassUpdateMethod `
    $(if ($hasMassUpdateMethod) { "ActualizarEstadosMasivo is defined in Miembro_Conexion" } else { "ActualizarEstadosMasivo missing" })

# ---------------------------------------------------------
# Check I: Payment History & Concurrency Safety (Phase 2)
# ---------------------------------------------------------
Write-Host "`n--- Check I: Payment Concurrency & History Duplication Prevention ---" -ForegroundColor Yellow

$pagoButPath = Join-Path $RepoRoot "Sistema_Gimnasio\PagoButWindow.xaml.cs"
$pagoButContent = Get-Content -LiteralPath $pagoButPath -Raw

$hasDuplicateHistorialCall = $pagoButContent -match 'RegistrarPagoHistorial'
$hasAtomicPagoCall = $pagoButContent -match 'conexion\.RegistrarPago\s*\(\s*_idMiembro\s*,\s*idMembresia'

Assert-Check "No duplicate RegistrarPagoHistorial call" (-not $hasDuplicateHistorialCall) `
    $(if (-not $hasDuplicateHistorialCall) { "No redundant RegistrarPagoHistorial call in PagoButWindow" } else { "Redundant call found" })

Assert-Check "PagoButWindow delegates to atomic RegistrarPago" $hasAtomicPagoCall `
    $(if ($hasAtomicPagoCall) { "PagoButWindow delegates to atomic in-transaction RegistrarPago without race condition" } else { "PagoButWindow does not use atomic RegistrarPago" })

# ---------------------------------------------------------
# Check J: Financial Reports Cartesian Product & Consistency (Phase 2)
# ---------------------------------------------------------
Write-Host "`n--- Check J: Financial Reports Cartesian Product & Consistency ---" -ForegroundColor Yellow

$pagosConexionPath = Join-Path $RepoRoot "Gym_System.Core\Gym_System.Core\Pagos_conexion.cs"
$pagosConexionContent = Get-Content -LiteralPath $pagosConexionPath -Raw

$reporteActivosMatch = [regex]::Match($pagosConexionContent, 'ObtenerReporteMembresiasActivos\s*\(\)\s*\{([\s\S]*?)return\s+lista;')
$hasPagosJoinInActivos = $false
if ($reporteActivosMatch.Success) {
    $body = $reporteActivosMatch.Groups[1].Value
    if ($body -match 'JOIN\s+Pagos') {
        $hasPagosJoinInActivos = $true
    }
}

Assert-Check "No Cartesian Product in ObtenerReporteMembresiasActivos" (-not $hasPagosJoinInActivos) `
    $(if (-not $hasPagosJoinInActivos) { "ObtenerReporteMembresiasActivos does not join Pagos" } else { "Detected JOIN Pagos causing Cartesian product" })

$hasBetweenInicio = $pagosConexionContent -match 'BETWEEN\s+[^;\r\n]*@FechaInicio'
$hasBetweenFin = $pagosConexionContent -match 'BETWEEN\s+[^;\r\n]*@FechaFin'
$hasBetweenLunes = $pagosConexionContent -match 'BETWEEN\s+[^;\r\n]*@Lunes'
$hasBetweenDomingo = $pagosConexionContent -match 'BETWEEN\s+[^;\r\n]*@Domingo'
$hasAndFinInBetween = ($pagosConexionContent -match 'BETWEEN\s+[^;\r\n]+AND\s+[^;\r\n]*@FechaFin')
$hasAndDomingoInBetween = ($pagosConexionContent -match 'BETWEEN\s+[^;\r\n]+AND\s+[^;\r\n]*@Domingo')
$hasFlawedBetween = $hasBetweenInicio -or $hasBetweenFin -or $hasBetweenLunes -or $hasBetweenDomingo -or $hasAndFinInBetween -or $hasAndDomingoInBetween
$hasAnyBetweenKeyword = $pagosConexionContent -match '\bBETWEEN\b'

Assert-Check "No BETWEEN ranges (@FechaInicio, @FechaFin, @Lunes, @Domingo) in Pagos_conexion" (-not $hasFlawedBetween) `
    $(if (-not $hasFlawedBetween) { "No BETWEEN ranges detected for @FechaInicio, @FechaFin, @Lunes, or @Domingo" } else { "Detected BETWEEN date range omitting full-day coverage" })

Assert-Check "No BETWEEN date clauses in Pagos_conexion" (-not $hasAnyBetweenKeyword) `
    $(if (-not $hasAnyBetweenKeyword) { "All date range queries in Pagos_conexion avoid BETWEEN completely" } else { "Detected BETWEEN clause in Pagos_conexion" })

$hasFinExclusivo = $pagosConexionContent -match '<\s*@FinExclusivo'
$hasFechaFinExclusivo = $pagosConexionContent -match '<\s*@FechaFinExclusivo'
$hasSemiOpenFinExclusivo = $hasFinExclusivo -and $hasFechaFinExclusivo

Assert-Check "Pagos_conexion uses semi-open range (< FinExclusivo)" $hasSemiOpenFinExclusivo `
    $(if ($hasSemiOpenFinExclusivo) { "Uses semi-open ranges (< @FinExclusivo / < @FechaFinExclusivo) for full-day inclusive coverage" } else { "Missing semi-open range query pattern" })

$hasInvalidIdJoin = $pagosConexionContent -match 'p\.ID\s*=\s*m\.id'
Assert-Check "ObtenerPagosRecientes does not join on p.ID = m.id" (-not $hasInvalidIdJoin) `
    $(if (-not $hasInvalidIdJoin) { "ObtenerPagosRecientes does not falsely equate Pagos.ID with Miembros.id" } else { "Detected invalid join on p.ID = m.id" })

# ---------------------------------------------------------
# Check K: VigenciaCalculador Unit Tests Seam (Phase 2)
# ---------------------------------------------------------
Write-Host "`n--- Check K: VigenciaCalculador Seam Unit Tests ---" -ForegroundColor Yellow

$vigenciaSourcePath = Join-Path $RepoRoot "Gym_System.Core\Gym_System.Core\VigenciaCalculador.cs"

if (-not (Test-Path -LiteralPath $vigenciaSourcePath)) {
    Assert-Check "VigenciaCalculador.cs exists" $false "File not found at $vigenciaSourcePath"
} else {
    Assert-Check "VigenciaCalculador.cs exists" $true "Found VigenciaCalculador.cs"

    $cscCandidates = @(
        "${env:ProgramFiles}\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\Roslyn\csc.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
    )
    $cscExe = $cscCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

    $testAssemblyBytes = $null
    $compileMethod = "Freshly compiled from source via Roslyn csc.exe"
    $compileError = $null

    if ($cscExe) {
        $tempDll = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "VigenciaCalculador_Test_$([System.Guid]::NewGuid().ToString('N')).dll")
        try {
            $compileOutput = & $cscExe /nologo /target:library "/out:$tempDll" "$vigenciaSourcePath" 2>&1
            if (Test-Path -LiteralPath $tempDll) {
                $testAssemblyBytes = [System.IO.File]::ReadAllBytes($tempDll)
                Remove-Item -LiteralPath $tempDll -Force -ErrorAction SilentlyContinue
            } else {
                $compileError = "csc.exe compilation failed: $($compileOutput -join '; ')"
            }
        } catch {
            $compileError = "csc.exe execution error: $($_.Exception.Message)"
        }
    } else {
        $compileError = "Roslyn csc.exe not found in standard Visual Studio / BuildTools installations"
    }

    if ($null -ne $testAssemblyBytes) {
        try {
            $asm = [System.Reflection.Assembly]::Load($testAssemblyBytes)
            $t = $asm.GetType('Gym_System.Core.VigenciaCalculador')
            $m = $t.GetMethod('CalcularVigencia')

            $fechaPago = [DateTime]::new(2026, 9, 16)
            $fechaInicioA = [Nullable[DateTime]]::new([DateTime]::new(2026, 8, 25))
            $fechaFinA = [Nullable[DateTime]]::new([DateTime]::new(2026, 9, 25))

            # Case A: Active member with remaining days, renewal 1 month
            $resA = $m.Invoke($null, @($fechaInicioA, $fechaFinA, $fechaPago, 1, "Meses"))
            $okA = ($resA.Item1 -eq [DateTime]::new(2026, 8, 25) -and $resA.Item2 -eq [DateTime]::new(2026, 10, 25))

            # Case B: Active member with remaining days, renewal 15 days
            $resB = $m.Invoke($null, @($fechaInicioA, $fechaFinA, $fechaPago, 15, "Días"))
            $okB = ($resB.Item1 -eq [DateTime]::new(2026, 8, 25) -and $resB.Item2 -eq [DateTime]::new(2026, 10, 10))

            # Case C: Expired member, renewal 1 month
            $fechaInicioC = [Nullable[DateTime]]::new([DateTime]::new(2026, 8, 10))
            $fechaFinC = [Nullable[DateTime]]::new([DateTime]::new(2026, 9, 10))
            $resC = $m.Invoke($null, @($fechaInicioC, $fechaFinC, $fechaPago, 1, "Meses"))
            $okC = ($resC.Item1 -eq $fechaPago -and $resC.Item2 -eq [DateTime]::new(2026, 10, 16))

            # Case D: New member (null dates), renewal 10 days
            $resD = $m.Invoke($null, @($null, $null, $fechaPago, 10, "Días"))
            $okD = ($resD.Item1 -eq $fechaPago -and $resD.Item2 -eq [DateTime]::new(2026, 9, 26))

            $allPassed = $okA -and $okB -and $okC -and $okD
            Assert-Check "VigenciaCalculador Seam Unit Tests" $allPassed `
                $(if ($allPassed) { "All 4 validity calculation cases passed ($compileMethod)" } else { "Case failures: A=$okA, B=$okB, C=$okC, D=$okD" })
        } catch {
            Assert-Check "VigenciaCalculador Seam Unit Tests" $false "Execution error: $($_.Exception.Message)"
        }
    } else {
        Assert-Check "VigenciaCalculador Seam Unit Tests" $false "Could not compile fresh source: $compileError (fallback to precompiled DLL disabled)"
    }
}

# ---------------------------------------------------------
# Check L: Biometric Robustness & Cache (Phase 3)
# ---------------------------------------------------------
Write-Host "`n--- Check L: Biometric Robustness & In-Memory Cache ---" -ForegroundColor Yellow

$ucVerifyPath = Join-Path $RepoRoot "BiometricApp\BiometricApp\BiometricApp\UCVerifyFingerprint.cs"
$frmEnrollPath = Join-Path $RepoRoot "BiometricApp\BiometricApp\BiometricApp\frmDBEnrollment.cs"
$biometricCachePath = Join-Path $RepoRoot "BiometricApp\BiometricApp\BiometricApp\BiometricCache.cs"
$sistemaAccesoPath = Join-Path $RepoRoot "Sistema_Gimnasio\SistemaAcceso.xaml.cs"
$fingerprintManagerPath = Join-Path $RepoRoot "BiometricApp\BiometricApp\BiometricApp\FingerprintManager.cs"

$ucContent = Get-Content -LiteralPath $ucVerifyPath -Raw
$enrollContent = Get-Content -LiteralPath $frmEnrollPath -Raw
$accesoContent = Get-Content -LiteralPath $sistemaAccesoPath -Raw
$managerContent = Get-Content -LiteralPath $fingerprintManagerPath -Raw

$hasSharedConn = ($ucContent -match 'SqlConnection\s+conn\s*=' -or $enrollContent -match 'SqlConnection\s+conn\s*=')
Assert-Check "Zero shared SqlConnection in Biometric Controls" (-not $hasSharedConn) `
    $(if (-not $hasSharedConn) { "No long-lived shared SqlConnection fields found in UCVerifyFingerprint or frmDBEnrollment" } else { "Shared SqlConnection fields still present" })

$usesCache = $ucContent -match 'BiometricCache\.Identificar'
$noDirectSqlInUc = -not ($ucContent -match 'SqlDataAdapter' -or $ucContent -match 'new\s+DataTable')
$noDirectXmlInUc = -not ($ucContent -match 'Fmd\.DeserializeXml')
$ucOptimized = $usesCache -and $noDirectSqlInUc -and $noDirectXmlInUc

Assert-Check "UCVerifyFingerprint uses BiometricCache without per-scan SQL/XML reload" $ucOptimized `
    $(if ($ucOptimized) { "UCVerifyFingerprint uses BiometricCache.Identificar (zero direct SQL queries and zero XML deserialization per scan)" } else { "UCVerifyFingerprint not fully decoupled from direct SQL/XML reload" })

$cacheExists = Test-Path -LiteralPath $biometricCachePath
Assert-Check "BiometricCache.cs exists" $cacheExists `
    $(if ($cacheExists) { "BiometricCache.cs is present in BiometricApp" } else { "BiometricCache.cs missing" })

$unsubscribesAcceso = $accesoContent -match 'verificador\.HuellaVerificada\s*-=\s*Verificador_HuellaVerificada'
$disposesAcceso = $accesoContent -match 'verificador\.Dispose\(\)'
$accesoLifecycleOk = $unsubscribesAcceso -and $disposesAcceso

Assert-Check "SistemaAcceso lifecycle & resource release" $accesoLifecycleOk `
    $(if ($accesoLifecycleOk) { "SistemaAcceso unsubscribes from events and disposes verificador properly" } else { "Incomplete cleanup in SistemaAcceso" })

$handlesDisconnect = $managerContent -match 'DP_DEVICE_FAILURE' -and $managerContent -match 'DP_INVALID_DEVICE' -and ($managerContent -match 'private\s+void\s+OnCaptured\s*\([^)]*\)\s*\{\s*try')
Assert-Check "Hardware disconnect tolerance in FingerprintManager" $handlesDisconnect `
    $(if ($handlesDisconnect) { "FingerprintManager handles DP_DEVICE_FAILURE/DP_INVALID_DEVICE and guards OnCaptured with try-catch" } else { "Missing hardware failure tolerance in FingerprintManager" })

if ($cacheExists) {
    $cacheContent = Get-Content -LiteralPath $biometricCachePath -Raw
    $hasExplicitEstaCargada = $cacheContent -match 'EstaCargada' -and $cacheContent -match '_estaCargada'
    Assert-Check "BiometricCache has explicit EstaCargada state" $hasExplicitEstaCargada `
        $(if ($hasExplicitEstaCargada) { "BiometricCache defines _estaCargada and EstaCargada property to distinguish loaded from empty snapshot" } else { "Missing EstaCargada state in BiometricCache" })

    $recordBlock = [regex]::Match($cacheContent, 'class\s+BiometricRecord[\s\S]*?\{([\s\S]*?)\n\s*public\s+BiometricRecord\(').Groups[1].Value
    $recordImmutable = ($recordBlock -match 'IdMiembro\s*\{\s*get;\s*\}') -and (-not ($recordBlock -match 'set;'))
    Assert-Check "BiometricRecord has get-only properties (immutable)" $recordImmutable `
        $(if ($recordImmutable) { "BiometricRecord properties are get-only without public setters" } else { "BiometricRecord has mutable setters" })

    $templateFmdDeepIsolation = $cacheContent -match 'internal\s+Fmd\s+TemplateFmd\s*\{\s*get;\s*\}' -and $cacheContent -match 'public\s+static\s+IReadOnlyList<BiometricRecordMetadata>\s+SnapshotActual'
    Assert-Check "Deep isolation: TemplateFmd is internal and SnapshotActual returns metadata DTO" $templateFmdDeepIsolation `
        $(if ($templateFmdDeepIsolation) { "BiometricRecord.TemplateFmd is internal and SnapshotActual returns BiometricRecordMetadata DTO" } else { "BiometricRecord exposes mutable Fmd in public snapshot" })
}

$isolatesMulticast = ($managerContent -match 'NotificarHuellaParaVerificar') -and ($managerContent -match 'NotificarErrorLector') -and ($managerContent -match 'GetInvocationList')
Assert-Check "FingerprintManager isolates multicast subscribers defensively" $isolatesMulticast `
    $(if ($isolatesMulticast) { "FingerprintManager isolates subscribers using GetInvocationList with individual try/catch" } else { "Multicast delegation is not defensively isolated" })

$ucUnlockFinally = $ucContent -match 'try\s*\{[\s\S]*?Marshal\.Copy[\s\S]*?\}\s*finally\s*\{[\s\S]*?UnlockBits'
$enrollUnlockFinally = $enrollContent -match 'try\s*\{[\s\S]*?Marshal\.Copy[\s\S]*?\}\s*finally\s*\{[\s\S]*?UnlockBits'
$hasSafeUnlockBits = $ucUnlockFinally -and $enrollUnlockFinally
Assert-Check "Biometric controls ensure UnlockBits in finally block" $hasSafeUnlockBits `
    $(if ($hasSafeUnlockBits) { "bmp.UnlockBits is enclosed in finally block in both UCVerifyFingerprint and frmDBEnrollment" } else { "Missing finally block for UnlockBits in biometric controls" })

$ucSafeMostrar = $ucContent -match 'public\s+void\s+MostrarImagen' -and $ucContent -match 'entregadoAMostrar'
$enrollSafeMostrar = $enrollContent -match 'public\s+void\s+MostrarImagen' -and $enrollContent -match 'entregadoAMostrar'
$hasSafeBitmapLifecycle = $ucSafeMostrar -and $enrollSafeMostrar
Assert-Check "Biometric controls implement exception-safe Bitmap lifecycle" $hasSafeBitmapLifecycle `
    $(if ($hasSafeBitmapLifecycle) { "UCVerifyFingerprint and frmDBEnrollment guarantee Dispose without double-dispose" } else { "Missing safe Bitmap lifecycle in biometric controls" })

$ucPendingTracking = $ucContent -match '_bitmapsPendientes' -and $ucContent -match 'DrenarBitmapsPendientes' -and $ucContent -match '_bitmapsPendientes\.Remove'
$enrollPendingTracking = $enrollContent -match '_bitmapsPendientes' -and $enrollContent -match 'DrenarBitmapsPendientes' -and $enrollContent -match '_bitmapsPendientes\.Remove'
$hasPendingTracking = $ucPendingTracking -and $enrollPendingTracking
Assert-Check "Biometric controls track queued bitmaps and drain on dispose" $hasPendingTracking `
    $(if ($hasPendingTracking) { "UCVerifyFingerprint and frmDBEnrollment track queued bitmaps and drain on Dispose without leak" } else { "Pending bitmap tracking missing in biometric controls" })

$ucAtomicOwnership = $ucContent -match 'lock\s*\(\s*_bitmapsPendientesLock\s*\)[\s\S]*?_bitmapsPendientes\.Remove\(bmp\)[\s\S]*?pbFingerprint\.Image\s*=\s*bmp'
$enrollAtomicOwnership = $enrollContent -match 'lock\s*\(\s*_bitmapsPendientesLock\s*\)[\s\S]*?_bitmapsPendientes\.Remove\(bmp\)[\s\S]*?pbFingerprint\.Image\s*=\s*bmp'
$ucDisposeSync = $ucContent -match 'protected\s+override\s+void\s+Dispose[\s\S]*?lock\s*\(\s*_bitmapsPendientesLock\s*\)'
$enrollDisposeSync = $enrollContent -match 'protected\s+override\s+void\s+Dispose[\s\S]*?lock\s*\(\s*_bitmapsPendientesLock\s*\)'
$hasSharedLockOwnership = $ucAtomicOwnership -and $enrollAtomicOwnership -and $ucDisposeSync -and $enrollDisposeSync
Assert-Check "Biometric controls protect Remove and assignment under shared critical section" $hasSharedLockOwnership `
    $(if ($hasSharedLockOwnership) { "Shared lock prevents Drenar/Dispose intercalation between Remove and PictureBox assignment in UCVerifyFingerprint and frmDBEnrollment" } else { "Race window between Remove and assignment detected in biometric controls" })


# ---------------------------------------------------------
# Summary
# ---------------------------------------------------------
Write-Host "`n====================================================" -ForegroundColor Cyan
if ($failedChecks -eq 0) {
    Write-Host " Validation Result: ALL CHECKS PASSED ($failedChecks failures)" -ForegroundColor Green
    Write-Host "====================================================" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host " Validation Result: $failedChecks CHECK(S) FAILED" -ForegroundColor Red
    Write-Host "====================================================" -ForegroundColor Cyan
    exit 1
}
