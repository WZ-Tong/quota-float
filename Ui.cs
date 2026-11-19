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
        public static Color Track = Color.FromArgb(10, 10, 14);
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
                Track = Color.FromArgb(190, 190, 202);
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
                Track = Color.FromArgb(10, 10, 14);
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
            if (string.IsNullOrEmpty(key)) return Strings.NotSet;
            string prefix = "";
            if (key.StartsWith("oc_sk_", StringComparison.OrdinalIgnoreCase)) prefix = key.Substring(0, 6);
            else if (key.StartsWith("sk-", StringComparison.OrdinalIgnoreCase)) prefix = key.Substring(0, 3);
            string tail = key.Length >= 4 ? key.Substring(key.Length - 4) : "";
            return prefix + "****" + tail;
        }
    }
}
