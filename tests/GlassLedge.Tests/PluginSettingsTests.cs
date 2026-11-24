using GlassLedge.Core.Models;
using GlassLedge.Core.Services;
using GlassLedge.TestSupport;

namespace GlassLedge.Tests;

public sealed class PluginSettingsTests
{
    [Fact]
    public void PluginWidgets_RoundTripThroughTheSettingsFile()
    {
        using var temp = new TempDirectory();
        var store = new JsonSettingsStore(temp.Combine("settings.json"));
        var saved = new PluginWidgetSettings
        {
            PluginId = "search-box",
            Left = 12.5,
            Top = 40,
            BackgroundColor = "#80112233",
            GlossEnabled = false,
        };

        store.Save(new AppSettings { PluginWidgets = [saved] });
        var loaded = Assert.Single(store.Load().PluginWidgets);

        Assert.Equal(saved.Id, loaded.Id);
        Assert.Equal("search-box", loaded.PluginId);
        Assert.Equal(12.5, loaded.Left);
        Assert.Equal("#80112233", loaded.BackgroundColor);
        Assert.False(loaded.GlossEnabled);
    }

    [Fact]
    public void NewPluginWidgets_AreGlossyWithNoColour()
    {
        var widget = new PluginWidgetSettings();

        Assert.True(widget.GlossEnabled);
        Assert.Null(widget.BackgroundColor);
        Assert.NotEqual(Guid.Empty, widget.Id);
    }

    [Fact]
    public void PluginFolders_AreBesideTheSettingsFileAndTheExecutable()
    {
        using var temp = new TempDirectory();
        var previous = Environment.GetEnvironmentVariable("GLASSLEDGE_SETTINGS_DIR");
        Environment.SetEnvironmentVariable("GLASSLEDGE_SETTINGS_DIR", temp.Path);
        try
        {
            Assert.Equal(Path.Combine(temp.Path, "plugins"), SettingsPaths.GetUserPluginsFolder());
        }
        finally
        {
            Environment.SetEnvironmentVariable("GLASSLEDGE_SETTINGS_DIR", previous);
        }

        Assert.Equal(Path.Combine("C:\\App", "plugins"), SettingsPaths.GetInstalledPluginsFolder("C:\\App"));
    }
}
