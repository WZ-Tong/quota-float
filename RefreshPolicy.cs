using System;

namespace QuotaFloat
{
    internal static class RefreshPolicy
    {
        public static int EffectiveSeconds(int normalSeconds, int failureCount)
        {
            if (failureCount <= 0) return normalSeconds;
            int shift = Math.Min(failureCount - 1, Timing.RetryShiftLimit);
            int retry = Timing.RetryBaseSeconds << shift;
            return Math.Min(normalSeconds, Math.Max(Timing.RetryBaseSeconds, retry));
        }

        public static float RemainingFraction(double ageSeconds, int intervalSeconds)
        {
            if (intervalSeconds <= 0) return 0f;
            double remaining = 1.0 - ageSeconds / intervalSeconds;
            return (float)Math.Max(0.0, Math.Min(1.0, remaining));
        }
    }
}
