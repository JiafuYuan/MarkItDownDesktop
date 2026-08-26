param(
    [string]$SourceRef = "main",
    [string]$Version = "0.1.7",
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "artifacts"),
    [switch]$RefreshSource
)

$ErrorActionPreference = "Stop"
$sourceDir = Join-Path $PSScriptRoot "work\markitdown"
$venvDir = Join-Path $sourceDir ".venv-build"
$python = Join-Path $venvDir "Scripts\python.exe"

if (-not (Test-Path (Join-Path $sourceDir ".git"))) {
    New-Item -ItemType Directory -Force -Path (Split-Path $sourceDir) | Out-Null
    git clone https://github.com/microsoft/markitdown.git $sourceDir
}

Push-Location $sourceDir
git fetch --tags --force origin
git checkout --force $SourceRef
if ($RefreshSource) { git pull --ff-only origin $SourceRef }
Pop-Location

if (-not (Test-Path $python)) {
    py -3.12 -m venv $venvDir
}

& $python -m pip install --upgrade pip
Push-Location (Join-Path $sourceDir "packages\markitdown")
& $python -m pip install ".[all]" pyinstaller
Pop-Location

$launcher = Join-Path $sourceDir "build\markitdown_launcher.py"
New-Item -ItemType Directory -Force -Path (Split-Path $launcher) | Out-Null
@'
from markitdown.__main__ import main

if __name__ == "__main__":
    main()
'@ | Set-Content -LiteralPath $launcher -Encoding utf8

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
& $python -m PyInstaller --noconfirm --clean --onefile --name markitdown --distpath $OutputDirectory --workpath (Join-Path $sourceDir "build\pyinstaller") --specpath (Join-Path $sourceDir "build\pyinstaller") --paths (Join-Path $sourceDir "packages\markitdown\src") --collect-all magika --copy-metadata markitdown $launcher
if ($LASTEXITCODE -ne 0) { throw "PyInstaller failed with exit code $LASTEXITCODE" }

$engine = Join-Path $OutputDirectory "markitdown.exe"
$sha256 = (Get-FileHash -LiteralPath $engine -Algorithm SHA256).Hash.ToLowerInvariant()
@{ version = $Version; url = "https://github.com/JiafuYuan/MarkItDownDesktop/releases/download/engine-v$Version/markitdown.exe"; sha256 = $sha256 } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory "engine-update.json") -Encoding utf8
Write-Host "Created: $engine"
