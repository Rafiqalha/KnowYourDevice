$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$output = Join-Path $projectRoot 'build\native'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$compilerArgs = @('/nologo','/target:exe','/platform:anycpu','/optimize+','/utf8output',('/out:' + (Join-Path $output 'HardwareBridge.exe')),('/win32manifest:' + (Join-Path $projectRoot 'src\app.manifest')))
foreach ($reference in @('System.dll','System.Core.dll','System.Management.dll','System.Web.Extensions.dll')) { $compilerArgs += '/reference:' + (Join-Path $framework $reference) }
foreach ($source in @('Models.cs','HardwareScanner.cs','ReportWriter.cs','HardwareBridge.cs')) { $compilerArgs += Join-Path $projectRoot ('src\' + $source) }
& (Join-Path $framework 'csc.exe') @compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Build hardware bridge gagal.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'src\App.config') -Destination (Join-Path $output 'HardwareBridge.exe.config') -Force
Write-Host 'Hardware bridge siap.'
