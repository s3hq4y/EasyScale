using System.Diagnostics;
using Microsoft.Win32;

namespace EasyScale.App.Services;

/// <summary>
/// 开机自启管理：写 HKCU\...\Run，无需管理员权限。
/// 值名固定，值为可执行文件的完整路径。
/// </summary>
public sealed class AutoStartManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "EasyScale";

    private readonly string _executablePath;

    public AutoStartManager(string? executablePath = null)
    {
        _executablePath = executablePath
            ?? Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot resolve current executable path.");
    }

    /// <summary>查询当前是否已启用自启。</summary>
    public bool IsEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is string value
            && string.Equals(value.Trim('"'), _executablePath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>启用或禁用自启。</summary>
    public void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException($"Cannot open registry key: {RunKeyPath}");

        if (enabled)
        {
            key.SetValue(ValueName, $"\"{_executablePath}\"", RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }

        Debug.WriteLine($"AutoStart set to {enabled} for {_executablePath}");
    }
}
