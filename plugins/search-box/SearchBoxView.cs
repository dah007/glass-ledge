using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using GlassLedge.PluginApi;

namespace GlassLedge.Plugins.SearchBox;

/// <summary>
/// The contents of one search box widget: a text box with a hint, and a Search button beside it.
/// <para>
/// This is a <see cref="Grid"/> with two columns, built in code. Because GlassLedge sizes the body to the
/// <c>size</c> in <c>plugin.json</c>, the grid simply fills the space it is given.
/// </para>
/// </summary>
internal sealed class SearchBoxView : Grid
{
    private readonly IWidgetContext _context;
    private readonly TextBlock _hint;

    internal SearchBoxView(IWidgetContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        // The look. Put the styles in this element's resources so they apply to its children only.
        Resources.MergedDictionaries.Add(SearchBoxStyles.Create());

        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        QueryBox = new TextBox
        {
            MaxLength = SearchUrl.MaxLength,
            Style = (Style)FindResource(SearchBoxStyles.TextBoxKey),
        };
        // Screen readers announce this instead of an unnamed edit box.
        AutomationProperties.SetName(QueryBox, "Search the web");
        QueryBox.TextChanged += (_, _) => UpdateHint();
        QueryBox.KeyDown += OnQueryKeyDown;
        SetColumn(QueryBox, 0);
        Children.Add(QueryBox);

        // Hint text, drawn over the box while it is empty. It never takes the mouse (IsHitTestVisible = false),
        // so clicking "through" it still lands in the text box.
        _hint = new TextBlock
        {
            Text = "Search the web",
            FontSize = 14,
            Margin = new Thickness(14, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
        };
        PluginTheme.Bind(_hint, TextBlock.ForegroundProperty, PluginTheme.SecondaryText);
        SetColumn(_hint, 0);
        Children.Add(_hint);

        SearchButton = new Button
        {
            Content = "Search",
            Margin = new Thickness(8, 0, 0, 0),
            Style = (Style)FindResource(SearchBoxStyles.ButtonKey),
        };
        AutomationProperties.SetName(SearchButton, "Search");
        SearchButton.Click += (_, _) => Search();
        SetColumn(SearchButton, 1);
        Children.Add(SearchButton);
    }

    internal TextBox QueryBox { get; }

    internal Button SearchButton { get; }

    /// <summary>Opens the web search for what is typed. With nothing typed it just moves the caret into the box.</summary>
    internal void Search()
    {
        var url = SearchUrl.For(QueryBox.Text);
        if (url is null)
        {
            QueryBox.Focus();
            return;
        }

        // The widget never starts a browser itself; it asks GlassLedge, which only opens web addresses.
        if (_context.TryOpenUri(new Uri(url)))
        {
            QueryBox.Clear();
        }
        else
        {
            MessageBox.Show(
                "Couldn't open your web browser.",
                "Search box",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void OnQueryKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            Search();
        }
    }

    private void UpdateHint() =>
        _hint.Visibility = QueryBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
}
