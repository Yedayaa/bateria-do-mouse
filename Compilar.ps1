$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) { throw 'Compilador do .NET Framework não encontrado.' }
Push-Location -LiteralPath $PSScriptRoot
try {
    foreach ($directory in @('build-tools','release','dist')) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
    & $compiler /nologo /reference:System.Drawing.dll /out:build-tools\MakeIcon.exe MakeIcon.cs
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o gerador de ícone.' }
    & '.\build-tools\MakeIcon.exe' 'BateriaMouse.ico'
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao gerar o ícone.' }
    & $compiler /nologo /target:winexe /optimize+ /win32icon:BateriaMouse.ico /win32manifest:app.manifest /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /out:release\BateriaMouse.exe AssemblyInfo.cs NativeStyle.cs HidBattery.cs BatteryAlerts.cs TrayApp.cs
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o aplicativo.' }
    Copy-Item -LiteralPath 'BateriaMouse.exe.config' -Destination 'release\BateriaMouse.exe.config' -Force
    & $compiler /nologo /out:build-tools\Verify.exe HidBattery.cs BatteryAlerts.cs Verify.cs
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar os testes.' }
    & '.\build-tools\Verify.exe'
    if ($LASTEXITCODE -ne 0) { throw 'Falha nos testes.' }
    & $compiler /nologo /out:build-tools\Probe.exe HidBattery.cs Probe.cs
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o diagnóstico.' }
    & $compiler /nologo /target:winexe /optimize+ /win32icon:BateriaMouse.ico /win32manifest:app.manifest /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /resource:release\BateriaMouse.exe,App.exe /resource:BateriaMouse.exe.config,App.config /resource:Desinstalar.ps1,Uninstall.ps1 /resource:README.md,README.md /out:release\Instalar.exe AssemblyInfo.cs Installer.cs
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o instalador.' }
    Copy-Item -LiteralPath 'README.md' -Destination 'release\LEIA-ME.md' -Force
    Copy-Item -LiteralPath 'LICENSE' -Destination 'release\LICENSE' -Force
    $packageFiles = @('Instalar.exe','BateriaMouse.exe','BateriaMouse.exe.config','LEIA-ME.md','LICENSE') | ForEach-Object { Join-Path 'release' $_ }
    Compress-Archive -LiteralPath $packageFiles -DestinationPath 'dist\BateriaMouse-v1.3.1-windows.zip' -Force
    $packageHash = Get-FileHash -LiteralPath 'dist\BateriaMouse-v1.3.1-windows.zip' -Algorithm SHA256
    ($packageHash.Hash.ToLowerInvariant() + '  BateriaMouse-v1.3.1-windows.zip') | Set-Content -LiteralPath 'dist\SHA256SUMS.txt' -Encoding ascii
    Write-Output 'Compilação e testes concluídos. Pacote disponível em dist\BateriaMouse-v1.3.1-windows.zip.'
} finally { Pop-Location }
