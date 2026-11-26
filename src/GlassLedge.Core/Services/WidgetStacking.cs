using GlassLedge.Core.Models;

namespace GlassLedge.Core.Services;

/// <summary>One widget in the stack: who it is, where it sits on the desktop, and whether it is pinned always on top.</summary>
public sealed record StackedWidget(Guid Id, ScreenRect Bounds, bool AlwaysOnTop = false);

/// <summary>
/// The rules for stacking widgets (which one is "in front of" which). Pure logic with no windows in it, so it
/// is easy to test. An order is a list of widget ids from the back of the stack to the front.
/// <para>
/// "Forward" and "Backward" step past the nearest widget that actually overlaps, so every use changes what
/// the person sees; stepping past a widget on the other side of the desktop would look like nothing happened.
/// </para>
/// <para>
/// Widgets pinned "always on top" form their own layer above all the others. Moves only reorder a widget among
/// widgets in the same layer, and the order that comes back lists every normal widget before every pinned one.
/// </para>
/// </summary>
public static class WidgetStacking
{
    /// <summary>
    /// Reconciles a saved order with the widgets that exist now: widgets that are gone are dropped, and widgets
    /// that have no saved place (new ones) go to the front, in the order given.
    /// </summary>
    public static IReadOnlyList<Guid> Normalize(IReadOnlyList<Guid> saved, IReadOnlyList<Guid> existing)
    {
        var present = new HashSet<Guid>(existing);
        var result = new List<Guid>();
        var placed = new HashSet<Guid>();

        foreach (var id in saved)
        {
            if (present.Contains(id) && placed.Add(id))
            {
                result.Add(id);
            }
        }

        foreach (var id in existing)
        {
            if (placed.Add(id))
            {
                result.Add(id);
            }
        }

        return result;
    }

    /// <summary>Whether the move would change anything (so menus can grey out items that do nothing).</summary>
    public static bool CanMove(IReadOnlyList<StackedWidget> backToFront, Guid id, LayerMove move)
    {
        var group = SameLayer(backToFront, id);
        return group is not null && CanMoveWithin(group, id, move);
    }

    /// <summary>The order after the move. Unchanged when the move doesn't apply.</summary>
    public static IReadOnlyList<Guid> Move(IReadOnlyList<StackedWidget> backToFront, Guid id, LayerMove move)
    {
        var group = SameLayer(backToFront, id);
        if (group is null || !CanMoveWithin(group, id, move))
        {
            return backToFront.Select(widget => widget.Id).ToList();
        }

        var pinned = group[IndexOf(group, id)].AlwaysOnTop;
        var moved = MoveWithin(group, id, move);
        var others = backToFront.Where(widget => widget.AlwaysOnTop != pinned).Select(widget => widget.Id);

        // Normal widgets first (back), pinned widgets last (front).
        return pinned ? others.Concat(moved).ToList() : moved.Concat(others).ToList();
    }

    /// <summary>The widgets in the same layer (pinned, or not) as the given one, in stacking order; null if unknown.</summary>
    private static List<StackedWidget>? SameLayer(IReadOnlyList<StackedWidget> backToFront, Guid id)
    {
        var target = backToFront.FirstOrDefault(widget => widget.Id == id);
        return target is null ? null : backToFront.Where(widget => widget.AlwaysOnTop == target.AlwaysOnTop).ToList();
    }

    private static bool CanMoveWithin(IReadOnlyList<StackedWidget> backToFront, Guid id, LayerMove move)
    {
        var index = IndexOf(backToFront, id);
        if (index < 0)
        {
            return false;
        }

        return move switch
        {
            LayerMove.ToFront => index < backToFront.Count - 1,
            LayerMove.ToBack => index > 0,
            LayerMove.Forward => NextOverlapAbove(backToFront, index) >= 0,
            LayerMove.Backward => NextOverlapBelow(backToFront, index) >= 0,
            _ => false,
        };
    }

    private static List<Guid> MoveWithin(IReadOnlyList<StackedWidget> backToFront, Guid id, LayerMove move)
    {
        var ids = backToFront.Select(widget => widget.Id).ToList();
        var index = IndexOf(backToFront, id);

        ids.RemoveAt(index);
        switch (move)
        {
            case LayerMove.ToFront:
                ids.Add(id);
                break;
            case LayerMove.ToBack:
                ids.Insert(0, id);
                break;
            case LayerMove.Forward:
                // The overlapped widget was above this one, so after removing it, it sits one place lower.
                ids.Insert(NextOverlapAbove(backToFront, index), id);
                break;
            case LayerMove.Backward:
                ids.Insert(NextOverlapBelow(backToFront, index), id);
                break;
        }

        return ids;
    }

    /// <summary>True when the two rectangles share some area (touching edges don't count).</summary>
    public static bool Overlaps(ScreenRect a, ScreenRect b) =>
        a.Left < b.Right && b.Left < a.Right && a.Top < b.Bottom && b.Top < a.Bottom;

    private static int NextOverlapAbove(IReadOnlyList<StackedWidget> items, int index)
    {
        for (var i = index + 1; i < items.Count; i++)
        {
            if (Overlaps(items[index].Bounds, items[i].Bounds))
            {
                return i;
            }
        }

        return -1;
    }

    private static int NextOverlapBelow(IReadOnlyList<StackedWidget> items, int index)
    {
        for (var i = index - 1; i >= 0; i--)
        {
            if (Overlaps(items[index].Bounds, items[i].Bounds))
            {
                return i;
            }
        }

        return -1;
    }

    private static int IndexOf(IReadOnlyList<StackedWidget> items, Guid id)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }
}
