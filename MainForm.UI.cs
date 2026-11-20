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
        // ---------- 等式内联编辑 ----------
        private void ValueMouseDown(int field)
        {
            if (!cfg.EquationEnabled || !equationPanel.Visible) return;
            if (editingField != UiMetrics.EquationFieldNone)
            {
                EndEdit(true);
                return;
            }
            DateTime now = DateTime.UtcNow;
            if (lastValueField == field &&
                (now - lastValueClick).TotalMilliseconds <= Timing.ValueClickWindowMilliseconds)
            {
                lastValueField = UiMetrics.EquationFieldNone;
                BeginEdit(field);
            }
            else
            {
                lastValueField = field;
                lastValueClick = now;
            }
        }

        private void BeginEdit(int field)
        {
            if (!cfg.EquationEnabled || !equationPanel.Visible) return;
            if (!lastDeepSeekBalanceKnown || editingField != UiMetrics.EquationFieldNone) return;
            Label target = field == UiMetrics.EquationFieldSpent ? spentLabel : totalLabel;
            editingField = field;
            inlineEditor.Bounds = target.Bounds;
            inlineEditor.Text = target.Text == "—" ? "" : target.Text;
            target.Visible = false;
            inlineEditor.Visible = true;
            refreshingPaused = true;
            inlineEditor.Focus();
            inlineEditor.SelectionStart = 0;
            inlineEditor.SelectionLength = inlineEditor.Text.Length;
        }

        private void EditorKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                EndEdit(true);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                EndEdit(false);
                e.SuppressKeyPress = true;
            }
        }

        private void EndEdit(bool commit)
        {
            if (editingField == UiMetrics.EquationFieldNone) return;
            int field = editingField;
            editingField = UiMetrics.EquationFieldNone;

            if (commit && lastDeepSeekBalanceKnown)
            {
                string text = inlineEditor.Text.Trim();
                if (field == UiMetrics.EquationFieldTotal)
                {
                    if (text.Length == 0)
                    {
                        cfg.DeepSeekHasTotal = false;
                        cfg.DeepSeekTotal = 0m;
                    }
                    else
                    {
                        decimal t;
                        if (TryParseDecimal(text, out t) && t >= 0m)
                        {
                            cfg.DeepSeekTotal = t;
                            cfg.DeepSeekHasTotal = t > 0m;
                        }
                    }
                }
                else
                {
                    decimal s;
                    if (text.Length > 0 && TryParseDecimal(text, out s) && s >= 0m)
                    {
                        decimal t = lastDeepSeekBalance + s;
                        cfg.DeepSeekTotal = t;
                        cfg.DeepSeekHasTotal = t > 0m;
                    }
                }
                cfg.DeepSeekBalance = lastDeepSeekBalance;
                cfg.DeepSeekCurrency = lastDeepSeekCurrency;
                cfg.DeepSeekBalanceKnown = true;
                cfg.DeepSeekSpent = cfg.DeepSeekHasTotal
                    ? Math.Max(0m, cfg.DeepSeekTotal - lastDeepSeekBalance)
                    : 0m;
                cfg.Save();
            }

            inlineEditor.Visible = false;
            spentLabel.Visible = true;
            totalLabel.Visible = true;
            refreshingPaused = false;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (!lastDeepSeekBalanceKnown)
            {
                equationPanel.Visible = false;
                deepSeekBalanceOnly.Visible = false;
                deepSeekStatus.Visible = true;
                return;
            }
            if (cfg.EquationEnabled)
            {
                equationPanel.Visible = true;
                deepSeekBalanceOnly.Visible = false;
                deepSeekStatus.Visible = false;
                UpdateEquationFields();
            }
            else
            {
                equationPanel.Visible = false;
                deepSeekBalanceOnly.Visible = true;
                deepSeekStatus.Visible = false;
                string sym = CurrencySymbol(lastDeepSeekCurrency);
                deepSeekBalanceOnly.Text = sym + " " + lastDeepSeekBalance.ToString("0.00", CultureInfo.InvariantCulture);
            }
        }

        private void UpdateEquationFields()
        {
            if (!lastDeepSeekBalanceKnown) return;
            string sym = CurrencySymbol(lastDeepSeekCurrency);
            deepSeekBalance.Text = sym + " " + lastDeepSeekBalance.ToString("0.00", CultureInfo.InvariantCulture);
            float barTarget;
            if (cfg.DeepSeekHasTotal)
            {
                decimal s = cfg.DeepSeekTotal - lastDeepSeekBalance;
                if (s < 0m) s = 0m;
                spentLabel.Text = s.ToString("0.00", CultureInfo.InvariantCulture);
                totalLabel.Text = cfg.DeepSeekTotal.ToString("0.00", CultureInfo.InvariantCulture);
                barTarget = cfg.DeepSeekTotal > 0m
                    ? (float)(lastDeepSeekBalance * UiMetrics.PercentMax / cfg.DeepSeekTotal) : 0f;
            }
            else
            {
                spentLabel.Text = "0";
                totalLabel.Text = lastDeepSeekBalance.ToString("0.00", CultureInfo.InvariantCulture);
                barTarget = lastDeepSeekBalance > 0m ? UiMetrics.PercentMax : 0f;
            }
            dsBar.SetAnimatedPercent(barTarget, clock.ElapsedMilliseconds, !edgeHidden);
            LayoutEquation();
        }

        private static bool TryParseDecimal(string s, out decimal v)
        {
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out v)) return true;
            return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out v);
        }

        // ---------- 主题 ----------
        private bool ResolveLightTheme()
        {
            if (cfg.ThemeMode == AppConfig.ThemeLight) return true;
            if (cfg.ThemeMode == AppConfig.ThemeDark) return false;
            return IsSystemLightTheme();
        }

        private static bool IsSystemLightTheme()
        {
            try
            {
                object value = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "AppsUseLightTheme", 0);
                return value != null && Convert.ToInt32(value) != 0;
            }
            catch { return false; }
        }

        private void SetThemeMode(int mode)
        {
            cfg.ThemeMode = mode;
            cfg.Save();
            ApplyTheme(mode == AppConfig.ThemeFollowSystem
                ? IsSystemLightTheme() : mode == AppConfig.ThemeLight);
        }

        private void ApplyTheme(bool light)
        {
            if (lightTheme == light) return;
            Color oldSub = Palette.Sub;
            Color oldText = Palette.Text;
            Color oldRed = Palette.Red;
            Color oldGreen = Palette.Green;
            Color oldYellow = Palette.Yellow;
            Color oldCard = Palette.Card;

            Palette.Apply(light);
            lightTheme = light;
            BackColor = Palette.Bg;

            dsBar.FillColor = Palette.Blue;
            refreshBar.FillColor = Palette.Red;
            RecolorControls(this, oldSub, oldText, oldRed, oldGreen, oldYellow, oldCard);
            Invalidate(true);
        }

        private void RecolorControls(Control parent, Color oldSub, Color oldText,
            Color oldRed, Color oldGreen, Color oldYellow, Color oldCard)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Label)
                {
                    Color f = c.ForeColor;
                    if (f == oldSub) c.ForeColor = Palette.Sub;
                    else if (f == oldText) c.ForeColor = Palette.Text;
                    else if (f == oldRed) c.ForeColor = Palette.Red;
                    else if (f == oldGreen) c.ForeColor = Palette.Green;
                    else if (f == oldYellow) c.ForeColor = Palette.Yellow;
                    if (c.BackColor == oldCard) c.BackColor = Palette.Card;
                }
                else if (c is TextBox)
                {
                    if (c.BackColor == oldCard) c.BackColor = Palette.Card;
                    if (c.ForeColor == oldText) c.ForeColor = Palette.Text;
                }
                else if (c is Card || c is UsageRow || c is MiniBar || c is Panel)
                {
                    if (c == refreshBar) c.BackColor = Palette.Bg;
                    else if (c.BackColor == oldCard) c.BackColor = Palette.Card;
                    c.Invalidate();
                }
                RecolorControls(c, oldSub, oldText, oldRed, oldGreen, oldYellow, oldCard);
            }
        }

        private static bool IsAutoStartEnabled()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return k != null && k.GetValue("QuotaFloat") != null;
                }
            }
            catch { return false; }
        }

        private static bool SetAutoStart(bool enabled)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run"))
                {
                    if (k == null) return false;
                    if (enabled)
                        k.SetValue("QuotaFloat", "\"" + Application.ExecutablePath + "\"",
                            RegistryValueKind.String);
                    else
                        k.DeleteValue("QuotaFloat", false);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Strings.AutoStartFailed(ex.Message), "QuotaFloat",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        private void ApplyLanguage()
        {
            Text = demoMode ? Strings.AppTitleDemo : Strings.AppTitle;
            row5h.Caption = Strings.FiveHours;
            rowWeek.Caption = Strings.ThisWeek;
            rowMonth.Caption = Strings.ThisMonth;
            row5h.Invalidate();
            rowWeek.Invalidate();
            rowMonth.Invalidate();

            peakState = null;
            UpdatePeakBadge();

            if (!lastDeepSeekBalanceKnown)
            {
                deepSeekStatus.Text = cfg.DeepSeekKey.Length == 0
                    ? Strings.NotConfiguredHint
                    : Strings.Loading;
            }
            else
            {
                UpdateDisplay();
            }

            if (!demoMode)
            {
                lastFetchStamp = Timing.NoStamp;
                StartFetch();
            }
            Invalidate(true);
        }

        private void SetLanguage(string code)
        {
            code = Strings.Normalize(code);
            if (code == Strings.Code) return;
            Strings.Code = code;
            cfg.Language = code;
            cfg.Save();
            ApplyLanguage();
            if (IsHandleCreated && !IsDisposed)
                BeginInvoke(new Action(RebuildMenu));
        }

        private void RebuildMenu()
        {
            if (IsDisposed) return;
            ContextMenuStrip old = menu;
            menu = CreateMenu();
            ContextMenuStrip = menu;
            AttachMenu(this);
            if (old != null) old.Dispose();
        }

        private static bool HasArg(string name)
        {
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        // ---------- 调试 ----------
        private void ApplyDemo()
        {
            lastFetchStamp = clock.ElapsedTicks;
            lastDeepSeekBalance = DemoData.DeepSeekBalance;
            lastDeepSeekCurrency = "CNY";
            lastDeepSeekBalanceKnown = true;
            cfg.DeepSeekTotal = DemoData.DeepSeekTotal;
            cfg.DeepSeekHasTotal = true;
            UpdateDisplay();

            row5h.Configured = true;
            row5h.Error = "";
            row5h.RateLimited = false;
            row5h.Remaining = DemoData.FiveHourRemaining;
            row5h.ResetsAt = DateTimeOffset.Now.AddHours(DemoData.FiveHourResetHours)
                .AddMinutes(DemoData.FiveHourResetMinutes)
                .AddSeconds(DemoData.FiveHourResetSeconds);
            row5h.Invalidate();

            rowWeek.Configured = true;
            rowWeek.Error = "";
            rowWeek.RateLimited = false;
            rowWeek.Remaining = DemoData.WeekRemaining;
            rowWeek.ResetsAt = DateTimeOffset.Now.AddDays(DemoData.WeekResetDays)
                .AddHours(DemoData.WeekResetHours)
                .AddMinutes(DemoData.WeekResetMinutes);
            rowWeek.Invalidate();

            rowMonth.Configured = true;
            rowMonth.Error = "";
            rowMonth.RateLimited = false;
            rowMonth.Remaining = DemoData.MonthRemaining;
            rowMonth.ResetsAt = DateTimeOffset.Now.AddDays(DemoData.MonthResetDays)
                .AddHours(DemoData.MonthResetHours);
            rowMonth.Invalidate();

            UpdatePeakBadge();
        }

        private void UpdatePeakBadge()
        {
            bool peak = IsPeakNow();
            if (peakState.HasValue && peakState.Value == peak) return;
            peakState = peak;
            deepSeekPeriod.Text = peak ? Strings.Peak : Strings.OffPeak;
            deepSeekPeriod.ForeColor = peak ? Palette.Red : Palette.Green;
        }

        private static bool IsPeakNow()
        {
            DateTimeOffset beijing = DateTimeOffset.UtcNow.ToOffset(
                TimeSpan.FromHours(PeakTime.BeijingUtcOffsetHours));
            DayOfWeek day = beijing.DayOfWeek;
            if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday) return false;
            TimeSpan time = beijing.TimeOfDay;
            return (time >= TimeSpan.FromHours(PeakTime.MorningStartHour) &&
                    time < TimeSpan.FromHours(PeakTime.MorningEndHour))
                || (time >= TimeSpan.FromHours(PeakTime.AfternoonStartHour) &&
                    time < TimeSpan.FromHours(PeakTime.AfternoonEndHour));
        }

        private void AttachDrag(Control c)
        {
            if (c == equationPanel) return;
            if (!(c is TextBox)) c.MouseDown += DragWindow;
            foreach (Control child in c.Controls)
            {
                if (!(child is TextBox)) AttachDrag(child);
            }
        }

        private void AttachMenu(Control c)
        {
            if (!(c is TextBox)) c.ContextMenuStrip = menu;
            foreach (Control child in c.Controls)
            {
                if (!(child is TextBox)) AttachMenu(child);
            }
        }
    }
}
