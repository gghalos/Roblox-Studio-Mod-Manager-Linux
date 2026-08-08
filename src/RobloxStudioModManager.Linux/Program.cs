using RobloxStudioModManager.Linux.Mods;
using RobloxStudioModManager.Linux.Vinegar;

var detector = new VinegarDetector();
var installation = detector.Detect();
var modManager = new ModManager();

Console.WriteLine("Roblox Studio Mod Manager for Linux");
Console.WriteLine();

if (!installation.IsInstalled)
{
    Console.WriteLine("Vinegar was not detected.");
    Console.WriteLine("Expected Flatpak app: org.vinegarhq.Vinegar");
    Console.WriteLine();
    Console.WriteLine("Install Vinegar from Flathub, then run this program again.");
    return 1;
}

Console.WriteLine("Vinegar: detected");
Console.WriteLine($"Vinegar data: {installation.DataDirectory}");
Console.WriteLine($"Studio prefix: {installation.StudioPrefix ?? "not found"}");
Console.WriteLine($"Studio executable: {installation.StudioExecutable ?? "not found"}");

if (args.Length > 0 && args[0].Equals("launch", StringComparison.OrdinalIgnoreCase))
{
    if (installation.StudioExecutable is null)
    {
        Console.Error.WriteLine("Studio executable was not found. Launch Vinegar once so Studio can be installed.");
        return 1;
    }

    var studioDirectory = Path.GetDirectoryName(installation.StudioExecutable)!;
    var studioVersion = Path.GetFileName(studioDirectory);

    foreach (var mod in modManager.ListMods())
    {
        try
        {
            modManager.Install(mod, studioDirectory, studioVersion);
            Console.WriteLine($"Applied mod: {mod}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to apply mod '{mod}': {ex.Message}");
            return 1;
        }
    }

    Console.WriteLine();
    Console.WriteLine("Launching Studio through Vinegar...");
    return await VinegarLauncher.LaunchAsync(args.Skip(1));
}

if (args.Length > 0 && args[0].Equals("mods", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine();
    Console.WriteLine($"Mod directory: {modManager.GetModsDirectory()}");
    Console.WriteLine();

    var mods = modManager.ListMods();
    if (mods.Count == 0)
    {
        Console.WriteLine("No mods installed in the mod directory.");
    }
    else
    {
        foreach (var mod in mods)
            Console.WriteLine($"  {mod}");
    }

    return 0;
}

if (args.Length > 0 && args[0].Equals("mods-dir", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine(modManager.GetModsDirectory());
    return 0;
}

if (args.Length > 1 && args[0].Equals("install", StringComparison.OrdinalIgnoreCase))
{
    if (installation.StudioExecutable is null)
    {
        Console.Error.WriteLine("Studio executable was not found.");
        return 1;
    }

    var studioDirectory = Path.GetDirectoryName(installation.StudioExecutable)!;
    var studioVersion = Path.GetFileName(studioDirectory);

    try
    {
        modManager.Install(args[1], studioDirectory, studioVersion);
        Console.WriteLine($"Installed mod: {args[1]}");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to install mod: {ex.Message}");
        return 1;
    }
}

if (args.Length > 1 && args[0].Equals("uninstall", StringComparison.OrdinalIgnoreCase))
{
    if (installation.StudioExecutable is null)
    {
        Console.Error.WriteLine("Studio executable was not found.");
        return 1;
    }

    var studioDirectory = Path.GetDirectoryName(installation.StudioExecutable)!;
    var studioVersion = Path.GetFileName(studioDirectory);

    try
    {
        modManager.Uninstall(args[1], studioDirectory, studioVersion);
        Console.WriteLine($"Uninstalled mod: {args[1]}");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to uninstall mod: {ex.Message}");
        return 1;
    }
}

Console.WriteLine();
Console.WriteLine("Usage:");
Console.WriteLine("  roblox-studio-mod-manager launch");
Console.WriteLine("  roblox-studio-mod-manager mods");
Console.WriteLine("  roblox-studio-mod-manager mods-dir");
Console.WriteLine("  roblox-studio-mod-manager install <mod>");
Console.WriteLine("  roblox-studio-mod-manager uninstall <mod>");
return 0;
