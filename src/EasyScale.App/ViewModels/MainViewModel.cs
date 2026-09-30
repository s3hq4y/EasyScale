using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyScale.App.Localization;
using EasyScale.App.Models;
using EasyScale.App.Services;
using EasyScale.Core.Dpi;

namespace EasyScale.App.ViewModels;

/// <summary>
/// 主窗口视图模型：列出活动显示器，选择档位后立即应用；
/// 同时暴露命名预设，供主窗口与托盘菜单使用。
/// 依赖 <see cref="IDpiScaleApplier"/> 抽象，不直接触碰原生层。
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IDpiScaleApplier _applier;
    private readonly AppSettings _settings;

    public MainViewModel(IDpiScaleApplier applier, AppSettings settings)
    {
        _applier = applier;
        _settings = settings;
        ReloadPresets();
        Refresh();
    }

    /// <summary>当前所有活动显示器。</summary>
    public ObservableCollection<MonitorItemViewModel> Monitors { get; } = new();

    /// <summary>命名预设（与设置窗口保持同步）。</summary>
    public ObservableCollection<ScalePreset> Presets { get; } = new();

    [ObservableProperty]
    private MonitorItemViewModel? _selectedMonitor;

    /// <summary>是否检测到显示器（用于切换空状态提示）。</summary>
    public bool HasMonitors => Monitors.Count > 0;

    /// <summary>重新枚举显示器并重建列表。</summary>
    [RelayCommand]
    public void Refresh()
    {
        string? previousPath = SelectedMonitor?.DevicePath;
        Monitors.Clear();

        try
        {
            foreach (var info in _applier.GetMonitors())
            {
                Monitors.Add(new MonitorItemViewModel(_applier, info));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(LocalizationManager.Instance.Get("Error_LoadMonitorsFailed"), ex.Message),
                LocalizationManager.Instance.Get("Error_Title"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

                SelectedMonitor = Monitors.FirstOrDefault(m => m.DevicePath == previousPath)
            ?? Monitors.FirstOrDefault();
        OnPropertyChanged(nameof(HasMonitors));
    }

    /// <summary>从设置重新载入预设（设置窗口改动后调用）。</summary>
    public void ReloadPresets()
    {
        Presets.Clear();
        foreach (ScalePreset preset in _settings.Presets)
        {
            Presets.Add(preset);
        }
    }

    /// <summary>
    /// 应用一个预设：把其记录的档位套用到对应显示器。
    /// 显示器不在或档位不受支持时静默忽略，避免打扰。
    /// </summary>
    public void ApplyPreset(ScalePreset preset)
    {
        MonitorItemViewModel? monitor = Monitors.FirstOrDefault(
            m => string.Equals(m.DevicePath, preset.DevicePath, StringComparison.OrdinalIgnoreCase));
        if (monitor is null)
        {
            return;
        }

        var option = monitor.Options.FirstOrDefault(o => o.Rel == preset.Rel);
        if (option is not null)
        {
            monitor.SelectedOption = option;
        }
    }
}
