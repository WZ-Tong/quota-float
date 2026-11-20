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
            if (min > EdgeMetrics.DockTolerance) return EdgeMetrics.DockNone;
            if (min == dl) return EdgeMetrics.DockLeft;
            if (min == dr) return EdgeMetrics.DockRight;
            if (min == dt) return EdgeMetrics.DockTop;
            return EdgeMetrics.DockBottom;
        }

        private Point VisibleLocation()
        {
            Rectangle wa = Screen.FromControl(this).WorkingArea;
            if (dockedEdge == EdgeMetrics.DockLeft) return new Point(wa.Left, Top);
            if (dockedEdge == EdgeMetrics.DockRight) return new Point(wa.Right - Width, Top);
            if (dockedEdge == EdgeMetrics.DockTop) return new Point(Left, wa.Top);
            if (dockedEdge == EdgeMetrics.DockBottom) return new Point(Left, wa.Bottom - Height);
            return Location;
        }

        private Point HiddenLocation()
        {
            Rectangle wa = Screen.FromControl(this).WorkingArea;
            if (dockedEdge == EdgeMetrics.DockLeft) return new Point(wa.Left - Width + EdgeMetrics.Strip, Top);
            if (dockedEdge == EdgeMetrics.DockRight) return new Point(wa.Right - EdgeMetrics.Strip, Top);
            if (dockedEdge == EdgeMetrics.DockTop) return new Point(Left, wa.Top - Height + EdgeMetrics.Strip);
            if (dockedEdge == EdgeMetrics.DockBottom) return new Point(Left, wa.Bottom - EdgeMetrics.Strip);
            return Location;
        }

        private Point EdgeLocation(int edge, double anchor, Rectangle wa)
        {
            int x = Left;
            int y = Top;
            if (edge == EdgeMetrics.DockLeft || edge == EdgeMetrics.DockRight)
            {
                x = edge == EdgeMetrics.DockLeft ? wa.Left : wa.Right - Width;
                y = wa.Top + (int)Math.Round(anchor * wa.Height) - Height / 2;
            }
            else
            {
                y = edge == EdgeMetrics.DockTop ? wa.Top : wa.Bottom - Height;
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
            if (edge == EdgeMetrics.DockLeft || edge == EdgeMetrics.DockRight)
                anchor = (Top + Height / 2.0 - wa.Top) / Math.Max(1.0, wa.Height);
            else
                anchor = (Left + Width / 2.0 - wa.Left) / Math.Max(1.0, wa.Width);
            if (anchor < EdgeMetrics.AnchorMin) anchor = EdgeMetrics.AnchorMin;
            if (anchor > EdgeMetrics.AnchorMax) anchor = EdgeMetrics.AnchorMax;
            return anchor;
        }

        private void SaveDockState()
        {
            if (dockedEdge == EdgeMetrics.DockNone)
            {
                cfg.EdgeDock = EdgeMetrics.DockNone;
                return;
            }
            Rectangle wa = Screen.FromControl(this).WorkingArea;
            cfg.EdgeDock = dockedEdge;
            cfg.EdgeAnchor = EdgeAnchorFor(dockedEdge, wa);
        }

        private void TickEdge()
        {
            if (!cfg.EdgeHide || dockedEdge == EdgeMetrics.DockNone) return;
            if (menu.Visible || refreshingPaused || editingField != UiMetrics.EquationFieldNone || resizing)
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
                    if ((now - hiddenHoverStart).TotalMilliseconds >= Timing.ShowDelayMilliseconds) ShowEdge();
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
                        (now - lastInside).TotalMilliseconds >= Timing.HideDelayMilliseconds)
                        HideEdge();
                }
            }
        }

        private void StartSlide(bool hide)
        {
            if (dockedEdge == EdgeMetrics.DockNone) return;
            if (hide && !cfg.EdgeHide) return;
            SetHighResolutionTimer(true);
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
            slideStartStamp = clock.ElapsedTicks;
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
            if (pendingEquationWidth)
            {
                pendingEquationWidth = false;
                EnsureEquationWidth();
            }
            StartSlide(false);
            RefreshIfStaleOnReveal();
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
            double elapsed = ElapsedMilliseconds(slideStartStamp);
            float t = (float)(elapsed / Timing.SlideDurationMilliseconds);
            if (t >= 1f)
            {
                Location = slideTo;
                slideAnimating = false;
                slideTimer.Stop();
                edgeHidden = slideOutTarget;
                hintAlpha = hintTo;
                if (!edgeHidden)
                {
                    lastInside = DateTime.UtcNow;
                    if (pendingEquationWidth)
                    {
                        pendingEquationWidth = false;
                        EnsureEquationWidth();
                    }
                }
                SyncHighResolutionTimer();
                Invalidate();
                return;
            }
            float ease = Ui.EaseOut(t);
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
                if (dockedEdge != EdgeMetrics.DockNone)
                {
                    Location = VisibleLocation();
                    edgeHidden = false;
                }
                hiddenHoverStart = DateTime.MinValue;
                hideNotBefore = DateTime.MinValue;
                hintAlpha = 0f;
                edgeTimer.Stop();
                SyncHighResolutionTimer();
                Invalidate();
                return;
            }

            dockedEdge = DockEdge();
            edgeHidden = false;
            hiddenHoverStart = DateTime.MinValue;
            hideNotBefore = DateTime.MinValue;
            if (dockedEdge != EdgeMetrics.DockNone)
            {
                Location = VisibleLocation();
                edgeArmed = true;
                lastInside = DateTime.UtcNow;
                edgeTimer.Start();
            }
            SyncHighResolutionTimer();
            Invalidate();
        }

        private void EnsureEdgeVisible()
        {
            if (dockedEdge == EdgeMetrics.DockNone) return;
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
            SyncHighResolutionTimer();
            Invalidate();
        }

        private void ApplyRoundedRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            using (GraphicsPath p = Ui.Rounded(new Rectangle(0, 0, Width, Height),
                UiMetrics.WindowCornerRadius))
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
                return row5h.RateLimited ? 0 : Math.Max(0, Math.Min(UiMetrics.PercentMax, row5h.Remaining));
            if (HasDeepSeekEdgeUsage())
            {
                float remaining;
                if (cfg.DeepSeekHasTotal && cfg.DeepSeekTotal > 0m)
                    remaining = (float)(lastDeepSeekBalance * UiMetrics.PercentMax / cfg.DeepSeekTotal);
                else
                    remaining = lastDeepSeekBalance > 0m ? UiMetrics.PercentMax : 0f;
                return Math.Max(0, Math.Min(UiMetrics.PercentMax, (int)Math.Round(remaining)));
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
            if (!cfg.EdgeHide || dockedEdge == EdgeMetrics.DockNone || hintAlpha <= EdgeMetrics.HintVisibleThreshold) return;
            int a = (int)Math.Round(EdgeMetrics.ColorAlphaMax *
                Math.Max(0f, Math.Min(1f, hintAlpha)));
            if (a <= 0) return;

            int pct = EdgeDisplayPercent();
            int len = EdgeMetrics.HintLength;
            int thick = EdgeMetrics.HintThickness;
            int x = 0;
            int y = 0;
            int w = 0;
            int h = 0;
            bool vertical = dockedEdge == EdgeMetrics.DockLeft || dockedEdge == EdgeMetrics.DockRight;
            if (dockedEdge == EdgeMetrics.DockRight)
            {
                len = Math.Max(EdgeMetrics.HintMinLength, Height - EdgeMetrics.HintEndInset);
                x = EdgeMetrics.HintEdgeInset;
                y = (Height - len) / 2;
                w = thick;
                h = len;
            }
            else if (dockedEdge == EdgeMetrics.DockLeft)
            {
                len = Math.Max(EdgeMetrics.HintMinLength, Height - EdgeMetrics.HintEndInset);
                x = Width - EdgeMetrics.HintOppositeInset;
                y = (Height - len) / 2;
                w = thick;
                h = len;
            }
            else if (dockedEdge == EdgeMetrics.DockBottom)
            {
                len = Math.Max(EdgeMetrics.HintMinLength, Width - EdgeMetrics.HintEndInset);
                x = (Width - len) / 2;
                y = EdgeMetrics.HintEdgeInset;
                w = len;
                h = thick;
            }
            else
            {
                len = Math.Max(EdgeMetrics.HintMinLength, Width - EdgeMetrics.HintEndInset);
                x = (Width - len) / 2;
                y = Height - EdgeMetrics.HintOppositeInset;
                w = len;
                h = thick;
            }
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (x + w > Width) x = Width - w;
            if (y + h > Height) y = Height - h;

            int segments;
            int gridStep;
            if (vertical)
            {
                segments = EdgeMetrics.VerticalSegments;
                gridStep = 0;
            }
            else
            {
                segments = len >= EdgeMetrics.HorizontalLongThreshold
                    ? EdgeMetrics.HorizontalLongSegments : EdgeMetrics.HorizontalShortSegments;
                gridStep = segments == EdgeMetrics.HorizontalLongSegments
                    ? EdgeMetrics.HorizontalLongGrid : EdgeMetrics.HorizontalShortGrid;
            }

            Color fillColor = Color.FromArgb(a, EdgeDisplayColor());
            int fillLen = pct > 0
                ? Math.Max(1, (int)Math.Round(len * pct / (double)UiMetrics.PercentMax))
                : 0;
            int subAlpha = (int)Math.Round(EdgeMetrics.SubTickAlpha * hintAlpha);
            int dotAlpha = (int)Math.Round(EdgeMetrics.ParentDotAlpha * hintAlpha);
            Color dotColor = Color.FromArgb(dotAlpha, lightTheme ? Color.Black : Color.White);

            using (SolidBrush tb = new SolidBrush(Color.FromArgb(a, Palette.Track)))
            using (SolidBrush fb = new SolidBrush(fillColor))
            using (Pen subTick = new Pen(Color.FromArgb(subAlpha, Palette.Bg),
                EdgeMetrics.SubTickWidth))
            using (SolidBrush db = new SolidBrush(dotColor))
            {
                Rectangle bar = new Rectangle(x, y, w, h);
                using (GraphicsPath tp = Ui.Rounded(bar, EdgeMetrics.SegmentRadius))
                    g.FillPath(tb, tp);

                if (fillLen > 0)
                {
                    Rectangle fill = vertical
                        ? new Rectangle(x, y + h - fillLen, w, fillLen)
                        : new Rectangle(x, y, fillLen, h);
                    using (GraphicsPath fp = Ui.Rounded(fill, EdgeMetrics.SegmentRadius))
                        g.FillPath(fb, fp);
                }

                if (gridStep > 0 && len >= EdgeMetrics.SegmentMinLengthForTicks)
                {
                    int parentStep = UiMetrics.PercentMax / segments;
                    for (int p = gridStep; p < UiMetrics.PercentMax; p += gridStep)
                    {
                        if (p % parentStep == 0) continue;
                        int pos = (vertical ? y : x) +
                            (int)Math.Round(len * p / (double)UiMetrics.PercentMax);
                        if (vertical)
                            g.DrawLine(subTick, x + EdgeMetrics.SubTickInset, pos,
                                x + thick - EdgeMetrics.SubTickEndInset, pos);
                        else
                            g.DrawLine(subTick, pos, y + EdgeMetrics.SubTickInset,
                                pos, y + thick - EdgeMetrics.SubTickEndInset);
                    }
                }

                if (dotAlpha > 0)
                {
                    float radius = EdgeMetrics.ParentDotDiameter / 2f;
                    for (int i = 1; i < segments; i++)
                    {
                        double fraction = (double)i / segments;
                        if (vertical)
                        {
                            int cy = y + (int)Math.Round(len * fraction);
                            int cx = x + thick / 2;
                            g.FillEllipse(db, cx - radius, cy - radius,
                                EdgeMetrics.ParentDotDiameter, EdgeMetrics.ParentDotDiameter);
                        }
                        else
                        {
                            int cx = x + (int)Math.Round(len * fraction);
                            int cy = y + thick / 2;
                            g.FillEllipse(db, cx - radius, cy - radius,
                                EdgeMetrics.ParentDotDiameter, EdgeMetrics.ParentDotDiameter);
                        }
                    }
                }
            }
        }
    }
}
