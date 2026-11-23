using GlassLedge.Core.Models;
using GlassLedge.Core.Services;

namespace GlassLedge.Tests;

public class AccessibilityPaletteTests
{
    private static double Contrast(uint a, uint b)
    {
        var (_, ar, ag, ab) = ColorMath.Split(a);
        var (_, br, bg, bb) = ColorMath.Split(b);
        return ColorMath.ContrastRatio(ColorMath.RelativeLuminance(ar, ag, ab), ColorMath.RelativeLuminance(br, bg, bb));
    }

    [Fact]
    public void HighContrast_IsSolidAndUsesOnlyTheSystemColours()
    {
        var palette = WidgetPalette.HighContrast(window: 0x000000, windowText: 0xFFFFFF, hotTrack: 0xFFFF00);

        Assert.Equal(0xFF000000u, palette.Tint);
        Assert.Equal(0xFFFFFFFFu, palette.Text);
        Assert.Equal(0xFFFFFFFFu, palette.SecondaryText);
        Assert.Equal(0xFFFFFFFFu, palette.Border);
        Assert.Equal(0xFFFFFF00u, palette.Accent);
    }

    [Fact]
    public void HighContrast_HasNoGlossOrGlassLayers()
    {
        var palette = WidgetPalette.HighContrast(0xFFFFFF, 0x000000, 0x0000FF);

        Assert.Equal(0u, palette.GlossVeil);
        Assert.Equal(0u, palette.Gloss);
        Assert.Equal(0u, palette.GlossBounce);
        Assert.Equal(0u, palette.GlossStreak);
        Assert.Equal(0u, palette.TileGloss);
    }

    [Theory]
    [InlineData(0x000000u, 0xFFFFFFu, 0xFFFF00u)] // High Contrast Black
    [InlineData(0xFFFFFFu, 0x000000u, 0x0000FFu)] // High Contrast White
    public void HighContrast_TextAndAccentReadOnTheCard(uint window, uint text, uint hotTrack)
    {
        var palette = WidgetPalette.HighContrast(window, text, hotTrack);

        Assert.True(Contrast(palette.Text, palette.Tint) >= 7, "text on the card");
        Assert.True(Contrast(palette.Accent, palette.Tint) >= 3, "hover and focus ring on the card");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Opaque_OnlyMakesTheTintSolid(bool light)
    {
        var palette = WindowsTheme.PaletteFor(light);

        var opaque = palette.Opaque();

        Assert.Equal(0xFFu, ColorMath.Split(opaque.Tint).A);
        Assert.Equal(palette.Tint & 0x00FFFFFFu, opaque.Tint & 0x00FFFFFFu);
        Assert.Equal(palette with { Tint = opaque.Tint }, opaque);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Opaque_ThemeTextKeepsItsContrast(bool light)
    {
        var opaque = WindowsTheme.PaletteFor(light).Opaque();

        Assert.True(Contrast(opaque.Text, opaque.Tint) >= 4.5);
        Assert.True(Contrast(opaque.SecondaryText, opaque.Tint) >= 4.5);
    }

    [Fact]
    public void ForBackground_SolidColour_StaysSolid()
    {
        var palette = WidgetPalette.ForBackground(0xFF3A3F6B, wallpaper: 0xFFFFFFFF);

        Assert.Equal(0xFFu, ColorMath.Split(palette.Tint).A);
        Assert.True(Contrast(palette.Text, palette.Tint) >= 4.5);
    }

    [Fact]
    public void BrandHighContrast_UsesTheSystemColours()
    {
        var palette = BrandPalette.HighContrast(window: 0x000000, windowText: 0xFFFFFF, hotTrack: 0xFFFF00, highlightText: 0x000000);

        Assert.Equal(0xFF000000u, palette.Page);
        Assert.Equal(0xFFFFFFFFu, palette.Text);
        Assert.Equal(0xFFFFFFFFu, palette.Hairline);
        Assert.Equal(0xFFFFFF00u, palette.Focus);
        Assert.Equal(0xFF000000u, palette.Menu);
        Assert.True(Contrast(palette.Text, palette.Page) >= 7);
        Assert.True(Contrast(palette.Focus, palette.Page) >= 3);
    }
}
