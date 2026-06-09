# WindowsTools 🧰

> 宁宁的 Windows 工具箱 —— 轻量、便携、实用的小工具集合 (⁎⁍̴̛ᴗ⁍̴̛⁎)

## 项目结构

```
WindowsTools/
├── Klick/          # 键盘/鼠标连点器（源码）
├── Deck/           # 🃏 卡组 — 成品工具，双击即用
│   └── Klick/      # Klick.exe 直接启动
├── README.md       # 本文件
└── CLAUDE.md       # AI 导览文档
```

## 工具列表

### [Klick](Klick/) — 键盘/鼠标连点器

一个带 GUI 的窗口连点器，支持全局热键启停。

| 功能 | 说明 |
|------|------|
| 🖱 **鼠标连点** | 左键 / 右键 / 中键，1~10000ms 可调间隔 |
| ⌨ **键盘连发** | 捕获模式：点击按钮后按下任意键即设为目标 |
| 🔥 **全局热键** | F8 启动、F9 停止，任意窗口下生效 |
| 📦 **系统托盘** | 最小化到托盘，右键恢复/退出 |
| 🔒 **单实例** | 防止重复启动 |

**技术栈**：.NET 8.0 + WinForms + SendInput (P/Invoke)

---

### 直接使用

双击 `Deck/Klick/Klick.exe` 即可启动 🃏

```bash
# 开发者运行
dotnet run --project Klick

# 重新发布到 Deck
dotnet publish Klick/Klick.csproj -c Release -o Deck/Klick
```

## 开发环境

- .NET 8.0 SDK
- Windows 10/11
- VS Code / Rider / Visual Studio

## License

MIT
