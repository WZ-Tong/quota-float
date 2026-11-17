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
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try { ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; }
            catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal static class Palette
    {
        public static Color Bg = Color.FromArgb(24, 24, 28);
        public static Color Card = Color.FromArgb(36, 36, 42);
        public static Color CardBorder = Color.FromArgb(52, 52, 60);
        public static Color WindowBorder = Color.FromArgb(58, 58, 68);
        public static Color Text = Color.FromArgb(240, 240, 245);
        public static Color Sub = Color.FromArgb(150, 150, 162);
        public static Color Green = Color.FromArgb(61, 214, 140);
        public static Color Yellow = Color.FromArgb(245, 200, 76);
        public static Color Red = Color.FromArgb(242, 109, 109);
        public static Color Blue = Color.FromArgb(77, 107, 254);
        public static Color BarBg = Color.FromArgb(58, 58, 68);
        public static Color InputBg = Color.FromArgb(46, 46, 54);

        public static void Apply(bool light)
        {
            if (light)
            {
                Bg = Color.FromArgb(236, 236, 242);
                Card = Color.FromArgb(252, 252, 255);
                CardBorder = Color.FromArgb(224, 224, 232);
                WindowBorder = Color.FromArgb(200, 200, 212);
                Text = Color.FromArgb(28, 28, 33);
                Sub = Color.FromArgb(96, 96, 108);
                Green = Color.FromArgb(16, 148, 84);
                Yellow = Color.FromArgb(184, 124, 0);
                Red = Color.FromArgb(196, 56, 56);
                Blue = Color.FromArgb(64, 88, 236);
                BarBg = Color.FromArgb(226, 226, 234);
                InputBg = Color.FromArgb(238, 238, 244);
            }
            else
            {
                Bg = Color.FromArgb(20, 20, 24);
                Card = Color.FromArgb(36, 36, 42);
                CardBorder = Color.FromArgb(52, 52, 60);
                WindowBorder = Color.FromArgb(58, 58, 68);
                Text = Color.FromArgb(240, 240, 245);
                Sub = Color.FromArgb(150, 150, 162);
                Green = Color.FromArgb(61, 214, 140);
                Yellow = Color.FromArgb(245, 200, 76);
                Red = Color.FromArgb(242, 109, 109);
                Blue = Color.FromArgb(77, 107, 254);
                BarBg = Color.FromArgb(58, 58, 68);
                InputBg = Color.FromArgb(46, 46, 54);
            }
        }
    }

    internal static class Ui
    {
        public static GraphicsPath Rounded(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d < 2) d = 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static Color LevelColor(int remaining)
        {
            if (remaining <= 5) return Palette.Red;
            if (remaining <= 20) return Palette.Yellow;
            return Palette.Green;
        }

        public static string MaskKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return "未设置";
            string prefix = "";
            if (key.StartsWith("oc_sk_", StringComparison.OrdinalIgnoreCase)) prefix = key.Substring(0, 6);
            else if (key.StartsWith("sk-", StringComparison.OrdinalIgnoreCase)) prefix = key.Substring(0, 3);
            string tail = key.Length >= 4 ? key.Substring(key.Length - 4) : "";
            return prefix + "****" + tail;
        }
    }

    internal sealed class AppConfig
    {
        public string DeepSeekKey = "";
        public string OpenCodeKey = "";
        public int WindowWidth = 380;
        public bool WidthLocked;
        public int ThemeMode;
        public bool EquationEnabled = true;
        public bool ShowDeepSeek = true;
        public bool ShowOpenCode = true;
        public decimal DeepSeekTotal;
        public bool DeepSeekHasTotal;
        public decimal DeepSeekBalance;
        public decimal DeepSeekSpent;
        public string DeepSeekCurrency = "CNY";
        public bool DeepSeekBalanceKnown;
        public int X = -999999;
        public int Y = -999999;
        public bool TopMost = true;
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
                c.WindowWidth = GetInt(d, "windowWidth", 380);
                if (c.WindowWidth < 360) c.WindowWidth = 360;
                if (c.WindowWidth > 720) c.WindowWidth = 720;
                c.WidthLocked = GetBool(d, "widthLocked", false);
                c.ThemeMode = GetInt(d, "themeMode", 0);
                if (c.ThemeMode < 0 || c.ThemeMode > 2) c.ThemeMode = 0;
                c.EquationEnabled = GetBool(d, "equationEnabled", true);
                c.ShowDeepSeek = GetBool(d, "showDeepSeek", true);
                c.ShowOpenCode = GetBool(d, "showOpenCode", true);
                c.DeepSeekTotal = GetDecimal(d, "deepseekTotal", 0m);
                c.DeepSeekHasTotal = GetBool(d, "deepseekHasTotal", false);
                c.DeepSeekBalance = GetDecimal(d, "deepseekBalance", 0m);
                c.DeepSeekSpent = GetDecimal(d, "deepseekSpent", 0m);
                c.DeepSeekCurrency = GetStr(d, "deepseekCurrency");
                if (string.IsNullOrEmpty(c.DeepSeekCurrency)) c.DeepSeekCurrency = "CNY";
                c.DeepSeekBalanceKnown = GetBool(d, "deepseekBalanceKnown", false);
                c.X = GetInt(d, "x", -999999);
                c.Y = GetInt(d, "y", -999999);
                c.TopMost = GetBool(d, "topMost", true);
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
                d["themeMode"] = ThemeMode;
                d["equationEnabled"] = EquationEnabled;
                d["showDeepSeek"] = ShowDeepSeek;
                d["showOpenCode"] = ShowOpenCode;
                d["deepseekTotal"] = DeepSeekTotal;
                d["deepseekHasTotal"] = DeepSeekHasTotal;
                d["deepseekBalance"] = DeepSeekBalance;
                d["deepseekSpent"] = DeepSeekSpent;
                d["deepseekCurrency"] = DeepSeekCurrency;
                d["deepseekBalanceKnown"] = DeepSeekBalanceKnown;
                d["x"] = X;
                d["y"] = Y;
                d["topMost"] = TopMost;
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                File.WriteAllText(ConfigPath,
                    new JavaScriptSerializer().Serialize(d), new UTF8Encoding(false));
            }
            catch { }
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

        private static bool GetBool(Dictionary<string, object> d, string k, bool def)
        {
            object v;
            if (!d.TryGetValue(k, out v) || v == null) return def;
            try { return Convert.ToBoolean(v); }
            catch { return def; }
        }
    }

    internal sealed class DeepSeekState
    {
        public bool Configured;
        public bool Ok;
        public string Error = "";
        public string Currency = "CNY";
        public decimal Total;
    }

    internal sealed class GoWindow
    {
        public bool HasData;
        public int Used;
        public bool RateLimited;
        public DateTimeOffset? ResetsAt;
    }

    internal sealed class GoState
    {
        public bool Configured;
        public bool Ok;
        public string Error = "";
        public GoWindow Rolling = new GoWindow();
        public GoWindow Weekly = new GoWindow();
        public GoWindow Monthly = new GoWindow();
    }

    internal sealed class FetchResult
    {
        public DeepSeekState DeepSeek;
        public GoState Go;
        public FetchResult(DeepSeekState ds, GoState go) { DeepSeek = ds; Go = go; }
    }

    internal sealed class Card : Panel
    {
        public Card()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Palette.Card;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width <= 0 || Height <= 0) return;
            using (GraphicsPath p = Ui.Rounded(new Rectangle(0, 0, Width, Height), 10))
            {
                Region old = Region;
                Region = new Region(p);
                if (old != null) old.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush b = new SolidBrush(Palette.Card))
                e.Graphics.FillRectangle(b, ClientRectangle);
            using (GraphicsPath p = Ui.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 10))
            using (Pen pen = new Pen(Palette.CardBorder))
                e.Graphics.DrawPath(pen, p);
        }
    }

    internal sealed class MiniBar : Control
    {
        public float Percent;
        public Color FillColor = Palette.Blue;

        public MiniBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 4;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int w = ClientSize.Width;
            int h = ClientSize.Height;
            if (w <= 0 || h <= 0) return;
            using (GraphicsPath bgp = Ui.Rounded(new Rectangle(0, 0, w, h), 2))
            using (SolidBrush bg = new SolidBrush(Palette.BarBg))
                e.Graphics.FillPath(bg, bgp);
            float pct = Math.Max(0f, Math.Min(100f, Percent));
            if (pct <= 0f) return;
            int fw = (int)Math.Round(w * pct / 100f);
            if (fw < 1) fw = 1;
            if (fw > w) fw = w;
            using (GraphicsPath fp = Ui.Rounded(new Rectangle(0, 0, fw, h), 2))
            using (SolidBrush fb = new SolidBrush(FillColor))
                e.Graphics.FillPath(fb, fp);
        }
    }

    internal sealed class UsageRow : Control
    {
        public string Caption = "";
        public int Remaining = -1;
        public bool Configured;
        public bool RateLimited;
        public string Error = "";
        public DateTimeOffset? ResetsAt;

        private readonly Font captionFont = new Font("Segoe UI", 8.25F);

        public UsageRow()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Palette.Card;
            Height = 30;
        }

        private string Countdown()
        {
            if (!ResetsAt.HasValue) return "--";
            TimeSpan span = ResetsAt.Value - DateTimeOffset.Now;
            if (span <= TimeSpan.Zero) return "0d 00h 00m 00s";
            int days = (int)span.TotalDays;
            return days.ToString(CultureInfo.InvariantCulture) + "d " +
                   span.Hours.ToString("00", CultureInfo.InvariantCulture) + "h " +
                   span.Minutes.ToString("00", CultureInfo.InvariantCulture) + "m " +
                   span.Seconds.ToString("00", CultureInfo.InvariantCulture) + "s";
        }

        private string SuffixText()
        {
            if (!Configured) return "未配置 Key";
            if (Error.Length > 0) return Error;
            if (RateLimited) return "耗尽 · " + Countdown();
            if (Remaining < 0) return "等待数据";
            return Countdown();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = ClientSize.Width;
            Color level = Ui.LevelColor(Remaining);
            string pct = Remaining >= 0 ? "剩余 " + Remaining.ToString(CultureInfo.InvariantCulture) + "%" : "—";
            Color pctColor = Remaining >= 0 ? level : Palette.Sub;
            if (Error.Length > 0 || RateLimited) { pct = "—"; pctColor = Palette.Red; }

            TextRenderer.DrawText(g, Caption, captionFont, new Rectangle(0, 0, 56, 16),
                Palette.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            string suffix = SuffixText();
            Color suffixColor = (Error.Length > 0 || RateLimited) ? Palette.Red : Palette.Sub;
            const int suffixLeft = 62;
            int suffixW = w - 84 - suffixLeft;
            if (suffixW > 10)
            {
                TextRenderer.DrawText(g, suffix, captionFont, new Rectangle(suffixLeft, 0, suffixW, 16),
                    suffixColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            }

            TextRenderer.DrawText(g, pct, captionFont, new Rectangle(w - 84, 0, 84, 16),
                pctColor, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            int barY = 20;
            int barH = 4;
            using (GraphicsPath bgp = Ui.Rounded(new Rectangle(0, barY, w, barH), 2))
            using (SolidBrush bgb = new SolidBrush(Palette.BarBg))
                g.FillPath(bgb, bgp);

            if (Remaining > 0 && Error.Length == 0)
            {
                int fw = Math.Max(3, w * Remaining / 100);
                if (fw > w) fw = w;
                using (GraphicsPath fp = Ui.Rounded(new Rectangle(0, barY, fw, barH), 2))
                using (SolidBrush fb = new SolidBrush(pctColor))
                    g.FillPath(fb, fp);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                captionFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal sealed class MainForm : Form
    {
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int CS_DROPSHADOW = 0x20000;
        private const int WM_EXITSIZEMOVE = 0x0232;
        private const int RefreshSeconds = 60;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private readonly AppConfig cfg;
        private readonly bool demoMode;
        private readonly Timer uiTimer;
        private readonly Timer barTimer;
        private readonly Label deepSeekBalance;
        private readonly Label deepSeekPeriod;
        private readonly Label deepSeekStatus;
        private readonly Label deepSeekBalanceOnly;
        private readonly Panel equationPanel;
        private readonly Label spentLabel;
        private readonly Label totalLabel;
        private readonly TextBox inlineEditor;
        private readonly MiniBar dsBar;
        private readonly MiniBar refreshBar;
        private readonly Card dsCard;
        private readonly Card goCard;
        private readonly UsageRow row5h;
        private readonly UsageRow rowWeek;
        private readonly UsageRow rowMonth;
        private readonly ContextMenuStrip menu;

        private bool fetching;
        private DateTime lastFetch = DateTime.MinValue;
        private bool placed;
        private bool? peakState;
        private bool refreshingPaused;
        private bool lightTheme;
        private bool resizing;
        private int resizeEdge;
        private int resizeStartX;
        private Rectangle resizeStartBounds;
        private int editingField;
        private decimal lastDeepSeekBalance;
        private bool lastDeepSeekBalanceKnown;
        private string lastDeepSeekCurrency = "CNY";

        private DateTime lastHeaderClick = DateTime.MinValue;
        private int headerClicks;
        private DateTime lastValueClick = DateTime.MinValue;
        private int lastValueField;

        // ---------- 生命周期 ----------
        public MainForm()
        {
            demoMode = HasArg("--demo");
            int demoWidth = HasArg("--wide") ? 700 : 380;
            cfg = demoMode ? new AppConfig { WindowWidth = demoWidth, NoSave = true } : AppConfig.Load();
            if (demoMode)
            {
                if (HasArg("--only-ds")) cfg.ShowOpenCode = false;
                if (HasArg("--only-go")) cfg.ShowDeepSeek = false;
                if (HasArg("--no-cards")) { cfg.ShowDeepSeek = false; cfg.ShowOpenCode = false; }
                if (HasArg("--no-equation")) cfg.EquationEnabled = false;
            }
            lightTheme = HasArg("--light") ? true : (HasArg("--dark") ? false : ResolveLightTheme());
            Palette.Apply(lightTheme);

            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            int startWidth = Math.Max(360, Math.Min(720, cfg.WindowWidth));
            ClientSize = new Size(startWidth, 236);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            BackColor = Palette.Bg;
            TopMost = cfg.TopMost;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            Text = demoMode ? "AI 额度 (demo)" : "AI 额度";
            Font = new Font("Segoe UI", 9F);
            try { Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }

            dsCard = new Card();
            dsCard.Bounds = new Rectangle(12, 12, ClientSize.Width - 24, 72);
            Controls.Add(dsCard);

            Label dsTitle = NewLabel("DeepSeek", 14, 8, 120, 16, 8.25F, FontStyle.Regular, Palette.Sub);
            dsCard.Controls.Add(dsTitle);

            deepSeekPeriod = NewLabel("", dsCard.Width - 100, 8, 86, 16, 8F, FontStyle.Regular, Palette.Sub);
            deepSeekPeriod.TextAlign = ContentAlignment.MiddleRight;
            dsCard.Controls.Add(deepSeekPeriod);

            deepSeekStatus = NewLabel("正在读取…", 14, 30, dsCard.Width - 28, 24, 9F, FontStyle.Regular, Palette.Sub);
            deepSeekStatus.TextAlign = ContentAlignment.MiddleLeft;
            dsCard.Controls.Add(deepSeekStatus);

            deepSeekBalanceOnly = NewLabel("—", 14, 26, dsCard.Width - 28, 40, 14F, FontStyle.Bold, Palette.Text);
            deepSeekBalanceOnly.BackColor = Palette.Card;
            deepSeekBalanceOnly.TextAlign = ContentAlignment.MiddleLeft;
            deepSeekBalanceOnly.Visible = false;
            dsCard.Controls.Add(deepSeekBalanceOnly);

            equationPanel = new Panel();
            equationPanel.Bounds = new Rectangle(0, 26, dsCard.Width, 44);
            equationPanel.BackColor = Palette.Card;
            dsCard.Controls.Add(equationPanel);

            deepSeekBalance = NewLabel("—", 14, 0, 100, 24, 12F, FontStyle.Bold, Palette.Text);
            deepSeekBalance.BackColor = Palette.Card;
            deepSeekBalance.TextAlign = ContentAlignment.MiddleCenter;
            equationPanel.Controls.Add(deepSeekBalance);

            Label plus = NewLabel("+", 116, 0, 14, 24, 12F, FontStyle.Bold, Palette.Sub);
            plus.TextAlign = ContentAlignment.MiddleCenter;
            equationPanel.Controls.Add(plus);

            spentLabel = NewValueLabel(132, 0, 90);
            equationPanel.Controls.Add(spentLabel);

            Label equals = NewLabel("=", 226, 0, 14, 24, 12F, FontStyle.Bold, Palette.Sub);
            equals.TextAlign = ContentAlignment.MiddleCenter;
            equationPanel.Controls.Add(equals);

            totalLabel = NewValueLabel(244, 0, 84);
            equationPanel.Controls.Add(totalLabel);

            dsBar = new MiniBar();
            dsBar.Bounds = new Rectangle(14, 30, equationPanel.Width - 28, 4);
            dsBar.FillColor = Palette.Blue;
            dsBar.BackColor = Palette.Card;
            equationPanel.Controls.Add(dsBar);

            inlineEditor = new TextBox();
            inlineEditor.BorderStyle = BorderStyle.None;
            inlineEditor.BackColor = Palette.Card;
            inlineEditor.ForeColor = Palette.Text;
            inlineEditor.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            inlineEditor.TextAlign = HorizontalAlignment.Center;
            inlineEditor.Visible = false;
            equationPanel.Controls.Add(inlineEditor);

            equationPanel.Visible = false;
            deepSeekStatus.Text = cfg.DeepSeekKey.Length == 0 ? "未配置 · 三击设置" : "正在读取…";

            spentLabel.MouseDown += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) ValueMouseDown(1); };
            totalLabel.MouseDown += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) ValueMouseDown(2); };
            inlineEditor.Leave += delegate { EndEdit(true); };
            inlineEditor.KeyDown += EditorKeyDown;

            refreshBar = new MiniBar();
            refreshBar.Bounds = new Rectangle(14, 89, ClientSize.Width - 28, 4);
            refreshBar.FillColor = Palette.Red;
            refreshBar.BackColor = Palette.Bg;
            Controls.Add(refreshBar);

            goCard = new Card();
            goCard.Bounds = new Rectangle(12, 98, ClientSize.Width - 24, 126);
            Controls.Add(goCard);

            Label goTitle = NewLabel("OpenCode", 14, 8, 160, 16, 8.25F, FontStyle.Regular, Palette.Sub);
            goCard.Controls.Add(goTitle);

            row5h = NewRow("5 小时", 14, 28, goCard.Width - 28);
            rowWeek = NewRow("本周", 14, 58, goCard.Width - 28);
            rowMonth = NewRow("本月", 14, 88, goCard.Width - 28);
            goCard.Controls.Add(row5h);
            goCard.Controls.Add(rowWeek);
            goCard.Controls.Add(rowMonth);

            menu = new ContextMenuStrip();
            menu.Items.Add("立即刷新", null, delegate { StartFetch(); });
            ToolStripMenuItem eqItem = new ToolStripMenuItem("显示余额等式");
            eqItem.Checked = cfg.EquationEnabled;
            eqItem.Click += delegate
            {
                cfg.EquationEnabled = !cfg.EquationEnabled;
                eqItem.Checked = cfg.EquationEnabled;
                cfg.Save();
                UpdateDisplay();
            };
            menu.Items.Add(eqItem);
            ToolStripMenuItem showDsItem = new ToolStripMenuItem("显示 DeepSeek");
            showDsItem.Checked = cfg.ShowDeepSeek;
            showDsItem.Click += delegate
            {
                cfg.ShowDeepSeek = !cfg.ShowDeepSeek;
                showDsItem.Checked = cfg.ShowDeepSeek;
                UpdateProviderLayout();
                cfg.Save();
            };
            menu.Items.Add(showDsItem);
            ToolStripMenuItem showGoItem = new ToolStripMenuItem("显示 OpenCode");
            showGoItem.Checked = cfg.ShowOpenCode;
            showGoItem.Click += delegate
            {
                cfg.ShowOpenCode = !cfg.ShowOpenCode;
                showGoItem.Checked = cfg.ShowOpenCode;
                UpdateProviderLayout();
                cfg.Save();
            };
            menu.Items.Add(showGoItem);
            ToolStripMenuItem top = new ToolStripMenuItem("窗口置顶");
            top.Checked = TopMost;
            top.Click += delegate
            {
                TopMost = !TopMost;
                top.Checked = TopMost;
                cfg.TopMost = TopMost;
                cfg.Save();
            };
            menu.Items.Add(top);
            ToolStripMenuItem themeMenu = new ToolStripMenuItem("主题");
            ToolStripMenuItem autoItem = new ToolStripMenuItem("跟随系统");
            ToolStripMenuItem lightItem = new ToolStripMenuItem("浅色");
            ToolStripMenuItem darkItem = new ToolStripMenuItem("深色");
            autoItem.Checked = cfg.ThemeMode == 0;
            lightItem.Checked = cfg.ThemeMode == 1;
            darkItem.Checked = cfg.ThemeMode == 2;
            autoItem.Click += delegate
            {
                SetThemeMode(0);
                autoItem.Checked = true;
                lightItem.Checked = false;
                darkItem.Checked = false;
            };
            lightItem.Click += delegate
            {
                SetThemeMode(1);
                autoItem.Checked = false;
                lightItem.Checked = true;
                darkItem.Checked = false;
            };
            darkItem.Click += delegate
            {
                SetThemeMode(2);
                autoItem.Checked = false;
                lightItem.Checked = false;
                darkItem.Checked = true;
            };
            themeMenu.DropDownItems.Add(autoItem);
            themeMenu.DropDownItems.Add(lightItem);
            themeMenu.DropDownItems.Add(darkItem);
            menu.Items.Add(themeMenu);
            ToolStripMenuItem lockItem = new ToolStripMenuItem("锁定宽度");
            lockItem.Checked = cfg.WidthLocked;
            lockItem.Click += delegate
            {
                cfg.WidthLocked = !cfg.WidthLocked;
                lockItem.Checked = cfg.WidthLocked;
                cfg.Save();
            };
            menu.Items.Add(lockItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("设置…", null, delegate { OpenSettings(); });
            menu.Items.Add("退出", null, delegate { Close(); });
            ContextMenuStrip = menu;
            AttachMenu(this);

            MouseDown += DragWindow;
            MouseMove += ResizeMouseMove;
            MouseUp += ResizeMouseUp;
            MouseLeave += delegate { if (!resizing) Cursor = Cursors.Default; };
            AttachDrag(dsCard);
            AttachDrag(refreshBar);
            AttachDrag(goCard);

            LayoutContent();
            UpdateProviderLayout();
            UpdatePeakBadge();

            if (cfg.DeepSeekKey.Length > 0 && cfg.DeepSeekBalanceKnown)
            {
                lastDeepSeekBalance = cfg.DeepSeekBalance;
                lastDeepSeekCurrency = string.IsNullOrEmpty(cfg.DeepSeekCurrency) ? "CNY" : cfg.DeepSeekCurrency;
                lastDeepSeekBalanceKnown = true;
            }
            UpdateDisplay();

            uiTimer = new Timer();
            uiTimer.Interval = 1000;
            uiTimer.Tick += delegate
            {
                UpdatePeakBadge();
                row5h.Invalidate();
                rowWeek.Invalidate();
                rowMonth.Invalidate();
                if (!demoMode && !refreshingPaused && !fetching && (DateTime.Now - lastFetch).TotalSeconds >= RefreshSeconds)
                    StartFetch();
            };

            barTimer = new Timer();
            barTimer.Interval = 33;
            barTimer.Tick += delegate
            {
                float remain = 1f;
                if (lastFetch != DateTime.MinValue)
                    remain = 1f - (float)(DateTime.Now - lastFetch).TotalSeconds / RefreshSeconds;
                if (remain < 0f) remain = 0f;
                if (remain > 1f) remain = 1f;
                refreshBar.Percent = remain * 100f;
                refreshBar.Invalidate();
            };
        }

        private Label NewLabel(string text, int x, int y, int w, int h, float size, FontStyle style, Color color)
        {
            Label l = new Label();
            l.Text = text;
            l.Bounds = new Rectangle(x, y, w, h);
            l.Font = new Font("Segoe UI", size, style);
            l.ForeColor = color;
            l.BackColor = Color.Transparent;
            return l;
        }

        private UsageRow NewRow(string caption, int x, int y, int w)
        {
            UsageRow r = new UsageRow();
            r.Caption = caption;
            r.Bounds = new Rectangle(x, y, w, 30);
            return r;
        }

        private Label NewValueLabel(int x, int y, int w)
        {
            Label l = NewLabel("—", x, y, w, 24, 12F, FontStyle.Bold, Palette.Text);
            l.BackColor = Palette.Card;
            l.TextAlign = ContentAlignment.MiddleCenter;
            l.Cursor = Cursors.IBeam;
            return l;
        }

        // ---------- 布局 / 显示 ----------
        private void LayoutContent()
        {
            if (dsCard == null || goCard == null || refreshBar == null) return;
            int w = ClientSize.Width;
            dsCard.Width = w - 24;
            goCard.Width = w - 24;
            refreshBar.Width = w - 28;
            if (equationPanel != null) equationPanel.Width = dsCard.Width;
            if (dsBar != null) dsBar.Width = dsCard.Width - 28;
            if (deepSeekStatus != null) deepSeekStatus.Width = dsCard.Width - 28;
            if (deepSeekBalanceOnly != null) deepSeekBalanceOnly.Width = dsCard.Width - 28;
            if (deepSeekPeriod != null) deepSeekPeriod.Left = dsCard.Width - 100;
            if (row5h != null && rowWeek != null && rowMonth != null)
            {
                int rowWidth = goCard.Width - 28;
                row5h.Width = rowWidth;
                rowWeek.Width = rowWidth;
                rowMonth.Width = rowWidth;
            }
        }

        private Rectangle ValueRect(Label l)
        {
            return new Rectangle(
                dsCard.Left + equationPanel.Left + l.Left,
                dsCard.Top + equationPanel.Top + l.Top,
                l.Width,
                l.Height);
        }

        private void UpdateProviderLayout()
        {
            if (dsCard == null || goCard == null || refreshBar == null) return;
            bool ds = cfg.ShowDeepSeek;
            bool go = cfg.ShowOpenCode;
            dsCard.Visible = ds;
            goCard.Visible = go;

            const int pad = 12;
            const int gap = 5;
            int w = ClientSize.Width;
            int top = pad;

            if (ds)
            {
                dsCard.Bounds = new Rectangle(12, top, w - 24, 72);
                top += 72;
            }

            if (ds && go)
            {
                refreshBar.Bounds = new Rectangle(14, top + gap, w - 28, 4);
                top = refreshBar.Bottom + gap;
            }

            if (go)
            {
                goCard.Bounds = new Rectangle(12, top, w - 24, 126);
                top += 126;
            }

            if (!ds || !go)
            {
                refreshBar.Bounds = new Rectangle(14, (ds || go) ? top + gap : pad, w - 28, 4);
                top = refreshBar.Bottom;
            }

            refreshBar.Visible = true;
            int height = top + pad;
            if (ClientSize.Height != height)
            {
                ClientSize = new Size(ClientSize.Width, height);
                if (IsHandleCreated)
                {
                    Rectangle wa = Screen.FromControl(this).WorkingArea;
                    if (Bottom > wa.Bottom) Top = Math.Max(wa.Top, wa.Bottom - Height);
                }
            }

            LayoutContent();
            Invalidate(true);
        }

        // ---------- 等式内联编辑 ----------
        private void ValueMouseDown(int field)
        {
            if (!cfg.EquationEnabled || !equationPanel.Visible) return;
            if (editingField != 0)
            {
                EndEdit(true);
                return;
            }
            DateTime now = DateTime.UtcNow;
            if (lastValueField == field && (now - lastValueClick).TotalMilliseconds <= 700)
            {
                lastValueField = 0;
                BeginEdit(field);
            }
            else
            {
                lastValueField = field;
                lastValueClick = now;
            }
        }

        private void BeginEdit(int field)
        {
            if (!cfg.EquationEnabled || !equationPanel.Visible) return;
            if (!lastDeepSeekBalanceKnown || editingField != 0) return;
            Label target = field == 1 ? spentLabel : totalLabel;
            editingField = field;
            inlineEditor.Bounds = target.Bounds;
            inlineEditor.Text = target.Text == "—" ? "" : target.Text;
            target.Visible = false;
            inlineEditor.Visible = true;
            refreshingPaused = true;
            inlineEditor.Focus();
            inlineEditor.SelectionStart = 0;
            inlineEditor.SelectionLength = inlineEditor.Text.Length;
        }

        private void EditorKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                EndEdit(true);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                EndEdit(false);
                e.SuppressKeyPress = true;
            }
        }

        private void EndEdit(bool commit)
        {
            if (editingField == 0) return;
            int field = editingField;
            editingField = 0;

            if (commit && lastDeepSeekBalanceKnown)
            {
                string text = inlineEditor.Text.Trim();
                if (field == 2)
                {
                    if (text.Length == 0)
                    {
                        cfg.DeepSeekHasTotal = false;
                        cfg.DeepSeekTotal = 0m;
                    }
                    else
                    {
                        decimal t;
                        if (TryParseDecimal(text, out t) && t >= 0m)
                        {
                            cfg.DeepSeekTotal = t;
                            cfg.DeepSeekHasTotal = t > 0m;
                        }
                    }
                }
                else
                {
                    decimal s;
                    if (text.Length > 0 && TryParseDecimal(text, out s) && s >= 0m)
                    {
                        decimal t = lastDeepSeekBalance + s;
                        cfg.DeepSeekTotal = t;
                        cfg.DeepSeekHasTotal = t > 0m;
                    }
                }
                cfg.DeepSeekBalance = lastDeepSeekBalance;
                cfg.DeepSeekCurrency = lastDeepSeekCurrency;
                cfg.DeepSeekBalanceKnown = true;
                cfg.DeepSeekSpent = cfg.DeepSeekHasTotal
                    ? Math.Max(0m, cfg.DeepSeekTotal - lastDeepSeekBalance)
                    : 0m;
                cfg.Save();
            }

            inlineEditor.Visible = false;
            spentLabel.Visible = true;
            totalLabel.Visible = true;
            refreshingPaused = false;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (!lastDeepSeekBalanceKnown)
            {
                equationPanel.Visible = false;
                deepSeekBalanceOnly.Visible = false;
                deepSeekStatus.Visible = true;
                return;
            }
            if (cfg.EquationEnabled)
            {
                equationPanel.Visible = true;
                deepSeekBalanceOnly.Visible = false;
                deepSeekStatus.Visible = false;
                UpdateEquationFields();
            }
            else
            {
                equationPanel.Visible = false;
                deepSeekBalanceOnly.Visible = true;
                deepSeekStatus.Visible = false;
                string sym = CurrencySymbol(lastDeepSeekCurrency);
                deepSeekBalanceOnly.Text = sym + " " + lastDeepSeekBalance.ToString("0.00", CultureInfo.InvariantCulture);
            }
        }

        private void UpdateEquationFields()
        {
            if (!lastDeepSeekBalanceKnown) return;
            string sym = CurrencySymbol(lastDeepSeekCurrency);
            deepSeekBalance.Text = sym + " " + lastDeepSeekBalance.ToString("0.00", CultureInfo.InvariantCulture);
            if (cfg.DeepSeekHasTotal)
            {
                decimal s = cfg.DeepSeekTotal - lastDeepSeekBalance;
                if (s < 0m) s = 0m;
                spentLabel.Text = s.ToString("0.00", CultureInfo.InvariantCulture);
                totalLabel.Text = cfg.DeepSeekTotal.ToString("0.00", CultureInfo.InvariantCulture);
                dsBar.Percent = cfg.DeepSeekTotal > 0m ? (float)(lastDeepSeekBalance * 100m / cfg.DeepSeekTotal) : 0f;
            }
            else
            {
                spentLabel.Text = "0";
                totalLabel.Text = lastDeepSeekBalance.ToString("0.00", CultureInfo.InvariantCulture);
                dsBar.Percent = lastDeepSeekBalance > 0m ? 100f : 0f;
            }
            dsBar.Invalidate();
        }

        private static bool TryParseDecimal(string s, out decimal v)
        {
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out v)) return true;
            return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out v);
        }

        // ---------- 主题 ----------
        private bool ResolveLightTheme()
        {
            if (cfg.ThemeMode == 1) return true;
            if (cfg.ThemeMode == 2) return false;
            return IsSystemLightTheme();
        }

        private static bool IsSystemLightTheme()
        {
            try
            {
                object value = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "AppsUseLightTheme", 0);
                return value != null && Convert.ToInt32(value) != 0;
            }
            catch { return false; }
        }

        private void SetThemeMode(int mode)
        {
            cfg.ThemeMode = mode;
            cfg.Save();
            ApplyTheme(mode == 0 ? IsSystemLightTheme() : mode == 1);
        }

        private void ApplyTheme(bool light)
        {
            if (lightTheme == light) return;
            Color oldSub = Palette.Sub;
            Color oldText = Palette.Text;
            Color oldRed = Palette.Red;
            Color oldGreen = Palette.Green;
            Color oldYellow = Palette.Yellow;
            Color oldCard = Palette.Card;

            Palette.Apply(light);
            lightTheme = light;
            BackColor = Palette.Bg;

            dsBar.FillColor = Palette.Blue;
            refreshBar.FillColor = Palette.Red;
            RecolorControls(this, oldSub, oldText, oldRed, oldGreen, oldYellow, oldCard);
            Invalidate(true);
        }

        private void RecolorControls(Control parent, Color oldSub, Color oldText,
            Color oldRed, Color oldGreen, Color oldYellow, Color oldCard)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Label)
                {
                    Color f = c.ForeColor;
                    if (f == oldSub) c.ForeColor = Palette.Sub;
                    else if (f == oldText) c.ForeColor = Palette.Text;
                    else if (f == oldRed) c.ForeColor = Palette.Red;
                    else if (f == oldGreen) c.ForeColor = Palette.Green;
                    else if (f == oldYellow) c.ForeColor = Palette.Yellow;
                    if (c.BackColor == oldCard) c.BackColor = Palette.Card;
                }
                else if (c is TextBox)
                {
                    if (c.BackColor == oldCard) c.BackColor = Palette.Card;
                    if (c.ForeColor == oldText) c.ForeColor = Palette.Text;
                }
                else if (c is Card || c is UsageRow || c is MiniBar || c is Panel)
                {
                    if (c == refreshBar) c.BackColor = Palette.Bg;
                    else if (c.BackColor == oldCard) c.BackColor = Palette.Card;
                    c.Invalidate();
                }
                RecolorControls(c, oldSub, oldText, oldRed, oldGreen, oldYellow, oldCard);
            }
        }

        private static bool HasArg(string name)
        {
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        // ---------- 调试 ----------
        private void ApplyDemo()
        {
            lastFetch = DateTime.Now;
            lastDeepSeekBalance = 123.45m;
            lastDeepSeekCurrency = "CNY";
            lastDeepSeekBalanceKnown = true;
            cfg.DeepSeekTotal = 200m;
            cfg.DeepSeekHasTotal = true;
            UpdateDisplay();

            row5h.Configured = true;
            row5h.Error = "";
            row5h.RateLimited = false;
            row5h.Remaining = 76;
            row5h.ResetsAt = DateTimeOffset.Now.AddHours(3).AddMinutes(12).AddSeconds(45);
            row5h.Invalidate();

            rowWeek.Configured = true;
            rowWeek.Error = "";
            rowWeek.RateLimited = false;
            rowWeek.Remaining = 59;
            rowWeek.ResetsAt = DateTimeOffset.Now.AddDays(4).AddHours(8).AddMinutes(30);
            rowWeek.Invalidate();

            rowMonth.Configured = true;
            rowMonth.Error = "";
            rowMonth.RateLimited = false;
            rowMonth.Remaining = 37;
            rowMonth.ResetsAt = DateTimeOffset.Now.AddDays(22).AddHours(6);
            rowMonth.Invalidate();

            UpdatePeakBadge();
        }

        private void UpdatePeakBadge()
        {
            bool peak = IsPeakNow();
            if (peakState.HasValue && peakState.Value == peak) return;
            peakState = peak;
            deepSeekPeriod.Text = peak ? "峰时" : "谷时";
            deepSeekPeriod.ForeColor = peak ? Palette.Red : Palette.Green;
        }

        private static bool IsPeakNow()
        {
            DateTimeOffset beijing = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8));
            DayOfWeek day = beijing.DayOfWeek;
            if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday) return false;
            TimeSpan time = beijing.TimeOfDay;
            return (time >= TimeSpan.FromHours(9) && time < TimeSpan.FromHours(12))
                || (time >= TimeSpan.FromHours(14) && time < TimeSpan.FromHours(18));
        }

        private void AttachDrag(Control c)
        {
            if (c == equationPanel) return;
            if (!(c is TextBox)) c.MouseDown += DragWindow;
            foreach (Control child in c.Controls)
            {
                if (!(child is TextBox)) AttachDrag(child);
            }
        }

        private void AttachMenu(Control c)
        {
            if (!(c is TextBox)) c.ContextMenuStrip = menu;
            foreach (Control child in c.Controls)
            {
                if (!(child is TextBox)) AttachMenu(child);
            }
        }

        // ---------- 窗口拖动 / 横向拉伸 ----------
        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (editingField != 0)
            {
                EndEdit(true);
                return;
            }
            Point p = PointToClient(Cursor.Position);

            if (!cfg.WidthLocked && (p.X <= 6 || p.X >= ClientSize.Width - 7))
            {
                resizeEdge = p.X <= 6 ? -1 : 1;
                resizeStartX = Cursor.Position.X;
                resizeStartBounds = Bounds;
                resizing = true;
                Capture = true;
                Cursor = Cursors.SizeWE;
                return;
            }

            if (cfg.EquationEnabled && equationPanel.Visible && lastDeepSeekBalanceKnown && editingField == 0)
            {
                if (ValueRect(spentLabel).Contains(p))
                {
                    ValueMouseDown(1);
                    return;
                }
                if (ValueRect(totalLabel).Contains(p))
                {
                    ValueMouseDown(2);
                    return;
                }
            }

            if (p.Y <= 38)
            {
                DateTime now = DateTime.UtcNow;
                if ((now - lastHeaderClick).TotalMilliseconds > 800) headerClicks = 0;
                lastHeaderClick = now;
                headerClicks++;
                if (headerClicks >= 3)
                {
                    headerClicks = 0;
                    OpenSettings();
                    return;
                }
            }

            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
        }

        private void ResizeMouseMove(object sender, MouseEventArgs e)
        {
            if (resizing)
            {
                int dx = Cursor.Position.X - resizeStartX;
                Rectangle b = resizeStartBounds;
                int frame = Width - ClientSize.Width;
                int minOuter = 360 + frame;
                int maxOuter = 720 + frame;
                if (resizeEdge < 0)
                {
                    int newLeft = b.Left + dx;
                    int newWidth = b.Width - dx;
                    if (newWidth < minOuter) { newLeft = b.Right - minOuter; newWidth = minOuter; }
                    if (newWidth > maxOuter) { newLeft = b.Right - maxOuter; newWidth = maxOuter; }
                    Bounds = new Rectangle(newLeft, b.Top, newWidth, b.Height);
                }
                else
                {
                    int newWidth = b.Width + dx;
                    if (newWidth < minOuter) newWidth = minOuter;
                    if (newWidth > maxOuter) newWidth = maxOuter;
                    Bounds = new Rectangle(b.Left, b.Top, newWidth, b.Height);
                }
                return;
            }

            Point p = PointToClient(Cursor.Position);
            if (!cfg.WidthLocked && (p.X <= 6 || p.X >= ClientSize.Width - 7)) Cursor = Cursors.SizeWE;
            else if (Cursor == Cursors.SizeWE) Cursor = Cursors.Default;
        }

        private void ResizeMouseUp(object sender, MouseEventArgs e)
        {
            if (!resizing) return;
            resizing = false;
            Capture = false;
            Cursor = Cursors.Default;
            cfg.WindowWidth = Math.Max(360, Math.Min(720, ClientSize.Width));
            cfg.X = Left;
            cfg.Y = Top;
            cfg.Save();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_EXITSIZEMOVE)
            {
                cfg.X = Left;
                cfg.Y = Top;
                cfg.WindowWidth = Math.Max(360, Math.Min(720, ClientSize.Width));
                cfg.Save();
            }
            if (m.Msg == 0x001A || m.Msg == 0x031A)
            {
                if (cfg.ThemeMode == 0)
                {
                    bool light = IsSystemLightTheme();
                    if (light != lightTheme) ApplyTheme(light);
                }
            }
            base.WndProc(ref m);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width <= 0 || Height <= 0) return;
            LayoutContent();
            using (GraphicsPath p = Ui.Rounded(new Rectangle(0, 0, Width, Height), 12))
            {
                Region old = Region;
                Region = new Region(p);
                if (old != null) old.Dispose();
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath p = Ui.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 12))
            using (Pen pen = new Pen(Palette.WindowBorder))
                g.DrawPath(pen, p);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (placed) return;
            placed = true;
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int x = cfg.X;
            int y = cfg.Y;
            bool onScreen = false;
            foreach (Screen s in Screen.AllScreens)
            {
                Rectangle r = new Rectangle(x, y, Width, Height);
                if (s.WorkingArea.IntersectsWith(r)) { onScreen = true; break; }
            }
            if (x == -999999 || y == -999999 || !onScreen)
            {
                x = wa.Right - Width - 24;
                y = wa.Top + 80;
            }
            Location = new Point(x, y);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            uiTimer.Start();
            barTimer.Start();
            if (demoMode) ApplyDemo();
            else StartFetch();
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (string.Equals(arg, "--settings", StringComparison.OrdinalIgnoreCase))
                {
                    BeginInvoke(new Action(OpenSettings));
                    break;
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (editingField != 0) EndEdit(true);
            cfg.X = Left;
            cfg.Y = Top;
            cfg.WindowWidth = Math.Max(360, Math.Min(720, ClientSize.Width));
            cfg.TopMost = TopMost;
            cfg.Save();
            uiTimer.Stop();
            barTimer.Stop();
            base.OnFormClosing(e);
        }

        private void OpenSettings()
        {
            string oldKey = cfg.DeepSeekKey;
            refreshingPaused = true;
            try
            {
                using (SettingsForm f = new SettingsForm(cfg))
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                    {
                        if (cfg.DeepSeekKey != oldKey)
                        {
                            lastDeepSeekBalanceKnown = false;
                            deepSeekStatus.Text = "正在读取…";
                            deepSeekStatus.ForeColor = Palette.Sub;
                            UpdateDisplay();
                        }
                        lastFetch = DateTime.MinValue;
                        StartFetch();
                    }
                }
            }
            finally { refreshingPaused = false; }
        }

        // ---------- 数据获取 ----------
        private void StartFetch()
        {
            if (fetching) return;
            fetching = true;

            string dsKey = cfg.DeepSeekKey;
            string goKey = cfg.OpenCodeKey;

            if (string.IsNullOrEmpty(dsKey) && string.IsNullOrEmpty(goKey))
            {
                ApplyFetch(new FetchResult(new DeepSeekState(), new GoState()));
                return;
            }

            Task<FetchResult>.Factory.StartNew(delegate
            {
                DeepSeekState ds = FetchDeepSeek(dsKey);
                GoState go = FetchGo(goKey);
                return new FetchResult(ds, go);
            }).ContinueWith(delegate(Task<FetchResult> t)
            {
                try
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    BeginInvoke(new Action(delegate
                    {
                        if (t.IsFaulted || t.IsCanceled)
                            ApplyFetch(new FetchResult(
                                new DeepSeekState { Configured = dsKey.Length > 0, Error = "查询失败" },
                                new GoState { Configured = goKey.Length > 0, Error = "查询失败" }));
                        else
                            ApplyFetch(t.Result);
                    }));
                }
                catch { }
            });
        }

        private void ApplyFetch(FetchResult r)
        {
            if (IsDisposed) return;
            fetching = false;
            lastFetch = DateTime.Now;

            DeepSeekState ds = r.DeepSeek;
            if (!ds.Configured)
            {
                if (editingField != 0) EndEdit(false);
                lastDeepSeekBalanceKnown = false;
                deepSeekStatus.Text = "未配置 · 三击设置";
                deepSeekStatus.ForeColor = Palette.Sub;
                UpdateDisplay();
            }
            else if (!ds.Ok)
            {
                if (editingField != 0) EndEdit(false);
                lastDeepSeekBalanceKnown = false;
                deepSeekStatus.Text = ds.Error;
                deepSeekStatus.ForeColor = Palette.Red;
                UpdateDisplay();
            }
            else
            {
                lastDeepSeekBalance = ds.Total;
                lastDeepSeekCurrency = ds.Currency;
                lastDeepSeekBalanceKnown = true;

                cfg.DeepSeekBalance = ds.Total;
                cfg.DeepSeekCurrency = ds.Currency;
                cfg.DeepSeekBalanceKnown = true;
                cfg.DeepSeekSpent = cfg.DeepSeekHasTotal
                    ? Math.Max(0m, cfg.DeepSeekTotal - ds.Total)
                    : 0m;
                cfg.Save();

                if (editingField != 0 && cfg.EquationEnabled)
                {
                    string sym = CurrencySymbol(ds.Currency);
                    deepSeekBalance.Text = sym + " " + ds.Total.ToString("0.00", CultureInfo.InvariantCulture);
                }
                else
                {
                    UpdateDisplay();
                }
            }

            GoState go = r.Go;
            ApplyRow(row5h, go, go.Rolling);
            ApplyRow(rowWeek, go, go.Weekly);
            ApplyRow(rowMonth, go, go.Monthly);
        }

        private static string CurrencySymbol(string currency)
        {
            if (currency == "CNY") return "¥";
            if (currency == "USD") return "$";
            return string.IsNullOrEmpty(currency) ? "" : currency + " ";
        }

        private static void ApplyRow(UsageRow row, GoState go, GoWindow w)
        {
            row.Configured = go.Configured;
            row.Error = go.Ok ? "" : go.Error;
            row.RateLimited = go.Ok && w.RateLimited;
            row.ResetsAt = go.Ok ? w.ResetsAt : null;
            row.Remaining = go.Ok && w.HasData && !w.RateLimited ? Math.Max(0, 100 - w.Used) : -1;
            if (go.Ok && w.RateLimited) row.Remaining = 0;
            row.Invalidate();
        }

        private static DeepSeekState FetchDeepSeek(string key)
        {
            DeepSeekState st = new DeepSeekState();
            if (string.IsNullOrEmpty(key)) return st;
            st.Configured = true;
            int status = 0;
            try
            {
                string json = HttpGet("https://api.deepseek.com/user/balance", key, out status);
                JavaScriptSerializer js = new JavaScriptSerializer();
                Dictionary<string, object> root = js.DeserializeObject(json) as Dictionary<string, object>;
                if (root == null) { st.Error = "响应解析失败"; return st; }
                if (root.ContainsKey("is_available") && !Convert.ToBoolean(root["is_available"]))
                {
                    st.Error = "账户余额不可用";
                    return st;
                }
                object[] infos = root.ContainsKey("balance_infos") ? root["balance_infos"] as object[] : null;
                if (infos == null || infos.Length == 0) { st.Error = "无余额信息"; return st; }
                Dictionary<string, object> pick = null;
                foreach (object it in infos)
                {
                    Dictionary<string, object> d = it as Dictionary<string, object>;
                    if (d == null) continue;
                    if (pick == null) pick = d;
                    if (GetString(d, "currency") == "CNY") { pick = d; break; }
                }
                if (pick == null) { st.Error = "无余额信息"; return st; }
                st.Currency = GetString(pick, "currency");
                st.Total = GetDecimal(pick, "total_balance");
                st.Ok = true;
            }
            catch (Exception ex)
            {
                st.Error = MapError(status, ex);
            }
            return st;
        }

        private static GoState FetchGo(string key)
        {
            GoState st = new GoState();
            if (string.IsNullOrEmpty(key)) return st;
            st.Configured = true;

            string json = null;
            int status = 0;
            Exception last = null;
            string[] urls = new string[]
            {
                "https://opencode.ai/zen/go/v1/usage",
                "https://opencode.ai/inference/go/v1/usage"
            };
            foreach (string url in urls)
            {
                try
                {
                    json = HttpGet(url, key, out status);
                    last = null;
                    break;
                }
                catch (Exception ex)
                {
                    last = ex;
                    if (status != 404) break;
                }
            }
            if (json == null)
            {
                st.Error = MapError(status, last);
                return st;
            }

            try
            {
                JavaScriptSerializer js = new JavaScriptSerializer();
                Dictionary<string, object> root = js.DeserializeObject(json) as Dictionary<string, object>;
                if (root == null || !root.ContainsKey("usage")) { st.Error = "响应缺少 usage"; return st; }
                Dictionary<string, object> usage = root["usage"] as Dictionary<string, object>;
                if (usage == null) { st.Error = "响应缺少 usage"; return st; }
                st.Rolling = ParseWindow(usage, "rolling");
                st.Weekly = ParseWindow(usage, "weekly");
                st.Monthly = ParseWindow(usage, "monthly");
                st.Ok = true;
            }
            catch (Exception ex)
            {
                st.Error = "解析失败：" + ex.Message;
            }
            return st;
        }

        private static GoWindow ParseWindow(Dictionary<string, object> usage, string name)
        {
            GoWindow w = new GoWindow();
            object raw;
            if (!usage.TryGetValue(name, out raw)) return w;
            Dictionary<string, object> d = raw as Dictionary<string, object>;
            if (d == null) return w;
            w.HasData = true;
            if (d.ContainsKey("percent"))
                w.Used = (int)Math.Round(Math.Max(0, Math.Min(100, GetDouble(d, "percent"))));
            else if (d.ContainsKey("used") && d.ContainsKey("limit"))
            {
                double limit = GetDouble(d, "limit");
                if (limit > 0)
                    w.Used = (int)Math.Round(Math.Max(0, Math.Min(100, GetDouble(d, "used") * 100.0 / limit)));
            }
            if (d.ContainsKey("status"))
                w.RateLimited = string.Equals(GetString(d, "status"), "rate-limited", StringComparison.OrdinalIgnoreCase);
            if (d.ContainsKey("resetsAt"))
                w.ResetsAt = ParseTime(d["resetsAt"]);
            return w;
        }

        private static DateTimeOffset? ParseTime(object o)
        {
            string s = o as string;
            if (string.IsNullOrEmpty(s)) return null;
            DateTimeOffset dto;
            if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dto))
                return dto;
            return null;
        }

        private static string HttpGet(string url, string key, out int status)
        {
            status = 0;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.UserAgent = "QuotaFloat/1.0";
            req.Headers["Authorization"] = "Bearer " + key;
            req.Accept = "application/json";
            req.Timeout = 12000;
            req.ReadWriteTimeout = 12000;
            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    status = (int)resp.StatusCode;
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                        return sr.ReadToEnd();
                }
            }
            catch (WebException we)
            {
                HttpWebResponse hr = we.Response as HttpWebResponse;
                if (hr != null) status = (int)hr.StatusCode;
                throw;
            }
        }

        private static string MapError(int status, Exception ex)
        {
            if (status == 401) return "Key 无效 (401)";
            if (status == 402) return "余额不足 (402)";
            if (status == 403) return "无访问权限 (403)";
            if (status == 429) return "请求过多 (429)";
            if (status > 0) return "HTTP " + status.ToString(CultureInfo.InvariantCulture);
            WebException we = ex as WebException;
            if (we != null)
            {
                if (we.Status == WebExceptionStatus.Timeout) return "连接超时";
                if (we.Status == WebExceptionStatus.NameResolutionFailure) return "域名解析失败";
                if (we.Status == WebExceptionStatus.ConnectFailure) return "无法连接";
            }
            return "网络错误";
        }

        private static string GetString(Dictionary<string, object> d, string k)
        {
            object v;
            return d.TryGetValue(k, out v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : "";
        }

        private static decimal GetDecimal(Dictionary<string, object> d, string k)
        {
            object v;
            if (!d.TryGetValue(k, out v) || v == null) return 0m;
            try { return Convert.ToDecimal(v, CultureInfo.InvariantCulture); }
            catch { return 0m; }
        }

        private static double GetDouble(Dictionary<string, object> d, string k)
        {
            object v;
            if (!d.TryGetValue(k, out v) || v == null) return 0;
            try { return Convert.ToDouble(v, CultureInfo.InvariantCulture); }
            catch { return 0; }
        }
    }

    // ---------- 设置窗口 ----------
    internal sealed class SettingsForm : Form
    {
        private readonly AppConfig cfg;
        private readonly TextBox dsBox;
        private readonly TextBox goBox;

        public SettingsForm(AppConfig config)
        {
            cfg = config;
            Text = "设置";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(440, 178);
            BackColor = Palette.Bg;
            ForeColor = Palette.Text;
            Font = new Font("Segoe UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);

            AddLabel("DeepSeek API Key", 16, 14, 210);
            Label dsHint = AddLabel("当前 " + Ui.MaskKey(cfg.DeepSeekKey), 240, 14, 184);
            dsHint.TextAlign = ContentAlignment.MiddleRight;
            dsHint.Font = new Font("Segoe UI", 7.25F);
            dsBox = AddInput(16, 36, 408);

            AddLabel("OpenCode API Key", 16, 72, 210);
            Label goHint = AddLabel("当前 " + Ui.MaskKey(cfg.OpenCodeKey), 240, 72, 184);
            goHint.TextAlign = ContentAlignment.MiddleRight;
            goHint.Font = new Font("Segoe UI", 7.25F);
            goBox = AddInput(16, 94, 408);

            Button save = new Button();
            save.Text = "保存";
            save.Bounds = new Rectangle(232, 132, 90, 30);
            StyleButton(save, Palette.Blue, Color.White);
            save.Click += delegate
            {
                string dsNew = dsBox.Text.Trim();
                string goNew = goBox.Text.Trim();
                bool dsChanged = dsNew.Length > 0 && dsNew != cfg.DeepSeekKey;
                if (dsNew.Length > 0) cfg.DeepSeekKey = dsNew;
                if (goNew.Length > 0) cfg.OpenCodeKey = goNew;
                if (dsChanged)
                {
                    cfg.DeepSeekTotal = 0m;
                    cfg.DeepSeekHasTotal = false;
                    cfg.DeepSeekBalance = 0m;
                    cfg.DeepSeekSpent = 0m;
                    cfg.DeepSeekCurrency = "CNY";
                    cfg.DeepSeekBalanceKnown = false;
                }
                cfg.Save();
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(save);

            Button cancel = new Button();
            cancel.Text = "取消";
            cancel.Bounds = new Rectangle(332, 132, 90, 30);
            StyleButton(cancel, Palette.InputBg, Palette.Text);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            AcceptButton = save;
            CancelButton = cancel;
        }

        private Label AddLabel(string text, int x, int y, int w)
        {
            Label l = new Label();
            l.Text = text;
            l.Bounds = new Rectangle(x, y, w, 18);
            l.ForeColor = Palette.Sub;
            l.BackColor = Color.Transparent;
            Controls.Add(l);
            return l;
        }

        private TextBox AddInput(int x, int y, int w)
        {
            TextBox t = new TextBox();
            t.Bounds = new Rectangle(x, y, w, 24);
            t.BackColor = Palette.InputBg;
            t.ForeColor = Palette.Text;
            t.BorderStyle = BorderStyle.FixedSingle;
            t.UseSystemPasswordChar = true;
            Controls.Add(t);
            return t;
        }

        private static void StyleButton(Button b, Color back, Color fore)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = back;
            b.ForeColor = fore;
            b.Cursor = Cursors.Hand;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80;
                return cp;
            }
        }
    }
}

