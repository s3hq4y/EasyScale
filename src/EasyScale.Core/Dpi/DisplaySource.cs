namespace EasyScale.Core.Dpi;

using EasyScale.Core.Interop;

/// <summary>定位一个显示源所需的最小信息。</summary>
internal readonly record struct DisplaySource(Luid AdapterId, uint Id);
