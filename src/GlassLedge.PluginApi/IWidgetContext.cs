namespace GlassLedge.PluginApi;

/// <summary>
/// What one widget may ask of GlassLedge. Deliberately small: a plugin that needs more reaches for the
/// normal .NET APIs, but note that plugins are trusted code (see docs/plugins/security.md).
/// </summary>
public interface IWidgetContext
{
    /// <summary>The <c>id</c> from the plugin's plugin.json.</summary>
    string PluginId { get; }

    /// <summary>Identifies this widget. Stable across restarts, so it can key per-widget data.</summary>
    Guid WidgetId { get; }

    /// <summary>
    /// Opens a web address (http or https only) in the person's default browser.
    /// </summary>
    /// <returns>False when the address isn't a web address or the browser couldn't be started.</returns>
    bool TryOpenUri(Uri uri);

    /// <summary>Raised when the widget is removed or plugins are turned off. Release anything you hold.</summary>
    event EventHandler? Closed;
}
