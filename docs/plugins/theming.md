# Theming: matching the widget

A plugin never draws the glass. It only needs its own controls (text boxes, buttons, labels) to look like they belong *on* the glass. The trick is to use the widget's colours **by name**, so they follow along automatically.

## The names

`GlassLedge.PluginApi.PluginTheme` lists them. In XAML use them as `DynamicResource` keys:

| Constant | Resource key | Use for |
| --- | --- | --- |
| `PluginTheme.Text` | `WidgetTextBrush` | Main text. Readable whatever colour the widget is. |
| `PluginTheme.SecondaryText` | `WidgetSecondaryTextBrush` | Hints and captions. |
| `PluginTheme.Tile` | `WidgetTileBrush` | Fill of inputs and buttons. |
| `PluginTheme.TileHover` | `WidgetTileHoverBrush` | The tile under the mouse. |
| `PluginTheme.Accent` | `WidgetAccentBrush` | Focus rings and highlights. |
| `PluginTheme.Border` | `WidgetBorderBrush` | The widget's rim colour. |

In code, `PluginTheme.Bind(element, property, key)` does the same as `SetResourceReference`.

These are **dynamic** references: when the person picks a colour with **Color…**, when Windows switches light/dark, when Transparency effects change, or in High Contrast, the brushes are replaced and your controls repaint. Use `DynamicResource`, never `StaticResource`, and never copy a brush's colour into your own.

## Where the styles live

The Search box keeps its two styles (text box and button) as XAML in a string and parses them with `XamlReader` ([`SearchBoxStyles.cs`](../../plugins/search-box/SearchBoxStyles.cs)), then adds them to the body's `Resources`. Why not a `.xaml` file? WPF's compiled resource ("pack") addresses don't cross the boundary between a plugin's load context and the app, so a `ResourceDictionary` loaded by pack URI would fail. Parsing text, or building controls in code, works everywhere.

Any style you author this way can reference the widget keys with `{DynamicResource WidgetTextBrush}`: they are looked up through the widget's window at run time.

## Checklist

- Text and glyphs: `Text` or `SecondaryText`. Don't set a fixed colour.
- Inputs and buttons: `Tile` fill, `TileHover` on hover, a `Accent` border while keyboard-focused, so keyboard users can see where they are.
- Don't add a background or border to the whole body. The glass is behind it.
- Give each control an accessible name with `AutomationProperties.SetName`.
- Try it: add the widget, use **Color…** with a pale and a dark colour, toggle Windows light/dark, and turn on a High Contrast theme.
