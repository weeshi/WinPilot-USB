[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$SourcePath,
    [string[]]$Name,
    [switch]$All,
    [switch]$List,
    [switch]$DryRun,
    [switch]$AllowUnknown,
    [switch]$SkipElevation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot
if ([string]::IsNullOrWhiteSpace($SourcePath)) { $SourcePath = $projectRoot }
$manifestPath = Join-Path $scriptRoot 'local-apps.json'
$logDir = Join-Path $scriptRoot 'logs'
New-Item -ItemType Directory -Path $logDir -Force | Out-Null

if (-not (Test-Path -LiteralPath $SourcePath -PathType Container)) {
    throw "Source folder not found: $SourcePath"
}
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Manifest not found: $manifestPath"
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$excludedRoot = (Join-Path $projectRoot 'scripts').TrimEnd('\') + '\'

function Write-InstallEvent {
    param([string]$State, [string]$EntryId, [string]$EntryName, [string]$Message = '')
    Write-Output ("[WINPILOT] {0}|{1}|{2}|{3}" -f $State, $EntryId, $EntryName, $Message)
}

function Get-Matches {
    param([object]$Entry)
    @(Get-ChildItem -LiteralPath $SourcePath -Filter $Entry.Pattern -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notlike "$excludedRoot*" } |
        Sort-Object LastWriteTime -Descending)
}

function Get-DisplayMatches {
    foreach ($entry in $manifest) {
        $matches = @(Get-Matches $entry)
        if ($matches.Count -gt 0) {
            [pscustomobject]@{
                Id = $entry.Id
                Name = $entry.Name
                Type = $entry.Type
                File = $matches[0].FullName
                SizeMB = [math]::Round($matches[0].Length / 1MB, 1)
                SilentArgs = $entry.SilentArgs
            }
        }
    }
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Invoke-LocalInstall {
    param(
        [object]$Entry,
        [System.IO.FileInfo]$Installer
    )

    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $safeId = $Entry.Id -replace '[^A-Za-z0-9_-]', '_'
    $logPath = Join-Path $logDir "$stamp-$safeId.log"
    $description = "$($Entry.Name) from $($Installer.Name)"
    $hashProperty = $Entry.PSObject.Properties['SHA256']
    $expectedHash = if ($hashProperty) { $hashProperty.Value } else { $null }
    if ($expectedHash) {
        $actualHash = (Get-FileHash -LiteralPath $Installer.FullName -Algorithm SHA256).Hash
        if ($actualHash -ne ([string]$expectedHash).ToUpperInvariant()) { throw "Hash verification failed for $($Installer.Name). Expected $expectedHash, got $actualHash" }
        Write-InstallEvent 'VERIFIED' $Entry.Id $Entry.Name 'SHA256 verified'
    } else { Write-Warning "No SHA256 pin configured for $($Entry.Name); integrity was not independently verified." }
    Write-InstallEvent 'INSTALLING' $Entry.Id $Entry.Name $Installer.Name

    if ($Entry.Type -eq 'msixbundle') {
        if ($DryRun) {
            Write-Host "[DRY-RUN] Add-AppxPackage -Path `"$($Installer.FullName)`"" -ForegroundColor Yellow
            return
        }
        if ($PSCmdlet.ShouldProcess($description, 'Install MSIX bundle')) {
            try { Add-AppxPackage -Path $Installer.FullName -ErrorAction Stop 2>&1 | Tee-Object -FilePath $logPath }
            catch { throw "MSIX installation failed; dependencies or a newer Windows build may be required. $($_.Exception.Message)" }
            Write-InstallEvent 'INSTALLED' $Entry.Id $Entry.Name 'exit=0'
            Write-Host "Installed: $($Entry.Name)" -ForegroundColor Green
        }
        return
    }

    if ($Entry.Type -eq 'manual' -or $Entry.Type -eq 'archive') {
        Write-InstallEvent 'FAILED' $Entry.Id $Entry.Name 'This item has no verified silent installer command.'
        throw "Not silently installable: $($Entry.Name). File: $($Installer.FullName)"
    }

    if ($Entry.Type -eq 'msi') {
        $arguments = "/i `"$($Installer.FullName)`" $($Entry.SilentArgs) /L*v `"$logPath`""
        if ($DryRun) {
            Write-Host "[DRY-RUN] msiexec.exe $arguments" -ForegroundColor Yellow
            return
        }
        if ($PSCmdlet.ShouldProcess($description, 'Install MSI silently')) {
            $process = Start-Process -FilePath 'msiexec.exe' -ArgumentList $arguments -Wait -PassThru
            if ($process.ExitCode -notin @(0, 3010)) { throw "MSI failed with exit code $($process.ExitCode): $($Entry.Name)" }
            Write-InstallEvent 'INSTALLED' $Entry.Id $Entry.Name "exit=$($process.ExitCode)"
            Write-Host "Installed: $($Entry.Name) (exit $($process.ExitCode))" -ForegroundColor Green
        }
        return
    }

    if ($Entry.Type -eq 'exe') {
        if ([string]::IsNullOrWhiteSpace([string]$Entry.SilentArgs) -and -not $AllowUnknown) {
            Write-Warning "Skipped $($Entry.Name): no silent arguments configured. Use -AllowUnknown only after verifying the installer."
            return
        }
        $arguments = [string]$Entry.SilentArgs
        if ($DryRun) {
            Write-Host "[DRY-RUN] `"$($Installer.FullName)`" $arguments" -ForegroundColor Yellow
            return
        }
        if ($PSCmdlet.ShouldProcess($description, 'Install EXE silently')) {
            $process = Start-Process -FilePath $Installer.FullName -ArgumentList $arguments -Wait -PassThru
            $process.ExitCode | Set-Content -LiteralPath $logPath
            if ($process.ExitCode -notin @(0, 3010)) { throw "Installer failed with exit code $($process.ExitCode): $($Entry.Name)" }
            Write-InstallEvent 'INSTALLED' $Entry.Id $Entry.Name "exit=$($process.ExitCode)"
            Write-Host "Installed: $($Entry.Name) (exit $($process.ExitCode))" -ForegroundColor Green
        }
        return
    }

    throw "Unsupported installer type '$($Entry.Type)' for $($Entry.Name)"
}

$available = @(Get-DisplayMatches)
if ($List -or (-not $All -and -not $Name)) {
    if ($available.Count -eq 0) {
        Write-Warning "No supported installers were found below $SourcePath"
    } else {
        $available | Format-Table Id,Name,Type,SizeMB,File -AutoSize
        Write-Host "`nUse -All for every listed installer, or -Name id1,id2 for selected apps." -ForegroundColor Cyan
    }
    return
}

if (-not $SkipElevation -and -not (Test-IsAdministrator)) {
    throw 'Administrator privileges are required for silent installation. Re-run PowerShell as Administrator or use -SkipElevation only for per-user packages.'
}

$selected = @(
    if ($All) {
        $manifest
    } else {
        $wanted = @($Name | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim().ToLowerInvariant() } | Where-Object { $_ })
        $manifest | Where-Object { $wanted -contains $_.Id.ToLowerInvariant() -or $wanted -contains $_.Name.ToLowerInvariant() }
    }
)

if ($selected.Count -eq 0) { throw 'No matching apps selected.' }

foreach ($entry in $selected) {
    try {
        $matches = @(Get-Matches $entry)
        if ($matches.Count -eq 0) {
            Write-InstallEvent 'FAILED' $entry.Id $entry.Name "Installer not found: $($entry.Pattern)"
            throw "Installer not found: $($entry.Pattern)"
        }
        Invoke-LocalInstall -Entry $entry -Installer $matches[0]
    } catch {
        Write-InstallEvent 'FAILED' $entry.Id $entry.Name $_.Exception.Message
        throw
    }
}

Write-Host "Completed local installation pass. Logs: $logDir" -ForegroundColor Cyan
