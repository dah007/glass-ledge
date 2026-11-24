using System.Windows;

namespace GlassLedge.PluginApi;

/// <summary>
/// The one interface a plugin implements. The class that does is named by <c>entryType</c> in plugin.json and
/// needs a public parameterless constructor.
/// <para>
/// GlassLedge creates one instance of it the first time the plugin is used, then calls
/// <see cref="CreateBody"/> once for every widget the person adds.
/// </para>
/// </summary>
public interface IWidgetPlugin
{
    /// <summary>
    /// Builds the contents of one widget. GlassLedge draws the glass window around it (the tint, the sheen, the
    /// Color and Glossy choices, dragging, remembering where it sits and the right-click menu), and gives the
    /// element the size from plugin.json.
    /// </summary>
    /// <param name="context">What this one widget may ask of GlassLedge.</param>
    /// <returns>The element to show. It is created and used on the UI thread.</returns>
    FrameworkElement CreateBody(IWidgetContext context);
}
