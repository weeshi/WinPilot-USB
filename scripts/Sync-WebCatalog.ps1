[CmdletBinding()]
param([switch]$Verify)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$catalogPath = Join-Path $root 'scripts\local-apps.json'
$propsPath = Join-Path $root 'Directory.Build.props'
$outputPath = Join-Path $root 'web-app\catalog-data.js'
$versionPath = Join-Path $root 'docs\VERSION.txt'
$manifestPaths = @(
    (Join-Path $root '.work\app.manifest'),
    (Join-Path $root '.work\updater.manifest')
)

[xml]$props = Get-Content -LiteralPath $propsPath -Raw
$version = [string]$props.Project.PropertyGroup.WinPilotProductVersion
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Invalid WinPilotProductVersion in Directory.Build.props: $version" }
$fileVersion = "$version.0"

$categoryOrder = [ordered]@{
    office = @('foxit','googledrive','libreoffice','notepadpp')
    media = @('gom','klite','vlc','poweriso')
    security = @('360','kaspersky-vrt')
    browser = @('brave','chromium','firefox','chrome')
    communication = @('telegram','whatsapp','zoom')
    tools = @('anydesk','crystaldiskinfo','winrar')
    runtime = @('vcredist-x64','vcredist-x86','dotnet-desktop-x64','dotnet-desktop-x86')
}

$source = @(Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json)
$byId = @{}
foreach ($item in $source) {
    if ($byId.ContainsKey([string]$item.Id)) { throw "Duplicate application Id: $($item.Id)" }
    $byId[[string]$item.Id] = $item
}

$webApps = [System.Collections.Generic.List[object]]::new()
foreach ($category in $categoryOrder.Keys) {
    $ids = @($categoryOrder[$category])
    for ($index = 0; $index -lt $ids.Count; $index++) {
        $id = $ids[$index]
        if (-not $byId.ContainsKey($id)) { throw "Web category references missing application Id: $id" }
        $item = $byId[$id]
        $webApps.Add([ordered]@{
            id = $id
            name = [string]$item.Name
            type = ([string]$item.Type).ToUpperInvariant()
            cat = $category
            end = ($index -eq $ids.Count - 1)
        })
        $byId.Remove($id)
    }
}
if ($byId.Count -gt 0) { throw "Applications missing from web category order: $($byId.Keys -join ', ')" }

$payload = [ordered]@{ version = $version; fileVersion = $fileVersion; apps = $webApps }
$json = $payload | ConvertTo-Json -Depth 5 -Compress
$expectedCatalog = "window.WinPilotCatalog=$json;" + [Environment]::NewLine

function Assert-Or-Write([string]$Path, [string]$Expected, [string]$Label) {
    $current = if (Test-Path -LiteralPath $Path) { [IO.File]::ReadAllText($Path) } else { '' }
    if ($Verify) {
        $normalizedCurrent = $current.Replace([Environment]::NewLine, [string][char]10)
        $normalizedExpected = $Expected.Replace([Environment]::NewLine, [string][char]10)
        if ($normalizedCurrent -ne $normalizedExpected) {
            throw "$Label is out of sync. Run .\scripts\Sync-WebCatalog.ps1"
        }
    } else {
        [IO.File]::WriteAllText($Path, $Expected, [Text.UTF8Encoding]::new($false))
    }
}

Assert-Or-Write $outputPath $expectedCatalog 'Web catalog'

foreach ($manifestPath in $manifestPaths) {
    $current = [IO.File]::ReadAllText($manifestPath)
    $expected = [regex]::Replace($current, '(<assemblyIdentity\s+version=")[^"]+(")', { param($match) $match.Groups[1].Value + $fileVersion + $match.Groups[2].Value }, 1)
    Assert-Or-Write $manifestPath $expected "Manifest $([IO.Path]::GetFileName($manifestPath))"
}

$versionCurrent = if (Test-Path -LiteralPath $versionPath) { [IO.File]::ReadAllText($versionPath) } else { throw 'docs/VERSION.txt is missing' }
if ($versionCurrent -notmatch '(?m)^Version:\s*\S+') { throw 'docs/VERSION.txt has no Version field' }
$versionExpected = [regex]::Replace($versionCurrent, '(?m)^Version:\s*\S+', "Version: $fileVersion", 1)
Assert-Or-Write $versionPath $versionExpected 'VERSION.txt'

Write-Host "Catalog synchronized: $($webApps.Count) apps | V$version"

