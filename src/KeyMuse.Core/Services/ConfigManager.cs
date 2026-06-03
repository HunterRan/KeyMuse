using System.Text.Json;
using KeyMuse.Core.Models;

namespace KeyMuse.Core.Services;

public class ConfigManager
{
    private readonly string _basePath;
    private ProfileConfig? _current;

    public ProfileConfig? Current => _current;

    public event Action<StatusMessage>? OnStatusChanged;

    public ConfigManager()
    {
        _basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyMuse", "profiles");
        Directory.CreateDirectory(_basePath);
    }

    public string[] ListProfiles()
    {
        if (!Directory.Exists(_basePath)) return [];
        return Directory.GetDirectories(_basePath)
            .Select(Path.GetFileName)
            .Where(x => x != null)
            .Cast<string>()
            .ToArray();
    }

    public ProfileConfig? LoadProfile(string name)
    {
        var path = GetConfigPath(name);
        if (!File.Exists(path)) return null;

        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<ProfileConfig>(json);
            if (config != null)
            {
                config.Name = name;
                _current = config;
            }
            return config;
        }
        catch (JsonException)
        {
            var backupPath = path + ".bak";
            File.Copy(path, backupPath, true);
            File.Delete(path);

            OnStatusChanged?.Invoke(new StatusMessage
            {
                Type = StatusMessageType.Error,
                Text = $"配置文件损坏，已备份到 {Path.GetFileName(backupPath)}，已创建新配置",
                ProgressCurrent = 0,
                ProgressTotal = 0
            });

            var config = new ProfileConfig { Name = name };
            SaveProfile(config);
            _current = config;
            return config;
        }
    }

    public void SaveProfile(ProfileConfig config)
    {
        var dir = GetProfileDir(config.Name);
        Directory.CreateDirectory(dir);
        var path = GetConfigPath(config.Name);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        _current = config;
    }

    public void DeleteProfile(string name)
    {
        var dir = GetProfileDir(name);
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, true);
        }
        if (_current?.Name == name)
        {
            _current = null;
        }
    }

    public ProfileConfig CreateProfile(string name)
    {
        var config = new ProfileConfig { Name = name };
        SaveProfile(config);
        _current = config;
        return config;
    }

    private string GetProfileDir(string name) =>
        Path.Combine(_basePath, SanitizeName(name));

    private string GetConfigPath(string name) =>
        Path.Combine(GetProfileDir(name), "config.json");

    private static string SanitizeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}
