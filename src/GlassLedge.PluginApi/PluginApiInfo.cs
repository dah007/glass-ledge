namespace GlassLedge.PluginApi;

/// <summary>Facts about this version of the plugin contract.</summary>
public static class PluginApiInfo
{
    /// <summary>
    /// The contract's version. A plugin's <c>apiVersion</c> in plugin.json must not be higher than this.
    /// It goes up only when a change would break existing plugins.
    /// </summary>
    public const int Version = 1;
}
