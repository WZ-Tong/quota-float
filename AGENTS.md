# QuotaFloat Agent Notes

QuotaFloat 是一个自用 Windows 悬浮窗，显示 DeepSeek 余额与 OpenCode Go 额度。

## Source of truth

- 只编辑按功能拆分的 `.cs` 文件；`QuotaFloat.single.cs` 由构建生成，不要手改。
- 手工构建会先重新导出单文件，再只编译该单文件。
- 单文件导出必须是确定性的：UTF-8 BOM、无时间戳、using 放在各自 namespace 内。

## Commands

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File ./build.ps1 -Configuration Debug
powershell -NoProfile -ExecutionPolicy Bypass -File ./build.ps1 -ExportOnly
powershell -NoProfile -ExecutionPolicy Bypass -File ./regression.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File ./verify.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File ./verify.ps1 -Tag v1.3.0 -CheckRemote
```

- 构建前若程序正在运行，需要先退出，否则 exe 被占用。
- Debug 构建会生成 `QuotaFloat.debug.exe` 和控制台，仅用于本地调试。

## Module responsibilities

- `Program.cs`：进程入口、单实例 Mutex、TLS 初始化、消息循环。
- `MainForm.cs`：窗口构造、基础布局、菜单、字段和定时器装配。
- `MainForm.Window.cs`：窗口事件、拖动/拉伸、截图排除、显示器时序同步。
- `MainForm.Edge.cs`：贴边吸附、滑出/恢复动画、边缘用量指示条。
- `MainForm.UI.cs`：主题、语言、tooltip、demo 数据、显示刷新。
- `MainForm.Fetch.cs`：DeepSeek/OpenCode 请求、结果应用和请求代次。
- `MainForm.Cache.cs`：OpenCode 缓存、过期行显示、行数据应用。
- `MainForm.Schedule.cs`：刷新间隔、失败退避、时间戳和到期判断。
- `RefreshPolicy.cs`：不依赖 UI 的退避与剩余比例纯逻辑。
- `DisplayTiming.cs`：显示器刷新率探测和帧间隔计算。
- `Controls.cs`：Card、MiniBar、UsageRow、RefreshButton 自绘控件。
- `Metrics.cs`：布局、时序、颜色语义之外的数值常量。
- `Strings.cs`：全部双语 UI 文案和动态状态文本。
- `AppConfig.cs`：配置持久化、DPAPI 和缓存字段。
- `SettingsForm.cs`：API Key 设置窗口。

## Coding conventions

- 保持 C# 5 语法和 .NET Framework 4.x，不引入第三方运行时依赖。
- 新增布局、时间、网络或绘制数值时，优先放入 `Metrics.cs` / `Timing` 并命名，不在调用处留裸数字。
- 新增 UI 文案必须进入 `Strings.cs` 并使用中英文两套文本。
- 自绘控件优先使用比例或已命名度量，避免再出现固定宽度小于字形宽度的问题。
- 按“职责”拆分 partial 文件，不按函数数量机械拆分；不创建深层测试目录。
- 修改配置结构时必须为新字段提供向后兼容默认值。
- 修改版本时必须同步 `AssemblyInfo.cs`、README 中的版本说明和 release tag。
- 不手工编辑 `QuotaFloat.single.cs`；先改拆分源码，再运行构建。


## Release and Git

- Release 资产只能包含 `QuotaFloat.exe`；不上传 Debug exe 或任何 PDB。分支构建由 `build.yml` 负责，tag 构建与自动发布由 `release.yml` 负责，两者保持独立。
- 发布前运行 `verify.ps1 -Tag vX.Y.Z -CheckRemote`，确认单文件源码、版本和远程资产。
- 版本变化需同步 `AssemblyInfo.cs`、release tag 和 README 中的版本说明。
- commit 与 tag 必须 GPG 签名；使用普通 `git commit`，不要禁用签名。
- `v*` tag push 会触发自动 Release；未明确要求时不要 push tag、手工创建 release 或删除远端资产。

## Verification

- 不截图、不抓屏；使用 Debug 控制台的 `status`、`menu`、`eq`、`policy` 命令和根目录 `regression.ps1`。
- 不创建深层测试目录，不引入测试框架或额外依赖。
- 修改布局时重点检查：标题高度、等式 `+`/`=` 宽度、320px 基础宽度与等式内容驱动的自动加宽、单卡片刷新条 5px 边距。
- 新增 UI 文案使用 `Strings.cs` 双语属性；新增布局/时间/阈值常量使用 `Metrics.cs` 的命名常量。

## Behavior constraints

- 展开刷新 120 秒，贴边收起 600 秒；失败按 30/60/120/240/480/600 秒退避。
- DeepSeek/OpenCode 独立刷新；请求代次必须防止旧结果覆盖新 Key 的数据。
- OpenCode 成功结果缓存；启动/断网时显示旧数据并标记“已过期”。
- 单实例运行；`--demo` 可绕过单实例锁用于预览和回归。
- 支持 Windows 10 全系列与 Windows 11，运行时目标 .NET Framework 4.6+。

## UI constraints

- 窗口不进入任务栏/Alt+Tab；不显示标题栏。
- 截图隐藏默认开启；旧版 Windows 10 回退为黑色遮挡。
- 前台动画按当前显示器刷新率推进，用单调时钟计算，不依赖固定帧间隔。
- 刷新按钮、tooltip、主题和语言切换需保持中英文同步。
