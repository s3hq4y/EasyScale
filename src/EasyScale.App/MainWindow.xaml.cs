using System.ComponentModel;
using System.Windows;
using EasyScale.App.Localization;
using EasyScale.App.ViewModels;
using Wpf.Ui.Controls;

// Wpf.Ui.Controls 与 System.Windows.Controls 存在同名类型，显式别名消歧。
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using Separator = System.Windows.Controls.Separator;

namespace EasyScale.App;

/// <summary>
/// 主窗口：展示显示器与档位；同时承载托盘图标。
/// 关闭时默认最小化到托盘而非退出。
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;

    /// <summary>为 true 时才真正关闭（由 <see cref="RequestExit"/> 设置）。</summary>
    private bool _allowClose;

    /// <summary>请求退出应用（托盘「退出」调用）。</summary>
    public event EventHandler? ExitRequested;

    /// <summary>请求打开设置窗口（托盘「设置」调用）。</summary>
    public event EventHandler? SettingsRequested;

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        BuildTrayMenu();
    }

    /// <summary>允许本次关闭真正退出。</summary>
    public void RequestExit()
    {
        _allowClose = true;
        ExitRequested?.Invoke(this, EventArgs.Empty);
        Close();
    }

    /// <summary>从托盘唤起主窗口。</summary>
    public void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose)
        {
            // 关窗 = 最小化到托盘，保持进程与托盘图标存活。
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }

        /// <summary>主窗口「设置」按钮：转发给 <see cref="SettingsRequested"/>。</summary>
    private void OnSettingsClick(object sender, RoutedEventArgs e)
        => SettingsRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// 构建托盘右键菜单。菜单文本在打开时按当前语言生成，
    /// 因此语言切换后菜单也会跟随。
    /// </summary>
    private void BuildTrayMenu()
    {
        var menu = new ContextMenu();
        menu.Opened += (_, _) => PopulateTrayMenu(menu);
        TrayIcon.Menu = menu;
    }

    private void PopulateTrayMenu(ContextMenu menu)
    {
        LocalizationManager loc = LocalizationManager.Instance;
        menu.Items.Clear();

        menu.Items.Add(CreateMenuItem(loc.Get("Tray_Show"), (_, _) => ShowFromTray()));

        // 预设子菜单：来自主视图模型，点击即应用。
        if (_viewModel.Presets.Count > 0)
        {
            var presetsItem = new MenuItem { Header = loc.Get("Tray_Presets") };
            foreach (var preset in _viewModel.Presets)
            {
                var item = CreateMenuItem(preset.Name, (_, _) => _viewModel.ApplyPreset(preset));
                presetsItem.Items.Add(item);
            }
            menu.Items.Add(presetsItem);
        }

        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem(loc.Get("Tray_Settings"), (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty)));
        menu.Items.Add(CreateMenuItem(loc.Get("Tray_Exit"), (_, _) => RequestExit()));
    }

    private static MenuItem CreateMenuItem(string header, RoutedEventHandler onClick)
    {
        var item = new MenuItem { Header = header };
        item.Click += onClick;
        return item;
    }
}