$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $repo
New-Item -ItemType Directory -Force -Path '.tools' | Out-Null
if (-not (Test-Path -LiteralPath '.tools/dotnet/dotnet.exe')) {
    Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile '.tools/dotnet-install.ps1'
    & '.tools/dotnet-install.ps1' -Version 10.0.404 -InstallDir "$repo\.tools\dotnet" -NoPath
}
if (-not (Test-Path -LiteralPath '.tools/package.json')) {
    '{"name":"metermind-local-tools","private":true,"type":"module","dependencies":{"embedded-postgres":"18.7.0-beta.19"}}' | Set-Content -LiteralPath '.tools/package.json'
}
& npm.cmd install --prefix .tools
if ($LASTEXITCODE -ne 0) { throw 'Local PostgreSQL tool installation failed' }
& npm.cmd ci --prefix frontend
if ($LASTEXITCODE -ne 0) { throw 'Frontend dependency installation failed' }
& '.\.tools\dotnet\dotnet.exe' restore MeterMind.slnx --locked-mode
if ($LASTEXITCODE -ne 0) { throw 'Locked .NET restore failed' }
Write-Output 'Local toolchain ready. Start node scripts/local-postgres.mjs in a separate terminal, then follow README.md.'
