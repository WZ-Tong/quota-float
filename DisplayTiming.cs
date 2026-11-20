using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace QuotaFloat
{
    internal static class DisplayTiming
    {
        private const int EnumCurrentSettings = -1;
        private const int DisplayFlagInterlaced = 0x00000001;
        private const int ModeIndexStart = 0;
        private const int UnknownRefreshHzThreshold = 1;
        private const int DeviceNameLength = 32;
        private const int FormNameLength = 32;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DevMode
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = DeviceNameLength)]
            public string DeviceName;
            public short SpecVersion;
            public short DriverVersion;
            public short Size;
            public short DriverExtra;
            public int Fields;
            public int PositionX;
            public int PositionY;
            public int DisplayOrientation;
            public int DisplayFixedOutput;
            public short Color;
            public short Duplex;
            public short YResolution;
            public short TTOption;
            public short Collate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = FormNameLength)]
            public string FormName;
            public short LogPixels;
            public int BitsPerPel;
            public int PelsWidth;
            public int PelsHeight;
            public int DisplayFlags;
            public int DisplayFrequency;
            public int IcmMethod;
            public int IcmIntent;
            public int MediaType;
            public int DitherType;
            public int Reserved1;
            public int Reserved2;
            public int PanningWidth;
            public int PanningHeight;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DevMode devMode);

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint period);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint period);

        public static int HighestRefreshHz(Screen screen)
        {
            if (screen == null) return Timing.DefaultRefreshHz;

            int width;
            int height;
            int currentHz;
            DevMode current = NewDevMode();
            if (!EnumDisplaySettings(screen.DeviceName, EnumCurrentSettings, ref current))
                return Timing.DefaultRefreshHz;
            width = current.PelsWidth;
            height = current.PelsHeight;
            currentHz = Sanitize(current.DisplayFrequency);

            int highest = currentHz;
            for (int mode = ModeIndexStart; ; mode++)
            {
                DevMode candidate = NewDevMode();
                if (!EnumDisplaySettings(screen.DeviceName, mode, ref candidate)) break;
                if (candidate.PelsWidth != width || candidate.PelsHeight != height) continue;
                if ((candidate.DisplayFlags & DisplayFlagInterlaced) != 0) continue;
                int hz = Sanitize(candidate.DisplayFrequency);
                if (hz > highest) highest = hz;
            }

            if (highest < Timing.MinimumRefreshHz) highest = Timing.DefaultRefreshHz;
            if (highest > Timing.MaximumRefreshHz) highest = Timing.MaximumRefreshHz;
            return highest;
        }

        public static int FrameIntervalMilliseconds(int refreshHz)
        {
            if (refreshHz < Timing.MinimumRefreshHz) refreshHz = Timing.DefaultRefreshHz;
            int interval = Timing.MillisecondsPerSecond / refreshHz;
            return Math.Max(Timing.MinimumTimerIntervalMilliseconds, interval);
        }

        public static bool BeginHighResolutionTimer()
        {
            return TimeBeginPeriod(Timing.TimerResolutionMilliseconds) == 0;
        }

        public static void EndHighResolutionTimer()
        {
            TimeEndPeriod(Timing.TimerResolutionMilliseconds);
        }

        private static DevMode NewDevMode()
        {
            DevMode mode = new DevMode();
            mode.Size = (short)Marshal.SizeOf(typeof(DevMode));
            return mode;
        }

        private static int Sanitize(int hz)
        {
            if (hz <= UnknownRefreshHzThreshold) return 0;
            if (hz > Timing.MaximumRefreshHz) return Timing.MaximumRefreshHz;
            return hz;
        }
    }
}
