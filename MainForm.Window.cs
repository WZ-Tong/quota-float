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
    internal sealed partial class MainForm
    {
        // ---------- 窗口拖动 / 横向拉伸 ----------
        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (edgeHidden || (slideAnimating && slideOutTarget))
            {
                ShowEdge();
                return;
            }
            if (editingField != UiMetrics.EquationFieldNone)
            {
                EndEdit(true);
                return;
            }
            Point p = PointToClient(Cursor.Position);

            if (!cfg.WidthLocked && (p.X <= UiMetrics.ResizeLeftHotZone ||
                p.X >= ClientSize.Width - UiMetrics.ResizeRightHotZone))
            {
                resizeEdge = p.X <= UiMetrics.ResizeLeftHotZone
                    ? UiMetrics.ResizeDirectionLeft : UiMetrics.ResizeDirectionRight;
                resizeStartX = Cursor.Position.X;
                resizeStartBounds = Bounds;
                resizing = true;
                Capture = true;
                Cursor = Cursors.SizeWE;
                return;
            }

            if (cfg.EquationEnabled && equationPanel.Visible && lastDeepSeekBalanceKnown && editingField == UiMetrics.EquationFieldNone)
            {
                if (ValueRect(spentLabel).Contains(p))
                {
                    ValueMouseDown(UiMetrics.EquationFieldSpent);
                    return;
                }
                if (ValueRect(totalLabel).Contains(p))
                {
                    ValueMouseDown(UiMetrics.EquationFieldTotal);
                    return;
                }
            }

            if (p.Y <= UiMetrics.HeaderHeight)
            {
                DateTime now = DateTime.UtcNow;
                if ((now - lastHeaderClick).TotalMilliseconds > Timing.HeaderClickWindowMilliseconds)
                    headerClicks = 0;
                lastHeaderClick = now;
                headerClicks++;
                if (headerClicks >= UiMetrics.HeaderClickCount)
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
                int minOuter = AppConfig.MinWindowWidth + frame;
                int maxOuter = AppConfig.MaxWindowWidth + frame;
                if (resizeEdge == UiMetrics.ResizeDirectionLeft)
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
            if (!cfg.WidthLocked && (p.X <= UiMetrics.ResizeLeftHotZone ||
                p.X >= ClientSize.Width - UiMetrics.ResizeRightHotZone)) Cursor = Cursors.SizeWE;
            else if (Cursor == Cursors.SizeWE) Cursor = Cursors.Default;
        }

        private void ResizeMouseUp(object sender, MouseEventArgs e)
        {
            if (!resizing) return;
            resizing = false;
            Capture = false;
            Cursor = Cursors.Default;
            resizeEdge = 0;
            cfg.WindowWidth = Math.Max(AppConfig.MinWindowWidth, Math.Min(AppConfig.MaxWindowWidth, ClientSize.Width));
            cfg.X = Left;
            cfg.Y = Top;
            cfg.Save();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_EXITSIZEMOVE)
            {
                int edge = DockEdge();
                if (cfg.EdgeHide && edge != EdgeMetrics.DockNone)
                {
                    dockedEdge = edge;
                    Location = VisibleLocation();
                    slideAnimating = false;
                    slideOutTarget = false;
                    slideTimer.Stop();
                    edgeHidden = false;
                    edgeArmed = true;
                    lastInside = DateTime.UtcNow;
                    hiddenHoverStart = DateTime.MinValue;
                    hideNotBefore = DateTime.MinValue;
                    edgeTimer.Start();
                }
                else
                {
                    dockedEdge = EdgeMetrics.DockNone;
                    edgeHidden = false;
                    slideAnimating = false;
                    slideOutTarget = false;
                    slideTimer.Stop();
                    edgeTimer.Stop();
                }
                cfg.X = Left;
                cfg.Y = Top;
                cfg.WindowWidth = Math.Max(AppConfig.MinWindowWidth, Math.Min(AppConfig.MaxWindowWidth, ClientSize.Width));
                SaveDockState();
                cfg.Save();
                SyncDisplayTiming();
                Invalidate();
            }
            if (m.Msg == WM_DISPLAYCHANGE)
            {
                SyncDisplayTiming();
            }
            if (m.Msg == WM_SETTINGCHANGE || m.Msg == WM_THEMECHANGED)
            {
                if (cfg.ThemeMode == AppConfig.ThemeFollowSystem)
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
            ApplyRoundedRegion();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath p = Ui.Rounded(new Rectangle(0, 0,
                Width - UiMetrics.WindowBorderInset, Height - UiMetrics.WindowBorderInset),
                UiMetrics.WindowCornerRadius))
            using (Pen pen = new Pen(Palette.WindowBorder))
                g.DrawPath(pen, p);
            PaintEdgeHint(g);
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
            if (x == AppConfig.UnsetCoordinate || y == AppConfig.UnsetCoordinate || !onScreen)
            {
                x = wa.Right - Width - UiMetrics.InitialWindowRightInset;
                y = wa.Top + UiMetrics.InitialWindowTop;
            }
            Location = new Point(x, y);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyCaptureVisibility();
            SyncDisplayTiming();
            uiTimer.Start();
            barTimer.Start();
            if (demoMode) ApplyDemo();
            else StartFetch();

            bool startupDocked = false;
            if (cfg.EdgeHide && cfg.EdgeDock >= EdgeMetrics.DockLeft &&
                cfg.EdgeDock <= EdgeMetrics.DockBottom)
            {
                Rectangle wa = Screen.FromControl(this).WorkingArea;
                Location = EdgeLocation(cfg.EdgeDock, cfg.EdgeAnchor, wa);
                dockedEdge = cfg.EdgeDock;
                startupDocked = true;
            }
            else
            {
                dockedEdge = cfg.EdgeHide ? DockEdge() : EdgeMetrics.DockNone;
                if (dockedEdge != EdgeMetrics.DockNone)
                {
                    Location = VisibleLocation();
                    startupDocked = true;
                }
            }

            DateTime now = DateTime.UtcNow;
            edgeArmed = startupDocked;
            lastInside = now;
            hiddenHoverStart = DateTime.MinValue;
            hideNotBefore = startupDocked
                ? now.AddMilliseconds(Timing.StartupHideDelayMilliseconds)
                : DateTime.MinValue;
            if (startupDocked)
            {
                SaveDockState();
                cfg.Save();
            }
            SyncHighResolutionTimer();
            edgeTimer.Start();
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
            if (editingField != UiMetrics.EquationFieldNone) EndEdit(true);
            Point save = dockedEdge != EdgeMetrics.DockNone ? VisibleLocation() : Location;
            cfg.X = save.X;
            cfg.Y = save.Y;
            cfg.WindowWidth = Math.Max(AppConfig.MinWindowWidth, Math.Min(AppConfig.MaxWindowWidth, ClientSize.Width));
            cfg.TopMost = TopMost;
            cfg.Language = Strings.Code;
            SaveDockState();
            cfg.Save();
            uiTimer.Stop();
            barTimer.Stop();
            edgeTimer.Stop();
            slideTimer.Stop();
            SetHighResolutionTimer(false);
            tips.Dispose();
            base.OnFormClosing(e);
        }

        private void OpenSettings()
        {
            EnsureEdgeVisible();
            string oldDeepSeekKey = cfg.DeepSeekKey;
            string oldOpenCodeKey = cfg.OpenCodeKey;
            refreshingPaused = true;
            try
            {
                using (SettingsForm f = new SettingsForm(cfg))
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                    {
                        bool deepSeekChanged = cfg.DeepSeekKey != oldDeepSeekKey;
                        bool openCodeChanged = cfg.OpenCodeKey != oldOpenCodeKey;
                        if (deepSeekChanged)
                        {
                            InvalidateDeepSeekFetch();
                            lastDeepSeekBalanceKnown = false;
                            deepSeekStatus.Text = Strings.Loading;
                            deepSeekStatus.ForeColor = Palette.Sub;
                            UpdateDisplay();
                        }
                        if (openCodeChanged) InvalidateOpenCodeFetch();
                        lastDeepSeekFetchStamp = Timing.NoStamp;
                        lastOpenCodeFetchStamp = Timing.NoStamp;
                        StartFetch();
                    }
                }
            }
            finally { refreshingPaused = false; }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyCaptureVisibility();
        }

        private void ApplyCaptureVisibility()
        {
            if (!IsHandleCreated) return;
            uint desired = WDA_NONE;
            if (cfg.HideFromCapture)
            {
                desired = Environment.OSVersion.Version.Build >= CaptureExcludeMinBuild
                    ? WDA_EXCLUDEFROMCAPTURE : WDA_MONITOR;
            }

            uint applied = desired;
            bool appliedOk = SetWindowDisplayAffinity(Handle, applied);
            if (!appliedOk && desired == WDA_EXCLUDEFROMCAPTURE)
            {
                applied = WDA_MONITOR;
                appliedOk = SetWindowDisplayAffinity(Handle, applied);
            }
            captureExcluded = appliedOk && applied != WDA_NONE;
            captureBlackedOut = appliedOk && applied == WDA_MONITOR;
        }

        private void SyncDisplayTiming()
        {
            if (!IsHandleCreated) return;
            Screen screen = Screen.FromControl(this);
            displayRefreshHz = DisplayTiming.HighestRefreshHz(screen);
            int interval = DisplayTiming.FrameIntervalMilliseconds(displayRefreshHz);
            if (barTimer == null || slideTimer == null) return;
            if (interval == frameIntervalMilliseconds) return;
            frameIntervalMilliseconds = interval;
            barTimer.Interval = interval;
            slideTimer.Interval = interval;
        }

        private void SetHighResolutionTimer(bool enabled)
        {
            if (enabled == highResolutionTimer) return;
            if (enabled)
                highResolutionTimer = DisplayTiming.BeginHighResolutionTimer();
            else
            {
                DisplayTiming.EndHighResolutionTimer();
                highResolutionTimer = false;
            }
        }

        private void SyncHighResolutionTimer()
        {
            SetHighResolutionTimer(!edgeHidden || slideAnimating || !cfg.EdgeHide);
        }
    }
}
