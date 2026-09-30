# EasyScale

一个方便修改 Windows「设置 → 系统 → 屏幕 → 缩放」的小工具。
带托盘图标，点击弹出 Fluent UI 风格主窗口，支持命名缩放预设。

[English](README.md)

## 特性

- **即时生效** —— 修改缩放无需注销、无需管理员权限。
- **Fluent UI** —— 基于 [WPF-UI](https://wpfui.lepo.co/)，跟随系统主题。
- **托盘常驻** —— 左键唤起主窗口；右键菜单（显示 / 预设 / 设置 / 退出）。关闭窗口时最小化到托盘。
- **命名预设** —— 把某台显示器的某个档位存为预设，一键切换。
- **多语言** —— 中文（默认）/ English，运行时切换，无需重启。
- **单文件绿色版** —— 自包含发布，配置存于程序同目录。

## 技术栈

- .NET 8 + WPF
- WPF-UI 4.x（Fluent 2 风格控件 + 托盘）
- CommunityToolkit.Mvvm（MVVM 源生成器）
- 核心：`user32.dll` 的 `DisplayConfigGetDeviceInfo` / `DisplayConfigSetDeviceInfo`

> 说明：DPI 缩放读写使用两个**未公开**的 info type（`-3` GET / `-4` SET）。
> 二者已在 Windows 26H1（build 28000）实测：立即生效、无需提权。
> 实测记录见 [`docs/可行性评估.md`](docs/可行性评估.md)。
> 为防接口随 Windows 版本变化，缩放读写抽象为 `IDpiScaleApplier`，可整体替换实现。

## 项目结构

```
src/
  EasyScale.Core/     # 与 UI 无关的核心：互操作、枚举显示器、读写缩放
  EasyScale.App/      # WPF 应用：托盘、Fluent UI、i18n、设置与预设
  EasyScale.Probe/    # 控制台验证程序（交叉校验 档位 ↔ 实际 DPI）
tools/
  dpi-scale-probe.ps1 # PowerShell 探测脚本（-List / -Delta / -SetRel）
  make-icon.ps1       # 生成托盘/应用图标（可复现，不硬编码二进制）
docs/
  可行性评估.md        # 可行性结论与实测证据
```

## 构建与运行

需要 .NET 8 SDK。

```powershell
# 运行
dotnet run --project src/EasyScale.App

# 单文件绿色版（产物在 publish/ 下）
dotnet publish src/EasyScale.App -c Release -r win-x64 -o publish
```

## 验证

`EasyScale.Probe` 可独立验证「档位 ↔ 实际 DPI」映射：

```powershell
dotnet run --project src/EasyScale.Probe
```

> 注意：交叉验证要求调用方进程 DPI 感知（PerMonitorV2）。
> 控制台默认不感知，`GetDpiForMonitor` 会恒返回 96 DPI。Probe 已显式声明感知。

## 已知限制

- 仅支持 Windows。
- 依赖未公开 API；若未来 Windows 版本调整结构体布局，
  `QueryDisplayConfig` 返回的尺寸会最先暴露不一致。

## 许可

[MIT](LICENSE)
