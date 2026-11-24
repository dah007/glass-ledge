namespace GlassLedge.Core.Models;

/// <summary>
/// Root settings document saved to disk.
/// </summary>
public sealed class AppSettings
{
    public const int CurrentSchemaVersion = 1;

    /// <summary>Lets future versions migrate older settings files.</summary>
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public List<FolderWindowSettings> Windows { get; set; } = [];

    /// <summary>True when the user chose "Hide all" from the tray; widgets stay hidden across restarts.</summary>
    public bool WidgetsHidden { get; set; }

    /// <summary>Preferences: clicking a folder tile opens it in a GlassLedge window instead of File Explorer.</summary>
    public bool OpenFoldersInPane { get; set; }

    /// <summary>Preferences: plugin code may run. Off by default, because a plugin is a program.</summary>
    public bool AllowPlugins { get; set; }
}
