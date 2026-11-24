using GlassLedge.Core.Services;

namespace GlassLedge.Tests;

public sealed class FolderPaneNavigatorTests
{
    [Fact]
    public void NewPane_StartsInTheGivenFolder_WithNothingToGoBackTo()
    {
        var pane = new FolderPaneNavigator(@"C:\Projects\glass-ledge");

        Assert.Equal(@"C:\Projects\glass-ledge", pane.Current);
        Assert.False(pane.CanGoBack);
    }

    [Fact]
    public void NavigateTo_ThenGoBack_ReturnsToThePreviousFolder()
    {
        var pane = new FolderPaneNavigator(@"C:\A");
        pane.NavigateTo(@"C:\A\B");
        pane.NavigateTo(@"C:\A\B\C");

        Assert.True(pane.GoBack());
        Assert.Equal(@"C:\A\B", pane.Current);
        Assert.True(pane.GoBack());
        Assert.Equal(@"C:\A", pane.Current);
        Assert.False(pane.GoBack());
        Assert.Equal(@"C:\A", pane.Current);
    }

    [Fact]
    public void NavigateTo_TheCurrentFolder_AddsNoHistory()
    {
        var pane = new FolderPaneNavigator(@"C:\A");

        pane.NavigateTo(@"c:\a");

        Assert.False(pane.CanGoBack);
    }

    [Fact]
    public void GoUp_MovesToTheParent_AndBackReturns()
    {
        var pane = new FolderPaneNavigator(@"C:\A\B");

        Assert.True(pane.GoUp());
        Assert.Equal(@"C:\A", pane.Current);
        Assert.True(pane.GoBack());
        Assert.Equal(@"C:\A\B", pane.Current);
    }

    [Fact]
    public void GoUp_AtADriveRoot_DoesNothing()
    {
        var pane = new FolderPaneNavigator(@"C:\");

        Assert.False(pane.CanGoUp);
        Assert.False(pane.GoUp());
        Assert.Equal(@"C:\", pane.Current);
    }

    [Theory]
    [InlineData(@"C:\Projects\glass-ledge", "glass-ledge")]
    [InlineData(@"C:\Projects\glass-ledge\", "glass-ledge")]
    [InlineData(@"C:\", @"C:\")]
    public void Title_IsTheFolderName_OrTheDriveRoot(string path, string expected) =>
        Assert.Equal(expected, new FolderPaneNavigator(path).Title);
}
