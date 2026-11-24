using System.IO;
using GlassLedge.App;
using GlassLedge.Core.Models;
using GlassLedge.Core.Services;
using GlassLedge.PluginApi;

namespace GlassLedge.AppTests;

public sealed class PluginHostTests
{
    [Fact]
    public void TheApiVersionTheHostReadsIsTheOneThePluginContractDeclares()
    {
        Assert.Equal(PluginApiInfo.Version, PluginManifestReader.SupportedApiVersion);
    }

    [Theory]
    [InlineData("file:///C:/Windows/System32/calc.exe")]
    [InlineData("ms-settings:display")]
    [InlineData("ftp://example.com/x")]
    public void TryOpenUri_RefusesAnythingButWebAddresses(string address)
    {
        var context = new PluginWidgetContext("p", Guid.NewGuid());

        Assert.False(context.TryOpenUri(new Uri(address)));
    }

    [Fact]
    public void TryOpenUri_RefusesNull()
    {
        Assert.False(new PluginWidgetContext("p", Guid.NewGuid()).TryOpenUri(null!));
    }

    [Fact]
    public void RaiseClosed_NotifiesThePluginAndSurvivesItsMistakes()
    {
        var context = new PluginWidgetContext("p", Guid.NewGuid());
        var calls = 0;
        context.Closed += (_, _) => calls++;
        context.Closed += (_, _) => throw new InvalidOperationException("plugin bug");

        context.RaiseClosed();

        Assert.Equal(1, calls);
    }

    [Fact]
    public void PluginLoader_ReportsAMissingEntryTypeInPlainWords()
    {
        using var temp = new TestSupport.TempDirectory();
        var plugin = PluginFolderHoldingThisAssembly(temp, "No.Such.Type");

        var error = Assert.Throws<PluginLoadException>(() => PluginLoader.Load(plugin));

        Assert.Contains("entryType", error.Message);
    }

    [Fact]
    public void PluginLoader_RefusesATypeThatIsNotAPlugin()
    {
        using var temp = new TestSupport.TempDirectory();
        var plugin = PluginFolderHoldingThisAssembly(temp, typeof(PluginHostTests).FullName!);

        var error = Assert.Throws<PluginLoadException>(() => PluginLoader.Load(plugin));

        Assert.Contains("IWidgetPlugin", error.Message);
    }

    [Fact]
    public void PluginLoader_ReportsAFileThatIsNotAnAssembly()
    {
        using var temp = new TestSupport.TempDirectory();
        File.WriteAllText(temp.Combine("Fake.dll"), "this is not a program");
        var manifest = new PluginManifest("fake", "Fake", "1", null, null, 1, "Fake.dll", "A.B", null);

        var error = Assert.Throws<PluginLoadException>(() => PluginLoader.Load(new DiscoveredPlugin(manifest, temp.Path)));

        Assert.Contains("couldn't be loaded", error.Message);
    }

    // Loads a copy of this test assembly the way a real plugin is loaded, in a folder of its own.
    private static DiscoveredPlugin PluginFolderHoldingThisAssembly(TestSupport.TempDirectory temp, string entryType)
    {
        var source = typeof(PluginHostTests).Assembly.Location;
        var name = Path.GetFileName(source);
        File.Copy(source, temp.Combine(name));
        var manifest = new PluginManifest("host-tests", "Host tests", "1", null, null, 1, name, entryType, null);
        return new DiscoveredPlugin(manifest, temp.Path);
    }
}
