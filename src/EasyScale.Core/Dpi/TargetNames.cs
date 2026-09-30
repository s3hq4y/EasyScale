namespace EasyScale.Core.Dpi;

/// <summary>一次 GET_TARGET_NAME 调用取回的显示目标名称信息。</summary>
internal readonly record struct TargetNames(string? FriendlyName, string? DevicePath);
