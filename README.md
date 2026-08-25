# WindowsTools 🧰

> 宁宁的 Windows 工具箱 —— 轻量、便携、实用的小工具集合 (⁎⁍̴̛ᴗ⁍̴̛⁎)

## 项目结构

```
WindowsTools/
├── Klick/          # 键盘/鼠标连点器（源码）
├── DailyVoice/     # 每日语音播放器（源码）
├── ThumbPin/       # 窗口置顶工具（源码）
├── MoodyBlues/     # 操作录制与重播（源码）
├── Yoink/          # 媒体下载器（源码）
├── ClaudeMaster/   # Claude Code 环境配置器（源码）
├── QqPlaylist/     # QQ 音乐歌单抓取器（源码）
├── Deck/           # 🃏 卡组 — 成品工具，双击即用
│   ├── Klick/      # Klick.exe
│   ├── DailyVoice/ # DailyVoice.exe
│   ├── ThumbPin/   # ThumbPin.exe
│   ├── MoodyBlues/ # MoodyBlues.exe
│   ├── Yoink/      # Yoink.exe
│   ├── ClaudeMaster/ # ClaudeMaster.exe
│   └── QqPlaylist/ # QqPlaylist.exe
├── README.md       # 本文件
└── CLAUDE.md       # AI 导览文档
```

## 工具列表

### [Klick](Klick/) — 键盘/鼠标连点器

带 GUI 的窗口连点器，支持全局热键启停。

| 功能 | 说明 |
|------|------|
| 🖱 **鼠标连点** | 左键 / 右键 / 中键，1~10000ms 可调间隔 |
| ⌨ **键盘连发** | 捕获模式：点击按钮后按下任意键即设为目标 |
| 🔥 **全局热键** | F8 启动、F9 停止，任意窗口下生效 |
| 📦 **系统托盘** | 最小化到托盘，右键恢复/退出 |
| 🔒 **单实例** | 防止重复启动 |

**技术栈**：.NET 8.0 + WinForms + SendInput (P/Invoke)

### [DailyVoice](DailyVoice/) — 每日语音播放器

每天指定时间自动播放语音 + 视频，支持持久化洗牌队列。

| 功能 | 说明 |
|------|------|
| ⏰ **独立定时** | 音频/视频各自独立定时，每秒精度检测 |
| 🔀 **持久化洗牌** | 队列状态存盘，重启不重复播放同一首 |
| 🎵 **连续流播放** | 静音前导唤醒设备 + intro + 正文无缝拼接 |
| 🎬 **视频播放** | 独立定时，自动全屏，播放完毕自动关闭 |
| 📋 **视频排班** | 按顺序播放多个视频，同一视频可重复排班，UI 高亮当前项 |
| 🔊 **音量设置** | UI 滑块调节 0~100% |
| 🚀 **开机自启** | 勾选即写入注册表 Run |
| 📂 **文件管理** | 一键打开 voice 文件夹，支持试听 |
| 📦 **系统托盘** | 关闭窗口即隐藏到托盘，右键菜单操作 |

**技术栈**：.NET 8.0 + WinForms + NAudio + WebView2

### [ThumbPin](ThumbPin/) — 窗口置顶

点击捕获目标窗口置顶/取消，支持全局热键。

| 功能 | 说明 |
|------|------|
| 📌 **点击置顶** | 点击「捕获窗口」→ 点击目标窗口 → 置顶/取消 |
| ⌨ **全局热键** | Ctrl+Shift+F7 快速切换前台窗口置顶状态 |
| 📋 **批量管理** | 一键取消全部置顶，显示已置顶窗口计数 |
| 📦 **系统托盘** | 关闭窗口即隐藏到托盘 |

**技术栈**：.NET 8.0 + WinForms + SetWindowPos + DWM

### [MoodyBlues](MoodyBlues/) — 操作录制与重播

键盘/鼠标操作的录制与精准回放。

| 功能 | 说明 |
|------|------|
| 🔴 **录制** | F4 开始，录制所有键盘按键 + 鼠标点击（含绝对坐标） |
| ⏹ **停止** | F5 停止录制，自动保存为 JSON |
| ▶ **播放** | F6 按时间戳精准还原每一个事件 |
| 📼 **列表管理** | 按时间倒序显示，可播放/删除任意一条录制 |
| 🎯 **侧键支持** | 支持 XButton1/XButton2 侧键录制与播放 |
| 📋 **录制预设** | 可视化编辑器 + 按键捕获窗体，复用录制方案 |
| 🖱 **鼠标追踪** | 可选录制/回放鼠标绝对坐标，📍 标记区分 |
| ⌨ **虚拟键码** | VK ↔ ScanCode 转换，绕过前台焦点触发组合键 |

