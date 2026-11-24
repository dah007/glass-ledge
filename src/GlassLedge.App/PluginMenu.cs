using System.Windows;
using System.Windows.Controls;

namespace GlassLedge.App;

/// <summary>The "Add plugin widget" submenu, shared by the tray menu and every widget's right-click menu.</summary>
internal static class PluginMenu
{
    /// <summary>
    /// Fills the submenu just before it is shown: one entry per plugin found when plugins are on; when they
    /// are off, a single entry that opens Preferences, so the way to turn them on is right there.
    /// </summary>
    internal static void Fill(MenuItem parent, App app)
    {
        parent.Items.Clear();

        if (!app.Plugins.Enabled)
        {
            parent.Items.Add(Entry("Turn on plugins in Preferences…", null, (_, _) => app.ShowPreferences()));
            return;
        }

        if (app.Plugins.Available.Count == 0)
        {
            parent.Items.Add(new MenuItem { Header = "No plugins found", IsEnabled = false });
            return;
        }

        foreach (var plugin in app.Plugins.Available)
        {
            var id = plugin.Manifest.Id;
            parent.Items.Add(Entry(plugin.Manifest.Name, plugin.Manifest.Description, (_, _) => app.Plugins.Add(id)));
        }
    }

    internal static MenuItem Entry(string text, string? toolTip, RoutedEventHandler onClick)
    {
        // Underscores mark access keys in WPF menu headers; double them so names show as written.
        var item = new MenuItem { Header = text.Replace("_", "__", StringComparison.Ordinal), ToolTip = toolTip };
        item.Click += onClick;
        return item;
    }
}
