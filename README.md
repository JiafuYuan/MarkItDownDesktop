# MarkItDownDesktop

Windows desktop interface for [MarkItDown](https://github.com/microsoft/markitdown). Drag files into the window, or drop files directly on `MarkItDownDesktop.exe`, then choose an output folder and start the conversion.

The C# application is the interface. `markitdown.exe` is a separately versioned conversion engine that contains the Python runtime and its dependencies, so end users do not need Python installed.

## Build the desktop application

Place the conversion engine at `outputs\markitdown.exe`, then run:

```powershell
.\build_desktop.ps1
```

The self-contained desktop application is written to `outputs\MarkItDownDesktop`.

## Release an engine update

The desktop app checks this repository's latest GitHub Release for `engine-update.json`. The manifest contains the engine version, its download URL, and a SHA-256 hash. The app validates the hash before replacing its local `markitdown.exe`.

To publish a new engine version, open the repository's **Actions** tab, choose **Release MarkItDown engine**, click **Run workflow**, and provide:

- `version`: a new engine version, such as `0.1.8`
- `markitdown_ref`: a Microsoft MarkItDown tag, commit SHA, or `main`

The workflow builds `markitdown.exe`, creates `engine-update.json`, and publishes both files in a GitHub Release named `engine-v<version>`. The desktop app's **检查引擎更新** button then downloads that release directly from GitHub.

For a local engine build, run:

```powershell
.\build_engine.ps1 -SourceRef main -Version 0.1.8
```

This requires Git and Python 3.12. The GitHub Actions workflow installs Python automatically.
