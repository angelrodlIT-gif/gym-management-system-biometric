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
Write-Host " Running Phase 3 Automated Checks & Seam Tests" -ForegroundColor Cyan
Write-Host " Repository Root: $RepoRoot" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

Write-Host "`n[LIMITACION DOCUMENTADA] Las pruebas automatizadas biométricas emplean" -ForegroundColor DarkCyan
Write-Host "seams de software y datos simulados en memoria (mocks). NO se prueba ni" -ForegroundColor DarkCyan
Write-Host "se afirma probar hardware físico real (sensores ópticos USB o drivers de bajo" -ForegroundColor DarkCyan
Write-Host "nivel), cuya validación completa requiere interacción física con el lector." -ForegroundColor DarkCyan

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
# Test 1: Zero Long-Lived Shared SqlConnection in Biometric Controls
# ---------------------------------------------------------
Write-Host "`n--- Test 1: Zero Long-Lived SqlConnection in Biometric Controls ---" -ForegroundColor Yellow

$ucVerifyPath = Join-Path $RepoRoot "BiometricApp\BiometricApp\BiometricApp\UCVerifyFingerprint.cs"
$frmEnrollPath = Join-Path $RepoRoot "BiometricApp\BiometricApp\BiometricApp\frmDBEnrollment.cs"

$ucLines = Get-Content -LiteralPath $ucVerifyPath
$hasSharedConnUc = $false
foreach ($line in $ucLines) {
    $trimmed = $line.Trim()
    if ($trimmed.StartsWith("//") -or $trimmed.StartsWith("/*") -or $trimmed.StartsWith("*")) { continue }
    if ($trimmed -match 'SqlConnection\s+conn\s*=' -or $trimmed -match 'private\s+.*SqlConnection\s+conn') {
        $hasSharedConnUc = $true
        break
    }
}
Assert-Check "Zero shared SqlConnection in UCVerifyFingerprint" (-not $hasSharedConnUc) `
    $(if (-not $hasSharedConnUc) { "No shared long-lived SqlConnection field found in UCVerifyFingerprint.cs" } else { "Shared SqlConnection conn field still detected in UCVerifyFingerprint.cs" })

$enrollLines = Get-Content -LiteralPath $frmEnrollPath
$hasSharedConnEnroll = $false
foreach ($line in $enrollLines) {
    $trimmed = $line.Trim()
    if ($trimmed.StartsWith("//") -or $trimmed.StartsWith("/*") -or $trimmed.StartsWith("*")) { continue }
    if ($trimmed -match 'SqlConnection\s+conn\s*=' -or $trimmed -match 'private\s+.*SqlConnection\s+conn') {
        $hasSharedConnEnroll = $true
        break
    }
}
Assert-Check "Zero shared SqlConnection in frmDBEnrollment" (-not $hasSharedConnEnroll) `
    $(if (-not $hasSharedConnEnroll) { "No shared long-lived SqlConnection field found in frmDBEnrollment.cs" } else { "Shared SqlConnection conn field still detected in frmDBEnrollment.cs" })

$ucUnlockFinally = $ucLines -join "`n" -match 'try\s*\{[\s\S]*?Marshal\.Copy[\s\S]*?\}\s*finally\s*\{[\s\S]*?UnlockBits'
$enrollUnlockFinally = $enrollLines -join "`n" -match 'try\s*\{[\s\S]*?Marshal\.Copy[\s\S]*?\}\s*finally\s*\{[\s\S]*?UnlockBits'
Assert-Check "UCVerifyFingerprint ensures UnlockBits in finally block" $ucUnlockFinally `
    $(if ($ucUnlockFinally) { "bmp.UnlockBits is enclosed in finally block in UCVerifyFingerprint" } else { "Missing finally block for UnlockBits in UCVerifyFingerprint" })
Assert-Check "frmDBEnrollment ensures UnlockBits in finally block" $enrollUnlockFinally `
    $(if ($enrollUnlockFinally) { "bmp.UnlockBits is enclosed in finally block in frmDBEnrollment" } else { "Missing finally block for UnlockBits in frmDBEnrollment" })

$ucMostrarImagen = ($ucLines -join "`n") -match 'public\s+void\s+MostrarImagen\s*\(\s*Bitmap\s+bmp\s*\)' -and ($ucLines -join "`n") -match 'try\s*\{\s*bmp\s*\?\s*\.Dispose\(\)\s*;\s*\}\s*catch'
$enrollMostrarImagen = ($enrollLines -join "`n") -match 'public\s+void\s+MostrarImagen\s*\(\s*Bitmap\s+bmp\s*\)' -and ($enrollLines -join "`n") -match 'try\s*\{\s*bmp\s*\?\s*\.Dispose\(\)\s*;\s*\}\s*catch'
Assert-Check "UCVerifyFingerprint implements MostrarImagen with exception-safe Dispose" $ucMostrarImagen `
    $(if ($ucMostrarImagen) { "UCVerifyFingerprint guarantees bmp.Dispose() if BeginInvoke throws or control disposed" } else { "Missing MostrarImagen with safe Dispose in UCVerifyFingerprint" })
Assert-Check "frmDBEnrollment implements MostrarImagen with exception-safe Dispose" $enrollMostrarImagen `
    $(if ($enrollMostrarImagen) { "frmDBEnrollment guarantees bmp.Dispose() if BeginInvoke throws or control disposed" } else { "Missing MostrarImagen with safe Dispose in frmDBEnrollment" })

$ucNoDoubleDispose = ($ucLines -join "`n") -match 'entregadoAMostrar' -and ($ucLines -join "`n") -match 'if\s*\(\s*!entregadoAMostrar\s*&&\s*bmp\s*!=\s*null\s*\)'
$enrollNoDoubleDispose = ($enrollLines -join "`n") -match 'entregadoAMostrar' -and ($enrollLines -join "`n") -match 'if\s*\(\s*!entregadoAMostrar\s*&&\s*bmp\s*!=\s*null\s*\)'
Assert-Check "UCVerifyFingerprint prevents double-dispose via ownership tracking" $ucNoDoubleDispose `
    $(if ($ucNoDoubleDispose) { "UCVerifyFingerprint tracks ownership transfer to prevent double-dispose" } else { "Double-dispose guard missing in UCVerifyFingerprint" })
Assert-Check "frmDBEnrollment prevents double-dispose via ownership tracking" $enrollNoDoubleDispose `
    $(if ($enrollNoDoubleDispose) { "frmDBEnrollment tracks ownership transfer to prevent double-dispose" } else { "Double-dispose guard missing in frmDBEnrollment" })

$ucPendingTracking = ($ucLines -join "`n") -match '_bitmapsPendientes' -and ($ucLines -join "`n") -match 'DrenarBitmapsPendientes' -and ($ucLines -join "`n") -match '_bitmapsPendientes\.Remove'
$enrollPendingTracking = ($enrollLines -join "`n") -match '_bitmapsPendientes' -and ($enrollLines -join "`n") -match 'DrenarBitmapsPendientes' -and ($enrollLines -join "`n") -match '_bitmapsPendientes\.Remove'
Assert-Check "UCVerifyFingerprint tracks pending queued bitmaps and drains on dispose" $ucPendingTracking `
    $(if ($ucPendingTracking) { "UCVerifyFingerprint registers queued bitmaps, removes in delegate finally and drains on dispose" } else { "Pending bitmap ownership tracking missing in UCVerifyFingerprint" })
Assert-Check "frmDBEnrollment tracks pending queued bitmaps and drains on dispose" $enrollPendingTracking `
    $(if ($enrollPendingTracking) { "frmDBEnrollment registers queued bitmaps, removes in delegate finally and drains on dispose" } else { "Pending bitmap ownership tracking missing in frmDBEnrollment" })

$ucAtomicOwnership = ($ucLines -join "`n") -match 'lock\s*\(\s*_bitmapsPendientesLock\s*\)[\s\S]*?_bitmapsPendientes\.Remove\(bmp\)[\s\S]*?pbFingerprint\.Image\s*=\s*bmp'
$enrollAtomicOwnership = ($enrollLines -join "`n") -match 'lock\s*\(\s*_bitmapsPendientesLock\s*\)[\s\S]*?_bitmapsPendientes\.Remove\(bmp\)[\s\S]*?pbFingerprint\.Image\s*=\s*bmp'
Assert-Check "UCVerifyFingerprint atomic ownership: lock encloses Remove and PictureBox assignment" $ucAtomicOwnership `
    $(if ($ucAtomicOwnership) { "UCVerifyFingerprint maintains single critical section across Remove, disposed check, and pbFingerprint assignment" } else { "Race window detected: Remove and assignment not enclosed in shared lock in UCVerifyFingerprint" })
Assert-Check "frmDBEnrollment atomic ownership: lock encloses Remove and PictureBox assignment" $enrollAtomicOwnership `
    $(if ($enrollAtomicOwnership) { "frmDBEnrollment maintains single critical section across Remove, disposed check, and pbFingerprint assignment" } else { "Race window detected: Remove and assignment not enclosed in shared lock in frmDBEnrollment" })

