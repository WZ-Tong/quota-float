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
            resizeEdge = 0;
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
                int edge = DockEdge();
                if (cfg.EdgeHide && edge != 0)
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
                    dockedEdge = 0;
                    edgeHidden = false;
                    slideAnimating = false;
                    slideOutTarget = false;
                    slideTimer.Stop();
                    edgeTimer.Stop();
                }
                cfg.X = Left;
                cfg.Y = Top;
                cfg.WindowWidth = Math.Max(360, Math.Min(720, ClientSize.Width));
                SaveDockState();
                cfg.Save();
                Invalidate();
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
            ApplyRoundedRegion();
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

            bool startupDocked = false;
            if (cfg.EdgeHide && cfg.EdgeDock >= 1 && cfg.EdgeDock <= 4)
            {
                Rectangle wa = Screen.FromControl(this).WorkingArea;
                Location = EdgeLocation(cfg.EdgeDock, cfg.EdgeAnchor, wa);
                dockedEdge = cfg.EdgeDock;
                startupDocked = true;
            }
            else
            {
                dockedEdge = cfg.EdgeHide ? DockEdge() : 0;
                if (dockedEdge != 0)
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
                ? now.AddMilliseconds(StartupHideDelay)
                : DateTime.MinValue;
            if (startupDocked)
            {
                SaveDockState();
                cfg.Save();
            }
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
            if (editingField != 0) EndEdit(true);
            Point save = dockedEdge != 0 ? VisibleLocation() : Location;
            cfg.X = save.X;
            cfg.Y = save.Y;
            cfg.WindowWidth = Math.Max(360, Math.Min(720, ClientSize.Width));
            cfg.TopMost = TopMost;
            SaveDockState();
            cfg.Save();
            uiTimer.Stop();
            barTimer.Stop();
            edgeTimer.Stop();
            slideTimer.Stop();
            base.OnFormClosing(e);
        }

        private void OpenSettings()
        {
            EnsureEdgeVisible();
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
    }
}
