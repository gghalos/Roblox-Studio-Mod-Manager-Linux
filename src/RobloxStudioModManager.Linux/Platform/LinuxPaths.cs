namespace RobloxStudioModManager.Linux.Platform;

public static class LinuxPaths
{
    public static string DataDirectory
    {
        get
        {
            var xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (!string.IsNullOrWhiteSpace(xdg))
                return Path.Combine(xdg, "RobloxStudioModManager");

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local", "share", "RobloxStudioModManager");
        }
    }

    public static string ModsDirectory => Path.Combine(DataDirectory, "Mods");
    public static string StateFile => Path.Combine(DataDirectory, "state.json");
    public static string BackupsDirectory => Path.Combine(DataDirectory, "Backups");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(ModsDirectory);
        Directory.CreateDirectory(BackupsDirectory);
    }
}
