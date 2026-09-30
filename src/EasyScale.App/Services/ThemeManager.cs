using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace EasyScale.App.Services;

/// <summary>
/// 主题管理：把持久化的字符串（"system"/"light"/"dark"）映射到 WPF-UI 的主题枚举。
/// 映射集中在此处，避免 UI 层与配置层各写一套字符串。
/// </summary>
public static class ThemeManager
{
    /// <summary>按设置应用主题。<paramref name="themeSetting"/> 为 <see cref="AppSettings"/> 的常量之一。</summary>
    public static void Apply(string themeSetting)
    {
        ApplicationTheme theme = themeSetting switch
        {
            AppSettings.ThemeLight => ApplicationTheme.Light,
            AppSettings.ThemeDark => ApplicationTheme.Dark,
            _ => ResolveSystemTheme(),
        };

        ApplicationThemeManager.Apply(theme, WindowBackdropType.Mica, updateAccent: true);
    }

    private static ApplicationTheme ResolveSystemTheme()
    {
        return ApplicationThemeManager.GetSystemTheme() switch
        {
            SystemTheme.Dark => ApplicationTheme.Dark,
            SystemTheme.HCBlack or SystemTheme.HCWhite => ApplicationTheme.HighContrast,
            _ => ApplicationTheme.Light,
        };
    }
}
