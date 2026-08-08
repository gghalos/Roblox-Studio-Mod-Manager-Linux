using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using RobloxStudioModManager.Linux.Mods;
using RobloxStudioModManager.Linux.Vinegar;

namespace RobloxStudioModManager.Linux;

public sealed class MainWindow : Window
{
    private readonly ModManager _mods = new();
    private readonly VinegarDetector _detector = new();
    private readonly ListBox _modList = new();
    private readonly TextBlock _status = new();
    private readonly TextBlock _studio = new();

    public MainWindow()
    {
        Title = "Roblox Studio Mod Manager";
        Width = 900;
        Height = 600;
        MinWidth = 700;
        MinHeight = 450;
        Background = Brush.Parse("#111318");

        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Margin = new Thickness(24) };
        var header = new StackPanel { Spacing = 6 };
        header.Children.Add(new TextBlock { Text = "Roblox Studio Mod Manager", FontSize = 28, FontWeight = FontWeight.Bold, Foreground = Brushes.White });
        _studio.Foreground = Brush.Parse("#b8beca");
        header.Children.Add(_studio);
        root.Children.Add(header);

        var content = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Margin = new Thickness(0, 20, 0, 20) };
        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        toolbar.Children.Add(MakeButton("Install Mod", InstallModAsync));
        toolbar.Children.Add(MakeButton("Remove Selected", RemoveSelected));
        toolbar.Children.Add(MakeButton("Open Mods Folder", OpenModsFolder));
        toolbar.Children.Add(MakeButton("Refresh", Refresh));
        toolbar.Children.Add(MakeButton("Launch Studio", LaunchStudio));
        content.Children.Add(toolbar);

        _modList.Margin = new Thickness(0, 14, 0, 14);
        _modList.Background = Brush.Parse("#191c22");
        _modList.Foreground = Brushes.White;
        Grid.SetRow(_modList, 1);
        content.Children.Add(_modList);

        _status.Foreground = Brush.Parse("#b8beca");
        Grid.SetRow(_status, 2);
        content.Children.Add(_status);
        Grid.SetRow(content, 1);
        root.Children.Add(content);

        Content = root;
        Refresh();
    }

    private static Button MakeButton(string text, Action action)
    {
        var button = new Button { Content = text, Padding = new Thickness(14, 8) };
        button.Click += (_, _) => action();
        return button;
    }

    private void Refresh()
    {
        var installation = _detector.Detect();
        _studio.Text = installation.StudioExecutable is null
            ? "Vinegar detected, but Roblox Studio is not installed."
            : $"Studio: {installation.StudioExecutable}";

        _modList.ItemsSource = _mods.ListMods()
            .Select(name => new ModEntry(name, _mods.IsInstalled(name) ? "Installed" : "Available"))
            .ToArray();
        _status.Text = $"Mods directory: {_mods.GetModsDirectory()}";
    }

    private async void InstallModAsync()
    {
        try
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select a Roblox Studio mod folder",
                AllowMultiple = false
            });
            var folder = folders.FirstOrDefault();
            if (folder is null || string.IsNullOrWhiteSpace(folder.Path.LocalPath)) return;

            var source = folder.Path.LocalPath;
            var name = Path.GetFileName(source.TrimEnd(Path.DirectorySeparatorChar));
            if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("The selected folder needs a name.");

            var destination = Path.Combine(_mods.GetModsDirectory(), name);
            if (Directory.Exists(destination)) Directory.Delete(destination, true);
            CopyDirectory(source, destination);

            var installation = _detector.Detect();
            if (installation.StudioExecutable is null)
                throw new InvalidOperationException("Roblox Studio was not found. Launch Vinegar once to install Studio.");

            var directory = Path.GetDirectoryName(installation.StudioExecutable)!;
            _mods.Install(name, directory, Path.GetFileName(directory));
            _status.Text = $"Installed {name}.";
            Refresh();
        }
        catch (Exception ex) { await ShowError(ex.Message); }
    }

    private async void RemoveSelected()
    {
        if (_modList.SelectedItem is not ModEntry entry || !_mods.IsInstalled(entry.Name)) return;
        try
        {
            var installation = _detector.Detect();
            if (installation.StudioExecutable is null) throw new InvalidOperationException("Roblox Studio was not found.");
            var directory = Path.GetDirectoryName(installation.StudioExecutable)!;
            _mods.Uninstall(entry.Name, directory, Path.GetFileName(directory));
            _status.Text = $"Removed {entry.Name}.";
            Refresh();
        }
        catch (Exception ex) { await ShowError(ex.Message); }
    }

    private void OpenModsFolder()
    {
        Directory.CreateDirectory(_mods.GetModsDirectory());
        _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "xdg-open",
            ArgumentList = { _mods.GetModsDirectory() },
            UseShellExecute = false
        });
    }

    private async void LaunchStudio()
    {
        try
        {
            var installation = _detector.Detect();
            if (installation.StudioExecutable is null) throw new InvalidOperationException("Roblox Studio was not found.");
            var directory = Path.GetDirectoryName(installation.StudioExecutable)!;
            foreach (var mod in _mods.ListInstalledMods())
                _mods.Install(mod, directory, Path.GetFileName(directory));
            Hide();
            await VinegarLauncher.LaunchAsync(Array.Empty<string>());
            Show();
        }
        catch (Exception ex) { await ShowError(ex.Message); }
    }

    private async Task ShowError(string message)
    {
        var ok = new Button { Content = "OK", HorizontalAlignment = HorizontalAlignment.Right };
        var dialog = new Window
        {
            Title = "Mod Manager Error", Width = 520, Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(20), Spacing = 16,
                Children = { new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap }, ok }
            }
        };
        ok.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(this);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
        }
    }

    private sealed record ModEntry(string Name, string State)
    {
        public override string ToString() => $"{Name}    —    {State}";
    }
}
