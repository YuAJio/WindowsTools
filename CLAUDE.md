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
│   ├── Klick.csproj           # net8.0-windows, WinExe
│   ├── Program.cs             # 入口 + 单实例 Mutex
│   ├── MainForm.cs            # 主窗体逻辑
│   ├── MainForm.Designer.cs   # UI 布局（手写，非 Designer 生成）
│   └── NativeMethods.cs       # P/Invoke 封装（SendInput, 热键, 键盘钩子）
├── README.md
└── CLAUDE.md                  # 本文件
```

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
3. 在 README.md 的「工具列表」添加条目
4. 更新本文件的「项目结构」章节
5. 每个工具自包含，不跨项目引用

## Git 提交规范

- 使用 Conventional Commits 格式（`feat:` / `fix:` / `refactor:` / `docs:` / `chore:`）
- 描述用中文
- 署名：`Coded with love by Ning QingHan ♡`
