<p align="center">
  <img src="icon/outputs/QPeek-transparent.png" width="128" alt="QPeek 图标">
</p>

# QPeek

QPeek 是一个小型 Windows 文件快速预览工具。在 Windows 资源管理器中选中文件，按下 `Space` 即可快速查看内容。

这是一个以学习为目的的项目，灵感来自 macOS Quick Look。当前首个 SLC（最小完整功能切片）已经完成，并在两台 Windows 11 电脑上通过手动测试。

## 当前功能

- 在 Windows 资源管理器中选中文件，按 `Space` 打开或关闭预览。
- 支持 JPG/JPEG、PNG、WEBP、TXT 和 Markdown 文件。
- 图片按照原始比例缩放，并根据屏幕空间使用合理的预览窗口尺寸。
- TXT 和 Markdown 使用可滚动的只读文本区域预览；Markdown 当前显示原文，不做样式渲染。
- 使用 `←` 和 `→`，按照 Explorer 当前显示顺序浏览同一文件夹中的受支持文件。
- 切换预览时，Explorer 的选中项会同步到当前文件。
- 按 `Enter` 关闭预览，并使用 Windows 默认应用打开当前文件。
- 按 `Ctrl+C` 复制当前文件本身，可在其他文件夹中粘贴。
- 拖动预览窗口后会记住其中心位置，下次预览继续使用该位置。
- 程序只允许一个实例运行，并提供系统托盘图标和退出菜单。

## 下载与使用

请前往项目的 [GitHub Releases 页面](https://github.com/cfyofjackie/QPeek/releases)，在 `QPeek v0.1.0 Preview` 下展开 **Assets**，然后点击 **QPeek-v0.1.0-preview-win-x64.zip** 下载。

这里显示的是需要点击下载的附件名称，不是 PowerShell 命令。请不要选择 GitHub 自动生成的 `Source code (zip)`，那里面只有源代码，不能直接运行 QPeek。

该版本面向 Windows 11 x64，并采用 self-contained（自包含）发布，目标电脑不需要预先安装 .NET 10 Desktop Runtime。

使用步骤：

1. 解压 ZIP。
2. 双击 `QPeek.exe`。程序会进入后台，并在系统托盘显示图标。
3. 在 Windows 资源管理器中选中一个受支持的文件。
4. 按 `Space` 打开预览。
5. 再按一次 `Space` 或按 `Esc` 关闭预览。

如果 Windows SmartScreen 提示未知发布者，请只在确认文件来自本项目 GitHub Release 时继续运行。目前的预览版本没有代码签名。

要完全停止 QPeek，请右键单击系统托盘中的 QPeek 图标，然后选择 `Exit`。直接关闭预览窗口不会停止后台监听器。

## 快捷键

| 快捷键 | 作用 |
| --- | --- |
| `Space` | 打开或关闭预览 |
| `Esc` | 关闭预览 |
| `←` / `→` | 浏览上一个或下一个受支持文件 |
| `Enter` | 用 Windows 默认应用打开当前文件，并关闭预览 |
| `Ctrl+C` | 将当前文件复制到剪贴板 |

## 当前限制

- 仅支持 Windows 11 和 Windows 资源管理器。
- 暂不支持 PDF、视频、Office 文档、RAW、PSD 和压缩包等格式。
- Markdown 只显示原文，不渲染标题、列表或代码样式。
- WEBP 预览依赖 Windows 中可用的 WEBP 图片编解码能力。
- 大图片会按照预览所需尺寸解码，以提高打开速度；当前没有放大查看原始像素的功能。
- 在 TXT 或 Markdown 预览中选中文字后，方向键有时会被文本区域接收，无法切换文件。
- 当前提供 ZIP 便携包，没有安装器、自动启动、自动更新或代码签名。

## 技术栈与项目原则

- Windows 11
- C# 与 .NET 10 LTS
- WPF
- `dotnet` CLI
- Zed（推荐，但不是项目依赖）
- Coding Agent 辅助开发

项目不依赖 Visual Studio、Visual Studio Designer 或只能通过 GUI 完成的配置。实现优先保持文件少、代码直接、步骤容易理解；在保留未来发布可能性的同时，不提前引入生产级架构。

## 从源码运行

开发环境需要 Windows 11 和 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)。先确认 SDK：

```powershell
dotnet --list-sdks
```

输出中应包含以 `10.0` 开头的版本。然后在仓库根目录运行：

```powershell
dotnet build
dotnet run
```

`dotnet run` 启动后不会立即显示窗口，而是在后台等待 Explorer 中的 `Space`。开发时可以在终端按 `Ctrl+C` 停止程序，也可以通过托盘菜单退出。

生成与首次预览版相同类型的 Windows x64 自包含发布目录：

```powershell
dotnet publish --configuration Release --runtime win-x64 --self-contained true
```

## 主要文件

- `App.xaml` / `App.xaml.cs`：应用启动、单实例、系统托盘、全局快捷键和 Explorer 交互。
- `MainWindow.xaml`：预览窗口的 XAML 界面。
- `MainWindow.xaml.cs`：图片和文本显示、窗口尺寸、文件切换与快捷键行为。
- `GlobalKeyboardHook.cs`：后台监听全局快捷键。
- `ExplorerViewOrder.cs`：读取 Explorer 当前显示的文件顺序。
- `QuickLook.csproj`：可由 `dotnet` CLI 读取的项目配置；生成的应用名称是 `QPeek`。

## 项目文档

- [SLC.md](SLC.md)：首个可用版本的范围、目标与非目标。
- [PROGRESS.md](PROGRESS.md)：开发切片、完成记录和后续候选功能。
- [RELEASE_NOTES.md](RELEASE_NOTES.md)：首次 GitHub Release 可直接使用的版本说明。
- [AGENTS.md](AGENTS.md)：项目的开发、学习与 Coding Agent 协作原则。

## 开源许可证

QPeek 使用 [MIT License](LICENSE)。你可以使用、学习、修改和分发代码，但需要保留原始版权与许可证声明。
