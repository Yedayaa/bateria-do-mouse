$ErrorActionPreference = 'Stop'
$expectedDirectory = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\Bateria do Mouse'
if ([IO.Path]::GetFullPath($PSScriptRoot) -ne [IO.Path]::GetFullPath($expectedDirectory)) { throw 'Unexpected application directory.' }
if ((Get-Item -LiteralPath $expectedDirectory).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Unexpected directory link.' }
$appExecutable = Join-Path $expectedDirectory 'BateriaMouse.exe'
Get-Process -Name BateriaMouse -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $appExecutable } | Stop-Process -ErrorAction Stop
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$runCommand = (Get-ItemProperty -LiteralPath $runKey -Name K1NGProBattery -ErrorAction SilentlyContinue).K1NGProBattery
if ($runCommand -eq ('"' + $appExecutable + '" --background')) { Remove-ItemProperty -LiteralPath $runKey -Name K1NGProBattery }
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Programs')) 'Bateria do mouse.lnk'
if (Test-Path -LiteralPath $shortcutPath) {
    $shortcutShell = New-Object -ComObject WScript.Shell
    $shortcut = $shortcutShell.CreateShortcut($shortcutPath)
    if ($shortcut.TargetPath -eq $appExecutable) { Remove-Item -LiteralPath $shortcutPath }
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shortcut)
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shortcutShell)
}
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\BateriaDoMouse'
if ((Get-ItemProperty -LiteralPath $uninstallKey -Name InstallLocation -ErrorAction SilentlyContinue).InstallLocation -eq $expectedDirectory) {
    Remove-Item -LiteralPath $uninstallKey
}
# Only this application's known files are removed; no recursive filesystem deletion.
foreach ($fileName in @('BateriaMouse.exe','BateriaMouse.exe.previous','BateriaMouse.exe.config','LEIA-ME.md','Desinstalar.ps1')) {
    $filePath = Join-Path $expectedDirectory $fileName
    if (Test-Path -LiteralPath $filePath -PathType Leaf) { Remove-Item -LiteralPath $filePath }
}
if (@(Get-ChildItem -LiteralPath $expectedDirectory -Force).Count -eq 0) { Remove-Item -LiteralPath $expectedDirectory }
