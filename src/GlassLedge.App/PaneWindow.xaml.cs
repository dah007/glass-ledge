using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GlassLedge.App.Interop;
using GlassLedge.Core.Services;

namespace GlassLedge.App;

/// <summary>One row in a pane: a file or a sub-folder.</summary>
public sealed record PaneItem(string Name, string FullPath, bool IsFolder, ImageSource? Icon, string SpokenName);

/// <summary>
/// A folder opened inside GlassLedge instead of File Explorer ("Open folders in a pane"). Folders open in
/// place, with Back and Up; files open in their own app.
/// </summary>
public partial class PaneWindow : Window
{
    private readonly FolderPaneNavigator _navigator;

    public PaneWindow(string folder)
    {
        InitializeComponent();
        BrandTheme.Apply(Resources);

        _navigator = new FolderPaneNavigator(folder);
        Load();
    }

    private void Load()
    {
        Title = _navigator.Title;
        PathText.Text = _navigator.Current;
        AutomationProperties.SetName(PathText, "Current folder, " + _navigator.Current);
        BackButton.IsEnabled = _navigator.CanGoBack;
        UpButton.IsEnabled = _navigator.CanGoUp;

        try
        {
            var entries = FolderContents.List(_navigator.Current);
            Items.ItemsSource = entries
                .Select(entry => new PaneItem(
                    entry.Name,
                    entry.FullPath,
                    entry.IsDirectory,
                    ShellIcons.GetIcon(entry.FullPath),
                    TileAccessibility.NameFor(entry)))
                .ToList();
            StatusText.Text = TileAccessibility.ItemCount(entries.Count);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Items.ItemsSource = null;
            StatusText.Text = "This folder isn't available.";
            return;
        }

        // Keep the keyboard going: land on the first item once the list has drawn.
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (Items.Items.Count > 0)
            {
                Items.SelectedIndex = 0;
                (Items.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem)?.Focus();
            }
        });
    }

    private void OpenItem(PaneItem item)
    {
        if (item.IsFolder)
        {
            _navigator.NavigateTo(item.FullPath);
            Load();
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(item.FullPath) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException)
        {
            MessageBox.Show(
                this,
                $"Couldn't open \"{item.Name}\".\n\n{ex.Message}",
                Title,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e) => GoBack();

    private void Up_Click(object sender, RoutedEventArgs e) => GoUp();

    private void GoBack()
    {
        if (_navigator.GoBack())
        {
            Load();
        }
    }

    private void GoUp()
    {
        if (_navigator.GoUp())
        {
            Load();
        }
    }

    private void Items_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Only a double-click on a row; the empty space below the rows does nothing.
        if (ItemsControl.ContainerFromElement(Items, e.OriginalSource as DependencyObject) is ListBoxItem
            {
                DataContext: PaneItem item,
            })
        {
            OpenItem(item);
        }
    }

    private void Items_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Items.SelectedItem is PaneItem item)
        {
            e.Handled = true;
            OpenItem(item);
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var alt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;

        if (alt && key == Key.Left)
        {
            e.Handled = true;
            GoBack();
        }
        else if (alt && key == Key.Up)
        {
            e.Handled = true;
            GoUp();
        }
        else if (key == Key.F5)
        {
            e.Handled = true;
            Load();
        }
    }
}
