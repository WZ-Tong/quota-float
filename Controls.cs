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
}
