namespace GlassLedge.Plugins.SearchBox;

/// <summary>
/// Turns what someone typed into a web search address. Plain logic with no WPF in it, so it is easy to
/// test (see tests/GlassLedge.Plugins.Tests).
/// </summary>
internal static class SearchUrl
{
    /// <summary>
    /// The search engine's address up to the query text. To use another engine, change this one line,
    /// for example <c>https://www.google.com/search?q=</c> or <c>https://www.bing.com/search?q=</c>.
    /// </summary>
    internal const string Template = "https://duckduckgo.com/?q=";

    /// <summary>The longest search the box accepts.</summary>
    internal const int MaxLength = 500;

    /// <summary>The address to open for this text, or null when there is nothing to search for.</summary>
    internal static string? For(string? text)
    {
        var query = text?.Trim();
        if (string.IsNullOrEmpty(query))
        {
            return null;
        }

        if (query.Length > MaxLength)
        {
            query = query[..MaxLength];
        }

        // Escape so characters like & # ? and spaces can't change what the address means.
        return Template + Uri.EscapeDataString(query);
    }
}
