using System.Diagnostics;

namespace RobloxStudioModManager.Linux.Vinegar;

public static class VinegarLauncher
{
    public static async Task<int> LaunchAsync(IEnumerable<string> arguments)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "flatpak",
            ArgumentList = { "run", VinegarDetector.FlatpakAppId },
            UseShellExecute = false
        });

        if (process is null)
        {
            Console.Error.WriteLine("Unable to start Flatpak/Vinegar.");
            return 1;
        }

        await process.WaitForExitAsync();
        return process.ExitCode;
    }
}
