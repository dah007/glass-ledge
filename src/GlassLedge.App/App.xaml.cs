using System.IO;
using System.Windows;
using GlassLedge.Core.Models;
using GlassLedge.Core.Services;
using Microsoft.Win32;

namespace GlassLedge.App;

public partial class App : Application
{
    private const double NewWidgetOffset = 100;
    private const double NewWidgetCascade = 30;

    // Per-user ("Local\") so each signed-in user can run their own copy. Suffixed with
    // GLASSLEDGE_INSTANCE_ID when set, so automated tests never collide with a real running instance.
    private static string SingleInstanceMutexName =>
        Environment.GetEnvironmentVariable("GLASSLEDGE_INSTANCE_ID") is { Length: > 0 } id
            ? @$"Local\GlassLedge.SingleInstance.{id}"
            : @"Local\GlassLedge.SingleInstance";

    private Mutex? _singleInstanceMutex;

    private readonly List<FolderWindow> _widgets = [];

    private ISettingsStore _store = null!;
    private AppSettings _settings = null!;
    private TrayIcon? _tray;
    private AboutWindow? _about;
    private PreferencesWindow? _preferences;

    /// <summary>Every open widget, in the order they were created.</summary>
    internal IReadOnlyList<FolderWindow> Widgets => _widgets;

    /// <summary>The settings document, shared with the plugin manager.</summary>
    internal AppSettings Settings => _settings;

    /// <summary>Finds plugins and owns their widgets.</summary>
    internal PluginManager Plugins => _plugins ??= new PluginManager(this);
    private PluginManager? _plugins;

    /// <summary>Shows a Windows notification next to the tray icon, if the tray exists yet.</summary>
    internal void Notify(string message) => _tray?.ShowNotification(message);

    /// <summary>Folder widgets and plugin widgets together: everything that has a place in the stack.</summary>
    internal IEnumerable<ILayeredWidget> LayeredWidgets => _widgets.Cast<ILayeredWidget>().Concat(Plugins.Windows);

    /// <summary>Keeps the widgets in their saved front-to-back order.</summary>
    internal WidgetLayers Layers => _layers ??= new WidgetLayers(this);
    private WidgetLayers? _layers;

    /// <summary>Puts every widget window back in its place in the stack (the desktop layer, in order).</summary>
    internal void RestackWidgets() => Layers.Restack();

    internal bool WidgetsHidden => _settings.WidgetsHidden;

    /// <summary>Preferences: folders open in a GlassLedge pane instead of File Explorer.</summary>
    internal bool OpenFoldersInPane => _settings.OpenFoldersInPane;

    /// <summary>
    /// Average colour of the desktop wallpaper (0xFFRRGGBB), or null when it can't be read. Widgets with a
    /// custom colour use it to pick their text colour and glass opacity.
    /// </summary>
    internal uint? WallpaperColor { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Started at sign-in and again from the Start menu would otherwise show every widget twice.
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            Shutdown();
            return;
        }

        WidgetTheme.Apply(Resources);
        BrandTheme.Apply(Resources);
        WallpaperColor = Wallpaper.AverageColor();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        // Existing users keep their widgets: settings saved under the old app name are copied over once.
        LegacySettingsMigration.MigrateDefault();

        var settingsPath = SettingsPaths.GetDefaultSettingsFilePath();
        var isFirstRun = !File.Exists(settingsPath);
        _store = new JsonSettingsStore(settingsPath);
        _settings = _store.Load();

        // The app now lives in the tray, so it keeps running even with no widgets.
        _tray = new TrayIcon(this);

        foreach (var widgetSettings in _settings.Windows)
        {
            CreateWidget(widgetSettings);
        }

        // Plugin widgets come back only when Preferences > Allow plugins is on.
        Plugins.Start(_settings.AllowPlugins);

        // Only the very first run asks for a folder; someone who removed every widget isn't nagged at each sign-in.
        if (isFirstRun && _settings.Windows.Count == 0)
        {
            AddWidget();
        }

