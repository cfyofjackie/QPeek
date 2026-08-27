QPeek 的第一个公开预览版本。

在 Windows 资源管理器中选中文件，按下 `Space`，即可快速预览文件内容。

## 主要功能

- 预览 JPG/JPEG、PNG 和 WEBP 图片。
- 预览 TXT 和 Markdown 原文。
- 使用 `←` 和 `→`，按照 Explorer 当前显示顺序切换文件。
- 按 `Enter` 关闭预览，并使用 Windows 默认应用打开当前文件。
- 按 `Ctrl+C` 复制当前文件本身。
- 记住预览窗口的位置。
- 只允许一个 QPeek 进程运行。
- 通过系统托盘菜单完全退出程序。

## 下载与运行

1. 在本 Release 的 **Assets** 中下载 `QPeek-v0.1.0-preview-win-x64.zip`。
2. 解压 ZIP。
3. 双击 `QPeek.exe`。程序会进入后台，并在系统托盘显示图标。
4. 在 Windows 资源管理器中选中文件，按 `Space` 打开或关闭预览。
5. 要完全停止 QPeek，请右键单击系统托盘图标，然后选择 `Exit`。

该版本面向 Windows 11 x64，采用 self-contained（自包含）发布，目标电脑不需要预先安装 .NET Desktop Runtime。

QPeek 的源代码使用 MIT License 开源。

## 已知限制

- Markdown 当前只显示原文，不渲染样式。
- 暂不支持 PDF、视频、Office 文档、RAW、PSD 和压缩包等格式。
- WEBP 预览依赖 Windows 中可用的 WEBP 图片编解码能力。
- 当前没有图片放大查看功能。
- 在 TXT 或 Markdown 预览中选中文字后，方向键有时会被文本区域接收，无法切换文件。
- 当前没有安装器、自动更新或代码签名；Windows SmartScreen 可能显示未知发布者提示。

## 文件校验

`QPeek-v0.1.0-preview-win-x64.zip`

SHA-256：

`9C9C13B4380C983242D6FF1BF106DEE8E44827B97FF547F4BB7B58C4831F841B`
