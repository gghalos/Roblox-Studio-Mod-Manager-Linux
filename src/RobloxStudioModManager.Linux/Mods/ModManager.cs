using System.Security.Cryptography;
using System.Text.Json;
using RobloxStudioModManager.Linux.Platform;

namespace RobloxStudioModManager.Linux.Mods;

public sealed class ModManager
{
    private sealed record InstalledFile(string RelativePath, string InstalledSha256);
    private sealed record InstalledMod(string Name, string StudioVersion, List<InstalledFile> Files);
    private sealed record ModState(List<InstalledMod> Installed);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public ModManager() => LinuxPaths.EnsureDirectories();

    public string GetModsDirectory() => LinuxPaths.ModsDirectory;

    public IReadOnlyList<string> ListMods()
    {
        if (!Directory.Exists(LinuxPaths.ModsDirectory))
            return Array.Empty<string>();

        return Directory.EnumerateDirectories(LinuxPaths.ModsDirectory)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToArray();
    }

    public IReadOnlyList<string> ListInstalledMods() => LoadState().Installed
        .Select(x => x.Name)
        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public bool IsInstalled(string modName) => LoadState().Installed.Any(x =>
        string.Equals(x.Name, modName, StringComparison.OrdinalIgnoreCase));

    public void Install(string modName, string studioDirectory, string studioVersion)
    {
        var source = ResolveModDirectory(modName);
        if (!Directory.Exists(source))
            throw new DirectoryNotFoundException($"Mod not found: {source}");

        if (!Directory.Exists(studioDirectory))
            throw new DirectoryNotFoundException($"Studio directory not found: {studioDirectory}");

        var previous = LoadState().Installed.FirstOrDefault(x =>
            string.Equals(x.Name, modName, StringComparison.OrdinalIgnoreCase));
        var backupRoot = Path.Combine(LinuxPaths.BackupsDirectory, SanitizeName(studioVersion), SanitizeName(modName));
        var installedFiles = new List<InstalledFile>();

        foreach (var sourceFile in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, sourceFile);
            var destination = GetSafeDestination(studioDirectory, relative);
            var backup = GetSafeDestination(backupRoot, relative);

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            if (File.Exists(destination) && !File.Exists(backup))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                File.Copy(destination, backup);
            }

            File.Copy(sourceFile, destination, overwrite: true);
            installedFiles.Add(new InstalledFile(relative, ComputeSha256(destination)));
        }

        var state = LoadState();
        state.Installed.RemoveAll(x => string.Equals(x.Name, modName, StringComparison.OrdinalIgnoreCase));
        state.Installed.Add(new InstalledMod(modName, studioVersion, installedFiles));
        SaveState(state);
    }

    public void Uninstall(string modName, string studioDirectory, string studioVersion)
    {
        var state = LoadState();
        var installed = state.Installed.FirstOrDefault(x =>
            string.Equals(x.Name, modName, StringComparison.OrdinalIgnoreCase));

        if (installed is null)
            throw new InvalidOperationException($"Mod is not installed: {modName}");

        var backupRoot = Path.Combine(LinuxPaths.BackupsDirectory, SanitizeName(installed.StudioVersion), SanitizeName(modName));
        foreach (var file in installed.Files)
        {
            var destination = GetSafeDestination(studioDirectory, file.RelativePath);
            var backup = GetSafeDestination(backupRoot, file.RelativePath);

            // Never silently destroy a user's changes made after the mod was applied.
            if (File.Exists(destination) && !string.Equals(ComputeSha256(destination), file.InstalledSha256, StringComparison.OrdinalIgnoreCase))
                continue;

            if (File.Exists(backup))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(backup, destination, overwrite: true);
            }
            else if (File.Exists(destination))
            {
                File.Delete(destination);
            }
        }

        state.Installed.Remove(installed);
        SaveState(state);
    }

    private static string ResolveModDirectory(string modName)
    {
        var fullRoot = Path.GetFullPath(LinuxPaths.ModsDirectory);
        var path = Path.GetFullPath(Path.Combine(fullRoot, modName));
        if (!path.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
            !string.Equals(path, fullRoot, StringComparison.Ordinal))
            throw new ArgumentException("Mod name resolves outside the mods directory.", nameof(modName));
        return path;
    }

    private static string GetSafeDestination(string root, string relative)
    {
        var fullRoot = Path.GetFullPath(root);
        var path = Path.GetFullPath(Path.Combine(fullRoot, relative));
        if (!path.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
            !string.Equals(path, fullRoot, StringComparison.Ordinal))
            throw new InvalidOperationException("Mod contains a path outside its destination directory.");
        return path;
    }

    private static string SanitizeName(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');
        return value;
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static ModState LoadState()
    {
        if (!File.Exists(LinuxPaths.StateFile))
            return new ModState(new List<InstalledMod>());

        try
        {
            return JsonSerializer.Deserialize<ModState>(File.ReadAllText(LinuxPaths.StateFile), JsonOptions)
                   ?? new ModState(new List<InstalledMod>());
        }
        catch (JsonException)
        {
            return new ModState(new List<InstalledMod>());
        }
    }

    private static void SaveState(ModState state) =>
        File.WriteAllText(LinuxPaths.StateFile, JsonSerializer.Serialize(state, JsonOptions));
}
