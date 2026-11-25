[CmdletBinding()]
param(
    [string]$ExecutablePath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'WinPilot-USB.exe'),
    [switch]$Unregister
)

$ErrorActionPreference = 'Stop'
$key = 'HKCU:\Software\Classes\winpilot'
if ($Unregister) {
    if (Test-Path -LiteralPath $key) { Remove-Item -LiteralPath $key -Recurse -Force }
    Write-Host 'WinPilot protocol removed for the current user.'
    return
}
$resolved = (Resolve-Path -LiteralPath $ExecutablePath).Path
New-Item -Path $key -Force | Out-Null
Set-Item -Path $key -Value 'URL:WinPilot Selection Protocol'
New-ItemProperty -Path $key -Name 'URL Protocol' -Value '' -PropertyType String -Force | Out-Null
$commandKey = Join-Path $key 'shell\open\command'
New-Item -Path $commandKey -Force | Out-Null
Set-Item -Path $commandKey -Value ('"{0}" "%1"' -f $resolved)
Write-Host "WinPilot protocol registered for the current user: $resolved"

