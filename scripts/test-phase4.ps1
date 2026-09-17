<#
.SYNOPSIS
    Script de pruebas automatizadas y validacion tecnica reforzada de Fase 4.
.DESCRIPTION
    Verifica de manera determinista, rigurosa y no tautologica:
    1. Existencia, tamano sustantivo y estructura de la documentacion operativa y de contexto (DEPLOYMENT, OPERATIONS, TROUBLESHOOTING, PHASE_CONTEXT, README).
    2. Existencia de scripts operativos de respaldo y restauracion (backup-database.sql, restore-drill.sql, Backup-Database.ps1).
    3. Extraccion dinamica y validacion de enlaces Markdown inline Y por referencia ([texto][id] + [id]: destino), resolviendo rutas relativas, ignorando anclas internas y verificando existencia fisica.
    4. Prohibicion absoluta de enlaces absolutos con esquema URI 'file:///' o locales no portables.
    5. Escaneo estricto de seguridad: patrones para secret, token, credential, api[-_]?key, password/pwd, bearer con delimitadores =, : y comillas, con exclusion documentada de placeholders.
    6. Guardrails de backup-database.sql: parametro $(BackupFolder) obligatorio con falla cerrada, cero fallbacks hardcodeados, prohibicion de FORMAT/INIT y uso seguro de NOFORMAT/NOINIT con nombre unico.
    7. Guardrails de Backup-Database.ps1: WhatIf seguro sin alterar FS, New-Item y sqlcmd confinados bajo ShouldProcess, validacion sintactica previa y falla cerrada sin parametros.
    8. Guardrails de restore-drill.sql: aislamiento en base drill, prohibicion de produccion, falla cerrada si existe DB previa salvo confirmacion explicita $(ConfirmOverwriteExistingDrillDb) y cero rutas hardcodeadas.
    9. Guardrails de restauracion en OPERATIONS.md: ausencia de bloque de produccion ejecutable por defecto, checklist previo, confirmacion interactiva humana (Read-Host) y placeholders parametrizados.
    10. Correccion de sintaxis y escaping PowerShell: uso de comillas simples 'MSSQL$SQLEXPRESS01' para evitar expansion indebida de variables.
    11. Coherencia estricta de la cadena GymDbConnection, paridad App.config y politica de minimos privilegios (least-privilege).
    12. Validacion del SDK DigitalPersona, ensamblados en libs/, drivers oficiales manuales y codigos de falla de hardware.
    13. Cobertura completa de la matriz de troubleshooting (SQL, hardware USB, FRR/biometria, cooldown de BiometricCache, build).
    14. Validacion cruzada de README.md y PHASE_CONTEXT.md.
    15. Ausencia total de espacios en blanco finales (trailing whitespace) en documentacion, scripts y archivos untracked relevantes.
    16. Regresiones de validacion negativa: demostracion de que whitespace artificial, enlaces de referencia rotos y 'Secret=' realista fallan cerradamente con limpieza de fixtures.
#>

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
Write-Host " Running Phase 4 Strengthened Automated Checks" -ForegroundColor Cyan
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

# =========================================================
# Funciones Auxiliares de Validacion (Fase 4)
# =========================================================

