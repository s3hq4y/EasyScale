using System.Runtime.InteropServices;

namespace EasyScale.Core.Interop;

/// <summary>
/// DISPLAYCONFIG 结构体的尺寸与字段偏移，集中定义以避免散落的魔法数字。
/// 偏移已在本机（Windows 26H1）实测校验，见 docs/可行性评估.md。
/// </summary>
internal static class DisplayConfigLayout
{
    /// <summary>所有 DeviceInfo 结构的公共头部字节数。</summary>
    internal const int DeviceInfoHeaderSize = 20;

    /// <summary>DISPLAYCONFIG_TARGET_DEVICE_NAME 中友好名的字节偏移。</summary>
    internal const int TargetFriendlyNameOffset = 36;

    /// <summary>DISPLAYCONFIG_TARGET_DEVICE_NAME 中设备路径的字节偏移。</summary>
    internal const int TargetDevicePathOffset = 164;

    /// <summary>DISPLAYCONFIG_TARGET_DEVICE_NAME 的总字节数。</summary>
    internal const int TargetDeviceNameSize = 420;

    /// <summary>DISPLAYCONFIG_SOURCE_DEVICE_NAME 中 GDI 设备名的字节偏移。</summary>
    internal const int SourceGdiNameOffset = 20;

    /// <summary>DISPLAYCONFIG_SOURCE_DEVICE_NAME 的总字节数。</summary>
    internal const int SourceDeviceNameSize = 84;

    /// <summary>写入 DeviceInfo 的公共头部。</summary>
    internal static void WriteHeader(IntPtr buffer, int type, int size, Luid adapterId, uint id)
    {
        Marshal.WriteInt32(buffer, 0, type);
        Marshal.WriteInt32(buffer, 4, size);
        Marshal.WriteInt32(buffer, 8, unchecked((int)adapterId.LowPart));
        Marshal.WriteInt32(buffer, 12, adapterId.HighPart);
        Marshal.WriteInt32(buffer, 16, unchecked((int)id));
    }

    /// <summary>在指定字节偏移处读取以 null 结尾的 UTF-16 字符串。</summary>
    internal static string? ReadString(IntPtr buffer, int byteOffset)
    {
        string? value = Marshal.PtrToStringUni(IntPtr.Add(buffer, byteOffset));
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>按 4 字节步长清零缓冲区（本文件涉及的尺寸均为 4 的倍数）。</summary>
    internal static void ZeroMemory(IntPtr buffer, int size)
    {
        for (int offset = 0; offset < size; offset += sizeof(int))
        {
            Marshal.WriteInt32(buffer, offset, 0);
        }
    }
}
