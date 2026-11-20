#if DEBUG
using System;
using System.Threading;
using System.Windows.Forms;

namespace QuotaFloat
{
    internal static class DebugShell
    {
        public static void Start(MainForm form)
        {
            Thread thread = new Thread(delegate() { Loop(form); });
            thread.IsBackground = true;
            thread.Name = "QuotaFloat.DebugShell";
            thread.Start();
        }

        private static void Loop(MainForm form)
        {
            try { Console.Title = "QuotaFloat Debug"; }
            catch { }

            Console.WriteLine("debug-ready");
            Console.WriteLine("commands: status, menu, eq, policy, show, hide, dock left|right|top|bottom, move X Y, top on|off, lang en|zh, quit");

            while (true)
            {
                string line = Console.ReadLine();
                if (line == null) return;
                line = line.Trim();
                if (line.Length == 0) continue;

                string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string command = parts[0].ToLowerInvariant();

                if (command == "quit" || command == "exit")
                {
                    Run(form, delegate { form.Close(); });
                    return;
                }
                if (command == "help")
                {
                    Console.WriteLine("commands: status, menu, eq, policy, show, hide, dock left|right|top|bottom, move X Y, top on|off, lang en|zh, quit");
                    continue;
                }
                if (command == "status")
                {
                    Run(form, delegate { Console.WriteLine("status " + form.DebugStatus()); });
                    continue;
                }
                if (command == "menu")
                {
                    Run(form, delegate { Console.WriteLine("menu " + form.DebugMenu()); });
                    continue;
                }
                if (command == "eq")
                {
                    Run(form, delegate { Console.WriteLine("eq " + form.DebugEquation()); });
                    continue;
                }

                if (command == "policy")
                {
                    Run(form, delegate { Console.WriteLine("policy " + form.DebugRefreshPolicy()); });
                    continue;
                }
                if (command == "show")
                {
                    Run(form, delegate { form.DebugShow(); });
                    continue;
                }
                if (command == "hide")
                {
                    Run(form, delegate { form.DebugHide(); });
                    continue;
                }
                if (command == "dock" && parts.Length >= 2)
                {
                    int edge;
                    if (!TryEdge(parts[1], out edge))
                    {
                        Console.WriteLine("error invalid edge");
                        continue;
                    }
                    Run(form, delegate { form.DebugDock(edge); });
                    continue;
                }
                if (command == "move" && parts.Length >= 3)
                {
                    int x;
                    int y;
                    if (!int.TryParse(parts[1], out x) || !int.TryParse(parts[2], out y))
                    {
                        Console.WriteLine("error invalid coordinates");
                        continue;
                    }
                    Run(form, delegate { form.DebugMove(x, y); });
                    continue;
                }
                if (command == "lang" && parts.Length >= 2)
                {
                    string value = parts[1].ToLowerInvariant();
                    if (value != "en" && value != "zh" && value != "zh-cn")
                    {
                        Console.WriteLine("error use lang en|zh");
                        continue;
                    }
                    string code = value == "en" ? Strings.En : Strings.ZhCn;
                    Run(form, delegate { form.DebugLanguage(code); });
                    continue;
                }
                if (command == "top" && parts.Length >= 2)
                {
                    string value = parts[1].ToLowerInvariant();
                    if (value != "on" && value != "off")
                    {
                        Console.WriteLine("error use top on|off");
                        continue;
                    }
                    bool topMost = value == "on";
                    Run(form, delegate { form.TopMost = topMost; });
                    continue;
                }

                Console.WriteLine("error unknown command");
            }
        }

        private static void Run(MainForm form, Action action)
        {
            if (form == null || form.IsDisposed) return;
            try { form.BeginInvoke(action); }
            catch { }
        }

        private static bool TryEdge(string value, out int edge)
        {
            edge = EdgeMetrics.DockNone;
            if (value.Equals("left", StringComparison.OrdinalIgnoreCase)) { edge = EdgeMetrics.DockLeft; return true; }
            if (value.Equals("right", StringComparison.OrdinalIgnoreCase)) { edge = EdgeMetrics.DockRight; return true; }
            if (value.Equals("top", StringComparison.OrdinalIgnoreCase)) { edge = EdgeMetrics.DockTop; return true; }
            if (value.Equals("bottom", StringComparison.OrdinalIgnoreCase)) { edge = EdgeMetrics.DockBottom; return true; }
            return false;
        }
    }
}
#endif
