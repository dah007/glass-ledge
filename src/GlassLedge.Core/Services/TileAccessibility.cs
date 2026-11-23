using System.Globalization;
using GlassLedge.Core.Models;

namespace GlassLedge.Core.Services;

/// <summary>The names a screen reader speaks for a widget's tiles and item count.</summary>
public static class TileAccessibility
{
    /// <summary>A file is announced by its name; a folder as "name, folder", so the two can be told apart.</summary>
    public static string NameFor(FolderEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return entry.IsDirectory ? entry.Name + ", folder" : entry.Name;
    }

    /// <summary>The "+N" tile, spoken in words, including what it does.</summary>
    public static string OverflowName(int hiddenCount) =>
        hiddenCount == 1
            ? "1 more item, opens the folder"
            : hiddenCount.ToString(CultureInfo.CurrentCulture) + " more items, opens the folder";

    /// <summary>The number in the title row's count pill, with its unit.</summary>
    public static string ItemCount(int count) =>
        count == 1 ? "1 item" : count.ToString(CultureInfo.CurrentCulture) + " items";
}
