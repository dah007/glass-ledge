using GlassLedge.Core.Services;

namespace GlassLedge.Tests;

public class WindowsTransparencyTests
{
    [Fact]
    public void RegistryValueZero_MeansTransparencyOff()
    {
        Assert.False(WindowsTheme.IsTransparencyEnabled(0));
        Assert.False(WindowsTheme.IsTransparencyEnabled(0L));
    }

    [Fact]
    public void RegistryValueOne_MeansTransparencyOn()
    {
        Assert.True(WindowsTheme.IsTransparencyEnabled(1));
    }

    [Fact]
    public void MissingOrUnexpectedValue_DefaultsToOn()
    {
        Assert.True(WindowsTheme.IsTransparencyEnabled(null));
        Assert.True(WindowsTheme.IsTransparencyEnabled("unexpected"));
    }
}
