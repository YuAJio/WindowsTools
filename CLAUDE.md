# CLAUDE.md — WindowsTools

> 宁宁的 Windows 工具箱项目导览文档。供 AI 助手快速理解项目结构和规范。

## 项目概述

- **仓库**：git@github.com:YuAJio/WindowsTools.git
- **类型**：C# Windows 工具集（多项目）
- **框架**：.NET 8.0 + Windows Forms
- **语言**：C# 12，ImplicitUsings + Nullable enabled
- **平台**：Windows 10/11 x64

## 项目结构

```
WindowsTools/
├── Klick/                     # 键盘/鼠标连点器
│   ├── Klick.csproj           # net8.0-windows, WinExe + PublishSingleFile
│   ├── Program.cs             # 入口 + 单实例 Mutex
│   ├── MainForm.cs            # 主窗体逻辑
│   ├── MainForm.Designer.cs   # UI 布局（手写，非 Designer 生成）
│   └── NativeMethods.cs       # P/Invoke 封装（SendInput, 热键, 键盘钩子）
├── DailyVoice/                # 每日语音播放器
│   ├── DailyVoice.csproj      # net8.0-windows, WinExe + SelfContained + PublishSingleFile + NAudio
│   ├── Program.cs             # 入口 + 单实例 + 托盘
│   ├── MainForm.cs            # 配置窗口逻辑（音频+视频）
│   ├── MainForm.Designer.cs   # UI 布局（手写）
│   ├── Config.cs              # 配置模型 + JSON 读写
│   ├── AudioPlayer.cs         # NAudio 播放引擎（连续流静音前导）
│   ├── Scheduler.cs           # 定时检测 + 持久化洗牌队列 + 视频定时
│   ├── SilenceSampleProvider.cs # ISampleProvider 静音生成器
│   ├── ShuffleState.cs        # 洗牌状态持久化
│   └── VideoPlayerForm.cs     # WebView2 全屏视频播放窗体
├── ThumbPin/                  # 窗口置顶工具
│   ├── ThumbPin.csproj        # net8.0-windows, WinExe + PublishSingleFile
│   ├── Program.cs             # 入口 + 单实例
│   ├── MainForm.cs            # 主窗体 + 置顶逻辑 + 鼠标捕获
│   ├── MainForm.Designer.cs   # UI 布局（手写）
│   └── NativeMethods.cs       # P/Invoke（SetWindowPos, 热键, 鼠标钩子）
├── MoodyBlues/                # 操作录制与重播
│   ├── MoodyBlues.csproj      # net8.0-windows, WinExe + PublishSingleFile
│   ├── Program.cs             # 入口 + 单实例
│   ├── MainForm.cs            # 主窗体 + 热键分发 + 列表管理
│   ├── MainForm.Designer.cs   # UI 布局（手写）
│   ├── NativeMethods.cs       # P/Invoke（双钩子, SendInput, 热键, SetCursorPos）
│   ├── RecordEngine.cs        # 双钩子录制引擎
│   ├── PlaybackEngine.cs      # 按时间戳 SendInput 播放
│   └── RecordStore.cs         # JSON 存取 + 数据模型
├── Yoink/                     # 媒体下载器
│   ├── Yoink.csproj           # net8.0-windows, WinExe + SelfContained + PublishSingleFile
│   ├── Program.cs             # 入口 + 单实例
│   ├── MainForm.cs            # 主窗体 + yt-dlp 进程管理 + 进度解析
│   ├── MainForm.Designer.cs   # UI 布局（手写）
│   └── Config.cs              # 配置模型 + JSON 读写
├── Deck/                      # 🃏 卡组 — 成品工具发布目录
│   ├── Klick/
│   ├── DailyVoice/
│   ├── ThumbPin/
│   ├── MoodyBlues/
│   └── Yoink/
├── README.md
└── CLAUDE.md                  # 本文件
```

## Deck 发布规范 🃏

**每个工具完成后必须发布到 Deck 目录**，方便直接双击启动。

### 发布命令

```bash
dotnet publish <Project>/<Project>.csproj -c Release -o Deck/<Project>
```

### 配置要求

每个工具的 `.csproj` 必须包含以下发布配置（单文件、依赖系统运行时）：

```xml
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>false</SelfContained>
<IncludeNativeContentInSingleFile>true</IncludeNativeContentInSingleFile>
<IncludeAllContentForSelfExtract>true</IncludeAllContentForSelfExtract>
```

### Deck 目录规则

- `Deck/` 下按工具名建子文件夹，每个工具一个文件夹
- 发布产物只保留 `*.exe`（不含 pdb），代码改动后需要重新 `dotnet publish`
- **`Deck/*/` 已加入 `.gitignore`**，二进制不进 Git 仓库
- 发布完成后通知用户：「`Deck/<工具名>/<工具名>.exe` 可直接双击启动喵~ ✨」

## 命名规范

- **命名空间**：与项目文件夹同名（如 `Klick`）
- **类名**：PascalCase
- **私有字段**：`_camelCase` 下划线前缀
- **常量**：PascalCase（如 `HOTKEY_START`）
- **Win32 常量**：`UPPER_SNAKE_CASE`（如 `WM_HOTKEY`，遵循 Win32 惯例）

## 编码约定

- P/Invoke 声明统一放在 `NativeMethods` 静态类
- WinForms UI 采用**手写布局代码**，不使用 Designer 拖控件
- 全局热键用 `RegisterHotKey` + 消息循环，不用 .NET 自带的键盘事件
- 模拟输入统一用 `SendInput`（非 `SendKeys` 或 `keybd_event`）
- 长时间循环用 `Task.Run` + `CancellationToken`，不阻塞 UI 线程
- `ApplicationIcon` 等资源文件非必选项，没放的话别报错

## 新增工具规范

当向本项目新增工具时：

1. 在根目录新建文件夹（工具名 PascalCase）
2. 项目文件 `<RootNamespace>` 与文件夹名一致
3. 在 `.csproj` 中添加 `PublishSingleFile` 等发布配置
4. 完成后 `dotnet publish` 到 `Deck/<工具名>/`
5. 在 README.md 的「工具列表」添加条目
6. 更新本文件的「项目结构」章节
7. 每个工具自包含，不跨项目引用

## Git 提交规范

- 使用 Conventional Commits 格式（`feat:` / `fix:` / `refactor:` / `docs:` / `chore:`）
- 描述用中文
- 署名：`Coded with love by Ning QingHan ♡`
