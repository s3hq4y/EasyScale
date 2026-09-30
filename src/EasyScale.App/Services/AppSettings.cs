using EasyScale.App.Models;

namespace EasyScale.App.Services;

/// <summary>用户可配置项的持久化模型。存于程序同目录的 settings.json。</summary>
public sealed class AppSettings
{
    /// <summary>语言：null/空 = 跟随系统；否则为 CultureInfo 名称，如 "zh-CN"、"en"。</summary>
    public string? Language { get; set; }

    /// <summary>主题："system" | "light" | "dark"。</summary>
    public string Theme { get; set; } = ThemeSystem;

    /// <summary>是否随 Windows 启动。</summary>
    public bool AutoStart { get; set; }

    /// <summary>命名预设列表。</summary>
    public List<ScalePreset> Presets { get; set; } = new();

    public const string ThemeSystem = "system";
    public const string ThemeLight = "light";
    public const string ThemeDark = "dark";
}
