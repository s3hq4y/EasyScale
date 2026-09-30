using System.Windows.Data;
using System.Windows.Markup;

namespace EasyScale.App.Localization;

/// <summary>
/// XAML 标记扩展：<c>Text="{loc:Tr Main_Title}"</c>。
/// 返回绑定到 <see cref="LocalizationManager"/> 索引器的 <see cref="Binding"/>，
/// 因此语言切换时会自动刷新，无需重建界面。
/// </summary>
[MarkupExtensionReturnType(typeof(Binding))]
public sealed class TrExtension : MarkupExtension
{
    public TrExtension()
    {
    }

    public TrExtension(string key) => Key = key;

    /// <summary>资源键。</summary>
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationManager.Instance,
            Mode = BindingMode.OneWay,
        };

        return binding.ProvideValue(serviceProvider);
    }
}
