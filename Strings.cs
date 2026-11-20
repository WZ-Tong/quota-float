using System;
using System.Globalization;

namespace QuotaFloat
{
    internal static class Strings
    {
        public const string ZhCn = "zh-CN";
        public const string En = "en";
        private const string TooltipDateFormat = "yyyy-MM-dd HH:mm:ss";

        public static string Code = ZhCn;

        public static bool IsZh
        {
            get { return Code == ZhCn; }
        }

        public static string SystemDefault
        {
            get
            {
                return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? ZhCn : En;
            }
        }

        public static string Normalize(string code)
        {
            if (code == En) return En;
            if (code == ZhCn) return ZhCn;
            return SystemDefault;
        }

        private static string T(string zh, string en)
        {
            return IsZh ? zh : en;
        }

        public static string AppTitle { get { return T("AI 额度", "AI Quota"); } }
        public static string AppTitleDemo { get { return T("AI 额度 (demo)", "AI Quota (demo)"); } }
        public static string Loading { get { return T("正在读取…", "Loading..."); } }
        public static string NotConfiguredHint { get { return T("未配置 · 三击设置", "Not configured - triple-click to set"); } }
        public static string NotConfiguredKey { get { return T("未配置 Key", "No key"); } }
        public static string WaitingData { get { return T("等待数据", "Waiting for data"); } }
        public static string FiveHours { get { return T("5 小时", "5 hours"); } }
        public static string ThisWeek { get { return T("本周", "Week"); } }
        public static string ThisMonth { get { return T("本月", "Month"); } }
        public static string Peak { get { return T("峰时", "Peak"); } }
        public static string OffPeak { get { return T("谷时", "Off-peak"); } }
        public static string NotSet { get { return T("未设置", "Not set"); } }
        public static string DeepSeekName { get { return "DeepSeek"; } }
        public static string OpenCodeName { get { return "OpenCode"; } }

        public static string WindowTip { get { return T("拖动空白处移动；三击顶部打开设置", "Drag empty space to move; triple-click the header for settings"); } }
        public static string DeepSeekCardTip { get { return T("DeepSeek 账户余额", "DeepSeek account balance"); } }
        public static string DeepSeekTitleTip { get { return T("DeepSeek 余额卡片", "DeepSeek balance card"); } }
        public static string DeepSeekPeriodTip { get { return T("DeepSeek API 峰谷时段（UTC+8）", "DeepSeek API peak and off-peak hours (UTC+8)"); } }
        public static string DeepSeekStatusTip { get { return T("DeepSeek 查询状态与错误信息", "DeepSeek request status and errors"); } }
        public static string DeepSeekBalanceTip { get { return T("当前 DeepSeek 账户余额", "Current DeepSeek account balance"); } }
        public static string EquationTip { get { return T("余额 = 花费与总额的关系；双击数值可编辑", "Balance with spent and total; double-click a value to edit"); } }
        public static string BalanceTip { get { return T("当前余额", "Current balance"); } }
        public static string SpentTip { get { return T("已花费；双击编辑", "Spent; double-click to edit"); } }
        public static string TotalTip { get { return T("总额；双击编辑", "Total; double-click to edit"); } }
        public static string PlusTip { get { return T("花费", "Spent"); } }
        public static string EqualsTip { get { return T("总额", "Total"); } }
        public static string DeepSeekBarTip { get { return T("余额占总额比例", "Balance as a share of total"); } }
        public static string RefreshBarTip { get { return T("距离下次自动刷新", "Time until the next automatic refresh"); } }
        public static string InlineEditorTip { get { return T("回车提交，Esc 取消", "Press Enter to apply, Esc to cancel"); } }
        public static string OpenCodeCardTip { get { return T("OpenCode Go 额度窗口", "OpenCode Go quota windows"); } }
        public static string OpenCodeTitleTip { get { return T("OpenCode 额度卡片", "OpenCode quota card"); } }
        public static string FiveHoursTip { get { return T("5 小时滚动窗口中的剩余额度", "Remaining in the 5-hour rolling window"); } }
        public static string WeekTip { get { return T("本周剩余额度", "Remaining this week"); } }
        public static string MonthTip { get { return T("本月剩余额度", "Remaining this month"); } }
        public static string ThemeTip { get { return T("选择浅色、深色或跟随系统", "Choose light, dark, or follow the system"); } }
        public static string FollowSystemTip { get { return T("跟随 Windows 应用主题", "Follow the Windows app theme"); } }
        public static string LightTip { get { return T("始终使用浅色主题", "Always use the light theme"); } }
        public static string DarkTip { get { return T("始终使用深色主题", "Always use the dark theme"); } }
        public static string LanguageTip { get { return T("切换界面语言", "Switch interface language"); } }
        public static string SettingsTip { get { return T("配置 API Key", "Configure API keys"); } }
        public static string ExitTip { get { return T("关闭 QuotaFloat", "Close QuotaFloat"); } }
        public static string DeepSeekApiKey { get { return T("DeepSeek API 密钥", "DeepSeek API Key"); } }
        public static string OpenCodeApiKey { get { return T("OpenCode API 密钥", "OpenCode API Key"); } }
        public static string CurrentKeyTip { get { return T("当前 Key 的掩码", "Masked current key"); } }
        public static string ApiKeyInputTip { get { return T("留空则保持当前 Key", "Leave blank to keep the current key"); } }
        public static string SaveTip { get { return T("保存并立即刷新", "Save and refresh now"); } }
        public static string CancelTip { get { return T("放弃修改并关闭", "Discard changes and close"); } }
        public static string SettingsFormTip { get { return T("API Key 使用 Windows DPAPI 加密保存在本机", "API keys are stored locally and protected with Windows DPAPI"); } }

