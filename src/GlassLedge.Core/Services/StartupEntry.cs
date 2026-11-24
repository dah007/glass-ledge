namespace GlassLedge.Core.Services;

/// <summary>
/// The per-user "Run" registry entry that starts GlassLedge at sign-in. The installer writes the same value,
/// so the Preferences switch and the installer's startup task stay in step.
/// </summary>
public static class StartupEntry
{
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string ValueName = "GlassLedge";

    /// <summary>The command stored in the Run value: the executable path in quotes.</summary>
    public static string Command(string exePath) => $"\"{exePath}\"";

    /// <summary>True when the Run value points at this executable. A missing value or another path means off.</summary>
    public static bool IsEnabled(object? runValue, string exePath) =>
        runValue is string text && string.Equals(text.Trim(), Command(exePath), StringComparison.OrdinalIgnoreCase);
}
