using System.ComponentModel;
using System.Diagnostics;
using GlassLedge.PluginApi;

namespace GlassLedge.App;

/// <summary>GlassLedge's side of <see cref="IWidgetContext"/>: what one plugin widget is allowed to ask of the app.</summary>
internal sealed class PluginWidgetContext(string pluginId, Guid widgetId) : IWidgetContext
{
    public string PluginId { get; } = pluginId;

    public Guid WidgetId { get; } = widgetId;

    public event EventHandler? Closed;

    public bool TryOpenUri(Uri uri)
    {
        // Web addresses only: a plugin can't use this to start a program or open a file.
        if (uri is null || !uri.IsAbsoluteUri || uri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    /// <summary>Tells the plugin its widget is gone. A plugin's handler must not be able to break shutdown.</summary>
    internal void RaiseClosed()
    {
        try
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            // Plugin code ran in a handler; whatever it threw, the widget is closing regardless.
        }
    }
}
