# EyeGuard 👁️

一款强制护眼休息的 Windows 桌面工具，帮助久坐屏幕前的你按时休息、保护视力。

## 功能特性

- **定时提醒** — 默认每 40 分钟工作后强制锁屏休息 5 分钟，参数可自定义
- **预警通知** — 锁屏前 30 秒在屏幕右下角弹出倒计时警告（蓝→橙→红渐变提示紧迫感）
- **全屏锁屏** — 在所有显示器上同时显示不可关闭的锁屏界面，含动态渐变背景动画
- **键盘锁定** — 锁屏期间屏蔽 Win/Alt+Tab/F1-F24/PrintScreen 等快捷键，仅保留密码输入相关按键
- **密码保护** — 首次使用需设置密码；紧急退出与修改设置均需密码验证，密码使用 Windows DPAPI 加密存储
- **开机自启** — 可选开机自动启动（通过注册表写入）
- **系统托盘** — 最小化到托盘运行，实时显示剩余时间，右键菜单支持立即锁屏/设置/退出

## 工作流程

```
工作计时（40min）
       ↓ 剩余 ≤ 30s
预警弹窗（30s倒计时）
       ↓ 倒计时结束
全屏锁屏（5min倒计时 + 键盘锁定）
       ↓ 倒计时结束 / 密码解锁
回到工作计时
```

## 技术栈

- **.NET 10** (Windows) + **WPF** + **WinForms** (NotifyIcon)
- C# 12 / Nullable enabled / ImplicitUsings
- Windows DPAPI (`ProtectedData`) 密码加密
- 全局键盘钩子 (`SetWindowsHookEx`) 锁屏期间键位过滤
- `DispatcherTimer` 驱动计时与 UI 动画

## 构建

```bash
dotnet build
```

需要 .NET 10 SDK 及 Windows 环境。

## 运行

```bash
dotnet run
```

首次启动将弹出初始化设置窗口，要求设置密码和时间参数。

## 配置

配置文件 `config.json` 位于程序目录下，结构如下：

```json
{
  "WorkDurationMinutes": 40,
  "BreakDurationMinutes": 5,
  "WarningSeconds": 30,
  "PasswordHash": "<DPAPI encrypted>",
  "AutoStart": true
}
```

也可通过托盘右键 → 设置修改（需密码验证）。

## 项目结构

```
EyeGuard/
├── App.xaml / App.xaml.cs       应用入口与全局样式资源
├── MainWindow.xaml.cs           主窗口：托盘图标、计时器调度
├── TimerService.cs              计时状态机（Working→Warning→Break）
├── WarningWindow.xaml.cs        右下角预警弹窗
├── LockScreenWindow.xaml.cs     全屏锁屏 + 渐变动画
├── SettingsWindow.xaml.cs       设置窗口（密码/时间/自启）
├── PasswordVerifyWindow.xaml.cs 密码验证对话框
├── GlobalKeyboardHook.cs        全局键盘钩子
├── Config.cs / ConfigManager.cs 配置模型与持久化
└── LICENSE                      MIT License
```

## License

[MIT](LICENSE) © 2026 Felix Liu