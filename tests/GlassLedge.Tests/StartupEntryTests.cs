using GlassLedge.Core.Services;

namespace GlassLedge.Tests;

public sealed class StartupEntryTests
{
    private const string Exe = @"C:\Users\me\AppData\Local\Programs\GlassLedge\GlassLedge.exe";

    [Fact]
    public void Command_QuotesThePath() => Assert.Equal($"\"{Exe}\"", StartupEntry.Command(Exe));

    [Fact]
    public void IsEnabled_WhenValueMatches_IsTrue() =>
        Assert.True(StartupEntry.IsEnabled(StartupEntry.Command(Exe), Exe));

    [Fact]
    public void IsEnabled_IgnoresCaseAndOuterSpaces() =>
        Assert.True(StartupEntry.IsEnabled("  " + StartupEntry.Command(Exe).ToUpperInvariant() + " ", Exe));

    [Fact]
    public void IsEnabled_WhenValueMissing_IsFalse() => Assert.False(StartupEntry.IsEnabled(null, Exe));

    [Fact]
    public void IsEnabled_WhenValuePointsElsewhere_IsFalse() =>
        Assert.False(StartupEntry.IsEnabled("\"D:\\Old\\GlassLedge.exe\"", Exe));

    [Fact]
    public void IsEnabled_WhenValueIsNotText_IsFalse() => Assert.False(StartupEntry.IsEnabled(1, Exe));
}
