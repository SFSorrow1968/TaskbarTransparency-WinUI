using System.Text.Json;
using System.Text.Json.Serialization;
using TaskbarTransparency.Models;

namespace TaskbarTransparency.Services;

public sealed class SettingsStore
{
    private string? _lastSerializedSettings;
    private DateTime _lastWriteTimeUtc;

    public SettingsStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OxygenTaskbar",
            "settings.json"))
    {
    }

    internal SettingsStore(string settingsPath)
    {
        SettingsPath = settingsPath;
    }

    public string SettingsPath { get; }

    public AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.AppSettings) ?? new AppSettings();
            settings.ActiveProfile = settings.ActiveProfile.NormalizeTransitions();
            RememberExistingSettings(json);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.AppSettings);
        if (CachedSettingsMatch(json))
        {
            return;
        }

        if (ExistingSettingsMatch(json))
        {
            RememberExistingSettings(json);
            return;
        }

        var tempPath = Path.Combine(directory, $"{Path.GetFileName(SettingsPath)}.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, SettingsPath, overwrite: true);
        RememberExistingSettings(json);
    }

    private bool CachedSettingsMatch(string json)
    {
        return _lastSerializedSettings is not null
            && string.Equals(_lastSerializedSettings, json, StringComparison.Ordinal)
            && File.Exists(SettingsPath)
            && File.GetLastWriteTimeUtc(SettingsPath) == _lastWriteTimeUtc;
    }

    private bool ExistingSettingsMatch(string json)
    {
        try
        {
            return File.Exists(SettingsPath) && string.Equals(File.ReadAllText(SettingsPath), json, StringComparison.Ordinal);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void RememberExistingSettings(string json)
    {
        _lastSerializedSettings = json;
        _lastWriteTimeUtc = File.Exists(SettingsPath) ? File.GetLastWriteTimeUtc(SettingsPath) : default;
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;
