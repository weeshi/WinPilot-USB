[CmdletBinding()]
param(
    [switch]$Clean,
    [string]$Configuration = 'Release',
    [string]$SignCertificateThumbprint
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'WinPilot-USB.csproj'
if (-not (Test-Path $project)) { throw "Project file not found: $project" }

if ($Clean) { dotnet clean $project -c $Configuration; if ($LASTEXITCODE -ne 0) { throw 'dotnet clean failed' } }
dotnet build $project -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed' }

$built = Join-Path $root "bin\$Configuration\net48\WinPilot-USB.exe"
$target = Join-Path $root 'WinPilot-USB.exe'
if (-not (Test-Path $built)) { throw "Build output not found: $built" }
Copy-Item -LiteralPath $built -Destination $target -Force

if ($SignCertificateThumbprint) {
    $cert = Get-ChildItem Cert:\CurrentUser\My\$SignCertificateThumbprint -ErrorAction Stop
    if (-not $cert.HasPrivateKey) { throw 'The selected certificate has no private key.' }
    $signtool = Get-ChildItem "$env:ProgramFiles(x86)\Windows Kits\10\bin" -Filter signtool.exe -Recurse -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
    if (-not $signtool) { throw 'signtool.exe was not found. Install the Windows SDK.' }
    & $signtool.FullName sign /sha1 $SignCertificateThumbprint /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 $target
    if ($LASTEXITCODE -ne 0) { throw 'Authenticode signing failed.' }
}

Get-Item $target | Select-Object FullName,Length,LastWriteTime
Get-AuthenticodeSignature $target | Select-Object Status,StatusMessage
