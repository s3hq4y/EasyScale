using EasyScale.App.ViewModels;
using Wpf.Ui.Controls;

namespace EasyScale.App.Views;

/// <summary>设置窗口：语言、主题、自启与预设管理。</summary>
public partial class SettingsWindow : FluentWindow
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
