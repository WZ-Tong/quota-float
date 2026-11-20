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
        // ---------- 刷新调度 ----------
        private int CurrentRefreshSeconds()
        {
            return edgeHidden ? Timing.HiddenRefreshSeconds : Timing.VisibleRefreshSeconds;
        }

        private double SecondsSince(long stamp)
        {
            if (stamp == Timing.NoStamp) return double.MaxValue;
            return ElapsedSeconds(stamp);
        }

        private double ElapsedSeconds(long stamp)
        {
            if (stamp == Timing.NoStamp) return 0.0;
            return (clock.ElapsedTicks - stamp) / (double)Stopwatch.Frequency;
        }

        private double ElapsedMilliseconds(long stamp)
        {
            return ElapsedSeconds(stamp) * Timing.MillisecondsPerSecond;
        }

        private double OldestFetchAgeSeconds()
        {
            double age = -1.0;
            if (lastDeepSeekFetchStamp != Timing.NoStamp)
                age = SecondsSince(lastDeepSeekFetchStamp);
            if (lastOpenCodeFetchStamp != Timing.NoStamp)
            {
                double goAge = SecondsSince(lastOpenCodeFetchStamp);
                if (age < 0.0 || goAge > age) age = goAge;
            }
            return age;
        }

        private float NextRefreshFraction()
        {
            float fraction = 1f;
            bool any = false;
            if (cfg.DeepSeekKey.Length > 0)
            {
                fraction = Math.Min(fraction, RemainingFraction(lastDeepSeekFetchStamp,
                    EffectiveFetchSeconds(deepSeekFailureCount)));
                any = true;
            }
            if (cfg.OpenCodeKey.Length > 0)
            {
                fraction = Math.Min(fraction, RemainingFraction(lastOpenCodeFetchStamp,
                    EffectiveFetchSeconds(openCodeFailureCount)));
                any = true;
            }
            return any ? fraction : 1f;
        }

        private float RemainingFraction(long stamp, int interval)
        {
            if (stamp == Timing.NoStamp) return 0f;
            return RefreshPolicy.RemainingFraction(SecondsSince(stamp), interval);
        }

        private int EffectiveFetchSeconds(int failureCount)
        {
            return RefreshPolicy.EffectiveSeconds(CurrentRefreshSeconds(), failureCount);
        }

        private void StartDueFetches()
        {
            if (cfg.DeepSeekKey.Length > 0 && !fetchingDeepSeek &&
                SecondsSince(lastDeepSeekFetchStamp) >= EffectiveFetchSeconds(deepSeekFailureCount))
                StartDeepSeekFetch();
            if (cfg.OpenCodeKey.Length > 0 && !fetchingOpenCode &&
                SecondsSince(lastOpenCodeFetchStamp) >= EffectiveFetchSeconds(openCodeFailureCount))
                StartOpenCodeFetch();
        }

        private void RefreshIfStaleOnReveal()
        {
            if (demoMode || refreshingPaused) return;
            int dsAge = Math.Min(Timing.RevealRefreshMinAgeSeconds,
                EffectiveFetchSeconds(deepSeekFailureCount));
            int goAge = Math.Min(Timing.RevealRefreshMinAgeSeconds,
                EffectiveFetchSeconds(openCodeFailureCount));
            if (cfg.DeepSeekKey.Length > 0 && !fetchingDeepSeek &&
                SecondsSince(lastDeepSeekFetchStamp) >= dsAge)
                StartDeepSeekFetch();
            if (cfg.OpenCodeKey.Length > 0 && !fetchingOpenCode &&
                SecondsSince(lastOpenCodeFetchStamp) >= goAge)
                StartOpenCodeFetch();
        }

        private void StartFetch()
        {
            StartDeepSeekFetch();
            StartOpenCodeFetch();
        }

    }
}
