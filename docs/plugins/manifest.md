# plugin.json reference

Each plugin folder has a `plugin.json`. GlassLedge reads it **before** running any plugin code, to list the plugin and decide whether it can be loaded. Comments (`//`, `/* */`) and trailing commas are allowed.

```json
{
  "id": "search-box",
  "name": "Search box",
  "version": "1.0.0",
  "description": "Type, press Search, and the web search opens in your browser.",
  "author": "dah007",
  "apiVersion": 1,
  "entry": "SearchBox.dll",
  "entryType": "GlassLedge.Plugins.SearchBox.SearchBoxPlugin",
  "size": { "width": 320, "height": 40 }
}
```

| Field | Required | Rules |
| --- | --- | --- |
| `id` | yes | Lower-case letters, digits and single hyphens (`^[a-z0-9]+(-[a-z0-9]+)*$`). **Must equal the folder's name.** Unique: if two plugins share an id, the first one found is used (the app's own folder is searched first). |
| `name` | yes | Shown in the **Add plugin widget** menu and the tray's Go to / Close lists. |
| `version` | yes | The plugin's own version. Any text; semantic versions are conventional. |
| `description` | no | One line. Shown as a tooltip in the menu. |
| `author` | no | Who made it. |
| `apiVersion` | yes | A whole number, 1 or more, and **not higher than the version this GlassLedge supports** (currently 1). A newer plugin is refused with a hint to update GlassLedge. |
| `entry` | yes | The built assembly: a `.dll` file name in the plugin's folder. No path, no `..`. The file must exist. |
| `entryType` | yes | The full name (namespace and class) of the class in `entry` that implements `IWidgetPlugin`. It must have a public parameterless constructor. |
| `size` | no | The widget's **contents** size in device-independent pixels, `{ "width": …, "height": … }`. Width 120 to 800, height 24 to 400. GlassLedge adds 24 pixels of glass on each axis. Default 320 x 40. |

Every problem is reported at once, in plain words, in the tray notification and in [troubleshooting.md](troubleshooting.md)'s checklist. The rules live in code at `src/GlassLedge.Core/Services/PluginManifestReader.cs`, and `PluginManifestReaderTests` covers each one.

## Versioning the contract

`apiVersion` names the contract in `GlassLedge.PluginApi` that the plugin was built against (`PluginApiInfo.Version`). The number goes up only for a breaking change, so a plugin written for 1 keeps working for as long as GlassLedge supports 1.
