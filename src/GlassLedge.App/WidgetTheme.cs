using System.Windows;
using System.Windows.Media;
using GlassLedge.Core.Models;
using GlassLedge.Core.Services;
using Microsoft.Win32;

namespace GlassLedge.App;

/// <summary>
/// Publishes the widget brushes as resources. App-level resources follow the Windows light/dark
/// setting; a widget with a custom colour puts its own brushes in its window's resources, which
/// DynamicResource lookups find first.
/// </summary>
internal static class WidgetTheme
{
    public const string TintBrushKey = "WidgetTintBrush";
    public const string BorderBrushKey = "WidgetBorderBrush";
    public const string TextBrushKey = "WidgetTextBrush";
    public const string SecondaryTextBrushKey = "WidgetSecondaryTextBrush";
    public const string TileBrushKey = "WidgetTileBrush";
    public const string TileHoverBrushKey = "WidgetTileHoverBrush";
    public const string GlossVeilBrushKey = "WidgetGlossVeilBrush";
    public const string GlossBrushKey = "WidgetGlossBrush";
    public const string GlossStreakBrushKey = "WidgetGlossStreakBrush";
    public const string GlossRimBrushKey = "WidgetGlossRimBrush";
    public const string TileGlossBrushKey = "WidgetTileGlossBrush";
    public const string TileEdgeBrushKey = "WidgetTileEdgeBrush";
    public const string AccentBrushKey = "WidgetAccentBrush";

    private static readonly string[] Keys =
    [
        TintBrushKey, BorderBrushKey, TextBrushKey, SecondaryTextBrushKey, TileBrushKey, TileHoverBrushKey,
        GlossVeilBrushKey, GlossBrushKey, GlossStreakBrushKey, GlossRimBrushKey, TileGlossBrushKey, TileEdgeBrushKey,
        AccentBrushKey,
    ];

    /// <summary>
    /// Applies the palette for the current Windows settings: High Contrast colours when that mode is on,
    /// otherwise the light or dark glass, made opaque when Windows transparency effects are off.
    /// </summary>
    public static void Apply(ResourceDictionary resources) => Apply(resources, CurrentPalette());

    internal static WidgetPalette CurrentPalette()
    {
        if (SystemParameters.HighContrast)
        {
            return WidgetPalette.HighContrast(
                ToArgb(SystemColors.WindowColor),
                ToArgb(SystemColors.WindowTextColor),
                ToArgb(SystemColors.HotTrackColor));
        }

        var palette = WindowsTheme.PaletteFor(IsWindowsLightTheme());
        return IsTransparencyEnabled() ? palette : palette.Opaque();
    }

