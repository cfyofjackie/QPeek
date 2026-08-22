# Windows Quick Preview

一个学习型 Windows 文件快速预览工具，灵感来自 macOS Quick Look。

目标是实现一个小而完整的体验：在 Windows 资源管理器中选中文件后按下 `Space`，快速显示预览；再次按下 `Space` 或 `Esc` 关闭预览。

这不是要一次性复刻 QuickLook，而是一个通过实际完成 Windows 桌面工具来学习的项目。

## 当前运行方式

$env:PATH = "$PWD\.dotnet;$env:PATH"
dotnet run

## 当前状态

已完成 V0.1 核心 JPG 快速预览和 V0.2 静态图片预览体验。

当前支持 JPG/JPEG、PNG 和 WEBP 静态图片；WEBP 需要 Windows 提供可用的 WebP 图片编解码器。

首个版本的功能范围、非目标和产品原则见 [SLC.md](SLC.md)。

## 技术栈

- 操作系统：Windows 11
- 语言：C#
- 平台：.NET 10 LTS
- 桌面 UI：WPF
- 构建与运行：`dotnet` CLI
- 编辑器：Zed（推荐，但不是项目依赖）
- 开发协作：Coding Agent

项目的真实构建前提是 Windows 与 .NET 10 SDK；不依赖 Visual Studio、Visual Studio Designer 或任何需要 GUI 配置的步骤。

WPF 是 Windows 专属 UI 框架，因此应用仅在 Windows 上创建和运行。项目的目标框架将使用 `net10.0-windows`。

## 环境要求

1. Windows 11。
2. 安装 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)。
3. 使用任意编辑器和终端；本项目推荐 Zed 与其内置终端。

安装后可验证 SDK：

```powershell
dotnet --list-sdks
```

输出中应包含 `10.0` 开头的版本。

## CLI 工作流

项目代码创建后，所有日常操作均通过命令行完成：

```powershell
dotnet build
dotnet run
```

当前 V0.1 的使用方式：

1. 运行 `dotnet run`。程序会在后台等待，不会立刻显示窗口。
2. 在 Windows 资源管理器中选中一个 JPG。
3. 保持 Explorer 位于前台，按下 `Space` 显示预览。
4. 按 `Space` 或 `Esc` 关闭预览。

开发时，按终端中的 `Ctrl+C` 可以停止后台监听程序。

首次创建 WPF 项目时，在仓库根目录执行：

```powershell
dotnet new wpf --name QuickLook --framework net10.0
```

该命令会生成 WPF 项目文件；之后仍可仅使用 Zed 和 `dotnet` CLI 编辑、构建与运行。

## 文档

- [SLC.md](SLC.md)：首个可用版本的产品范围与功能优先级。
- [PROGRESS.md](PROGRESS.md)：当前开发切片、完成记录与下一步。
- [AGENTS.md](AGENTS.md)：开发与 Coding Agent 协作时的约束和学习原则。
