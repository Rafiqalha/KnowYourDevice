$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1') -Test
Push-Location (Join-Path $PSScriptRoot 'desktop')
try {
    & npm.cmd run package
    if ($LASTEXITCODE -ne 0) { throw 'Pembuatan EXE portable gagal.' }
    $version = (Get-Content -LiteralPath '.\package.json' -Raw | ConvertFrom-Json).version
    $binary = Join-Path $PWD ('release\KnowYourDevice-' + $version + '-Windows-x64.exe')
    $destination = Join-Path $PSScriptRoot 'dist'
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    Copy-Item -LiteralPath $binary -Destination $destination -Force
    Get-FileHash -LiteralPath $binary -Algorithm SHA256 | ForEach-Object { $_.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($binary) } | Set-Content -LiteralPath (Join-Path $destination 'SHA256SUMS.txt')
    Get-Item -LiteralPath $binary | Select-Object FullName,Length
} finally { Pop-Location }
