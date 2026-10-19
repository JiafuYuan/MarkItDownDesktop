# 打包与发布

本仓库把**完整程序**和**引擎更新资产**放在同一个 Release 里，所以日常发版只有一步：跑一个 workflow，填一个版本号。

| 产物 | 内容 | 在 Release 中的资产 |
| --- | --- | --- |
| 完整程序 | C# 界面（自包含 .NET）+ `markitdown.exe`（内含 Python 运行时） | `MarkItDownDesktop-<version>-win-x64.zip` |
| 引擎更新资产 | 供应用内“检查引擎更新”使用 | `markitdown.exe`、`engine-update.json`、`engine.version` |

任何 Release 都带有 `engine-update.json`，因此应用总能从 `releases/latest` 读到它，**不需要**为 latest 指向哪个 Release 操心。

以下命令均假设当前目录为仓库根目录。

## 1. 环境要求

| 用途 | 要求 |
| --- | --- |
| 打包桌面程序 | .NET SDK 8.0 或更高（项目目标为 `net8.0-windows`，用 SDK 10 构建同样正常） |
| 本地构建引擎 | Git、Python 3.12（有 `py` 启动器时优先用 `py -3.12`，否则回退到 `python`） |
| 只下载现成引擎 | 无需任何本地环境，只要能访问 GitHub |

## 2. 第一步：准备引擎

### 方式 A：下载现成引擎（推荐，最快）

```powershell
New-Item -ItemType Directory -Force outputs | Out-Null
$base = 'https://github.com/JiafuYuan/MarkItDownDesktop/releases/latest/download'
Invoke-WebRequest "$base/markitdown.exe" -OutFile outputs\markitdown.exe
Invoke-WebRequest "$base/engine-update.json" -OutFile outputs\engine-update.json
```

`releases/latest/download/...` 会取最新 Release 里的同名资产；要指定版本就把 `latest` 换成 tag，例如 `releases/download/engine-v0.1.8`。

务必校验哈希，避免拿到不完整的下载：

```powershell
$manifest = Get-Content outputs\engine-update.json -Raw | ConvertFrom-Json
(Get-FileHash outputs\markitdown.exe -Algorithm SHA256).Hash.ToLowerInvariant() -eq $manifest.sha256   # 应为 True
```

### 方式 B：本地从源码构建引擎

```powershell
.\build_engine.ps1 -SourceRef main -Version 1.0.0
```

可选参数：`-ReleaseTag`（写入清单的下载地址所用的 tag，默认 `engine-v$Version`）、`-OutputDirectory`（默认 `artifacts`）、`-RefreshSource`。

脚本流程：clone/更新 `microsoft/markitdown` 到 `work\markitdown` → 建虚拟环境 → `pip install ".[all]" pyinstaller` → PyInstaller `--onefile` 打包。

产物（默认 `artifacts\`）：

- `markitdown.exe`：转换引擎本体
- `engine-update.json`：更新清单，含 `version`、`url`、`sha256`
- `engine.version`：纯文本版本号，供桌面程序判定版本（见第 5 节）

构建完把产物放到桌面打包脚本期望的位置：

```powershell
Copy-Item artifacts\markitdown.exe,artifacts\engine-update.json outputs\ -Force
```

## 3. 第二步：打包桌面程序

```powershell
.\build_desktop.ps1 -EngineVersion 1.0.0 -AppVersion 1.0.0
```

- `-EngineVersion`：写入发布目录的 `engine.version`。可省略，省略时依次尝试 `outputs\engine-update.json` 的 `version`、`markitdown.exe --version`。
- `-AppVersion`：作为 `-p:Version` 传给 `dotnet publish`，使 exe 的版本信息可查。可省略。
- 脚本要求 `outputs\markitdown.exe` 已存在，否则报错 `Missing conversion engine`。

实际执行的发布命令：

```powershell
dotnet publish MarkItDownDesktop.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:DebugType=None -o outputs\MarkItDownDesktop
```

产物位于 `outputs\MarkItDownDesktop\`：

| 文件 | 大小 | 说明 |
| --- | --- | --- |
| `MarkItDownDesktop.exe` | 约 146 MB | 界面程序，已内含 .NET 运行时 |
| `markitdown.exe` | 约 103 MB | 转换引擎，已内含 Python 运行时 |
| `engine.version` | 数十字节 | 已打包引擎的版本号 |
| `app.ico` | 约 20 KB | 图标资源 |
| `D3DCompiler_47_cor3.dll` 等 5 个 dll | 合计约 8 MB | WinForms 单文件发布所需的原生依赖 |

整个目录约 257 MB，可直接整体拷贝分发；目标机器无需安装 .NET 或 Python。

冒烟验证：

```powershell
# 引擎能转换
.\outputs\MarkItDownDesktop\markitdown.exe .\README.md -o $env:TEMP\readme.md

