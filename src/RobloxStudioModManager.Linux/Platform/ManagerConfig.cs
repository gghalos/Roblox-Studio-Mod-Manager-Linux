using System.Text.Json;

namespace RobloxStudioModManager.Linux.Platform;

public sealed record ManagerConfig(bool AutoSync = true)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string FilePath => Path.Combine(LinuxPaths.DataDirectory, "config.json");

    public static ManagerConfig Load()
    {
        LinuxPaths.EnsureDirectories();
        if (!File.Exists(FilePath))
            return new ManagerConfig();

        try
        {
            return JsonSerializer.Deserialize<ManagerConfig>(File.ReadAllText(FilePath), JsonOptions)
                   ?? new ManagerConfig();
        }
        catch (JsonException)
        {
            return new ManagerConfig();
        }
    }

    public void Save()
    {
        LinuxPaths.EnsureDirectories();
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
    }
}
