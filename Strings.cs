using System;
using System.Globalization;

namespace QuotaFloat
{
    internal static class Strings
    {
        public const string ZhCn = "zh-CN";
        public const string En = "en";

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

        public static string Remaining(int percent)
        {
            return IsZh ? "剩余 " + percent.ToString(CultureInfo.InvariantCulture) + "%"
                        : percent.ToString(CultureInfo.InvariantCulture) + "% left";
        }

        public static string RateLimited(string countdown)
        {
            return IsZh ? "耗尽 · " + countdown : "Exhausted · " + countdown;
        }

        public static string RefreshNow { get { return T("立即刷新", "Refresh now"); } }
        public static string ShowEquation { get { return T("显示余额等式", "Show balance equation"); } }
        public static string ShowDeepSeek { get { return T("显示 DeepSeek", "Show DeepSeek"); } }
        public static string ShowOpenCode { get { return T("显示 OpenCode", "Show OpenCode"); } }
        public static string AlwaysOnTop { get { return T("窗口置顶", "Always on top"); } }
        public static string Theme { get { return T("主题", "Theme"); } }
        public static string FollowSystem { get { return T("跟随系统", "Follow system"); } }
        public static string Light { get { return T("浅色", "Light"); } }
        public static string Dark { get { return T("深色", "Dark"); } }
        public static string LockWidth { get { return T("锁定宽度", "Lock width"); } }
        public static string EdgeSnap { get { return T("贴边吸附", "Snap to edge"); } }
        public static string EdgeSnapTip { get { return T("拖到屏幕边缘时吸附，鼠标移出后自动滑出", "Snap at a screen edge and slide out when the mouse leaves"); } }
        public static string AutoStart { get { return T("开机自启动", "Start with Windows"); } }
        public static string AutoStartTip { get { return T("当前用户登录时自动启动", "Start automatically when the current user signs in"); } }
        public static string Language { get { return T("语言", "Language"); } }
        public static string English { get { return "English"; } }
        public static string Chinese { get { return "简体中文"; } }
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
