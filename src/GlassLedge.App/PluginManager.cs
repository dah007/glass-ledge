using System.IO;
using GlassLedge.Core.Models;
using GlassLedge.Core.Services;
using GlassLedge.PluginApi;

namespace GlassLedge.App;

/// <summary>
/// Knows which plugins exist, loads them when Allow plugins is on, and owns the plugin widget windows.
/// Finding plugins only reads <c>plugin.json</c> files; no plugin code runs until <see cref="Enabled"/>.
/// </summary>
internal sealed class PluginManager(App app)
{
    private const double NewWidgetOffset = 100;
    private const double NewWidgetCascade = 30;

    private readonly List<PluginWidgetWindow> _windows = [];
    private readonly Dictionary<string, IWidgetPlugin> _instances = new(StringComparer.OrdinalIgnoreCase);
    private PluginDiscoveryResult _discovery = new([], []);

    /// <summary>True while Allow plugins is on and plugin code may run.</summary>
    internal bool Enabled { get; private set; }

    internal IReadOnlyList<PluginWidgetWindow> Windows => _windows;

    /// <summary>The valid plugins found on disk, whether or not plugins are enabled.</summary>
    internal IReadOnlyList<DiscoveredPlugin> Available => _discovery.Plugins;

    /// <summary>Plugin folders that were skipped, and why (a bad plugin.json, a missing .dll, and so on).</summary>
    internal IReadOnlyList<PluginProblem> Problems => _discovery.Problems;

    /// <summary>The folders searched, in priority order: shipped with the app first, then the person's own.</summary>
    internal static IReadOnlyList<string> SearchFolders { get; } =
    [
        SettingsPaths.GetInstalledPluginsFolder(AppContext.BaseDirectory),
        SettingsPaths.GetUserPluginsFolder(),
    ];

    /// <summary>Called once at startup, after the folder widgets exist.</summary>
    internal void Start(bool allowPlugins)
    {
        _discovery = PluginDiscovery.Find(SearchFolders);
        if (allowPlugins)
        {
            Enable();
        }
    }

    /// <summary>Applies a change to Preferences > Allow plugins straight away, with no restart.</summary>
    internal void SetEnabled(bool allowPlugins)
    {
        if (allowPlugins == Enabled)
        {
            return;
        }

        if (allowPlugins)
        {
            _discovery = PluginDiscovery.Find(SearchFolders); // pick up plugins dropped in since startup
            Enable();
        }
        else
        {
            Enabled = false;
            foreach (var window in _windows.ToList())
            {
                window.ClosePluginWindow();
            }

            _windows.Clear();
            _instances.Clear();
        }
    }

    private void Enable()
    {
        Enabled = true;

        // Bring back the widgets that were saved. A plugin that is gone keeps its settings (it may return).
        var failures = new List<string>();
        foreach (var saved in app.Settings.PluginWidgets.ToList())
        {
            Create(saved, failures);
        }

        if (failures.Count > 0)
        {
            app.Notify(string.Join(" ", failures));
        }
    }

    /// <summary>Adds a new widget for a plugin, cascaded like a new folder widget. Returns null if it couldn't start.</summary>
    internal PluginWidgetWindow? Add(string pluginId)
    {
        if (!Enabled)
        {
            return null;
        }

        // A new widget added while the others are hidden would be invisible; bring them all back.
        if (app.WidgetsHidden)
        {
            app.ShowAllWidgets();
        }

        var cascade = NewWidgetCascade * (app.Settings.Windows.Count + app.Settings.PluginWidgets.Count);
        var saved = new PluginWidgetSettings
        {
            PluginId = pluginId,
            Left = NewWidgetOffset + cascade,
            Top = NewWidgetOffset + cascade,
        };

        var failures = new List<string>();
        var window = Create(saved, failures);
        if (window is null)
        {
            if (failures.Count > 0)
            {
                app.Notify(string.Join(" ", failures));
            }

            return null;
        }

        app.Settings.PluginWidgets.Add(saved);
        app.SaveSettings();
        return window;
    }

    /// <summary>Removes a widget for good: from settings and from the screen.</summary>
    internal void Remove(PluginWidgetWindow window)
    {
        app.Settings.PluginWidgets.Remove(window.Settings);
        _windows.Remove(window);
        app.SaveSettings();
        window.ClosePluginWindow();
    }

    private PluginWidgetWindow? Create(PluginWidgetSettings saved, List<string> failures)
    {
        var plugin = Available.FirstOrDefault(found =>
            string.Equals(found.Manifest.Id, saved.PluginId, StringComparison.OrdinalIgnoreCase));
        if (plugin is null)
        {
            return null;
        }

        try
        {
            if (!_instances.TryGetValue(plugin.Manifest.Id, out var instance))
            {
                instance = PluginLoader.Load(plugin);
                _instances[plugin.Manifest.Id] = instance;
            }

            var context = new PluginWidgetContext(plugin.Manifest.Id, saved.Id);
            var body = instance.CreateBody(context);
            var window = new PluginWidgetWindow(saved, plugin.Manifest, body, context, app);
            _windows.Add(window);
            if (!app.WidgetsHidden)
            {
                window.Show();
            }

            return window;
        }
        catch (Exception ex)
        {
            // Plugin code can throw anything. A broken plugin is skipped with a notice; it never takes the app down.
            failures.Add($"The \"{plugin.Manifest.Name}\" plugin couldn't start: {ex.Message}");
            return null;
        }
    }

    internal void HideAll()
    {
        foreach (var window in _windows)
        {
            window.Hide();
        }
    }

    internal void ShowAll()
    {
        foreach (var window in _windows)
        {
            window.ShowOnDesktop();
        }
    }

    internal void RefreshColors()
    {
        foreach (var window in _windows)
        {
            window.RefreshColor();
        }
    }

    /// <summary>Menu labels for the open plugin widgets: the plugin's name, numbered when there are several of one kind.</summary>
    internal IReadOnlyList<string> MenuLabels()
    {
        var totals = _windows.GroupBy(window => window.Manifest.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var labels = new List<string>();
        foreach (var window in _windows)
        {
            var id = window.Manifest.Id;
            seen[id] = seen.GetValueOrDefault(id) + 1;
            labels.Add(totals[id] == 1 ? window.Manifest.Name : $"{window.Manifest.Name} {seen[id]}");
        }

        return labels;
    }
}
