# 打包与发布

本仓库产出**两个相互独立的产物**，各有自己的版本号：

| 产物 | 内容 | 版本号来源 | 发布形式 |
| --- | --- | --- | --- |
| 转换引擎 `markitdown.exe` | Python 运行时 + MarkItDown 及其全部依赖，单文件 | `engine-v<version>` | GitHub Release `engine-v<version>` |
| 桌面程序 `MarkItDownDesktop.exe` | C# 界面，自包含 .NET 运行时，单文件 | `desktop-v<version>` | GitHub Release `desktop-v<version>` 中的 zip |

桌面程序**不自带**引擎，它要求 `markitdown.exe` 与自身位于同一目录，并可以联网单独更新该引擎。因此打包桌面程序前必须先准备好引擎。

以下命令均假设当前目录为仓库根目录。

## 1. 环境要求

| 用途 | 要求 |
| --- | --- |
| 打包桌面程序 | .NET SDK 8.0 或更高（项目目标为 `net8.0-windows`，用 SDK 10 构建同样正常） |
| 本地构建引擎 | Git、Python 3.12，且 Windows 上有 `py` 启动器（脚本使用 `py -3.12 -m venv`） |
| 单独下载引擎 | 无需任何本地环境，只要能访问 GitHub |
| 网络 | 打包桌面程序时需要下载引擎或已本地备好引擎；本地构建引擎需要拉取 PyPI 依赖 |

## 2. 第一步：准备引擎

### 方式 A：下载已发布的引擎（推荐，最快）

```powershell
New-Item -ItemType Directory -Force outputs | Out-Null
$base = 'https://github.com/JiafuYuan/MarkItDownDesktop/releases/download/engine-v0.1.8'
Invoke-WebRequest "$base/markitdown.exe" -OutFile outputs\markitdown.exe
Invoke-WebRequest "$base/engine-update.json" -OutFile outputs\engine-update.json
```

务必校验哈希，避免拿到不完整的下载：

```powershell
$manifest = Get-Content outputs\engine-update.json -Raw | ConvertFrom-Json
(Get-FileHash outputs\markitdown.exe -Algorithm SHA256).Hash.ToLowerInvariant() -eq $manifest.sha256   # 应为 True
```

### 方式 B：本地从源码构建引擎

```powershell
.\build_engine.ps1 -SourceRef main -Version 0.1.9
```

可选参数：`-OutputDirectory`（默认 `artifacts`）、`-RefreshSource`（对已 clone 的源码执行 `git pull --ff-only`）。

脚本流程：clone/更新 `microsoft/markitdown` 到 `work\markitdown` → 在 `work\markitdown\.venv-build` 建虚拟环境 → `pip install ".[all]" pyinstaller` → 生成入口脚本 → PyInstaller `--onefile` 打包。

产物（默认 `artifacts\`）：

- `markitdown.exe`：转换引擎本体
- `engine-update.json`：更新清单，含 `version`、`url`、`sha256`
- `engine.version`：纯文本版本号，供桌面程序判定版本（见第 6 节）

本地构建完成后，把产物放到桌面打包脚本期望的位置：

```powershell
Copy-Item artifacts\markitdown.exe,artifacts\engine-update.json outputs\ -Force
```

### 方式 C：用 GitHub Actions 构建引擎

仓库 **Actions** → **Release MarkItDown engine** → **Run workflow**，填写：

- `version`：新的引擎版本，如 `0.1.9`
- `markitdown_ref`：Microsoft MarkItDown 的 tag、commit SHA 或 `main`

工作流在 `windows-latest` 上构建，并发布 `engine-v<version>`，包含 `markitdown.exe`、`engine-update.json`、`engine.version` 三个资产，同时标记为 **latest**（这一点很重要，见第 5 节）。

## 3. 第二步：打包桌面程序

```powershell
.\build_desktop.ps1 -EngineVersion 0.1.8
```

`-EngineVersion` 可省略，省略时按以下顺序推断：`outputs\engine-update.json` 的 `version` → `markitdown.exe --version` 输出中的版本号 → 都拿不到则只打印警告、不写 `engine.version`。

脚本要求 `outputs\markitdown.exe` 已存在，否则直接报错 `Missing conversion engine`。

实际执行的发布命令：

```powershell
dotnet publish MarkItDownDesktop.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:DebugType=None -o outputs\MarkItDownDesktop
```

随后将引擎和版本标记复制进发布目录。产物位于 `outputs\MarkItDownDesktop\`：

| 文件 | 大小 | 说明 |
| --- | --- | --- |
| `MarkItDownDesktop.exe` | 约 146 MB | 界面程序，已内含 .NET 运行时 |
| `markitdown.exe` | 约 103 MB | 转换引擎，已内含 Python 运行时 |
| `engine.version` | 数十字节 | 已打包引擎的版本号 |
| `app.ico` | 约 20 KB | 图标资源 |
| `D3DCompiler_47_cor3.dll` 等 5 个 dll | 合计约 8 MB | WinForms 单文件发布所需的原生依赖 |

整个目录约 257 MB，可直接整体拷贝分发，目标机器无需安装 .NET 或 Python。

冒烟验证：

```powershell
# 引擎能转换
.\outputs\MarkItDownDesktop\markitdown.exe .\README.md -o $env:TEMP\readme.md

