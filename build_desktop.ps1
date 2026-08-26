$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "MarkItDownDesktop.csproj"
$output = Join-Path $PSScriptRoot "outputs\MarkItDownDesktop"
$engine = Join-Path $PSScriptRoot "outputs\markitdown.exe"

if (-not (Test-Path $engine)) { throw "Missing conversion engine: $engine" }

dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -o $output
Copy-Item -LiteralPath $engine -Destination (Join-Path $output "markitdown.exe") -Force
Write-Host "Created: $(Join-Path $output 'MarkItDownDesktop.exe')"
