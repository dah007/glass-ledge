using GlassLedge.Core.Models;
using GlassLedge.Core.Services;

namespace GlassLedge.Tests;

public class TileAccessibilityTests
{
    [Fact]
    public void NameFor_File_IsTheName()
    {
        Assert.Equal("report.docx", TileAccessibility.NameFor(new FolderEntry("report.docx", @"C:\x\report.docx", IsDirectory: false)));
    }

    [Fact]
    public void NameFor_Folder_SaysFolder()
    {
        Assert.Equal("Invoices, folder", TileAccessibility.NameFor(new FolderEntry("Invoices", @"C:\x\Invoices", IsDirectory: true)));
    }

    [Fact]
    public void OverflowName_SaysHowManyAndWhatItDoes()
    {
        Assert.Equal("1 more item, opens the folder", TileAccessibility.OverflowName(1));
        Assert.Contains("5 more items", TileAccessibility.OverflowName(5), StringComparison.Ordinal);
        Assert.EndsWith("opens the folder", TileAccessibility.OverflowName(5), StringComparison.Ordinal);
    }

    [Fact]
    public void ItemCount_UsesSingularAndPlural()
    {
        Assert.Equal("1 item", TileAccessibility.ItemCount(1));
        Assert.Equal("0 items", TileAccessibility.ItemCount(0));
        Assert.Contains("items", TileAccessibility.ItemCount(12), StringComparison.Ordinal);
    }
}
