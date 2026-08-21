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

## 下一步

确定 V0.2 的下一个小切片。
