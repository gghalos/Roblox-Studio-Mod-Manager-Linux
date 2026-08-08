using System.Text.Json;

namespace RobloxStudioModManager.Linux.Mods;

public sealed record ModManifest(
    string Id,
    string Name,
    string Version,
    string Author,
    string Description,
    string? StudioVersion = null)
{
    public static ModManifest Load(string directory)
    {
        var path = Path.Combine(directory, "manifest.json");
        if (!File.Exists(path))
            throw new InvalidDataException($"Mod manifest not found: {path}");

        var manifest = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(path));
        if (manifest is null)
            throw new InvalidDataException("The mod manifest is empty or invalid.");

        Validate(manifest);
        return manifest;
    }

    public void Save(string directory)
    {
        Validate(this);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "manifest.json"),
            JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void Validate(ModManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.Id)) throw new InvalidDataException("Mod ID is required.");
        if (string.IsNullOrWhiteSpace(manifest.Name)) throw new InvalidDataException("Mod name is required.");
        if (string.IsNullOrWhiteSpace(manifest.Version)) throw new InvalidDataException("Mod version is required.");
        if (string.IsNullOrWhiteSpace(manifest.Author)) throw new InvalidDataException("Mod author is required.");

        if (manifest.Id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            manifest.Id.Contains(Path.DirectorySeparatorChar) ||
            manifest.Id.Contains(Path.AltDirectorySeparatorChar) ||
            manifest.Id is "." or "..")
            throw new InvalidDataException("Mod ID contains invalid path characters.");
    }
}
