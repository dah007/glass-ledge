namespace GlassLedge.Core.Models;

/// <summary>What the Preferences window shows and returns. Start-with-Windows lives in Windows, not in the settings file.</summary>
public sealed record PreferencesState(bool StartWithWindows, bool OpenFoldersInPane, bool AllowPlugins);
