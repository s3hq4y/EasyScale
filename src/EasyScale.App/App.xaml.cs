using System.Threading;
using System.Windows;
using EasyScale.App.Localization;
using EasyScale.App.Services;
using EasyScale.App.ViewModels;
using EasyScale.App.Views;
using EasyScale.Core.Dpi;

namespace EasyScale.App;

/// <summary>
/// 应用入口：负责单实例、组合根（依赖装配）、主题/语言初始化，
/// 以及主窗口与托盘的联动。
/// </summary>
public partial class App : Application
{
    private const string MutexName = "EasyScale.SingleInstance";

        private Mutex? _singleInstanceMutex;

    /// <summary>当前进程是否持有单实例 Mutex（决定 OnExit 是否应释放）。</summary>
    private bool _ownsMutex;

    private AppSettings _settings = new();
    private SettingsStore _store = null!;
    private AutoStartManager _autoStart = null!;
    private MainViewModel _mainViewModel = null!;
    private MainWindow _mainWindow = null!;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 单实例：已有一个在跑则直接退出，把焦点留给已有实例。
                _singleInstanceMutex = new Mutex(initiallyOwned: true, MutexName, out bool isNew);
        _ownsMutex = isNew;
        if (!isNew)
        {
            // 已有实例在运行：直接干净退出（不触碰不归本进程所有的 Mutex）。
            Environment.Exit(0);
        }

        base.OnStartup(e);

        // 关窗只隐藏，因此显式控制退出时机。
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _store = new SettingsStore();
        _settings = _store.Load();
        _autoStart = new AutoStartManager();

        LocalizationManager.Instance.SetCulture(
            string.IsNullOrEmpty(_settings.Language)
                ? null
                : System.Globalization.CultureInfo.GetCultureInfo(_settings.Language));
        ThemeManager.Apply(_settings.Theme);

        _mainViewModel = new MainViewModel(new DisplayConfigDpiScaleApplier(), _settings);
        _mainWindow = new MainWindow(_mainViewModel);
        _mainWindow.ExitRequested += (_, _) => Shutdown();
        _mainWindow.SettingsRequested += (_, _) => ShowSettings();
        _mainWindow.Show();
    }

    private void ShowSettings()
    {
        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.Activate();
            return;
        }

        var viewModel = new SettingsViewModel(_settings, _store, _autoStart, _mainViewModel);
        _settingsWindow = new SettingsWindow(viewModel) { Owner = _mainWindow };
        _settingsWindow.Closed += (_, _) =>
        {
            // 设置可能改了预设，主视图模型需同步。
            _mainViewModel.ReloadPresets();
            _settingsWindow = null;
        };
        _settingsWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
                if (_ownsMutex)
        {
            _singleInstanceMutex?.ReleaseMutex();
        }

        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}

