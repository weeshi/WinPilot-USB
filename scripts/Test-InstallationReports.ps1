$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
$root = [string](Split-Path -Parent $PSScriptRoot)
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $root 'bin\Release\net48\WinPilot-USB.exe'))
$formType = $assembly.GetType('MainForm')
$resultType = $assembly.GetType('InstallResult')
$flags = [Reflection.BindingFlags]'Instance,NonPublic'
$form = [Activator]::CreateInstance($formType, [object[]]@($root))
$htmlPath = $null
$jsonPath = $null
try {
    $result = [Activator]::CreateInstance($resultType)
    $resultType.GetField('Id').SetValue($result, 'test-app')
    $resultType.GetField('Name').SetValue($result, 'Test Application')
    $resultType.GetField('Result').SetValue($result, 'Succeeded')
    $resultType.GetField('Message').SetValue($result, 'Test result')
    $resultType.GetField('ExitCode').SetValue($result, 0)
    $resultType.GetField('StartedAt').SetValue($result, [DateTime]::UtcNow.ToString('o'))
    $resultType.GetField('FinishedAt').SetValue($result, [DateTime]::UtcNow.ToString('o'))
    $results = $formType.GetField('installResults', $flags).GetValue($form)
    $results.Add($result)
    $formType.GetField('totalInstallCount', $flags).SetValue($form, 1)
    $formType.GetField('installStartedAt', $flags).SetValue($form, [DateTime]::Now)
    $null = $formType.GetMethod('WriteInstallationReports', $flags).Invoke($form, @('Test'))
    $htmlPath = [string]$formType.GetField('lastReportPath', $flags).GetValue($form)
    $jsonPath = [IO.Path]::ChangeExtension($htmlPath, '.json')
    if (-not (Test-Path -LiteralPath $htmlPath) -or -not (Test-Path -LiteralPath $jsonPath)) { throw 'Report files were not created.' }
    if ((Get-Content -LiteralPath $htmlPath -Raw) -notmatch 'Test Application') { throw 'HTML report is missing the test result.' }
    $json = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
    if ($json.productVersion -ne '1.3.1.0' -or $json.results.Count -ne 1 -or $json.results[0].Result -ne 'Succeeded') { throw 'JSON report content is invalid.' }
    Write-Output "PASS: HTML and JSON installation reports created and validated for V$($json.productVersion)."
} finally {
    $form.Dispose()
    if ($htmlPath -and (Test-Path -LiteralPath $htmlPath)) { Remove-Item -LiteralPath $htmlPath -Force }
    if ($jsonPath -and (Test-Path -LiteralPath $jsonPath)) { Remove-Item -LiteralPath $jsonPath -Force }
}

