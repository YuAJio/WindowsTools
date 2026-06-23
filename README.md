# WindowsTools 🧰

> 宁宁的 Windows 工具箱 —— 轻量、便携、实用的小工具集合 (⁎⁍̴̛ᴗ⁍̴̛⁎)

## 项目结构

```
WindowsTools/
├── Klick/          # 键盘/鼠标连点器（源码）
├── DailyVoice/     # 每日语音播放器（源码）
├── ThumbPin/       # 窗口置顶工具（源码）
├── MoodyBlues/     # 操作录制与重播（源码）
├── Deck/           # 🃏 卡组 — 成品工具，双击即用
│   ├── Klick/      # Klick.exe
│   ├── DailyVoice/ # DailyVoice.exe
│   ├── ThumbPin/   # ThumbPin.exe
│   └── MoodyBlues/ # MoodyBlues.exe
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

**技术栈**：.NET 8.0 + WinForms + 双低级钩子 + SendInput

---

### 直接使用

双击 `Deck/` 下对应工具的 exe 即可启动 🃏

```bash
# 开发者运行
dotnet run --project Klick
dotnet run --project DailyVoice
dotnet run --project ThumbPin
dotnet run --project MoodyBlues
dotnet run --project ThumbPin

# 重新发布到 Deck
dotnet publish Klick/Klick.csproj -c Release -o Deck/Klick
dotnet publish DailyVoice/DailyVoice.csproj -c Release -o Deck/DailyVoice
```

## 开发环境

- .NET 8.0 SDK
- Windows 10/11
- VS Code / Rider / Visual Studio

## License

MIT
