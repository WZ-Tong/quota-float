using System;

namespace QuotaFloat
{
    internal static class UiMetrics
    {
        public const string FontFamily = "Segoe UI";

        public const float FontTiny = 7.25f;
        public const float FontCaption = 8.25f;
        public const float FontSmall = 8f;
        public const float FontBody = 9f;
        public const float FontValue = 12f;
        public const float FontBalance = 14f;
        public const float DefaultDpi = 96f;

        public const int WindowCornerRadius = 12;
        public const int CardCornerRadius = 10;
        public const int BarCornerRadius = 2;
        public const int WindowBorderInset = 1;

        public const int OuterMargin = 12;
        public const int CardWidthInset = OuterMargin * 2;
        public const int CardTextInset = 14;
        public const int CardContentInset = CardTextInset * 2;
        public const int RefreshBarInset = CardTextInset;
        public const int RefreshBarWidthInset = RefreshBarInset * 2;
        public const int RefreshBarHeight = 4;
        public const int RefreshBarGap = 5;

        public const int DeepSeekCardHeight = 72;
        public const int OpenCodeCardHeight = 126;
        public const int InitialWindowHeight = 236;
        public const int InitialWindowRightInset = 24;
        public const int InitialWindowTop = 80;

        public const int TitleButtonSize = 18;
        public const int TitleButtonTop = 7;
        public const int TitleHeight = TitleButtonSize;
        public const int TitleTop = TitleButtonTop;
        public const int TitleButtonRightInset = CardTextInset;
        public const int TitleButtonGap = 4;
        public const int DeepSeekTitleWidth = 120;
        public const int DeepSeekPeriodWidth = 86;
        public const int DeepSeekPeriodRightInset =
            TitleButtonRightInset + TitleButtonSize + TitleButtonGap + DeepSeekPeriodWidth;
        public const int DeepSeekStatusTop = 30;
        public const int DeepSeekStatusHeight = 24;
        public const int DeepSeekBalanceTop = 26;
        public const int DeepSeekBalanceHeight = 40;

        public const int EquationTop = 26;
        public const int EquationHeight = 44;
        public const int EquationLabelHeight = 26;
        public const int EquationFieldNone = 0;
        public const int EquationFieldSpent = 1;
        public const int EquationFieldTotal = 2;
        public const int EquationHorizontalMargin = 10;
        public const int EquationGap = 4;
        public const int EquationGapCount = 4;
        public const int EquationSymbolWidth = 14;
        public const int EquationSymbolCount = 2;
        public const int EquationMinLabelWidth = 8;
        public const int EquationMinValueWidth = 20;
        public const int EquationMinValuesAreaWidth = 30;
        public const int EquationMeasurePadding = 12;
        public const int EquationAvailableInset = 20;
        public const int EquationMinAvailableWidth = 40;

        public const int DeepSeekBarLeft = 10;
        public const int DeepSeekBarWidthInset = DeepSeekBarLeft * 2;
        public const int DeepSeekBarTop = 30;

        public const int BalanceInitialLeft = CardTextInset;
        public const int BalanceInitialWidth = 100;
        public const int PlusInitialLeft = 116;
        public const int SpentInitialLeft = 132;
        public const int SpentInitialWidth = 90;
        public const int EqualsInitialLeft = 226;
        public const int TotalInitialLeft = 244;
        public const int TotalInitialWidth = 84;

        public const int RefreshBarInitialTop = OuterMargin + DeepSeekCardHeight + RefreshBarGap;
        public const int OpenCodeCardInitialTop = RefreshBarInitialTop + RefreshBarHeight + RefreshBarGap;
        public const int GoTitleWidth = 160;
        public const int GoRowTop = 28;
        public const int GoRowSpacing = 30;
        public const int GoRowMonthOffset = 2;
        public const int GoRowCount = 3;

        public const int UsageRowHeight = 30;
        public const int UsageCaptionWidth = 56;
        public const int UsageSuffixLeft = 62;
        public const int UsageSuffixMinWidth = 10;
        public const int UsagePercentWidth = 84;
        public const int UsageTextHeight = 18;
        public const int UsageBarTop = 20;
        public const int UsageBarHeight = 4;
        public const int UsageBarMinWidth = 3;

        public const int ShimmerSpanDivisor = 5;
        public const int ShimmerCoreDivisor = 2;
        public const int ShimmerMinSpan = 14;
        public const int ShimmerMinCore = 6;
        public const int ShimmerOuterAlpha = 150;
        public const int ShimmerCoreAlpha = 220;

        public const int HeaderHeight = 38;
        public const int HeaderClickCount = 3;
        public const int ResizeLeftHotZone = 6;
        public const int ResizeRightHotZone = 7;
        public const int ResizeDirectionLeft = -1;
        public const int ResizeDirectionRight = 1;

        public const int PercentMax = 100;
        public const int LevelRedPercent = 5;
        public const int LevelYellowPercent = 20;

