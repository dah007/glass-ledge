using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using GlassLedge.App.Interop;
using GlassLedge.Core.Models;
using GlassLedge.Core.Services;

namespace GlassLedge.App;

/// <summary>An iOS-style folder widget pinned to the desktop.</summary>
public partial class FolderWindow : Window
{
    public static readonly DependencyProperty GridColumnsProperty = DependencyProperty.Register(
        nameof(GridColumns), typeof(int), typeof(FolderWindow), new PropertyMetadata(4));

    public static readonly DependencyProperty GridRowsProperty = DependencyProperty.Register(
        nameof(GridRows), typeof(int), typeof(FolderWindow), new PropertyMetadata(2));

    /// <summary>True when the glossy glass layers are drawn (the card, its rim and each tile bind to it).</summary>
    public static readonly DependencyProperty IsGlossyProperty = DependencyProperty.Register(
        nameof(IsGlossy), typeof(bool), typeof(FolderWindow), new PropertyMetadata(true));

    private static readonly Duration CollapseAnimationDuration = TimeSpan.FromMilliseconds(200);

    private readonly App _host;

    /// <summary>Bumped on every collapse/expand so a superseded animation's Completed is ignored.</summary>
    private int _collapseAnimationVersion;

    /// <summary>Where the left button went down, while we wait to see if it becomes a drag.</summary>
    private Point? _pressPoint;

    /// <summary>True while the title row is a text box.</summary>
    private bool _isRenaming;

    /// <summary>True while the widget has taken keyboard focus (see <see cref="FocusFromKeyboard"/>).</summary>
    private bool _keyboardMode;

    /// <summary>The window that had focus before keyboard mode, so Escape can hand focus back to it.</summary>
    private IntPtr _previousForeground;

    public FolderWindow(FolderWindowSettings settings, App host)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(host);

        InitializeComponent();

        Settings = settings;
        _host = host;

