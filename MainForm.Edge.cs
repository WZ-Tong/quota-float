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
        // ---------- 贴边滑出 / 悬停恢复 ----------
        private int DockEdge()
        {
            Rectangle wa = Screen.FromControl(this).WorkingArea;
            int dl = Math.Max(0, Left - wa.Left);
            int dr = Math.Max(0, wa.Right - Right);
            int dt = Math.Max(0, Top - wa.Top);
            int db = Math.Max(0, wa.Bottom - Bottom);
            int min = Math.Min(Math.Min(dl, dr), Math.Min(dt, db));
            if (min > DockTolerance) return 0;
            if (min == dl) return 1;
            if (min == dr) return 2;
            if (min == dt) return 3;
            return 4;
        }

        private Point VisibleLocation()
        {
            Rectangle wa = Screen.FromControl(this).WorkingArea;
            if (dockedEdge == 1) return new Point(wa.Left, Top);
            if (dockedEdge == 2) return new Point(wa.Right - Width, Top);
            if (dockedEdge == 3) return new Point(Left, wa.Top);
            if (dockedEdge == 4) return new Point(Left, wa.Bottom - Height);
            return Location;
        }

        private Point HiddenLocation()
        {
            Rectangle wa = Screen.FromControl(this).WorkingArea;
            if (dockedEdge == 1) return new Point(wa.Left - Width + EdgeStrip, Top);
            if (dockedEdge == 2) return new Point(wa.Right - EdgeStrip, Top);
            if (dockedEdge == 3) return new Point(Left, wa.Top - Height + EdgeStrip);
            if (dockedEdge == 4) return new Point(Left, wa.Bottom - EdgeStrip);
            return Location;
        }

        private Point EdgeLocation(int edge, double anchor, Rectangle wa)
        {
            int x = Left;
            int y = Top;
            if (edge == 1 || edge == 2)
            {
                x = edge == 1 ? wa.Left : wa.Right - Width;
                y = wa.Top + (int)Math.Round(anchor * wa.Height) - Height / 2;
            }
            else
            {
                y = edge == 3 ? wa.Top : wa.Bottom - Height;
                x = wa.Left + (int)Math.Round(anchor * wa.Width) - Width / 2;
            }
            if (x < wa.Left) x = wa.Left;
            if (y < wa.Top) y = wa.Top;
            if (x + Width > wa.Right) x = wa.Right - Width;
            if (y + Height > wa.Bottom) y = wa.Bottom - Height;
            return new Point(x, y);
        }

        private double EdgeAnchorFor(int edge, Rectangle wa)
        {
            double anchor;
            if (edge == 1 || edge == 2)
                anchor = (Top + Height / 2.0 - wa.Top) / Math.Max(1.0, wa.Height);
            else
                anchor = (Left + Width / 2.0 - wa.Left) / Math.Max(1.0, wa.Width);
            if (anchor < 0.02) anchor = 0.02;
            if (anchor > 0.98) anchor = 0.98;
            return anchor;
        }

        private void SaveDockState()
        {
            if (dockedEdge == 0)
            {
                cfg.EdgeDock = 0;
                return;
            }
            Rectangle wa = Screen.FromControl(this).WorkingArea;
            cfg.EdgeDock = dockedEdge;
            cfg.EdgeAnchor = EdgeAnchorFor(dockedEdge, wa);
        }

        private void TickEdge()
        {
            if (!cfg.EdgeHide || dockedEdge == 0) return;
            if (menu.Visible || refreshingPaused || editingField != 0 || resizing)
            {
                lastInside = DateTime.UtcNow;
                hiddenHoverStart = DateTime.MinValue;
                return;
            }
            if (slideAnimating) return;

            bool inside = Bounds.Contains(Cursor.Position);
            DateTime now = DateTime.UtcNow;
            if (inside)
            {
                lastInside = now;
                edgeArmed = true;
                if (edgeHidden)
                {
                    if (hiddenHoverStart == DateTime.MinValue) hiddenHoverStart = now;
                    if ((now - hiddenHoverStart).TotalMilliseconds >= ShowDelay) ShowEdge();
                }
                else
                {
                    hiddenHoverStart = DateTime.MinValue;
                }
            }
            else
            {
                hiddenHoverStart = DateTime.MinValue;
                if (edgeArmed && !edgeHidden)
                {
                    if (now >= hideNotBefore &&
                        (now - lastInside).TotalMilliseconds >= HideDelay)
                        HideEdge();
                }
            }
        }

        private void StartSlide(bool hide)
        {
            if (dockedEdge == 0) return;
            if (hide && !cfg.EdgeHide) return;
            hiddenHoverStart = DateTime.MinValue;
            Point target = hide ? HiddenLocation() : VisibleLocation();
            if (Location == target)
            {
                slideAnimating = false;
                slideTimer.Stop();
                slideOutTarget = hide;
                edgeHidden = hide;
                hintAlpha = hide ? 1f : 0f;
                if (!hide)
                {
                    hideNotBefore = DateTime.MinValue;
                    lastInside = DateTime.UtcNow;
                }
                Invalidate();
                return;
            }
            slideFrom = Location;
            slideTo = target;
            slideOutTarget = hide;
            slideAnimating = true;
            slideStartTick = Environment.TickCount;
            hintFrom = hintAlpha;
            hintTo = hide ? 1f : 0f;
            if (!hide)
            {
                edgeHidden = false;
                hideNotBefore = DateTime.MinValue;
                lastInside = DateTime.UtcNow;
            }
            slideTimer.Start();
        }

        private void ShowEdge()
        {
            StartSlide(false);
        }

        private void HideEdge()
        {
            StartSlide(true);
        }

        private void TickSlide()
        {
            if (!slideAnimating)
            {
                slideTimer.Stop();
                return;
            }
            int elapsed = Environment.TickCount - slideStartTick;
            float t = (float)elapsed / SlideDuration;
            if (t >= 1f)
            {
                Location = slideTo;
                slideAnimating = false;
                slideTimer.Stop();
                edgeHidden = slideOutTarget;
                hintAlpha = hintTo;
                if (!edgeHidden) lastInside = DateTime.UtcNow;
                Invalidate();
                return;
            }
            float ease = 1f - (1f - t) * (1f - t) * (1f - t);
            int x = slideFrom.X + (int)Math.Round((slideTo.X - slideFrom.X) * ease);
            int y = slideFrom.Y + (int)Math.Round((slideTo.Y - slideFrom.Y) * ease);
            Location = new Point(x, y);
            hintAlpha = hintFrom + (hintTo - hintFrom) * ease;
            Invalidate();
        }

        private void ApplyEdgeHideSetting()
        {
            if (!cfg.EdgeHide)
            {
                slideTimer.Stop();
                slideAnimating = false;
                slideOutTarget = false;
                if (dockedEdge != 0)
                {
                    Location = VisibleLocation();
                    edgeHidden = false;
                }
                hiddenHoverStart = DateTime.MinValue;
                hideNotBefore = DateTime.MinValue;
                hintAlpha = 0f;
                edgeTimer.Stop();
                Invalidate();
                return;
            }

            dockedEdge = DockEdge();
            edgeHidden = false;
            hiddenHoverStart = DateTime.MinValue;
            hideNotBefore = DateTime.MinValue;
            if (dockedEdge != 0)
            {
                Location = VisibleLocation();
                edgeArmed = true;
                lastInside = DateTime.UtcNow;
                edgeTimer.Start();
            }
            Invalidate();
        }

        private void EnsureEdgeVisible()
        {
            if (dockedEdge == 0) return;
            if (!edgeHidden && !(slideAnimating && slideOutTarget)) return;
            slideTimer.Stop();
            slideAnimating = false;
            slideOutTarget = false;
            edgeHidden = false;
            hiddenHoverStart = DateTime.MinValue;
            hideNotBefore = DateTime.MinValue;
            hintAlpha = 0f;
            Location = VisibleLocation();
            lastInside = DateTime.UtcNow;
            Invalidate();
        }

        private void ApplyRoundedRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            using (GraphicsPath p = Ui.Rounded(new Rectangle(0, 0, Width, Height), 12))
            {
                Region old = Region;
                Region = new Region(p);
                if (old != null) old.Dispose();
            }
        }

        private bool HasOpenCodeEdgeUsage()
        {
            return cfg.ShowOpenCode && row5h.Configured &&
                   row5h.Error.Length == 0 && row5h.Remaining >= 0;
        }

        private bool HasDeepSeekEdgeUsage()
        {
            return cfg.ShowDeepSeek && lastDeepSeekBalanceKnown;
        }

        private int EdgeDisplayPercent()
        {
            if (HasOpenCodeEdgeUsage())
                return row5h.RateLimited ? 0 : Math.Max(0, Math.Min(100, row5h.Remaining));
            if (HasDeepSeekEdgeUsage())
            {
                float remaining;
                if (cfg.DeepSeekHasTotal && cfg.DeepSeekTotal > 0m)
                    remaining = (float)(lastDeepSeekBalance * 100m / cfg.DeepSeekTotal);
                else
                    remaining = lastDeepSeekBalance > 0m ? 100f : 0f;
                return Math.Max(0, Math.Min(100, (int)Math.Round(remaining)));
            }
            return -1;
        }

        private Color EdgeDisplayColor()
        {
            if (HasOpenCodeEdgeUsage())
                return row5h.RateLimited ? Palette.Red : Ui.LevelColor(row5h.Remaining);
            if (HasDeepSeekEdgeUsage()) return Palette.Blue;
            return Palette.Sub;
        }

        private void PaintEdgeHint(Graphics g)
        {
            if (!cfg.EdgeHide || dockedEdge == 0 || hintAlpha <= 0.01f) return;
            int a = (int)Math.Round(255f * Math.Max(0f, Math.Min(1f, hintAlpha)));
            if (a <= 0) return;

            int pct = EdgeDisplayPercent();
            int len = 56;
            int thick = 4;
            int x = 0;
            int y = 0;
            int w = 0;
            int h = 0;
            bool vertical = dockedEdge == 1 || dockedEdge == 2;
            if (dockedEdge == 2)
            {
                len = Math.Max(8, Height - 24);
                x = 3;
                y = (Height - len) / 2;
                w = thick;
                h = len;
            }
            else if (dockedEdge == 1)
            {
                len = Math.Max(8, Height - 24);
                x = Width - 7;
                y = (Height - len) / 2;
                w = thick;
                h = len;
            }
            else if (dockedEdge == 4)
            {
                len = Math.Max(8, Width - 24);
                x = (Width - len) / 2;
                y = 3;
                w = len;
                h = thick;
            }
            else
            {
                len = Math.Max(8, Width - 24);
                x = (Width - len) / 2;
                y = Height - 7;
                w = len;
                h = thick;
            }
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (x + w > Width) x = Width - w;
            if (y + h > Height) y = Height - h;

            Rectangle track = new Rectangle(x, y, w, h);
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(a, Palette.Track)))
            using (GraphicsPath tp = Ui.Rounded(track, 2))
                g.FillPath(tb, tp);

            if (pct > 0)
            {
                int fillLen = (int)Math.Round(len * pct / 100f);
                if (fillLen < thick) fillLen = thick;
                if (fillLen > len) fillLen = len;
                Rectangle fill;
                if (vertical)
                    fill = new Rectangle(x, track.Bottom - fillLen, thick, fillLen);
                else
                    fill = new Rectangle(track.X, y, fillLen, thick);

                using (SolidBrush fb = new SolidBrush(Color.FromArgb(a, EdgeDisplayColor())))
                using (GraphicsPath fp = Ui.Rounded(fill, 2))
                    g.FillPath(fb, fp);
            }

            using (Pen tick = new Pen(Color.FromArgb(110, Palette.Sub), 1f))
            {
                for (int i = 1; i < 4; i++)
                {
                    int pos = (int)Math.Round(len * i / 4.0);
                    if (vertical)
                        g.DrawLine(tick, x, track.Y + pos, x + thick - 1, track.Y + pos);
                    else
                        g.DrawLine(tick, track.X + pos, y, track.X + pos, y + thick - 1);
                }
            }
        }
    }
}
