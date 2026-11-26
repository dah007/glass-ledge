using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using GlassLedge.App.Interop;
using GlassLedge.Core.Models;
using GlassLedge.Core.Services;

namespace GlassLedge.App;

/// <summary>
/// The window for one plugin widget. GlassLedge draws all of it (the glass, the colour and Glossy choices,
/// dragging, the saved position, the right-click menu); the plugin supplies only the element in the middle.
/// <para>
/// Like the folder widgets it sits on the desktop layer and never takes focus on its own. The exception is
/// text entry: clicking a text box in the body makes the window take keyboard focus until the person
/// presses Esc or clicks another window.
/// </para>
/// </summary>
internal partial class PluginWidgetWindow : Window, ILayeredWidget
{
    /// <summary>The card's border (2) and padding (10) on each side, around the plugin's contents.</summary>
    private const double Chrome = 24;

    private readonly App _host;
    private readonly PluginWidgetContext _context;
    private bool _typing;
    private bool _closedByHost;
    private IntPtr _previousForeground;

    internal PluginWidgetWindow(
        PluginWidgetSettings settings,
        PluginManifest manifest,
        FrameworkElement body,
        PluginWidgetContext context,
        App host)
    {
        InitializeComponent();
        Settings = settings;
        Manifest = manifest;
        _context = context;
        _host = host;

        Title = manifest.Name;
        var size = manifest.Size ?? new PluginSize(320, 40);
        BodyHost.Width = size.Width;
        BodyHost.Height = size.Height;
        Width = size.Width + Chrome;
        Height = size.Height + Chrome;
        BodyHost.Child = body;

        KeepOnScreen();
        ApplyColor(SavedColor);
        ApplyGloss();
    }

    internal PluginWidgetSettings Settings { get; }

    internal PluginManifest Manifest { get; }

    private uint? SavedColor =>
        ColorMath.TryParseHex(Settings.BackgroundColor, out var argb, out _) ? argb : null;

    private IntPtr Handle => new WindowInteropHelper(this).Handle;

    Guid ILayeredWidget.WidgetId => Settings.Id;

    ScreenRect ILayeredWidget.Bounds => new(Left, Top, ActualWidth, ActualHeight);

    IntPtr ILayeredWidget.LayerHandle => Handle;

    bool ILayeredWidget.AlwaysOnTop
    {
        get => Settings.AlwaysOnTop;
        set => Settings.AlwaysOnTop = value;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        DesktopAttachment.Attach(Handle);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        _host.RestackWidgets();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _context.RaiseClosed();
    }

    /// <summary>Closes the window because the widget was removed or plugins were turned off.</summary>
    internal void ClosePluginWindow()
    {
        _closedByHost = true;
        Close();
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

    /// <summary>Shows the widget again after "Hide all", back on the desktop layer.</summary>
    internal void ShowOnDesktop()
    {
        Show();
        _host.RestackWidgets();
    }

    // ---- Colour and gloss: the same choices, and the same code path, as a folder widget ----

    private void ApplyColor(uint? argb)
    {
        // In High Contrast the system colours win, whatever colour was chosen.
        if (argb is { } color && !SystemParameters.HighContrast)
        {
            // With Windows transparency effects off the card is solid, so text is chosen against the colour itself.
            var solid = WidgetTheme.IsTransparencyEnabled() ? color : color | 0xFF000000;
            WidgetTheme.Apply(Resources, WidgetPalette.ForBackground(solid, _host.WallpaperColor));
        }
        else
        {
            WidgetTheme.Clear(Resources);
        }
    }

    private void ApplyGloss() =>
        Gloss.Visibility = Settings.GlossEnabled && !SystemParameters.HighContrast ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Re-works out colour and gloss after the wallpaper or a Windows setting changed.</summary>
    internal void RefreshColor()
    {
        ApplyColor(SavedColor);
        ApplyGloss();
    }

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
        _host.RestackWidgets();
    }

