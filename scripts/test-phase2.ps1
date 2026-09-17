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
Write-Host " Running Phase 2 Automated Checks & Seam Tests" -ForegroundColor Cyan
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
# Test 1: Zero MessageBox and WinForms references in Gym_System.Core
# ---------------------------------------------------------
Write-Host "`n--- Test 1: Zero MessageBox & WinForms in Gym_System.Core ---" -ForegroundColor Yellow
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
    $(if ($msgBoxMatches.Count -eq 0) { "No MessageBox references found in Core" } else { "Found: $($msgBoxMatches -join '; ')" })

Assert-Check "Zero WinForms using in Gym_System.Core" ($winFormsUsings.Count -eq 0) `
    $(if ($winFormsUsings.Count -eq 0) { "No WinForms using statements found in Core" } else { "Found: $($winFormsUsings -join '; ')" })

# Absence of System.Windows.Forms reference in Gym_System.Core.csproj
$coreCsprojPath = Join-Path $RepoRoot "Gym_System.Core\Gym_System.Core\Gym_System.Core.csproj"
if (Test-Path -LiteralPath $coreCsprojPath) {
    $csprojContent = Get-Content -LiteralPath $coreCsprojPath -Raw
    $hasWinFormsRef = $csprojContent -match 'System\.Windows\.Forms'
    Assert-Check "Zero WinForms in Gym_System.Core.csproj" (-not $hasWinFormsRef) `
        $(if (-not $hasWinFormsRef) { "Gym_System.Core.csproj has no System.Windows.Forms reference (UI decoupled)" } else { "Gym_System.Core.csproj retains System.Windows.Forms reference" })
} else {
    Assert-Check "Zero WinForms in Gym_System.Core.csproj" $false "Gym_System.Core.csproj not found"
}

# ---------------------------------------------------------
# Test 2: N+1 Prevention & Mass Update in MiembrosView.xaml.cs
# ---------------------------------------------------------
Write-Host "`n--- Test 2: N+1 Prevention & Mass Update in MiembrosView.xaml.cs ---" -ForegroundColor Yellow
$miembrosViewPath = Join-Path $RepoRoot "Sistema_Gimnasio\MiembrosView.xaml.cs"
$miembrosViewContent = Get-Content -LiteralPath $miembrosViewPath -Raw

$hasActualizarEstadosMasivoInView = $miembrosViewContent -match 'conexion\.ActualizarEstadosMasivo\s*\('
$hasN1LoopInView = $miembrosViewContent -match 'foreach\s*\([^\)]*\)\s*\{[^}]*ActualizarEstadoMiembro'

# Verify ActualizarEstados_Automatico does not show popups
$automaticoMatch = [regex]::Match($miembrosViewContent, 'void\s+ActualizarEstados_Automatico\s*\(\)\s*\{([\s\S]*?)\n\s*\}')
$hasPopupInAutomatico = $false
if ($automaticoMatch.Success) {
    if ($automaticoMatch.Groups[1].Value -match 'MessageBox\.Show') {
        $hasPopupInAutomatico = $true
    }
}

Assert-Check "MiembrosView uses ActualizarEstadosMasivo" $hasActualizarEstadosMasivoInView `
    $(if ($hasActualizarEstadosMasivoInView) { "View invokes ActualizarEstadosMasivo" } else { "View does not invoke ActualizarEstadosMasivo" })

Assert-Check "No N+1 foreach loop updating member states" (-not $hasN1LoopInView) `
    $(if (-not $hasN1LoopInView) { "No N+1 iteration detected in MiembrosView" } else { "Detected N+1 foreach loop calling ActualizarEstadoMiembro" })

Assert-Check "No repetitive MessageBox in ActualizarEstados_Automatico" (-not $hasPopupInAutomatico) `
    $(if (-not $hasPopupInAutomatico) { "ActualizarEstados_Automatico executes silently without popup" } else { "Detected MessageBox.Show in ActualizarEstados_Automatico" })

# ---------------------------------------------------------
# Test 3: Atomic Transaction in Miembros_Conexion.cs (RegistrarPago)
# ---------------------------------------------------------
Write-Host "`n--- Test 3: Atomic Transaction in Miembros_Conexion.cs ---" -ForegroundColor Yellow
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

