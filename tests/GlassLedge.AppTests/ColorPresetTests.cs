using GlassLedge.App;
using GlassLedge.Core.Models;
using GlassLedge.Core.Services;

namespace GlassLedge.AppTests;

public sealed class ColorPresetTests
{
    [Fact]
    public void Presets_AreTwelveDistinctNamedBrandColours()
    {
        var presets = ColorPickerWindow.Presets;

        Assert.Equal(12, presets.Length);
        Assert.Equal(presets.Length, presets.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(presets.Length, presets.Select(p => p.Rgb).Distinct().Count());
    }

    [Fact]
    public void Presets_IncludeTheCoreBrandColours()
    {
        var rgb = ColorPickerWindow.Presets.ToDictionary(p => p.Name, p => p.Rgb, StringComparer.Ordinal);

        Assert.Equal(BrandPalette.Amber & 0xFFFFFFu, rgb["Amber"]);
        Assert.Equal(BrandPalette.Dusk & 0xFFFFFFu, rgb["Dusk"]);
        Assert.Equal(BrandPalette.Graphite & 0xFFFFFFu, rgb["Graphite"]);
        Assert.Equal(BrandPalette.Frost & 0xFFFFFFu, rgb["Frost"]);
        Assert.Equal(BrandPalette.Haze & 0xFFFFFFu, rgb["Haze"]);
    }

    [Fact]
    public void Presets_EveryOneGetsReadableWidgetText()
    {
        foreach (var (name, rgb) in ColorPickerWindow.Presets)
        {
            var argb = 0xFF000000 | rgb;
            var palette = WidgetPalette.ForBackground(argb);
            var (_, r, g, b) = ColorMath.Split(argb);
            var (_, tr, tg, tb) = ColorMath.Split(palette.Text);

            var ratio = ColorMath.ContrastRatio(
                ColorMath.RelativeLuminance(r, g, b),
                ColorMath.RelativeLuminance(tr, tg, tb));

            Assert.True(ratio >= 4.5, $"{name}: contrast {ratio:F2}");
        }
    }
}