        ApplySize();
        KeepOnScreen();
        ApplyColor(SavedColor);
        ApplyGloss();
        LoadFolder();
    }

    /// <summary>The widget's saved custom colour, or null to follow the Windows theme.</summary>
    private uint? SavedColor =>
        ColorMath.TryParseHex(Settings.BackgroundColor, out var argb, out _) ? argb : null;

    public FolderWindowSettings Settings { get; }

    public int GridColumns
    {
        get => (int)GetValue(GridColumnsProperty);
        set => SetValue(GridColumnsProperty, value);
    }

    public int GridRows
    {
        get => (int)GetValue(GridRowsProperty);
        set => SetValue(GridRowsProperty, value);
    }

    public bool IsGlossy
    {
        get => (bool)GetValue(IsGlossyProperty);
        set => SetValue(IsGlossyProperty, value);
    }

    private IntPtr Handle => new WindowInteropHelper(this).Handle;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        DesktopAttachment.Attach(Handle);

        // No Windows blur and no window region: the blur filled the window's whole rectangle on
        // Windows 11, and a region clips without anti-aliasing (jagged corners). The window is
        // per-pixel transparent, so WPF's own smooth rounded card is the shape people see.
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        // Show() puts a new window on top of everything; push it back down to the desktop layer.
        DesktopAttachment.SendToBottom(Handle);
    }

    private void ApplySize()
    {
        BeginAnimation(HeightProperty, null); // stop any collapse animation in progress
        _collapseAnimationVersion++;

        var dimensions = WidgetLayout.For(Settings.Size);
        Width = dimensions.Width;
        Height = WidgetLayout.HeightFor(Settings.Size, Settings.IsCollapsed);
        GridColumns = dimensions.Columns;
        GridRows = dimensions.Rows;

        // Content area = expanded height minus the card's border (2 + 2) and padding (11 + 11).
        ContentPanel.Height = dimensions.Height - WidgetLayout.CollapsedHeight + TitleRow.Height;
        Tiles.Visibility = Settings.IsCollapsed ? Visibility.Collapsed : Visibility.Visible;
    }

    private void SetCollapsed(bool collapse)
    {
        if (Settings.IsCollapsed == collapse)
        {
            return;
        }

        Settings.IsCollapsed = collapse;
        _host.SaveSettings();

        var version = ++_collapseAnimationVersion;
        var targetHeight = WidgetLayout.HeightFor(Settings.Size, collapse);

        if (!collapse)
        {
            Tiles.Visibility = Visibility.Visible;
        }

        var animation = new DoubleAnimation(targetHeight, CollapseAnimationDuration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        animation.Completed += (_, _) =>
        {
            if (version != _collapseAnimationVersion)
            {
                return; // another collapse/expand started meanwhile
            }

            // Replace the animated value with a real one so later size changes work normally.
            BeginAnimation(HeightProperty, null);
            Height = targetHeight;

            if (collapse)
            {
                Tiles.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Expanding near the bottom of the screen may push the widget off it.
                KeepOnScreen();
                if (Left != Settings.Left || Top != Settings.Top)
                {
                    SavePosition();
                }
            }
        };

        BeginAnimation(HeightProperty, animation);
    }

    private void KeepOnScreen()
    {
        var desktop = new ScreenRect(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);
        (Left, Top) = WindowPlacement.EnsureVisible(Settings.Left, Settings.Top, Width, Height, desktop);
    }

    private void LoadFolder()
    {
        var path = Settings.FolderPath;
        FolderNameText.Text = WidgetName.For(Settings);
        FolderNameText.ToolTip = path;
        Title = FolderNameText.Text;

        IReadOnlyList<FolderEntry> entries;
        try
        {
            entries = FolderContents.List(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Tiles.ItemsSource = null;
            ItemCountText.Text = "Unavailable";
            AutomationProperties.SetName(ItemCountText, "Folder unavailable");
            return;
        }

        ItemCountText.Text = entries.Count.ToString(CultureInfo.CurrentCulture);
        AutomationProperties.SetName(ItemCountText, TileAccessibility.ItemCount(entries.Count));

        var contents = WidgetLayout.Fit(entries, WidgetLayout.For(Settings.Size).Capacity);
        var tiles = contents.Visible
            .Select(entry => new TileViewModel
            {
                Label = entry.Name,
                TargetPath = entry.FullPath,
                IsFolder = entry.IsDirectory,
                Icon = ShellIcons.GetIcon(entry.FullPath),
                AccessibleName = TileAccessibility.NameFor(entry),
            })
            .ToList();

        if (contents.HasOverflow)
        {
            tiles.Add(new TileViewModel
            {
                Label = "More",
                TargetPath = path,
                IsFolder = true,
                OverflowText = "+" + contents.OverflowCount.ToString(CultureInfo.CurrentCulture),
                AccessibleName = TileAccessibility.OverflowName(contents.OverflowCount),
            });
        }

        Tiles.ItemsSource = tiles;
    }

    // ---- Mouse: double-click opens, press-and-move drags the widget ----

    // Preview, so it runs before the tile buttons: the card owns every mouse click (double-click opens,
    // press-and-move drags). The buttons exist for the keyboard and screen readers.
    private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // The rename box handles its own clicks (placing the caret, selecting).
        if (_isRenaming && IsInRenameBox(e.OriginalSource))
        {
            return;
        }

        e.Handled = true;

        // A click on the card (not in the rename box, which handles its own clicks) finishes renaming.
        EndRename(save: true);

        if (e.ClickCount == 2)
        {
            _pressPoint = null;
            if (IsInTitleRow(e.OriginalSource))
            {
                SetCollapsed(!Settings.IsCollapsed);
            }
            else if (TileFrom(e.OriginalSource) is { } tile)
            {
                OpenTile(tile);
            }

            return;
        }

        _pressPoint = e.GetPosition(this);
        Card.CaptureMouse();
    }

    private void Card_MouseMove(object sender, MouseEventArgs e)
    {
        if (_pressPoint is not { } start || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var current = e.GetPosition(this);
        if (!DragThreshold.IsExceeded(
                current.X - start.X,
                current.Y - start.Y,
                SystemParameters.MinimumHorizontalDragDistance,
                SystemParameters.MinimumVerticalDragDistance))
        {
            return;
        }

        _pressPoint = null;
        Card.ReleaseMouseCapture();

        // DragMove blocks until the button is released, so this saves once, when dragging is complete.
        DragMove();
        DesktopAttachment.SendToBottom(Handle);
        SavePosition();
    }

    private void Card_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _pressPoint = null;
        Card.ReleaseMouseCapture();
    }

    private bool IsInRenameBox(object source) =>
        source is DependencyObject element && (element == NameEditor || NameEditor.IsAncestorOf(element));

    private bool IsInTitleRow(object source) =>
        source is DependencyObject element && (element == TitleRow || TitleRow.IsAncestorOf(element));

    private static TileViewModel? TileFrom(object source) => source switch
    {
        FrameworkElement element => element.DataContext as TileViewModel,
        FrameworkContentElement element => element.DataContext as TileViewModel,
        _ => null,
    };

    private void SavePosition()
    {
        Settings.Left = Left;
        Settings.Top = Top;
        _host.SaveSettings();
    }

    /// <summary>A tile was opened: a folder goes to a GlassLedge pane when Preferences says so, anything else to Windows.</summary>
    private void OpenTile(TileViewModel tile)
    {
        if (tile.IsFolder && _host.OpenFoldersInPane)
        {
            _host.OpenPane(tile.TargetPath);
            return;
        }

        Open(tile.TargetPath, tile.Label);
    }

    private void Open(string path, string displayName)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException)
        {
            MessageBox.Show(
                $"Couldn't open \"{displayName}\".\n\n{ex.Message}",
                Title,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    // ---- Keyboard and screen readers ----

    /// <summary>
    /// Takes keyboard focus for someone who can't use the mouse: the widget becomes activatable, comes
    /// forward, and focus lands on its first tile (or the card when there are none). Arrow keys move between
    /// tiles, Enter opens one, the Menu key or Shift+F10 opens the menu, F2 renames, F5 refreshes, and
    /// Escape gives focus back to the window that had it.
    /// </summary>
    internal void FocusFromKeyboard()
    {
        if (!IsVisible)
        {
            ShowOnDesktop();
        }

        var previous = NativeMethods.GetForegroundWindow();
        if (previous != Handle)
        {
            _previousForeground = previous;
        }

        _keyboardMode = true;
        DesktopAttachment.SetActivatable(Handle, true);
        Activate();
        FocusFirstTileOrCard();
    }

    private void FocusFirstTileOrCard()
    {
        Tiles.UpdateLayout();
        if (FindFirstTile() is { } tile)
        {
            tile.Focus();
            Keyboard.Focus(tile);
        }
        else
        {
            Card.Focus();
            Keyboard.Focus(Card);
        }
    }

    private Button? FindFirstTile() =>
        Tiles.Visibility == Visibility.Visible ? FindDescendant<Button>(Tiles) : null;

    private static T? FindDescendant<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            if (FindDescendant<T>(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }

    private void LeaveKeyboardMode(bool restoreForeground)
    {
        if (!_keyboardMode)
        {
            return;
        }

        _keyboardMode = false;
        Keyboard.ClearFocus();
        DesktopAttachment.SetActivatable(Handle, false);
        DesktopAttachment.SendToBottom(Handle);

        if (restoreForeground && _previousForeground != IntPtr.Zero)
        {
            NativeMethods.SetForegroundWindow(_previousForeground);
        }

        _previousForeground = IntPtr.Zero;
    }

    /// <summary>A tile pressed with Enter or Space, or invoked by a screen reader. The mouse never gets here.</summary>
    private void Tile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TileViewModel tile })
        {
            OpenTile(tile);
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // The rename box handles Enter and Escape itself, and an open menu owns Escape.
        if (_isRenaming || Card.ContextMenu is { IsOpen: true })
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Escape when _keyboardMode:
                LeaveKeyboardMode(restoreForeground: true);
                e.Handled = true;
                break;
            case Key.F5:
                LoadFolder();
                e.Handled = true;
                break;
            case Key.F2:
                BeginRename();
                e.Handled = true;
                break;
        }
    }

    // ---- Right-click menu ----

    private void ContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        SizeSmallItem.IsChecked = Settings.Size == WidgetSize.Small;
        SizeMediumItem.IsChecked = Settings.Size == WidgetSize.Medium;
        SizeLargeItem.IsChecked = Settings.Size == WidgetSize.Large;
        SizeExtraLargeItem.IsChecked = Settings.Size == WidgetSize.ExtraLarge;
        CollapseItem.Header = Settings.IsCollapsed ? "Expand" : "Collapse";
        GlossItem.IsChecked = Settings.GlossEnabled;
        PluginMenu.Fill(AddPluginMenu, _host);
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadFolder();

    // ---- Rename (in place, in the title row) ----

    private void Rename_Click(object sender, RoutedEventArgs e) => BeginRename();

    private void BeginRename()
    {
        if (_isRenaming)
        {
            return;
        }

        _isRenaming = true;
        NameEditor.Text = WidgetName.For(Settings);
        FolderNameText.Visibility = Visibility.Hidden;
        NameEditor.Visibility = Visibility.Visible;

        // The widget normally never takes focus; it must, briefly, to receive typing.
        DesktopAttachment.SetActivatable(Handle, true);
        Activate();
        NameEditor.Focus();
        Keyboard.Focus(NameEditor);
        NameEditor.SelectAll();
    }

    private void EndRename(bool save)
    {
        if (!_isRenaming)
        {
            return;
        }

        _isRenaming = false;
        if (save)
        {
            Settings.CustomName = WidgetName.NormalizeCustomName(NameEditor.Text, Settings.FolderPath);
            _host.SaveSettings();
        }

        NameEditor.Visibility = Visibility.Collapsed;
        FolderNameText.Visibility = Visibility.Visible;
        FolderNameText.Text = WidgetName.For(Settings);
        Title = FolderNameText.Text;

        if (_keyboardMode)
        {
            FocusFirstTileOrCard(); // keep the keyboard where it was; the widget stays activatable
            return;
        }

        DesktopAttachment.SetActivatable(Handle, false);
        DesktopAttachment.SendToBottom(Handle);
    }

    private void NameEditor_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            EndRename(save: true);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            EndRename(save: false);
            e.Handled = true;
        }
    }

    // Clicking anywhere else, or switching apps, keeps what was typed.
    private void NameEditor_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => EndRename(save: true);

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);

        // Switched to another window: stop being activatable, but don't pull focus back.
        LeaveKeyboardMode(restoreForeground: false);
        EndRename(save: true);
    }

    // ---- Background colour ----

    private void Color_Click(object sender, RoutedEventArgs e)
    {
        var picker = new ColorPickerWindow(SavedColor);
        picker.ColorChanged += (_, _) => ApplyColor(picker.SelectedColor); // live preview

        if (picker.ShowDialog() == true)
        {
            Settings.BackgroundColor = picker.SelectedColor is { } argb ? ColorMath.ToArgbHex(argb) : null;
            _host.SaveSettings();
        }

        ApplyColor(SavedColor); // the saved choice, or back to it after Cancel
        DesktopAttachment.SendToBottom(Handle);
    }

    /// <summary>
    /// A custom colour gets this window's own brushes, worked out against the wallpaper's average colour
    /// so the text reads on any colour; null falls back to the theme's.
    /// </summary>
    private void ApplyColor(uint? argb)
    {
        // In High Contrast the system colours win, whatever colour was chosen.
        if (argb is { } color && !SystemParameters.HighContrast)
        {
            // With Windows transparency effects off the card is solid, so the text is chosen against the colour itself.
            var solid = WidgetTheme.IsTransparencyEnabled() ? color : color | 0xFF000000;
            WidgetTheme.Apply(Resources, WidgetPalette.ForBackground(solid, _host.WallpaperColor));
        }
        else
        {
            WidgetTheme.Clear(Resources);
        }
    }

    // ---- Glossy sheen ----

    private void Gloss_Click(object sender, RoutedEventArgs e)
    {
        Settings.GlossEnabled = !Settings.GlossEnabled;
        ApplyGloss();
        _host.SaveSettings();
    }

    /// <summary>Shows or hides the glass layers. They are never hit-testable, so they can't swallow clicks either way.</summary>
    private void ApplyGloss() => IsGlossy = Settings.GlossEnabled && !SystemParameters.HighContrast;

    /// <summary>Re-works out the colour and gloss, e.g. after the wallpaper or a Windows setting changed.</summary>
    internal void RefreshColor()
    {
        ApplyColor(SavedColor);
        ApplyGloss();
    }

    private void ToggleCollapsed_Click(object sender, RoutedEventArgs e) => SetCollapsed(!Settings.IsCollapsed);

    private void OpenInExplorer_Click(object sender, RoutedEventArgs e) =>
        Open(Settings.FolderPath, FolderNameText.Text);

    private void ChangeFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = App.PromptForFolder();
        if (folder is null)
        {
            return;
        }

        Settings.FolderPath = folder;
        LoadFolder();
        _host.SaveSettings();
    }

    private void Size_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string tag } || !Enum.TryParse<WidgetSize>(tag, out var size))
        {
            return;
        }

        Settings.Size = size;
        ApplySize();
        KeepOnScreen(); // a bigger size may push the widget past the screen edge
        Settings.Left = Left;
        Settings.Top = Top;
        LoadFolder();
        _host.SaveSettings();
    }

    private void AddWidget_Click(object sender, RoutedEventArgs e) => _host.AddWidget();

    private void RemoveWidget_Click(object sender, RoutedEventArgs e) => _host.ConfirmAndRemoveWidget(this);

    /// <summary>Shows the widget again after "Hide all", back on the desktop layer.</summary>
    internal void ShowOnDesktop()
    {
        Show();
        DesktopAttachment.SendToBottom(Handle);
    }

    private void Preferences_Click(object sender, RoutedEventArgs e) => _host.ShowPreferences();

    private void About_Click(object sender, RoutedEventArgs e) => _host.ShowAbout();

    private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}