    private void Gloss_Click(object sender, RoutedEventArgs e)
    {
        Settings.GlossEnabled = !Settings.GlossEnabled;
        ApplyGloss();
        _host.SaveSettings();
    }

    // ---- Keyboard focus: only while someone is typing ----

    /// <summary>Brings the widget forward and moves focus into its contents (the tray's "Go to widget").</summary>
    internal void FocusFromKeyboard()
    {
        if (!IsVisible)
        {
            ShowOnDesktop();
        }

        BeginTyping();
        BodyHost.UpdateLayout();
        BodyHost.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }

    private void BeginTyping()
    {
        if (_typing)
        {
            return;
        }

        var previous = NativeMethods.GetForegroundWindow();
        if (previous != Handle)
        {
            _previousForeground = previous;
        }

        _typing = true;
        DesktopAttachment.SetActivatable(Handle, true);
        Activate();
    }

    private void EndTyping(bool restoreForeground)
    {
        if (!_typing)
        {
            return;
        }

        _typing = false;
        Keyboard.ClearFocus();
        DesktopAttachment.SetActivatable(Handle, false);
        _host.RestackWidgets();

        if (restoreForeground && _previousForeground != IntPtr.Zero)
        {
            NativeMethods.SetForegroundWindow(_previousForeground);
        }

        _previousForeground = IntPtr.Zero;
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);

        // Switched to another window: back to the desktop layer, without pulling focus back.
        if (!_closedByHost)
        {
            EndTyping(restoreForeground: false);
        }
    }

    private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // A click in a text box is the one click that needs the window to take focus.
        if (AncestorOf<TextBoxBase>(e.OriginalSource) is { } box)
        {
            BeginTyping();
            Keyboard.Focus(box);
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _typing && Card.ContextMenu is not { IsOpen: true })
        {
            EndTyping(restoreForeground: true);
            e.Handled = true;
        }
    }

    // ---- Moving: a click on a control is the plugin's; a click on anything else drags the widget ----

    private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Only controls inside the card count: the window itself is a Control too, so an unbounded walk would
        // always find it and the widget could never be dragged.
        if (AncestorOf<Control>(e.OriginalSource, stopAt: Card) is not null)
        {
            return;
        }

        var (left, top) = (Left, Top);
        DragMove(); // blocks until the button is released, so this saves once, when dragging is complete
        _host.RestackWidgets();
        if (Left != left || Top != top)
        {
            Settings.Left = Left;
            Settings.Top = Top;
            _host.SaveSettings();
        }
    }

    /// <summary>The closest element of this type at or above the clicked one (text runs have no visual, so the logical tree is used for them).</summary>
    private static T? AncestorOf<T>(object source, DependencyObject? stopAt = null)
        where T : DependencyObject
    {
        for (var current = source as DependencyObject; current is not null && current != stopAt; current = ParentOf(current))
        {
            if (current is T match)
            {
                return match;
            }
        }

        return null;
    }

    private static DependencyObject? ParentOf(DependencyObject element) =>
        element is Visual ? VisualTreeHelper.GetParent(element) : LogicalTreeHelper.GetParent(element);

    // ---- Menu ----

    // Showing the right-click menu can lift the widget above its neighbours. Put the saved order back as the
    // menu opens and again as it closes, so looking at the menu never changes which widget is on top.
    private void RestackSoon() =>
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(_host.RestackWidgets));

    private void ContextMenu_Closed(object sender, RoutedEventArgs e) => RestackSoon();

    private void ContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        RestackSoon();
        GlossItem.IsChecked = Settings.GlossEnabled;
        PluginMenu.Fill(AddPluginMenu, _host);
        LayerMenu.Fill(LayerItem, _host, this);
    }

    private void Remove_Click(object sender, RoutedEventArgs e) => _host.Plugins.Remove(this);

    private void AddWidget_Click(object sender, RoutedEventArgs e) => _host.AddWidget();

    private void Preferences_Click(object sender, RoutedEventArgs e) => _host.ShowPreferences();

    private void About_Click(object sender, RoutedEventArgs e) => _host.ShowAbout();

    private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}
