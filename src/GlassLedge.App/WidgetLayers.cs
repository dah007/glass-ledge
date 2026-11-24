using GlassLedge.App.Interop;
using GlassLedge.Core.Models;
using GlassLedge.Core.Services;

namespace GlassLedge.App;

/// <summary>A widget window that takes part in the front-to-back stacking: folder widgets and plugin widgets.</summary>
public interface ILayeredWidget
{
    /// <summary>The widget's id from its settings; the stacking order is a list of these.</summary>
    Guid WidgetId { get; }

    /// <summary>Where the widget sits on the desktop, to tell which widgets overlap.</summary>
    ScreenRect Bounds { get; }

    /// <summary>The window handle, or zero before the window has been shown.</summary>
    IntPtr LayerHandle { get; }
}

/// <summary>
/// Keeps every widget window in one saved front-to-back order. All widgets sit on the desktop layer, so the
/// order only matters between widgets: this class puts them in that order, and moves a widget when asked.
/// </summary>
internal sealed class WidgetLayers(App app)
{
    /// <summary>
    /// Puts the widget windows in the saved order. Called whenever a window changes, in place of "send this
    /// one to the bottom", which would put it behind its neighbours and lose the order.
    /// </summary>
    internal void Restack()
    {
        var order = CurrentOrder(out var widgets);

        // Sending to the bottom front-to-back leaves the last one sent, the back widget, lowest.
        foreach (var id in order.Reverse())
        {
            var handle = widgets[id].LayerHandle;
            if (handle != IntPtr.Zero)
            {
                DesktopAttachment.SendToBottom(handle);
            }
        }
    }

    internal bool CanMove(ILayeredWidget widget, LayerMove move) =>
        WidgetStacking.CanMove(Snapshot(), widget.WidgetId, move);

    /// <summary>Moves the widget, saves the new order and applies it to the windows.</summary>
    internal void Move(ILayeredWidget widget, LayerMove move)
    {
        var stack = Snapshot();
        if (!WidgetStacking.CanMove(stack, widget.WidgetId, move))
        {
            return;
        }

        app.Settings.LayerOrder = WidgetStacking.Move(stack, widget.WidgetId, move).ToList();
        app.SaveSettings();
        Restack();
    }

    private List<StackedWidget> Snapshot()
    {
        var order = CurrentOrder(out var widgets);
        return order.Select(id => new StackedWidget(id, widgets[id].Bounds)).ToList();
    }

    private IReadOnlyList<Guid> CurrentOrder(out Dictionary<Guid, ILayeredWidget> widgets)
    {
        widgets = [];
        var existing = new List<Guid>();
        foreach (var widget in app.LayeredWidgets)
        {
            if (widgets.TryAdd(widget.WidgetId, widget))
            {
                existing.Add(widget.WidgetId);
            }
        }

        var order = WidgetStacking.Normalize(app.Settings.LayerOrder, existing);
        if (!order.SequenceEqual(app.Settings.LayerOrder))
        {
            // Remember the reconciled order; it is written to disk with the next save.
            app.Settings.LayerOrder = order.ToList();
        }

        return order;
    }
}
