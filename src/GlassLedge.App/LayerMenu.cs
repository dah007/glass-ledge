using System.Windows;
using System.Windows.Controls;
using GlassLedge.Core.Models;

namespace GlassLedge.App;

/// <summary>The "Layer" submenu shared by every widget's right-click menu.</summary>
internal static class LayerMenu
{
    /// <summary>
    /// Fills the submenu just before it is shown. Items that wouldn't change anything are greyed out (for
    /// example Bring forward when nothing overlapping is in front).
    /// </summary>
    internal static void Fill(MenuItem parent, App app, ILayeredWidget widget)
    {
        parent.Items.Clear();
        Add(parent, "Bring to front", LayerMove.ToFront, app, widget);
        Add(parent, "Bring forward", LayerMove.Forward, app, widget);
        Add(parent, "Send backward", LayerMove.Backward, app, widget);
        Add(parent, "Send to back", LayerMove.ToBack, app, widget);
    }

    private static void Add(MenuItem parent, string header, LayerMove move, App app, ILayeredWidget widget)
    {
        var item = new MenuItem { Header = header, IsEnabled = app.Layers.CanMove(widget, move) };
        item.Click += (_, _) => app.Layers.Move(widget, move);
        parent.Items.Add(item);
    }
}