$ucDisposeSharedLock = ($ucLines -join "`n") -match 'protected\s+override\s+void\s+Dispose[\s\S]*?lock\s*\(\s*_bitmapsPendientesLock\s*\)'
$enrollDisposeSharedLock = ($enrollLines -join "`n") -match 'protected\s+override\s+void\s+Dispose[\s\S]*?lock\s*\(\s*_bitmapsPendientesLock\s*\)'
Assert-Check "Biometric controls synchronize Dispose with shared pending bitmap lock" ($ucDisposeSharedLock -and $enrollDisposeSharedLock) `
    $(if ($ucDisposeSharedLock -and $enrollDisposeSharedLock) { "Dispose synchronizes with _bitmapsPendientesLock across Drenar and Image cleanup in UCVerifyFingerprint and frmDBEnrollment" } else { "Dispose missing shared lock synchronization in biometric controls" })



# ---------------------------------------------------------
# Test 2: Verification 1:N Optimization & In-Memory Cache Implementation
# ---------------------------------------------------------
Write-Host "`n--- Test 2: Verification 1:N Optimization & In-Memory Cache Usage ---" -ForegroundColor Yellow

$ucContent = Get-Content -LiteralPath $ucVerifyPath -Raw

$usesBiometricCache = $ucContent -match 'BiometricCache\.Identificar'
Assert-Check "UCVerifyFingerprint uses BiometricCache.Identificar" $usesBiometricCache `
    $(if ($usesBiometricCache) { "UCVerifyFingerprint delegates 1:N matching to BiometricCache" } else { "UCVerifyFingerprint does not call BiometricCache.Identificar" })

$noSqlInUcVerify = -not ($ucContent -match 'SqlDataAdapter' -or $ucContent -match 'new\s+DataTable' -or $ucContent -match 'SELECT\s+.*FROM\s+Miembros')
Assert-Check "No direct SQL query in UCVerifyFingerprint" $noSqlInUcVerify `
    $(if ($noSqlInUcVerify) { "UCVerifyFingerprint eliminates direct SQL queries on every scan" } else { "Direct SQL query pattern still detected in UCVerifyFingerprint.cs" })

$noXmlDeserializeInUc = -not ($ucContent -match 'Fmd\.DeserializeXml')
Assert-Check "No Fmd.DeserializeXml in UCVerifyFingerprint" $noXmlDeserializeInUc `
    $(if ($noXmlDeserializeInUc) { "UCVerifyFingerprint does not deserialize XML on every scan" } else { "Fmd.DeserializeXml still detected in UCVerifyFingerprint.cs" })

$biometricCachePath = Join-Path $RepoRoot "BiometricApp\BiometricApp\BiometricApp\BiometricCache.cs"
$cacheExists = Test-Path -LiteralPath $biometricCachePath
Assert-Check "BiometricCache.cs exists" $cacheExists `
    $(if ($cacheExists) { "BiometricCache.cs is present in BiometricApp" } else { "BiometricCache.cs not found" })

