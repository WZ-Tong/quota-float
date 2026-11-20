#if DEBUG
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace QuotaFloat
{
    internal sealed partial class MainForm
    {
        internal string DebugStatus()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "bounds={0},{1},{2},{3} dock={4} hidden={5} hint={6:0.00} topmost={7} lang={8} interval={9} fetching={10} age={11:0.0} hz={12} frame={13} capture={14} highres={15} dsbusy={16} gobusy={17}",
                Bounds.Left, Bounds.Top, Bounds.Width, Bounds.Height,
                dockedEdge, edgeHidden ? "True" : "False",
                hintAlpha, TopMost ? "True" : "False", Strings.Code,
                CurrentRefreshSeconds(), AnyFetching ? "True" : "False",
                OldestFetchAgeSeconds(),
                displayRefreshHz, frameIntervalMilliseconds,
                captureExcluded ? (captureBlackedOut ? "black" : "excluded") : "visible",
                highResolutionTimer ? "on" : "off",
                fetchingDeepSeek ? "True" : "False",
                fetchingOpenCode ? "True" : "False");
        }

        internal string DebugMenu()
        {
            List<string> items = new List<string>();
            foreach (ToolStripItem item in menu.Items)
                items.Add(item.Text);
            return string.Join("|", items.ToArray());
        }

        internal string DebugEquation()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "eqpanel={0} balance={1},{2},\"{3}\" plus={4},{5},\"{6}\" spent={7},{8},\"{9}\" equals={10},{11},\"{12}\" total={13},{14},\"{15}\" symbolMin={16}",
                equationPanel.Width,
                deepSeekBalance.Left, deepSeekBalance.Width, deepSeekBalance.Text,
                plusLabel.Left, plusLabel.Width, plusLabel.Text,
                spentLabel.Left, spentLabel.Width, spentLabel.Text,
                equalsLabel.Left, equalsLabel.Width, equalsLabel.Text,
                totalLabel.Left, totalLabel.Width, totalLabel.Text,
                UiMetrics.EquationSymbolInitialWidth);
        }

        internal string DebugRefreshPolicy()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "v0={0} v1={1} v2={2} v3={3} v4={4} h0={5} h1={6} h2={7} h3={8} h4={9}",
                RefreshPolicy.EffectiveSeconds(Timing.VisibleRefreshSeconds, 0),
                RefreshPolicy.EffectiveSeconds(Timing.VisibleRefreshSeconds, 1),
                RefreshPolicy.EffectiveSeconds(Timing.VisibleRefreshSeconds, 2),
                RefreshPolicy.EffectiveSeconds(Timing.VisibleRefreshSeconds, 3),
                RefreshPolicy.EffectiveSeconds(Timing.VisibleRefreshSeconds, 4),
                RefreshPolicy.EffectiveSeconds(Timing.HiddenRefreshSeconds, 0),
                RefreshPolicy.EffectiveSeconds(Timing.HiddenRefreshSeconds, 1),
                RefreshPolicy.EffectiveSeconds(Timing.HiddenRefreshSeconds, 2),
                RefreshPolicy.EffectiveSeconds(Timing.HiddenRefreshSeconds, 3),
                RefreshPolicy.EffectiveSeconds(Timing.HiddenRefreshSeconds, 4));
        }

        internal void DebugShow()
        {
            if (dockedEdge == EdgeMetrics.DockNone) return;
            ShowEdge();
        }

        internal void DebugHide()
        {
            if (dockedEdge == EdgeMetrics.DockNone) return;
            HideEdge();
        }

        internal void DebugDock(int edge)
        {
            if (edge < EdgeMetrics.DockLeft || edge > EdgeMetrics.DockBottom) return;
            dockedEdge = edge;
            slideAnimating = false;
            slideTimer.Stop();
            slideOutTarget = false;
            edgeHidden = false;
            hintAlpha = 0f;
            Location = VisibleLocation();
            edgeArmed = true;
            lastInside = DateTime.UtcNow;
            hideNotBefore = DateTime.MinValue;
            hiddenHoverStart = DateTime.MinValue;
            edgeTimer.Start();
            Invalidate();
        }

        internal void DebugLanguage(string code)
        {
            SetLanguage(code);
        }

        internal void DebugMove(int x, int y)
        {
            dockedEdge = EdgeMetrics.DockNone;
            slideAnimating = false;
            slideTimer.Stop();
            slideOutTarget = false;
            edgeHidden = false;
            hintAlpha = 0f;
            edgeTimer.Stop();
            Location = new Point(x, y);
            Invalidate();
        }
    }
}
#endif
