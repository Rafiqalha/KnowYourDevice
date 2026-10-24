$ErrorActionPreference = 'Stop'
$binary = Join-Path $PSScriptRoot 'build\KnowYourDevice.exe'
if (!(Test-Path -LiteralPath $binary)) { throw 'Jalankan build.ps1 terlebih dahulu.' }
$portable = Join-Path $PSScriptRoot 'dist\KnowYourDevice-Portable'
New-Item -ItemType Directory -Force -Path $portable | Out-Null
Copy-Item -LiteralPath $binary -Destination $portable -Force
Copy-Item -LiteralPath ($binary + '.config') -Destination $portable -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination (Join-Path $portable 'PANDUAN.md') -Force
$archive = Join-Path $PSScriptRoot 'dist\KnowYourDevice-Portable.zip'
Compress-Archive -Path (Join-Path $portable '*') -DestinationPath $archive -Force
Get-FileHash -LiteralPath $archive -Algorithm SHA256 | ForEach-Object { $_.Hash + '  KnowYourDevice-Portable.zip' } | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'dist\SHA256.txt')
Get-Item -LiteralPath $archive | Select-Object FullName,Length
