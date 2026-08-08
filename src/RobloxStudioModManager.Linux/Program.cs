using Avalonia;
using RobloxStudioModManager.Linux.Mods;
using RobloxStudioModManager.Linux.Platform;
using RobloxStudioModManager.Linux.Vinegar;

namespace RobloxStudioModManager.Linux;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length == 0 || args[0].Equals("ui", StringComparison.OrdinalIgnoreCase))
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args.Skip(1).ToArray());

        return RunCliAsync(args).GetAwaiter().GetResult();
    }

    private static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

    private static async Task<int> RunCliAsync(string[] args)
    {
        var detector = new VinegarDetector();
        var installation = detector.Detect();
        var modManager = new ModManager();
        var sync = new ModSyncService(modManager);
        var config = ManagerConfig.Load();

        Console.WriteLine("Roblox Studio Mod Manager for Linux");
        Console.WriteLine();

        if (args[0].Equals("status", StringComparison.OrdinalIgnoreCase))
        {
            PrintStatus(installation, modManager, config);
            return installation.IsInstalled ? 0 : 1;
        }

        if (args[0].Equals("config", StringComparison.OrdinalIgnoreCase))
            return HandleConfig(args, config);

        if (!installation.IsInstalled)
        {
            Console.WriteLine("Vinegar was not detected.");
            Console.WriteLine("Expected Flatpak app: org.vinegarhq.Vinegar");
            return 1;
        }

        Console.WriteLine("Vinegar: detected");
        Console.WriteLine($"Vinegar data: {installation.DataDirectory}");
        Console.WriteLine($"Studio prefix: {installation.StudioPrefix ?? "not found"}");
        Console.WriteLine($"Studio executable: {installation.StudioExecutable ?? "not found"}");

        if (args[0].Equals("launch", StringComparison.OrdinalIgnoreCase) ||
            args[0].Equals("sync", StringComparison.OrdinalIgnoreCase))
        {
            if (installation.StudioExecutable is null)
            {
                Console.Error.WriteLine("Studio executable was not found. Launch Vinegar once so Studio can be installed.");
                return 1;
            }

            if (args[0].Equals("sync", StringComparison.OrdinalIgnoreCase) || config.AutoSync)
            {
                var result = sync.Sync(installation);
                Console.WriteLine(result.VersionChanged
                    ? $"Studio update detected: {result.StudioVersion}"
                    : $"Studio version: {result.StudioVersion}");

                foreach (var mod in result.AppliedMods)
                    Console.WriteLine($"Applied mod: {mod}");
            }
            else
            {
                Console.WriteLine("Auto-sync disabled; launching without applying mods.");
            }

            if (args[0].Equals("sync", StringComparison.OrdinalIgnoreCase))
                return 0;

            Console.WriteLine("Launching Studio through Vinegar...");
            return await VinegarLauncher.LaunchAsync(args.Skip(1));
        }

        if (args[0].Equals("mods", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Mod directory: {modManager.GetModsDirectory()}");
            foreach (var mod in modManager.ListMods())
                Console.WriteLine($"  {mod} {(modManager.IsInstalled(mod) ? "[installed]" : "[available]")}");
            return 0;
        }

        if (args[0].Equals("mods-dir", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(modManager.GetModsDirectory());
            return 0;
        }

        if (args.Length > 1 && args[0].Equals("install", StringComparison.OrdinalIgnoreCase))
            return InstallCli(args[1], installation, modManager);

        if (args.Length > 1 && args[0].Equals("uninstall", StringComparison.OrdinalIgnoreCase))
            return UninstallCli(args[1], installation, modManager);

        Console.WriteLine("Usage:");
        Console.WriteLine("  roblox-studio-mod-manager");
        Console.WriteLine("  roblox-studio-mod-manager ui");
        Console.WriteLine("  roblox-studio-mod-manager status");
        Console.WriteLine("  roblox-studio-mod-manager config");
        Console.WriteLine("  roblox-studio-mod-manager config auto-sync on|off");
        Console.WriteLine("  roblox-studio-mod-manager launch");
        Console.WriteLine("  roblox-studio-mod-manager sync");
        Console.WriteLine("  roblox-studio-mod-manager mods");
        Console.WriteLine("  roblox-studio-mod-manager mods-dir");
        Console.WriteLine("  roblox-studio-mod-manager install <mod>");
        Console.WriteLine("  roblox-studio-mod-manager uninstall <mod>");
        return 0;
    }

    private static void PrintStatus(VinegarInstallation installation, ModManager manager, ManagerConfig config)
    {
        Console.WriteLine($"Vinegar: {(installation.IsInstalled ? "detected" : "not detected")}");
        Console.WriteLine($"Studio: {(installation.StudioExecutable is null ? "not found" : "detected")}");
        Console.WriteLine($"Studio executable: {installation.StudioExecutable ?? "not found"}");
        Console.WriteLine($"Mods directory: {manager.GetModsDirectory()}");
        Console.WriteLine($"Installed mods: {manager.ListInstalledMods().Count}");
        Console.WriteLine($"Auto-sync: {(config.AutoSync ? "enabled" : "disabled")}");
    }

    private static int HandleConfig(string[] args, ManagerConfig config)
    {
        if (args.Length == 1)
        {
            Console.WriteLine($"Config: {ManagerConfig.FilePath}");
            Console.WriteLine($"Auto-sync: {(config.AutoSync ? "enabled" : "disabled")}");
            return 0;
        }

        if (args.Length == 3 && args[1].Equals("auto-sync", StringComparison.OrdinalIgnoreCase))
        {
            if (!args[2].Equals("on", StringComparison.OrdinalIgnoreCase) &&
                !args[2].Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("Expected 'on' or 'off'.");
                return 1;
            }

            var updated = config with { AutoSync = args[2].Equals("on", StringComparison.OrdinalIgnoreCase) };
            updated.Save();
            Console.WriteLine($"Auto-sync: {(updated.AutoSync ? "enabled" : "disabled")}");
            return 0;
        }

        Console.Error.WriteLine("Usage: roblox-studio-mod-manager config [auto-sync on|off]");
        return 1;
    }

    private static int InstallCli(string mod, VinegarInstallation installation, ModManager manager)
    {
        if (installation.StudioExecutable is null)
        {
            Console.Error.WriteLine("Studio executable was not found.");
            return 1;
        }

        var directory = Path.GetDirectoryName(installation.StudioExecutable)!;
        manager.Install(mod, directory, Path.GetFileName(directory));
        Console.WriteLine($"Installed mod: {mod}");
        return 0;
    }

    private static int UninstallCli(string mod, VinegarInstallation installation, ModManager manager)
    {
        if (installation.StudioExecutable is null)
        {
            Console.Error.WriteLine("Studio executable was not found.");
            return 1;
        }

        var directory = Path.GetDirectoryName(installation.StudioExecutable)!;
        manager.Uninstall(mod, directory, Path.GetFileName(directory));
        Console.WriteLine($"Uninstalled mod: {mod}");
        return 0;
    }
}
