namespace EasyScale.Core.Models;

/// <summary>
/// 一个可选的缩放档位。
/// <para><see cref="Rel"/> 是相对「推荐档」的偏移（可为负）；</para>
/// <para><see cref="Percent"/> 是界面展示的实际百分比。</para>
/// </summary>
public sealed record DpiScaleOption(int Rel, int Percent)
{
    /// <summary>是否为系统推荐档（rel == 0）。</summary>
    public bool IsRecommended => Rel == 0;
}
