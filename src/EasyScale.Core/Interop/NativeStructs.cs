using System.Runtime.InteropServices;

namespace EasyScale.Core.Interop;

// 本文件中的结构体布局与字段偏移，均在本机（Windows NT 10.0.28000 / 26H1）
// 实测校验通过，详见 docs/可行性评估.md。若未来 Windows 调整布局，
// QueryDisplayConfig 的返回尺寸会最先暴露不一致。

/// <summary>本地唯一标识符（适配器标识）。</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct Luid
{
    public uint LowPart;
    public int HighPart;
}

/// <summary>点坐标，用于 MonitorFromPoint。</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct Point
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigRational
{
    public uint Numerator;
    public uint Denominator;
}

/// <summary>显示路径的「源」信息。DPI 缩放归属此对象。</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigPathSourceInfo
{
    public Luid AdapterId;
    public uint Id;
    public uint ModeInfoIndex;
    public uint StatusFlags;
}

/// <summary>显示路径的「目标」信息（物理输出）。显示器名称归属此对象。</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigPathTargetInfo
{
    public Luid AdapterId;
    public uint Id;
    public uint ModeInfoIndex;
    public uint OutputTechnology;
    public uint Rotation;
    public uint Scaling;
    public DisplayConfigRational RefreshRate;
    public uint ScanLineOrdering;
    public int TargetAvailable;
    public uint StatusFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigPathInfo
{
    public DisplayConfigPathSourceInfo SourceInfo;
    public DisplayConfigPathTargetInfo TargetInfo;
    public uint Flags;
}

/// <summary>模式信息。此处只需占用正确大小，内容不使用。</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigModeInfo
{
    public uint InfoType;
    public uint Id;
    public Luid AdapterId;
    public ulong Union0;
    public ulong Union1;
    public ulong Union2;
    public ulong Union3;
    public ulong Union4;
    public ulong Union5;
}

/// <summary>所有 DeviceInfo 查询/设置的公共头部。Size 必须精确等于结构体实际大小。</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigDeviceInfoHeader
{
    public uint Type;
    public uint Size;
    public Luid AdapterId;
    public uint Id;
}

/// <summary>GET_DPI_SCALE 的返回体。</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigDpiScaleInfo
{
    public DisplayConfigDeviceInfoHeader Header;
    public int MinScaleRel;
    public int CurScaleRel;
    public int MaxScaleRel;
}

/// <summary>SET_DPI_SCALE 的请求体。</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigSetDpiScaleInfo
{
    public DisplayConfigDeviceInfoHeader Header;
    public int DesiredScaleRel;
}

/// <summary>GET_TARGET_NAME 的返回体，含显示器友好名与设备路径。</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DisplayConfigTargetDeviceName
{
    public DisplayConfigDeviceInfoHeader Header;
    public uint Flags;
    public uint OutputTechnology;
    public ushort EdidManufactureId;
    public ushort EdidProductCodeId;
    public uint ConnectorInstance;
    public fixed char FriendlyName[64];
    public fixed char DevicePath[128];
}

/// <summary>GET_SOURCE_NAME 的返回体：形如 \\.\DISPLAY1 的 GDI 设备名。</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DisplayConfigSourceDeviceName
{
    public DisplayConfigDeviceInfoHeader Header;
    public fixed char ViewGdiDeviceName[32];
}

/// <summary>MONITORINFOEX：含显示器矩形与 GDI 设备名。</summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct MonitorInfoEx
{
    public uint Size;
    public Rect Monitor;
    public Rect WorkArea;
    public uint Flags;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string DeviceName;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Rect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}
