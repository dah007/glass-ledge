using System.Windows;

namespace GlassLedge.PluginApi;

/// <summary>
/// The names of the colours a widget's glass provides. Use them as <c>DynamicResource</c> keys (or with
/// <see cref="Bind"/>) and your contents match the widget: they follow Windows light/dark, High Contrast and
/// the colour the person chose, with no code of your own.
/// </summary>
public static class PluginTheme
{
    /// <summary>Main text colour; readable on the widget whatever its colour.</summary>
    public const string Text = "WidgetTextBrush";

    /// <summary>Quieter text, for hints and captions.</summary>
    public const string SecondaryText = "WidgetSecondaryTextBrush";

    /// <summary>A soft tile fill, for inputs and buttons.</summary>
    public const string Tile = "WidgetTileBrush";

    /// <summary>The tile fill while the mouse is over it.</summary>
    public const string TileHover = "WidgetTileHoverBrush";

    /// <summary>The accent colour, for focus rings and highlights.</summary>
    public const string Accent = "WidgetAccentBrush";

    /// <summary>The widget's rim colour.</summary>
    public const string Border = "WidgetBorderBrush";

    /// <summary>Points a property at one of the names above, so it updates when the theme does.</summary>
    public static void Bind(FrameworkElement element, DependencyProperty property, string resourceKey)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(property);
        element.SetResourceReference(property, resourceKey);
    }
}
