# How plugins work

```
GlassLedge.exe
 ├─ PluginManager ── finds ──▶ plugins\<id>\plugin.json   (beside the exe)
 │                           └▶ %APPDATA%\GlassLedge\plugins\<id>\plugin.json
 │      (only if Allow plugins is on)
 ├─ PluginLoader ── loads ───▶ <id>.dll in its own AssemblyLoadContext
 │                              └ shares GlassLedge.PluginApi with the app
 └─ PluginWidgetWindow ◀── CreateBody(context) ── IWidgetPlugin (the plugin)
      glass, colour, gloss, drag, menu               contents only
```

## The pieces

| Piece | Where | Job |
| --- | --- | --- |
| Contract | `src/GlassLedge.PluginApi` | `IWidgetPlugin`, `IWidgetContext`, `PluginTheme`, `PluginApiInfo`. The only thing a plugin references. |
| Manifest | `Core/Services/PluginManifestReader.cs` | Parses and validates `plugin.json`. Pure logic; unit-tested. |
| Discovery | `Core/Services/PluginDiscovery.cs` | Walks the two folders, reports problems, resolves duplicates. No plugin code runs. |
| Settings | `Core/Models/PluginWidgetSettings.cs`, `AppSettings.PluginWidgets` | Which plugin, where, colour, gloss, per widget. |
| Loader | `App/PluginLoader.cs` | One `AssemblyLoadContext` per plugin; hands `GlassLedge.PluginApi` back to the app's copy so types match. |
| Manager | `App/PluginManager.cs` | Starts/stops plugins when **Allow plugins** changes, adds and removes widgets, never lets a plugin crash the app. |
| Window | `App/PluginWidgetWindow.xaml(.cs)` | The glass: the same card, rim and sheen as a folder widget, with **Color…** and **Glossy**, drag, remembered position and the menu. |
| Context | `App/PluginWidgetContext.cs` | The app's side of `IWidgetContext`. |

## Why the host draws the glass

Every widget looks and behaves the same way (Windows theme, High Contrast, colour picking, glossy), and a plugin author can't get it wrong or forget it. The plugin writes a few controls.

## Why one shared copy of the API

If the plugin brought its own `GlassLedge.PluginApi.dll`, the app and the plugin would each load a copy, and .NET treats the two copies of `IWidgetPlugin` as different types: the cast fails. `PluginLoadContext` therefore never loads the API itself, and the plugin's project references it with `Private=false`.

## Load order and lifetime

1. Startup: discovery runs (reads manifests). If **Allow plugins** is on, saved plugin widgets are recreated.
2. Preferences OK with the switch changed: discovery runs again and plugins start or stop immediately.
3. A widget is removed, or plugins are switched off: the window closes and the plugin's `Closed` event fires.
4. Assemblies stay loaded until the app exits (the load contexts aren't collectible), which is why updating a plugin needs a restart.

## Not yet (planned, not built)

Install-from-URL and a gallery, packaged `.fwoplugin` files, project templates and richer APIs (settings pages, per-widget saved data) are future phases.
