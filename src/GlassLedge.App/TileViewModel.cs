using System.Windows.Media;

namespace GlassLedge.App;

/// <summary>One tile in a widget: a file/folder, or the "+N" overflow tile.</summary>
public sealed class TileViewModel
{
    public required string Label { get; init; }

    /// <summary>What double-clicking opens. For the overflow tile this is the widget's folder.</summary>
    public required string TargetPath { get; init; }

    public ImageSource? Icon { get; init; }

    /// <summary>"+N" on the overflow tile; null on normal tiles.</summary>
    public string? OverflowText { get; init; }

    /// <summary>What a screen reader says for this tile when it differs from <see cref="Label"/>.</summary>
    public string? AccessibleName { get; init; }

    /// <summary>The tile's name for assistive technology: <see cref="AccessibleName"/>, or the label.</summary>
    public string SpokenName => AccessibleName ?? Label;
}
