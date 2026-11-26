[CmdletBinding()]
param(
    [switch]$Clean,
    [string]$Configuration = 'Release',
    [string]$SignCertificateThumbprint
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'WinPilot-USB.csproj'
$updaterProject = Join-Path $root 'WinPilot-Updater.csproj'
if (-not (Test-Path $project)) { throw "Project file not found: $project" }

& (Join-Path $PSScriptRoot 'Sync-WebCatalog.ps1')

if ($Clean) { dotnet clean $project -c $Configuration; if ($LASTEXITCODE -ne 0) { throw 'dotnet clean failed' } }
dotnet build $project -c $Configuration --nologo --disable-build-servers
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed' }
dotnet build $updaterProject -c $Configuration --nologo --disable-build-servers
if ($LASTEXITCODE -ne 0) { throw 'updater build failed' }

$built = Join-Path $root "bin\$Configuration\net48\WinPilot-USB.exe"
$target = Join-Path $root 'WinPilot-USB.exe'
$updaterBuilt = Join-Path $root "bin\$Configuration\net48\WinPilot-Updater.exe"
$updaterTarget = Join-Path $root 'WinPilot-Updater.exe'
if (-not (Test-Path $built)) { throw "Build output not found: $built" }
Copy-Item -LiteralPath $built -Destination $target -Force
Copy-Item -LiteralPath $updaterBuilt -Destination $updaterTarget -Force

if ($SignCertificateThumbprint) {
    $cert = Get-ChildItem Cert:\CurrentUser\My\$SignCertificateThumbprint -ErrorAction Stop
    if (-not $cert.HasPrivateKey) { throw 'The selected certificate has no private key.' }
    $signtool = Get-ChildItem "$env:ProgramFiles(x86)\Windows Kits\10\bin" -Filter signtool.exe -Recurse -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
    if (-not $signtool) { throw 'signtool.exe was not found. Install the Windows SDK.' }
    foreach ($file in @($target, $updaterTarget)) {
        & $signtool.FullName sign /sha1 $SignCertificateThumbprint /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 $file
        if ($LASTEXITCODE -ne 0) { throw "Authenticode signing failed: $file" }
    }
}

Get-Item $target,$updaterTarget | Select-Object FullName,Length,LastWriteTime
Get-AuthenticodeSignature $target,$updaterTarget | Select-Object Path,Status,StatusMessage