    /// <summary>True when Windows apps are set to light mode (the default when the setting is missing).</summary>
    public static bool IsWindowsLightTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(WindowsTheme.PersonalizeKeyPath);
        return WindowsTheme.IsLightTheme(key?.GetValue(WindowsTheme.AppsUseLightThemeValueName));
    }

    /// <summary>True unless "Transparency effects" is switched off in Windows settings.</summary>
    public static bool IsTransparencyEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(WindowsTheme.PersonalizeKeyPath);
        return WindowsTheme.IsTransparencyEnabled(key?.GetValue(WindowsTheme.EnableTransparencyValueName));
    }

    internal static uint ToArgb(Color color) => ColorMath.ToArgb(color.A, color.R, color.G, color.B);

    public static void Apply(ResourceDictionary resources, WidgetPalette palette)
    {
        resources[TintBrushKey] = ToBrush(palette.Tint);
        resources[BorderBrushKey] = ToBrush(palette.Border);
        resources[TextBrushKey] = ToBrush(palette.Text);
        resources[SecondaryTextBrushKey] = ToBrush(palette.SecondaryText);
        resources[TileBrushKey] = ToBrush(palette.Tile);
        resources[TileHoverBrushKey] = ToBrush(palette.TileHover);
        resources[GlossVeilBrushKey] = ToBrush(palette.GlossVeil);
        resources[GlossBrushKey] = CreateGlossBrush(palette.Gloss, palette.GlossBounce);
        resources[GlossStreakBrushKey] = CreateGlossStreakBrush(palette.GlossStreak);
        resources[GlossRimBrushKey] = CreateGlossRimBrush(palette.GlossRim, palette.GlossRimShade);
        resources[TileGlossBrushKey] = CreateTileGlossBrush(palette.TileGloss);
        resources[TileEdgeBrushKey] = ToBrush(palette.TileEdge);
        resources[AccentBrushKey] = ToBrush(palette.Accent);
    }

    /// <summary>
    /// Top to bottom: a glare that is strongest at the top edge, drops off fast and is gone before the
    /// middle, then clear glass, then a faint light bouncing back up from the bottom edge.
    /// </summary>
    public static LinearGradientBrush CreateGlossBrush(uint glareArgb, uint bounceArgb)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
        brush.GradientStops.Add(new GradientStop(ToColor(glareArgb), 0));
        brush.GradientStops.Add(new GradientStop(ToColor(glareArgb, 0.32), 0.12));
        brush.GradientStops.Add(new GradientStop(ToColor(glareArgb, 0), 0.46));
        brush.GradientStops.Add(new GradientStop(ToColor(bounceArgb, 0), 0.86));
        brush.GradientStops.Add(new GradientStop(ToColor(bounceArgb), 1));
        brush.Freeze();
        return brush;
    }

    /// <summary>A soft diagonal streak across the pane, falling gently from left to right.</summary>
    public static LinearGradientBrush CreateGlossStreakBrush(uint peakArgb)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0.2), EndPoint = new Point(1, 0.8) };
        brush.GradientStops.Add(new GradientStop(ToColor(peakArgb, 0), 0.30));
        brush.GradientStops.Add(new GradientStop(ToColor(peakArgb), 0.38));
        brush.GradientStops.Add(new GradientStop(ToColor(peakArgb, 0), 0.52));
        brush.Freeze();
        return brush;
    }

    /// <summary>
    /// The outline: bright along the top edge, fading down the sides, dark along the bottom edge.
    /// Light on one side and dark on the other, so the edge shows against any colour.
    /// </summary>
    public static LinearGradientBrush CreateGlossRimBrush(uint highlightArgb, uint shadeArgb)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
        brush.GradientStops.Add(new GradientStop(ToColor(highlightArgb), 0));
        brush.GradientStops.Add(new GradientStop(ToColor(highlightArgb, 0.35), 0.2));
        brush.GradientStops.Add(new GradientStop(ToColor(shadeArgb, 0.35), 0.8));
        brush.GradientStops.Add(new GradientStop(ToColor(shadeArgb), 1));
        brush.Freeze();
        return brush;
    }

    /// <summary>Each icon tile's own glare: strongest at its top edge, gone just past its middle.</summary>
    public static LinearGradientBrush CreateTileGlossBrush(uint peakArgb)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
        brush.GradientStops.Add(new GradientStop(ToColor(peakArgb), 0));
        brush.GradientStops.Add(new GradientStop(ToColor(peakArgb, 0), 0.55));
        brush.Freeze();
        return brush;
    }

    /// <summary>Removes widget brushes so lookups fall back to the app-level (theme) ones.</summary>
    public static void Clear(ResourceDictionary resources)
    {
        foreach (var key in Keys)
        {
            resources.Remove(key);
        }
    }

    public static SolidColorBrush ToBrush(uint argb)
    {
        var brush = new SolidColorBrush(ToColor(argb));
        brush.Freeze();
        return brush;
    }

    /// <summary>The colour with its alpha multiplied by <paramref name="alphaScale"/> (0 to 1).</summary>
    private static Color ToColor(uint argb, double alphaScale = 1)
    {
        var (a, r, g, b) = ColorMath.Split(argb);
        return Color.FromArgb((byte)Math.Round(a * alphaScale), r, g, b);
    }
}
