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
    private readonly ModPackageInstaller _packages;
    private readonly VinegarDetector _detector = new();
    private readonly ListBox _modList = new();
    private readonly TextBlock _status = new();
    private readonly TextBlock _studio = new();
    private readonly TextBlock _details = new();

    public MainWindow()
    {
        _packages = new ModPackageInstaller(_mods);
        Title = "Roblox Studio Mod Manager";
        Width = 980;
        Height = 650;
        MinWidth = 760;
        MinHeight = 480;
        Background = Brush.Parse("#111318");

        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Margin = new Thickness(24) };
        var header = new StackPanel { Spacing = 6 };
        header.Children.Add(new TextBlock { Text = "Roblox Studio Mod Manager", FontSize = 28, FontWeight = FontWeight.Bold, Foreground = Brushes.White });
        _studio.Foreground = Brush.Parse("#b8beca");
        header.Children.Add(_studio);
        root.Children.Add(header);

        var content = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,3*"), RowDefinitions = new RowDefinitions("Auto,*,Auto"), Margin = new Thickness(0, 20, 0, 20) };
        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        toolbar.Children.Add(MakeButton("Install Mod", InstallModAsync));
        toolbar.Children.Add(MakeButton("Remove Selected", RemoveSelected));
        toolbar.Children.Add(MakeButton("Open Mods Folder", OpenModsFolder));
        toolbar.Children.Add(MakeButton("Refresh", Refresh));
        toolbar.Children.Add(MakeButton("Launch Studio", LaunchStudio));
        Grid.SetColumnSpan(toolbar, 2);
        content.Children.Add(toolbar);

        _modList.Margin = new Thickness(0, 14, 14, 14);
        _modList.Background = Brush.Parse("#191c22");
        _modList.Foreground = Brushes.White;
        _modList.SelectionChanged += (_, _) => UpdateDetails();
        Grid.SetRow(_modList, 1);
        content.Children.Add(_modList);

        var detailsPanel = new StackPanel { Margin = new Thickness(14, 14, 0, 14), Spacing = 10 };
        detailsPanel.Children.Add(new TextBlock { Text = "Mod details", FontSize = 20, FontWeight = FontWeight.Bold, Foreground = Brushes.White });
        _details.Text = "Select a mod to inspect it.";
        _details.Foreground = Brush.Parse("#b8beca");
        _details.TextWrapping = TextWrapping.Wrap;
        detailsPanel.Children.Add(_details);
        Grid.SetColumn(detailsPanel, 1);
        Grid.SetRow(detailsPanel, 1);
        content.Children.Add(detailsPanel);

        _status.Foreground = Brush.Parse("#b8beca");
        Grid.SetColumnSpan(_status, 2);
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
            .Select(name => new ModEntry(name, LoadManifest(name), _mods.IsInstalled(name)))
            .ToArray();
        _status.Text = $"Mods directory: {_mods.GetModsDirectory()}    •    Installed: {_mods.ListInstalledMods().Count}";
        UpdateDetails();
    }

    private ModManifest? LoadManifest(string name)
    {
        try { return ModManifest.Load(Path.Combine(_mods.GetModsDirectory(), name)); }
        catch { return null; }
    }

    private void UpdateDetails()
    {
        if (_modList.SelectedItem is not ModEntry entry)
        {
            _details.Text = "Select a mod to inspect it.";
            return;
        }

        if (entry.Manifest is null)
        {
            _details.Text = $"{entry.Name}\n\nNo valid manifest.json was found. This mod can be kept in the library, but packaged installs should include a manifest.";
            return;
        }

        _details.Text = $"{entry.Manifest.Name}\nVersion: {entry.Manifest.Version}\nAuthor: {entry.Manifest.Author}\nStatus: {(entry.Installed ? "Installed" : "Available")}\n\n{entry.Manifest.Description}";
    }

    private async void InstallModAsync()
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Install Roblox Studio mod",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("Mod packages") { Patterns = new[] { "*.zip" } } }
            });
            var file = files.FirstOrDefault();
            if (file is null) return;

            var path = file.Path.LocalPath;
            var manifest = _packages.Inspect(path);
            var installed = _packages.InstallPackage(path);
            _status.Text = $"Installed {installed.Name} v{installed.Version}.";
            Refresh();
        }
        catch (Exception ex) { await ShowError(ex.Message); }
    }

    private async void RemoveSelected()
    {
        if (_modList.SelectedItem is not ModEntry entry || !entry.Installed) return;
        try
        {
            var installation = _detector.Detect();
            if (installation.StudioExecutable is null) throw new InvalidOperationException("Roblox Studio was not found.");
            var directory = Path.GetDirectoryName(installation.StudioExecutable)!;
            _mods.Uninstall(entry.Name, directory, Path.GetFileName(directory));
            _status.Text = $"Removed {entry.Name} from Studio.";
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

    private sealed record ModEntry(string Name, ModManifest? Manifest, bool Installed)
    {
        public override string ToString() => Manifest is null
            ? $"{Name}    —    No manifest"
            : $"{Manifest.Name}    v{Manifest.Version}    —    {(Installed ? "Installed" : "Available")}";
    }
}
