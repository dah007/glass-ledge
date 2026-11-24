using GlassLedge.Core.Services;
using GlassLedge.TestSupport;

namespace GlassLedge.Tests;

public sealed class PluginDiscoveryTests
{
    private static string Manifest(string id) =>
        $$"""{ "id": "{{id}}", "name": "{{id}}", "version": "1", "apiVersion": 1, "entry": "P.dll", "entryType": "P.P" }""";

    private static string MakePlugin(string root, string folder, string id, bool withDll = true)
    {
        var dir = Path.Combine(root, folder);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "plugin.json"), Manifest(id));
        if (withDll)
        {
            File.WriteAllText(Path.Combine(dir, "P.dll"), "");
        }

        return dir;
    }

    [Fact]
    public void Find_ReturnsValidPluginsFromEveryRoot()
    {
        using var temp = new TempDirectory();
        var one = temp.Combine("one");
        var two = temp.Combine("two");
        MakePlugin(one, "alpha", "alpha");
        MakePlugin(two, "beta", "beta");

        var result = PluginDiscovery.Find([one, two]);

        Assert.Equal(["alpha", "beta"], result.Plugins.Select(p => p.Manifest.Id).ToArray());
        Assert.Empty(result.Problems);
    }

    [Fact]
    public void Find_IgnoresMissingRootsAndFoldersWithoutAManifest()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Combine("root", "not-a-plugin"));

        var result = PluginDiscovery.Find([temp.Combine("nowhere"), temp.Combine("root")]);

        Assert.Empty(result.Plugins);
        Assert.Empty(result.Problems);
    }

    [Fact]
    public void Find_FirstRootWinsADuplicateId()
    {
        using var temp = new TempDirectory();
        var shipped = temp.Combine("shipped");
        var user = temp.Combine("user");
        var first = MakePlugin(shipped, "dup", "dup");
        MakePlugin(user, "dup", "dup");

        var result = PluginDiscovery.Find([shipped, user]);

        Assert.Equal(first, Assert.Single(result.Plugins).Folder);
        Assert.Contains("found first", Assert.Single(result.Problems).Message);
    }

    [Fact]
    public void Find_SkipsAFolderWhoseNameDiffersFromItsId()
    {
        using var temp = new TempDirectory();
        MakePlugin(temp.Path, "wrong-name", "right-name");

        var result = PluginDiscovery.Find([temp.Path]);

        Assert.Empty(result.Plugins);
        Assert.Contains("must match", Assert.Single(result.Problems).Message);
    }

    [Fact]
    public void Find_SkipsAPluginWhoseDllIsMissing()
    {
        using var temp = new TempDirectory();
        MakePlugin(temp.Path, "unbuilt", "unbuilt", withDll: false);

        var result = PluginDiscovery.Find([temp.Path]);

        Assert.Empty(result.Plugins);
        Assert.Contains("wasn't found", Assert.Single(result.Problems).Message);
    }

    [Fact]
    public void Find_ReportsAnInvalidManifestAndKeepsGoing()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Combine("broken"));
        File.WriteAllText(temp.Combine("broken", "plugin.json"), "{ nope");
        MakePlugin(temp.Path, "good", "good");

        var result = PluginDiscovery.Find([temp.Path]);

        Assert.Equal("good", Assert.Single(result.Plugins).Manifest.Id);
        Assert.Equal("broken", Path.GetFileName(Assert.Single(result.Problems).Folder));
    }
}