        public static string Remaining(int percent)
        {
            return IsZh ? "剩余 " + percent.ToString(CultureInfo.InvariantCulture) + "%"
                        : percent.ToString(CultureInfo.InvariantCulture) + "% left";
        }

        public static string RateLimited(string countdown)
        {
            return IsZh ? "耗尽 · " + countdown : "Exhausted · " + countdown;
        }

        public static string StalePrefix { get { return T("已过期 · ", "Stale · "); } }

        public static string LastSuccess(DateTime? time)
        {
            if (!time.HasValue) return T("最后成功：尚无记录", "Last success: none");
            string value = time.Value.ToString(TooltipDateFormat, CultureInfo.InvariantCulture);
            return T("最后成功：" + value, "Last success: " + value);
        }

        public static string ConsecutiveFailures(int count)
        {
            return IsZh
                ? "连续失败：" + count.ToString(CultureInfo.InvariantCulture) + " 次"
                : "Consecutive failures: " + count.ToString(CultureInfo.InvariantCulture);
        }

        public static string RefreshNow { get { return T("全部刷新", "Refresh all"); } }
        public static string RefreshNowTip { get { return T("同时刷新 DeepSeek 与 OpenCode", "Refresh DeepSeek and OpenCode"); } }
        public static string RefreshDeepSeek { get { return T("刷新 DeepSeek", "Refresh DeepSeek"); } }
        public static string RefreshDeepSeekTip { get { return T("手动刷新 DeepSeek 余额", "Refresh the DeepSeek balance"); } }
        public static string RefreshOpenCode { get { return T("刷新 OpenCode", "Refresh OpenCode"); } }
        public static string RefreshOpenCodeTip { get { return T("手动刷新 OpenCode 额度", "Refresh the OpenCode quota"); } }
        public static string ShowEquation { get { return T("显示余额等式", "Show balance equation"); } }
        public static string ShowEquationTip { get { return T("在 DeepSeek 卡片中显示余额等式", "Show the balance equation in the DeepSeek card"); } }
        public static string ShowDeepSeek { get { return T("显示 DeepSeek", "Show DeepSeek"); } }
        public static string ShowDeepSeekTip { get { return T("显示 DeepSeek 余额卡片", "Show the DeepSeek balance card"); } }
        public static string ShowOpenCode { get { return T("显示 OpenCode", "Show OpenCode"); } }
        public static string ShowOpenCodeTip { get { return T("显示 OpenCode 额度卡片", "Show the OpenCode quota card"); } }
        public static string AlwaysOnTop { get { return T("窗口置顶", "Always on top"); } }
        public static string AlwaysOnTopTip { get { return T("保持窗口位于其他窗口之上", "Keep the window above other windows"); } }
        public static string CaptureHide { get { return T("截图时隐藏", "Hide from capture"); } }
        public static string CaptureHideTip { get { return T("从屏幕截图和录屏中排除此窗口", "Exclude this window from screenshots and screen recording"); } }
        public static string Theme { get { return T("主题", "Theme"); } }
        public static string FollowSystem { get { return T("跟随系统", "Follow system"); } }
        public static string Light { get { return T("浅色", "Light"); } }
        public static string Dark { get { return T("深色", "Dark"); } }
        public static string LockWidth { get { return T("锁定宽度", "Lock width"); } }
        public static string LockWidthTip { get { return T("禁止拖动左右边缘调整宽度", "Prevent resizing by dragging the side edges"); } }
        public static string EdgeSnap { get { return T("贴边吸附", "Snap to edge"); } }
        public static string EdgeSnapTip { get { return T("拖到屏幕边缘时吸附，鼠标移出后自动滑出", "Snap at a screen edge and slide out when the mouse leaves"); } }
        public static string AutoStart { get { return T("开机自启动", "Start with Windows"); } }
        public static string AutoStartTip { get { return T("当前用户登录时自动启动", "Start automatically when the current user signs in"); } }
        public static string LanguageSwitch { get { return IsZh ? "English" : "中文"; } }
        public static string OtherLanguageCode { get { return IsZh ? En : ZhCn; } }
        public static string SettingsEllipsis { get { return T("设置…", "Settings..."); } }
        public static string Exit { get { return T("退出", "Exit"); } }
        public static string Settings { get { return T("设置", "Settings"); } }
        public static string Save { get { return T("保存", "Save"); } }
        public static string Cancel { get { return T("取消", "Cancel"); } }

