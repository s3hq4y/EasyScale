using System.Runtime.InteropServices;
using EasyScale.Core.Interop;
using EasyScale.Core.Models;

namespace EasyScale.Core.Dpi;

/// <summary>
/// 默认实现：通过未公开的 DisplayConfig DPI Scale 接口（type -3 / -4）读写每显示器缩放。
/// 该接口可立即生效且无需管理员权限，见 docs/可行性评估.md。
/// </summary>
public sealed class DisplayConfigDpiScaleApplier : IDpiScaleApplier
{
    public IReadOnlyList<MonitorScaleInfo> GetMonitors()
    {
        var paths = QueryActivePaths();
        var monitors = new List<MonitorScaleInfo>(paths.Count);

        foreach (var path in paths)
        {
            if (path.TargetInfo.TargetAvailable == 0)
            {
                continue;
            }

            var names = TryGetTargetNames(path);
            string devicePath = names.DevicePath ?? string.Empty;
            string friendlyName = names.FriendlyName ?? devicePath;

            if (!TryGetDpiScale(path.SourceInfo.Id, path.SourceInfo.AdapterId,
                    out int minRel, out int curRel, out int maxRel))
            {
                continue;
            }

            int recommendedPercent = ResolveRecommendedPercent(path, curRel);
            var options = BuildOptions(minRel, maxRel, recommendedPercent);

            monitors.Add(new MonitorScaleInfo
            {
                DevicePath = devicePath,
                FriendlyName = friendlyName,
                CurrentRel = curRel,
                RecommendedPercent = recommendedPercent,
                Options = options,
            });
        }

        return monitors;
    }

    public void SetScale(string devicePath, int rel)
    {
        foreach (var path in QueryActivePaths())
        {
            if (!string.Equals(TryGetTargetNames(path).DevicePath, devicePath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var info = new DisplayConfigSetDpiScaleInfo
            {
                Header = new DisplayConfigDeviceInfoHeader
                {
                    Type = unchecked((uint)NativeMethods.SetDpiScaleType),
                    Size = (uint)Marshal.SizeOf<DisplayConfigSetDpiScaleInfo>(),
                    AdapterId = path.SourceInfo.AdapterId,
                    Id = path.SourceInfo.Id,
                },
                DesiredScaleRel = rel,
            };

            int code = NativeMethods.SetDpiScale(ref info);
            Win32Error.ThrowIfFailed(code, "DisplayConfigSetDeviceInfo(SET_DPI_SCALE)");
            return;
        }

        throw new InvalidOperationException($"Monitor not found: {devicePath}");
    }

    private static List<DisplayConfigPathInfo> QueryActivePaths()
    {
        int code = NativeMethods.GetDisplayConfigBufferSizes(
            NativeMethods.QdcOnlyActivePaths, out uint pathCount, out uint modeCount);
        Win32Error.ThrowIfFailed(code, "GetDisplayConfigBufferSizes");

        var paths = new DisplayConfigPathInfo[pathCount];
        var modes = new DisplayConfigModeInfo[modeCount];

        code = NativeMethods.QueryDisplayConfig(
            NativeMethods.QdcOnlyActivePaths, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
        Win32Error.ThrowIfFailed(code, "QueryDisplayConfig");

        return paths.Take((int)pathCount).ToList();
    }

    /// <summary>由当前实际百分比与当前档位，反推推荐档（rel=0）对应的百分比。</summary>
    private static int ResolveRecommendedPercent(DisplayConfigPathInfo path, int currentRel)
    {
        uint? dpi = MonitorDpiResolver.TryGetEffectiveDpi(path.SourceInfo.Id, path.SourceInfo.AdapterId);
        if (dpi is null)
        {
            // 无法读取实际 DPI 时，退化为「100% + 偏移」，至少保证档位自洽。
            return 100 + currentRel * DpiScaleConstants.StepPercent;
        }

        int currentPercent = (int)Math.Round(dpi.Value * 100.0 / DpiScaleConstants.BaselineDpi);
        return currentPercent - currentRel * DpiScaleConstants.StepPercent;
    }

    private static List<DpiScaleOption> BuildOptions(int minRel, int maxRel, int recommendedPercent)
    {
        var options = new List<DpiScaleOption>(maxRel - minRel + 1);
        for (int rel = minRel; rel <= maxRel; rel++)
        {
            int percent = recommendedPercent + rel * DpiScaleConstants.StepPercent;
            options.Add(new DpiScaleOption(rel, percent));
        }
        return options;
    }

    /// <summary>
    /// 一次调用取得显示器的友好名与设备路径。
    /// 用原始缓冲区按已知偏移读取：实测「按偏移 Marshal」可靠，
    /// 而直接读取结构体 fixed buffer 会截断字符串。
    /// </summary>
    private static TargetNames TryGetTargetNames(DisplayConfigPathInfo path)
    {
        IntPtr buffer = Marshal.AllocHGlobal(DisplayConfigLayout.TargetDeviceNameSize);
        try
        {
            DisplayConfigLayout.ZeroMemory(buffer, DisplayConfigLayout.TargetDeviceNameSize);
            DisplayConfigLayout.WriteHeader(
                buffer,
                (int)NativeMethods.GetTargetName,
                DisplayConfigLayout.TargetDeviceNameSize,
                path.TargetInfo.AdapterId,
                path.TargetInfo.Id);

            if (NativeMethods.GetTargetDeviceName(buffer) != Win32Error.Success)
            {
                return default;
            }

            return new TargetNames(
                DisplayConfigLayout.ReadString(buffer, DisplayConfigLayout.TargetFriendlyNameOffset),
                DisplayConfigLayout.ReadString(buffer, DisplayConfigLayout.TargetDevicePathOffset));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool TryGetDpiScale(uint sourceId, Luid adapterId, out int min, out int cur, out int max)
    {
        min = cur = max = 0;

        var info = new DisplayConfigDpiScaleInfo
        {
            Header = new DisplayConfigDeviceInfoHeader
            {
                Type = unchecked((uint)NativeMethods.GetDpiScaleType),
                Size = (uint)Marshal.SizeOf<DisplayConfigDpiScaleInfo>(),
                AdapterId = adapterId,
                Id = sourceId,
            },
        };

        if (NativeMethods.GetDpiScale(ref info) != Win32Error.Success)
        {
            return false;
        }

        min = info.MinScaleRel;
        cur = info.CurScaleRel;
        max = info.MaxScaleRel;
        return true;
    }
}
