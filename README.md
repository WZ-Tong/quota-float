# QuotaFloat

一个用于 Windows 的轻量悬浮窗，同时显示 DeepSeek API 余额和 OpenCode Go 套餐额度。

![icon](icon.svg)

## 下载

- [最新版本](https://github.com/WZ-Tong/quota-float/releases/latest)（`QuotaFloat.exe`）

## 功能

- DeepSeek 余额；可选余额等式：`余额 + 花费 = 总额`，双击数值原位编辑，自动算出另一项；初始无总额时按 `+0` 显示。
- DeepSeek 峰谷提示：周一至周五 `09:00–12:00`、`14:00–18:00`（UTC+8）为峰时（红），其余时间及周末为谷时（绿）。
- OpenCode Go：5 小时 / 本周 / 本月剩余百分比、进度条和 `0d 01h 23m 45s` 形式的重置倒计时。
- 每 60 秒自动刷新，卡片间细条显示下次刷新进度。
- 黑/白主题：默认跟随系统，也可右键强制浅色/深色。
- 无标题栏；不出现在任务栏和 Alt+Tab；左右边缘可直接拖动宽度，支持锁定并持久化。
- 贴边滑出：拖到屏幕边缘松开后吸附；鼠标移出约 0.45 秒后，窗口用动画滑出屏幕，只在边缘保留 10px 的边；鼠标在边缘停留约 0.2 秒后滑回，停留期间不自动收起。滑出只改位置，窗口宽高保持原尺寸。贴边时边缘指示条显示剩余比例，与内部进度条一致（100% 为全填充），长度几乎贯穿整条边；轨道使用更深底色标出 100% 范围。OpenCode 按 5 小时剩余量分级填充（绿/黄/红），无 OpenCode 数据时显示 DeepSeek 余额比例（蓝），无数据为空轨道。贴边指示条按长度自适应分段：短边 4 段（25%，3px 间隙）、中等长度 5 段（20%，5px 间隙）、长边 10 段（10%，5px 间隙）。每段中间另有一条低对比 2px 细线，可把精度提高到 12.5%/10%/5%；展开面板已有精确百分比文字，不叠加分隔。展开时指示条随窗口一起被拉出并同步淡出，收起时同步淡入。
- 位置持久化：保存绝对坐标、贴边方向和沿边锚点；启动时按当前屏幕恢复，分辨率变化后仍可回到原边。若启动时处于贴边状态，约 1.5 秒后自动滑出；鼠标停留在窗口上则不收起。
- 开机自启动：右键菜单写入/删除当前用户登录启动项（`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`），无需管理员权限，使用当前 exe 路径。
- 右键菜单提供设置、主题、等式开关、宽度锁定和退出；三击顶部也可打开设置。
- 所有配置保存在本机 `%APPDATA%\QuotaFloat\config.json`，API Key 使用 Windows DPAPI（CurrentUser）加密，不上传。

## 使用

1. 运行 `QuotaFloat.exe`。
2. 三击顶部区域，或右键 → 设置…，填入：
   - DeepSeek API Key
   - OpenCode Go API Key
3. 保存。输入框留空表示保持原 Key；设置页只显示掩码。
4. 右键菜单：
   - 立即刷新
   - 显示余额等式：开关 DeepSeek 等式
   - 显示 DeepSeek / 显示 OpenCode：分别控制两张卡片
   - 窗口置顶
   - 主题 → 跟随系统 / 浅色 / 深色
   - 锁定宽度
   - 贴边吸附：开关贴边与自动滑出行为；关闭后窗口可自由摆放，不吸边
   - 开机自启动：当前用户登录时自动运行
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

需要 Windows 10/11 自带的 .NET Framework 4.x，无第三方依赖：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

- 只保留 `build.ps1` 作为唯一构建入口。
- 默认 Release：`-optimize+ -debug-`，输出 `QuotaFloat.exe`，不生成 PDB，产物更小。
- Debug：追加 `-Configuration Debug`，输出 `QuotaFloat.debug.exe` 和 `.pdb`，使用 `-optimize- -debug:full -define:DEBUG -target:exe`；会打开控制台，便于交互调试。
- `DebugShell.cs` 与 `MainForm.Debug.cs` 只在 Debug 构建中参与编译；Release 源码列表会排除它们，且文件内部另有 `#if DEBUG` 双保险。
- 脚本不写死编译器路径：依次检查 `-CompilerPath`、`CSC` 环境变量、PATH 中的 `csc.exe`，再在 `%WINDIR%\Microsoft.NET\Framework64` 和 `Framework` 下查找 v4.x 的 `csc.exe`。
- 自动编译仓库内所有 `.cs`（排除 `bin`、`obj`），后续拆分源码无需改构建命令。
- 覆盖 `QuotaFloat.exe` 前请先退出正在运行的实例。
- 编码统一为 UTF-8：C# 源码为 UTF-8 BOM，并通过 `-codepage:65001 -utf8output` 编译；`build.ps1` 为 UTF-8 BOM；配置文件和网络读取均使用 UTF-8。

仓库内含已编译的 `QuotaFloat.exe`，Releases 中也提供同样的二进制。

## 调试

```bat
QuotaFloat.exe --demo          :: 示例数据，不读写真实配置
QuotaFloat.exe --demo --light  :: 亮色预览
QuotaFloat.exe --demo --dark   :: 暗色预览
QuotaFloat.exe --demo --wide   :: 宽窗口预览
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
quit
```

## 回归测试

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\regression.ps1
```

脚本会构建 Debug 版本到临时目录，使用 `--demo` 启动，通过调试控制台验证贴边、隐藏和恢复，不读取真实配置，也不依赖截图。

## 配置与隐私

- 配置文件：`%APPDATA%\QuotaFloat\config.json`
- API Key：Windows DPAPI `CurrentUser` 加密；同一 Windows 用户可解密，换用户/换机器不可解密。
- 程序仅访问 `api.deepseek.com` 和 `opencode.ai`，没有遥测或中转服务器。
- 本工具与 DeepSeek、OpenCode 官方无隶属关系；第三方接口可能变化。

## 说明

- OpenCode Go 的额度接口不是公开稳定契约，字段或地址变化时可能需要调整。
- DeepSeek 的峰谷时段按 2026 年规则硬编码（UTC+8），如官方调整需更新 `IsPeakNow()`。
