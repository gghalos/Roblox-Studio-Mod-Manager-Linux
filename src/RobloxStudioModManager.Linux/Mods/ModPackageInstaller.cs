using System.IO.Compression;

namespace RobloxStudioModManager.Linux.Mods;

public sealed class ModPackageInstaller
{
    private readonly ModManager _manager;

    public ModPackageInstaller(ModManager manager) => _manager = manager;

    public ModManifest Inspect(string packagePath)
    {
        if (!File.Exists(packagePath))
            throw new FileNotFoundException("Mod package not found.", packagePath);
        if (!string.Equals(Path.GetExtension(packagePath), ".zip", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Mod packages must be .zip files.");

        using var archive = ZipFile.OpenRead(packagePath);
        var manifestEntry = archive.Entries.FirstOrDefault(e =>
            string.Equals(e.FullName, "manifest.json", StringComparison.OrdinalIgnoreCase) ||
            e.FullName.EndsWith("/manifest.json", StringComparison.OrdinalIgnoreCase));

        if (manifestEntry is null)
            throw new InvalidDataException("The package does not contain manifest.json.");

        var temp = Path.Combine(Path.GetTempPath(), "rsm-manifest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var manifestPath = Path.Combine(temp, "manifest.json");
            using var input = manifestEntry.Open();
            using var output = File.Create(manifestPath);
            input.CopyTo(output);
            return ModManifest.Load(temp);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    public ModManifest InstallPackage(string packagePath)
    {
        var manifest = Inspect(packagePath);
        var destination = Path.Combine(_manager.GetModsDirectory(), manifest.Id);
        var staging = destination + ".staging-" + Guid.NewGuid().ToString("N");

        Directory.CreateDirectory(staging);
        try
        {
            using var archive = ZipFile.OpenRead(packagePath);
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                    continue;

                var relative = NormalizeEntryPath(entry.FullName);
                if (relative is null)
                    throw new InvalidDataException($"Unsafe path in package: {entry.FullName}");
                if (string.Equals(relative, "manifest.json", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (relative.StartsWith("files/", StringComparison.OrdinalIgnoreCase))
                    relative = relative[6..];

                if (string.IsNullOrWhiteSpace(relative))
                    continue;

                var target = Path.GetFullPath(Path.Combine(staging, relative));
                var root = Path.GetFullPath(staging) + Path.DirectorySeparatorChar;
                if (!target.StartsWith(root, StringComparison.Ordinal))
                    throw new InvalidDataException($"Unsafe path in package: {entry.FullName}");

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                using var input = entry.Open();
                using var output = File.Create(target);
                input.CopyTo(output);
            }

            if (Directory.Exists(destination))
                Directory.Delete(destination, recursive: true);
            Directory.Move(staging, destination);
            return manifest;
        }
        catch
        {
            if (Directory.Exists(staging))
                Directory.Delete(staging, recursive: true);
            throw;
        }
    }

    private static string? NormalizeEntryPath(string path)
    {
        path = path.Replace('\\', '/').TrimStart('/');
        if (string.IsNullOrWhiteSpace(path) ||
            path.Contains("../", StringComparison.Ordinal) ||
            path == ".." ||
            path.Contains(':'))
            return null;
        return path;
    }
}