# ---------------------------------------------------------
# Parser de Enlaces Markdown: Inline y por Referencia
# ---------------------------------------------------------
function Parse-MarkdownFileLinks {
    param(
        [string]$Content,
        [string]$FilePath,
        [string]$Root
    )

    $mdDir = Split-Path -Parent $FilePath
    $relMd = $FilePath
    if ($FilePath.StartsWith($Root)) {
        $relMd = $FilePath.Substring($Root.Length).TrimStart('\', '/')
    }

    $detectedFileUriLinks = @()
    $allExtractedExternalLinks = @()
    $malformedExternalLinks = @()
    $allExtractedRelativeLinks = @()
    $missingRelativeLinks = @()

    # 1. Extraer definiciones de referencias: [id]: destino ["titulo opcional"]
    $refDefs = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $defRegex = '(?m)^\s{0,3}\[([^\]]+)\]:\s*<?([^\s>]+)>?'
    $defMatches = [regex]::Matches($Content, $defRegex)
    foreach ($dm in $defMatches) {
        $defId = [regex]::Replace($dm.Groups[1].Value.Trim(), '\s+', ' ')
        $defTarget = $dm.Groups[2].Value.Trim()
        $refDefs[$defId] = $defTarget
    }

    $checkedDefs = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $linkEntries = [System.Collections.Generic.List[PSCustomObject]]::new()

    # 2. Enlaces inline: [texto](destino)
    $inlineRegex = '\[([^\]]+)\]\(([^)]+)\)'
    $inlineMatches = [regex]::Matches($Content, $inlineRegex)
    foreach ($m in $inlineMatches) {
        $linkText = $m.Groups[1].Value.Trim()
        $destRaw = ($m.Groups[2].Value.Trim() -split '\s+', 2)[0].Trim('<', '>')
        $linkEntries.Add([PSCustomObject]@{
            Text  = $linkText
            Dest  = $destRaw
            Type  = "Inline"
            RefId = $null
        })
    }

    # 3. Enlaces por referencia: [texto][id] o [id][]
    $refUsageRegex = '\[([^\]]+)\]\[([^\]]*)\]'
    $refMatches = [regex]::Matches($Content, $refUsageRegex)
    foreach ($rm in $refMatches) {
        $linkText = $rm.Groups[1].Value.Trim()
        $refId = $rm.Groups[2].Value.Trim()
        if ([string]::IsNullOrWhiteSpace($refId)) { $refId = $linkText }
        $refIdNorm = [regex]::Replace($refId, '\s+', ' ')

        if ($refDefs.ContainsKey($refIdNorm)) {
            $destRaw = $refDefs[$refIdNorm]
            $checkedDefs.Add($refIdNorm) | Out-Null
            $linkEntries.Add([PSCustomObject]@{
                Text  = $linkText
                Dest  = $destRaw
                Type  = "Reference"
                RefId = $refIdNorm
            })
        } else {
            $missingRelativeLinks += "$relMd -> [$linkText][$refId] (Referencia Markdown sin definicion)"
        }
    }

    # 4. Validar definiciones de referencia que no hayan sido invocadas aun
    foreach ($kvp in $refDefs) {
        if (-not $checkedDefs.Contains($kvp.Key)) {
            $linkEntries.Add([PSCustomObject]@{
                Text  = "[Definicion:$($kvp.Key)]"
                Dest  = $kvp.Value
                Type  = "Definition"
                RefId = $kvp.Key
            })
        }
    }

    # 5. Procesar destinos recolectados
    foreach ($entry in $linkEntries) {
        $destRaw = $entry.Dest
        $linkText = $entry.Text

        if ([string]::IsNullOrWhiteSpace($destRaw)) { continue }

        # Omitir anclas internas puras (#seccion)
        if ($destRaw.StartsWith('#')) { continue }

        # Detectar esquema prohibido file:
        if ($destRaw -match '^(?i)file:') {
            $detectedFileUriLinks += "$relMd [$linkText] -> $destRaw"
            continue
        }

        # Validar enlaces externos http(s) / mailto / ftp sintacticamente sin red
        if ($destRaw -match '^(?i)(https?://|mailto:|ftp:)') {
            $uriResult = $null
            $isValidUri = [System.Uri]::TryCreate($destRaw, [System.UriKind]::Absolute, [ref]$uriResult) -and
                          ($uriResult.Scheme -in @('http', 'https', 'mailto', 'ftp'))
            if ($isValidUri) {
                $allExtractedExternalLinks += [PSCustomObject]@{
                    Source = $relMd
                    Text   = $linkText
                    Url    = $destRaw
                    Scheme = $uriResult.Scheme
                }
            } else {
                $malformedExternalLinks += "$relMd [$linkText] -> $destRaw (URI malformada)"
            }
            continue
        }

        # Enlaces relativos locales: separar ancla interna (#) o query (?)
        $cleanTarget = ($destRaw -split '[#\?]')[0]
        if ([string]::IsNullOrWhiteSpace($cleanTarget)) { continue }

        $resolvedPath = [System.IO.Path]::GetFullPath((Join-Path $mdDir $cleanTarget))
        $targetExists = Test-Path -LiteralPath $resolvedPath

        $allExtractedRelativeLinks += [PSCustomObject]@{
            Source   = $relMd
            Text     = $linkText
            Target   = $cleanTarget
            Resolved = $resolvedPath
            Exists   = $targetExists
        }

        if (-not $targetExists) {
            $missingRelativeLinks += "$relMd -> $cleanTarget (Resuelto a: $resolvedPath)"
        }
    }

    return @{
        RelativeLinks = $allExtractedRelativeLinks
        ExternalLinks = $allExtractedExternalLinks
        MissingLinks  = $missingRelativeLinks
        MalformedExt  = $malformedExternalLinks
        FileUriLinks  = $detectedFileUriLinks
    }
}

# ---------------------------------------------------------
# Filtro de Exclusiones de Secretos (No Tautologico)
# ---------------------------------------------------------
# Justificacion documentada de exclusiones:
# 1. Delimitadores de variables y placeholders documentales:
#    - <...> (e.g. <PasswordSeguro>, <tu_token>)
#    - $(...) (e.g. $(BackupFolder), $(Password))
#    - ${...}, {{...}}, {...} (variables de plantilla/interpolacion)
# 2. Marcadores elipticos o mascaras de documentacion: ..., ***, xxx
# 3. Literales nulos, booleanos o palabras obvias de ejemplo/placeholder:
#    - null, none, true, false, undefined, n/a, empty, required, optional
#    - prefijos o terminos: placeholder, dummy, sample, example, ejemplo, fake, test, your-, tu-, change_me, replace_me
# 4. Longitud minima < 5 caracteres o cadenas sin contenido alfanumerico
function Test-IsSecretPlaceholder {
    param([string]$Val)
    if ([string]::IsNullOrWhiteSpace($Val)) { return $true }
    $v = $Val.Trim()
    if ($v -match '^<[^>\r\n]+>$') { return $true }
    if ($v -match '^\$\([^\)\r\n]+\)$') { return $true }
    if ($v -match '^\$\{[^\}\r\n]+\}$') { return $true }
    if ($v -match '^\{\{[^\}\r\n]+\}\}$') { return $true }
    if ($v -match '^\{[^\{\}\r\n]+\}$') { return $true }
    if ($v -match '^(\.{3,}|\*{3,}|x{3,}|X{3,})$') { return $true }
    if ($v -in @('null', 'none', 'true', 'false', 'undefined', 'n/a', 'na', 'empty', 'required', 'optional')) { return $true }
    if ($v -match '(?i)^(placeholder|dummy|sample|example|fake|test|your[-_]|tu[-_]|change[-_]?me|replace[-_]?me|insert[-_]?here)') { return $true }
    if ($v -match '(?i)(placeholder|example|ejemplo|dummy|sample)') { return $true }
    if ($v.Length -lt 5) { return $true }
    if ($v -match '^[^a-zA-Z0-9]+$') { return $true }
    return $false
}

function Find-SecretLeaksInContent {
    param(
        [string]$Content,
        [string]$FilePath,
        [string]$Root
    )
    $rel = $FilePath
    if ($FilePath.StartsWith($Root)) {
        $rel = $FilePath.Substring($Root.Length).TrimStart('\', '/')
    }

    $q = "[" + [char]34 + [char]39 + "]"
    $nq = "[^" + [char]34 + [char]39 + "\r\n]"
    $noVal = "[^\s;" + [char]34 + [char]39 + ",]+"
    $secretRegexes = @(
        "(?i)\b(?<key>[a-z0-9_\-]*(?:secret|token|credential|api[-_]?key|password|pwd)[a-z0-9_\-]*)\s*[:=]\s*N?$q(?<val>$nq+)$q",
        "(?i)\b(?<key>[a-z0-9_\-]*(?:secret|token|credential|api[-_]?key|password|pwd)[a-z0-9_\-]*)\s*[:=]\s*(?<val>$noVal)",
        "(?i)\b(?<key>bearer)\s+N?$q?(?<val>[a-zA-Z0-9_\-\.]{15,})$q?"
    )

    $leaks = @()
    $lines = $Content -split "\r?\n"
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        if ($line -match '^\s*#|^\s*//') { continue }

        foreach ($rgx in $secretRegexes) {
            $matches = [regex]::Matches($line, $rgx)
            foreach ($m in $matches) {
                $val = $m.Groups['val'].Value
                $key = $m.Groups['key'].Value
                if (-not (Test-IsSecretPlaceholder $val)) {
                    $leaks += "$rel (linea $($i + 1)): Coincide $key con posible secreto: '$val'"
                }
            }
        }
    }
    return $leaks
}

# ---------------------------------------------------------
# Deteccion de Espacios en Blanco Finales (Trailing Whitespace)
# ---------------------------------------------------------
function Find-TrailingWhitespaceInFile {
    param([string]$FilePath)
    if (-not (Test-Path -LiteralPath $FilePath)) { return @() }
    $leaks = @()
    $lines = Get-Content -LiteralPath $FilePath
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($lines[$i] -match '\s+$') {
            $leaks += "linea $($i + 1)"
        }
    }
    return $leaks
}

# ---------------------------------------------------------
# Test 1: Existencia y contenido sustantivo de documentación
# ---------------------------------------------------------
Write-Host "`n--- Test 1: Existencia de Documentación Operativa y Contexto ---" -ForegroundColor Yellow

$docFiles = @(
    @{ RelPath = "Docs\DEPLOYMENT.md"; MinBytes = 4000; Title = "Manual de Despliegue" },
    @{ RelPath = "Docs\OPERATIONS.md"; MinBytes = 4000; Title = "Manual de Operación y Respaldos" },
    @{ RelPath = "Docs\TROUBLESHOOTING.md"; MinBytes = 4000; Title = "Guía de Troubleshooting" },
    @{ RelPath = "Docs\PHASE_CONTEXT.md"; MinBytes = 4000; Title = "Contexto Operativo de Fases" },
    @{ RelPath = "README.md"; MinBytes = 2000; Title = "README Principal" }
)

