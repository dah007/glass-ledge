using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using GlassLedge.App.Interop;
using GlassLedge.Core.Services;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace GlassLedge.App;

/// <summary>
/// The notification-area icon. Left-click hides or shows every widget; right-click (or the Menu key) opens a
/// themed WPF menu: Add widget, Go to widget (moves keyboard focus to a widget), Close widget, Hide all,
/// Show all, About, Exit.
/// </summary>
internal sealed class TrayIcon : IDisposable
{
    private const string AppName = "GlassLedge";

    private readonly App _app;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly ContextMenu _menu;
    private readonly MenuItem _goToWidgetItem;
    private readonly MenuItem _closeWidgetItem;
    private readonly MenuItem _hideAllItem;
    private readonly MenuItem _showAllItem;

    public TrayIcon(App app)
    {
        _app = app;

        _goToWidgetItem = new MenuItem { Header = "Go to widget" };
        _closeWidgetItem = new MenuItem { Header = "Close widget" };
        _hideAllItem = MenuEntry("Hide all", (_, _) => _app.HideAllWidgets());
        _showAllItem = MenuEntry("Show all", (_, _) => _app.ShowAllWidgets());

        _menu = new ContextMenu { Placement = PlacementMode.MousePoint };
        _menu.SetResourceReference(Control.BackgroundProperty, BrandTheme.MenuBrushKey);
        _menu.SetResourceReference(Control.BorderBrushProperty, BrandTheme.HairlineBrushKey);
        _menu.Items.Add(MenuEntry("Add widget…", (_, _) => _app.AddWidget()));
        _menu.Items.Add(_goToWidgetItem);
        _menu.Items.Add(_closeWidgetItem);
        _menu.Items.Add(new Separator());
        _menu.Items.Add(_hideAllItem);
        _menu.Items.Add(_showAllItem);
        _menu.Items.Add(new Separator());
        _menu.Items.Add(MenuEntry("About", (_, _) => _app.ShowAbout()));
        _menu.Items.Add(MenuEntry("Exit", (_, _) => _app.Shutdown()));

        // A menu opened from the tray only closes on an outside click if its window is in the
        // foreground; without this it can stay stuck open.
        _menu.Opened += (_, _) =>
        {
            if (PresentationSource.FromVisual(_menu) is HwndSource source)
            {
                NativeMethods.SetForegroundWindow(source.Handle);
            }
        };

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = AppName,
            Visible = true,
        };
        _notifyIcon.MouseClick += OnMouseClick;
    }

    /// <summary>Shows a Windows notification next to the tray icon.</summary>
    public void ShowNotification(string message) =>
        _notifyIcon.ShowBalloonTip(5000, AppName, message, Forms.ToolTipIcon.Info);

    public void Dispose()
    {
        // Hide first so no "ghost" icon lingers in the notification area after exit.
        _notifyIcon.Visible = false;
        _notifyIcon.Icon?.Dispose();
        _notifyIcon.Dispose();
    }

    private void OnMouseClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
        {
            _app.ToggleWidgetsHidden();
        }
        else if (e.Button == Forms.MouseButtons.Right)
        {
            UpdateMenu();
            _menu.IsOpen = true;
        }
    }

    private void UpdateMenu()
    {
        var widgets = _app.Widgets;

        _goToWidgetItem.Items.Clear();
        _closeWidgetItem.Items.Clear();
        var labels = WidgetName.MenuLabels(widgets.Select(widget => widget.Settings).ToList());
        for (var i = 0; i < widgets.Count; i++)
        {
            var widget = widgets[i];
            _goToWidgetItem.Items.Add(MenuEntry(labels[i], (_, _) => widget.FocusFromKeyboard()));
            _closeWidgetItem.Items.Add(MenuEntry(labels[i], (_, _) => _app.ConfirmAndRemoveWidget(widget)));
        }

        // Moving keyboard focus to a hidden widget would show nothing, so it waits for "Show all".
        _goToWidgetItem.IsEnabled = widgets.Count > 0 && !_app.WidgetsHidden;
        _closeWidgetItem.IsEnabled = widgets.Count > 0;
        _hideAllItem.IsEnabled = widgets.Count > 0 && !_app.WidgetsHidden;
        _showAllItem.IsEnabled = _app.WidgetsHidden;
    }

    private static MenuItem MenuEntry(string text, RoutedEventHandler onClick)
    {
        // Underscores mark access keys in WPF menu headers; double them so folder names show as typed.
        var item = new MenuItem { Header = text.Replace("_", "__", StringComparison.Ordinal) };
        item.Click += onClick;
        return item;
    }

    private static Drawing.Icon LoadIcon()
    {
        var resource = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"));
        if (resource is null)
        {
            return (Drawing.Icon)Drawing.SystemIcons.Application.Clone();
        }

        using var stream = resource.Stream;
        return new Drawing.Icon(stream, Forms.SystemInformation.SmallIconSize);
    }
}
