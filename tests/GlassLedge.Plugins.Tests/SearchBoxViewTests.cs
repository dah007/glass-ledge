using System.Windows;
using GlassLedge.Plugins.SearchBox;

namespace GlassLedge.Plugins.Tests;

public sealed class SearchBoxViewTests
{
    [Fact]
    public void Search_OpensTheWebSearchAndClearsTheBox()
    {
        Sta.Run(() =>
        {
            var context = new FakeWidgetContext();
            var view = new SearchBoxView(context);
            view.QueryBox.Text = "hello world";

            view.Search();

            Assert.Equal(SearchUrl.Template + "hello%20world", Assert.Single(context.Opened).AbsoluteUri);
            Assert.Equal("", view.QueryBox.Text);
        });
    }

    [Fact]
    public void Search_WithNothingTypedOpensNothing()
    {
        Sta.Run(() =>
        {
            var context = new FakeWidgetContext();
            var view = new SearchBoxView(context);

            view.Search();

            Assert.Empty(context.Opened);
        });
    }

    [Fact]
    public void ThePlugin_BuildsAnElementThatUsesTheWidgetColours()
    {
        Sta.Run(() =>
        {
            var plugin = new SearchBoxPlugin();

            var body = plugin.CreateBody(new FakeWidgetContext());

            var view = Assert.IsType<SearchBoxView>(body);
            Assert.NotNull(view.QueryBox.Style);
            Assert.NotNull(view.SearchButton.Style);
        });
    }

    [Fact]
    public void TheHint_ShowsOnlyWhileTheBoxIsEmpty()
    {
        Sta.Run(() =>
        {
            var view = new SearchBoxView(new FakeWidgetContext());
            var hint = view.Children.OfType<System.Windows.Controls.TextBlock>().Single();
            Assert.Equal(Visibility.Visible, hint.Visibility);

            view.QueryBox.Text = "x";

            Assert.Equal(Visibility.Collapsed, hint.Visibility);
        });
    }
}
