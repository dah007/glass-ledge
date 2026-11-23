using GlassLedge.Core.Models;
using GlassLedge.Core.Services;

namespace GlassLedge.Tests;

public sealed class BrandPaletteTests
{
    public static TheoryData<BrandPalette> Palettes => new() { BrandPalette.Light, BrandPalette.Dark };

    [Theory]
    [MemberData(nameof(Palettes))]
    public void Text_IsReadableOnThePage(BrandPalette palette)
    {
        Assert.True(Contrast(palette.Text, palette.Page) >= 4.5);
        Assert.True(Contrast(palette.SecondaryText, palette.Page) >= 4.5);
    }

    [Theory]
    [MemberData(nameof(Palettes))]
    public void Text_IsReadableOnMenus(BrandPalette palette)
    {
        Assert.True(Contrast(palette.Text, palette.Menu) >= 4.5);
    }

    [Theory]
    [MemberData(nameof(Palettes))]
    public void FocusRing_IsVisibleOnThePage(BrandPalette palette)
    {
        // WCAG asks 3:1 for the boundary of a user-interface component.
        Assert.True(Contrast(palette.Focus, palette.Page) >= 3);
    }

    [Theory]
    [MemberData(nameof(Palettes))]
    public void SolidColors_AreOpaque(BrandPalette palette)
    {
        foreach (var argb in new[] { palette.Page, palette.Text, palette.SecondaryText, palette.Focus, palette.Menu, palette.OnAmberFocus })
        {
            Assert.Equal(0xFFu, argb >> 24);
        }
    }

    [Theory]
    [MemberData(nameof(Palettes))]
    public void Overlays_AreTranslucent(BrandPalette palette)
    {
        foreach (var argb in new[] { palette.Row, palette.Hairline, palette.Footer })
        {
            Assert.InRange(argb >> 24, 1u, 0xFEu);
        }
    }

    [Fact]
    public void PrimaryButton_AmberWithGraphiteText_IsReadable()
    {
        Assert.True(Contrast(BrandPalette.Graphite, BrandPalette.Amber) >= 4.5);
        Assert.True(Contrast(BrandPalette.Graphite, BrandPalette.AmberHover) >= 4.5);
        Assert.True(Contrast(BrandPalette.Graphite, BrandPalette.AmberPressed) >= 4.5);
    }

    [Theory]
    [MemberData(nameof(Palettes))]
    public void FocusRingOnAmberButton_IsVisible(BrandPalette palette)
    {
        Assert.True(Contrast(palette.OnAmberFocus, BrandPalette.Amber) >= 3);
    }

    [Fact]
    public void BrandColors_MatchTheAppIdentity()
    {
        Assert.Equal(0xFFFFB020u, BrandPalette.Amber);
        Assert.Equal(0xFF1C1C1Eu, BrandPalette.Graphite);
    }

    [Fact]
    public void For_PicksByWindowsTheme()
    {
        Assert.Same(BrandPalette.Light, BrandPalette.For(light: true));
        Assert.Same(BrandPalette.Dark, BrandPalette.For(light: false));
        Assert.NotEqual(BrandPalette.Light.Page, BrandPalette.Dark.Page);
    }

    private static double Contrast(uint a, uint b)
    {
        var (_, ar, ag, ab) = ColorMath.Split(a);
        var (_, br, bg, bb) = ColorMath.Split(b);
        return ColorMath.ContrastRatio(
            ColorMath.RelativeLuminance(ar, ag, ab),
            ColorMath.RelativeLuminance(br, bg, bb));
    }
}
