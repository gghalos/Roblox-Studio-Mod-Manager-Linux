using System.Diagnostics;

namespace RobloxStudioModManager.Linux.Vinegar;

public sealed record VinegarInstallation(
    bool IsInstalled,
    string? DataDirectory,
    string? StudioPrefix,
    string? StudioExecutable);

public sealed class VinegarDetector
{
    public const string FlatpakAppId = "org.vinegarhq.Vinegar";

    public VinegarInstallation Detect()
    {
        if (!IsFlatpakInstalled())
            return new(false, null, null, null);

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var data = Path.Combine(home, ".var", "app", FlatpakAppId, "data");

        if (!Directory.Exists(data))
        {
            // Keep detection useful for non-default Flatpak setups.
            var fallback = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                var candidate = Path.Combine(fallback, FlatpakAppId, "data");
                if (Directory.Exists(candidate))
                    data = candidate;
            }
        }

        var vinegarRoot = Path.Combine(data, "vinegar");
        var prefix = FindDirectory(vinegarRoot, "prefixes", "studio");
        var executable = prefix is null ? null : FindFile(prefix, "RobloxStudioBeta.exe");

        return new(true, data, prefix, executable);
    }

    private static bool IsFlatpakInstalled()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "flatpak",
                ArgumentList = { "info", FlatpakAppId },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindDirectory(string root, params string[] relativePath)
    {
        var exact = Path.Combine(new[] { root }.Concat(relativePath).ToArray());
        return Directory.Exists(exact) ? exact : null;
    }

    private static string? FindFile(string root, string fileName)
    {
        try
        {
            return Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories).FirstOrDefault();
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