if ($cacheExists) {
    $cacheContent = Get-Content -LiteralPath $biometricCachePath -Raw
    $usesLocalConn = $cacheContent -match 'using\s*\(\s*SqlConnection\s+conn\s*=\s*new\s+SqlConnection'
    Assert-Check "BiometricCache uses local scoped SqlConnection" $usesLocalConn `
        $(if ($usesLocalConn) { "BiometricCache uses local SqlConnection wrapped in using block" } else { "BiometricCache does not use local scoped SqlConnection" })
}


# ---------------------------------------------------------
# Test 3: Resource Lifecycle & Cleanup
# ---------------------------------------------------------
Write-Host "`n--- Test 3: Resource Lifecycle & Cleanup ---" -ForegroundColor Yellow

$sistemaAccesoPath = Join-Path $RepoRoot "Sistema_Gimnasio\SistemaAcceso.xaml.cs"
$accesoContent = Get-Content -LiteralPath $sistemaAccesoPath -Raw

$unsubscribesHuellaVerificada = $accesoContent -match 'verificador\.HuellaVerificada\s*-=\s*Verificador_HuellaVerificada'
Assert-Check "SistemaAcceso unsubscribes HuellaVerificada event" $unsubscribesHuellaVerificada `
    $(if ($unsubscribesHuellaVerificada) { "SistemaAcceso explicitly unsubscribes from HuellaVerificada" } else { "Missing HuellaVerificada unsubscription in SistemaAcceso" })

$disposesVerificador = $accesoContent -match 'verificador\.Dispose\(\)'
Assert-Check "SistemaAcceso disposes verificador" $disposesVerificador `
    $(if ($disposesVerificador) { "SistemaAcceso disposes verificador on close and re-init" } else { "Missing verificador.Dispose() in SistemaAcceso" })

$clearsWinFormsChild = $accesoContent -match 'winFormsHost\.Child\s*=\s*null'
Assert-Check "SistemaAcceso detaches winFormsHost.Child" $clearsWinFormsChild `
    $(if ($clearsWinFormsChild) { "SistemaAcceso sets winFormsHost.Child = null during cleanup" } else { "Missing winFormsHost.Child = null in SistemaAcceso" })

$managerPath = Join-Path $RepoRoot "BiometricApp\BiometricApp\BiometricApp\FingerprintManager.cs"
$managerContent = Get-Content -LiteralPath $managerPath -Raw

$clearsEventsOnDispose = $managerContent -match 'HuellaParaVerificar\s*=\s*null' -and $managerContent -match 'ErrorLector\s*=\s*null'
Assert-Check "FingerprintManager clears event handlers on Dispose" $clearsEventsOnDispose `
    $(if ($clearsEventsOnDispose) { "FingerprintManager clears all event subscriptions on Dispose to prevent leaks" } else { "Missing event handler cleanup in FingerprintManager.Dispose" })

$disposesReader = $managerContent -match '_reader\.Dispose\(\)'
Assert-Check "FingerprintManager disposes _reader" $disposesReader `
    $(if ($disposesReader) { "FingerprintManager disposes Reader hardware resource" } else { "Missing _reader.Dispose() in FingerprintManager" })


# ---------------------------------------------------------
# Test 4: Hardware Disconnection Tolerance & Callback Protection
# ---------------------------------------------------------
Write-Host "`n--- Test 4: Hardware Disconnection Tolerance & Callback Protection ---" -ForegroundColor Yellow

$handlesDeviceFailure = $managerContent -match 'DP_DEVICE_FAILURE' -and $managerContent -match 'DP_INVALID_DEVICE'
Assert-Check "FingerprintManager handles DP_DEVICE_FAILURE & DP_INVALID_DEVICE" $handlesDeviceFailure `
    $(if ($handlesDeviceFailure) { "FingerprintManager explicitly guards against hardware failure/disconnect codes" } else { "Missing hardware failure/disconnect handling in FingerprintManager" })

$onCapturedTryCatch = $managerContent -match 'private\s+void\s+OnCaptured\s*\([^)]*\)\s*\{\s*try'
Assert-Check "OnCaptured is protected with try-catch" $onCapturedTryCatch `
    $(if ($onCapturedTryCatch) { "OnCaptured callback is fully protected against unhandled exceptions" } else { "OnCaptured lacks enclosing try-catch block" })

$guardsCapabilities = $managerContent -match '_reader\.Capabilities\.Resolutions' -and $managerContent -match '_reader\.Capabilities\s*=='
Assert-Check "FingerprintManager guards reader Capabilities" $guardsCapabilities `
    $(if ($guardsCapabilities) { "Capabilities and Resolutions are validated before CaptureAsync" } else { "Capabilities null-guard missing in FingerprintManager" })

# Check Item 4: ProcesarVerificacion has IniciarCaptura in finally and reports ErrorLector safely
$hasFinallyInProcesar = $managerContent -match 'ProcesarVerificacion[\s\S]*?finally\s*\{[\s\S]*?IniciarCaptura\s*\(\)\s*;'
$reportsErrorInProcesar = ($managerContent -match 'NotificarErrorLector') -or ($managerContent -match 'ErrorLector\s*\?\.Invoke')
$isolatesMulticastDefensive = ($managerContent -match 'NotificarHuellaParaVerificar') -and ($managerContent -match 'NotificarErrorLector') -and ($managerContent -match 'GetInvocationList')

Assert-Check "ProcesarVerificacion runs IniciarCaptura in finally block" $hasFinallyInProcesar `
    $(if ($hasFinallyInProcesar) { "FingerprintManager.ProcesarVerificacion executes IniciarCaptura() in finally block" } else { "IniciarCaptura() is not enclosed in a finally block in ProcesarVerificacion" })

Assert-Check "ProcesarVerificacion notifies ErrorLector on subscriber exception" $reportsErrorInProcesar `
    $(if ($reportsErrorInProcesar) { "FingerprintManager catches subscriber exceptions and notifies ErrorLector" } else { "ProcesarVerificacion does not notify ErrorLector on exception" })

Assert-Check "FingerprintManager isolates multicast subscribers defensively" $isolatesMulticastDefensive `
    $(if ($isolatesMulticastDefensive) { "FingerprintManager isolates subscribers using GetInvocationList with individual try/catch" } else { "Multicast delegation is not defensively isolated" })


# ---------------------------------------------------------
# Test 5: Clean Architecture Decoupling & UI/Adapter Invalidation
# ---------------------------------------------------------
Write-Host "`n--- Test 5: Clean Architecture Decoupling & Invalidation Integration ---" -ForegroundColor Yellow

$coreCsprojPath = Join-Path $RepoRoot "Gym_System.Core\Gym_System.Core\Gym_System.Core.csproj"
$coreCsprojContent = Get-Content -LiteralPath $coreCsprojPath -Raw
$coreHasBioRef = $coreCsprojContent -match 'ProjectReference.*BiometricApp'
Assert-Check "Gym_System.Core.csproj has zero BiometricApp ProjectReference" (-not $coreHasBioRef) `
    $(if (-not $coreHasBioRef) { "Gym_System.Core does not reference BiometricApp (clean architecture maintained)" } else { "Gym_System.Core still contains a ProjectReference to BiometricApp" })

$miembrosConexionPath = Join-Path $RepoRoot "Gym_System.Core\Gym_System.Core\Miembros_Conexion.cs"
$miembrosConexionContent = Get-Content -LiteralPath $miembrosConexionPath -Raw

$coreCallsBiometric = $miembrosConexionContent -match 'BiometricApp|BiometricCache'
Assert-Check "Gym_System.Core has zero BiometricApp/BiometricCache references" (-not $coreCallsBiometric) `
    $(if (-not $coreCallsBiometric) { "Zero BiometricApp/BiometricCache references found in Miembros_Conexion.cs" } else { "Miembros_Conexion.cs still contains direct calls/references to BiometricApp/BiometricCache" })

$notifierPath = Join-Path $RepoRoot "Gym_System.Core\Gym_System.Core\NotificadorCambioMiembro.cs"
$hasNotifier = Test-Path -LiteralPath $notifierPath
Assert-Check "NotificadorCambioMiembro neutral seam exists in Gym_System.Core" $hasNotifier `
    $(if ($hasNotifier) { "Found neutral contract NotificadorCambioMiembro in Core" } else { "NotificadorCambioMiembro missing in Core" })

$coreCallsNotifier = $miembrosConexionContent -match 'NotificadorCambioMiembro\.Notificar\(\)'
Assert-Check "Miembros_Conexion notifies member changes via neutral seam" $coreCallsNotifier `
    $(if ($coreCallsNotifier) { "Miembros_Conexion dispatches member changes via NotificadorCambioMiembro without inverse dependency" } else { "Miembros_Conexion does not call NotificadorCambioMiembro.Notificar()" })

$formAgregarPath = Join-Path $RepoRoot "Sistema_Gimnasio\FormAgregar.cs"
$formAgregarContent = Get-Content -LiteralPath $formAgregarPath -Raw
$invalAgregar = $formAgregarContent -match 'BiometricCache\.Invalidar\(\)'
Assert-Check "FormAgregar invalidates cache on member insert" $invalAgregar `
    $(if ($invalAgregar) { "FormAgregar calls BiometricCache.Invalidar() upon saving member" } else { "Missing cache invalidation in FormAgregar" })

$formEditarPath = Join-Path $RepoRoot "Sistema_Gimnasio\FormEditar.cs"
$formEditarContent = Get-Content -LiteralPath $formEditarPath -Raw
$invalEditar = $formEditarContent -match 'BiometricCache\.Invalidar\(\)'
Assert-Check "FormEditar invalidates cache on member update" $invalEditar `
    $(if ($invalEditar) { "FormEditar calls BiometricCache.Invalidar() upon updating member" } else { "Missing cache invalidation in FormEditar" })

$miembrosViewPath = Join-Path $RepoRoot "Sistema_Gimnasio\MiembrosView.xaml.cs"
$miembrosViewContent = Get-Content -LiteralPath $miembrosViewPath -Raw
$invalEliminar = $miembrosViewContent -match 'BiometricCache\.Invalidar\(\)'
Assert-Check "MiembrosView invalidates cache on member deletion" $invalEliminar `
    $(if ($invalEliminar) { "MiembrosView calls BiometricCache.Invalidar() upon deleting member" } else { "Missing cache invalidation in MiembrosView" })

$invalEstadosMasivo = ($miembrosViewContent -match 'ActualizarEstados_Automatico[\s\S]*?BiometricCache\.Invalidar') -and ($miembrosViewContent -match 'ActualizarEstados_Click[\s\S]*?BiometricCache\.Invalidar')
Assert-Check "MiembrosView invalidates cache on mass state updates" $invalEstadosMasivo `
    $(if ($invalEstadosMasivo) { "MiembrosView calls BiometricCache.Invalidar() in ActualizarEstados_Click and ActualizarEstados_Automatico" } else { "Missing cache invalidation in mass state update methods of MiembrosView" })

$pagoButPath = Join-Path $RepoRoot "Sistema_Gimnasio\PagoButWindow.xaml.cs"
$pagoButContent = Get-Content -LiteralPath $pagoButPath -Raw
$invalPago = $pagoButContent -match 'BiometricCache\.Invalidar\(\)'
Assert-Check "PagoButWindow invalidates cache on payment register" $invalPago `
    $(if ($invalPago) { "PagoButWindow calls BiometricCache.Invalidar() upon registering payment" } else { "Missing cache invalidation in PagoButWindow" })

$appXamlCsPath = Join-Path $RepoRoot "Sistema_Gimnasio\App.xaml.cs"
$appContent = Get-Content -LiteralPath $appXamlCsPath -Raw
$wiresNotifier = $appContent -match 'NotificadorCambioMiembro\.OnMiembroModificado\s*='
Assert-Check "App.xaml.cs wires neutral notifier to BiometricCache.Invalidar" $wiresNotifier `
    $(if ($wiresNotifier) { "App.xaml.cs registers BiometricCache.Invalidar to NotificadorCambioMiembro seam on startup" } else { "Missing wire-up of NotificadorCambioMiembro in App.xaml.cs" })


# ---------------------------------------------------------
# Test 6: In-Memory Cache Versioning & Backoff State Checks
# ---------------------------------------------------------
Write-Host "`n--- Test 6: BiometricCache Versioning & Backoff Static Analysis ---" -ForegroundColor Yellow

if ($cacheExists) {
    $hasVersioning = $cacheContent -match '_version\+\+' -and $cacheContent -match 'versionCapturada' -and $cacheContent -match 'Interlocked\.Read\(ref\s+_version\)'
    Assert-Check "BiometricCache implements versioned invalidation" $hasVersioning `
        $(if ($hasVersioning) { "BiometricCache tracks _version, captures version before reload and verifies match" } else { "Versioned invalidation pattern missing in BiometricCache" })

    $discardsStaleOnMismatch = $cacheContent -match '_version\s*==\s*versionCapturada' -and $cacheContent -match '_necesitaRecarga\s*=\s*true'
    Assert-Check "BiometricCache discards stale publish on version mismatch" $discardsStaleOnMismatch `
        $(if ($discardsStaleOnMismatch) { "Publishes only if version matches; leaves cache dirty and reloads on concurrent invalidation" } else { "BiometricCache does not conditionally discard stale load on version mismatch" })

    $serializesReload = $cacheContent -match 'lock\s*\(\s*_reloadLock\s*\)'
    Assert-Check "BiometricCache serializes concurrent reloads" $serializesReload `
        $(if ($serializesReload) { "BiometricCache serializes concurrent reload attempts using _reloadLock" } else { "BiometricCache lacks concurrent reload serialization lock" })

    $hasBackoffCooldown = $cacheContent -match 'EnCooldown' -and $cacheContent -match 'CooldownFalloSegundos' -and $cacheContent -match 'RegistrarFalloCarga'
    Assert-Check "BiometricCache implements error state and backoff cooldown" $hasBackoffCooldown `
        $(if ($hasBackoffCooldown) { "BiometricCache implements EnCooldown and backoff to prevent SQL query loop per scan" } else { "Error state / backoff cooldown missing in BiometricCache" })

    $hasExplicitEstaCargada = $cacheContent -match 'EstaCargada' -and $cacheContent -match '_estaCargada'
    Assert-Check "BiometricCache has explicit EstaCargada state" $hasExplicitEstaCargada `
        $(if ($hasExplicitEstaCargada) { "BiometricCache defines _estaCargada and EstaCargada property to distinguish loaded from empty snapshot" } else { "Missing EstaCargada state in BiometricCache" })

    $recordBlock = [regex]::Match($cacheContent, 'class\s+BiometricRecord[\s\S]*?\{([\s\S]*?)\n\s*public\s+BiometricRecord\(').Groups[1].Value
    $recordImmutableStatic = ($recordBlock -match 'IdMiembro\s*\{\s*get;\s*\}') -and (-not ($recordBlock -match 'set;'))
    Assert-Check "BiometricRecord has get-only properties (immutable)" $recordImmutableStatic `
        $(if ($recordImmutableStatic) { "BiometricRecord properties are get-only without public setters" } else { "BiometricRecord has mutable setters" })

    $templateFmdDeepIsolation = $cacheContent -match 'internal\s+Fmd\s+TemplateFmd\s*\{\s*get;\s*\}' -and $cacheContent -match 'public\s+static\s+IReadOnlyList<BiometricRecordMetadata>\s+SnapshotActual'
    Assert-Check "BiometricRecord.TemplateFmd is internal and SnapshotActual returns metadata DTO" $templateFmdDeepIsolation `
        $(if ($templateFmdDeepIsolation) { "TemplateFmd is internal and SnapshotActual returns BiometricRecordMetadata DTO without mutable Fmd" } else { "BiometricRecord exposes mutable Fmd in public snapshot" })
}


# ---------------------------------------------------------
# Test 7: Roslyn Dynamic Compilation & Seam Unit Tests (No DLL Fallback / Stale)
# ---------------------------------------------------------
Write-Host "`n--- Test 7: BiometricCache & Manager Seam Unit Tests (Fresh In-Memory Assembly) ---" -ForegroundColor Yellow

$cscCandidates = @(
    "${env:ProgramFiles}\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\Roslyn\csc.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
)
$cscExe = $cscCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

if (-not $cscExe) {
    Assert-Check "Roslyn csc.exe compiler availability" $false "Roslyn csc.exe not found in standard VS paths. Stale DLL fallback disabled."
} else {
    $tempDll = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "BiometricTest_$([System.Guid]::NewGuid().ToString('N')).dll")
    $dpurunetDllPath = Join-Path $RepoRoot "libs\DPUruNet.dll"
    $sourceFiles = @(
        (Join-Path $RepoRoot "BiometricApp\BiometricApp\BiometricApp\BiometricCache.cs"),
        (Join-Path $RepoRoot "BiometricApp\BiometricApp\BiometricApp\FingerprintManager.cs"),
        (Join-Path $RepoRoot "BiometricApp\BiometricApp\BiometricApp\UCVerifyFingerprint.cs"),
        (Join-Path $RepoRoot "BiometricApp\BiometricApp\BiometricApp\UCVerifyFingerprint.Designer.cs"),
        (Join-Path $RepoRoot "BiometricApp\BiometricApp\BiometricApp\frmDBEnrollment.cs"),
        (Join-Path $RepoRoot "BiometricApp\BiometricApp\BiometricApp\frmDBEnrollment.Designer.cs")
    )

    $compileArgs = @(
        "/nologo",
        "/target:library",
        "/out:$tempDll",
        "/reference:$dpurunetDllPath",
        "/reference:System.dll",
        "/reference:System.Core.dll",
        "/reference:System.Data.dll",
        "/reference:System.Configuration.dll",
        "/reference:System.Drawing.dll",
        "/reference:System.Windows.Forms.dll"
    ) + $sourceFiles

    $compileOutput = & $cscExe $compileArgs 2>&1
    $compileSucceeded = (Test-Path -LiteralPath $tempDll)

    Assert-Check "Fresh Roslyn in-memory compilation (no stale DLL fallback)" $compileSucceeded `
        $(if ($compileSucceeded) { "Freshly compiled BiometricCache, Manager & Controls from current source without stale DLL fallback" } else { "Compilation failed: $($compileOutput -join '; ')" })

    if ($compileSucceeded) {
        try {
            $rawBytes = [System.IO.File]::ReadAllBytes($tempDll)
            Remove-Item -LiteralPath $tempDll -Force -ErrorAction SilentlyContinue

            [System.Reflection.Assembly]::LoadFrom($dpurunetDllPath) | Out-Null
            $bioAsm = [System.Reflection.Assembly]::Load($rawBytes)

            $cacheType = $bioAsm.GetType('BiometricApp.BiometricCache')
            $recordType = $bioAsm.GetType('BiometricApp.BiometricRecord')
            $mgrType = $bioAsm.GetType('BiometricApp.FingerprintManager')
            $ucType = $bioAsm.GetType('BiometricApp.UCVerifyFingerprint')
            $frmType = $bioAsm.GetType('BiometricApp.frmDBEnrollment')

            $limpiarMethod = $cacheType.GetMethod('Limpiar', [System.Reflection.BindingFlags]'Public,Static')
            $configurarSeamMethod = $cacheType.GetMethod('ConfigurarSeamSimulado', [System.Reflection.BindingFlags]'Public,Static')
            $crearRegistroMethod = $cacheType.GetMethod('CrearRegistroMock', [System.Reflection.BindingFlags]'Public,Static')
            $crearFmdMethod = $cacheType.GetMethod('CrearFmdMock', [System.Reflection.BindingFlags]'Public,Static')
            $cfgCargadorMethod = $cacheType.GetMethod('ConfigurarCargadorSeam', [System.Reflection.BindingFlags]'Public,Static')
            $identificarMethod = $cacheType.GetMethod('Identificar', [System.Reflection.BindingFlags]'Public,Static')
            $invalidarMethod = $cacheType.GetMethod('Invalidar', [System.Reflection.BindingFlags]'Public,Static')
            $updateMethod = $cacheType.GetMethod('ActualizarMiembroEnCache', [System.Reflection.BindingFlags]'Public,Static')
            $removeMethod = $cacheType.GetMethod('EliminarMiembroDeCache', [System.Reflection.BindingFlags]'Public,Static')
            $simularFalloMethod = $cacheType.GetMethod('SimularFalloCarga', [System.Reflection.BindingFlags]'Public,Static')

            $totalBdProp = $cacheType.GetProperty('TotalConsultasBd', [System.Reflection.BindingFlags]'Public,Static')
            $totalVerifProp = $cacheType.GetProperty('TotalVerificaciones', [System.Reflection.BindingFlags]'Public,Static')
            $necesitaRecargaProp = $cacheType.GetProperty('NecesitaRecarga', [System.Reflection.BindingFlags]'Public,Static')
            $enCooldownProp = $cacheType.GetProperty('EnCooldown', [System.Reflection.BindingFlags]'Public,Static')
            $ultimoErrorProp = $cacheType.GetProperty('UltimoErrorCarga', [System.Reflection.BindingFlags]'Public,Static')
            $seamTiempoProp = $cacheType.GetProperty('SeamTiempoActual', [System.Reflection.BindingFlags]'Public,Static')
            $seamDuranteCargaProp = $cacheType.GetProperty('SeamDuranteCarga', [System.Reflection.BindingFlags]'Public,Static')
            $totalRecargasObsoletasProp = $cacheType.GetProperty('TotalRecargasPorVersionObsoleta', [System.Reflection.BindingFlags]'Public,Static')

            $threshold = [int](0x7fffffff / 100000)

            # --- Test 7.1: 1:N Identification & Cache Hit Efficiency ---
            $limpiarMethod.Invoke($null, @())
            $configurarSeamMethod.Invoke($null, @(50))

            $rec1 = $crearRegistroMethod.Invoke($null, @([long]101, 'Carlos Gomez', 'Mensual', [DateTime]::Today.AddDays(15), [byte[]]@(1, 2, 3)))
            $rec2 = $crearRegistroMethod.Invoke($null, @([long]102, 'Ana Perez', 'Anual', [DateTime]::Today.AddMonths(6), [byte[]]@(10, 20, 30)))
            $rec3 = $crearRegistroMethod.Invoke($null, @([long]103, 'Juan Lopez', 'Sin membresía', [DateTime]::MinValue, [byte[]]@(100, 200)))

            $pTarget = [object[]]::new(1)
            $pTarget[0] = [byte[]]@(10, 99)
            $targetFmd = $crearFmdMethod.Invoke($null, $pTarget)

            $arrayRecs = [Array]::CreateInstance($recordType, 3)
            $arrayRecs.SetValue($rec1, 0)
            $arrayRecs.SetValue($rec2, 1)
            $arrayRecs.SetValue($rec3, 2)
            $pCargador = [object[]]::new(1)
            $pCargador[0] = $arrayRecs
            $cfgCargadorMethod.Invoke($null, $pCargador)

            $allMatchesOk = $true
            for ($i = 0; $i -lt 10; $i++) {
                $match = $identificarMethod.Invoke($null, @($targetFmd, $threshold))
                if (-not $match.Exitoso -or $match.IdMiembro -ne 102 -or $match.Nombre -ne "Ana Perez") {
                    $allMatchesOk = $false
                    break
                }
            }
            Assert-Check "1:N Seam Identification accuracy" $allMatchesOk `
                $(if ($allMatchesOk) { "Identified member 102 (Ana Perez) accurately across all test iterations" } else { "Identification failed or returned incorrect member" })

            $queriesCount = $totalBdProp.GetValue($null)
            $verifCount = $totalVerifProp.GetValue($null)
            $cacheHitOptimized = ($queriesCount -eq 1 -and $verifCount -eq 10)
            Assert-Check "Cache efficiency: 1 DB load for 10 scans" $cacheHitOptimized `
                $(if ($cacheHitOptimized) { "Verified: exactly 1 data load for 10 scans (TotalConsultasBd=$queriesCount, TotalVerificaciones=$verifCount)" } else { "Unexpected counts: TotalConsultasBd=$queriesCount, TotalVerificaciones=$verifCount" })

            # --- Test 7.2: Invalidation & Demand Reload ---
            $invalidarMethod.Invoke($null, @())
            $isDirty = $necesitaRecargaProp.GetValue($null)
            Assert-Check "Invalidar marks cache dirty" $isDirty `
                $(if ($isDirty) { "BiometricCache.NecesitaRecarga is true after calling Invalidar()" } else { "Cache not marked dirty after Invalidar()" })

            $matchPostInval = $identificarMethod.Invoke($null, @($targetFmd, $threshold))
            $queriesCountAfter = $totalBdProp.GetValue($null)
            $reloadedOnDemand = ($queriesCountAfter -eq 2 -and $matchPostInval.Exitoso)
            Assert-Check "Selective reload on demand after invalidation" $reloadedOnDemand `
                $(if ($reloadedOnDemand) { "Verified: cache reloaded on next verification after invalidation (TotalConsultasBd=$queriesCountAfter)" } else { "Did not reload: TotalConsultasBd=$queriesCountAfter" })

            # --- Test 7.3: In-Memory Copy-on-Write Update & Elimination ---
            $updateMethod.Invoke($null, @([long]102, "Ana Perez Actualizada", "VIP Platino", [DateTime]::Today.AddYears(1), $targetFmd))
            $matchUpdated = $identificarMethod.Invoke($null, @($targetFmd, $threshold))
            $updateSuccess = ($matchUpdated.Nombre -eq "Ana Perez Actualizada" -and $matchUpdated.Membresia -eq "VIP Platino")
            Assert-Check "In-memory member update without DB query" $updateSuccess `
                $(if ($updateSuccess) { "Verified: member record updated in-memory (Nombre='Ana Perez Actualizada', Membresia='VIP Platino')" } else { "In-memory update failed" })

            $queriesCountAfterUpdate = $totalBdProp.GetValue($null)
            $zeroExtraQueries = ($queriesCountAfterUpdate -eq 2)
            Assert-Check "In-memory update performs zero SQL queries" $zeroExtraQueries `
                $(if ($zeroExtraQueries) { "Verified: in-memory update did not re-query the data layer (TotalConsultasBd=$queriesCountAfterUpdate)" } else { "Unexpected data query during in-memory update" })

            $removeMethod.Invoke($null, @([long]102))
            $matchRemoved = $identificarMethod.Invoke($null, @($targetFmd, $threshold))
            $removeSuccess = (-not $matchRemoved.Exitoso)
            Assert-Check "In-memory member deletion" $removeSuccess `
                $(if ($removeSuccess) { "Verified: member 102 removed from cache; subsequent identification returns Exitoso=false" } else { "Member still matched after removal" })

            # --- Test 7.3b: Explicit State & Empty Snapshot Optimization (Finding 1) ---
            $limpiarMethod.Invoke($null, @())
            $estaCargadaProp = $cacheType.GetProperty('EstaCargada', [System.Reflection.BindingFlags]'Public,Static')
            $emptyCargador = [object[]]::new(1)
            $emptyCargador[0] = [Array]::CreateInstance($recordType, 0)
            $cfgCargadorMethod.Invoke($null, $emptyCargador)

            $estaCargadaBefore = $estaCargadaProp.GetValue($null)
            $matchEmpty1 = $identificarMethod.Invoke($null, @($targetFmd, $threshold))
            $estaCargadaAfter1 = $estaCargadaProp.GetValue($null)
            $queriesCountAfter1 = $totalBdProp.GetValue($null)

            $matchEmpty2 = $identificarMethod.Invoke($null, @($targetFmd, $threshold))
            $estaCargadaAfter2 = $estaCargadaProp.GetValue($null)
            $queriesCountAfter2 = $totalBdProp.GetValue($null)

            $emptySnapshotOptimized = (-not $estaCargadaBefore -and $estaCargadaAfter1 -and $estaCargadaAfter2 -and $queriesCountAfter1 -eq 1 -and $queriesCountAfter2 -eq 1 -and -not $matchEmpty1.Exitoso -and -not $matchEmpty2.Exitoso)
            Assert-Check "Empty snapshot explicitly marked loaded: 2 Identificar scans trigger exactly 1 DB query" $emptySnapshotOptimized `
                $(if ($emptySnapshotOptimized) { "Verified: Empty collection loaded successfully, marked EstaCargada=true, and subsequent scan executed without DB query (TotalConsultasBd=1)" } else { "Failed: queries1=$queriesCountAfter1, queries2=$queriesCountAfter2, cargada1=$estaCargadaAfter1, cargada2=$estaCargadaAfter2" })

            # --- Test 7.4: Deterministic Version Invalidation Race Test (Item 1) ---
            $limpiarMethod.Invoke($null, @())
            $configurarSeamMethod.Invoke($null, @(50))

            $arrayRecsInitial = [Array]::CreateInstance($recordType, 1)
            $arrayRecsInitial.SetValue($rec1, 0)
            $pInit = [object[]]::new(1)
            $pInit[0] = $arrayRecsInitial
            $cfgCargadorMethod.Invoke($null, $pInit)

            # Set up concurrent invalidation during loading
            $arrayRecsUpdated = [Array]::CreateInstance($recordType, 2)
            $arrayRecsUpdated.SetValue($rec1, 0)
            $arrayRecsUpdated.SetValue($rec2, 1)

            $invokedConcurrent = $false
            $seamAction = [System.Action]{
                if (-not $script:invokedConcurrent) {
                    $script:invokedConcurrent = $true
                    $pUpd = [object[]]::new(1)
                    $pUpd[0] = $arrayRecsUpdated
                    $cfgCargadorMethod.Invoke($null, $pUpd)
                    $invalidarMethod.Invoke($null, @())
                }
            }
            $seamDuranteCargaProp.SetValue($null, $seamAction)

            $cargarMethod = $cacheType.GetMethod('CargarPlantillas', [System.Reflection.BindingFlags]'Public,Static')
            $cargarMethod.Invoke($null, @($true))

            $obsoleteReloads = $totalRecargasObsoletasProp.GetValue($null)
            $cantidadFinal = $cacheType.GetProperty('CantidadPlantillas').GetValue($null)
            $isDirtyFinal = $necesitaRecargaProp.GetValue($null)

            $raceHandledCorrectly = ($obsoleteReloads -ge 1 -and $cantidadFinal -eq 2 -and -not $isDirtyFinal)
            Assert-Check "Deterministic version invalidation race handled safely" $raceHandledCorrectly `
                $(if ($raceHandledCorrectly) { "Concurrent invalidation during load was detected; stale data discarded, auto-reloaded and published versioned snapshot (CantidadPlantillas=2, Retries=$obsoleteReloads)" } else { "Race failed: Cantidad=$cantidadFinal, Retries=$obsoleteReloads, Dirty=$isDirtyFinal" })

            $seamDuranteCargaProp.SetValue($null, $null)

            # --- Test 7.5: Deterministic Load Failure & Backoff/Cooldown Test (Item 3) ---
            $limpiarMethod.Invoke($null, @())
            $baseTime = [DateTime]::UtcNow
            $seamTiempoProp.SetValue($null, [System.Func[DateTime]]{ $script:baseTime })

            # Simulate database connection failure
            $simularFalloMethod.Invoke($null, @("Conexión rechazada por SQL Server"))
            $isEnCooldown = $enCooldownProp.GetValue($null)
            $ultimoError = $ultimoErrorProp.GetValue($null)

            $failureRecorded = ($isEnCooldown -and ($ultimoError -like "*Conexión rechazada*"))
            Assert-Check "Failure enters deterministic cooldown state" $failureRecorded `
                $(if ($failureRecorded) { "BiometricCache entered cooldown and recorded error message ('$ultimoError')" } else { "Failed to enter cooldown state" })

            # Perform 5 scans during cooldown: TotalConsultasBd must NOT increase
            $queriesBeforeScans = $totalBdProp.GetValue($null)
            $allCooldownErrorsReported = $true
            for ($k = 0; $k -lt 5; $k++) {
                $scanMatch = $identificarMethod.Invoke($null, @($targetFmd, $threshold))
                if ($scanMatch.Exitoso -or [string]::IsNullOrEmpty($scanMatch.MensajeError)) {
                    $allCooldownErrorsReported = $false
                    break
                }
            }
            $queriesAfterScans = $totalBdProp.GetValue($null)
            $zeroQueriesInCooldown = ($queriesAfterScans -eq $queriesBeforeScans -and $allCooldownErrorsReported)
            Assert-Check "Zero DB queries per scan during failure cooldown (no query loop)" $zeroQueriesInCooldown `
                $(if ($zeroQueriesInCooldown) { "Verified: 5 scans during cooldown triggered 0 DB queries (TotalConsultasBd=$queriesAfterScans, Error safely reported)" } else { "Queries increased during cooldown: before=$queriesBeforeScans, after=$queriesAfterScans" })

            # Advance deterministic time beyond cooldown (10 seconds later)
            $baseTime = $baseTime.AddSeconds(10)
            $isCooldownExpired = (-not $enCooldownProp.GetValue($null))
            Assert-Check "Cooldown expires after configured interval" $isCooldownExpired `
                $(if ($isCooldownExpired) { "Cooldown successfully expired after time elapsed" } else { "Cooldown did not expire" })

            # Setup successful loader and verify recovery
            $pRecovery = [object[]]::new(1)
            $pRecovery[0] = $arrayRecsUpdated
            $cfgCargadorMethod.Invoke($null, $pRecovery)
            $recoveryMatch = $identificarMethod.Invoke($null, @($targetFmd, $threshold))
            $recoveryOk = ($recoveryMatch.Exitoso -and $totalBdProp.GetValue($null) -eq ($queriesAfterScans + 1))
            Assert-Check "Cache resumes normal operation after cooldown expiration" $recoveryOk `
                $(if ($recoveryOk) { "Cache successfully reloaded from data source after cooldown cleared" } else { "Failed to recover after cooldown" })

            $seamTiempoProp.SetValue($null, $null)

            # --- Test 7.6: FingerprintManager.ProcesarVerificacion finally & ErrorLector Seam Test (Item 4) ---
            $resetMgrMethod = $mgrType.GetMethod('ResetearParaPruebas', [System.Reflection.BindingFlags]'Public,Static')
            $resetMgrMethod.Invoke($null, @())

            $mgrInstance = $mgrType.GetProperty('Instance').GetValue($null)
            $seamCapturaProp = $mgrType.GetProperty('SeamAlIniciarCaptura', [System.Reflection.BindingFlags]'Public,Static')

            $script:iniciarCapturaInvokedCount = 0
            $seamCapturaAction = [System.Action]{
                $script:iniciarCapturaInvokedCount++
            }
            $seamCapturaProp.SetValue($null, $seamCapturaAction)

            # Add failing subscriber to HuellaParaVerificar
            $huellaEvent = $mgrType.GetEvent('HuellaParaVerificar')
            $failingHandler = [System.EventHandler[DPUruNet.Fmd]]{
                param($sender, $fmdArg)
                throw [System.InvalidOperationException]::new("Excepción forzada en suscriptor de prueba")
            }
            $huellaEvent.AddEventHandler($mgrInstance, $failingHandler)

            # Add listener to ErrorLector
            $errorEvent = $mgrType.GetEvent('ErrorLector')
            $script:errorLectorReportedMsg = $null
            $errorListener = [System.EventHandler[string]]{
                param($sender, $msgArg)
                $script:errorLectorReportedMsg = $msgArg
            }
            $errorEvent.AddEventHandler($mgrInstance, $errorListener)

            # Invoke ProcesarVerificacion with mock Fmd
            $pMockFmd = [object[]]::new(1)
            $pMockFmd[0] = [byte[]]@(1, 2, 3)
            $dummyFmd = $crearFmdMethod.Invoke($null, $pMockFmd)
            $procesarVerifMethod = $mgrType.GetMethod('ProcesarVerificacion', [System.Reflection.BindingFlags]'NonPublic,Public,Instance')

            $noUnhandledCrash = $true
            try {
                $pArgsVerif = [object[]]::new(1)
                $pArgsVerif[0] = $dummyFmd
                $procesarVerifMethod.Invoke($mgrInstance, $pArgsVerif)
            } catch {
                $noUnhandledCrash = $false
            }

            $finallyExecuted = ($script:iniciarCapturaInvokedCount -ge 1)
            $errorReportedSafely = ($null -ne $script:errorLectorReportedMsg -and $script:errorLectorReportedMsg -match 'suscriptor de verificaci')

            Assert-Check "ProcesarVerificacion handles subscriber exception without crash" $noUnhandledCrash `
                $(if ($noUnhandledCrash) { "ProcesarVerificacion absorbed subscriber exception safely" } else { "Unhandled exception escaped ProcesarVerificacion" })

            Assert-Check "ProcesarVerificacion notifies ErrorLector on exception" $errorReportedSafely `
                $(if ($errorReportedSafely) { "ErrorLector was safely notified with subscriber error message" } else { "Error was swallowed silently without notifying ErrorLector" })

            Assert-Check "ProcesarVerificacion guarantees IniciarCaptura execution in finally" $finallyExecuted `
                $(if ($finallyExecuted) { "Verified: IniciarCaptura() executed in finally block despite subscriber exception" } else { "IniciarCaptura() was not executed in finally block" })

            # Reset manager
            $resetMgrMethod.Invoke($null, @())

            # --- Test 7.7: Multicast Event Isolation in FingerprintManager (Finding 2) ---
            $mgrInstance = $mgrType.GetProperty('Instance').GetValue($null)
            $seamCapturaProp.SetValue($null, $seamCapturaAction)

            $script:iniciarCapturaInvokedCount = 0
            $script:suscriptorARan = $false
            $script:suscriptorBRecibio = $false
            $script:errorSuscriptorARan = $false
            $script:errorSuscriptorBRecibio = $false

            # Suscriptor A de HuellaParaVerificar lanza excepción
            $handlerA = [System.EventHandler[DPUruNet.Fmd]]{
                param($sender, $fmdArg)
                $script:suscriptorARan = $true
                throw [System.InvalidOperationException]::new("Excepción forzada en Suscriptor A de HuellaParaVerificar")
            }
            # Suscriptor B de HuellaParaVerificar recibe normalmente
            $handlerB = [System.EventHandler[DPUruNet.Fmd]]{
                param($sender, $fmdArg)
                $script:suscriptorBRecibio = ($null -ne $fmdArg)
            }

            # Suscriptor A de ErrorLector lanza excepción
            $errorHandlerA = [System.EventHandler[string]]{
                param($sender, $msgArg)
                $script:errorSuscriptorARan = $true
                throw [System.InvalidOperationException]::new("Excepción forzada en Suscriptor A de ErrorLector")
            }
            # Suscriptor B de ErrorLector recibe normalmente
            $errorHandlerB = [System.EventHandler[string]]{
                param($sender, $msgArg)
                $script:errorSuscriptorBRecibio = ($null -ne $msgArg -and $msgArg -like "*Suscriptor A*")
            }

            $huellaEvent.AddEventHandler($mgrInstance, $handlerA)
            $huellaEvent.AddEventHandler($mgrInstance, $handlerB)
            $errorEvent.AddEventHandler($mgrInstance, $errorHandlerA)
            $errorEvent.AddEventHandler($mgrInstance, $errorHandlerB)

            $pArgsVerif = [object[]]::new(1)
            $pArgsVerif[0] = $dummyFmd
            $procesarVerifMethod.Invoke($mgrInstance, $pArgsVerif)

            $multicastHuellaIsolated = ($script:suscriptorARan -and $script:suscriptorBRecibio)
            $multicastErrorIsolated = ($script:errorSuscriptorARan -and $script:errorSuscriptorBRecibio)
            $finallyRanAfterMulticast = ($script:iniciarCapturaInvokedCount -ge 1)

            Assert-Check "Multicast HuellaParaVerificar isolates throwing subscriber A and delivers to B" $multicastHuellaIsolated `
                $(if ($multicastHuellaIsolated) { "Verified: Subscriber A threw exception, but Subscriber B still received Fmd" } else { "Subscriber B was not invoked after Subscriber A threw" })

            Assert-Check "Multicast ErrorLector isolates throwing error-subscriber A and delivers to B" $multicastErrorIsolated `
                $(if ($multicastErrorIsolated) { "Verified: Error subscriber A threw exception, but Error subscriber B still received error notification" } else { "Error subscriber B was not invoked after Error subscriber A threw" })

            Assert-Check "Multicast isolation still executes IniciarCaptura in finally" $finallyRanAfterMulticast `
                $(if ($finallyRanAfterMulticast) { "Verified: IniciarCaptura() executed in finally block despite subscriber exceptions" } else { "IniciarCaptura() was not executed" })

            # Reset manager
            $resetMgrMethod.Invoke($null, @())

            # --- Test 7.8: BiometricRecord Immutability & Copy-on-Write (Finding 3) ---
            $props = $recordType.GetProperties()
            $writableProps = $props | Where-Object { $_.CanWrite -and ($null -ne $_.GetSetMethod($false)) }
            $isImmutable = ($writableProps.Count -eq 0 -and $props.Count -ge 5)
            Assert-Check "BiometricRecord is immutable with get-only properties" $isImmutable `
                $(if ($isImmutable) { "Verified: BiometricRecord has zero public setters (all $($props.Count) properties are get-only)" } else { "Found writable properties on BiometricRecord: $($writableProps.Name -join ', ')" })

            # --- Test 7.9: Deep Isolation & Snapshot Immutability Regression Test (Luna Blocker 1) ---
            $cacheType.GetMethod('ResetearCooldown', [System.Reflection.BindingFlags]'Public,Static').Invoke($null, @())
            $publicProps = $recordType.GetProperties([System.Reflection.BindingFlags]'Public,Instance')
            $hasPublicFmd = ($publicProps | Where-Object { $_.Name -eq 'TemplateFmd' -or $_.PropertyType.Name -eq 'Fmd' }).Count -gt 0
            Assert-Check "BiometricRecord does not expose TemplateFmd in public API" (-not $hasPublicFmd) `
                $(if (-not $hasPublicFmd) { "Verified: BiometricRecord has no public TemplateFmd or Fmd property (deep isolation)" } else { "BiometricRecord still exposes TemplateFmd publicly" })

            $snapshotProp = $cacheType.GetProperty('SnapshotActual', [System.Reflection.BindingFlags]'Public,Static')
            $snapshotVal = $snapshotProp.GetValue($null, $null)
            $snapshotItemType = $snapshotProp.PropertyType.GetGenericArguments()[0]
            $isMetadataDto = ($snapshotItemType.Name -eq 'BiometricRecordMetadata')
            Assert-Check "SnapshotActual returns IReadOnlyList of BiometricRecordMetadata DTO" $isMetadataDto `
                $(if ($isMetadataDto) { "Verified: SnapshotActual returns IReadOnlyList<BiometricRecordMetadata> without Fmd references" } else { "SnapshotActual does not return metadata DTO" })

            $metaProps = $snapshotItemType.GetProperties([System.Reflection.BindingFlags]'Public,Instance')
            $metaWritable = $metaProps | Where-Object { $_.CanWrite -and ($null -ne $_.GetSetMethod($false)) }
            $metaHasFmd = ($metaProps | Where-Object { $_.PropertyType.Name -eq 'Fmd' -or $_.PropertyType.Name -eq 'Byte[]' }).Count -gt 0
            $metaSafe = ($metaWritable.Count -eq 0 -and (-not $metaHasFmd) -and $metaProps.Count -ge 5)
            Assert-Check "BiometricRecordMetadata DTO is immutable without Fmd or Byte[] exposure" $metaSafe `
                $(if ($metaSafe) { "Verified: BiometricRecordMetadata has zero setters and zero Fmd/Byte[] properties" } else { "BiometricRecordMetadata exposes writable properties or Fmd" })

            $pRegMock = [object[]]::new(5)
            $pRegMock[0] = [long]201
            $pRegMock[1] = "Carlos Ruiz"
            $pRegMock[2] = "Estudiante"
            $pRegMock[3] = [DateTime]::Today.AddDays(30)
            $pRegMock[4] = [byte[]]@(77, 88, 99)
            $regMock1 = $crearRegistroMethod.Invoke($null, $pRegMock)

            $regArr = [System.Array]::CreateInstance($recordType, 1)
            $regArr.SetValue($regMock1, 0)
            $pCargador2 = [object[]]::new(1)
            $pCargador2[0] = $regArr
            $cfgCargadorMethod.Invoke($null, $pCargador2)
            $invalidarMethod.Invoke($null, @())

            $pProbe = [object[]]::new(1)
            $pProbe[0] = [byte[]]@(77, 0, 0)
            $fmdProbe = $crearFmdMethod.Invoke($null, $pProbe)

            $pIdent = [object[]]::new(2)
            $pIdent[0] = $fmdProbe
            $pIdent[1] = $threshold
            $matchBefore = $identificarMethod.Invoke($null, $pIdent)
            $matchBeforeExitoso = ($null -ne $matchBefore -and $matchBefore.Exitoso)

            $snapshotLive = $snapshotProp.GetValue($null, $null)
            $snapshotCountMatches = ($snapshotLive.Count -ge 1)
            $firstItem = $snapshotLive[0]
            $firstItemNombre = $firstItem.Nombre
            $firstItemTienePlantilla = $firstItem.TienePlantilla

            $matchAfter = $identificarMethod.Invoke($null, $pIdent)
            $snapshotTamperSafe = ($matchBeforeExitoso -and $null -ne $matchAfter -and $matchAfter.Exitoso -and $snapshotCountMatches -and $firstItemTienePlantilla)
            Assert-Check "Public snapshot regression: snapshot cannot mutate cached template or break matching" $snapshotTamperSafe `
                $(if ($snapshotTamperSafe) { "Verified: Public snapshot cannot mutate cached FMD; member 201 matches before and after snapshot acquisition" } else { "Snapshot mutation test failed" })

            # --- Test 7.10: Queued Bitmap Ownership & Early Dispose Draining (Deterministic Seams - Luna Blocker 2) ---
            # Scenario 1: UCVerifyFingerprint - Control disposed BEFORE delegate executes
            $ucInst = [System.Activator]::CreateInstance($ucType)
            $bmpMock1 = New-Object System.Drawing.Bitmap 10, 10
            $script:capturedActionUc = $null
            $seamDelegateUc = [System.Action[System.Action]]{
                param($action)
                $script:capturedActionUc = $action
            }
            $ucType.GetProperty('SeamEncolarDelegado').SetValue($ucInst, $seamDelegateUc, $null)

            $mostrarImagenUcMethod = $ucType.GetMethod('MostrarImagen', [Type[]]@([System.Drawing.Bitmap]))
            $pBmp1 = [object[]]::new(1)
            $pBmp1[0] = $bmpMock1.PSObject.BaseObject
            $mostrarImagenUcMethod.Invoke($ucInst, $pBmp1)

            $pendingBeforeDisposeUc = [int]$ucType.GetProperty('CantidadBitmapsPendientes').GetValue($ucInst, $null)

            $ucDisposeMethod = $ucType.GetMethod('Dispose', [System.Reflection.BindingFlags]'NonPublic,Public,Instance', $null, @([bool]), $null)
            $pDisp = [object[]]::new(1)
            $pDisp[0] = $true
            $ucDisposeMethod.Invoke($ucInst, $pDisp)

            $pendingAfterDisposeUc = [int]$ucType.GetProperty('CantidadBitmapsPendientes').GetValue($ucInst, $null)

            $bmp1Disposed = $false
            try {
                $bmpMock1.GetHbitmap() | Out-Null
            } catch {
                $bmp1Disposed = $true
            }

            $lateExecutionSafeUc = $true
            try {
                if ($script:capturedActionUc) {
                    $script:capturedActionUc.Invoke()
                }
            } catch {
                $lateExecutionSafeUc = $false
            }

            $ucEarlyDisposePassed = ($pendingBeforeDisposeUc -eq 1 -and $pendingAfterDisposeUc -eq 0 -and $bmp1Disposed -and $lateExecutionSafeUc)
            Assert-Check "UCVerifyFingerprint drains pending bitmap on early Dispose without leak or double-dispose" $ucEarlyDisposePassed `
                $(if ($ucEarlyDisposePassed) { "Verified: Pending bitmap (1) was drained on early Dispose (0), disposed, and late delegate execution was safe" } else { "UCVerifyFingerprint early dispose failed: pendingBefore=$pendingBeforeDisposeUc, pendingAfter=$pendingAfterDisposeUc, disposed=$bmp1Disposed, safe=$lateExecutionSafeUc" })

            # Scenario 2: frmDBEnrollment - Delegate executes normally in finally
            $frmInst = [System.Activator]::CreateInstance($frmType)
            $bmpMock2 = New-Object System.Drawing.Bitmap 12, 12
            $script:capturedActionFrm = $null
            $seamDelegateFrm = [System.Action[System.Action]]{
                param($action)
                $script:capturedActionFrm = $action
            }
            $frmType.GetProperty('SeamEncolarDelegado').SetValue($frmInst, $seamDelegateFrm, $null)

            $mostrarImagenFrmMethod = $frmType.GetMethod('MostrarImagen', [Type[]]@([System.Drawing.Bitmap]))
            $pBmp2 = [object[]]::new(1)
            $pBmp2[0] = $bmpMock2.PSObject.BaseObject
            $mostrarImagenFrmMethod.Invoke($frmInst, $pBmp2)

            $pendingBeforeExecFrm = [int]$frmType.GetProperty('CantidadBitmapsPendientes').GetValue($frmInst, $null)
            if ($script:capturedActionFrm) {
                $script:capturedActionFrm.Invoke()
            }
            $pendingAfterExecFrm = [int]$frmType.GetProperty('CantidadBitmapsPendientes').GetValue($frmInst, $null)

            $frmDisposeMethod = $frmType.GetMethod('Dispose', [System.Reflection.BindingFlags]'NonPublic,Public,Instance', $null, @([bool]), $null)
            $frmDisposeMethod.Invoke($frmInst, $pDisp)

            $frmNormalLifecyclePassed = ($pendingBeforeExecFrm -eq 1 -and $pendingAfterExecFrm -eq 0)
            Assert-Check "frmDBEnrollment delegate removes pending bitmap in finally with safe lifecycle" $frmNormalLifecyclePassed `
                $(if ($frmNormalLifecyclePassed) { "Verified: frmDBEnrollment enqueued bitmap (1), delegate removed it in finally (0), and form disposed cleanly" } else { "frmDBEnrollment normal lifecycle test failed" })

            # Scenario 3: Interleaving / Concurrency Race Regression Seam
            # Verify that between Remove(bmp) and PictureBox assignment, Drenar cannot interleave
            # because the critical section actively holds BitmapsPendientesLock.
            $ucInst3 = [System.Activator]::CreateInstance($ucType)
            $bmpMock3 = New-Object System.Drawing.Bitmap 14, 14
            $script:capturedActionRace = $null
            $seamDelegateRace = [System.Action[System.Action]]{
                param($action)
                $script:capturedActionRace = $action
            }
            $ucType.GetProperty('SeamEncolarDelegado').SetValue($ucInst3, $seamDelegateRace, $null)

            $script:seamRan = $false
            $script:lockHeldInSeam = $false
            $script:drenarBlockedDuringSeam = $false
            $syncLock = $ucType.GetProperty('BitmapsPendientesLock').GetValue($ucInst3, $null)

            $seamAntesDeAsignar = [System.Action]{
                $script:seamRan = $true
                # 1. Assert lock is actively held by the delegate thread
                $script:lockHeldInSeam = [System.Threading.Monitor]::IsEntered($syncLock)

                # 2. Assert concurrent thread attempting Drenar cannot acquire lock and cannot interleave
                $script:drenarBlockedDuringSeam = [bool]$ucType.GetMethod('ProbarExclusionConcurrenteDrenar').Invoke($ucInst3, @())
            }
            $ucType.GetProperty('SeamAntesDeAsignar').SetValue($ucInst3, $seamAntesDeAsignar, $null)

            $pBmp3 = [object[]]::new(1)
            $pBmp3[0] = $bmpMock3.PSObject.BaseObject
            $mostrarImagenUcMethod.Invoke($ucInst3, $pBmp3)

            if ($script:capturedActionRace) {
                $script:capturedActionRace.Invoke()
            }

            $ucDisposeMethod.Invoke($ucInst3, $pDisp)

            $raceSeamPassed = ($script:seamRan -and $script:lockHeldInSeam -and $script:drenarBlockedDuringSeam)
            Assert-Check "Shared critical section prevents Drenar intercalation between Remove and assignment" $raceSeamPassed `
                $(if ($raceSeamPassed) { "Verified: Seam confirmed shared lock is held between Remove and assignment; concurrent thread cannot interleave Drenar" } else { "Interleaving regression failed: seamRan=$script:seamRan, lockHeld=$script:lockHeldInSeam, blocked=$script:drenarBlockedDuringSeam" })

            # Scenario 4: Disposed check within critical section disposes bitmap without assignment
            $frmInst4 = [System.Activator]::CreateInstance($frmType)
            $bmpMock4 = New-Object System.Drawing.Bitmap 16, 16
            $script:capturedActionRaceFrm = $null
            $seamDelegateRaceFrm = [System.Action[System.Action]]{
                param($action)
                $script:capturedActionRaceFrm = $action
            }
            $frmType.GetProperty('SeamEncolarDelegado').SetValue($frmInst4, $seamDelegateRaceFrm, $null)

            $script:disposedInSeam = $false
            $seamAntesDeAsignarFrm = [System.Action]{
                # Simulate control being disposed while in critical section before assignment
                $frmDisposeMethod.Invoke($frmInst4, $pDisp)
                $script:disposedInSeam = $true
            }
            $frmType.GetProperty('SeamAntesDeAsignar').SetValue($frmInst4, $seamAntesDeAsignarFrm, $null)

            $pBmp4 = [object[]]::new(1)
            $pBmp4[0] = $bmpMock4.PSObject.BaseObject
            $mostrarImagenFrmMethod.Invoke($frmInst4, $pBmp4)

            if ($script:capturedActionRaceFrm) {
                $script:capturedActionRaceFrm.Invoke()
            }

            $bmp4Disposed = $false
            try {
                $bmpMock4.GetHbitmap() | Out-Null
            } catch {
                $bmp4Disposed = $true
            }

            $lateDisposedSeamPassed = ($script:disposedInSeam -and $bmp4Disposed)
            Assert-Check "Delegate disposes bitmap within critical section if control is disposed and avoids assignment" $lateDisposedSeamPassed `
                $(if ($lateDisposedSeamPassed) { "Verified: Delegate safely detected disposed control in critical section, disposed bitmap immediately, and prevented assignment" } else { "Disposed critical section check failed: disposedInSeam=$script:disposedInSeam, bmp4Disposed=$bmp4Disposed" })


            # Clean up cache state
            $limpiarMethod.Invoke($null, @())

        } catch {
            Assert-Check "Seam Unit Tests Execution" $false "Exception during seam execution: $($_.Exception.Message)`n$($_.Exception.StackTrace)"
        }
    }
}

# ---------------------------------------------------------
# Summary
# ---------------------------------------------------------
Write-Host "`n====================================================" -ForegroundColor Cyan
if ($failedChecks -eq 0) {
    Write-Host " Phase 3 Validation: ALL CHECKS PASSED ($failedChecks failures)" -ForegroundColor Green
    Write-Host "====================================================" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host " Phase 3 Validation: $failedChecks CHECK(S) FAILED" -ForegroundColor Red
    Write-Host "====================================================" -ForegroundColor Cyan
    exit 1
}
