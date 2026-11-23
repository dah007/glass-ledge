using GlassLedge.App;

namespace GlassLedge.AppTests;

public sealed class TileViewModelTests
{
    [Fact]
    public void SpokenName_FallsBackToTheLabel()
    {
        var tile = new TileViewModel { Label = "notes.txt", TargetPath = @"C:\x\notes.txt" };

        Assert.Equal("notes.txt", tile.SpokenName);
    }

    [Fact]
    public void SpokenName_PrefersTheAccessibleName()
    {
        var tile = new TileViewModel { Label = "More", TargetPath = @"C:\x", OverflowText = "+3", AccessibleName = "3 more items, opens the folder" };

        Assert.Equal("3 more items, opens the folder", tile.SpokenName);
    }
}