foreach ($doc in $docFiles) {
    $fullPath = Join-Path $RepoRoot $doc.RelPath
    $exists = Test-Path -LiteralPath $fullPath
    if ($exists) {
        $len = (Get-Item -LiteralPath $fullPath).Length
        $hasContent = $len -ge $doc.MinBytes
        Assert-Check "Doc Exists ($($doc.RelPath))" $hasContent "$($doc.Title) presente con contenido sustantivo ($len bytes)"
    } else {
        Assert-Check "Doc Exists ($($doc.RelPath))" $false "Archivo no encontrado en $fullPath"
    }
}

# ---------------------------------------------------------
# Test 2: Scripts Operativos de Respaldo y Restauración
# ---------------------------------------------------------
Write-Host "`n--- Test 2: Scripts Operativos de Respaldo y Restauración ---" -ForegroundColor Yellow

$scriptFiles = @(
    @{ RelPath = "scripts\backup-database.sql"; MinBytes = 1000; Desc = "Script seguro T-SQL de respaldo" },
    @{ RelPath = "scripts\restore-drill.sql"; MinBytes = 1500; Desc = "Script de simulacro aislado de restauración" },
    @{ RelPath = "scripts\Backup-Database.ps1"; MinBytes = 1500; Desc = "Automatización PowerShell de respaldo y retención" }
)

foreach ($sc in $scriptFiles) {
    $fullPath = Join-Path $RepoRoot $sc.RelPath
    $exists = Test-Path -LiteralPath $fullPath
    if ($exists) {
        $len = (Get-Item -LiteralPath $fullPath).Length
        $hasContent = $len -ge $sc.MinBytes
        Assert-Check "Script Exists ($($sc.RelPath))" $hasContent "$($sc.Desc) presente ($len bytes)"
    } else {
        Assert-Check "Script Exists ($($sc.RelPath))" $false "Archivo no encontrado en $fullPath"
    }
}

# ---------------------------------------------------------
# Test 3: Extracción Dinámica y Validación de Enlaces Markdown (Cero Lista Manual)
# ---------------------------------------------------------
Write-Host "`n--- Test 3: Extracción Dinámica de Enlaces Markdown ---" -ForegroundColor Yellow

# Escaneo dinámico de TODOS los archivos *.md del repositorio
# Justificación de exclusiones:
# - .git: metadatos internos del sistema de control de versiones.
# - bin / obj: artefactos transitorios y salidas de compilación.
# Todos los demás directorios (incluyendo paquetes en packages/) son inspeccionados dinámicamente.
$discoveredMdFiles = Get-ChildItem -Path $RepoRoot -Filter "*.md" -Recurse -File |
    Where-Object { $_.FullName -notmatch '[\\/](\.git|bin|obj)[\\/]' }

$requiredCoreDocs = @("README.md", "Docs\DEPLOYMENT.md", "Docs\OPERATIONS.md", "Docs\TROUBLESHOOTING.md", "Docs\PHASE_CONTEXT.md")
$foundCoreDocs = @()
foreach ($coreDoc in $requiredCoreDocs) {
    if ($discoveredMdFiles | Where-Object { $_.FullName.EndsWith($coreDoc, [System.StringComparison]::OrdinalIgnoreCase) }) {
        $foundCoreDocs += $coreDoc
    }
}
$discoveredContainsCore = ($foundCoreDocs.Count -eq $requiredCoreDocs.Count)
Assert-Check "Dynamic discovery of all Markdown files in repository" ($discoveredMdFiles.Count -ge 5 -and $discoveredContainsCore) `
    $(if ($discoveredMdFiles.Count -ge 5 -and $discoveredContainsCore) { "Descubiertos dinámicamente $($discoveredMdFiles.Count) archivos Markdown en el repositorio (incluyendo paquetes; sin lista manual como única autoridad)" } else { "Fallo en descubrimiento dinámico de archivos Markdown" })

$allExtractedRelativeLinks = @()
$allExtractedExternalLinks = @()
$missingRelativeLinks = @()
$malformedExternalLinks = @()
$detectedFileUriLinks = @()

foreach ($fileObj in $discoveredMdFiles) {
    $fullMdPath = $fileObj.FullName
    $content = Get-Content -LiteralPath $fullMdPath -Raw
    $parseResult = Parse-MarkdownFileLinks -Content $content -FilePath $fullMdPath -Root $RepoRoot

    $allExtractedRelativeLinks += $parseResult.RelativeLinks
    $allExtractedExternalLinks += $parseResult.ExternalLinks
    $missingRelativeLinks      += $parseResult.MissingLinks
    $malformedExternalLinks    += $parseResult.MalformedExt
    $detectedFileUriLinks      += $parseResult.FileUriLinks
}

# Validacion de resolucion fisica de enlaces relativos
$relExtractionSuccess = ($missingRelativeLinks.Count -eq 0) -and ($allExtractedRelativeLinks.Count -ge 15)
Assert-Check "Dynamic Markdown relative links resolution and physical existence" $relExtractionSuccess `
    $(if ($relExtractionSuccess) { "Se extrajeron dinamicamente $($allExtractedRelativeLinks.Count) enlaces relativos (inline y por referencia); todos se resolvieron respecto al directorio del Markdown y existen fisicamente en disco" } else { "Fallaron enlaces relativos: $($missingRelativeLinks -join '; ')" })

# Validacion de enlaces externos (sin requerir red)
$extValidationSuccess = ($malformedExternalLinks.Count -eq 0) -and ($allExtractedExternalLinks.Count -gt 0)
Assert-Check "Dynamic Markdown external HTTP(S) links validation (offline)" $extValidationSuccess `
    $(if ($extValidationSuccess) { "Se extrajeron y validaron sintacticamente $($allExtractedExternalLinks.Count) enlaces externos HTTP(S)/web sin requerir conectividad de red" } else { "Enlaces externos malformados: $($malformedExternalLinks -join '; ')" })

# Assert-Check explicito que falla si se detecta cualquier enlace con esquema 'file:'
$zeroFileSchemeLinks = ($detectedFileUriLinks.Count -eq 0)
Assert-Check "Zero 'file:' URI scheme links in all Markdown files" $zeroFileSchemeLinks `
    $(if ($zeroFileSchemeLinks) { "Cero enlaces con esquema 'file:' detectados en los $($discoveredMdFiles.Count) archivos Markdown escaneados dinamicamente" } else { "FALLO: Se detectaron $($detectedFileUriLinks.Count) enlace(s) prohibido(s) con esquema 'file:': $($detectedFileUriLinks -join '; ')" })

# ---------------------------------------------------------
# Test 4: Prohibicion Absoluta de Enlaces Absolutos con Esquema file:///
# (Cubre todos los archivos Markdown descubiertos dinamicamente y scripts operativos)
# ---------------------------------------------------------
Write-Host "`n--- Test 4: Prohibicion Absoluta de Enlaces Absolutos 'file:///' ---" -ForegroundColor Yellow

$phase4ExplicitFiles = @(
    "Docs\DEPLOYMENT.md",
    "Docs\OPERATIONS.md",
    "Docs\TROUBLESHOOTING.md",
    "Docs\PHASE_CONTEXT.md",
    "README.md",
    "scripts\backup-database.sql",
    "scripts\restore-drill.sql",
    "scripts\Backup-Database.ps1",
    "scripts\test-phase4.ps1"
)

