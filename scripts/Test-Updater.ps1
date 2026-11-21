$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = Split-Path -Parent $PSScriptRoot
$updater = Join-Path $root 'bin\Release\net48\WinPilot-Updater.exe'
if (-not (Test-Path -LiteralPath $updater)) { throw "Build updater first: $updater" }

$testRoot = Join-Path ([IO.Path]::GetTempPath()) ("WinPilot-Updater-Test-" + [Guid]::NewGuid().ToString('N'))
$target = Join-Path $testRoot 'target'
$source = Join-Path $testRoot 'source'
$package = Join-Path $testRoot 'update.zip'
try {
    New-Item -ItemType Directory -Force (Join-Path $target 'scripts'),(Join-Path $source 'scripts') | Out-Null
    Set-Content -LiteralPath (Join-Path $target 'WinPilot-USB.exe') -Value 'old-app' -Encoding ascii
    Set-Content -LiteralPath (Join-Path $target 'scripts\local-apps.json') -Value 'old-catalog' -Encoding ascii
    Set-Content -LiteralPath (Join-Path $target 'info.txt') -Value 'keep-user-info' -Encoding ascii
    Set-Content -LiteralPath (Join-Path $source 'WinPilot-USB.exe') -Value 'new-app' -Encoding ascii
    Set-Content -LiteralPath (Join-Path $source 'scripts\local-apps.json') -Value 'new-catalog' -Encoding ascii
    Compress-Archive -Path (Join-Path $source '*') -DestinationPath $package

    $process = Start-Process -FilePath $updater -ArgumentList @('--package',('"' + $package + '"'),'--target',('"' + $target + '"'),'--pid','0','--restart','WinPilot-USB.exe','--no-restart','--silent') -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Updater exited with $($process.ExitCode)" }
    if ((Get-Content (Join-Path $target 'WinPilot-USB.exe') -Raw).Trim() -ne 'new-app') { throw 'Application was not replaced' }
    if ((Get-Content (Join-Path $target 'scripts\local-apps.json') -Raw).Trim() -ne 'new-catalog') { throw 'Catalog was not replaced' }
    if ((Get-Content (Join-Path $target 'info.txt') -Raw).Trim() -ne 'keep-user-info') { throw 'User file was changed' }

    $rollbackTarget = Join-Path $testRoot 'rollback-target'; New-Item -ItemType Directory -Force $rollbackTarget | Out-Null
    Set-Content -LiteralPath (Join-Path $rollbackTarget 'WinPilot-USB.exe') -Value 'rollback-old-app' -Encoding ascii
    Set-Content -LiteralPath (Join-Path $rollbackTarget 'locked.txt') -Value 'locked-old' -Encoding ascii
    (Get-Item (Join-Path $rollbackTarget 'locked.txt')).IsReadOnly = $true
    $rollbackPackage = Join-Path $testRoot 'rollback.zip'
    $stream = [IO.File]::Open($rollbackPackage, [IO.FileMode]::CreateNew)
    try {
        $archive = New-Object IO.Compression.ZipArchive($stream, [IO.Compression.ZipArchiveMode]::Create)
        try {
            foreach ($pair in @(@('WinPilot-USB.exe','rollback-new-app'),@('locked.txt','locked-new'))) {
                $entry = $archive.CreateEntry($pair[0]); $writer = New-Object IO.StreamWriter($entry.Open())
                try { $writer.Write($pair[1]) } finally { $writer.Dispose() }
            }
        } finally { $archive.Dispose() }
    } finally { $stream.Dispose() }
    $rollbackProcess = Start-Process -FilePath $updater -ArgumentList @('--package',('"' + $rollbackPackage + '"'),'--target',('"' + $rollbackTarget + '"'),'--pid','0','--restart','WinPilot-USB.exe','--no-restart','--silent') -Wait -PassThru
    if ($rollbackProcess.ExitCode -eq 0) { throw 'Expected replacement failure was not detected' }
    if ((Get-Content (Join-Path $rollbackTarget 'WinPilot-USB.exe') -Raw).Trim() -ne 'rollback-old-app') { throw 'Rollback did not restore the original application' }
    (Get-Item (Join-Path $rollbackTarget 'locked.txt')).IsReadOnly = $false

    $badTarget = Join-Path $testRoot 'bad-target'; New-Item -ItemType Directory -Force $badTarget | Out-Null
    $badPackage = Join-Path $testRoot 'bad.zip'
    $stream = [IO.File]::Open($badPackage, [IO.FileMode]::CreateNew)
    try {
        $archive = New-Object IO.Compression.ZipArchive($stream, [IO.Compression.ZipArchiveMode]::Create)
        try {
            $entry = $archive.CreateEntry('../escaped.txt')
            $writer = New-Object IO.StreamWriter($entry.Open()); try { $writer.Write('unsafe') } finally { $writer.Dispose() }
        } finally { $archive.Dispose() }
    } finally { $stream.Dispose() }
    $badProcess = Start-Process -FilePath $updater -ArgumentList @('--package',('"' + $badPackage + '"'),'--target',('"' + $badTarget + '"'),'--pid','0','--restart','WinPilot-USB.exe','--no-restart','--silent') -Wait -PassThru
    if ($badProcess.ExitCode -eq 0) { throw 'Unsafe ZIP was accepted' }
    if (Test-Path (Join-Path $testRoot 'escaped.txt')) { throw 'ZIP traversal escaped the staging directory' }
    Write-Output 'PASS: replacement, user-file preservation, rollback, and ZIP traversal rejection.'
} finally {
    if (Test-Path -LiteralPath $testRoot) { Remove-Item -LiteralPath $testRoot -Recurse -Force }
}
