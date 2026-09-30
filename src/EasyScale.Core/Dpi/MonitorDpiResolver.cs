using System.Runtime.InteropServices;
using EasyScale.Core.Interop;

namespace EasyScale.Core.Dpi;

/// <summary>
/// 把 DisplayConfig 的「源」关联到 HMONITOR，从而读取该显示器当前的实际 DPI。
/// 关联依据是 GDI 设备名（例如 \\.\DISPLAY1），它同时出现在源名称与 MONITORINFOEX 中。
/// </summary>
internal static class MonitorDpiResolver
{
    /// <summary>返回当前有效 DPI；无法关联时返回 null。</summary>
    internal static uint? TryGetEffectiveDpi(uint sourceId, Luid adapterId)
    {
        string? gdiName = TryGetSourceGdiName(sourceId, adapterId);
        if (gdiName is null)
        {
            return null;
        }

        return TryGetDpiByGdiName(gdiName);
    }

    private static string? TryGetSourceGdiName(uint sourceId, Luid adapterId)
    {
        // 用原始缓冲区按已知偏移读取：实测「按偏移 Marshal」可靠，
        // 而直接读取结构体 fixed buffer 在 .NET 上会截断字符串。
        IntPtr buffer = Marshal.AllocHGlobal(DisplayConfigLayout.SourceDeviceNameSize);
        try
        {
            DisplayConfigLayout.ZeroMemory(buffer, DisplayConfigLayout.SourceDeviceNameSize);
            DisplayConfigLayout.WriteHeader(
                buffer,
                (int)NativeMethods.GetSourceName,
                DisplayConfigLayout.SourceDeviceNameSize,
                adapterId,
                sourceId);

            int code = NativeMethods.GetSourceDeviceName(buffer);
            if (code != Win32Error.Success)
            {
                return null;
            }

            return DisplayConfigLayout.ReadString(buffer, DisplayConfigLayout.SourceGdiNameOffset);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static uint? TryGetDpiByGdiName(string gdiName)
    {
        uint? result = null;

        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
            (IntPtr hMonitor, IntPtr _, ref Rect _, IntPtr _) =>
            {
                var info = new MonitorInfoEx();
                info.Size = (uint)Marshal.SizeOf<MonitorInfoEx>();
                if (!NativeMethods.GetMonitorInfo(hMonitor, ref info))
                {
                    return true;
                }

                if (string.Equals(info.DeviceName, gdiName, StringComparison.OrdinalIgnoreCase))
                {
                    if (NativeMethods.GetDpiForMonitor(
                            hMonitor, NativeMethods.MdtEffectiveDpi, out uint dpiX, out _) == Win32Error.Success)
                    {
                        result = dpiX;
                    }
                    return false;
                }
                return true;
            }, IntPtr.Zero);

        return result;
    }
}
