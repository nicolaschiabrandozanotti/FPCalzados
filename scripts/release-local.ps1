param(
    [switch]$SkipVerification
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $root '.dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }

[xml]$project = Get-Content (Join-Path $root 'src\FPCalzados.App\FPCalzados.App.csproj')
$version = $project.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $version) { throw 'No se encontró Version en FPCalzados.App.csproj.' }

if (-not $SkipVerification) {
    & (Join-Path $PSScriptRoot 'verify.ps1')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$output = Join-Path $root "publish\FPCalzados-$version-win-x64"
$zip = Join-Path $root "publish\FPCalzados-v$version-win-x64.zip"
Remove-Item $output -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $zip -Force -ErrorAction SilentlyContinue

& $dotnet publish (Join-Path $root 'src\FPCalzados.App\FPCalzados.App.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $output
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Compress-Archive "$output\*" $zip
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $(Split-Path $zip -Leaf)" | Set-Content (Join-Path $root 'publish\SHA256SUMS.txt')
Write-Host "Release local v$version creada: $zip" -ForegroundColor Green
