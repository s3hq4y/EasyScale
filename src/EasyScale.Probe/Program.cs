using System.Runtime.InteropServices;
using EasyScale.Core.Dpi;
using EasyScale.Core.Models;

// 验证 Core 的 SetScale 写路径：
//   列出显示器 -> 记录当前档位 -> 切到相邻档 -> 读回校验 -> 还原。
// 这是 Core 层完成前的最后一项实测，对应 docs/可行性评估.md 的「进行中」。

// 控制台进程默认 DPI 不感知，GetDpiForMonitor 会返回基准值而非每显示器实际缩放。
// 显式声明 PerMonitorV2，才能观察到 SET_DPI_SCALE 的效果。
ProbeNative.TryEnablePerMonitorV2();

var applier = new DisplayConfigDpiScaleApplier();

Console.WriteLine("=== 活动显示器 ===");
IReadOnlyList<MonitorScaleInfo> monitors = applier.GetMonitors();
if (monitors.Count == 0)
{
    Console.WriteLine("未找到活动显示器。");
    return 1;
}

foreach (MonitorScaleInfo m in monitors)
{
    Console.WriteLine($"  {m.FriendlyName}  ({m.DevicePath})");
    Console.WriteLine($"    当前档位 rel={m.CurrentRel}  ({m.Current.Percent}%)  推荐={m.RecommendedPercent}%");
    Console.WriteLine($"    可选: {string.Join(", ", m.Options.Select(o => $"{o.Percent}%{(o.IsRecommended ? "(推荐)" : string.Empty)}"))}");
}

MonitorScaleInfo target = monitors[0];
int original = target.CurrentRel;
int probe = original == target.Options[0].Rel ? target.Options[^1].Rel : target.Options[0].Rel;

Console.WriteLine();
Console.WriteLine("=== 交叉验证: 档位 rel 与系统实测 DPI 的对应 ===");
foreach (DpiScaleOption o in target.Options)
{
    applier.SetScale(target.DevicePath, o.Rel);
    Thread.Sleep(500);
    uint? dpi = ProbeNative.GetPrimaryDpi();
    if (dpi is null)
    {
        Console.WriteLine($"  rel={o.Rel}  期望={o.Percent}%  实测=(读取失败)  ?");
        continue;
    }

    int measured = (int)Math.Round(dpi.Value * 100.0 / 96);
    string mark = measured == o.Percent ? "OK" : "!! 不符";
    Console.WriteLine($"  rel={o.Rel}  期望={o.Percent}%  实测={measured}%  {mark}");
}

// 交叉验证会改动缩放，先还原，再做往返一致性测试。
applier.SetScale(target.DevicePath, original);
Thread.Sleep(500);

Console.WriteLine();
Console.WriteLine($"=== 往返测试: rel {original} -> {probe} -> {original} ===");

applier.SetScale(target.DevicePath, probe);
Thread.Sleep(800);
int afterSet = ReloadFirst(applier);
Console.WriteLine($"  写入 {probe} 后读回: rel={afterSet}  (期望 {probe})");

bool forward = afterSet == probe;
if (!forward)
{
    Console.WriteLine("  !! 写路径未生效，跳过还原以避免破坏用户当前设置");
    return 2;
}

applier.SetScale(target.DevicePath, original);
Thread.Sleep(800);
int afterRestore = ReloadFirst(applier);
Console.WriteLine($"  还原 {original} 后读回: rel={afterRestore}  (期望 {original})");

if (afterRestore != original)
{
    Console.WriteLine("  !! 还原失败，请手动检查系统缩放设置");
    return 3;
}

Console.WriteLine();
Console.WriteLine("OK: SetScale 写路径可用，往返一致。");
return 0;

// 重新读取第一台显示器的当前档位。
static int ReloadFirst(IDpiScaleApplier applier) => applier.GetMonitors()[0].CurrentRel;

/// <summary>
/// 仅供 Probe 交叉验证使用的原生入口。
/// 读主显示器「有效 DPI」以独立确认 Core 推导出的档位百分比。
/// </summary>
internal static class ProbeNative
{
    private const int MdtEffectiveDpi = 0;
    private const uint MonitorDefaultToNearest = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

        // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2
    private static readonly IntPtr PerMonitorAwareV2 = new(-4);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(Point pt, uint flags);

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    internal static void TryEnablePerMonitorV2() => SetProcessDpiAwarenessContext(PerMonitorAwareV2);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint dpiX, out uint dpiY);

    internal static uint? GetPrimaryDpi()
    {
        IntPtr monitor = MonitorFromPoint(new Point { X = 0, Y = 0 }, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return null;
        }

        return GetDpiForMonitor(monitor, MdtEffectiveDpi, out uint dpiX, out _) == 0 ? dpiX : null;
    }
}

