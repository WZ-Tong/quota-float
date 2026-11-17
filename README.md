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
- 贴边滑出：拖到屏幕边缘松开后吸附；鼠标移出约 0.45 秒后，窗口用动画滑出屏幕，只在边缘保留 10px 的边；鼠标移入立即滑回，停留期间不自动收起。滑出只改位置，窗口宽高保持原尺寸。贴边时边缘指示条显示剩余比例，与内部进度条一致（100% 为全填充），长度几乎贯穿整条边；轨道使用更深底色标出 100% 范围。OpenCode 按 5 小时剩余量分级填充（绿/黄/红），无 OpenCode 数据时显示 DeepSeek 余额比例（蓝），无数据为空轨道。展开时指示条随窗口一起被拉出并同步淡出，收起时同步淡入。
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

Windows 10/11 自带 .NET Framework 4.x，可直接用系统 `csc.exe` 编译，无第三方依赖：

```bat
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" ^
  -nologo -codepage:65001 -target:winexe -optimize+ ^
  -win32manifest:app.manifest -win32icon:app.ico ^
  -out:QuotaFloat.exe ^
  -reference:System.dll -reference:System.Core.dll ^
  -reference:System.Drawing.dll -reference:System.Windows.Forms.dll ^
  -reference:System.Web.Extensions.dll -reference:System.Security.dll ^
  QuotaFloat.cs
```

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

## 配置与隐私

- 配置文件：`%APPDATA%\QuotaFloat\config.json`
- API Key：Windows DPAPI `CurrentUser` 加密；同一 Windows 用户可解密，换用户/换机器不可解密。
- 程序仅访问 `api.deepseek.com` 和 `opencode.ai`，没有遥测或中转服务器。
- 本工具与 DeepSeek、OpenCode 官方无隶属关系；第三方接口可能变化。

## 说明

- OpenCode Go 的额度接口不是公开稳定契约，字段或地址变化时可能需要调整。
- DeepSeek 的峰谷时段按 2026 年规则硬编码（UTC+8），如官方调整需更新 `IsPeakNow()`。
