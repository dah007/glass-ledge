using GlassLedge.Core.Services;

namespace GlassLedge.Tests;

public sealed class PluginManifestReaderTests
{
    private const string Valid = """
        {
          "id": "search-box", "name": "Search box", "version": "1.0.0",
          "apiVersion": 1, "entry": "SearchBox.dll", "entryType": "A.B.C"
        }
        """;

    [Fact]
    public void Parse_AcceptsAMinimalManifest()
    {
        var result = PluginManifestReader.Parse(Valid);

        Assert.True(result.IsValid);
        Assert.Equal("search-box", result.Manifest!.Id);
        Assert.Equal("SearchBox.dll", result.Manifest.Entry);
        Assert.Null(result.Manifest.Size);
    }

    [Fact]
    public void Parse_AllowsCommentsAndTrailingCommas()
    {
        const string json = """
            // a comment
            { "id": "a", "name": "A", "version": "1", "apiVersion": 1, /* inline */
              "entry": "A.dll", "entryType": "A.A", "size": { "width": 200, "height": 50, }, }
            """;

        var result = PluginManifestReader.Parse(json);

        Assert.True(result.IsValid, string.Join(" ", result.Errors));
        Assert.Equal(200, result.Manifest!.Size!.Width);
        Assert.Equal(50, result.Manifest.Size.Height);
    }

    [Theory]
    [InlineData("Search-Box")]
    [InlineData("search_box")]
    [InlineData("-search")]
    [InlineData("search--box")]
    [InlineData("search box")]
    public void Parse_RejectsBadIds(string id)
    {
        var result = PluginManifestReader.Parse(Valid.Replace("search-box", id));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("\"id\""));
    }

    [Theory]
    [InlineData("SearchBox.exe")]
    [InlineData("sub/SearchBox.dll")]
    [InlineData("sub\\\\SearchBox.dll")]
    [InlineData("..\\\\SearchBox.dll")]
    [InlineData("C:\\\\x\\\\SearchBox.dll")]
    public void Parse_RejectsEntriesThatAreNotAPlainDllName(string entry)
    {
        var result = PluginManifestReader.Parse(Valid.Replace("SearchBox.dll", entry));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("\"entry\""));
    }

    [Fact]
    public void Parse_RejectsANewerApiVersionWithAnUpdateHint()
    {
        var result = PluginManifestReader.Parse(Valid.Replace("\"apiVersion\": 1", "\"apiVersion\": 2"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Update GlassLedge"));
    }

    [Theory]
    [InlineData("\"id\": \"search-box\",")]
    [InlineData("\"name\": \"Search box\",")]
    [InlineData("\"version\": \"1.0.0\",")]
    [InlineData("\"apiVersion\": 1,")]
    [InlineData("\"entry\": \"SearchBox.dll\",")]
    public void Parse_ReportsMissingRequiredFields(string line)
    {
        var result = PluginManifestReader.Parse(Valid.Replace(line, ""));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(119, 40)]
    [InlineData(801, 40)]
    [InlineData(320, 23)]
    [InlineData(320, 401)]
    public void Parse_RejectsSizesOutsideTheLimits(double width, double height)
    {
        var json = Valid.TrimEnd().TrimEnd('}') + $", \"size\": {{ \"width\": {width}, \"height\": {height} }} }}";

        Assert.False(PluginManifestReader.Parse(json).IsValid);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("")]
    public void Parse_RefusesAnythingThatIsNotAnObject(string json)
    {
        var result = PluginManifestReader.Parse(json);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Parse_CollectsEveryProblemAtOnce()
    {
        var result = PluginManifestReader.Parse("{ \"id\": \"BAD\", \"apiVersion\": 9 }");

        Assert.True(result.Errors.Count >= 4);
    }
}
