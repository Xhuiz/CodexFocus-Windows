using System.IO;
using System.Text.Json;
using CodexFocus.Core.Settings;

namespace CodexFocus.Windows.Services;

public sealed class JsonSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public JsonSettingsStore()
        : this(System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CodexFocusWindows",
            "settings.json"))
    {
    }

    public JsonSettingsStore(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public AppSettings Load()
    {
        if (!File.Exists(Path))
        {
            return AppSettings.CreateDefault();
        }

        try
        {
            var json = File.ReadAllText(Path);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions) ?? AppSettings.CreateDefault();
            settings.Normalize();
            return settings;
        }
        catch (JsonException)
        {
            return AppSettings.CreateDefault();
        }
        catch (IOException)
        {
            return AppSettings.CreateDefault();
        }
    }

    public void Save(AppSettings settings)
    {
        settings.Normalize();
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        File.WriteAllText(Path, json);
    }
}