# 界面能启动（手动关闭窗口）
.\outputs\MarkItDownDesktop\MarkItDownDesktop.exe
```

## 4. 第三步：发布（唯一需要跑的一步）

**Actions** → **Release MarkItDown** → **Run workflow**，填写：

- `version`：发布版本，如 `1.0.0`
- `markitdown_ref`：Microsoft MarkItDown 的 tag、commit SHA 或 `main`

工作流依次执行：构建引擎（PyInstaller）→ 校验 `engine-update.json` 的版本与 SHA-256 → 把引擎放到 `outputs\` → `build_desktop.ps1` → 压缩为 zip → 发布 Release `v<version>`。

发布后用户只需要：

1. 打开 Releases 页面下载 `MarkItDownDesktop-1.0.0-win-x64.zip`
2. 解压到任意目录，双击 `MarkItDownDesktop.exe`

没有安装步骤，也不需要 Python 或 .NET。Release 同时带上引擎资产，所以 `v1.0.0` 就是最新的引擎更新源。

### 本地手动发布

```powershell
New-Item -ItemType Directory -Force artifacts | Out-Null
Compress-Archive -Path outputs\MarkItDownDesktop -DestinationPath artifacts\MarkItDownDesktop-1.0.0-win-x64.zip
gh release create v1.0.0 artifacts\MarkItDownDesktop-1.0.0-win-x64.zip `
  outputs\markitdown.exe outputs\engine-update.json outputs\engine.version `
  --title "MarkItDown 1.0.0" --generate-notes --latest
```

其中 `outputs\engine.version` 若不存在，用 `outputs\MarkItDownDesktop\engine.version` 代替；`--latest` 表示该 Release 成为 `releases/latest`，这是安全的（见下节）。

## 5. 版本判定与更新机制

1. 应用请求 `releases/latest/download/engine-update.json`，校验字段合法（`url` 必须为 https、`sha256` 必须为 64 位十六进制），否则提示“描述文件格式无效”。
2. 判断是否需要更新时，“已安装版本”的来源优先级为：
   - **`engine.version`** 文件（位于 `markitdown.exe` 同目录，由本程序与打包脚本写入）
   - 回退：运行 `markitdown.exe --version` 并提取版本号
   - 引擎缺失或损坏（无法执行）时视为“版本未知”，即允许下载安装以修复
3. `markitdown.exe --version` 输出的是**上游 Microsoft MarkItDown 包**的版本，通常落后于我们发布的版本（例如 0.1.8 的引擎自报 0.1.7）。若以它为准，更新完成后仍会反复提示“有新版本”。`engine.version` 记录的才是我们的发布版本。
4. 下载到 `markitdown.exe.download` → 校验 SHA-256 → `File.Replace` 替换并删除备份 → 写入 `engine.version`。临时文件在结束时会自动清理。
5. **兼容性**：早期分发、没有 `engine.version` 的副本会在第一次执行更新后自愈。

### 为什么现在可以放心标记 latest

因为每个 Release 都包含 `engine-update.json`，无论 `releases/latest` 指向哪个 Release，应用的更新检查都能解析成功。因此不再需要“桌面 Release 不能标 latest”之类的约束。

### 应用启动时自动补齐引擎

如果应用启动时同目录没有 `markitdown.exe`，它会自动联网下载并显示进度，无需用户操作；失败时给出提示，按钮文字会变为“下载转换引擎”，可点击重试。所以只分发 146 MB 的应用本体而不带引擎也是可行的，代价是首次启动需要联网。

## 6. 常见问题

| 现象 | 原因 | 处理 |
| --- | --- | --- |
| `Missing conversion engine: ...\outputs\markitdown.exe` | 本地打包前未准备引擎 | 按第 2 节方式 A 下载引擎 |
| 首次启动停在“未找到转换引擎，正在自动下载...”很久 | 正在下载约 103 MB 的引擎 | 等待进度走完；失败会弹提示，可点“下载转换引擎”重试 |
| 明明已是新版，仍提示“发现新的转换引擎” | 该副本没有 `engine.version` | 执行一次更新即自愈；或重新打包把 `engine.version` 一并分发 |
| 点击更新时提示“尚未发布可下载的引擎版本” | `releases/latest` 的 Release 里没有 `engine-update.json` | 用第 4 节的 workflow 发布带全套资产的 Release |
| 引擎构建阶段失败：找不到 `py` | 本地没有 `py` 启动器 | 脚本会自动回退到 `python`；仍失败则安装官方 Python 3.12 |
| 转换音频文件时报 `Couldn't find ffmpeg or avconv` | 引擎未内置 ffmpeg | 属已知限制，不影响其他格式；需要转音频请自行安装 ffmpeg |
| 构建报找不到 `net8.0` 目标包 | 缺少 .NET 8 目标包 | 安装 .NET 8 SDK，或用更高版本 SDK 构建 |
| 产物/zip 体积很大（257 MB / 167 MB） | 单文件自包含把 .NET 与 Python 运行时都打了进去 | 正常现象，换来的是用户无需安装任何运行时 |

## 7. 发布前检查清单

- [ ] workflow 的 `engine-update.json` 版本与 SHA-256 校验通过（工作流会自动做）
- [ ] `outputs\MarkItDownDesktop\engine.version` 内容与本次版本一致
- [ ] `MarkItDownDesktop.exe` 能启动，`markitdown.exe` 能完成一次转换
- [ ] Release 中同时存在 zip、`markitdown.exe`、`engine-update.json`、`engine.version`
