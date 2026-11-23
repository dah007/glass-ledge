using GlassLedge.Core.Services;
using GlassLedge.TestSupport;

namespace GlassLedge.Tests;

[Collection("EnvironmentVariables")]
public class LegacySettingsMigrationTests
{
    [Fact]
    public void Migrate_CopiesLegacyFile_WhenNewOneIsMissing()
    {
        using var temp = new TempDirectory();
        var legacy = temp.Combine("old", "settings.json");
        var current = temp.Combine("new", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(legacy)!);
        File.WriteAllText(legacy, "{\"SchemaVersion\":1}");

        var migrated = LegacySettingsMigration.Migrate(legacy, current);

        Assert.True(migrated);
        Assert.Equal("{\"SchemaVersion\":1}", File.ReadAllText(current));
    }

    [Fact]
    public void Migrate_LeavesLegacyFileInPlace()
    {
        using var temp = new TempDirectory();
        var legacy = temp.Combine("old", "settings.json");
        var current = temp.Combine("new", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(legacy)!);
        File.WriteAllText(legacy, "legacy");

        LegacySettingsMigration.Migrate(legacy, current);

        Assert.True(File.Exists(legacy));
        Assert.Equal("legacy", File.ReadAllText(legacy));
    }

    [Fact]
    public void Migrate_NeverOverwritesExistingSettings()
    {
        using var temp = new TempDirectory();
        var legacy = temp.Combine("old", "settings.json");
        var current = temp.Combine("new", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(legacy)!);
        Directory.CreateDirectory(Path.GetDirectoryName(current)!);
        File.WriteAllText(legacy, "legacy");
        File.WriteAllText(current, "current");

        var migrated = LegacySettingsMigration.Migrate(legacy, current);

        Assert.False(migrated);
        Assert.Equal("current", File.ReadAllText(current));
    }

    [Fact]
    public void Migrate_DoesNothing_WhenThereIsNoLegacyFile()
    {
        using var temp = new TempDirectory();
        var legacy = temp.Combine("old", "settings.json");
        var current = temp.Combine("new", "settings.json");

        var migrated = LegacySettingsMigration.Migrate(legacy, current);

        Assert.False(migrated);
        Assert.False(File.Exists(current));
        Assert.False(Directory.Exists(temp.Combine("new")));
    }

    [Fact]
    public void Migrate_LeavesNoTemporaryFileBehind()
    {
        using var temp = new TempDirectory();
        var legacy = temp.Combine("old", "settings.json");
        var current = temp.Combine("new", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(legacy)!);
        File.WriteAllText(legacy, "x");

        LegacySettingsMigration.Migrate(legacy, current);

        Assert.False(File.Exists(current + ".migrating"));
    }

    [Fact]
    public void LegacyPath_IsInsideAppDataUnderOldFolderName()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        Assert.Equal(
            Path.Combine(appData, "FloatingWindowOrganizer", "settings.json"),
            LegacySettingsMigration.GetLegacySettingsFilePath());
    }

    [Fact]
    public void MigrateDefault_DoesNothing_WhenSettingsDirIsOverridden()
    {
        using var temp = new TempDirectory();
        var previous = Environment.GetEnvironmentVariable("GLASSLEDGE_SETTINGS_DIR");
        Environment.SetEnvironmentVariable("GLASSLEDGE_SETTINGS_DIR", temp.Path);
        try
        {
            Assert.False(LegacySettingsMigration.MigrateDefault());
            Assert.False(File.Exists(Path.Combine(temp.Path, "settings.json")));
        }
        finally
        {
            Environment.SetEnvironmentVariable("GLASSLEDGE_SETTINGS_DIR", previous);
        }
    }
}
