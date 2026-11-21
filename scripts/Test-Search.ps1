$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
$root = [string](Split-Path -Parent $PSScriptRoot)
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $root 'bin\Release\net48\WinPilot-USB.exe'))
$type = $assembly.GetType('MainForm')
$flags = [Reflection.BindingFlags]'Instance,NonPublic'
$form = [Activator]::CreateInstance($type, [object[]]@($root))
try {
    $null = $form.Handle
    $null = $type.GetMethod('LoadApps', $flags).Invoke($form, @())
    $search = $type.GetField('search', $flags).GetValue($form)
    $list = $type.GetField('appList', $flags).GetValue($form)
    $total = $list.Items.Count
    if ($search.Text -ne '' -or $total -lt 1) { throw 'Initial search/list failed' }
    $search.Text = ' vLc '
    if ($list.Items.Count -ne 1 -or $list.Items[0].Id -ne 'vlc') { throw 'English filter failed' }
    $list.SetItemChecked(0, $true)
    [Windows.Forms.Application]::DoEvents()
    $search.Text = 'الحماية'
    if ($list.Items.Count -lt 1) { throw 'Arabic category filter failed' }
    $selected = $type.GetMethod('SelectedApps', $flags).Invoke($form, @())
    if ($selected.Count -ne 1 -or $selected[0].Id -ne 'vlc') { throw 'Hidden selection lost' }
    $search.Text = 'no-such-app-xyz'
    if ($list.Items.Count -ne 0) { throw 'No matches failed' }
    $search.Text = ''
    if ($list.Items.Count -ne $total -or $list.CheckedItems.Count -ne 1) { throw 'Reset failed' }
    if ($list.Items[0].Category -ne 'مكتبية') { throw 'Category order failed' }
    $null = $type.GetMethod('SetAllChecked', $flags).Invoke($form, @($false))
    if ($list.CheckedItems.Count -ne 0) { throw 'Clear failed' }
    Write-Output "PASS: empty search, English/Arabic filtering, hidden selections, no matches, reset, category order, clear ($total apps)."
} finally { $form.Dispose() }
