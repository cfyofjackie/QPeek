QPeek v0.2.0 是第二个公开版本，重点加入 Markdown 渲染与 Explorer 选择跟随，并修复上一预览版中发现的交互问题。

在 Windows 资源管理器中选中文件，按下 `Space`，即可快速预览文件内容。

## 主要更新

- Markdown 不再显示原文，而是渲染标题、段落、强调、列表、只读任务列表、引用、链接、行内代码和代码块。
- Markdown 使用适合阅读的间距、配色与居中阅读栏；长代码行会自动换行。
- 预览打开后，在对应 Explorer 中单选另一个受支持文件，预览会直接跟随切换。
- 修复文本预览中选中文字后，`←` / `→` 无法继续切换文件的问题。
- 收紧全局快捷键边界，避免 QPeek 干扰其他软件中的 `Space`、`Enter` 和 `Ctrl+C`。
- 改善 Explorer 与预览窗口重叠时的层级关系。

## 当前功能

- 预览 JPG/JPEG、PNG 和 WEBP 图片。
- 预览 TXT，并渲染常用 Markdown 内容。
- 使用 `←` 和 `→`，按照 Explorer 当前显示顺序切换受支持文件。
- 按 `Enter` 关闭预览，并使用 Windows 默认应用打开当前文件。
- 按 `Ctrl+C` 复制当前文件本身。
- 记住预览窗口位置，并通过系统托盘菜单完全退出程序。

## 下载与运行

1. 在本 Release 的 **Assets** 中下载 `QPeek-v0.2.0-win-x64.zip`。
2. 解压 ZIP。
3. 双击 `QPeek.exe`。程序会进入后台，并在系统托盘显示图标。
4. 在 Windows 资源管理器中选中文件，按 `Space` 打开或关闭预览。
5. 要完全停止 QPeek，请右键单击系统托盘图标，然后选择 `Exit`。

该版本面向 Windows 11 x64，采用 self-contained（自包含）发布，目标电脑不需要预先安装 .NET Desktop Runtime。

QPeek 使用 MIT License 开源。目前没有代码签名，Windows SmartScreen 可能显示未知发布者提示。

## 已知限制

- 暂不支持 PDF、视频、Office 文档、RAW、PSD 和压缩包等格式。
- Markdown 暂不支持内嵌图片、远程资源、原始 HTML、Mermaid、数学公式或语法高亮。
- WEBP 预览依赖 Windows 中可用的 WEBP 图片编解码能力。
- 大图片会按照预览所需尺寸解码；当前没有放大查看原始像素的功能。
- 当前没有安装器、自动启动、自动更新或代码签名。

## 文件校验

`QPeek-v0.2.0-win-x64.zip`

SHA-256：

`7381B1611E21CB250DFA873DF1483363512C3981764EDE3A56C1135CBB0B7A83`
