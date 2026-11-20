using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const int WM_EXITSIZEMOVE = 0x0232;
        private const int WM_DISPLAYCHANGE = 0x007E;
        private const int WM_SETTINGCHANGE = 0x001A;
        private const int WM_THEMECHANGED = 0x031A;
        private const int WS_EX_TOOLWINDOW = 0x80;
        private const uint WDA_NONE = 0x00000000;
        private const uint WDA_MONITOR = 0x00000001;
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
        private const int CaptureExcludeMinBuild = 19041;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
        private readonly AppConfig cfg;
        private readonly bool demoMode;
        private readonly Timer uiTimer;
        private readonly Timer barTimer;
        private readonly Timer edgeTimer;
        private readonly Timer slideTimer;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Label dsTitle;
        private readonly Label goTitle;
        private readonly Label deepSeekBalance;
        private readonly Label deepSeekPeriod;
        private readonly Label deepSeekStatus;
        private readonly Label deepSeekBalanceOnly;
        private readonly Panel equationPanel;
        private readonly Label spentLabel;
        private readonly Label totalLabel;
        private readonly Label plusLabel;
        private readonly Label equalsLabel;
        private readonly TextBox inlineEditor;
        private readonly MiniBar dsBar;
        private readonly MiniBar refreshBar;
        private readonly Card dsCard;
        private readonly Card goCard;
        private readonly UsageRow row5h;
        private readonly UsageRow rowWeek;
        private readonly UsageRow rowMonth;
        private readonly RefreshButton dsRefreshButton;
        private readonly RefreshButton goRefreshButton;
        private readonly ToolTip tips = new ToolTip();
        private ContextMenuStrip menu;
        private ToolStripMenuItem edgeHideItem;

        private bool fetchingDeepSeek;
        private bool fetchingOpenCode;
        private int deepSeekRequestVersion;
        private int openCodeRequestVersion;
        private long lastDeepSeekFetchStamp = Timing.NoStamp;
        private long lastOpenCodeFetchStamp = Timing.NoStamp;
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
        private long slideStartStamp;
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
        private int frameIntervalMilliseconds = Timing.FallbackFrameIntervalMilliseconds;
        private int displayRefreshHz = Timing.DefaultRefreshHz;
        private bool highResolutionTimer;
        private bool captureExcluded;
        private bool captureBlackedOut;

        private bool AnyFetching
        {
            get { return fetchingDeepSeek || fetchingOpenCode; }
        }

        private DateTime lastHeaderClick = DateTime.MinValue;
        private int headerClicks;
        private DateTime lastValueClick = DateTime.MinValue;
        private int lastValueField;

        // ---------- 生命周期 ----------
        public MainForm()
        {
            demoMode = HasArg("--demo");
            int demoWidth = HasArg("--wide") ? UiMetrics.WideDemoWidth :
                (HasArg("--narrow") ? AppConfig.MinWindowWidth : AppConfig.DefaultWindowWidth);
            cfg = demoMode ? new AppConfig { WindowWidth = demoWidth, NoSave = true } : AppConfig.Load();
            if (demoMode)
            {
                if (HasArg("--only-ds")) cfg.ShowOpenCode = false;
                if (HasArg("--only-go")) cfg.ShowDeepSeek = false;
                if (HasArg("--no-cards")) { cfg.ShowDeepSeek = false; cfg.ShowOpenCode = false; }
                if (HasArg("--no-equation")) cfg.EquationEnabled = false;
                if (HasArg("--no-edge")) cfg.EdgeHide = false;
                if (HasArg("--lang=en")) cfg.Language = Strings.En;
                if (HasArg("--lang=zh")) cfg.Language = Strings.ZhCn;
            }
            Strings.Code = string.IsNullOrEmpty(cfg.Language) ? Strings.ZhCn : Strings.Normalize(cfg.Language);
            lightTheme = HasArg("--light") ? true : (HasArg("--dark") ? false : ResolveLightTheme());
            Palette.Apply(lightTheme);

            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(UiMetrics.DefaultDpi, UiMetrics.DefaultDpi);
            int startWidth = Math.Max(AppConfig.MinWindowWidth, Math.Min(AppConfig.MaxWindowWidth, cfg.WindowWidth));
            ClientSize = new Size(startWidth, UiMetrics.InitialWindowHeight);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            BackColor = Palette.Bg;
            TopMost = cfg.TopMost;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            Text = demoMode ? Strings.AppTitleDemo : Strings.AppTitle;
            Font = new Font(UiMetrics.FontFamily, UiMetrics.FontBody);
            try { Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }

            dsCard = new Card();
            dsCard.Bounds = new Rectangle(UiMetrics.OuterMargin, UiMetrics.OuterMargin,
                ClientSize.Width - UiMetrics.CardWidthInset, UiMetrics.DeepSeekCardHeight);
            Controls.Add(dsCard);

            dsTitle = NewLabel(Strings.DeepSeekName, UiMetrics.CardTextInset, UiMetrics.TitleTop,
                UiMetrics.DeepSeekTitleWidth, UiMetrics.TitleHeight, UiMetrics.FontCaption,
                FontStyle.Regular, Palette.Sub);
            dsTitle.TextAlign = ContentAlignment.MiddleLeft;
            dsCard.Controls.Add(dsTitle);

            deepSeekPeriod = NewLabel("", dsCard.Width - UiMetrics.DeepSeekPeriodRightInset,
                UiMetrics.TitleTop, UiMetrics.DeepSeekPeriodWidth, UiMetrics.TitleHeight,
                UiMetrics.FontSmall, FontStyle.Regular, Palette.Sub);
            deepSeekPeriod.TextAlign = ContentAlignment.MiddleRight;
            dsCard.Controls.Add(deepSeekPeriod);

            dsRefreshButton = new RefreshButton();
            dsRefreshButton.Bounds = new Rectangle(
                dsCard.Width - UiMetrics.TitleButtonRightInset - UiMetrics.TitleButtonSize,
                UiMetrics.TitleButtonTop, UiMetrics.TitleButtonSize, UiMetrics.TitleButtonSize);
            dsRefreshButton.AccessibleName = Strings.RefreshDeepSeek;
            dsRefreshButton.Click += delegate { StartDeepSeekFetch(); };
            dsCard.Controls.Add(dsRefreshButton);

            deepSeekStatus = NewLabel(Strings.Loading, UiMetrics.CardTextInset,
                UiMetrics.DeepSeekStatusTop, dsCard.Width - UiMetrics.CardContentInset,
                UiMetrics.DeepSeekStatusHeight, UiMetrics.FontBody, FontStyle.Regular, Palette.Sub);
            deepSeekStatus.TextAlign = ContentAlignment.MiddleLeft;
            dsCard.Controls.Add(deepSeekStatus);

            deepSeekBalanceOnly = NewLabel("—", UiMetrics.CardTextInset, UiMetrics.DeepSeekBalanceTop,
                dsCard.Width - UiMetrics.CardContentInset, UiMetrics.DeepSeekBalanceHeight,
                UiMetrics.FontBalance, FontStyle.Bold, Palette.Text);
            deepSeekBalanceOnly.BackColor = Palette.Card;
            deepSeekBalanceOnly.TextAlign = ContentAlignment.MiddleLeft;
            deepSeekBalanceOnly.AutoEllipsis = true;
            deepSeekBalanceOnly.Visible = false;
            dsCard.Controls.Add(deepSeekBalanceOnly);

            equationPanel = new Panel();
            equationPanel.Bounds = new Rectangle(0, UiMetrics.EquationTop, dsCard.Width,
                UiMetrics.EquationHeight);
            equationPanel.BackColor = Palette.Card;
            dsCard.Controls.Add(equationPanel);

            deepSeekBalance = NewLabel("—", UiMetrics.BalanceInitialLeft, 0,
                UiMetrics.BalanceInitialWidth, UiMetrics.EquationLabelHeight,
                UiMetrics.FontValue, FontStyle.Bold, Palette.Text);
            deepSeekBalance.BackColor = Palette.Card;
            deepSeekBalance.TextAlign = ContentAlignment.MiddleCenter;
            deepSeekBalance.AutoEllipsis = true;
            equationPanel.Controls.Add(deepSeekBalance);

            plusLabel = NewLabel("+", UiMetrics.PlusInitialLeft, 0, UiMetrics.EquationSymbolWidth,
                UiMetrics.EquationLabelHeight, UiMetrics.FontValue, FontStyle.Bold, Palette.Sub);
            plusLabel.TextAlign = ContentAlignment.MiddleCenter;
            equationPanel.Controls.Add(plusLabel);

            spentLabel = NewValueLabel(UiMetrics.SpentInitialLeft, 0, UiMetrics.SpentInitialWidth);
            equationPanel.Controls.Add(spentLabel);

            equalsLabel = NewLabel("=", UiMetrics.EqualsInitialLeft, 0, UiMetrics.EquationSymbolWidth,
                UiMetrics.EquationLabelHeight, UiMetrics.FontValue, FontStyle.Bold, Palette.Sub);
            equalsLabel.TextAlign = ContentAlignment.MiddleCenter;
            equationPanel.Controls.Add(equalsLabel);

            totalLabel = NewValueLabel(UiMetrics.TotalInitialLeft, 0, UiMetrics.TotalInitialWidth);
            equationPanel.Controls.Add(totalLabel);

            dsBar = new MiniBar();
            dsBar.Bounds = new Rectangle(UiMetrics.DeepSeekBarLeft, UiMetrics.DeepSeekBarTop,
                equationPanel.Width - UiMetrics.DeepSeekBarWidthInset, UiMetrics.RefreshBarHeight);
            dsBar.FillColor = Palette.Blue;
            dsBar.BackColor = Palette.Card;
            equationPanel.Controls.Add(dsBar);

            inlineEditor = new TextBox();
            inlineEditor.BorderStyle = BorderStyle.None;
            inlineEditor.BackColor = Palette.Card;
            inlineEditor.ForeColor = Palette.Text;
            inlineEditor.Font = new Font(UiMetrics.FontFamily, UiMetrics.FontValue, FontStyle.Bold);
            inlineEditor.TextAlign = HorizontalAlignment.Center;
            inlineEditor.Visible = false;
            equationPanel.Controls.Add(inlineEditor);

            equationPanel.Visible = false;
            deepSeekStatus.Text = cfg.DeepSeekKey.Length == 0 ? Strings.NotConfiguredHint : Strings.Loading;

            spentLabel.MouseDown += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) ValueMouseDown(UiMetrics.EquationFieldSpent); };
            totalLabel.MouseDown += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) ValueMouseDown(UiMetrics.EquationFieldTotal); };
            inlineEditor.Leave += delegate { EndEdit(true); };
            inlineEditor.KeyDown += EditorKeyDown;

            refreshBar = new MiniBar();
            refreshBar.Bounds = new Rectangle(UiMetrics.RefreshBarInset, UiMetrics.RefreshBarInitialTop,
                ClientSize.Width - UiMetrics.RefreshBarWidthInset, UiMetrics.RefreshBarHeight);
            refreshBar.FillColor = Palette.Red;
            refreshBar.BackColor = Palette.Bg;
            Controls.Add(refreshBar);

            goCard = new Card();
            goCard.Bounds = new Rectangle(UiMetrics.OuterMargin, UiMetrics.OpenCodeCardInitialTop,
                ClientSize.Width - UiMetrics.CardWidthInset, UiMetrics.OpenCodeCardHeight);
            Controls.Add(goCard);

            goTitle = NewLabel(Strings.OpenCodeName, UiMetrics.CardTextInset, UiMetrics.TitleTop,
                UiMetrics.GoTitleWidth, UiMetrics.TitleHeight, UiMetrics.FontCaption,
                FontStyle.Regular, Palette.Sub);
            goTitle.TextAlign = ContentAlignment.MiddleLeft;
            goCard.Controls.Add(goTitle);

            goRefreshButton = new RefreshButton();
            goRefreshButton.Bounds = new Rectangle(
                goCard.Width - UiMetrics.TitleButtonRightInset - UiMetrics.TitleButtonSize,
                UiMetrics.TitleButtonTop, UiMetrics.TitleButtonSize, UiMetrics.TitleButtonSize);
            goRefreshButton.AccessibleName = Strings.RefreshOpenCode;
            goRefreshButton.Click += delegate { StartOpenCodeFetch(); };
            goCard.Controls.Add(goRefreshButton);

            row5h = NewRow(Strings.FiveHours, UiMetrics.CardTextInset, UiMetrics.GoRowTop,
                goCard.Width - UiMetrics.CardContentInset);
            rowWeek = NewRow(Strings.ThisWeek, UiMetrics.CardTextInset,
                UiMetrics.GoRowTop + UiMetrics.GoRowSpacing, goCard.Width - UiMetrics.CardContentInset);
            rowMonth = NewRow(Strings.ThisMonth, UiMetrics.CardTextInset,
                UiMetrics.GoRowTop + UiMetrics.GoRowSpacing * UiMetrics.GoRowMonthOffset, goCard.Width - UiMetrics.CardContentInset);
            goCard.Controls.Add(row5h);
            goCard.Controls.Add(rowWeek);
            goCard.Controls.Add(rowMonth);

            menu = CreateMenu();
            ContextMenuStrip = menu;
            AttachMenu(this);

            MouseDown += DragWindow;
            MouseMove += ResizeMouseMove;
            MouseUp += ResizeMouseUp;
            MouseEnter += delegate
            {
                lastInside = DateTime.UtcNow;
                if (!cfg.EdgeHide || dockedEdge == EdgeMetrics.DockNone) return;
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
            ApplyTooltips();

            uiTimer = new Timer();
            uiTimer.Interval = Timing.UiTickMilliseconds;
            uiTimer.Tick += delegate
            {
                UpdatePeakBadge();
                row5h.Invalidate();
                rowWeek.Invalidate();
                rowMonth.Invalidate();
                if (edgeHidden || hintAlpha > 0f) Invalidate();
                if (!demoMode && !refreshingPaused) StartDueFetches();
            };

            barTimer = new Timer();
            barTimer.Interval = frameIntervalMilliseconds;
            barTimer.Tick += delegate
            {
                long nowMilliseconds = clock.ElapsedMilliseconds;
                if (dsBar.AdvanceAnimation(nowMilliseconds)) dsBar.Invalidate();
                if (row5h.AdvanceAnimation(nowMilliseconds)) row5h.Invalidate();
                if (rowWeek.AdvanceAnimation(nowMilliseconds)) rowWeek.Invalidate();
                if (rowMonth.AdvanceAnimation(nowMilliseconds)) rowMonth.Invalidate();
                if (dsRefreshButton.AdvanceAnimation(nowMilliseconds)) dsRefreshButton.Invalidate();
                if (goRefreshButton.AdvanceAnimation(nowMilliseconds)) goRefreshButton.Invalidate();

                if (AnyFetching)
                {
                    refreshBar.Indeterminate = true;
                    refreshBar.Phase = (float)((nowMilliseconds % Timing.ShimmerPeriodMilliseconds) /
                        (double)Timing.ShimmerPeriodMilliseconds);
                    refreshBar.Invalidate();
                    return;
                }

                refreshBar.Indeterminate = false;
                refreshBar.Percent = NextRefreshFraction() * UiMetrics.PercentMax;
                refreshBar.Invalidate();
            };

            edgeTimer = new Timer();
            edgeTimer.Interval = Timing.EdgeTickMilliseconds;
            edgeTimer.Tick += delegate { TickEdge(); };

            slideTimer = new Timer();
            slideTimer.Interval = frameIntervalMilliseconds;
            slideTimer.Tick += delegate { TickSlide(); };
        }

        private ContextMenuStrip CreateMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.ShowItemToolTips = true;
            ToolStripItem refreshItem = menu.Items.Add(Strings.RefreshNow, null,
                delegate { StartFetch(); });
            refreshItem.ToolTipText = Strings.RefreshNowTip;
            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem eqItem = new ToolStripMenuItem(Strings.ShowEquation);
            eqItem.ToolTipText = Strings.ShowEquationTip;
            eqItem.Checked = cfg.EquationEnabled;
            eqItem.Click += delegate
            {
                cfg.EquationEnabled = !cfg.EquationEnabled;
                eqItem.Checked = cfg.EquationEnabled;
                cfg.Save();
                UpdateDisplay();
            };
            menu.Items.Add(eqItem);
            ToolStripMenuItem showDsItem = new ToolStripMenuItem(Strings.ShowDeepSeek);
            showDsItem.ToolTipText = Strings.ShowDeepSeekTip;
            showDsItem.Checked = cfg.ShowDeepSeek;
            showDsItem.Click += delegate
            {
                cfg.ShowDeepSeek = !cfg.ShowDeepSeek;
                showDsItem.Checked = cfg.ShowDeepSeek;
                UpdateProviderLayout();
                cfg.Save();
            };
            menu.Items.Add(showDsItem);
            ToolStripMenuItem showGoItem = new ToolStripMenuItem(Strings.ShowOpenCode);
            showGoItem.ToolTipText = Strings.ShowOpenCodeTip;
            showGoItem.Checked = cfg.ShowOpenCode;
            showGoItem.Click += delegate
            {
                cfg.ShowOpenCode = !cfg.ShowOpenCode;
                showGoItem.Checked = cfg.ShowOpenCode;
                UpdateProviderLayout();
                cfg.Save();
            };
            menu.Items.Add(showGoItem);
            menu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem top = new ToolStripMenuItem(Strings.AlwaysOnTop);
            top.ToolTipText = Strings.AlwaysOnTopTip;
            top.Checked = TopMost;
            top.Click += delegate
            {
                TopMost = !TopMost;
                top.Checked = TopMost;
                cfg.TopMost = TopMost;
                cfg.Save();
            };
            menu.Items.Add(top);
            ToolStripMenuItem captureItem = new ToolStripMenuItem(Strings.CaptureHide);
            captureItem.ToolTipText = Strings.CaptureHideTip;
            captureItem.Checked = cfg.HideFromCapture;
            captureItem.Click += delegate
            {
                cfg.HideFromCapture = !cfg.HideFromCapture;
                captureItem.Checked = cfg.HideFromCapture;
                cfg.Save();
                ApplyCaptureVisibility();
            };
            menu.Items.Add(captureItem);
            ToolStripMenuItem themeMenu = new ToolStripMenuItem(Strings.Theme);
            themeMenu.ToolTipText = Strings.ThemeTip;
            ToolStripMenuItem autoItem = new ToolStripMenuItem(Strings.FollowSystem);
            autoItem.ToolTipText = Strings.FollowSystemTip;
            ToolStripMenuItem lightItem = new ToolStripMenuItem(Strings.Light);
            lightItem.ToolTipText = Strings.LightTip;
            ToolStripMenuItem darkItem = new ToolStripMenuItem(Strings.Dark);
            darkItem.ToolTipText = Strings.DarkTip;
            autoItem.Checked = cfg.ThemeMode == AppConfig.ThemeFollowSystem;
            lightItem.Checked = cfg.ThemeMode == AppConfig.ThemeLight;
            darkItem.Checked = cfg.ThemeMode == AppConfig.ThemeDark;
            autoItem.Click += delegate
            {
                SetThemeMode(AppConfig.ThemeFollowSystem);
                autoItem.Checked = true;
                lightItem.Checked = false;
                darkItem.Checked = false;
            };
            lightItem.Click += delegate
            {
                SetThemeMode(AppConfig.ThemeLight);
                autoItem.Checked = false;
                lightItem.Checked = true;
                darkItem.Checked = false;
            };
            darkItem.Click += delegate
            {
                SetThemeMode(AppConfig.ThemeDark);
                autoItem.Checked = false;
                lightItem.Checked = false;
                darkItem.Checked = true;
            };
            themeMenu.DropDownItems.Add(autoItem);
            themeMenu.DropDownItems.Add(lightItem);
            themeMenu.DropDownItems.Add(darkItem);
            themeMenu.DropDown.ShowItemToolTips = true;
            menu.Items.Add(themeMenu);
            ToolStripMenuItem lockItem = new ToolStripMenuItem(Strings.LockWidth);
            lockItem.ToolTipText = Strings.LockWidthTip;
            lockItem.Checked = cfg.WidthLocked;
            lockItem.Click += delegate
            {
                cfg.WidthLocked = !cfg.WidthLocked;
                lockItem.Checked = cfg.WidthLocked;
                cfg.Save();
            };
            menu.Items.Add(lockItem);
            edgeHideItem = new ToolStripMenuItem(Strings.EdgeSnap);
            edgeHideItem.ToolTipText = Strings.EdgeSnapTip;
            edgeHideItem.Checked = cfg.EdgeHide;
            edgeHideItem.Click += delegate
            {
                cfg.EdgeHide = !cfg.EdgeHide;
                edgeHideItem.Checked = cfg.EdgeHide;
                cfg.Save();
                ApplyEdgeHideSetting();
            };
            menu.Items.Add(edgeHideItem);
            menu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem autoStartItem = new ToolStripMenuItem(Strings.AutoStart);
            autoStartItem.ToolTipText = Strings.AutoStartTip;
            autoStartItem.Checked = IsAutoStartEnabled();
            autoStartItem.Enabled = !demoMode;
            autoStartItem.Click += delegate
            {
                bool enable = !autoStartItem.Checked;
                if (SetAutoStart(enable)) autoStartItem.Checked = enable;
            };
            menu.Items.Add(autoStartItem);
            ToolStripMenuItem languageItem = new ToolStripMenuItem(Strings.LanguageSwitch);
            languageItem.ToolTipText = Strings.LanguageTip;
            languageItem.Click += delegate { SetLanguage(Strings.OtherLanguageCode); };
            menu.Items.Add(languageItem);
            menu.Items.Add(new ToolStripSeparator());
            ToolStripItem settingsItem = menu.Items.Add(Strings.SettingsEllipsis, null,
                delegate { OpenSettings(); });
            settingsItem.ToolTipText = Strings.SettingsTip;
            ToolStripItem exitItem = menu.Items.Add(Strings.Exit, null, delegate { Close(); });
            exitItem.ToolTipText = Strings.ExitTip;
            return menu;
        }

        private Label NewLabel(string text, int x, int y, int w, int h, float size, FontStyle style, Color color)
        {
            Label l = new Label();
            l.Text = text;
            l.Bounds = new Rectangle(x, y, w, h);
            l.Font = new Font(UiMetrics.FontFamily, size, style);
            l.ForeColor = color;
            l.BackColor = Color.Transparent;
            l.AutoEllipsis = true;
            return l;
        }

        private UsageRow NewRow(string caption, int x, int y, int w)
        {
            UsageRow r = new UsageRow();
            r.Caption = caption;
            r.Bounds = new Rectangle(x, y, w, UiMetrics.UsageRowHeight);
            return r;
        }

        private Label NewValueLabel(int x, int y, int w)
        {
            Label l = NewLabel("—", x, y, w, UiMetrics.EquationLabelHeight,
                UiMetrics.FontValue, FontStyle.Bold, Palette.Text);
            l.BackColor = Palette.Card;
            l.TextAlign = ContentAlignment.MiddleCenter;
            l.Cursor = Cursors.IBeam;
            l.AutoEllipsis = true;
            return l;
        }

        // ---------- 布局 / 显示 ----------
        private void LayoutContent()
        {
            if (dsCard == null || goCard == null || refreshBar == null) return;
            int w = ClientSize.Width;
            dsCard.Width = w - UiMetrics.CardWidthInset;
            goCard.Width = w - UiMetrics.CardWidthInset;
            refreshBar.Width = w - UiMetrics.RefreshBarWidthInset;
            if (equationPanel != null) equationPanel.Width = dsCard.Width;
            if (dsBar != null)
            {
                dsBar.Left = UiMetrics.DeepSeekBarLeft;
                dsBar.Width = dsCard.Width - UiMetrics.DeepSeekBarWidthInset;
            }
            if (deepSeekStatus != null) deepSeekStatus.Width = dsCard.Width - UiMetrics.CardContentInset;
            if (deepSeekBalanceOnly != null) deepSeekBalanceOnly.Width = dsCard.Width - UiMetrics.CardContentInset;
            if (deepSeekPeriod != null) deepSeekPeriod.Left = dsCard.Width - UiMetrics.DeepSeekPeriodRightInset;
            if (dsRefreshButton != null)
                dsRefreshButton.Left = dsCard.Width - UiMetrics.TitleButtonRightInset -
                    UiMetrics.TitleButtonSize;
            if (goRefreshButton != null)
                goRefreshButton.Left = goCard.Width - UiMetrics.TitleButtonRightInset -
                    UiMetrics.TitleButtonSize;
            if (row5h != null && rowWeek != null && rowMonth != null)
            {
                int rowWidth = goCard.Width - UiMetrics.CardContentInset;
                row5h.Width = rowWidth;
                rowWeek.Width = rowWidth;
                rowMonth.Width = rowWidth;
            }
            LayoutEquation();
        }

        private static int MeasureEquationWidth(Label label)
        {
            string text = string.IsNullOrEmpty(label.Text) ? "0" : label.Text;
            Size size = TextRenderer.MeasureText(text, label.Font,
                new Size(int.MaxValue, label.Height),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            return size.Width + UiMetrics.EquationMeasurePadding;
        }

        private static void PlaceEquation(Label label, int x, int width)
        {
            if (width < UiMetrics.EquationMinLabelWidth) width = UiMetrics.EquationMinLabelWidth;
            label.Bounds = new Rectangle(x, 0, width, UiMetrics.EquationLabelHeight);
        }

        private void LayoutEquation()
        {
            if (equationPanel == null || deepSeekBalance == null || spentLabel == null ||
                totalLabel == null || plusLabel == null || equalsLabel == null) return;

            int available = equationPanel.Width - UiMetrics.EquationAvailableInset;
            if (available < UiMetrics.EquationMinAvailableWidth) return;
            int gap = UiMetrics.EquationGap;
            int symbol = UiMetrics.EquationSymbolWidth;

            int balanceWidth = MeasureEquationWidth(deepSeekBalance);
            int spentWidth = MeasureEquationWidth(spentLabel);
            int totalWidth = MeasureEquationWidth(totalLabel);
            int needed = balanceWidth + spentWidth + totalWidth + symbol * UiMetrics.EquationSymbolCount + gap * UiMetrics.EquationGapCount;
            int left = UiMetrics.EquationHorizontalMargin;

            if (needed > available)
            {
                int values = Math.Max(UiMetrics.EquationMinValuesAreaWidth,
                    available - symbol * UiMetrics.EquationSymbolCount - gap * UiMetrics.EquationGapCount);
                int sum = Math.Max(1, balanceWidth + spentWidth + totalWidth);
                balanceWidth = Math.Max(UiMetrics.EquationMinValueWidth,
                    (int)Math.Round((double)values * balanceWidth / sum));
                spentWidth = Math.Max(UiMetrics.EquationMinValueWidth,
                    (int)Math.Round((double)values * spentWidth / sum));
                totalWidth = Math.Max(UiMetrics.EquationMinValueWidth, values - balanceWidth - spentWidth);
                if (balanceWidth + spentWidth + totalWidth > values)
                {
                    int over = balanceWidth + spentWidth + totalWidth - values;
                    balanceWidth = Math.Max(UiMetrics.EquationMinValueWidth, balanceWidth - over);
                    over = balanceWidth + spentWidth + totalWidth - values;
                    if (over > 0) spentWidth = Math.Max(UiMetrics.EquationMinValueWidth, spentWidth - over);
                }
            }
            else
            {
                left += (available - needed) / 2;
            }

            int x = left;
            PlaceEquation(deepSeekBalance, x, balanceWidth);
            x += balanceWidth + gap;
            PlaceEquation(plusLabel, x, symbol);
            x += symbol + gap;
            PlaceEquation(spentLabel, x, spentWidth);
            x += spentWidth + gap;
            PlaceEquation(equalsLabel, x, symbol);
            x += symbol + gap;
            PlaceEquation(totalLabel, x, totalWidth);
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

            const int pad = UiMetrics.OuterMargin;
            const int barGap = UiMetrics.RefreshBarGap;
            int w = ClientSize.Width;
            int top = pad;

            if (ds)
            {
                dsCard.Bounds = new Rectangle(pad, top, w - UiMetrics.CardWidthInset,
                    UiMetrics.DeepSeekCardHeight);
                top += UiMetrics.DeepSeekCardHeight;
            }

            if (ds && go)
            {
                refreshBar.Bounds = new Rectangle(UiMetrics.RefreshBarInset, top + barGap,
                    w - UiMetrics.RefreshBarWidthInset, UiMetrics.RefreshBarHeight);
                top = refreshBar.Bottom + barGap;
            }

            if (go)
            {
                goCard.Bounds = new Rectangle(pad, top, w - UiMetrics.CardWidthInset,
                    UiMetrics.OpenCodeCardHeight);
                top += UiMetrics.OpenCodeCardHeight;
            }

            bool barAtEnd = !ds || !go;
            if (barAtEnd)
            {
                // At the top or bottom, use the same compact 5px padding on both sides.
                refreshBar.Bounds = new Rectangle(UiMetrics.RefreshBarInset,
                    (ds || go) ? top + barGap : barGap, w - UiMetrics.RefreshBarWidthInset,
                    UiMetrics.RefreshBarHeight);
                top = refreshBar.Bottom;
            }

            refreshBar.Visible = true;
            int height = top + (barAtEnd ? barGap : pad);
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