# 界面能启动（手动关闭窗口）
.\outputs\MarkItDownDesktop\MarkItDownDesktop.exe
```

## 4. 第三步：发布桌面压缩包

### 方式 A：GitHub Actions（推荐）

**Actions** → **Release MarkItDown desktop** → **Run workflow**，填写：

- `version`：新的桌面版本，如 `1.0.0`
- `engine_version`：要捆绑的引擎版本，如 `0.1.8`

工作流步骤：下载 `engine-v<engine_version>` 的 `markitdown.exe` 与 `engine-update.json` 并校验 SHA-256 → 执行 `build_desktop.ps1 -EngineVersion <engine_version>` → 用 `Compress-Archive` 打包 → 发布 `desktop-v<version>`。

产物为 `MarkItDownDesktop-<version>-win-x64.zip`（约 167 MB，zip 内为 `MarkItDownDesktop/` 目录）。

### 方式 B：本地手动打包

```powershell
New-Item -ItemType Directory -Force artifacts | Out-Null
Compress-Archive -Path outputs\MarkItDownDesktop -DestinationPath artifacts\MarkItDownDesktop-1.0.0-win-x64.zip
```

再用 `gh` 或网页上传。若用 `gh`，**必须显式关闭 latest**：

```powershell
gh release create desktop-v1.0.0 artifacts\MarkItDownDesktop-1.0.0-win-x64.zip --title "MarkItDown desktop 1.0.0" --generate-notes --latest=false
```

## 5. 为什么桌面 Release 不能标记为 latest

桌面程序的引擎更新清单地址是硬编码的：

```
https://github.com/JiafuYuan/MarkItDownDesktop/releases/latest/download/engine-update.json
```

GitHub 默认把最新发布的非草稿、非预发布 Release 视为 “latest”，`releases/latest` 会指向它。

- 若把 `desktop-v*` 标为 latest，`releases/latest/download/engine-update.json` 就会 404，所有旧版客户端都点不动“检查引擎更新”。
- 因此桌面发布工作流使用 `--latest=false`，保证 latest 始终是引擎 Release。
- 一旦误标，在发布页编辑该 Release 取消勾选 “Set as the latest release” 即可恢复。

## 6. 版本判定与更新机制

1. 应用请求 `engine-update.json`，校验字段合法（`url` 必须为 https、`sha256` 必须为 64 位十六进制），否则提示“描述文件格式无效”。
2. 判断是否需要更新，判定“已安装版本”的优先级为：
   - **`engine.version`** 文件（位于 `markitdown.exe` 同目录，由本程序与打包脚本写入）
   - 回退：运行 `markitdown.exe --version`，从输出中提取版本号
   - 引擎缺失或损坏（无法执行）时视为“未知版本”，即允许下载安装以修复
3. `markitdown.exe --version` 输出的是**上游 Microsoft MarkItDown 包**的版本，通常会落后于我们发布的引擎版本（例如 0.1.8 的引擎自报 0.1.7）。若以它为准，更新完成后仍会反复提示“有新版本”。`engine.version` 记录的才是我们的发布版本。
4. 用户确认后，应用下载到 `markitdown.exe.download` → 校验 SHA-256 → `File.Replace` 替换并删除备份 → 写入 `engine.version`。临时文件在结束时会自动清理。
5. **兼容性**：早期分发、没有 `engine.version` 的副本会在第一次执行更新后自愈——更新一次即写入正确的版本号，之后不再重复提示。

## 7. 常见问题

| 现象 | 原因 | 处理 |
| --- | --- | --- |
| `Missing conversion engine: ...\outputs\markitdown.exe` | 未先准备引擎 | 按第 2 节下载或构建引擎 |
| `py` 不是内部或外部命令 | 只装了 `python`，没有 `py` 启动器 | 安装官方 Python 3.12（自带 `py`），或把脚本里的 `py -3.12` 改为 `python` |
| 明明已是新版，仍提示“发现新的转换引擎” | 该副本没有 `engine.version` | 手动执行一次更新即自愈；或按第 2 节重新打包，把 `engine.version` 一并分发 |
| 点击“检查引擎更新”报 `文件或目录损坏且无法读取` | 引擎文件损坏，且目录内无 `engine.version` | 现在的版本会将其视为“版本未知”并允许重新下载；也可手动删除该 exe 后重启程序 |
| 转换音频文件时报 `Couldn't find ffmpeg or avconv` | 引擎未内置 ffmpeg | 属已知限制，不影响其他格式；需要转音频请自行安装 ffmpeg |
| 构建报找不到 `net8.0` 目标包 | 缺少 .NET 8 目标包 | 安装 .NET 8 SDK，或直接用更高版本 SDK 构建 |
| 桌面 Release 发布后“检查引擎更新”提示“尚未发布可下载的引擎版本” | latest 被桌面 Release 抢占 | 编辑桌面 Release 取消 “Set as the latest release” |
| 产物/zip 体积很大（257 MB / 167 MB） | 单文件自包含把 .NET 与 Python 运行时都打了进去 | 正常现象，换来的是用户无需安装任何运行时 |

## 8. 发布前检查清单

- [ ] 引擎 Release 已发布，且 `engine-update.json` 的 `sha256` 与实际 `markitdown.exe` 一致
- [ ] `dotnet publish` 无错误，`outputs\MarkItDownDesktop\engine.version` 内容与预期引擎版本一致
- [ ] `MarkItDownDesktop.exe` 能启动，`markitdown.exe` 能完成一次转换
- [ ] 桌面 Release 未标记为 latest，Releases 页面的 Latest 仍是 `engine-v*`
