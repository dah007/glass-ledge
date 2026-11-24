using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using GlassLedge.Core.Services;
using GlassLedge.PluginApi;

namespace GlassLedge.App;

/// <summary>Why a plugin couldn't be turned into a running <see cref="IWidgetPlugin"/>.</summary>
internal sealed class PluginLoadException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// Loads a plugin's assembly and makes its <see cref="IWidgetPlugin"/>. This is the only place plugin code
/// is loaded, and it is only called when Preferences > Allow plugins is on.
/// </summary>
internal static class PluginLoader
{
    internal static IWidgetPlugin Load(DiscoveredPlugin plugin)
    {
        var manifest = plugin.Manifest;
        var path = Path.GetFullPath(Path.Combine(plugin.Folder, manifest.Entry));

        Assembly assembly;
        try
        {
            // Each plugin gets its own load context, so two plugins can use different versions of the same
            // library. GlassLedge.PluginApi is the exception: see PluginLoadContext.
            assembly = new PluginLoadContext(path).LoadFromAssemblyPath(path);
        }
        catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or FileNotFoundException)
        {
            throw new PluginLoadException($"\"{manifest.Entry}\" couldn't be loaded: {ex.Message}", ex);
        }

        var type = assembly.GetType(manifest.EntryType, throwOnError: false)
            ?? throw new PluginLoadException($"\"{manifest.EntryType}\" wasn't found in {manifest.Entry}. Check \"entryType\" in plugin.json.");

        if (type.IsAbstract || !typeof(IWidgetPlugin).IsAssignableFrom(type))
        {
            throw new PluginLoadException($"\"{manifest.EntryType}\" doesn't implement IWidgetPlugin (or it is abstract). Was the plugin built against the same GlassLedge.PluginApi?");
        }

        if (type.GetConstructor(Type.EmptyTypes) is null)
        {
            throw new PluginLoadException($"\"{manifest.EntryType}\" needs a public parameterless constructor.");
        }

        return (IWidgetPlugin)Activator.CreateInstance(type)!;
    }
}

/// <summary>
/// A plugin's own place to load assemblies from. Its private dependencies (listed in its .deps.json) come
/// from its own folder; everything else, including WPF and GlassLedge.PluginApi, comes from GlassLedge
/// itself, so the plugin and the app share one copy of each type.
/// </summary>
internal sealed class PluginLoadContext(string pluginPath) : AssemblyLoadContext(Path.GetFileNameWithoutExtension(pluginPath), isCollectible: false)
{
    private static readonly string ApiAssemblyName = typeof(IWidgetPlugin).Assembly.GetName().Name!;

    private readonly AssemblyDependencyResolver _resolver = new(pluginPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Never load a second copy of the plugin contract: the types would look the same but not match.
        if (assemblyName.Name == ApiAssemblyName)
        {
            return null;
        }

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }
}
