# plugins

Every GlassLedge plugin lives here, in its own folder: **`plugins/<plugin-id>/`**. The folder name is the plugin's id, and it is the same folder that ends up (built) in `plugins\<id>\` beside the app, or in `%APPDATA%\GlassLedge\plugins\<id>\`.

| Plugin | What it does |
| --- | --- |
| [`search-box`](search-box/README.md) | A text box and a Search button; opens the web search in your browser. **The reference example.** |

## What a plugin folder holds

```
plugins/<id>/
  <Name>.csproj     a class library: net10.0-windows, UseWPF, EnableDynamicLoading
  plugin.json       the manifest (id, name, entry, size, …)
  <Plugin>.cs       a class implementing IWidgetPlugin
  …                 the plugin's other code
  README.md         what it is, how it works, how to change it
```

## Start a new one

1. Copy [`search-box`](search-box) to `plugins/<your-id>` and rename things (the checklist is in [docs/plugins/getting-started.md](../docs/plugins/getting-started.md)).
2. `dotnet build plugins/<your-id>/<Name>.csproj`
3. Drop the build output in `%APPDATA%\GlassLedge\plugins\<your-id>\`, turn on **Preferences > Allow plugins**, and add it from **Add plugin widget**.

## Documentation

All in [docs/plugins](../docs/plugins/README.md): getting started, the `plugin.json` reference, the API, theming, security, troubleshooting and architecture.

## Rules for plugins in this repository

- One folder per plugin, named for its id.
- Comment the code for the next reader; the Search box is the model.
- Colours only through `PluginTheme` names; no windows of its own.
- Tests go in `tests/GlassLedge.Plugins.Tests`.
- Plugins that ship with the app are added to `src/GlassLedge.App/GlassLedge.App.csproj` (one `ProjectReference` line) and to `GlassLedge.slnx`.
