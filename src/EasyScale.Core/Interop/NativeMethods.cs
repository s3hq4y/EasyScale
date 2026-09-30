using System.Runtime.InteropServices;

namespace EasyScale.Core.Interop;

/// <summary>user32 / shcore 的原生入口声明。</summary>
internal static partial class NativeMethods
{
    /// <summary>QDC_ONLY_ACTIVE_PATHS：只返回当前活动的显示路径。</summary>
    internal const uint QdcOnlyActivePaths = 0x00000002;

    internal const uint MonitorDefaultToNearest = 0x00000002;

    /// <summary>MDT_EFFECTIVE_DPI：取有效 DPI（含用户缩放）。</summary>
    internal const int MdtEffectiveDpi = 0;

    // DisplayConfigGetDeviceInfo 的 info type。
    // 正值是公开 API；负值的 DPI Scale 是未公开 API，靠实测确认，见 docs/可行性评估.md。
    internal const uint GetTargetName = 2;
    internal const int GetDpiScaleType = -3;
    internal const int SetDpiScaleType = -4;

    [DllImport("user32.dll")]
    internal static extern int GetDisplayConfigBufferSizes(
        uint flags,
        out uint numPathArrayElements,
        out uint numModeInfoArrayElements);

    [DllImport("user32.dll")]
    internal static extern int QueryDisplayConfig(
        uint flags,
        ref uint numPathArrayElements,
        [Out] DisplayConfigPathInfo[] pathInfoArray,
        ref uint numModeInfoArrayElements,
        [Out] DisplayConfigModeInfo[] modeInfoArray,
        IntPtr currentTopologyId);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
    internal static extern int GetTargetDeviceName(IntPtr deviceName);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
    internal static extern int GetDpiScale(ref DisplayConfigDpiScaleInfo dpiScale);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigSetDeviceInfo")]
    internal static extern int SetDpiScale(ref DisplayConfigSetDpiScaleInfo dpiScale);

    [DllImport("user32.dll")]
    internal static extern IntPtr MonitorFromPoint(Point pt, uint flags);

    [DllImport("shcore.dll")]
    internal static extern int GetDpiForMonitor(
        IntPtr monitor,
        int dpiType,
        out uint dpiX,
        out uint dpiY);
}
