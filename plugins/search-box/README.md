# Search box

A text box with a **Search** button beside it. Type, press Enter or the button, and the web search opens in your default browser. The widget sits on the desktop with the same glass as the folder widgets, and has the same **Color…** and **Glossy** choices, because GlassLedge draws that part.

This is the first GlassLedge plugin and the **reference example**: it is small enough to read in a few minutes and uses every part of the plugin contract. To make your own, start from here: [docs/plugins/getting-started.md](../../docs/plugins/getting-started.md).

## Using it

1. Preferences > **Allow plugins** > OK.
2. Right-click a widget or the tray icon > **Add plugin widget** > **Search box**.
3. Click the box and type; **Enter** or **Search** opens the results; **Esc** gives the keyboard back to the window you were in.
4. Right-click it for **Color…**, **Glossy** and **Remove widget**.

The search engine is DuckDuckGo. To change it, edit `Template` in [`SearchUrl.cs`](SearchUrl.cs) and rebuild.

## The files

| File | What it is |
| --- | --- |
| [`plugin.json`](plugin.json) | The manifest: id `search-box`, the entry assembly and class, and the contents size (320 x 40). |
| [`SearchBox.csproj`](SearchBox.csproj) | The project. Commented line by line: why `EnableDynamicLoading`, why `Private=false`, how it is staged for the app. |
| [`SearchBoxPlugin.cs`](SearchBoxPlugin.cs) | The entry point (`IWidgetPlugin`). One line: it returns the view. |
| [`SearchBoxView.cs`](SearchBoxView.cs) | The contents: a text box, a hint, a button. Enter and the button call `Search()`, which asks GlassLedge to open the address. |
| [`SearchBoxStyles.cs`](SearchBoxStyles.cs) | The look of the box and button, in XAML text using the widget's colour names, so they follow the widget's colour, light/dark and High Contrast. |
| [`SearchUrl.cs`](SearchUrl.cs) | Pure logic: trims, limits and escapes the text and builds the address. Tested without any UI. |

## How it maps to the contract

- **Contents only.** No window, no background, no border: GlassLedge's glass is behind it.
- **Theming** is by name (`WidgetTileBrush`, `WidgetAccentBrush`, …). Nothing is hard-coded.
- **Opening the browser** goes through `IWidgetContext.TryOpenUri`, which allows only `http` and `https`.
- **Accessibility:** both controls have names for screen readers; the box and button show an accent ring for keyboard focus.

## Tests

[`tests/GlassLedge.Plugins.Tests`](../../tests/GlassLedge.Plugins.Tests): `SearchUrlTests` (escaping, trimming, length) and `SearchBoxViewTests` (searching opens the right address, an empty box opens nothing, the hint shows only while empty), run against a fake context so no app is needed.

```powershell
dotnet test tests/GlassLedge.Plugins.Tests
```
