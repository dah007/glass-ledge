namespace GlassLedge.Core.Services;

/// <summary>
/// Where a GlassLedge pane is, and how it got there: a history for Back, and the parent folder for Up.
/// Pure path logic; it never touches the disk.
/// </summary>
public sealed class FolderPaneNavigator
{
    private readonly List<string> _history = [];

    public FolderPaneNavigator(string startFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startFolder);
        _history.Add(startFolder);
    }

    public string Current => _history[^1];

    public bool CanGoBack => _history.Count > 1;

    /// <summary>The folder above <see cref="Current"/>, or null at the top of a drive.</summary>
    public string? Parent => Directory.GetParent(Current)?.FullName;

    public bool CanGoUp => Parent is not null;

    /// <summary>The pane's title: the folder's name, or the whole path for a drive root.</summary>
    public string Title
    {
        get
        {
            var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(Current));
            return name.Length > 0 ? name : Current;
        }
    }

    /// <summary>Moves into a folder. Asking for the folder you're already in changes nothing.</summary>
    public void NavigateTo(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        if (string.Equals(folder, Current, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _history.Add(folder);
    }

    public bool GoBack()
    {
        if (!CanGoBack)
        {
            return false;
        }

        _history.RemoveAt(_history.Count - 1);
        return true;
    }

    public bool GoUp()
    {
        if (Parent is not { } parent)
        {
            return false;
        }

        NavigateTo(parent);
        return true;
    }
}
