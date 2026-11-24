using System.Windows;
using GlassLedge.PluginApi;

namespace GlassLedge.Plugins.SearchBox;

/// <summary>
/// The plugin's entry point: the class that <c>plugin.json</c> names in <c>entryType</c>.
/// <para>
/// GlassLedge makes one of these when the plugin first loads, then calls <see cref="CreateBody"/> for every
/// search box the person adds. Notice how little there is to do: the glass window, the Color and Glossy
/// choices, dragging, the saved position and the right-click menu all belong to GlassLedge. This class only
/// hands over the contents.
/// </para>
/// </summary>
public sealed class SearchBoxPlugin : IWidgetPlugin
{
    /// <inheritdoc />
    public FrameworkElement CreateBody(IWidgetContext context) => new SearchBoxView(context);
}
