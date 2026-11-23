using System.Windows;
using System.Windows.Media;
using GlassLedge.App;
using GlassLedge.Core.Models;
using GlassLedge.Core.Services;

namespace GlassLedge.AppTests;

public sealed class BrandThemeTests
{
    [Fact]
    public void Apply_SetsEveryBrushFromThePalette()
    {
        var resources = new ResourceDictionary();

        BrandTheme.Apply(resources, BrandPalette.Dark);

        AssertBrush(resources, BrandTheme.PageBrushKey, BrandPalette.Dark.Page);
        AssertBrush(resources, BrandTheme.TextBrushKey, BrandPalette.Dark.Text);
        AssertBrush(resources, BrandTheme.SecondaryTextBrushKey, BrandPalette.Dark.SecondaryText);
        AssertBrush(resources, BrandTheme.RowBrushKey, BrandPalette.Dark.Row);
        AssertBrush(resources, BrandTheme.HairlineBrushKey, BrandPalette.Dark.Hairline);
        AssertBrush(resources, BrandTheme.FooterBrushKey, BrandPalette.Dark.Footer);
        AssertBrush(resources, BrandTheme.FocusBrushKey, BrandPalette.Dark.Focus);
        AssertBrush(resources, BrandTheme.MenuBrushKey, BrandPalette.Dark.Menu);
        AssertBrush(resources, BrandTheme.OnAmberFocusBrushKey, BrandPalette.Dark.OnAmberFocus);
    }

    [Fact]
    public void Apply_LightAndDarkDiffer()
    {
        var light = new ResourceDictionary();
        var dark = new ResourceDictionary();

        BrandTheme.Apply(light, BrandPalette.Light);
        BrandTheme.Apply(dark, BrandPalette.Dark);

        AssertBrush(light, BrandTheme.PageBrushKey, BrandPalette.Frost);
        AssertBrush(dark, BrandTheme.PageBrushKey, BrandPalette.Dusk);
        Assert.NotEqual(
            Assert.IsType<SolidColorBrush>(light[BrandTheme.TextBrushKey]).Color,
            Assert.IsType<SolidColorBrush>(dark[BrandTheme.TextBrushKey]).Color);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Apply_PrimaryButtonIsAmberWithGraphiteText_InBothThemes(bool light)
    {
        var resources = new ResourceDictionary();

        BrandTheme.Apply(resources, BrandPalette.For(light));

        AssertBrush(resources, BrandTheme.AmberBrushKey, BrandPalette.Amber);
        AssertBrush(resources, BrandTheme.AmberHoverBrushKey, BrandPalette.AmberHover);
        AssertBrush(resources, BrandTheme.AmberPressedBrushKey, BrandPalette.AmberPressed);
        AssertBrush(resources, BrandTheme.OnAmberBrushKey, BrandPalette.Graphite);
    }

    private static void AssertBrush(ResourceDictionary resources, string key, uint expectedArgb)
    {
        var brush = Assert.IsType<SolidColorBrush>(resources[key]);
        var (a, r, g, b) = ColorMath.Split(expectedArgb);
        Assert.Equal(Color.FromArgb(a, r, g, b), brush.Color);
    }
}
