using GlassLedge.Core.Services;

namespace GlassLedge.Tests;

[Collection("EnvironmentVariables")]
public class SettingsPathsTests
{
    [Fact]
    public void DefaultPath_IsInsideAppDataUnderAppFolder()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var path = SettingsPaths.GetDefaultSettingsFilePath();

        Assert.Equal(Path.Combine(appData, "GlassLedge", "settings.json"), path);
    }
}
