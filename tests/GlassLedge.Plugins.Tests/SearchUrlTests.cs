using GlassLedge.Plugins.SearchBox;

namespace GlassLedge.Plugins.Tests;

public sealed class SearchUrlTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void For_ReturnsNullWhenThereIsNothingToSearch(string? text)
    {
        Assert.Null(SearchUrl.For(text));
    }

    [Fact]
    public void For_TrimsAndEscapesTheQuery()
    {
        Assert.Equal(SearchUrl.Template + "glass%20ledge", SearchUrl.For("  glass ledge  "));
    }

    [Fact]
    public void For_EscapesCharactersThatCouldChangeTheAddress()
    {
        var url = SearchUrl.For("a&b=c#d?e");

        Assert.Equal(SearchUrl.Template + "a%26b%3Dc%23d%3Fe", url);
    }

    [Fact]
    public void For_CutsOverlongQueries()
    {
        var url = SearchUrl.For(new string('x', SearchUrl.MaxLength + 100));

        Assert.Equal(SearchUrl.Template.Length + SearchUrl.MaxLength, url!.Length);
    }

    [Fact]
    public void Template_IsAWebAddress()
    {
        Assert.StartsWith("https://", SearchUrl.Template);
    }
}