        public static string CurrentKey(string masked)
        {
            return T("当前 ", "Current ") + masked;
        }

        public static string QueryFailed { get { return T("查询失败", "Request failed"); } }
        public static string ResponseParseFailed { get { return T("响应解析失败", "Response parse failed"); } }
        public static string AccountUnavailable { get { return T("账户余额不可用", "Account balance unavailable"); } }
        public static string NoBalanceInfo { get { return T("无余额信息", "No balance data"); } }
        public static string MissingUsage { get { return T("响应缺少 usage", "Response missing usage"); } }

        public static string ParseFailed(string message)
        {
            return T("解析失败：", "Parse failed: ") + message;
        }

        public static string InvalidKey(int status) { return T("Key 无效", "Invalid key") + " (" + status.ToString(CultureInfo.InvariantCulture) + ")"; }
        public static string InsufficientBalance(int status) { return T("余额不足", "Insufficient balance") + " (" + status.ToString(CultureInfo.InvariantCulture) + ")"; }
        public static string Forbidden(int status) { return T("无访问权限", "Access denied") + " (" + status.ToString(CultureInfo.InvariantCulture) + ")"; }
        public static string TooManyRequests(int status) { return T("请求过多", "Too many requests") + " (" + status.ToString(CultureInfo.InvariantCulture) + ")"; }
        public static string Timeout { get { return T("连接超时", "Connection timed out"); } }
        public static string DnsFailure { get { return T("域名解析失败", "DNS lookup failed"); } }
        public static string ConnectFailure { get { return T("无法连接", "Cannot connect"); } }
        public static string NetworkError { get { return T("网络错误", "Network error"); } }

        public static string AutoStartFailed(string message)
        {
            return T("无法设置开机自启动：", "Failed to set startup:") + "\n" + message;
        }
    }
}
