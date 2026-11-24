using GlassLedge.Core.Models;
using GlassLedge.Core.Services;

namespace GlassLedge.Tests;

public sealed class WidgetStackingTests
{
    private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid B = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid C = Guid.Parse("00000000-0000-0000-0000-00000000000c");
    private static readonly Guid D = Guid.Parse("00000000-0000-0000-0000-00000000000d");

    private static StackedWidget At(Guid id, double left) => new(id, new ScreenRect(left, 0, 100, 100));

    // A, B and C overlap their neighbours (each 50 apart); D is far away and overlaps nothing.
    private static List<StackedWidget> Stack(params Guid[] backToFront)
    {
        var lefts = new Dictionary<Guid, double> { [A] = 0, [B] = 50, [C] = 100, [D] = 1000 };
        return backToFront.Select(id => At(id, lefts[id])).ToList();
    }

    [Fact]
    public void ToFront_PutsTheWidgetLast()
    {
        Assert.Equal([B, C, A], WidgetStacking.Move(Stack(A, B, C), A, LayerMove.ToFront));
    }

    [Fact]
    public void ToBack_PutsTheWidgetFirst()
    {
        Assert.Equal([C, A, B], WidgetStacking.Move(Stack(A, B, C), C, LayerMove.ToBack));
    }

    [Fact]
    public void Forward_StepsInFrontOfTheNearestOverlappingWidget()
    {
        // A is under B ("widget a is under widget b"); bring A forward and B is now behind A.
        Assert.Equal([B, A], WidgetStacking.Move(Stack(A, B), A, LayerMove.Forward));
    }

    [Fact]
    public void Backward_StepsBehindTheNearestOverlappingWidget()
    {
        Assert.Equal([B, A], WidgetStacking.Move(Stack(A, B), B, LayerMove.Backward));
    }

    [Fact]
    public void Forward_SkipsWidgetsThatDoNotOverlap()
    {
        // D sits between A and C in the stack but is far away, so one step goes past C, the overlapping one.
        // (A at 0 and C at 100 touch edges only, so use B to overlap A.)
        var stack = new List<StackedWidget> { At(A, 0), At(D, 1000), At(B, 50) };

        Assert.Equal([D, B, A], WidgetStacking.Move(stack, A, LayerMove.Forward));
    }

    [Fact]
    public void Backward_SkipsWidgetsThatDoNotOverlap()
    {
        var stack = new List<StackedWidget> { At(A, 0), At(D, 1000), At(B, 50) };

        Assert.Equal([B, A, D], WidgetStacking.Move(stack, B, LayerMove.Backward));
    }

    [Fact]
    public void ForwardAndBackward_DoNothingWithoutAnOverlappingWidgetThatWay()
    {
        var stack = Stack(A, D);

        Assert.False(WidgetStacking.CanMove(stack, A, LayerMove.Forward));
        Assert.False(WidgetStacking.CanMove(stack, D, LayerMove.Backward));
        Assert.Equal([A, D], WidgetStacking.Move(stack, A, LayerMove.Forward));
    }

    [Fact]
    public void TheTopWidgetCannotGoForwardOrToFront_AndTheBottomCannotGoBackOrToBack()
    {
        var stack = Stack(A, B, C);

        Assert.False(WidgetStacking.CanMove(stack, C, LayerMove.ToFront));
        Assert.False(WidgetStacking.CanMove(stack, C, LayerMove.Forward));
        Assert.False(WidgetStacking.CanMove(stack, A, LayerMove.ToBack));
        Assert.False(WidgetStacking.CanMove(stack, A, LayerMove.Backward));
        Assert.True(WidgetStacking.CanMove(stack, A, LayerMove.ToFront));
        Assert.True(WidgetStacking.CanMove(stack, C, LayerMove.ToBack));
    }

    [Fact]
    public void ToFrontAndToBack_WorkEvenWhenNothingOverlaps()
    {
        var stack = Stack(A, D);

        Assert.Equal([D, A], WidgetStacking.Move(stack, A, LayerMove.ToFront));
        Assert.Equal([D, A], WidgetStacking.Move(stack, D, LayerMove.ToBack));
    }

    [Fact]
    public void Overlaps_NeedsSharedAreaNotJustTouchingEdges()
    {
        Assert.True(WidgetStacking.Overlaps(new ScreenRect(0, 0, 100, 100), new ScreenRect(99, 99, 100, 100)));
        Assert.False(WidgetStacking.Overlaps(new ScreenRect(0, 0, 100, 100), new ScreenRect(100, 0, 100, 100)));
        Assert.False(WidgetStacking.Overlaps(new ScreenRect(0, 0, 100, 100), new ScreenRect(0, 100, 100, 100)));
    }

    [Fact]
    public void UnknownWidget_ChangesNothing()
    {
        Assert.False(WidgetStacking.CanMove(Stack(A, B), C, LayerMove.ToFront));
        Assert.Equal([A, B], WidgetStacking.Move(Stack(A, B), C, LayerMove.ToFront));
    }

    [Fact]
    public void Normalize_KeepsSavedOrder_DropsGoneWidgets_AndPutsNewOnesInFront()
    {
        var result = WidgetStacking.Normalize(saved: [C, A, D], existing: [A, B, C]);

        Assert.Equal([C, A, B], result);
    }

    [Fact]
    public void Normalize_WithNothingSaved_UsesTheGivenOrder()
    {
        Assert.Equal([A, B, C], WidgetStacking.Normalize([], [A, B, C]));
    }

    [Fact]
    public void Normalize_IgnoresDuplicateIds()
    {
        Assert.Equal([A, B], WidgetStacking.Normalize([A, A, B, B], [A, B]));
    }

    [Fact]
    public void LayerOrder_RoundTripsThroughTheSettingsFile()
    {
        using var temp = new GlassLedge.TestSupport.TempDirectory();
        var store = new JsonSettingsStore(temp.Combine("settings.json"));

        store.Save(new AppSettings { LayerOrder = [B, A, C] });

        Assert.Equal([B, A, C], store.Load().LayerOrder);
    }
}
