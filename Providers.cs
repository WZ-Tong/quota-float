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
}
