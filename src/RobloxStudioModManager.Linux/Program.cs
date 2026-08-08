using RobloxStudioModManager.Linux.Vinegar;

var detector = new VinegarDetector();
var installation = detector.Detect();

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

Console.WriteLine($"Vinegar: detected");
Console.WriteLine($"Vinegar data: {installation.DataDirectory}");
Console.WriteLine($"Studio prefix: {installation.StudioPrefix ?? "not found"}");
Console.WriteLine($"Studio executable: {installation.StudioExecutable ?? "not found"}");

if (args.Length > 0 && args[0].Equals("launch", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine();
    Console.WriteLine("Launching Studio through Vinegar...");
    return await VinegarLauncher.LaunchAsync(args.Skip(1));
}

Console.WriteLine();
Console.WriteLine("Usage:");
Console.WriteLine("  roblox-studio-mod-manager launch");
return 0;
