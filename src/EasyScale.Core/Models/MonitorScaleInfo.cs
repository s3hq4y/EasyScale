using EasyScale.Core.Models;

namespace EasyScale.Core.Models;

/// <summary>一台活动显示器及其缩放档位。</summary>
public sealed class MonitorScaleInfo
{
    /// <summary>稳定标识，来自 DisplayConfig 的目标设备路径。</summary>
    public required string DevicePath { get; init; }

    /// <summary>显示器友好名，例如 "H27S12"。</summary>
    public required string FriendlyName { get; init; }

    /// <summary>当前档位偏移。</summary>
    public required int CurrentRel { get; init; }

    /// <summary>推荐档（rel == 0）对应的百分比。</summary>
    public required int RecommendedPercent { get; init; }

    /// <summary>该显示器真实支持的档位，由 [min, max] 推导。</summary>
    public required IReadOnlyList<DpiScaleOption> Options { get; init; }

    /// <summary>当前选中的档位。</summary>
    public DpiScaleOption Current =>
        Options.FirstOrDefault(o => o.Rel == CurrentRel) ?? Options[0];
}
