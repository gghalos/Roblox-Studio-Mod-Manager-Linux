using RobloxStudioModManager.Linux.Vinegar;

namespace RobloxStudioModManager.Linux.Mods;

public sealed record ModSyncResult(string StudioVersion, bool VersionChanged, IReadOnlyList<string> AppliedMods);

public sealed class ModSyncService
{
    private readonly ModManager _mods;

    public ModSyncService(ModManager mods) => _mods = mods;

    public ModSyncResult Sync(VinegarInstallation installation)
    {
        if (installation.StudioExecutable is null)
            throw new InvalidOperationException("Roblox Studio is not installed in Vinegar.");

        if (string.IsNullOrWhiteSpace(installation.DataDirectory))
            throw new InvalidOperationException("Vinegar data directory is not available.");

        var studioDirectory = Path.GetDirectoryName(installation.StudioExecutable);
        if (string.IsNullOrWhiteSpace(studioDirectory))
            throw new InvalidOperationException("Unable to determine the Roblox Studio installation directory.");

        var studioVersion = Path.GetFileName(studioDirectory);
        if (string.IsNullOrWhiteSpace(studioVersion))
            throw new InvalidOperationException("Unable to determine the Roblox Studio version.");

        var markerDirectory = Path.Combine(installation.DataDirectory, "roblox-studio-mod-manager");
        var markerFile = Path.Combine(markerDirectory, "studio-version");
        Directory.CreateDirectory(markerDirectory);

        var previousVersion = File.Exists(markerFile) ? File.ReadAllText(markerFile).Trim() : null;
        var changed = !string.Equals(previousVersion, studioVersion, StringComparison.Ordinal);
        var applied = new List<string>();

        foreach (var mod in _mods.ListInstalledMods())
        {
            _mods.Install(mod, studioDirectory, studioVersion);
            applied.Add(mod);
        }

        File.WriteAllText(markerFile, studioVersion);
        return new ModSyncResult(studioVersion, changed, applied);
    }
}
