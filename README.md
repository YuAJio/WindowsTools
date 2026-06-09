# WindowsTools 🧰

> 宁宁的 Windows 工具箱 —— 轻量、便携、实用的小工具集合 (⁎⁍̴̛ᴗ⁍̴̛⁎)

## 项目结构

```
WindowsTools/
├── Klick/          # 键盘/鼠标连点器（源码）
├── DailyVoice/     # 每日语音播放器（源码）
├── Deck/           # 🃏 卡组 — 成品工具，双击即用
│   ├── Klick/      # Klick.exe
│   └── DailyVoice/ # DailyVoice.exe
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

每天指定时间自动播放语音，支持 intro 前奏 + 随机洗牌队列。

| 功能 | 说明 |
|------|------|
| ⏰ **定时播放** | 每天指定时间自动播放，每秒精度检测 |
| 🔀 **洗牌队列** | Fisher-Yates 随机，不会重复播放同一首 |
| 🎵 **前奏支持** | voice/ 下放 intro.mp3 作为开场前奏 |
| 🔊 **音量设置** | UI 滑块调节 0~100% |
| 🚀 **开机自启** | 勾选即写入注册表 Run |
| 📂 **语音管理** | 一键打开 voice 文件夹，支持试听 |
| 📦 **系统托盘** | 关闭窗口即隐藏到托盘，右键菜单操作 |

**技术栈**：.NET 8.0 + WinForms + NAudio

---

### 直接使用

双击 `Deck/` 下对应工具的 exe 即可启动 🃏

```bash
# 开发者运行
dotnet run --project Klick
dotnet run --project DailyVoice

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