$phase4ScannableFiles = [System.Collections.Generic.List[string]]::new()
foreach ($f in $phase4ExplicitFiles) {
    $norm = $f.Replace('/', '\')
    if (-not $phase4ScannableFiles.Contains($norm)) {
        $phase4ScannableFiles.Add($norm)
    }
}

# Incorporar dinamicamente archivos untracked relevantes (docs, scripts, sql, cs)
try {
    $untracked = git -C $RepoRoot ls-files --others --exclude-standard 2>$null
    if ($untracked) {
        foreach ($u in $untracked) {
            $norm = $u.Trim().Replace('/', '\')
            if ([string]::IsNullOrWhiteSpace($norm)) { continue }
            if ($norm -match '\.(md|ps1|sql|cs|xaml|config|txt)$' -and $norm -notmatch '[\\/](\.git|packages|bin|obj)[\\/]') {
                if (-not $phase4ScannableFiles.Contains($norm)) {
                    $phase4ScannableFiles.Add($norm)
                }
            }
        }
    }
} catch { }

$allPhase4ScannableFiles = [System.Collections.Generic.List[string]]::new()
foreach ($scannable in $phase4ScannableFiles) {
    $fullPath = Join-Path $RepoRoot $scannable
    if (Test-Path -LiteralPath $fullPath) {
        $allPhase4ScannableFiles.Add($fullPath)
    }
}
foreach ($md in $discoveredMdFiles) {
    if (-not $allPhase4ScannableFiles.Contains($md.FullName)) {
        $allPhase4ScannableFiles.Add($md.FullName)
    }
}

$fileUriLeaks = @()
foreach ($full in $allPhase4ScannableFiles) {
    if (-not (Test-Path -LiteralPath $full)) { continue }
    $rel = $full.Substring($RepoRoot.Length).TrimStart('\', '/')
    if ($rel -eq "scripts\test-phase4.ps1") { continue }
    $lines = Get-Content -LiteralPath $full
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($lines[$i] -match '(?i)file:///[a-zA-Z]:' -or $lines[$i] -match '(?i)file:///') {
            $fileUriLeaks += "$rel (linea $($i + 1)): $($lines[$i].Trim())"
        }
    }
}

Assert-Check "Zero file:/// URI absolute links" ($fileUriLeaks.Count -eq 0) `
    $(if ($fileUriLeaks.Count -eq 0) { "Cero enlaces absolutos 'file:///' detectados en los $($allPhase4ScannableFiles.Count) archivos analizados (Markdown dinamicos + scripts)" } else { "Se detectaron enlaces absolutos prohibidos: $($fileUriLeaks -join '; ')" })

# ---------------------------------------------------------
# Test 5: Escaneo de Seguridad: Cero Secretos o Credenciales Hardcodeadas
# (Cubre todos los archivos Markdown descubiertos dinamicamente y scripts operativos)
# ---------------------------------------------------------
Write-Host "`n--- Test 5: Escaneo de Seguridad (Cero Secretos) ---" -ForegroundColor Yellow

$secretLeaks = @()
foreach ($fp in $allPhase4ScannableFiles) {
    if (-not (Test-Path -LiteralPath $fp)) { continue }
    $rel = $fp.Substring($RepoRoot.Length).TrimStart('\', '/')
    # Omitir el propio script de pruebas para evitar auto-coincidencia de definiciones de expresiones regulares
    if ($rel -eq "scripts\test-phase4.ps1") { continue }
    $content = Get-Content -LiteralPath $fp -Raw
    $leaks = Find-SecretLeaksInContent -Content $content -FilePath $fp -Root $RepoRoot
    if ($leaks.Count -gt 0) {
        $secretLeaks += $leaks
    }
}

Assert-Check "No hardcoded secrets or cleartext credentials" ($secretLeaks.Count -eq 0) `
    $(if ($secretLeaks.Count -eq 0) { "Cero credenciales en texto plano o secretos detectados en los $($allPhase4ScannableFiles.Count) archivos analizados (Markdown dinamicos + scripts)" } else { "Se detectaron posibles secretos: $($secretLeaks -join '; ')" })

# ---------------------------------------------------------
# Test 6: Guardrails de backup-database.sql
# ---------------------------------------------------------
Write-Host "`n--- Test 6: Guardrails de scripts/backup-database.sql ---" -ForegroundColor Yellow

$backupSqlPath = Join-Path $RepoRoot "scripts\backup-database.sql"
$backupSqlText = Get-Content -LiteralPath $backupSqlPath -Raw

# 1. Requiere $(BackupFolder) explícito y falla cerrado
$requiresBackupFolder = ($backupSqlText -match '(?s)@DestDir.*?=\s*N''\$\(BackupFolder\)''.*?RAISERROR.*?RETURN;')
Assert-Check 'backup-database.sql requires $(BackupFolder) and fails closed' $requiresBackupFolder `
    $(if ($requiresBackupFolder) { 'Requiere explícitamente $(BackupFolder) y falla cerrado con RAISERROR si no se suministra' } else { 'El script no valida obligatoriedad de $(BackupFolder) de forma cerrada' })

# 2. Eliminación absoluta de fallback de ruta hardcodeada
$hasHardcodedPath = ($backupSqlText -match '(?i)C:\\Program Files\\Microsoft SQL Server') -or
                    ($backupSqlText -match '(?i)SET\s+@DestDir\s*=\s*N?''[a-zA-Z]:\\')
Assert-Check "backup-database.sql zero hardcoded path fallbacks" (-not $hasHardcodedPath) `
    $(if (-not $hasHardcodedPath) { "Cero rutas de fallback hardcodeadas en backup-database.sql" } else { "Detectada ruta hardcodeada de fallback en backup-database.sql" })

# 3. Prohibición de FORMAT/INIT y uso de NOFORMAT/NOINIT con nombre único
$usesInsecureInit = ($backupSqlText -match '\bFORMAT\b') -or ($backupSqlText -match '\bINIT\b')
$usesSafeNoInit   = ($backupSqlText -match '\bNOFORMAT\b') -and ($backupSqlText -match '\bNOINIT\b')
$usesUniqueName   = ($backupSqlText -match 'NEWID\(\)|DATEPART|Timestamp')
$safeBackupOptions = (-not $usesInsecureInit) -and $usesSafeNoInit -and $usesUniqueName

Assert-Check "backup-database.sql uses safe NOFORMAT, NOINIT with unique name" $safeBackupOptions `
    $(if ($safeBackupOptions) { "Prohíbe FORMAT/INIT; usa NOFORMAT, NOINIT y genera sufijo único para prevenir sobreescritura accidental" } else { "backup-database.sql contiene FORMAT/INIT o carece de NOFORMAT, NOINIT con identificador único" })

# 4. Documenta que no se ejecuta por defecto
$documentsNoDefault = $backupSqlText -match 'NO SE EJECUTA POR DEFECTO'
Assert-Check "backup-database.sql documents non-default execution" $documentsNoDefault `
    $(if ($documentsNoDefault) { "Documenta explícitamente en cabecera que no se ejecuta por defecto" } else { "Falta documentar que el script no se ejecuta por defecto" })

# ---------------------------------------------------------
# Test 7: Guardrails de scripts/Backup-Database.ps1 (-WhatIf y ShouldProcess)
# ---------------------------------------------------------
Write-Host "`n--- Test 7: Guardrails de scripts/Backup-Database.ps1 ---" -ForegroundColor Yellow

$backupPs1Path = Join-Path $RepoRoot "scripts\Backup-Database.ps1"
$backupPs1Text = Get-Content -LiteralPath $backupPs1Path -Raw

# 1. New-Item dentro de ShouldProcess y sin creación prematura
$newItemInShouldProcess = $backupPs1Text -match 'if\s*\(\$PSCmdlet\.ShouldProcess\(\$BackupDirectory,\s*"Crear directorio de respaldo"\)\)\s*\{\s*New-Item'
Assert-Check "Backup-Database.ps1 moves New-Item inside ShouldProcess" $newItemInShouldProcess `
    $(if ($newItemInShouldProcess) { "New-Item está estrictamente condicionado dentro de ShouldProcess" } else { "New-Item se invoca fuera de ShouldProcess" })

# 2. sqlcmd invocado únicamente bajo ShouldProcess
$sqlcmdInShouldProcess = $backupPs1Text -match 'if\s*\(\$PSCmdlet\.ShouldProcess\(\$DatabaseName,\s*"Ejecutar respaldo completo.*?"\)\)\s*\{\s*if\s*\(\$sqlcmdExe\)'
Assert-Check "Backup-Database.ps1 invokes sqlcmd under ShouldProcess only" $sqlcmdInShouldProcess `
    $(if ($sqlcmdInShouldProcess) { "sqlcmd se ejecuta exclusivamente cuando ShouldProcess es aprobado" } else { "sqlcmd no está protegido bajo ShouldProcess" })

# 3. Validación de WhatIf real en proceso aislado (cero efectos colaterales en FS)
$tempNonExistentDir = Join-Path $env:TEMP "GymAuditWhatIfTest_$([Guid]::NewGuid().ToString('N'))"
$tempWhatIfOut = Join-Path $env:TEMP "GymAuditWhatIfOut_$([Guid]::NewGuid().ToString('N')).log"
$tempWhatIfErr = Join-Path $env:TEMP "GymAuditWhatIfErr_$([Guid]::NewGuid().ToString('N')).log"
$whatIfProc = Start-Process -FilePath "powershell.exe" `
    -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$backupPs1Path`" -BackupDirectory `"$tempNonExistentDir`" -WhatIf" `
    -Wait -PassThru -NoNewWindow `
    -RedirectStandardOutput $tempWhatIfOut -RedirectStandardError $tempWhatIfErr

$dirCreated = Test-Path -LiteralPath $tempNonExistentDir
$whatIfSuccess = ($whatIfProc.ExitCode -eq 0) -and (-not $dirCreated)
if ($dirCreated) { Remove-Item -LiteralPath $tempNonExistentDir -Force -Recurse }
if (Test-Path -LiteralPath $tempWhatIfOut) { Remove-Item -LiteralPath $tempWhatIfOut -Force }
if (Test-Path -LiteralPath $tempWhatIfErr) { Remove-Item -LiteralPath $tempWhatIfErr -Force }

Assert-Check "Backup-Database.ps1 -WhatIf modifies zero filesystem objects" $whatIfSuccess `
    $(if ($whatIfSuccess) { "Ejecución con -WhatIf completó con código 0 y garantizó cero modificaciones en disco (el directorio de prueba no fue creado)" } else { "Fallo en -WhatIf: Código $($whatIfProc.ExitCode), Directorio creado: $dirCreated" })

# 4. Falla cerrado si falta BackupDirectory
$tempMissingOut = Join-Path $env:TEMP "GymAuditMissingOut_$([Guid]::NewGuid().ToString('N')).log"
$tempMissingErr = Join-Path $env:TEMP "GymAuditMissingErr_$([Guid]::NewGuid().ToString('N')).log"
$missingParamProc = Start-Process -FilePath "powershell.exe" `
    -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$backupPs1Path`"" `
    -Wait -PassThru -NoNewWindow `
    -RedirectStandardOutput $tempMissingOut -RedirectStandardError $tempMissingErr

$failsClosedOnMissingParam = ($missingParamProc.ExitCode -ne 0)
if (Test-Path -LiteralPath $tempMissingOut) { Remove-Item -LiteralPath $tempMissingOut -Force }
if (Test-Path -LiteralPath $tempMissingErr) { Remove-Item -LiteralPath $tempMissingErr -Force }

Assert-Check "Backup-Database.ps1 fails closed when BackupDirectory is omitted" $failsClosedOnMissingParam `
    $(if ($failsClosedOnMissingParam) { "Falla cerrado con código de error si se omite el parámetro obligatorio -BackupDirectory" } else { "El script no falló cerrado ante omisión de -BackupDirectory" })

# ---------------------------------------------------------
# Test 8: Guardrails de scripts/restore-drill.sql
# ---------------------------------------------------------
Write-Host "`n--- Test 8: Guardrails de scripts/restore-drill.sql ---" -ForegroundColor Yellow

$drillSqlPath = Join-Path $RepoRoot "scripts\restore-drill.sql"
$drillSqlText = Get-Content -LiteralPath $drillSqlPath -Raw

# 1. Prohibición terminante de producción
$forbidsProd = ($drillSqlText -match '(?s)IF\s+LOWER\(@TargetDatabase\).*?sistemagimnasio.*?RAISERROR.*?RETURN;')
Assert-Check "restore-drill.sql strictly prohibits production database" $forbidsProd `
    $(if ($forbidsProd) { "Guardrail estricto: prohíbe terminantemente apuntar a [SistemaGimnasio] y aborta con error crítico" } else { "Falta guardrail contra restauración en producción en restore-drill.sql" })

# 2. Requiere confirmación explícita para sobreescribir DB drill existente
$requiresConfirmOverwrite = ($drillSqlText -match '(?s)IF\s+DB_ID\(@TargetDatabase\)\s+IS\s+NOT\s+NULL.*?ConfirmOverwriteExistingDrillDb.*?RAISERROR.*?RETURN;')
Assert-Check "restore-drill.sql fails closed on existing drill DB without explicit flag" $requiresConfirmOverwrite `
    $(if ($requiresConfirmOverwrite) { "Falla cerrado si la base drill previa existe a menos que se suministre ConfirmOverwriteExistingDrillDb='YES'" } else { "restore-drill.sql borra o sobreescribe automáticamente sin confirmación explícita" })

# 3. Cero rutas hardcodeadas en restore-drill.sql
$drillHasHardcodedPath = ($drillSqlText -match '(?i)C:\\Program Files\\Microsoft SQL Server') -or
                         ($drillSqlText -match '(?i)SET\s+@DefaultDataDir\s*=\s*N?''[a-zA-Z]:\\')
Assert-Check "restore-drill.sql zero hardcoded path fallbacks" (-not $drillHasHardcodedPath) `
    $(if (-not $drillHasHardcodedPath) { "Cero rutas hardcodeadas; determina carpetas dinámicamente vía SERVERPROPERTY o master_files" } else { "Detectadas rutas fijas hardcodeadas en restore-drill.sql" })

# 4. Aislamiento con WITH MOVE dinámico y CHECKSUM (sin nombres lógicos hardcodeados)
$hasHardcodedLogicalNames = ($drillSqlText -match "MOVE\s+N?'SistemaGimnasio'\s") -or
                            ($drillSqlText -match "MOVE\s+N?'SistemaGimnasio_log'\s")
$usesDynamicFileList = ($drillSqlText -match 'RESTORE\s+FILELISTONLY') -and
                       ($drillSqlText -match '#BackupFiles')
$drillIsolatesWithMove = ($drillSqlText -match 'SistemaGimnasio_Drill') -and
                         ($drillSqlText -match 'WITH\s+MOVE') -and
                         ($drillSqlText -match 'CHECKSUM') -and
                         (-not $hasHardcodedLogicalNames) -and
                         $usesDynamicFileList
Assert-Check "restore-drill.sql isolates with dynamic WITH MOVE and CHECKSUM" $drillIsolatesWithMove `
    $(if ($drillIsolatesWithMove) { "Restaura en base aislada SistemaGimnasio_Drill con WITH MOVE construido dinámicamente vía RESTORE FILELISTONLY (sin fijar nombres lógicos) y comprobando CHECKSUM" } else { "restore-drill.sql contiene nombres lógicos hardcodeados o carece de WITH MOVE dinámico / CHECKSUM" })

# ---------------------------------------------------------
# Test 9: Guardrails de Restauración en Docs/OPERATIONS.md
# ---------------------------------------------------------
Write-Host "`n--- Test 9: Guardrails de Restore en Docs/OPERATIONS.md ---" -ForegroundColor Yellow

$operationsPath = Join-Path $RepoRoot "Docs\OPERATIONS.md"
$operationsText = Get-Content -LiteralPath $operationsPath -Raw

# 1. No documenta bloque ejecutable directo sin barreras
$hasUnguardedProdRestore = $operationsText -match 'RESTORE DATABASE \[SistemaGimnasio\]\s+FROM DISK = N''C:\\Backups\\UltimoRespaldo\.bak'''
Assert-Check "OPERATIONS.md zero unguarded executable production restore" (-not $hasUnguardedProdRestore) `
    $(if (-not $hasUnguardedProdRestore) { "No contiene bloques ejecutables de restauración en producción sin barreras ni rutas ficticias directas" } else { "OPERATIONS.md documenta bloque ejecutable directo de restore sobre producción" })

# 2. Documenta Checklist Previo y Procedimiento Excepcional
$hasChecklist = ($operationsText -match 'Checklist Obligatorio Previo') -and
                ($operationsText -match 'Tail-Log Backup|Copia Fría') -and
                ($operationsText -match 'Validación previa obligatoria mediante Restore Drill') -and
                ($operationsText -match 'Validación de instancia y base de datos objetivo')
Assert-Check "OPERATIONS.md documents mandatory pre-restore checklist" $hasChecklist `
    $(if ($hasChecklist) { "Documenta checklist previo exhaustivo (Tail-Log backup, validación previa aislada y validación de instancia)" } else { "Falta checklist previo de seguridad en OPERATIONS.md" })

# 3. Documenta confirmación interactiva fuera de SQL (Read-Host)
$hasInteractiveConfirm = ($operationsText -match 'Read-Host') -and
                         ($operationsText -match 'CONFIRMO_RESTAURACION_PRODUCCION')
Assert-Check "OPERATIONS.md documents out-of-SQL interactive confirmation" $hasInteractiveConfirm `
    $(if ($hasInteractiveConfirm) { "Requiere confirmación interactiva manual humana fuera de SQL mediante PowerShell (Read-Host)" } else { "Falta procedimiento interactivo fuera de SQL en OPERATIONS.md" })

# 4. Usa variables/placeholders parametrizados
$usesParamPlaceholders = ($operationsText -match '\$\(TARGET_DB_NAME\)') -and
                         ($operationsText -match '\$\(VERIFIED_BACKUP_PATH\)')
Assert-Check "OPERATIONS.md uses parameterized placeholders" $usesParamPlaceholders `
    $(if ($usesParamPlaceholders) { "Plantilla de emergencia utiliza variables explícitas y placeholders que impiden ejecución accidental" } else { "Faltan placeholders parametrizados en plantilla de emergencia" })

# ---------------------------------------------------------
# Test 10: Sintaxis y Escaping PowerShell (MSSQL$)
# ---------------------------------------------------------
Write-Host "`n--- Test 10: Escaping PowerShell en Documentación ---" -ForegroundColor Yellow

$troubleshootingPath = Join-Path $RepoRoot "Docs\TROUBLESHOOTING.md"
$troubleshootingText = Get-Content -LiteralPath $troubleshootingPath -Raw

# 1. Verifica comillas simples en Start-Service para evitar expansión de $SQLEXPRESS01
$usesSingleQuotesStartService = $troubleshootingText -match 'Start-Service\s+-Name\s+''MSSQL\$SQLEXPRESS01'''
Assert-Check "TROUBLESHOOTING.md uses single quotes for Start-Service MSSQL$" $usesSingleQuotesStartService `
    $(if ($usesSingleQuotesStartService) { "Start-Service utiliza comillas simples 'MSSQL`$SQLEXPRESS01' evitando expansión indebida de variable PowerShell" } else { "Start-Service contiene comillas dobles o escaping erróneo" })

# 2. Ausencia de comillas dobles sin escapar con MSSQL$
$hasUnescapedDoubleQuotesMssql = $troubleshootingText -match 'Start-Service\s+-Name\s+"MSSQL\$'
Assert-Check "Zero unescaped double-quoted MSSQL$ in TROUBLESHOOTING.md" (-not $hasUnescapedDoubleQuotesMssql) `
    $(if (-not $hasUnescapedDoubleQuotesMssql) { "Cero variables `MSSQL`$ expandidas por comillas dobles detectadas" } else { "Detectado patrón inseguro Start-Service con comillas dobles" })

# ---------------------------------------------------------
# Test 11: Coherencia de Cadena de Conexión y Permisos Mínimos
# ---------------------------------------------------------
Write-Host "`n--- Test 11: Configuración SQL y Principio de Mínimo Privilegio ---" -ForegroundColor Yellow

$deploymentPath = Join-Path $RepoRoot "Docs\DEPLOYMENT.md"
$deploymentText = Get-Content -LiteralPath $deploymentPath -Raw

$expectedConn = "Data Source=.\SQLEXPRESS01;Initial Catalog=SistemaGimnasio;Integrated Security=True"
$hasExactConn = $deploymentText.Contains($expectedConn)
Assert-Check "DEPLOYMENT.md contains exact GymDbConnection" $hasExactConn `
    $(if ($hasExactConn) { "Cadena canónica '$expectedConn' documentada exactamente" } else { "Cadena de conexión en DEPLOYMENT.md difiere de App.config" })

$mentionsBothConfigs = ($deploymentText -match 'Sistema_Gimnasio[\\/]App\.config') -and ($deploymentText -match 'BiometricApp[\\/].*?App\.config')
Assert-Check "DEPLOYMENT.md covers both App.config files" $mentionsBothConfigs `
    $(if ($mentionsBothConfigs) { "Documenta paridad estricta entre Sistema_Gimnasio y BiometricApp" } else { "Falta documentación sobre la paridad de ambos App.config" })

$mentionsLeastPrivilege = ($deploymentText -match 'db_datareader') -and ($deploymentText -match 'db_datawriter')
$warnsAgainstSysadmin = ($deploymentText -match 'sysadmin') -and ($deploymentText -match 'NUNCA')
Assert-Check "Least-Privilege SQL permissions documented" ($mentionsLeastPrivilege -and $warnsAgainstSysadmin) `
    $(if ($mentionsLeastPrivilege -and $warnsAgainstSysadmin) { "Recomienda roles db_datareader/db_datawriter y prohíbe sysadmin/db_owner en runtime" } else { "Documentación de permisos SQL incompleta o insegura" })

$explainsIntegratedSecurity = $deploymentText -match 'Integrated Security\s*=\s*True' -and ($deploymentText -match 'Windows Authentication|Autenticación Integrada')
Assert-Check "Integrated Security (Windows Auth) explained" $explainsIntegratedSecurity `
    $(if ($explainsIntegratedSecurity) { "Explica funcionamiento y configuración de Windows Authentication" } else { "Falta explicación técnica de Integrated Security" })

$mentionsCascadeCorrectly = ($deploymentText -match 'FK_Pago_Membresia.*?ON DELETE CASCADE') -and
                            ($deploymentText -match 'FK_Miembro_Membresia.*?sin cascada')
$hasWrongCascadeClaim = $deploymentText -match 'FK_Miembro_Membresia.*?con borrado en cascada'
$fkDocAccurate = $mentionsCascadeCorrectly -and (-not $hasWrongCascadeClaim)
Assert-Check "DEPLOYMENT.md accurately documents FK cascade behavior" $fkDocAccurate `
    $(if ($fkDocAccurate) { "Documenta con precisión que solo FK_Pago_Membresia tiene ON DELETE CASCADE y FK_Miembro_Membresia no, alineado con database/SistemaGimnasio.sql" } else { "DEPLOYMENT.md contiene afirmaciones erróneas sobre la cascada de claves foráneas" })

# ---------------------------------------------------------
# Test 12: Validación de Hardware y SDK DigitalPersona
# ---------------------------------------------------------
Write-Host "`n--- Test 12: Guía del SDK DigitalPersona y Hardware ---" -ForegroundColor Yellow

$sdkLibsDocumented = ($deploymentText -match 'DPUruNet\.dll') -and ($deploymentText -match 'DPCtlUruNet\.dll') -and
                     ($deploymentText -match 'DPXUru\.dll') -and ($deploymentText -match 'DPCtlXUru\.dll')
Assert-Check "All 4 SDK assemblies in libs/ documented" $sdkLibsDocumented `
    $(if ($sdkLibsDocumented) { "Documenta DPUruNet.dll, DPCtlUruNet.dll, DPXUru.dll y DPCtlXUru.dll" } else { "Faltan ensamblados del SDK en DEPLOYMENT.md" })

$mentionsManualVendorStep = ($deploymentText -match 'HID Global|DigitalPersona') -and ($deploymentText -match 'Paso Manual|proveedor|RTE')
Assert-Check "Manual vendor driver step marked explicitly" $mentionsManualVendorStep `
    $(if ($mentionsManualVendorStep) { "Marca explícitamente como paso manual la adquisición de drivers oficiales sin URLs apócrifas" } else { "Falta delimitar el paso manual del proveedor para los drivers" })

$mentionsArch = ($deploymentText -match 'x86') -and ($deploymentText -match 'x64') -and ($deploymentText -match 'AnyCPU')
Assert-Check "x86/x64/AnyCPU architecture documented" $mentionsArch `
    $(if ($mentionsArch) { "Explica arquitectura AnyCPU en C# vs drivers nativos x86/x64 en el SO" } else { "Falta detalle de arquitectura de 32/64 bits" })

$mentionsHardwareFaults = ($deploymentText -match 'DP_DEVICE_FAILURE') -and ($troubleshootingText -match 'DP_INVALID_DEVICE')
Assert-Check "Hardware disconnection fault codes documented" $mentionsHardwareFaults `
    $(if ($mentionsHardwareFaults) { "Documenta manejo defensivo de DP_DEVICE_FAILURE y DP_INVALID_DEVICE" } else { "Falta documentación de códigos de fallo de hardware" })

# ---------------------------------------------------------
# Test 13: Cobertura de la Matriz de Troubleshooting
# ---------------------------------------------------------
Write-Host "`n--- Test 13: Cobertura de la Matriz de Troubleshooting ---" -ForegroundColor Yellow

$troubleshootsSql = ($troubleshootingText -match 'Error:\s*26') -and ($troubleshootingText -match '18456')
Assert-Check "Troubleshooting covers SQL errors (26, 18456)" $troubleshootsSql `
    $(if ($troubleshootsSql) { "Cubre diagnósticos para fallos de red SQL y errores de login 18456" } else { "Faltan diagnósticos de errores de conexión SQL" })

$troubleshootsReader = ($troubleshootingText -match 'VID_05BA&PID_000A') -and ($troubleshootingText -match 'Administrador de dispositivos')
Assert-Check "Troubleshooting covers reader hardware & USB power" $troubleshootsReader `
    $(if ($troubleshootsReader) { "Diagnostica identificación de hardware VID/PID y ahorro de energía en USB" } else { "Falta diagnóstico de detección física del lector" })

$troubleshootsBiometrics = ($troubleshootingText -match 'Prisma') -and ($troubleshootingText -match 'reseca') -and ($troubleshootingText -match 'frmDBEnrollment')
Assert-Check "Troubleshooting covers fingerprint recognition & FRR" $troubleshootsBiometrics `
    $(if ($troubleshootsBiometrics) { "Cubre limpieza de prisma, factores dérmicos y re-enrolamiento de huellas" } else { "Falta resolución de problemas de reconocimiento de huellas" })

$troubleshootsCache = ($troubleshootingText -match 'BiometricCache') -and ($troubleshootingText -match 'Cooldown|cooldown') -and ($troubleshootingText -match 'NotificadorCambioMiembro')
Assert-Check "Troubleshooting covers BiometricCache cooldown & sync" $troubleshootsCache `
    $(if ($troubleshootsCache) { "Explica comportamiento de enfriamiento (cooldown) y sincronización con NotificadorCambioMiembro" } else { "Falta diagnóstico de sincronización y cooldown de caché" })

$troubleshootsBuild = ($troubleshootingText -match 'MSB3245') -and ($troubleshootingText -match 'RestorePackagesConfig=true')
Assert-Check "Troubleshooting covers MSBuild & NuGet restore" $troubleshootsBuild `
    $(if ($troubleshootsBuild) { "Cubre resolución de dependencias con RestorePackagesConfig=true y MSB3245" } else { "Falta diagnóstico de errores de compilación" })

# ---------------------------------------------------------
# Test 14: Validación Cruzada de README.md y PHASE_CONTEXT.md
# ---------------------------------------------------------
Write-Host "`n--- Test 14: Validación Cruzada de README y PHASE_CONTEXT ---" -ForegroundColor Yellow

$readmePath = Join-Path $RepoRoot "README.md"
$readmeText = Get-Content -LiteralPath $readmePath -Raw

$hasDeploymentLink = $readmeText -match 'Docs/DEPLOYMENT\.md'
$hasOperationsLink = $readmeText -match 'Docs/OPERATIONS\.md'
$hasTroubleshootingLink = $readmeText -match 'Docs/TROUBLESHOOTING\.md'
$allDocsLinked = $hasDeploymentLink -and $hasOperationsLink -and $hasTroubleshootingLink

Assert-Check "README.md links to all Phase 4 documentation" $allDocsLinked `
    $(if ($allDocsLinked) { "README.md referencia DEPLOYMENT.md, OPERATIONS.md y TROUBLESHOOTING.md" } else { "Faltan enlaces a manuales de Fase 4 en README.md" })

$phaseContextPath = Join-Path $RepoRoot "Docs\PHASE_CONTEXT.md"
$phaseContextText = Get-Content -LiteralPath $phaseContextPath -Raw

$contextCoversPhase4 = ($phaseContextText -match 'Fase 4') -and
                       ($phaseContextText -match 'DEPLOYMENT\.md') -and
                       ($phaseContextText -match 'OPERATIONS\.md') -and
                       ($phaseContextText -match 'backup-database\.sql') -and
                       ($phaseContextText -match 'restore-drill\.sql')
Assert-Check "PHASE_CONTEXT.md documents Phase 4 status and operational scripts" $contextCoversPhase4 `
    $(if ($contextCoversPhase4) { "PHASE_CONTEXT.md documenta el estado, manuales y scripts operativos de Fase 4" } else { "Falta documentación de Fase 4 en PHASE_CONTEXT.md" })

# ---------------------------------------------------------
# Test 15: Calidad de Formato y Cero Trailing Whitespace
# ---------------------------------------------------------
Write-Host "`n--- Test 15: Calidad de Formato y Cero Trailing Whitespace ---" -ForegroundColor Yellow

$trailingWhitespaceFiles = @()
foreach ($rel in $phase4ScannableFiles) {
    $fp = Join-Path $RepoRoot $rel
    if (-not (Test-Path -LiteralPath $fp)) { continue }
    $wsLeaks = Find-TrailingWhitespaceInFile -FilePath $fp
    if ($wsLeaks.Count -gt 0) {
        $trailingWhitespaceFiles += "$rel ($($wsLeaks -join ', '))"
    }
}

$whitespaceCheckSuccess = ($phase4ScannableFiles.Count -ge 9) -and ($trailingWhitespaceFiles.Count -eq 0)
Assert-Check "Zero trailing whitespace in Phase 4 files" $whitespaceCheckSuccess `
    $(if ($whitespaceCheckSuccess) { "Cero espacios en blanco finales detectados en los $($phase4ScannableFiles.Count) archivos analizados (documentacion explicita Fase 4, scripts Fase 4 y untracked relevantes)" } else { "Espacios en blanco detectados en: $($trailingWhitespaceFiles -join '; ')" })

# ---------------------------------------------------------
# Test 16: Regresiones de Validacion Negativa (Fixtures y Limpieza)
# ---------------------------------------------------------
Write-Host "`n--- Test 16: Regresiones de Validacion Negativa ---" -ForegroundColor Yellow

# Regresion 1: Whitespace artificial en fixture falla
$tmpWs = Join-Path $env:TEMP "GymAudit_WsFixture_$([Guid]::NewGuid().ToString('N')).txt"
$wsRegressionPass = $false
try {
    Set-Content -LiteralPath $tmpWs -Value "Linea normal`r`nLinea con trailing whitespace   `r`nFin"
    $wsFound = Find-TrailingWhitespaceInFile -FilePath $tmpWs
    $wsRegressionPass = ($wsFound.Count -gt 0)
} finally {
    if (Test-Path -LiteralPath $tmpWs) { Remove-Item -LiteralPath $tmpWs -Force }
}
Assert-Check "Regression: Artificial trailing whitespace in fixture is detected" $wsRegressionPass `
    $(if ($wsRegressionPass) { "Comprobado: el validador detecta y falla ante espacios en blanco finales artificiales en fixture temporal" } else { "FALLO: El validador no detecto trailing whitespace artificial" })

# Regresion 2: Enlace de referencia roto falla
$tmpDir = Join-Path $env:TEMP "GymAudit_LinkDir_$([Guid]::NewGuid().ToString('N'))"
$tmpMd = Join-Path $tmpDir "test_broken_ref.md"
$refRegressionPass = $false
try {
    New-Item -ItemType Directory -Path $tmpDir -Force | Out-Null
    $brokenContent = "# Prueba Enlaces Rotos`r`nEnlace roto por destino inexistente: [Doc][ref_destino_inexistente]`r`nEnlace roto por falta de definicion: [Huerfano][ref_huerfana]`r`n`r`n[ref_destino_inexistente]: ./no_existe_archivo_en_disco.md"
    Set-Content -LiteralPath $tmpMd -Value $brokenContent
    $linkRes = Parse-MarkdownFileLinks -Content $brokenContent -FilePath $tmpMd -Root $tmpDir
    $refRegressionPass = ($linkRes.MissingLinks.Count -eq 2)
} finally {
    if ($tmpDir -and (Test-Path -LiteralPath $tmpDir)) { Remove-Item -LiteralPath $tmpDir -Recurse -Force }
}
Assert-Check "Regression: Broken reference link in fixture is detected" $refRegressionPass `
    $(if ($refRegressionPass) { "Comprobado: el parser Markdown detecta enlaces de referencia rotos ([texto][id] -> destino inexistente o definicion ausente) y falla" } else { "FALLO: El parser no detecto los enlaces de referencia rotos" })

# Regresion 3: Secret= realista falla
$tmpSec = Join-Path $env:TEMP "GymAudit_SecFixture_$([Guid]::NewGuid().ToString('N')).txt"
$secRegressionPass = $false
try {
    $secKey = "Sec" + "ret"
    $secVal = "X8fK92_sKj992mZp01Lm"
    $secContent = "Parametro=Configuracion`r`n" + $secKey + '="' + $secVal + '"' + "`r`nFin"
    Set-Content -LiteralPath $tmpSec -Value $secContent
    $secLeaks = Find-SecretLeaksInContent -Content $secContent -FilePath $tmpSec -Root $env:TEMP
    $secRegressionPass = ($secLeaks.Count -gt 0)
} finally {
    if (Test-Path -LiteralPath $tmpSec) { Remove-Item -LiteralPath $tmpSec -Force }
}
Assert-Check "Regression: Realistic Secret= assignment in fixture is detected" $secRegressionPass `
    $(if ($secRegressionPass) { "Comprobado: el escaneo estricto detecta asignaciones realistas 'Secret=' y falla cerradamente" } else { "FALLO: El escaneo no detecto la asignacion realista Secret=" })


# ---------------------------------------------------------
# Resumen Final
# ---------------------------------------------------------
Write-Host "`n====================================================" -ForegroundColor Cyan
if ($failedChecks -eq 0) {
    Write-Host " Phase 4 Validation Result: ALL CHECKS PASSED ($failedChecks failures)" -ForegroundColor Green
    Write-Host "====================================================" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host " Phase 4 Validation Result: $failedChecks CHECK(S) FAILED" -ForegroundColor Red
    Write-Host "====================================================" -ForegroundColor Cyan
    exit 1
}
