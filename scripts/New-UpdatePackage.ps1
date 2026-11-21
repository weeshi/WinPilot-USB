[CmdletBinding()]
param(
    [string[]]$AppIds = @(),
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $root 'release-local' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$stage = Join-Path ([IO.Path]::GetTempPath()) ("WinPilot-Package-" + [Guid]::NewGuid().ToString('N'))

try {
    & (Join-Path $PSScriptRoot 'Build-WinPilot.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    New-Item -ItemType Directory -Force $stage,(Join-Path $stage 'scripts'),$OutputDirectory | Out-Null
    Copy-Item (Join-Path $root 'WinPilot-USB.exe'),(Join-Path $root 'WinPilot-Updater.exe'),(Join-Path $root 'logo.png'),(Join-Path $root 'logo.ico') $stage
    Copy-Item (Join-Path $root 'assets') (Join-Path $stage 'assets') -Recurse
    Copy-Item (Join-Path $root 'scripts\Install-LocalApps.ps1'),(Join-Path $root 'scripts\Disable-Windows-Updates.ps1'),(Join-Path $root 'scripts\MAS_AIO.cmd'),(Join-Path $root 'scripts\local-apps.json') (Join-Path $stage 'scripts')

    if ($AppIds.Count -gt 0) {
        $manifest = Get-Content (Join-Path $root 'scripts\local-apps.json') -Raw | ConvertFrom-Json
        $appsStage = Join-Path $stage 'Apps'; New-Item -ItemType Directory -Force $appsStage | Out-Null
        foreach ($id in $AppIds) {
            $entry = $manifest | Where-Object Id -EQ $id | Select-Object -First 1
            if (-not $entry) { throw "App id not found in local-apps.json: $id" }
            $file = Get-ChildItem (Join-Path $root 'Apps') -File -Recurse -Filter $entry.Pattern | Select-Object -First 1
            if (-not $file) { throw "Installer not found for app id: $id ($($entry.Pattern))" }
            Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $appsStage $file.Name)
        }
    }

    $zip = Join-Path $OutputDirectory 'WinPilot-USB-Update.zip'
    if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal
    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash
    Set-Content -LiteralPath (Join-Path $OutputDirectory 'WinPilot-USB-Update.sha256') -Value "$hash  WinPilot-USB-Update.zip" -Encoding ascii
    Get-Item $zip | Select-Object FullName,Length
    Write-Output "SHA256: $hash"
} finally {
    if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
}
