param(
    [string]$EngineVersion,
    [string]$AppVersion
)

$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "MarkItDownDesktop.csproj"
$output = Join-Path $PSScriptRoot "outputs\MarkItDownDesktop"
$engine = Join-Path $PSScriptRoot "outputs\markitdown.exe"

if (-not (Test-Path $engine)) { throw "Missing conversion engine: $engine" }

$publishArgs = @($project, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-p:PublishSingleFile=true', '-p:DebugType=None', '-o', $output)
if ($AppVersion) { $publishArgs += "-p:Version=$AppVersion" }
dotnet publish @publishArgs
Copy-Item -LiteralPath $engine -Destination (Join-Path $output "markitdown.exe") -Force

if (-not $EngineVersion) {
    $manifest = Join-Path $PSScriptRoot "outputs\engine-update.json"
    if (Test-Path $manifest) { $EngineVersion = (Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json).version }
}
if (-not $EngineVersion) {
    $reported = (& $engine --version 2>$null) -join "`n"
    if ($reported -match '(\d+\.\d+(?:\.\d+)?)') { $EngineVersion = $Matches[1] }
}
if ($EngineVersion) {
    $EngineVersion | Set-Content -LiteralPath (Join-Path $output "engine.version") -Encoding utf8
    Write-Host "Bundled engine version: $EngineVersion"
}
else {
    Write-Warning "Could not determine the engine version. engine.version was not written."
}
if ($AppVersion) { Write-Host "Application version: $AppVersion" }

Write-Host "Created: $(Join-Path $output 'MarkItDownDesktop.exe')"
