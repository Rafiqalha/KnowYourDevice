param([switch]$Test)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.x compiler tidak ditemukan. Aktifkan .NET Framework 4.8 di Windows.' }
$output = Join-Path $projectRoot 'build'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$references = @('System.dll','System.Core.dll','System.Management.dll','System.Web.Extensions.dll','System.Xml.dll','System.Xaml.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll')
$compilerArgs = @('/nologo','/target:winexe','/platform:anycpu','/optimize+','/utf8output',('/out:' + (Join-Path $output 'KnowYourDevice.exe')),('/win32manifest:' + (Join-Path $projectRoot 'src\app.manifest')),('/resource:' + (Join-Path $projectRoot 'src\MainWindow.xaml') + ',KnowYourDevice.MainWindow.xaml'))
foreach ($reference in $references) { $compilerArgs += '/reference:' + (Join-Path $framework $reference) }
$compilerArgs += @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
& $compiler @compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Build gagal.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'src\App.config') -Destination (Join-Path $output 'KnowYourDevice.exe.config') -Force
Write-Host "Build berhasil: $output\KnowYourDevice.exe"
if ($Test) {
    $testResult = Join-Path $output 'self-test.json'
    $testProcess = Start-Process -FilePath (Join-Path $output 'KnowYourDevice.exe') -ArgumentList @('--self-test', ('"' + $testResult + '"')) -WindowStyle Hidden -Wait -PassThru
    Get-Content -LiteralPath $testResult
    if ($testProcess.ExitCode -ne 0) { throw 'Self-test gagal.' }
}
