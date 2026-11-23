using System.Windows;
using GlassLedge.Core.Models;

namespace GlassLedge.App;

/// <summary>
/// Publishes the GlassLedge brand brushes (see <see cref="BrandPalette"/>) as resources for the dialogs and
/// menus. App-level resources follow the Windows light/dark setting; a dialog also applies them to its own
/// resources so it looks right wherever it is created. Widgets use <see cref="WidgetTheme"/> instead.
/// </summary>
internal static class BrandTheme
{
    public const string PageBrushKey = "BrandPageBrush";
    public const string TextBrushKey = "BrandTextBrush";
    public const string SecondaryTextBrushKey = "BrandSecondaryTextBrush";
    public const string RowBrushKey = "BrandRowBrush";
    public const string HairlineBrushKey = "BrandHairlineBrush";
    public const string FooterBrushKey = "BrandFooterBrush";
    public const string FocusBrushKey = "BrandFocusBrush";
    public const string MenuBrushKey = "BrandMenuBrush";
    public const string OnAmberFocusBrushKey = "BrandOnAmberFocusBrush";

    // The same in light and dark: the primary button is always amber with graphite text.
    public const string AmberBrushKey = "BrandAmberBrush";
    public const string AmberHoverBrushKey = "BrandAmberHoverBrush";
    public const string AmberPressedBrushKey = "BrandAmberPressedBrush";
    public const string OnAmberBrushKey = "BrandOnAmberBrush";

    /// <summary>Applies the palette for the current Windows light/dark setting, or the system colours in High Contrast.</summary>
    public static void Apply(ResourceDictionary resources)
    {
        if (!SystemParameters.HighContrast)
        {
            Apply(resources, BrandPalette.For(WidgetTheme.IsWindowsLightTheme()));
            return;
        }

        var highlightText = WidgetTheme.ToArgb(SystemColors.HighlightTextColor);
        ApplyPalette(resources, BrandPalette.HighContrast(
            WidgetTheme.ToArgb(SystemColors.WindowColor),
            WidgetTheme.ToArgb(SystemColors.WindowTextColor),
            WidgetTheme.ToArgb(SystemColors.HotTrackColor),
            highlightText));

        // The primary button takes the system highlight pair instead of amber and graphite.
        var highlight = WidgetTheme.ToArgb(SystemColors.HighlightColor);
        ApplyAmber(resources, highlight, highlight, highlight, highlightText);
    }

    public static void Apply(ResourceDictionary resources, BrandPalette palette)
    {
        ApplyPalette(resources, palette);
        ApplyAmber(resources, BrandPalette.Amber, BrandPalette.AmberHover, BrandPalette.AmberPressed, BrandPalette.Graphite);
    }

    private static void ApplyPalette(ResourceDictionary resources, BrandPalette palette)
    {
        resources[PageBrushKey] = WidgetTheme.ToBrush(palette.Page);
        resources[TextBrushKey] = WidgetTheme.ToBrush(palette.Text);
        resources[SecondaryTextBrushKey] = WidgetTheme.ToBrush(palette.SecondaryText);
        resources[RowBrushKey] = WidgetTheme.ToBrush(palette.Row);
        resources[HairlineBrushKey] = WidgetTheme.ToBrush(palette.Hairline);
        resources[FooterBrushKey] = WidgetTheme.ToBrush(palette.Footer);
        resources[FocusBrushKey] = WidgetTheme.ToBrush(palette.Focus);
        resources[MenuBrushKey] = WidgetTheme.ToBrush(palette.Menu);
        resources[OnAmberFocusBrushKey] = WidgetTheme.ToBrush(palette.OnAmberFocus);
    }

    private static void ApplyAmber(ResourceDictionary resources, uint fill, uint hover, uint pressed, uint text)
    {
        resources[AmberBrushKey] = WidgetTheme.ToBrush(fill);
        resources[AmberHoverBrushKey] = WidgetTheme.ToBrush(hover);
        resources[AmberPressedBrushKey] = WidgetTheme.ToBrush(pressed);
        resources[OnAmberBrushKey] = WidgetTheme.ToBrush(text);
    }
}
