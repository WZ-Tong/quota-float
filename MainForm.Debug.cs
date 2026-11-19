#if DEBUG
using System;
using System.Drawing;
using System.Globalization;

namespace QuotaFloat
{
    internal sealed partial class MainForm
    {
        internal string DebugStatus()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "bounds={0},{1},{2},{3} dock={4} hidden={5} hint={6:0.00} topmost={7}",
                Bounds.Left, Bounds.Top, Bounds.Width, Bounds.Height,
                dockedEdge, edgeHidden ? "True" : "False",
                hintAlpha, TopMost ? "True" : "False");
        }

        internal void DebugShow()
        {
            if (dockedEdge == 0) return;
            ShowEdge();
        }

        internal void DebugHide()
        {
            if (dockedEdge == 0) return;
            HideEdge();
        }

        internal void DebugDock(int edge)
        {
            if (edge < 1 || edge > 4) return;
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

        internal void DebugMove(int x, int y)
        {
            dockedEdge = 0;
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
