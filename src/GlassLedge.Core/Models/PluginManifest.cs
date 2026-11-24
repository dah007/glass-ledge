namespace GlassLedge.Core.Models;

/// <summary>The size of a plugin widget's contents, in device-independent pixels. The glass is added around it.</summary>
public sealed record PluginSize(double Width, double Height);

/// <summary>
/// What a plugin's plugin.json says about it. Read before any plugin code runs. See docs/plugins/manifest.md.
/// </summary>
/// <param name="Id">Lower-case letters, digits and single hyphens; the same as the plugin's folder name.</param>
/// <param name="Name">What menus call the plugin.</param>
/// <param name="Version">The plugin's own version.</param>
/// <param name="Description">One line shown as a tooltip in the Add plugin widget menu.</param>
/// <param name="Author">Who made it.</param>
/// <param name="ApiVersion">The plugin contract the plugin was written for.</param>
/// <param name="Entry">The assembly (a .dll file name, in the plugin's folder).</param>
/// <param name="EntryType">The full name of the class in <paramref name="Entry"/> that implements IWidgetPlugin.</param>
/// <param name="Size">The widget's contents size; a default is used when missing.</param>
public sealed record PluginManifest(
    string Id,
    string Name,
    string Version,
    string? Description,
    string? Author,
    int ApiVersion,
    string Entry,
    string EntryType,
    PluginSize? Size);
