[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $PSScriptRoot 'local-apps.json'
$apps = Join-Path $root 'Apps'
$parsedManifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
while ($parsedManifest -and $parsedManifest.Count -eq 1 -and $parsedManifest[0].PSObject.Properties['value'] -and -not $parsedManifest[0].PSObject.Properties['Id']) { $parsedManifest = $parsedManifest[0].value }
$manifest = @($parsedManifest)
foreach ($entry in $manifest) {
    $file = Get-ChildItem $apps -Recurse -File | Where-Object { $_.Name -like $entry.Pattern } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($file) {
        $hash = (Get-FileHash $file.FullName -Algorithm SHA256).Hash
        $property = $entry.PSObject.Properties['SHA256']
        if ($property) { $property.Value = $hash } else { $entry | Add-Member -NotePropertyName SHA256 -NotePropertyValue $hash }
        Write-Host "$($entry.Id): $hash"
    }
}
ConvertTo-Json -InputObject $manifest -Depth 5 | Set-Content $manifestPath -Encoding UTF8
