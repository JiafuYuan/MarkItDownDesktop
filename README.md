# MarkItDownDesktop

Windows desktop interface for [MarkItDown](https://github.com/microsoft/markitdown). Drag files into the window, or drop files directly on `MarkItDownDesktop.exe`, then choose an output folder and start the conversion.

The C# application is the interface. `markitdown.exe` is the conversion engine that contains the Python runtime and its dependencies, so end users do not need Python installed. If the engine is missing when the app starts, the app downloads it automatically.

## Download

Grab `MarkItDownDesktop-<version>-win-x64.zip` from the [latest release](https://github.com/JiafuYuan/MarkItDownDesktop/releases/latest), extract it anywhere, and run `MarkItDownDesktop.exe`. Nothing else needs to be installed.

## Build the desktop application

Place the conversion engine at `outputs\markitdown.exe`, then run:

```powershell
.\build_desktop.ps1
```

The self-contained desktop application is written to `outputs\MarkItDownDesktop`. Pass `-EngineVersion 0.1.8` to record the bundled engine version explicitly; without it, the script reads `outputs\engine-update.json`, then falls back to `markitdown.exe --version`.

## Release

One release carries everything: the complete application and the engine assets that the in-app updater needs. Open the repository's **Actions** tab, choose **Release MarkItDown**, click **Run workflow**, and provide:

- `version`: the release version, such as `1.0.0`
- `markitdown_ref`: a Microsoft MarkItDown tag, commit SHA, or `main`

The workflow builds `markitdown.exe` with PyInstaller, builds the self-contained desktop application, and publishes a GitHub Release named `v<version>` containing:

- `MarkItDownDesktop-<version>-win-x64.zip` — the complete application to download
- `markitdown.exe`, `engine-update.json`, `engine.version` — used by the app's **检查引擎更新** button

Every release carries `engine-update.json`, so the app can always read it from `releases/latest` regardless of which release is newest.

For a local engine build, run:

```powershell
.\build_engine.ps1 -SourceRef main -Version 1.0.0
```

This requires Git and Python 3.12. The build writes `markitdown.exe`, `engine-update.json`, and `engine.version` to `artifacts`. `engine.version` is the version the desktop app records and compares against, because `markitdown.exe --version` reports the upstream MarkItDown package version instead.

