using GlassLedge.Core.Models;

namespace GlassLedge.Core.Services;

/// <summary>A valid plugin folder: its manifest and where it lives.</summary>
public sealed record DiscoveredPlugin(PluginManifest Manifest, string Folder);

/// <summary>A folder that looked like a plugin but was skipped, and the reason, in plain words.</summary>
public sealed record PluginProblem(string Folder, string Message);

public sealed record PluginDiscoveryResult(IReadOnlyList<DiscoveredPlugin> Plugins, IReadOnlyList<PluginProblem> Problems);

/// <summary>
/// Finds plugins on disk. A plugin is a folder, directly inside a search folder, that holds a plugin.json.
/// Only manifests are read; no plugin code runs. A bad plugin is reported and skipped, never fatal.
/// </summary>
public static class PluginDiscovery
{
    /// <summary>
    /// Looks in each root, in order. When two folders declare the same id the first one found wins and the
    /// later one is reported, so a plugin that ships with the app can't be silently replaced by a look-alike.
    /// Missing roots are fine (most people have no folder of their own yet).
    /// </summary>
    public static PluginDiscoveryResult Find(IEnumerable<string> roots)
    {
        var plugins = new List<DiscoveredPlugin>();
        var problems = new List<PluginProblem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            IEnumerable<string> folders;
            try
            {
                folders = Directory.EnumerateDirectories(root).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                problems.Add(new PluginProblem(root, $"The plugins folder couldn't be read: {ex.Message}"));
                continue;
            }

            foreach (var folder in folders)
            {
                var manifestPath = Path.Combine(folder, PluginManifestReader.FileName);
                if (!File.Exists(manifestPath))
                {
                    continue; // just some other folder
                }

                Inspect(folder, manifestPath, plugins, problems, seen);
            }
        }

        return new PluginDiscoveryResult(plugins, problems);
    }

    private static void Inspect(
        string folder,
        string manifestPath,
        List<DiscoveredPlugin> plugins,
        List<PluginProblem> problems,
        HashSet<string> seen)
    {
        string json;
        try
        {
            json = File.ReadAllText(manifestPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            problems.Add(new PluginProblem(folder, $"{PluginManifestReader.FileName} couldn't be read: {ex.Message}"));
            return;
        }

        var result = PluginManifestReader.Parse(json);
        if (result.Manifest is not { } manifest)
        {
            problems.Add(new PluginProblem(folder, string.Join(" ", result.Errors)));
            return;
        }

        var folderName = Path.GetFileName(folder);
        if (!string.Equals(folderName, manifest.Id, StringComparison.OrdinalIgnoreCase))
        {
            problems.Add(new PluginProblem(folder, $"The folder is named \"{folderName}\" but the plugin's id is \"{manifest.Id}\". They must match."));
            return;
        }

        if (!File.Exists(Path.Combine(folder, manifest.Entry)))
        {
            problems.Add(new PluginProblem(folder, $"\"{manifest.Entry}\" wasn't found in the plugin's folder. Was the plugin built?"));
            return;
        }

        if (!seen.Add(manifest.Id))
        {
            problems.Add(new PluginProblem(folder, $"Another plugin with the id \"{manifest.Id}\" was found first, so this one was skipped."));
            return;
        }

        plugins.Add(new DiscoveredPlugin(manifest, folder));
    }
}
