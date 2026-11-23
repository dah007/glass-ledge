namespace GlassLedge.Core.Services;

public static class SettingsPaths
{
    public const string AppFolderName = "GlassLedge";
    public const string FileName = "settings.json";

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
}
