namespace GlassLedge.Core.Models;

/// <summary>A change to where a widget sits in the stack of widgets on the desktop.</summary>
public enum LayerMove
{
    /// <summary>In front of every other widget.</summary>
    ToFront,

    /// <summary>In front of the nearest widget above it that it overlaps (one visible step).</summary>
    Forward,

    /// <summary>Behind the nearest widget below it that it overlaps (one visible step).</summary>
    Backward,

    /// <summary>Behind every other widget.</summary>
    ToBack,
}
