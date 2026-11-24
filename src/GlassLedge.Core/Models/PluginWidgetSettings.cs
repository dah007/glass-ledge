namespace GlassLedge.Core.Models;

/// <summary>
/// What GlassLedge remembers about one plugin widget on the desktop. The plugin's own contents are not saved
/// here; this is only the part the app owns: which plugin it is, where it sits, and its glass.
/// </summary>
public sealed class PluginWidgetSettings
{
    /// <summary>Identifies this widget (one plugin can have several).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The <c>id</c> from the plugin's plugin.json.</summary>
    public string PluginId { get; set; } = "";

    public double Left { get; set; }

    public double Top { get; set; }

    /// <summary>The chosen widget colour as #AARRGGBB, or null for the Windows theme colours.</summary>
    public string? BackgroundColor { get; set; }

    public bool GlossEnabled { get; set; } = true;
}
