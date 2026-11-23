using System.Windows;
using System.Windows.Media;
using GlassLedge.App;
using GlassLedge.Core.Models;

namespace GlassLedge.AppTests;

public sealed class WidgetThemeAccessibilityTests
{
    [Fact]
    public void Apply_HighContrastPalette_PublishesSolidSystemColours()
    {
        var resources = new ResourceDictionary();

        WidgetTheme.Apply(resources, WidgetPalette.HighContrast(window: 0x000000, windowText: 0xFFFFFF, hotTrack: 0xFFFF00));

        Assert.Equal(Color.FromArgb(0xFF, 0, 0, 0), ((SolidColorBrush)resources[WidgetTheme.TintBrushKey]).Color);
        Assert.Equal(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), ((SolidColorBrush)resources[WidgetTheme.TextBrushKey]).Color);
        Assert.Equal(Color.FromArgb(0xFF, 0xFF, 0xFF, 0), ((SolidColorBrush)resources[WidgetTheme.AccentBrushKey]).Color);
    }

    [Fact]
    public void ToArgb_RoundTripsAColour()
    {
        Assert.Equal(0x80112233u, WidgetTheme.ToArgb(Color.FromArgb(0x80, 0x11, 0x22, 0x33)));
    }

    [Fact]
    public void CurrentPalette_IsOneOfTheKnownPalettes()
    {
        // Whatever the machine's settings are (light, dark, opaque or High Contrast), the result is solid or
        // glass with readable text; this guards the registry and SystemParameters reads against throwing.
        var palette = WidgetTheme.CurrentPalette();

        Assert.NotEqual(0u, palette.Text);
        Assert.NotEqual(0u, palette.Tint);
    }
}
