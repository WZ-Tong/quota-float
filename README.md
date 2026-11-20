# QuotaFloat

一个用于 Windows 的轻量悬浮窗，同时显示 DeepSeek API 余额和 OpenCode Go 套餐额度。

![icon](icon.svg)

## 下载

- [最新版本](https://github.com/WZ-Tong/quota-float/releases/latest)（`QuotaFloat.exe`）

## 功能

- DeepSeek 余额；可选余额等式：`余额 + 花费 = 总额`，双击数值原位编辑，自动算出另一项；初始无总额时按 `+0` 显示。
- DeepSeek 峰谷提示：周一至周五 `09:00–12:00`、`14:00–18:00`（UTC+8）为峰时（红），其余时间及周末为谷时（绿）。
- OpenCode Go：5 小时 / 本周 / 本月剩余百分比、进度条和 `0d 01h 23m 45s` 形式的重置倒计时。最近一次成功结果会缓存；启动或断网时先显示缓存并标记“已过期”，再后台刷新。
- 两张卡片标题右侧各有一个刷新按钮：只请求对应服务；请求期间按钮旋转、底部刷新条进入流动状态，完成后恢复倒计时。右键“全部刷新”会同时启动两者。
- 自动刷新：展开时每 120 秒；贴边收起后降为 600 秒，减少后台请求；重新展开时若数据超过 45 秒则立即刷新。卡片间细条显示下次刷新进度，请求进行中显示流动提示，网络较慢时也能看出正在刷新。
- 失败重试：首次失败约 30 秒后重试，随后按 30/60/120/240/480/600 秒退避，上限跟随当前显示/隐藏刷新间隔；刷新按钮 tooltip 显示最后成功时间与连续失败次数。
- 刷新结果发生变化时，DeepSeek 余额条和 OpenCode 三行进度条使用约 320ms 缓动过渡；数值文字立即更新，避免延迟真实读数和等式宽度。贴边收起时直接更新，不播放动画。
- 仅显示一张卡片时，刷新细条落在顶部或底部：距卡片和窗口边缘均为 5px，与两张卡片之间的细条间距一致。
- 前端动画按当前显示器在当前分辨率下的最高刷新率驱动：刷新倒计时、刷新流动条与贴边滑出使用单调时钟计算相位；切换显示器或分辨率后自动重新检测。
- 截图时隐藏：默认开启，右键菜单可关闭；Windows 10 2004+ 使用 `WDA_EXCLUDEFROMCAPTURE` 从截图和录屏中排除主窗口，更早的 Windows 10 回退到 `WDA_MONITOR`（截图中显示黑色遮挡）。同时移除系统投影，避免阴影残留。
- 黑/白主题：默认跟随系统，也可右键强制浅色/深色。
- 无标题栏；不出现在任务栏和 Alt+Tab；基础宽度范围 320–720px，支持锁定并持久化。余额等式内容需要更多空间时会自动加宽到所需宽度（最高 720px），内容变短时不自动缩回。
- 余额等式按实际文本测量并分配宽度，空间不足时使用省略号；不会因数值变化自动改变窗口宽度，避免悬浮窗跳动和贴边位置漂移。
- 贴边滑出：拖到屏幕边缘松开后吸附；鼠标移出约 0.45 秒后，窗口用动画滑出屏幕，只在边缘保留 10px 的边；鼠标在边缘停留约 0.2 秒后滑回，停留期间不自动收起。滑出只改位置，窗口宽高保持原尺寸。贴边时边缘指示条显示剩余比例，与内部进度条一致（100% 为全填充），长度几乎贯穿整条边；轨道使用更深底色标出 100% 范围。OpenCode 按 5 小时剩余量分级填充（绿/黄/红），无 OpenCode 数据时显示 DeepSeek 余额比例（蓝），无数据为空轨道。贴边指示条按方向和长度自适应：左右贴边只有 4 段（25%，不设子刻痕）；上下贴边短/中等长度 5 段（20%）、长边 10 段（10%），网格自动取 10% 或 5%。父分割改为 3px 半透明白点，保持填色连续；每 10% 格点使用 1px 半透明白点，其他 5% 格点使用更淡的 2px 背景色刻痕，且不贯穿整条厚度；展开面板已有精确百分比文字，不叠加分隔。展开时指示条随窗口一起被拉出并同步淡出，收起时同步淡入。
- 位置持久化：保存绝对坐标、贴边方向和沿边锚点；启动时按当前屏幕恢复，分辨率变化后仍可回到原边。若启动时处于贴边状态，约 1.5 秒后自动滑出；鼠标停留在窗口上则不收起。
- 开机自启动：右键菜单写入/删除当前用户登录启动项（`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`），无需管理员权限，使用当前 exe 路径。
- 多语言：默认简体中文；右键菜单在中文界面直接显示 `English`，在英文界面显示 `中文`，点击即可切换菜单、状态、设置页与错误提示；选择会持久化。
- 卡片、标题、数值、进度条、刷新按钮、设置页和右键菜单项均提供中英文悬停说明，切换语言后同步更新。
- 右键菜单按“刷新 / 显示 / 窗口 / 通用 / 设置”分组，并提供悬停说明；三击顶部也可打开设置。
- 所有配置保存在本机 `%APPDATA%\QuotaFloat\config.json`，API Key 使用 Windows DPAPI（CurrentUser）加密，不上传。
- 单实例运行：重复启动会直接退出，避免并发覆盖配置文件；程序集版本为 `1.3.0`。

## 使用

1. 运行 `QuotaFloat.exe`。
2. 三击顶部区域，或右键 → 设置…，填入：
   - DeepSeek API Key
   - OpenCode Go API Key
3. 保存。输入框留空表示保持原 Key；设置页只显示掩码。
4. 右键菜单：
   - 全部刷新：同时刷新 DeepSeek 与 OpenCode
   - 显示余额等式：开关 DeepSeek 等式
   - 显示 DeepSeek / 显示 OpenCode：分别控制两张卡片
   - 窗口置顶
   - 截图时隐藏：从屏幕截图和录屏中排除主窗口
   - 主题 → 跟随系统 / 浅色 / 深色
   - 锁定宽度
   - 贴边吸附：开关贴边与自动滑出行为；关闭后窗口可自由摆放，不吸边
   - 开机自启动：当前用户登录时自动运行
   - `English`（中文界面）或 `中文`（英文界面），直接切换
   - 设置…
   - 退出

## 余额等式

DeepSeek 公开 API 只提供账户余额，不提供累计消费。因此“花费/总额”由用户提供并保存在本机：

- 双击 `+` 右侧或 `=` 右侧的数值开始编辑；输入期间暂停刷新。
- 输入花费后自动计算总额 = 余额 + 花费；输入总额后自动计算花费 = 总额 − 余额。
- 点击别处或回车提交并刷新等式，Esc 取消。
- 初始无总额时按 `+0` 显示。
- 更换 DeepSeek Key 会清空等式缓存，避免账号串数据。

## 构建

需要 Windows 10 全系列或 Windows 11；运行时使用系统自带的 .NET Framework 4.6+，构建机使用 .NET Framework 4.x 的 `csc.exe`，无第三方依赖：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

- 只保留 `build.ps1` 作为唯一构建入口。
- 每次构建先从仓库内所有拆分源码生成确定性的 `QuotaFloat.single.cs`，再只编译该单文件；文件纳入 Git，便于审阅、复制或单文件交付。
- 需要只导出源码而不编译时运行：`powershell -NoProfile -ExecutionPolicy Bypass -File ./build.ps1 -ExportOnly`。
- 默认 Release：`-optimize+ -debug-`，输出 `QuotaFloat.exe`，不生成 PDB，产物更小。
- Debug：追加 `-Configuration Debug`，输出 `QuotaFloat.debug.exe` 和 `.pdb`，使用 `-optimize- -debug:full -define:DEBUG -target:exe`；会打开控制台，便于交互调试。
- `DebugShell.cs` 与 `MainForm.Debug.cs` 保留在单文件源码中，并由文件级 `#if DEBUG` 包裹；Release 预处理时排除，Debug 通过 `-define:DEBUG` 启用。
- 脚本不写死编译器路径：依次检查 `-CompilerPath`、`CSC` 环境变量、PATH 中的 `csc.exe`，再在 `%WINDIR%\Microsoft.NET\Framework64` 和 `Framework` 下查找 v4.x 的 `csc.exe`。
- 导出器会移除各文件的 `using` 并放入各自的 `namespace QuotaFloat` 作用域，避免跨文件命名空间冲突；生成结果不含时间戳，重复构建保持字节稳定。
- 覆盖 `QuotaFloat.exe` 前请先退出正在运行的实例。
- 编码统一为 UTF-8：C# 源码为 UTF-8 BOM，并通过 `-codepage:65001 -utf8output` 编译；`build.ps1` 为 UTF-8 BOM；配置文件和网络读取均使用 UTF-8。

仓库内含已编译的 `QuotaFloat.exe`，Releases 中也提供同样的二进制。

每次 GitHub Release 仅提供正式版 `QuotaFloat.exe`；`QuotaFloat.debug.exe` 与 `.pdb` 仅作为本地构建产物，不上传。

## 调试

```bat
QuotaFloat.exe --demo          :: 示例数据，不读写真实配置
QuotaFloat.exe --demo --light  :: 亮色预览
QuotaFloat.exe --demo --dark   :: 暗色预览
QuotaFloat.exe --demo --wide   :: 宽窗口预览
QuotaFloat.exe --demo --narrow :: 320px 基础宽度预览（等式可能自动加宽）
QuotaFloat.exe --demo --only-ds / --only-go / --no-equation / --no-cards / --no-edge
QuotaFloat.exe --settings      :: 启动时打开设置
```

`--demo` 不会写入真实配置。

Debug 构建会打开控制台，支持以下命令：

```text
status
show
hide
dock left|right|top|bottom
move X Y
top on|off
lang en|zh
quit
```

## 回归测试

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\regression.ps1
```

脚本会构建 Debug 版本到临时目录，使用 `--demo` 启动，通过调试控制台验证贴边、隐藏、恢复、刷新间隔、显示器刷新率检测与截图排除状态，并检查中英文切换；不读取真实配置，也不依赖截图。

## 发布前检查

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\verify.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\verify.ps1 -Tag v1.3.0 -CheckRemote
```

`verify.ps1` 会重新导出并检查 `QuotaFloat.single.cs` 是否已提交、程序集版本与 Release 二进制是否一致；带 `-Tag` 时校验 tag 与版本，带 `-CheckRemote` 时确认远端 Release 只包含 `QuotaFloat.exe`。仓库根目录的 `AGENTS.md` 记录了维护与发布约束；`.github/workflows/build.yml` 在分支 push、PR 或手动触发时执行 Release/Debug 构建及上述源码/版本检查，并把正式版 `QuotaFloat.exe` 上传为 CI artifact。`.github/workflows/release.yml` 只在推送 `v*` tag 时运行：校验签名 tag 和版本后自动构建并创建 Release，且只上传 `QuotaFloat.exe`。

## 配置与隐私

- 配置文件：`%APPDATA%\QuotaFloat\config.json`
- API Key：Windows DPAPI `CurrentUser` 加密；同一 Windows 用户可解密，换用户/换机器不可解密。
- 程序仅访问 `api.deepseek.com` 和 `opencode.ai`，没有遥测或中转服务器。
- 本工具与 DeepSeek、OpenCode 官方无隶属关系；第三方接口可能变化。

## 说明

- OpenCode Go 的额度接口不是公开稳定契约，字段或地址变化时可能需要调整。
- DeepSeek 的峰谷时段按 2026 年规则硬编码（UTC+8），如官方调整需更新 `IsPeakNow()`。
