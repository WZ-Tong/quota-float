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
            using (GraphicsPath p = Ui.Rounded(new Rectangle(0, 0, Width, Height), UiMetrics.CardCornerRadius))
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
            using (GraphicsPath p = Ui.Rounded(new Rectangle(0, 0, Width - UiMetrics.WindowBorderInset, Height - UiMetrics.WindowBorderInset), UiMetrics.CardCornerRadius))
            using (Pen pen = new Pen(Palette.CardBorder))
                e.Graphics.DrawPath(pen, p);
        }
    }

    internal sealed class MiniBar : Control
    {
        public float Percent;
        public Color FillColor = Palette.Blue;
        public bool Indeterminate;
        public float Phase;
        public bool HasValue;

        private bool animating;
        private float displayPercent;
        private float animationFrom;
        private long animationStartMilliseconds;

        public MiniBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = UiMetrics.RefreshBarHeight;
        }

        public void SetAnimatedPercent(float target, long nowMilliseconds, bool animate)
        {
            target = Math.Max(0f, Math.Min(UiMetrics.PercentMax, target));
            if (!animate || !HasValue ||
                Math.Abs(target - Percent) < Timing.DataBarAnimationMinDeltaPercent)
            {
                Percent = target;
                displayPercent = target;
                animating = false;
                HasValue = true;
                Invalidate();
                return;
            }

            animationFrom = animating ? displayPercent : Percent;
            Percent = target;
            animationStartMilliseconds = nowMilliseconds;
            animating = true;
            HasValue = true;
            Invalidate();
        }

        public bool AdvanceAnimation(long nowMilliseconds)
        {
            if (!animating) return false;
            double t = (nowMilliseconds - animationStartMilliseconds) /
                (double)Timing.DataBarAnimationMilliseconds;
            if (t >= 1.0)
            {
                displayPercent = Percent;
                animating = false;
            }
            else
            {
                displayPercent = animationFrom +
                    (Percent - animationFrom) * Ui.EaseOut((float)t);
            }
            return true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int w = ClientSize.Width;
            int h = ClientSize.Height;
            if (w <= 0 || h <= 0) return;
            using (GraphicsPath bgp = Ui.Rounded(new Rectangle(0, 0, w, h), UiMetrics.BarCornerRadius))
            using (SolidBrush bg = new SolidBrush(Palette.BarBg))
                e.Graphics.FillPath(bg, bgp);

            if (Indeterminate)
            {
                int span = Math.Max(UiMetrics.ShimmerMinSpan, w / UiMetrics.ShimmerSpanDivisor);
                float travel = w + span;
                float phase = Phase - (float)Math.Floor(Phase);
                int x = (int)Math.Round(-span + travel * phase);
                using (GraphicsPath sp = Ui.Rounded(new Rectangle(x, 0, span, h), UiMetrics.BarCornerRadius))
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(UiMetrics.ShimmerOuterAlpha, FillColor)))
                    e.Graphics.FillPath(sb, sp);
                int core = Math.Max(UiMetrics.ShimmerMinCore, span / UiMetrics.ShimmerCoreDivisor);
                int coreX = x + (span - core) / 2;
                using (GraphicsPath cp = Ui.Rounded(new Rectangle(coreX, 0, core, h), UiMetrics.BarCornerRadius))
                using (SolidBrush cb = new SolidBrush(Color.FromArgb(UiMetrics.ShimmerCoreAlpha, FillColor)))
                    e.Graphics.FillPath(cb, cp);
                return;
            }

            float pct = Math.Max(0f, Math.Min(UiMetrics.PercentMax,
                animating ? displayPercent : Percent));
            if (pct <= 0f) return;
            int fw = (int)Math.Round(w * pct / UiMetrics.PercentMax);
            if (fw < 1) fw = 1;
            if (fw > w) fw = w;
            using (GraphicsPath fp = Ui.Rounded(new Rectangle(0, 0, fw, h), UiMetrics.BarCornerRadius))
            using (SolidBrush fb = new SolidBrush(FillColor))
                e.Graphics.FillPath(fb, fp);
        }
    }

    internal sealed class RefreshButton : Control
    {
        private const float IconInsetRatio = 0.18f;
        private const float PenWidthRatio = 0.09f;
        private const float ArcStartDegrees = 35f;
        private const float ArcSweepDegrees = 280f;
        private const float ArrowLengthRatio = 0.24f;
        private const float ArrowHalfWidthRatio = 0.15f;
        private const float ArrowTipRatio = 0.45f;
        private const float ArrowBaseRatio = 0.55f;

        private bool busy;
        private bool hover;
        private long spinStartMilliseconds;
        private float angle;

        public RefreshButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            TabStop = false;
            AccessibleRole = AccessibleRole.PushButton;
        }

        public bool Busy
        {
            get { return busy; }
        }

        public void SetBusy(bool value, long nowMilliseconds)
        {
            if (busy == value) return;
            busy = value;
            spinStartMilliseconds = nowMilliseconds;
            angle = 0f;
            Invalidate();
        }

        public bool AdvanceAnimation(long nowMilliseconds)
        {
            if (!busy) return false;
            double elapsed = nowMilliseconds - spinStartMilliseconds;
            angle = (float)((elapsed % Timing.RefreshSpinPeriodMilliseconds) /
                Timing.RefreshSpinPeriodMilliseconds * 360.0);
            return true;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hover = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float size = Math.Min(ClientSize.Width, ClientSize.Height);
            if (size <= 0f) return;

            Color color = busy ? Palette.Blue : (hover ? Palette.Text : Palette.Sub);
            float cx = ClientSize.Width / 2f;
            float cy = ClientSize.Height / 2f;
            float inset = size * IconInsetRatio;
            RectangleF arc = new RectangleF(inset, inset, size - inset * 2f, size - inset * 2f);
            using (Pen pen = new Pen(color, Math.Max(1f, size * PenWidthRatio)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                GraphicsState state = g.Save();
                g.TranslateTransform(cx, cy);
                g.RotateTransform(angle);
                g.TranslateTransform(-cx, -cy);
                g.DrawArc(pen, arc, ArcStartDegrees, ArcSweepDegrees);

                double radians = (ArcStartDegrees + ArcSweepDegrees) * Math.PI / 180.0;
                PointF end = new PointF(
                    cx + (float)(Math.Cos(radians) * arc.Width / 2.0),
                    cy + (float)(Math.Sin(radians) * arc.Height / 2.0));
                PointF tangent = new PointF(
                    (float)(-Math.Sin(radians)), (float)Math.Cos(radians));
                PointF normal = new PointF(-tangent.Y, tangent.X);
                float arrowLength = size * ArrowLengthRatio;
                float arrowHalfWidth = size * ArrowHalfWidthRatio;
                PointF tip = new PointF(
                    end.X + tangent.X * arrowLength * ArrowTipRatio,
                    end.Y + tangent.Y * arrowLength * ArrowTipRatio);
                PointF baseCenter = new PointF(
                    end.X - tangent.X * arrowLength * ArrowBaseRatio,
                    end.Y - tangent.Y * arrowLength * ArrowBaseRatio);
                PointF first = new PointF(
                    baseCenter.X + normal.X * arrowHalfWidth,
                    baseCenter.Y + normal.Y * arrowHalfWidth);
                PointF second = new PointF(
                    baseCenter.X - normal.X * arrowHalfWidth,
                    baseCenter.Y - normal.Y * arrowHalfWidth);
                using (GraphicsPath arrow = new GraphicsPath())
                {
                    arrow.AddPolygon(new PointF[] { tip, first, second });
                    using (SolidBrush arrowBrush = new SolidBrush(color))
                        g.FillPath(arrowBrush, arrow);
                }
                g.Restore(state);
            }
        }
    }

    internal sealed class UsageRow : Control
    {
        public string Caption = "";
        public int Remaining = -1;
        public bool Configured;
        public bool RateLimited;
        public bool Stale;
        public string Error = "";
        public DateTimeOffset? ResetsAt;

        private readonly Font captionFont = new Font(UiMetrics.FontFamily, UiMetrics.FontCaption);
        private bool animating;
        private float displayRemaining;
        private float animationFrom;
        private long animationStartMilliseconds;

        public UsageRow()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Palette.Card;
            Height = UiMetrics.UsageRowHeight;
        }

        public void SetAnimatedRemaining(int target, long nowMilliseconds, bool animate)
        {
            int previous = Remaining;
            Remaining = target;
            if (!animate || target < 0 || previous < 0 ||
                Math.Abs(target - previous) < Timing.DataBarAnimationMinDeltaPercent)
            {
                displayRemaining = target;
                animating = false;
                Invalidate();
                return;
            }

            animationFrom = animating ? displayRemaining : previous;
            animationStartMilliseconds = nowMilliseconds;
            animating = true;
            Invalidate();
        }

        public bool AdvanceAnimation(long nowMilliseconds)
        {
            if (!animating) return false;
            double t = (nowMilliseconds - animationStartMilliseconds) /
                (double)Timing.DataBarAnimationMilliseconds;
            if (t >= 1.0)
            {
                displayRemaining = Remaining;
                animating = false;
            }
            else
            {
                displayRemaining = animationFrom +
                    (Remaining - animationFrom) * Ui.EaseOut((float)t);
            }
            return true;
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
            if (!Configured) return Strings.NotConfiguredKey;
            if (Error.Length > 0) return Error;
            if (RateLimited) return Strings.RateLimited(Countdown());
            if (Remaining < 0) return Strings.WaitingData;
            if (Stale) return Strings.StalePrefix + Countdown();
            return Countdown();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = ClientSize.Width;
            Color level = Ui.LevelColor(Remaining);
            string pct = Remaining >= 0 ? Strings.Remaining(Remaining) : "—";
            Color pctColor = Remaining >= 0 ? level : Palette.Sub;
            if (Error.Length > 0 || RateLimited) { pct = "—"; pctColor = Palette.Red; }

            TextRenderer.DrawText(g, Caption, captionFont, new Rectangle(0, 0, UiMetrics.UsageCaptionWidth, UiMetrics.UsageTextHeight),
                Palette.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            string suffix = SuffixText();
            Color suffixColor = (Error.Length > 0 || RateLimited) ? Palette.Red :
                (Stale && Remaining >= 0 ? Palette.Yellow : Palette.Sub);
            const int suffixLeft = UiMetrics.UsageSuffixLeft;
            int suffixW = w - UiMetrics.UsagePercentWidth - suffixLeft;
            if (suffixW > UiMetrics.UsageSuffixMinWidth)
            {
                TextRenderer.DrawText(g, suffix, captionFont, new Rectangle(suffixLeft, 0, suffixW, UiMetrics.UsageTextHeight),
                    suffixColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            }

            TextRenderer.DrawText(g, pct, captionFont, new Rectangle(w - UiMetrics.UsagePercentWidth, 0, UiMetrics.UsagePercentWidth, UiMetrics.UsageTextHeight),
                pctColor, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            int barY = UiMetrics.UsageBarTop;
            int barH = UiMetrics.UsageBarHeight;
            using (GraphicsPath bgp = Ui.Rounded(new Rectangle(0, barY, w, barH), UiMetrics.BarCornerRadius))
            using (SolidBrush bgb = new SolidBrush(Palette.BarBg))
                g.FillPath(bgb, bgp);

            float barRemaining = animating ? displayRemaining : Remaining;
            float barPct = Math.Max(0f, Math.Min(UiMetrics.PercentMax, barRemaining));
            if (barPct > 0f && Error.Length == 0)
            {
                int fw = Math.Max(UiMetrics.UsageBarMinWidth,
                    (int)Math.Round(w * barPct / UiMetrics.PercentMax));
                if (fw > w) fw = w;
                using (GraphicsPath fp = Ui.Rounded(new Rectangle(0, barY, fw, barH), UiMetrics.BarCornerRadius))
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
}
