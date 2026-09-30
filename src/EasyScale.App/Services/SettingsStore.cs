using System.IO;
using System.Text.Json;

namespace EasyScale.App.Services;

/// <summary>
/// 设置的 JSON 持久化。文件与程序同目录，符合「绿色版」定位。
/// 读写失败时回退到默认值，不让配置问题阻断启动。
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;

    public SettingsStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, "settings.json");
    }

    /// <summary>配置文件的完整路径。</summary>
    public string FilePath => _filePath;

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch (Exception)
        {
            // 配置损坏不应阻断启动：回退默认值。
        }

        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception)
        {
            // 无写权限时静默失败，运行期仍可用。
        }
    }
}