**技术栈**：.NET 8.0 + WinForms + 双低级钩子 + SendInput

### [Yoink](Yoink/) — 媒体下载器

粘贴链接一键下载视频/音频，yt-dlp GUI 套皮。

| 功能 | 说明 |
|------|------|
| 🎬 **视频下载** | 最佳 / 1080p / 720p / 480p / 360p 可选 |
| 🎵 **音频提取** | 下载最优音频流 → 自动 ffmpeg 提取 mp3 |
| 📊 **实时进度** | 百分比 + 速度 + ETA，yt-dlp stdout 实时解析 |
| ⏹ **取消下载** | 中途可取消，自动 kill yt-dlp 子进程 |
| 📂 **自定输出** | 指定保存目录，一键切到 voice/ 或 video/ |
| 🔍 **自动定位** | 程序目录 → tools/ → C:\Software\tydlp\ → PATH 四级查找 |

**技术栈**：.NET 8.0 + WinForms + yt-dlp + ffmpeg

### [ClaudeMaster](ClaudeMaster/) — Claude Code 环境配置器

一键安装 Claude Code CLI 并配置 API 地址和 Token。

| 功能 | 说明 |
|------|------|
| 🔍 **环境检测** | 自动检测 Node.js 和 Claude Code 安装状态 |
| 📦 **一键安装** | 通过 npm 安装/更新 Claude Code CLI，实时日志输出 |
| ⚙ **API 配置** | 图形化配置 Base URL 和 API Key，写入用户环境变量 |
| 📌 **系统托盘** | 关闭窗口最小化到托盘，随时唤出 |

**技术栈**：.NET 8.0 + WinForms

### [QqPlaylist](QqPlaylist/) — QQ 音乐歌单抓取器

输入 QQ 音乐歌单 ID/URL，一键拉取所有歌曲为 Markdown，可选自动保存到本地。

| 功能 | 说明 |
|------|------|
| 🔗 **智能识别** | 支持纯数字 ID 或完整 URL（自动提取 ID） |
| 📋 **Markdown 输出** | 序号 / 歌名 / 歌手 / 专辑 / 时长 五列表格 |
| 💾 **自动保存** | 勾选 checkbox 后解析即写入到指定路径 |
| 📂 **手动浏览** | SaveFileDialog 自由选输出位置 |
| 📋 **一键复制** | 整个 Markdown 直接丢进剪贴板 |
| ⏎ **回车即抓** | 输入框回车直接触发抓取 |
| 📋 **我的歌单** | 粘贴 Cookie 后一键拉取个人主页（我创建/我收藏的歌单），双击跳到抓取 |

**技术栈**：.NET 8.0 + WinForms + HttpClient + System.Text.Json + DPAPI 加密

---

### 直接使用

双击 `Deck/` 下对应工具的 exe 即可启动 🃏

```bash
# 开发者运行
dotnet run --project Klick
dotnet run --project DailyVoice
dotnet run --project ThumbPin
dotnet run --project MoodyBlues
dotnet run --project Yoink
dotnet run --project ClaudeMaster
dotnet run --project QqPlaylist

# 重新发布到 Deck
dotnet publish Klick/Klick.csproj -c Release -o Deck/Klick
dotnet publish DailyVoice/DailyVoice.csproj -c Release -o Deck/DailyVoice
dotnet publish ThumbPin/ThumbPin.csproj -c Release -o Deck/ThumbPin
dotnet publish MoodyBlues/MoodyBlues.csproj -c Release -o Deck/MoodyBlues
dotnet publish Yoink/Yoink.csproj -c Release -o Deck/Yoink
dotnet publish ClaudeMaster/ClaudeMaster.csproj -c Release -o Deck/ClaudeMaster
dotnet publish QqPlaylist/QqPlaylist.csproj -c Release -o Deck/QqPlaylist
```

## 开发环境

- .NET 8.0 SDK
- Windows 10/11
- VS Code / Rider / Visual Studio

## License

MIT
