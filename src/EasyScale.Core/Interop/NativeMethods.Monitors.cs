
using System.Runtime.InteropServices;

namespace EasyScale.Core.Interop;

internal static partial class NativeMethods
{
    internal const uint MonitorInfofPrimary = 0x00000001;

    internal const uint GetSourceName = 1;

    internal delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref Rect rect, IntPtr data);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
    internal static extern int GetSourceDeviceName(IntPtr deviceName);

    [DllImport("user32.dll")]
    internal static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc proc, IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx info);
}