Assert-Check "RegistrarPago updates Estado to Activo" $hasEstadoActivoInUpdate `
    $(if ($hasEstadoActivoInUpdate) { "RegistrarPago SQL updates Estado = 'Activo'" } else { "UPDATE Miembros does not include Estado = 'Activo'" })

Assert-Check "RegistrarPago validates affected rows" $hasRowsAffectedValidation `
    $(if ($hasRowsAffectedValidation) { "RegistrarPago validates affected rows and rollbacks on failure/missing member" } else { "RegistrarPago does not validate affected rows" })

Assert-Check "RegistrarPago calculates vigencia inside transaction" $hasInTxVigencia `
    $(if ($hasInTxVigencia) { "RegistrarPago calculates validity in-transaction using VigenciaCalculador under row lock" } else { "RegistrarPago does not invoke VigenciaCalculador in-transaction" })

Assert-Check "ActualizarEstadosMasivo defined in Core" $hasMassUpdateMethod `
    $(if ($hasMassUpdateMethod) { "ActualizarEstadosMasivo exists in Miembro_Conexion" } else { "ActualizarEstadosMasivo not found in Miembro_Conexion" })

# ---------------------------------------------------------
# Test 4: PagoButWindow Concurrency & No Duplicate Payment History
# ---------------------------------------------------------
Write-Host "`n--- Test 4: PagoButWindow Concurrency & History ---" -ForegroundColor Yellow
$pagoButPath = Join-Path $RepoRoot "Sistema_Gimnasio\PagoButWindow.xaml.cs"
$pagoButContent = Get-Content -LiteralPath $pagoButPath -Raw

$hasDuplicateHistorialCall = $pagoButContent -match 'RegistrarPagoHistorial'
$hasAtomicPagoCall = $pagoButContent -match 'conexion\.RegistrarPago\s*\(\s*_idMiembro\s*,\s*idMembresia'

Assert-Check "No duplicate RegistrarPagoHistorial call" (-not $hasDuplicateHistorialCall) `
    $(if (-not $hasDuplicateHistorialCall) { "No redundant RegistrarPagoHistorial call found in PagoButWindow" } else { "Detected redundant RegistrarPagoHistorial call creating duplicate payment record" })

Assert-Check "PagoButWindow delegates to atomic RegistrarPago" $hasAtomicPagoCall `
    $(if ($hasAtomicPagoCall) { "PagoButWindow delegates to atomic in-transaction RegistrarPago without race condition" } else { "PagoButWindow does not use atomic RegistrarPago" })

# ---------------------------------------------------------
# Test 5: Financial Reports Cartesian Product & Consistency (Pagos_conexion.cs)
# ---------------------------------------------------------
Write-Host "`n--- Test 5: Financial Reports Cartesian Product & Consistency ---" -ForegroundColor Yellow
$pagosConexionPath = Join-Path $RepoRoot "Gym_System.Core\Gym_System.Core\Pagos_conexion.cs"
$pagosConexionContent = Get-Content -LiteralPath $pagosConexionPath -Raw

# In ObtenerReporteMembresiasActivos:
# Should NOT have LEFT JOIN Pagos
$reporteActivosMatch = [regex]::Match($pagosConexionContent, 'ObtenerReporteMembresiasActivos\s*\(\)\s*\{([\s\S]*?)return\s+lista;')
$hasPagosJoinInActivos = $false
if ($reporteActivosMatch.Success) {
    $body = $reporteActivosMatch.Groups[1].Value
    if ($body -match 'JOIN\s+Pagos') {
        $hasPagosJoinInActivos = $true
    }
}

Assert-Check "No Cartesian Product in ObtenerReporteMembresiasActivos" (-not $hasPagosJoinInActivos) `
    $(if (-not $hasPagosJoinInActivos) { "ObtenerReporteMembresiasActivos does not join Pagos (Cartesian product eliminated)" } else { "Detected JOIN Pagos in ObtenerReporteMembresiasActivos causing Cartesian product" })

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
# Test 6: Seam Unit Tests for VigenciaCalculador
# ---------------------------------------------------------
Write-Host "`n--- Test 6: Seam Unit Tests for VigenciaCalculador ---" -ForegroundColor Yellow
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
# Summary
# ---------------------------------------------------------
Write-Host "`n====================================================" -ForegroundColor Cyan
if ($failedChecks -eq 0) {
    Write-Host " Phase 2 Validation: ALL CHECKS PASSED ($failedChecks failures)" -ForegroundColor Green
    Write-Host "====================================================" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host " Phase 2 Validation: $failedChecks CHECK(S) FAILED" -ForegroundColor Red
    Write-Host "====================================================" -ForegroundColor Cyan
    exit 1
}
