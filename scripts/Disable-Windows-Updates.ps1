[CmdletBinding()]
param([switch]$Restore)

# This script changes Windows Update services and policy. Run as Administrator.
$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$statePath = Join-Path $scriptRoot 'windows-update-state.json'
$registryBackupPath = Join-Path $scriptRoot 'windows-update-registry-backup.json'
$services = @('wuauserv','bits','dosvc','UsoSvc')

function Assert-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    $p = New-Object Security.Principal.WindowsPrincipal($id)
    if (-not $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Run this script as Administrator.' }
}

Assert-Admin

if (-not $Restore) {
    Write-Warning 'This disables Windows Update and may block security updates.'
    if ((Read-Host 'Type DISABLE to continue') -cne 'DISABLE') { throw 'Operation cancelled.' }
}

if ($Restore) {
    if (Test-Path $statePath) {
        $state = Get-Content $statePath -Raw | ConvertFrom-Json
        foreach ($item in $state.Services) {
            try {
                Set-Service -Name $item.Name -StartupType $item.StartupType -ErrorAction Stop
                if ($item.WasRunning -eq $true) { Start-Service -Name $item.Name -ErrorAction SilentlyContinue }
            } catch { Write-Warning "Could not restore $($item.Name): $($_.Exception.Message)" }
        }
        Remove-Item 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU' -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item $registryBackupPath -Force -ErrorAction SilentlyContinue
        Write-Host 'Windows Update settings restored.' -ForegroundColor Green
    } else { Write-Warning "State file not found: $statePath" }
    exit 0
}

$current = foreach ($name in $services) {
    $svc = Get-CimInstance Win32_Service -Filter "Name='$name'" -ErrorAction SilentlyContinue
    if ($svc) { [pscustomobject]@{ Name=$name; StartupType=$svc.StartMode; WasRunning=($svc.State -eq 'Running') } }
}
$current | ConvertTo-Json | Set-Content -LiteralPath $statePath -Encoding UTF8
$au = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU'
$existing = Get-ItemProperty -Path $au -Name NoAutoUpdate -ErrorAction SilentlyContinue
[pscustomobject]@{ Path=$au; Exists=($null -ne $existing); Value=$(if($existing){[int]$existing.NoAutoUpdate}else{0}) } | ConvertTo-Json | Set-Content -LiteralPath $registryBackupPath -Encoding UTF8
foreach ($name in $services) {
    try { Stop-Service -Name $name -Force -ErrorAction SilentlyContinue } catch {}
    try { Set-Service -Name $name -StartupType Disabled -ErrorAction SilentlyContinue } catch {}
}
New-Item -Path $au -Force | Out-Null
New-ItemProperty -Path $au -Name NoAutoUpdate -PropertyType DWord -Value 1 -Force | Out-Null
Write-Host 'Windows Update services were disabled.' -ForegroundColor Yellow
Write-Host "Backup saved to $statePath"
Write-Host 'Run this script with -Restore to restore the previous service state.'
