using System.Windows;
using System.Windows.Markup;

namespace GlassLedge.Plugins.SearchBox;

/// <summary>
/// The look of the text box and button, as XAML kept in a string and parsed when the widget is built.
/// <para>
/// Why a string and not a <c>.xaml</c> file? GlassLedge loads each plugin into its own assembly context
/// so plugins can't clash with each other, and WPF's compiled-resource ("pack") addresses don't resolve
/// across that boundary. Parsing XAML text with <see cref="XamlReader"/> works everywhere, and building
/// controls directly in code works too. See docs/plugins/theming.md.
/// </para>
/// <para>
/// Every colour is a <c>DynamicResource</c> on one of the names in <c>PluginTheme</c> (the same names are
/// spelled out here because XAML can't read C# constants). That is the whole trick for matching the widget:
/// the colours change when Windows switches between light and dark, in High Contrast, and when the person
/// picks a colour for the widget, and these controls follow with no code.
/// </para>
/// </summary>
internal static class SearchBoxStyles
{
    internal const string TextBoxKey = "SearchBoxTextBoxStyle";
    internal const string ButtonKey = "SearchBoxButtonStyle";

    private const string Xaml = """
        <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

            <!-- The text box: a soft tile in the widget's own colours, with an accent ring while it has focus. -->
            <Style x:Key="SearchBoxTextBoxStyle" TargetType="TextBox">
                <Setter Property="FontSize" Value="14" />
                <Setter Property="Foreground" Value="{DynamicResource WidgetTextBrush}" />
                <Setter Property="CaretBrush" Value="{DynamicResource WidgetTextBrush}" />
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="TextBox">
                            <Border x:Name="Box"
                                    Background="{DynamicResource WidgetTileBrush}"
                                    BorderBrush="Transparent"
                                    BorderThickness="2"
                                    CornerRadius="12"
                                    Padding="10,0">
                                <ScrollViewer x:Name="PART_ContentHost" VerticalAlignment="Center" />
                            </Border>
                            <ControlTemplate.Triggers>
                                <Trigger Property="IsKeyboardFocusWithin" Value="True">
                                    <Setter TargetName="Box" Property="BorderBrush" Value="{DynamicResource WidgetAccentBrush}" />
                                </Trigger>
                            </ControlTemplate.Triggers>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>

            <!-- The Search button: the same tile, with a hover colour and the same accent ring for the keyboard. -->
            <Style x:Key="SearchBoxButtonStyle" TargetType="Button">
                <Setter Property="FontSize" Value="13" />
                <Setter Property="FontWeight" Value="SemiBold" />
                <Setter Property="Foreground" Value="{DynamicResource WidgetTextBrush}" />
                <Setter Property="FocusVisualStyle" Value="{x:Null}" />
                <Setter Property="Cursor" Value="Hand" />
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="Button">
                            <Border x:Name="Face"
                                    Background="{DynamicResource WidgetTileBrush}"
                                    BorderBrush="Transparent"
                                    BorderThickness="2"
                                    CornerRadius="12"
                                    Padding="14,0">
                                <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" />
                            </Border>
                            <ControlTemplate.Triggers>
                                <Trigger Property="IsMouseOver" Value="True">
                                    <Setter TargetName="Face" Property="Background" Value="{DynamicResource WidgetTileHoverBrush}" />
                                </Trigger>
                                <Trigger Property="IsKeyboardFocused" Value="True">
                                    <Setter TargetName="Face" Property="BorderBrush" Value="{DynamicResource WidgetAccentBrush}" />
                                </Trigger>
                            </ControlTemplate.Triggers>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>
        </ResourceDictionary>
        """;

    /// <summary>Parses the styles. Call it once per widget and put the result in the body's resources.</summary>
    internal static ResourceDictionary Create() => (ResourceDictionary)XamlReader.Parse(Xaml);
}
