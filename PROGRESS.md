# 开发进度

用这个文件记录当前开发切片、已完成事项和下一步。它不替代 [SLC.md](SLC.md)：SLC 定义产品范围，这里记录实际进展。

## 当前：V0.1 JPG 快速预览核心切片（已完成）

- [x] Step 0：WPF 窗口能跑
  - [x] 创建最小 `.NET 10 + WPF` 项目。
  - [x] `dotnet run` 能弹出一个简单窗口。
  - [x] 按 `Esc` 能关闭窗口。
  - [x] 不读取文件、不监听 `Space`、不显示图片。

- [x] Step 1：窗口能显示 JPG
  - [x] 给定一个本地 JPG 路径，窗口能正确显示图片。
  - [x] 图片不会明显拉伸变形。
  - [x] 暂不读取 Explorer 的选中项。

- [x] Step 2：能读取 Explorer 选中的 JPG
  - [x] 用户先在 Explorer 中选中一个 JPG。
  - [x] 程序能取得该选中项并显示对应图片。
  - [x] 暂不监听全局 `Space`。

- [x] Step 3：`Space` 触发预览
  - [x] Explorer 位于前台且选中 JPG 时，按 `Space` 打开预览。
  - [x] 预览打开后，按 `Space` 或 `Esc` 关闭。
  - [x] 不在其他应用中错误触发。

完成 Step 3 后，V0.1 完成。它验证 JPG 快速预览的核心交互，但不是 [SLC.md](SLC.md) 定义的完整 SLC。

## 已完成

- [x] 明确开发技术栈：Windows 11、Zed、C#、.NET 10 LTS、WPF 与 `dotnet` CLI。
- [x] 整理项目文档：README、SLC 与开发协作规则。
- [x] 将开发约束放入 `AGENTS.md`。
- [x] 完成 V0.1 Step 0：最小 WPF 窗口可启动并可用 `Esc` 关闭。
- [x] 完成 V0.1 Step 1：窗口可显示给定路径的 JPG，并保持图片比例。
- [x] 完成 V0.1 Step 2：无参数启动时，可读取 Explorer 选中的 JPG 并显示预览。
- [x] 完成 V0.1 Step 3：在 Explorer 中按 `Space` 打开 JPG 预览，并可用 `Space` 或 `Esc` 关闭。
- [x] 完成 V0.1：JPG 快速预览核心切片。

## 下一步：V0.2 图片预览体验

目标：在不改变 Explorer + `Space` 核心交互的前提下，完善静态图片预览的格式支持、缩放和初始窗口尺寸。

- [x] Step 1：JPG/JPEG 的缩放与窗口尺寸
  - [x] 图片保持原始宽高比例，不拉伸、不裁切。
  - [x] 小图片不会打开过大的窗口。
  - [x] 大图片不会超出屏幕可用区域。
  - [x] 宽图、竖图和方图均有合理的初始窗口尺寸。

- [x] Step 2：PNG 预览
  - [x] Explorer 中选中 PNG 后，按 `Space` 能显示预览。
  - [x] PNG 透明区域保持透明，显示为窗口背景色。
  - [x] PNG 使用与 JPG/JPEG 相同的缩放和窗口尺寸规则。

- [x] Step 3：WEBP 预览
  - [x] Explorer 中选中 WEBP 后，按 `Space` 能尝试显示预览。
  - [x] Windows 已安装 WebP 编解码器时，正常预览。
  - [x] 编解码器不可用时，显示清楚的提示，不崩溃。
  - [x] 不为此引入第三方图片解码依赖。

- [x] 完成 V0.2：静态图片预览体验。

V0.2 暂不包含：鼠标滚轮缩放、缩放滑条、旋转、EXIF 信息、缩略图列表、GIF 动画或图片导航。
