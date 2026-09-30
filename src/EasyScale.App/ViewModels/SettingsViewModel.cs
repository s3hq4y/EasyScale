using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyScale.App.Localization;
using EasyScale.App.Models;
using EasyScale.App.Services;

namespace EasyScale.App.ViewModels;

/// <summary>
/// 设置窗口视图模型：语言、主题、开机自启与命名预设的管理。
/// 所有变更立即持久化到 <see cref="SettingsStore"/>。
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
        private readonly AppSettings _settings;
    private readonly SettingsStore _store;
    private readonly AutoStartManager _autoStart;
    private readonly MainViewModel _main;

    public SettingsViewModel(AppSettings settings, SettingsStore store, AutoStartManager autoStart, MainViewModel main)
    {
        _settings = settings;
        _store = store;
        _autoStart = autoStart;
        _main = main;

                Presets = new ObservableCollection<ScalePreset>(settings.Presets);
                SelectedLanguage = Languages.FirstOrDefault(l => l.CultureName == settings.Language) ?? Languages[0];
                SelectedTheme = Themes.FirstOrDefault(t => t.Value == settings.Theme) ?? Themes[0];
                AutoStartEnabled = _autoStart.IsEnabled();
    }

    /// <summary>可选语言：null 表示跟随系统。</summary>
    public IReadOnlyList<LanguageOption> Languages { get; } = new[]
    {
        new LanguageOption(null),
        new LanguageOption("zh-CN"),
        new LanguageOption("en"),
    };

        /// <summary>可选主题：显示名本地化，值与 <see cref="AppSettings"/> 的主题常量对应。</summary>
    public IReadOnlyList<ThemeOption> Themes { get; } = new[]
    {
        new ThemeOption(AppSettings.ThemeSystem),
        new ThemeOption(AppSettings.ThemeLight),
        new ThemeOption(AppSettings.ThemeDark),
    };

    public ObservableCollection<ScalePreset> Presets { get; }

    [ObservableProperty]
    private LanguageOption? _selectedLanguage;

        [ObservableProperty]
    private ThemeOption _selectedTheme;

    [ObservableProperty]
    private bool _autoStartEnabled;

    [ObservableProperty]
    private ScalePreset? _selectedPreset;

    [ObservableProperty]
    private string _newPresetName = string.Empty;

    partial void OnSelectedLanguageChanged(LanguageOption? value)
    {
        CultureInfo? culture = value?.CultureName is { Length: > 0 } name
            ? CultureInfo.GetCultureInfo(name)
            : null;

        LocalizationManager.Instance.SetCulture(culture);
        _settings.Language = value?.CultureName;
        _store.Save(_settings);
    }

        partial void OnSelectedThemeChanged(ThemeOption value)
    {
        ThemeManager.Apply(value.Value);
        _settings.Theme = value.Value;
        _store.Save(_settings);
    }

    partial void OnAutoStartEnabledChanged(bool value)
    {
        _autoStart.SetEnabled(value);
        _settings.AutoStart = value;
        _store.Save(_settings);
    }

    /// <summary>把指定预设写回设置（供添加/删除后调用）。</summary>
    public void CommitPresets()
    {
        _settings.Presets = Presets.ToList();
        _store.Save(_settings);
    }

        [RelayCommand]
    private void DeletePreset()
    {
        if (SelectedPreset is null)
        {
            return;
        }

        Presets.Remove(SelectedPreset);
        CommitPresets();
    }

    /// <summary>
    /// 把主窗口当前选中的显示器与档位存为一个命名预设。
    /// 未填名称时用本地化的默认名。
    /// </summary>
    [RelayCommand]
    private void AddPreset()
    {
        MonitorItemViewModel? monitor = _main.SelectedMonitor;
        if (monitor is null)
        {
            return;
        }

        string name = string.IsNullOrWhiteSpace(NewPresetName)
            ? LocalizationManager.Instance.Get("Preset_NewName")
            : NewPresetName.Trim();

        Presets.Add(new ScalePreset
        {
            Name = name,
            DevicePath = monitor.DevicePath,
            FriendlyName = monitor.FriendlyName,
            Rel = monitor.SelectedOption.Rel,
        });

        CommitPresets();
        NewPresetName = string.Empty;
    }

        /// <summary>主题下拉项：显示名本地化 + 持久化值。</summary>
    public sealed record ThemeOption(string Value)
    {
        /// <summary>界面显示的标签，延迟到 <see cref="LocalizationManager"/> 取，以支持运行时切换。</summary>
        public string DisplayName => Value switch
        {
            AppSettings.ThemeLight => LocalizationManager.Instance.Get("Settings_ThemeLight"),
            AppSettings.ThemeDark => LocalizationManager.Instance.Get("Settings_ThemeDark"),
            _ => LocalizationManager.Instance.Get("Settings_ThemeSystem"),
        };
    }

    /// <summary>语言下拉项：显示名 + 可选文化名。</summary>
    public sealed record LanguageOption(string? CultureName)
    {
        /// <summary>界面显示的标签，延迟到 <see cref="LocalizationManager"/> 取，以支持运行时切换。</summary>
        public string DisplayName => CultureName switch
        {
            null => LocalizationManager.Instance.Get("Settings_LanguageSystem"),
            "zh-CN" => "中文（简体）",
            "en" => "English",
            _ => CultureName,
        };
    }
}
