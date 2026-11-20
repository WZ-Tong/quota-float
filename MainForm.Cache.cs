using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        // ---------- 缓存与行显示 ----------
        private static long ToUtcTicks(DateTimeOffset? value)
        {
            return value.HasValue ? value.Value.UtcDateTime.Ticks : 0;
        }

        private static DateTimeOffset? FromUtcTicks(long ticks)
        {
            if (ticks <= 0) return null;
            return new DateTimeOffset(new DateTime(ticks, DateTimeKind.Utc));
        }

        private static DateTime? LocalTimeFromUtcTicks(long ticks)
        {
            if (ticks <= 0) return null;
            return new DateTime(ticks, DateTimeKind.Utc).ToLocalTime();
        }

        private void RestoreOpenCodeCache()
        {
            if (!cfg.OpenCodeCacheKnown || cfg.OpenCodeKey.Length == 0) return;
            openCodeLastSuccess = LocalTimeFromUtcTicks(cfg.OpenCodeCacheUtcTicks);
            ApplyCachedOpenCodeRows(CachedOpenCodeState());
        }

        private void ApplyCachedOpenCodeRows(GoState cached)
        {
            ApplyRow(row5h, cached, cached.Rolling, true);
            ApplyRow(rowWeek, cached, cached.Weekly, true);
            ApplyRow(rowMonth, cached, cached.Monthly, true);
        }

        private GoState CachedOpenCodeState()
        {
            GoState go = new GoState { Configured = true, Ok = true };
            go.Rolling = CachedWindow(
                cfg.OpenCodeRollingUsed,
                cfg.OpenCodeRollingHasData,
                cfg.OpenCodeRollingRateLimited,
                cfg.OpenCodeRollingResetsUtcTicks);
            go.Weekly = CachedWindow(
                cfg.OpenCodeWeeklyUsed,
                cfg.OpenCodeWeeklyHasData,
                cfg.OpenCodeWeeklyRateLimited,
                cfg.OpenCodeWeeklyResetsUtcTicks);
            go.Monthly = CachedWindow(
                cfg.OpenCodeMonthlyUsed,
                cfg.OpenCodeMonthlyHasData,
                cfg.OpenCodeMonthlyRateLimited,
                cfg.OpenCodeMonthlyResetsUtcTicks);
            return go;
        }

        private static GoWindow CachedWindow(int used, bool hasData, bool rateLimited, long resetsUtcTicks)
        {
            GoWindow window = new GoWindow();
            window.Used = used;
            window.HasData = hasData;
            window.RateLimited = rateLimited;
            window.ResetsAt = FromUtcTicks(resetsUtcTicks);
            return window;
        }

        private void CacheOpenCode(GoState go)
        {
            cfg.OpenCodeCacheKnown = true;
            cfg.OpenCodeCacheUtcTicks = DateTime.UtcNow.Ticks;
            cfg.OpenCodeRollingUsed = go.Rolling.Used;
            cfg.OpenCodeRollingHasData = go.Rolling.HasData;
            cfg.OpenCodeRollingRateLimited = go.Rolling.RateLimited;
            cfg.OpenCodeRollingResetsUtcTicks = ToUtcTicks(go.Rolling.ResetsAt);
            cfg.OpenCodeWeeklyUsed = go.Weekly.Used;
            cfg.OpenCodeWeeklyHasData = go.Weekly.HasData;
            cfg.OpenCodeWeeklyRateLimited = go.Weekly.RateLimited;
            cfg.OpenCodeWeeklyResetsUtcTicks = ToUtcTicks(go.Weekly.ResetsAt);
            cfg.OpenCodeMonthlyUsed = go.Monthly.Used;
            cfg.OpenCodeMonthlyHasData = go.Monthly.HasData;
            cfg.OpenCodeMonthlyRateLimited = go.Monthly.RateLimited;
            cfg.OpenCodeMonthlyResetsUtcTicks = ToUtcTicks(go.Monthly.ResetsAt);
            cfg.Save();
        }

        private void ClearOpenCodeRows()
        {
            bool configured = cfg.OpenCodeKey.Length > 0;
            ResetRow(row5h, configured);
            ResetRow(rowWeek, configured);
            ResetRow(rowMonth, configured);
        }

        private void ResetRow(UsageRow row, bool configured)
        {
            row.Configured = configured;
            row.Error = "";
            row.RateLimited = false;
            row.Stale = false;
            row.ResetsAt = null;
            row.SetAnimatedRemaining(-1, clock.ElapsedMilliseconds, false);
            row.Invalidate();
        }

        private void ApplyRow(UsageRow row, GoState go, GoWindow w, bool stale)
        {
            row.Configured = go.Configured;
            row.Error = go.Ok ? "" : go.Error;
            row.RateLimited = go.Ok && w.RateLimited;
            row.Stale = stale;
            row.ResetsAt = go.Ok ? w.ResetsAt : null;
            int remaining = go.Ok && w.HasData && !w.RateLimited
                ? Math.Max(0, UiMetrics.PercentMax - w.Used) : -1;
            if (go.Ok && w.RateLimited) remaining = 0;
            row.SetAnimatedRemaining(remaining, clock.ElapsedMilliseconds, !edgeHidden);
            row.Invalidate();
        }

    }
}
