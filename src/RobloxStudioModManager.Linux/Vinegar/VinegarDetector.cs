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
        var executable = FindStudioExecutable(vinegarRoot);

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

    private static string? FindStudioExecutable(string vinegarRoot)
    {
        var versions = Path.Combine(vinegarRoot, "versions");
        if (!Directory.Exists(versions))
            return null;

        try
        {
            return Directory.EnumerateDirectories(versions, "version-*", SearchOption.TopDirectoryOnly)
                .Select(directory => Path.Combine(directory, "RobloxStudioBeta.exe"))
                .Where(File.Exists)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
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
