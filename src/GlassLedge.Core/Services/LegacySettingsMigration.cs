namespace GlassLedge.Core.Services;

/// <summary>
/// One-time move of saved widgets from the folder used before the app was renamed
/// (%APPDATA%\FloatingWindowOrganizer) to the current one (%APPDATA%\GlassLedge). The old folder is
/// left untouched, so nothing is lost if something goes wrong or someone goes back to an old build.
/// </summary>
public static class LegacySettingsMigration
{
    public const string LegacyAppFolderName = "FloatingWindowOrganizer";

    /// <summary>%APPDATA%\FloatingWindowOrganizer\settings.json, where the app saved settings before the rename.</summary>
    public static string GetLegacySettingsFilePath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            LegacyAppFolderName,
            SettingsPaths.FileName);

    /// <summary>
    /// Copies <paramref name="legacySettingsFile"/> to <paramref name="newSettingsFile"/> when the old file exists
    /// and the new one does not. Returns true when a copy was made. Never overwrites existing settings.
    /// </summary>
    public static bool Migrate(string legacySettingsFile, string newSettingsFile)
    {
        if (!File.Exists(legacySettingsFile) || File.Exists(newSettingsFile))
        {
            return false;
        }

        var directory = Path.GetDirectoryName(newSettingsFile);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Copy to a temporary name first so a crash part-way can't leave a half-written settings.json behind.
        var temporaryFile = newSettingsFile + ".migrating";
        File.Copy(legacySettingsFile, temporaryFile, overwrite: true);
        File.Move(temporaryFile, newSettingsFile);
        return true;
    }

    /// <summary>
    /// Migrates the real per-user settings. Does nothing while GLASSLEDGE_SETTINGS_DIR is set (test isolation),
    /// and never throws: a failed migration just means the app starts like a first run.
    /// </summary>
    public static bool MigrateDefault()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GLASSLEDGE_SETTINGS_DIR")))
        {
            return false;
        }

        try
        {
            return Migrate(GetLegacySettingsFilePath(), SettingsPaths.GetDefaultSettingsFilePath());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
