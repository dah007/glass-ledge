namespace GlassLedge.Core.Services;

public static class SettingsPaths
{
    public const string AppFolderName = "GlassLedge";
    public const string FileName = "settings.json";

    /// <summary>The folder name, inside the app folder and beside the executable, that holds plugins.</summary>
    public const string PluginsFolderName = "plugins";

    /// <summary>%APPDATA%\GlassLedge\settings.json, unless overridden by GLASSLEDGE_SETTINGS_DIR (test isolation only).</summary>
    public static string GetDefaultSettingsFilePath()
    {
        var overrideDir = Environment.GetEnvironmentVariable("GLASSLEDGE_SETTINGS_DIR");
        if (!string.IsNullOrEmpty(overrideDir))
        {
            return Path.Combine(overrideDir, FileName);
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppFolderName,
            FileName);
    }

    /// <summary>
    /// Where a person drops plugins of their own: %APPDATA%\GlassLedge\plugins (beside the settings file, so it
    /// follows GLASSLEDGE_SETTINGS_DIR too).
    /// </summary>
    public static string GetUserPluginsFolder() =>
        Path.Combine(Path.GetDirectoryName(GetDefaultSettingsFilePath())!, PluginsFolderName);

    /// <summary>Where plugins that ship with the app live: a <c>plugins</c> folder beside the executable.</summary>
    public static string GetInstalledPluginsFolder(string appBaseDirectory) =>
        Path.Combine(appBaseDirectory, PluginsFolderName);
}
