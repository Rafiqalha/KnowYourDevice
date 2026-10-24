param([switch]$Test, [switch]$Legacy)
$ErrorActionPreference = 'Stop'
if ($Legacy) {
    & (Join-Path $PSScriptRoot 'scripts\build-legacy.ps1') -Test:$Test
    exit
}
Push-Location (Join-Path $PSScriptRoot 'desktop')
try {
    if (!(Test-Path -LiteralPath '.\node_modules')) {
        & npm.cmd ci
        if ($LASTEXITCODE -ne 0) { throw 'Instalasi dependency gagal.' }
        & node .\node_modules\electron\install.js
        if ($LASTEXITCODE -ne 0) { throw 'Instalasi Electron gagal.' }
    }
    & npm.cmd run build
    if ($LASTEXITCODE -ne 0) { throw 'Build aplikasi gagal.' }
    if ($Test) {
        & npm.cmd test
        if ($LASTEXITCODE -ne 0) { throw 'Pengujian aplikasi gagal.' }
        & (Join-Path $PSScriptRoot 'scripts\build-legacy.ps1') -Test
    }
} finally { Pop-Location }
