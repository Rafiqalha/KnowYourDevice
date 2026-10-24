param([switch]$Test)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$output = Join-Path $projectRoot 'build\legacy'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$compilerArgs = @('/nologo','/target:winexe','/platform:anycpu','/optimize+','/utf8output',('/out:' + (Join-Path $output 'KnowYourDevice.exe')),('/win32manifest:' + (Join-Path $projectRoot 'src\app.manifest')),('/resource:' + (Join-Path $projectRoot 'src\MainWindow.xaml') + ',KnowYourDevice.MainWindow.xaml'))
foreach ($reference in @('System.dll','System.Core.dll','System.Management.dll','System.Web.Extensions.dll','System.Xml.dll','System.Xaml.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll')) { $compilerArgs += '/reference:' + (Join-Path $framework $reference) }
$compilerArgs += @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' | Where-Object Name -ne 'HardwareBridge.cs' | ForEach-Object FullName)
& (Join-Path $framework 'csc.exe') @compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Build WPF diagnostik gagal.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'src\App.config') -Destination (Join-Path $output 'KnowYourDevice.exe.config') -Force
if ($Test) {
    $result = Join-Path $output 'self-test.json'
    $process = Start-Process -FilePath (Join-Path $output 'KnowYourDevice.exe') -ArgumentList @('--self-test', ('"' + $result + '"')) -WindowStyle Hidden -Wait -PassThru
    Get-Content -LiteralPath $result
    if ($process.ExitCode -ne 0) { throw 'Self-test native gagal.' }
}
