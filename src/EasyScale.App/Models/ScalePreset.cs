namespace EasyScale.App.Models;

/// <summary>
/// 一个命名缩放预设：把某台显示器的某个档位记下来，便于一键切换。
/// <para><see cref="DevicePath"/> 为稳定标识；显示器不在时该预设不可用。</para>
/// </summary>
public sealed class ScalePreset
{
    /// <summary>用户可见的预设名。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>目标显示器设备路径（来自 DisplayConfig）。</summary>
    public string DevicePath { get; set; } = string.Empty;

    /// <summary>记录时的友好名，用于在显示器缺席时仍能给出可读提示。</summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>缩放档位偏移（rel）。</summary>
    public int Rel { get; set; }
}
