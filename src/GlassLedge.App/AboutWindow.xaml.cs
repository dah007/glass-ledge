using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using GlassLedge.Core.Services;

namespace GlassLedge.App;

/// <summary>Version, author and links: GitHub, LinkedIn, Buy Me a Coffee, and the licence.</summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        BrandTheme.Apply(Resources);

        var informational = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        VersionText.Text = $"Version {AboutInfo.VersionText(informational)} · Made by {AboutInfo.Author}";
        TaglineText.Text = AboutInfo.Tagline;

        GitHubButton.Tag = AboutInfo.GitHub;
        LinkedInButton.Tag = AboutInfo.LinkedIn;
        CoffeeButton.Tag = AboutInfo.BuyMeACoffee;
        LicenseLink.Tag = AboutInfo.License;

        GitHubText.Text = AboutInfo.DisplayText(AboutInfo.GitHub);
        LinkedInText.Text = AboutInfo.DisplayText(AboutInfo.LinkedIn);
        CoffeeText.Text = AboutInfo.DisplayText(AboutInfo.BuyMeACoffee);

        // Screen readers announce "GitHub, github.com/dah007" rather than the row's parts.
        SetAccessibleName(GitHubButton, "GitHub", AboutInfo.GitHub);
        SetAccessibleName(LinkedInButton, "LinkedIn", AboutInfo.LinkedIn);
        SetAccessibleName(CoffeeButton, "Buy me a coffee", AboutInfo.BuyMeACoffee);
    }

    private static void SetAccessibleName(UIElement element, string label, Uri link) =>
        System.Windows.Automation.AutomationProperties.SetName(element, $"{label}, {AboutInfo.DisplayText(link)}");

    private void Link_Click(object sender, RoutedEventArgs e)
    {
        var link = sender switch
        {
            FrameworkElement element => element.Tag as Uri,
            Hyperlink hyperlink => hyperlink.Tag as Uri,
            _ => null,
        };
        if (link is null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(link.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Win32Exception ex)
        {
            MessageBox.Show(this, $"Couldn't open {link.AbsoluteUri}.\n\n{ex.Message}", Title,
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
