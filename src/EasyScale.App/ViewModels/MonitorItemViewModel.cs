using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using EasyScale.App.Localization;
using EasyScale.Core.Dpi;
using EasyScale.Core.Models;

namespace EasyScale.App.ViewModels;

/// <summary>
/// 单台显示器的视图模型：展示友好名、可选档位，并在选择变化时立即应用缩放。
/// </summary>
public sealed partial class MonitorItemViewModel : ObservableObject
{
    private readonly IDpiScaleApplier _applier;

    /// <summary>为 true 时忽略 <see cref="SelectedOption"/> 的变化，避免初始化/刷新时回写。</summary>
    private bool _suppressApply;

    public MonitorItemViewModel(IDpiScaleApplier applier, MonitorScaleInfo info)
    {
        _applier = applier;
        DevicePath = info.DevicePath;
        FriendlyName = info.FriendlyName;
        Options = info.Options;

        _suppressApply = true;
        SelectedOption = Options.FirstOrDefault(o => o.Rel == info.CurrentRel) ?? Options[0];
        _suppressApply = false;
    }

    public string DevicePath { get; }

    public string FriendlyName { get; }

    public IReadOnlyList<DpiScaleOption> Options { get; }

    /// <summary>当前选中档位；变更时立即应用缩放。</summary>
    [ObservableProperty]
    private DpiScaleOption _selectedOption;

    partial void OnSelectedOptionChanged(DpiScaleOption value)
    {
        if (_suppressApply)
        {
            return;
        }

        try
        {
            _applier.SetScale(DevicePath, value.Rel);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(LocalizationManager.Instance.Get("Error_SetScaleFailed"), ex.Message),
                LocalizationManager.Instance.Get("Error_Title"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
