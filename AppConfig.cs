using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace QuotaFloat
{
    internal sealed class AppConfig
    {
        public const int MinWindowWidth = 320;
        public const int MaxWindowWidth = 720;
        public const int DefaultWindowWidth = 380;
        public const int ThemeFollowSystem = 0;
        public const int ThemeLight = 1;
        public const int ThemeDark = 2;
        public const int UnsetCoordinate = -999999;
        public const double DefaultEdgeAnchor = 0.5;

        public string DeepSeekKey = "";
        public string OpenCodeKey = "";
        public int WindowWidth = DefaultWindowWidth;
        public bool WidthLocked;
        public bool EdgeHide = true;
        public int EdgeDock = EdgeMetrics.DockNone;
        public double EdgeAnchor = DefaultEdgeAnchor;
        public int ThemeMode = ThemeFollowSystem;
        public bool EquationEnabled = true;
        public bool ShowDeepSeek = true;
        public bool ShowOpenCode = true;
        public decimal DeepSeekTotal;
        public bool DeepSeekHasTotal;
        public decimal DeepSeekBalance;
        public decimal DeepSeekSpent;
        public string DeepSeekCurrency = "CNY";
        public string Language = "";
        public bool DeepSeekBalanceKnown;
        public long DeepSeekCacheUtcTicks;
        public bool OpenCodeCacheKnown;
        public long OpenCodeCacheUtcTicks;
        public int OpenCodeRollingUsed;
        public bool OpenCodeRollingHasData;
        public bool OpenCodeRollingRateLimited;
        public long OpenCodeRollingResetsUtcTicks;
        public int OpenCodeWeeklyUsed;
        public bool OpenCodeWeeklyHasData;
        public bool OpenCodeWeeklyRateLimited;
        public long OpenCodeWeeklyResetsUtcTicks;
        public int OpenCodeMonthlyUsed;
        public bool OpenCodeMonthlyHasData;
        public bool OpenCodeMonthlyRateLimited;
        public long OpenCodeMonthlyResetsUtcTicks;
        public int X = UnsetCoordinate;
        public int Y = UnsetCoordinate;
        public bool TopMost = true;
        public bool HideFromCapture = true;
        public bool NoSave;

        public static string ConfigPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "QuotaFloat", "config.json");
            }
        }

        public static AppConfig Load()
        {
            AppConfig c = new AppConfig();
            try
            {
                if (!File.Exists(ConfigPath)) return c;
                string json = File.ReadAllText(ConfigPath, Encoding.UTF8);
                JavaScriptSerializer js = new JavaScriptSerializer();
                Dictionary<string, object> d = js.Deserialize<Dictionary<string, object>>(json);
                if (d == null) return c;
                c.DeepSeekKey = Unprotect(GetStr(d, "deepseekKey"));
                c.OpenCodeKey = Unprotect(GetStr(d, "opencodeKey"));
                c.WindowWidth = GetInt(d, "windowWidth", DefaultWindowWidth);
                if (c.WindowWidth < MinWindowWidth) c.WindowWidth = MinWindowWidth;
                if (c.WindowWidth > MaxWindowWidth) c.WindowWidth = MaxWindowWidth;
                c.WidthLocked = GetBool(d, "widthLocked", false);
                c.EdgeHide = GetBool(d, "edgeHide", true);
                c.EdgeDock = GetInt(d, "edgeDock", EdgeMetrics.DockNone);
                if (c.EdgeDock < EdgeMetrics.DockNone || c.EdgeDock > EdgeMetrics.DockBottom)
                    c.EdgeDock = EdgeMetrics.DockNone;
                c.EdgeAnchor = GetDouble(d, "edgeAnchor", DefaultEdgeAnchor);
                if (c.EdgeAnchor < 0.0) c.EdgeAnchor = 0.0;
                if (c.EdgeAnchor > 1.0) c.EdgeAnchor = 1.0;
                c.ThemeMode = GetInt(d, "themeMode", ThemeFollowSystem);
                if (c.ThemeMode < ThemeFollowSystem || c.ThemeMode > ThemeDark)
                    c.ThemeMode = ThemeFollowSystem;
                c.EquationEnabled = GetBool(d, "equationEnabled", true);
                c.ShowDeepSeek = GetBool(d, "showDeepSeek", true);
                c.ShowOpenCode = GetBool(d, "showOpenCode", true);
                c.DeepSeekTotal = GetDecimal(d, "deepseekTotal", 0m);
                c.DeepSeekHasTotal = GetBool(d, "deepseekHasTotal", false);
                c.DeepSeekBalance = GetDecimal(d, "deepseekBalance", 0m);
                c.DeepSeekSpent = GetDecimal(d, "deepseekSpent", 0m);
                c.DeepSeekCurrency = GetStr(d, "deepseekCurrency");
                if (string.IsNullOrEmpty(c.DeepSeekCurrency)) c.DeepSeekCurrency = "CNY";
                c.Language = GetStr(d, "language");
                c.DeepSeekBalanceKnown = GetBool(d, "deepseekBalanceKnown", false);
                c.DeepSeekCacheUtcTicks = GetLong(d, "deepseekCacheUtcTicks", 0);
                c.OpenCodeCacheKnown = GetBool(d, "opencodeCacheKnown", false);
                c.OpenCodeCacheUtcTicks = GetLong(d, "opencodeCacheUtcTicks", 0);
                c.OpenCodeRollingUsed = GetInt(d, "opencodeRollingUsed", 0);
                c.OpenCodeRollingHasData = GetBool(d, "opencodeRollingHasData", false);
                c.OpenCodeRollingRateLimited = GetBool(d, "opencodeRollingRateLimited", false);
                c.OpenCodeRollingResetsUtcTicks = GetLong(d, "opencodeRollingResetsUtcTicks", 0);
                c.OpenCodeWeeklyUsed = GetInt(d, "opencodeWeeklyUsed", 0);
                c.OpenCodeWeeklyHasData = GetBool(d, "opencodeWeeklyHasData", false);
                c.OpenCodeWeeklyRateLimited = GetBool(d, "opencodeWeeklyRateLimited", false);
                c.OpenCodeWeeklyResetsUtcTicks = GetLong(d, "opencodeWeeklyResetsUtcTicks", 0);
                c.OpenCodeMonthlyUsed = GetInt(d, "opencodeMonthlyUsed", 0);
                c.OpenCodeMonthlyHasData = GetBool(d, "opencodeMonthlyHasData", false);
                c.OpenCodeMonthlyRateLimited = GetBool(d, "opencodeMonthlyRateLimited", false);
                c.OpenCodeMonthlyResetsUtcTicks = GetLong(d, "opencodeMonthlyResetsUtcTicks", 0);
                c.X = GetInt(d, "x", UnsetCoordinate);
                c.Y = GetInt(d, "y", UnsetCoordinate);
                c.TopMost = GetBool(d, "topMost", true);
                c.HideFromCapture = GetBool(d, "hideFromCapture", true);
            }
            catch { }
            return c;
        }

        public void Save()
        {
            if (NoSave) return;
            try
            {
                Dictionary<string, object> d = new Dictionary<string, object>();
                d["deepseekKey"] = Protect(DeepSeekKey);
                d["opencodeKey"] = Protect(OpenCodeKey);
                d["windowWidth"] = WindowWidth;
                d["widthLocked"] = WidthLocked;
                d["edgeHide"] = EdgeHide;
                d["edgeDock"] = EdgeDock;
                d["edgeAnchor"] = EdgeAnchor;
                d["themeMode"] = ThemeMode;
                d["equationEnabled"] = EquationEnabled;
                d["showDeepSeek"] = ShowDeepSeek;
                d["showOpenCode"] = ShowOpenCode;
                d["deepseekTotal"] = DeepSeekTotal;
                d["deepseekHasTotal"] = DeepSeekHasTotal;
                d["deepseekBalance"] = DeepSeekBalance;
                d["deepseekSpent"] = DeepSeekSpent;
                d["deepseekCurrency"] = DeepSeekCurrency;
                d["language"] = Language;
                d["deepseekBalanceKnown"] = DeepSeekBalanceKnown;
                d["deepseekCacheUtcTicks"] = DeepSeekCacheUtcTicks;
                d["opencodeCacheKnown"] = OpenCodeCacheKnown;
                d["opencodeCacheUtcTicks"] = OpenCodeCacheUtcTicks;
                d["opencodeRollingUsed"] = OpenCodeRollingUsed;
                d["opencodeRollingHasData"] = OpenCodeRollingHasData;
                d["opencodeRollingRateLimited"] = OpenCodeRollingRateLimited;
                d["opencodeRollingResetsUtcTicks"] = OpenCodeRollingResetsUtcTicks;
                d["opencodeWeeklyUsed"] = OpenCodeWeeklyUsed;
                d["opencodeWeeklyHasData"] = OpenCodeWeeklyHasData;
                d["opencodeWeeklyRateLimited"] = OpenCodeWeeklyRateLimited;
                d["opencodeWeeklyResetsUtcTicks"] = OpenCodeWeeklyResetsUtcTicks;
                d["opencodeMonthlyUsed"] = OpenCodeMonthlyUsed;
                d["opencodeMonthlyHasData"] = OpenCodeMonthlyHasData;
                d["opencodeMonthlyRateLimited"] = OpenCodeMonthlyRateLimited;
                d["opencodeMonthlyResetsUtcTicks"] = OpenCodeMonthlyResetsUtcTicks;
                d["x"] = X;
                d["y"] = Y;
                d["topMost"] = TopMost;
                d["hideFromCapture"] = HideFromCapture;
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                File.WriteAllText(ConfigPath,
                    new JavaScriptSerializer().Serialize(d), new UTF8Encoding(false));
            }
            catch { }
        }

        public void ClearOpenCodeCache()
        {
            OpenCodeCacheKnown = false;
            OpenCodeCacheUtcTicks = 0;
            OpenCodeRollingUsed = 0;
            OpenCodeRollingHasData = false;
            OpenCodeRollingRateLimited = false;
            OpenCodeRollingResetsUtcTicks = 0;
            OpenCodeWeeklyUsed = 0;
            OpenCodeWeeklyHasData = false;
            OpenCodeWeeklyRateLimited = false;
            OpenCodeWeeklyResetsUtcTicks = 0;
            OpenCodeMonthlyUsed = 0;
            OpenCodeMonthlyHasData = false;
            OpenCodeMonthlyRateLimited = false;
            OpenCodeMonthlyResetsUtcTicks = 0;
        }

        private static string Protect(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            try
            {
                byte[] b = ProtectedData.Protect(Encoding.UTF8.GetBytes(s), null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(b);
            }
            catch { return ""; }
        }

        private static string Unprotect(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            try
            {
                byte[] b = ProtectedData.Unprotect(Convert.FromBase64String(s), null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(b);
            }
            catch { return ""; }
        }

        private static string GetStr(Dictionary<string, object> d, string k)
        {
            object v;
            return d.TryGetValue(k, out v) && v != null ? Convert.ToString(v) : "";
        }

        private static int GetInt(Dictionary<string, object> d, string k, int def)
        {
            object v;
            if (!d.TryGetValue(k, out v) || v == null) return def;
            try { return Convert.ToInt32(v, CultureInfo.InvariantCulture); }
            catch { return def; }
        }

        private static decimal GetDecimal(Dictionary<string, object> d, string k, decimal def)
        {
            object v;
            if (!d.TryGetValue(k, out v) || v == null) return def;
            try { return Convert.ToDecimal(v, CultureInfo.InvariantCulture); }
            catch { return def; }
        }

        private static double GetDouble(Dictionary<string, object> d, string k, double def)
        {
            object v;
            if (!d.TryGetValue(k, out v) || v == null) return def;
            try { return Convert.ToDouble(v, CultureInfo.InvariantCulture); }
            catch { return def; }
        }

        private static long GetLong(Dictionary<string, object> d, string k, long def)
        {
            object v;
            if (!d.TryGetValue(k, out v) || v == null) return def;
            try { return Convert.ToInt64(v, CultureInfo.InvariantCulture); }
            catch { return def; }
        }

        private static bool GetBool(Dictionary<string, object> d, string k, bool def)
        {
            object v;
            if (!d.TryGetValue(k, out v) || v == null) return def;
            try { return Convert.ToBoolean(v); }
            catch { return def; }
        }
    }
}
