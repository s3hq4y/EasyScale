using EasyScale.Core.Models;

namespace EasyScale.Core.Dpi;

/// <summary>
/// 缩放读写策略抽象。
/// 默认实现走未公开的 DisplayConfig DPI Scale 接口；
/// 保留此抽象是为了在该接口随 Windows 版本失效时，可无缝回退到注册表方案，
/// 见 docs/可行性评估.md 的「风险清单」。
/// </summary>
public interface IDpiScaleApplier
{
    /// <summary>枚举当前活动的显示器及其缩放档位。</summary>
    IReadOnlyList<MonitorScaleInfo> GetMonitors();

    /// <summary>按设备路径设置缩放档位（<paramref name="rel"/> 为相对推荐档的偏移）。</summary>
    void SetScale(string devicePath, int rel);
}