        public const int SettingsWidth = 440;
        public const int SettingsHeight = 178;
        public const int SettingsMargin = 16;
        public const int SettingsLabelTop = 14;
        public const int SettingsLabelWidth = 210;
        public const int SettingsHintLeft = 240;
        public const int SettingsHintWidth = 184;
        public const int SettingsInputTop = 36;
        public const int SettingsInputWidth = 408;
        public const int SettingsSecondLabelTop = 72;
        public const int SettingsSecondInputTop = 94;
        public const int SettingsButtonTop = 132;
        public const int SettingsButtonWidth = 90;
        public const int SettingsButtonHeight = 30;
        public const int SettingsSaveLeft = 232;
        public const int SettingsCancelLeft = 332;
        public const int SettingsLabelHeight = 20;
        public const int SettingsInputHeight = 24;

        public const int WideDemoWidth = 700;
    }

    internal static class Timing
    {
        public const int VisibleRefreshSeconds = 120;
        public const int HiddenRefreshSeconds = 600;
        public const int RevealRefreshMinAgeSeconds = 45;
        public const int RetryBaseSeconds = 30;
        public const int RetryShiftLimit = 8;

        public const int UiTickMilliseconds = 1000;
        public const int EdgeTickMilliseconds = 120;
        public const int FallbackFrameIntervalMilliseconds = 16;
        public const int ShimmerPeriodMilliseconds = 1100;
        public const int RefreshSpinPeriodMilliseconds = 900;
        public const int DataBarAnimationMilliseconds = 320;
        public const float DataBarAnimationMinDeltaPercent = 1f;
        public const int SlideDurationMilliseconds = 180;
        public const int HideDelayMilliseconds = 450;
        public const int ShowDelayMilliseconds = 200;
        public const int StartupHideDelayMilliseconds = 1500;
        public const int ValueClickWindowMilliseconds = 700;
        public const int HeaderClickWindowMilliseconds = 800;

        public const int NetworkTimeoutMilliseconds = 12000;
        public const int MillisecondsPerSecond = 1000;
        public const int MinimumTimerIntervalMilliseconds = 1;
        public const int TimerResolutionMilliseconds = 1;

        public const int DefaultRefreshHz = 60;
        public const int MinimumRefreshHz = 24;
        public const int MaximumRefreshHz = 500;
        public const long NoStamp = -1;
    }

    internal static class EdgeMetrics
    {
        public const int DockNone = 0;
        public const int DockLeft = 1;
        public const int DockRight = 2;
        public const int DockTop = 3;
        public const int DockBottom = 4;
        public const int Strip = 10;
        public const int DockTolerance = 14;
        public const double AnchorMin = 0.02;
        public const double AnchorMax = 0.98;

        public const float HintVisibleThreshold = 0.01f;
        public const int HintLength = 56;
        public const int HintThickness = 4;
        public const int HintEndInset = 24;
        public const int HintEdgeInset = 3;
        public const int HintOppositeInset = 7;
        public const int HintMinLength = 8;
        public const int ColorAlphaMax = 255;

        public const int VerticalSegments = 4;
        public const int VerticalGap = 3;
        public const int HorizontalShortSegments = 5;
        public const int HorizontalLongSegments = 10;
        public const int HorizontalLongThreshold = 400;
        public const int HorizontalGap = 5;
        public const int HorizontalLongGrid = 5;
        public const int HorizontalShortGrid = 10;

        public const int SubTickAlpha = 215;
        public const float SubTickWidth = 3f;
        public const int SegmentRadius = 2;
        public const int SegmentMinLengthForTicks = 8;
        public const int SubTickInset = 1;
        public const int SubTickEndInset = 2;
    }

    internal static class DemoData
    {
        public const decimal DeepSeekBalance = 123.45m;
        public const decimal DeepSeekTotal = 200m;
        public const int FiveHourRemaining = 76;
        public const int FiveHourResetHours = 3;
        public const int FiveHourResetMinutes = 12;
        public const int FiveHourResetSeconds = 45;
        public const int WeekRemaining = 59;
        public const int WeekResetDays = 4;
        public const int WeekResetHours = 8;
        public const int WeekResetMinutes = 30;
        public const int MonthRemaining = 37;
        public const int MonthResetDays = 22;
        public const int MonthResetHours = 6;
    }

    internal static class PeakTime
    {
        public const int BeijingUtcOffsetHours = 8;
        public const int MorningStartHour = 9;
        public const int MorningEndHour = 12;
        public const int AfternoonStartHour = 14;
        public const int AfternoonEndHour = 18;
    }

    internal static class KeyMask
    {
        public const int OpenCodePrefixLength = 6;
        public const int DeepSeekPrefixLength = 3;
        public const int TailLength = 4;
    }

    internal static class NetworkMetrics
    {
        public const int TooManyRequestsStatus = 429;
    }
}
