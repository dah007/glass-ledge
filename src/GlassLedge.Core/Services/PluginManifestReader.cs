using System.Text.Json;
using System.Text.RegularExpressions;
using GlassLedge.Core.Models;

namespace GlassLedge.Core.Services;

/// <summary>The outcome of reading a plugin.json: a manifest, or every reason it was refused.</summary>
public sealed record PluginManifestResult(PluginManifest? Manifest, IReadOnlyList<string> Errors)
{
    public bool IsValid => Manifest is not null;
}

/// <summary>
/// Reads and checks plugin.json. Comments and trailing commas are allowed. Nothing here runs plugin code, so
/// it is safe to call for plugins that are switched off. All the rules are listed in docs/plugins/manifest.md.
/// </summary>
public static partial class PluginManifestReader
{
    /// <summary>The newest plugin contract this version of GlassLedge understands.</summary>
    public const int SupportedApiVersion = 1;

    /// <summary>The file a plugin folder must contain.</summary>
    public const string FileName = "plugin.json";

    public const double MinWidth = 120;
    public const double MaxWidth = 800;
    public const double MinHeight = 24;
    public const double MaxHeight = 400;

    /// <summary>The size used when plugin.json has no "size".</summary>
    public static PluginSize DefaultSize { get; } = new(320, 40);

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex IdPattern();

    public static PluginManifestResult Parse(string json)
    {
        var errors = new List<string>();

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });
        }
        catch (JsonException ex)
        {
            return new PluginManifestResult(null, [$"{FileName} isn't valid JSON: {ex.Message}"]);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return new PluginManifestResult(null, [$"{FileName} must be a JSON object."]);
            }

            var id = ReadString(root, "id", required: true, errors);
            var name = ReadString(root, "name", required: true, errors);
            var version = ReadString(root, "version", required: true, errors);
            var description = ReadString(root, "description", required: false, errors);
            var author = ReadString(root, "author", required: false, errors);
            var entry = ReadString(root, "entry", required: true, errors);
            var entryType = ReadString(root, "entryType", required: true, errors);

            if (id is not null && !IdPattern().IsMatch(id))
            {
                errors.Add("\"id\" may only use lower-case letters, digits and single hyphens (for example \"search-box\").");
            }

            if (entry is not null && !IsPlainDllName(entry))
            {
                errors.Add("\"entry\" must be the name of a .dll file in the plugin's folder, with no path.");
            }

            var apiVersion = 0;
            if (!root.TryGetProperty("apiVersion", out var apiElement) || !apiElement.TryGetInt32(out apiVersion))
            {
                errors.Add("\"apiVersion\" is required and must be a whole number.");
            }
            else if (apiVersion < 1)
            {
                errors.Add("\"apiVersion\" must be 1 or more.");
            }
            else if (apiVersion > SupportedApiVersion)
            {
                errors.Add($"This plugin needs plugin API version {apiVersion}, but this GlassLedge understands up to {SupportedApiVersion}. Update GlassLedge.");
            }

            var size = ReadSize(root, errors);

            if (errors.Count > 0)
            {
                return new PluginManifestResult(null, errors);
            }

            return new PluginManifestResult(
                new PluginManifest(id!, name!, version!, description, author, apiVersion, entry!, entryType!, size),
                []);
        }
    }

    private static string? ReadString(JsonElement root, string property, bool required, List<string> errors)
    {
        if (!root.TryGetProperty(property, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            if (required)
            {
                errors.Add($"\"{property}\" is required.");
            }

            return null;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            errors.Add($"\"{property}\" must be text.");
            return null;
        }

        var text = element.GetString()?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            if (required)
            {
                errors.Add($"\"{property}\" can't be empty.");
            }

            return null;
        }

        return text;
    }

    private static PluginSize? ReadSize(JsonElement root, List<string> errors)
    {
        if (!root.TryGetProperty("size", out var element) || element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty("width", out var widthElement) || !widthElement.TryGetDouble(out var width)
            || !element.TryGetProperty("height", out var heightElement) || !heightElement.TryGetDouble(out var height))
        {
            errors.Add("\"size\" must look like { \"width\": 320, \"height\": 40 }.");
            return null;
        }

        if (width < MinWidth || width > MaxWidth)
        {
            errors.Add($"\"size.width\" must be between {MinWidth} and {MaxWidth}.");
        }

        if (height < MinHeight || height > MaxHeight)
        {
            errors.Add($"\"size.height\" must be between {MinHeight} and {MaxHeight}.");
        }

        return new PluginSize(width, height);
    }

    private static bool IsPlainDllName(string entry) =>
        entry.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
        && entry.IndexOfAny(['/', '\\', ':']) < 0
        && !entry.Contains("..", StringComparison.Ordinal);
}
