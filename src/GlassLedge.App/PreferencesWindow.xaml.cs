using System.Windows;
using GlassLedge.Core.Models;

namespace GlassLedge.App;

/// <summary>
/// Start with Windows, Open folders in a pane, Allow plugins. Nothing is applied until OK; Cancel, Esc and
/// the close button discard the changes. The caller reads <see cref="Result"/> after OK.
/// </summary>
public partial class PreferencesWindow : Window
{
    public PreferencesWindow(PreferencesState initial, bool startupManagedByWindows)
    {
        InitializeComponent();
        BrandTheme.Apply(Resources);

        StartSwitch.IsChecked = initial.StartWithWindows;
        PaneSwitch.IsChecked = initial.OpenFoldersInPane;
        PluginsSwitch.IsChecked = initial.AllowPlugins;

        if (startupManagedByWindows)
        {
            // The Store build can't write the Run entry; Windows owns the switch.
            StartSwitch.IsEnabled = false;
            StartDetail.Text = "Managed by Windows. Open Settings > Apps > Startup to change it.";
        }

        Result = initial;
    }

    public PreferencesState Result { get; private set; }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Result = new PreferencesState(
            StartSwitch.IsChecked == true,
            PaneSwitch.IsChecked == true,
            PluginsSwitch.IsChecked == true);
        DialogResult = true;
    }
}