        if (_widgets.Count == 0 && Plugins.Windows.Count == 0)
        {
            _tray.ShowNotification("Running in the notification area. Right-click the icon to add a widget.");
        }
        else if (_settings.WidgetsHidden)
        {
            _tray.ShowNotification("Your widgets are hidden. Click the icon to show them.");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _tray?.Dispose();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    /// <summary>Asks the user to choose a folder. Returns null if they cancel.</summary>
    internal static string? PromptForFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose a folder for the widget",
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    /// <summary>Prompts for a folder and adds a new widget for it. Returns null if cancelled.</summary>
    internal FolderWindow? AddWidget()
    {
        var folder = PromptForFolder();
        if (folder is null)
        {
            return null;
        }

        // A new widget added while the others are hidden would be invisible; bring them all back.
        if (_settings.WidgetsHidden)
        {
            ShowAllWidgets();
        }

        var cascade = NewWidgetCascade * _settings.Windows.Count;
        var widgetSettings = new FolderWindowSettings
        {
            FolderPath = folder,
            Left = NewWidgetOffset + cascade,
            Top = NewWidgetOffset + cascade,
            Size = WidgetSize.Medium,
        };
        _settings.Windows.Add(widgetSettings);
        SaveSettings();

        return CreateWidget(widgetSettings);
    }

    /// <summary>
    /// Asks, then removes the widget from settings and closes it. The app keeps running in the tray.
    /// </summary>
    internal void ConfirmAndRemoveWidget(FolderWindow widget)
    {
        var answer = MessageBox.Show(
            $"Remove the \"{WidgetName.For(widget.Settings)}\" widget? The folder itself isn't touched.",
            "Remove widget",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        _settings.Windows.Remove(widget.Settings);
        _widgets.Remove(widget);
        SaveSettings();
        widget.Close();
    }

    /// <summary>Opens a folder in its own GlassLedge pane window.</summary>
    internal void OpenPane(string folder)
    {
        var pane = new PaneWindow(folder);
        pane.Show();
        pane.Activate();
    }

    /// <summary>Shows Preferences, or brings it forward if it's already open. Changes apply only on OK.</summary>
    internal void ShowPreferences()
    {
        if (_preferences is not null)
        {
            _preferences.Activate();
            return;
        }

        var packaged = StartupRegistration.IsPackaged;
        var before = new PreferencesState(
            !packaged && StartupRegistration.IsEnabled(),
            _settings.OpenFoldersInPane,
            _settings.AllowPlugins);

        var window = new PreferencesWindow(before, packaged);
        _preferences = window;
        window.Closed += (_, _) => _preferences = null;
        if (window.ShowDialog() != true)
        {
            return;
        }

        var after = window.Result;
        if (!packaged && after.StartWithWindows != before.StartWithWindows && !StartupRegistration.TrySet(after.StartWithWindows))
        {
            MessageBox.Show(
                "Windows wouldn't let GlassLedge change its startup setting. The other preferences were saved.",
                "Preferences",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        _settings.OpenFoldersInPane = after.OpenFoldersInPane;
        _settings.AllowPlugins = after.AllowPlugins;
        SaveSettings();
        Plugins.SetEnabled(after.AllowPlugins);
    }

    /// <summary>Shows the About dialog, or brings it forward if it's already open.</summary>
    internal void ShowAbout()
    {
        if (_about is not null)
        {
            _about.Activate();
            return;
        }

        _about = new AboutWindow();
        _about.Closed += (_, _) => _about = null;
        _about.ShowDialog();
    }

    private void RefreshWidgetColors()
    {
        foreach (var widget in _widgets)
        {
            widget.RefreshColor();
        }

        Plugins.RefreshColors();
    }

    internal void HideAllWidgets()
    {
        _settings.WidgetsHidden = true;
        SaveSettings();
        foreach (var widget in _widgets)
        {
            widget.Hide();
        }

        Plugins.HideAll();
    }

    internal void ShowAllWidgets()
    {
        _settings.WidgetsHidden = false;
        SaveSettings();
        foreach (var widget in _widgets)
        {
            widget.ShowOnDesktop();
        }

        Plugins.ShowAll();
    }

    internal void ToggleWidgetsHidden()
    {
        if (_settings.WidgetsHidden)
        {
            ShowAllWidgets();
        }
        else if (_widgets.Count > 0 || Plugins.Windows.Count > 0)
        {
            HideAllWidgets();
        }
    }

    internal void SaveSettings()
    {
        try
        {
            _store.Save(_settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"Couldn't save widget settings.\n\n{ex.Message}",
                "GlassLedge",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    /// <summary>Creates a widget window; it's shown unless the user has hidden all widgets.</summary>
    private FolderWindow CreateWidget(FolderWindowSettings widgetSettings)
    {
        var widget = new FolderWindow(widgetSettings, this);
        _widgets.Add(widget);
        if (!_settings.WidgetsHidden)
        {
            widget.Show();
        }

        return widget;
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        // Light/dark and transparency switches arrive as General, High Contrast as Accessibility;
        // SystemEvents may raise this off the UI thread.
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color
            or UserPreferenceCategory.VisualStyle or UserPreferenceCategory.Accessibility)
        {
            // Idle priority: WPF refreshes its own cache of the system colours first.
            Dispatcher.InvokeAsync(
                () =>
                {
                    WidgetTheme.Apply(Resources);
                    BrandTheme.Apply(Resources);
                    RefreshWidgetColors();
                },
                System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        // A new wallpaper (or solid desktop colour) changes what custom-coloured glass sits on.
        if (e.Category is UserPreferenceCategory.Desktop or UserPreferenceCategory.Color)
        {
            Dispatcher.InvokeAsync(() =>
            {
                WallpaperColor = Wallpaper.AverageColor();
                RefreshWidgetColors();
            });
        }
    }
}
