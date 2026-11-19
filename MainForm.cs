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
    internal sealed partial class MainForm : Form
    {
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int CS_DROPSHADOW = 0x20000;
        private const int WM_EXITSIZEMOVE = 0x0232;
        private const int RefreshSeconds = 60;
        private const int EdgeStrip = 10;
        private const int SlideDuration = 180;
        private const int HideDelay = 450;
        private const int ShowDelay = 200;
        private const int StartupHideDelay = 1500;
        private const int DockTolerance = 14;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private readonly AppConfig cfg;
        private readonly bool demoMode;
        private readonly Timer uiTimer;
        private readonly Timer barTimer;
        private readonly Timer edgeTimer;
        private readonly Timer slideTimer;
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
        private ToolStripMenuItem edgeHideItem;

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
        private int dockedEdge;
        private bool edgeArmed;
        private bool edgeHidden;
        private bool slideAnimating;
        private bool slideOutTarget;
        private Point slideFrom;
        private Point slideTo;
        private int slideStartTick;
        private float hintAlpha;
        private float hintFrom;
        private float hintTo;
        private DateTime lastInside = DateTime.UtcNow;
        private DateTime hiddenHoverStart = DateTime.MinValue;
        private DateTime hideNotBefore = DateTime.MinValue;
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
                if (HasArg("--no-edge")) cfg.EdgeHide = false;
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
            edgeHideItem = new ToolStripMenuItem("贴边吸附");
            edgeHideItem.ToolTipText = "拖到屏幕边缘时吸附，鼠标移出后自动滑出";
            edgeHideItem.Checked = cfg.EdgeHide;
            edgeHideItem.Click += delegate
            {
                cfg.EdgeHide = !cfg.EdgeHide;
                edgeHideItem.Checked = cfg.EdgeHide;
                cfg.Save();
                ApplyEdgeHideSetting();
            };
            menu.Items.Add(edgeHideItem);
            ToolStripMenuItem autoStartItem = new ToolStripMenuItem("开机自启动");
            autoStartItem.ToolTipText = "当前用户登录时自动启动";
            autoStartItem.Checked = IsAutoStartEnabled();
            autoStartItem.Enabled = !demoMode;
            autoStartItem.Click += delegate
            {
                bool enable = !autoStartItem.Checked;
                if (SetAutoStart(enable)) autoStartItem.Checked = enable;
            };
            menu.Items.Add(autoStartItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("设置…", null, delegate { OpenSettings(); });
            menu.Items.Add("退出", null, delegate { Close(); });
            ContextMenuStrip = menu;
            AttachMenu(this);

            MouseDown += DragWindow;
            MouseMove += ResizeMouseMove;
            MouseUp += ResizeMouseUp;
            MouseEnter += delegate
            {
                lastInside = DateTime.UtcNow;
                if (!cfg.EdgeHide || dockedEdge == 0) return;
                edgeArmed = true;
                edgeTimer.Start();
                hideNotBefore = DateTime.MinValue;
                hiddenHoverStart = edgeHidden ? DateTime.UtcNow : DateTime.MinValue;
            };
            MouseLeave += delegate
            {
                if (!resizing) Cursor = Cursors.Default;
                hiddenHoverStart = DateTime.MinValue;
            };
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
                if (edgeHidden || hintAlpha > 0f) Invalidate();
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

            edgeTimer = new Timer();
            edgeTimer.Interval = 120;
            edgeTimer.Tick += delegate { TickEdge(); };

            slideTimer = new Timer();
            slideTimer.Interval = 15;
            slideTimer.Tick += delegate { TickSlide(); };
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
    }
}
