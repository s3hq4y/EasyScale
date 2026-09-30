namespace EasyScale.Core.Dpi;

/// <summary>
/// DPI 缩放的语义常量。集中定义，避免在读取、换算、UI 三处各写一套。
/// </summary>
internal static class DpiScaleConstants
{
    /// <summary>相邻档位之间的百分比步长。</summary>
    internal const int StepPercent = 25;

    /// <summary>100% 缩放对应 96 DPI，这是 Windows 的基准。</summary>
    internal const int BaselineDpi = 96;
}
