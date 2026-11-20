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
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace QuotaFloat
{
    internal static class Program
    {
        private const string MutexName = "Local\\QuotaFloat.SingleInstance";

        [STAThread]
        private static void Main()
        {
            bool demoMode = HasArg("--demo");
            Mutex mutex = null;
            if (!demoMode)
            {
                bool created;
                mutex = new Mutex(true, MutexName, out created);
                if (!created)
                {
                    mutex.Dispose();
                    return;
                }
            }

            try
            {
                try { ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; }
                catch { }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                MainForm form = new MainForm();
#if DEBUG
                DebugShell.Start(form);
#endif
                Application.Run(form);
            }
            finally
            {
                if (mutex != null)
                {
                    try { mutex.ReleaseMutex(); }
                    catch { }
                    mutex.Dispose();
                }
            }
        }

        private static bool HasArg(string name)
        {
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
