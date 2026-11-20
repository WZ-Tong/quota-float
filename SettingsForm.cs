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
    // ---------- 设置窗口 ----------
    internal sealed class SettingsForm : Form
    {
        private const int WS_EX_TOOLWINDOW = 0x80;

        private readonly AppConfig cfg;
        private readonly TextBox dsBox;
        private readonly TextBox goBox;

        public SettingsForm(AppConfig config)
        {
            cfg = config;
            Text = Strings.Settings;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(UiMetrics.SettingsWidth, UiMetrics.SettingsHeight);
            BackColor = Palette.Bg;
            ForeColor = Palette.Text;
            Font = new Font(UiMetrics.FontFamily, UiMetrics.FontBody);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(UiMetrics.DefaultDpi, UiMetrics.DefaultDpi);

            AddLabel("DeepSeek API Key", UiMetrics.SettingsMargin, UiMetrics.SettingsLabelTop, UiMetrics.SettingsLabelWidth);
            Label dsHint = AddLabel(Strings.CurrentKey(Ui.MaskKey(cfg.DeepSeekKey)), UiMetrics.SettingsHintLeft, UiMetrics.SettingsLabelTop, UiMetrics.SettingsHintWidth);
            dsHint.TextAlign = ContentAlignment.MiddleRight;
            dsHint.Font = new Font(UiMetrics.FontFamily, UiMetrics.FontTiny);
            dsBox = AddInput(UiMetrics.SettingsMargin, UiMetrics.SettingsInputTop, UiMetrics.SettingsInputWidth);

            AddLabel("OpenCode API Key", UiMetrics.SettingsMargin, UiMetrics.SettingsSecondLabelTop, UiMetrics.SettingsLabelWidth);
            Label goHint = AddLabel(Strings.CurrentKey(Ui.MaskKey(cfg.OpenCodeKey)), UiMetrics.SettingsHintLeft, UiMetrics.SettingsSecondLabelTop, UiMetrics.SettingsHintWidth);
            goHint.TextAlign = ContentAlignment.MiddleRight;
            goHint.Font = new Font(UiMetrics.FontFamily, UiMetrics.FontTiny);
            goBox = AddInput(UiMetrics.SettingsMargin, UiMetrics.SettingsSecondInputTop, UiMetrics.SettingsInputWidth);

            Button save = new Button();
            save.Text = Strings.Save;
            save.Bounds = new Rectangle(UiMetrics.SettingsSaveLeft, UiMetrics.SettingsButtonTop, UiMetrics.SettingsButtonWidth, UiMetrics.SettingsButtonHeight);
            StyleButton(save, Palette.Blue, Color.White);
            save.Click += delegate
            {
                string dsNew = dsBox.Text.Trim();
                string goNew = goBox.Text.Trim();
                bool dsChanged = dsNew.Length > 0 && dsNew != cfg.DeepSeekKey;
                if (dsNew.Length > 0) cfg.DeepSeekKey = dsNew;
                if (goNew.Length > 0) cfg.OpenCodeKey = goNew;
                if (dsChanged)
                {
                    cfg.DeepSeekTotal = 0m;
                    cfg.DeepSeekHasTotal = false;
                    cfg.DeepSeekBalance = 0m;
                    cfg.DeepSeekSpent = 0m;
                    cfg.DeepSeekCurrency = "CNY";
                    cfg.DeepSeekBalanceKnown = false;
                }
                cfg.Save();
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(save);

            Button cancel = new Button();
            cancel.Text = Strings.Cancel;
            cancel.Bounds = new Rectangle(UiMetrics.SettingsCancelLeft, UiMetrics.SettingsButtonTop, UiMetrics.SettingsButtonWidth, UiMetrics.SettingsButtonHeight);
            StyleButton(cancel, Palette.InputBg, Palette.Text);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            AcceptButton = save;
            CancelButton = cancel;
        }

        private Label AddLabel(string text, int x, int y, int w)
        {
            Label l = new Label();
            l.Text = text;
            l.Bounds = new Rectangle(x, y, w, UiMetrics.SettingsLabelHeight);
            l.ForeColor = Palette.Sub;
            l.BackColor = Color.Transparent;
            Controls.Add(l);
            return l;
        }

        private TextBox AddInput(int x, int y, int w)
        {
            TextBox t = new TextBox();
            t.Bounds = new Rectangle(x, y, w, UiMetrics.SettingsInputHeight);
            t.BackColor = Palette.InputBg;
            t.ForeColor = Palette.Text;
            t.BorderStyle = BorderStyle.FixedSingle;
            t.UseSystemPasswordChar = true;
            Controls.Add(t);
            return t;
        }

        private static void StyleButton(Button b, Color back, Color fore)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = back;
            b.ForeColor = fore;
            b.Cursor = Cursors.Hand;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW;
                return cp;
            }
        }
    }
}
