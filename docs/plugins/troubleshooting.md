# Troubleshooting plugins

## "Add plugin widget" says "Turn on plugins in Preferences…"

**Allow plugins** is off. Preferences > Allow plugins > **OK**.

## "No plugins found", or your plugin isn't in the list

GlassLedge looked in `plugins\` beside the exe and in `%APPDATA%\GlassLedge\plugins` and didn't find your plugin as a valid one. Check:

- The plugin is a **folder directly inside** one of those two folders (not nested another level).
- The folder holds a `plugin.json`.
- The folder's name **equals** the manifest's `id`.
- The `.dll` named by `entry` is **in that folder**. Did you build the project and copy the build output?
- `plugin.json` is valid ([manifest.md](manifest.md)): every required field, an `apiVersion` this GlassLedge supports, and a `size` within the limits.
- No other plugin has the same `id`. If one does, the first one found is used (the app's own folder is searched first).

GlassLedge skips a plugin that fails any of these checks without saying why, so go through the list above.

## "The "…" plugin couldn't start: …"

| Message says | Usually means |
| --- | --- |
| `…wasn't found in X.dll. Check "entryType"` | Typo in `entryType`; it must be the full name (namespace and class). |
| `…doesn't implement IWidgetPlugin` | The class doesn't implement `IWidgetPlugin`, or it was built against a different copy of `GlassLedge.PluginApi`. Make sure the project reference has `<Private>false</Private>` and that no `GlassLedge.PluginApi.dll` sits in the plugin folder. |
| `…needs a public parameterless constructor` | Add one. |
| `couldn't be loaded` | The file isn't a .NET assembly, or it targets a different framework. Target `net10.0-windows` with `UseWPF`. |
| a message from your own code | An exception in your constructor or `CreateBody`. Fix it, rebuild, copy, and add the widget again. |

## It loads, but looks wrong

- Colours don't change with **Color…**: you probably used `StaticResource` or a fixed colour. See [theming.md](theming.md).
- A custom style fails to load: load it with `XamlReader.Parse` or build it in code. Pack URIs don't work for plugins.

## It works on my machine but not for a friend

Copy **everything** from the build output, not just the `.dll`: `plugin.json`, `<name>.deps.json`, and any libraries the plugin uses.

## Updating a plugin

A plugin that has been loaded stays loaded until GlassLedge exits, and Windows keeps its `.dll` locked meanwhile. To update one: exit GlassLedge (tray > **Exit**), replace the files, then start GlassLedge again.

To pick up a **new** plugin folder without restarting, turn **Allow plugins** off and on again: GlassLedge looks for plugins each time it is turned on.
