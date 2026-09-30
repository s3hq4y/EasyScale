using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace EasyScale.App.Localization;

/// <summary>
/// 应用内的本地化管理器。
/// 通过索引器暴露字符串，并以 <see cref="INotifyPropertyChanged"/> 通知界面刷新，
/// 从而支持运行时切换语言而无需重启。
/// </summary>
public sealed class LocalizationManager : INotifyPropertyChanged
{
    /// <summary>资源基名，与 <c>Resources/Strings.resx</c> 的清单名一致。</summary>
    private static readonly ResourceManager Resources =
        new("EasyScale.App.Resources.Strings", typeof(LocalizationManager).Assembly);

    /// <summary>全局单例。</summary>
    public static LocalizationManager Instance { get; } = new();

    /// <summary>当前生效的区域。null 表示跟随系统。</summary>
    public CultureInfo? Culture { get; private set; }

    /// <summary>索引器：XAML 通过 <c>Binding [Key]</c> 使用，语言切换时自动刷新。</summary>
    public string this[string key] => Get(key);

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>按 key 取字符串；缺失时返回 <c>!key!</c> 以便快速定位漏翻项。</summary>
    public string Get(string key)
    {
        return Resources.GetString(key, Culture) ?? $"!{key}!";
    }

    /// <summary>设置运行时语言。<paramref name="culture"/> 为 null 时跟随系统。</summary>
    public void SetCulture(CultureInfo? culture)
    {
        if (Equals(Culture, culture))
        {
            return;
        }

        Culture = culture;
        CultureInfo.CurrentUICulture = culture ?? CultureInfo.InstalledUICulture;

        // 通知所有索引器绑定刷新。
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}
