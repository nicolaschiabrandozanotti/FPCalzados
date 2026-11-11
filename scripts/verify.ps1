$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $root '.dotnet\dotnet.exe'
$solution = Join-Path $root 'FPCalzados.slnx'

if (-not (Test-Path $dotnet)) {
    $dotnet = 'dotnet'
}

& $dotnet restore $solution
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $dotnet build $solution -c Release --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $dotnet test $solution -c Release --no-restore --no-build --blame-hang-timeout 60s
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$vulnerabilities = & $dotnet list $solution package --vulnerable --include-transitive --no-restore 2>&1 | Out-String
Write-Host $vulnerabilities
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($vulnerabilities -match '(?im)^\s*>\s+\S+') {
    Write-Error 'Se detectaron paquetes vulnerables.'
    exit 1
}

Write-Host 'Verificacion completa: OK' -ForegroundColor Green
