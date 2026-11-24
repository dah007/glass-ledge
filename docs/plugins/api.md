# Plugin API reference

Everything a plugin compiles against is in the `GlassLedge.PluginApi` project (`src/GlassLedge.PluginApi`). It is small on purpose. The code has full XML documentation, shown by your editor; this page is the overview.

Reference it **without copying it**, so the app and the plugin share one copy of every type:

```xml
<ProjectReference Include="..\..\src\GlassLedge.PluginApi\GlassLedge.PluginApi.csproj">
  <Private>false</Private>
</ProjectReference>
```

## `IWidgetPlugin`

```csharp
public interface IWidgetPlugin
{
    FrameworkElement CreateBody(IWidgetContext context);
}
```

The class `entryType` names implements this and needs a public parameterless constructor. GlassLedge makes **one instance** per plugin and calls `CreateBody` **once for each widget** the person adds, on the UI thread. Return any WPF element. It is sized to the manifest's `size` and placed inside the glass.

## `IWidgetContext`

What one widget may ask of GlassLedge.

| Member | Meaning |
| --- | --- |
| `string PluginId` | The id from `plugin.json`. |
| `Guid WidgetId` | Identifies this widget; stable across restarts. Use it to key per-widget data. |
| `bool TryOpenUri(Uri uri)` | Opens a **web address** (`http` or `https` only) in the default browser. Returns false for any other kind of address or if the browser can't start. Use it instead of starting a process yourself. |
| `event EventHandler? Closed` | Raised when the widget is removed or plugins are switched off. Stop timers and unsubscribe from events here. |

## `PluginTheme`

Constants naming the colours the glass provides, and `Bind(element, property, key)`. See [theming.md](theming.md).

## `PluginApiInfo`

`PluginApiInfo.Version` (currently `1`): the contract's version. See [manifest.md](manifest.md#versioning-the-contract).

## What the plugin does not get

The glass window, its colour and gloss, dragging, the saved position, the right-click menu and keyboard hand-over are all handled by GlassLedge. Plugins don't draw their own window or reach the window object. (Plugins *are* ordinary .NET code and can call any .NET API, which is why they are [trusted code](security.md).)

## Behaviour to know about

- **Keyboard focus.** Widgets sit on the desktop layer and don't take focus. Clicking a text box in a plugin makes its window take keyboard focus until the person presses **Esc** (focus goes back to the previous window) or clicks elsewhere. Other controls (buttons) work with the mouse without taking focus; the tray's **Go to widget** moves keyboard focus to the first control.
- **Dragging.** A click that lands on a `Control` belongs to the plugin; a click on anything else (text, panels, the glass) drags the widget.
- **Failures.** Exceptions from the plugin's constructor or `CreateBody` are caught; the person sees a tray notification and the widget is skipped.
