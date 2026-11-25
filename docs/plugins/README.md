# GlassLedge plugins

A plugin adds a new kind of widget to the desktop. GlassLedge draws the glass; the plugin supplies what is inside it.

| If you want to… | Read |
| --- | --- |
| Use plugins, or turn them on | [Using plugins](#using-plugins) below |
| Build your first plugin | [getting-started.md](getting-started.md) |
| Know every field in `plugin.json` | [manifest.md](manifest.md) |
| Know every type a plugin can use | [api.md](api.md) |
| Match the widget's colours, light/dark and High Contrast | [theming.md](theming.md) |
| Understand the trust model and **Allow plugins** | [security.md](security.md) |
| Fix a plugin that doesn't appear or won't start | [troubleshooting.md](troubleshooting.md) |
| See how the pieces fit together | [architecture.md](architecture.md) |

The first plugin, the **Search box**, is also the worked example: [`plugins/search-box`](../../plugins/search-box/README.md). Every plugin lives in its own folder under [`plugins/`](../../plugins/README.md).

## Using plugins

1. Right-click a widget or the tray icon and choose **Preferences…**. Turn on **Allow plugins** and press **OK**. It is off by default, because a plugin is a program (see [security.md](security.md)). It takes effect straight away; no restart.
2. Right-click a widget or the tray icon, then **Add plugin widget** and pick one, for example **Search box**.
3. Right-click the new widget for **Color…**, **Glossy**, **Layer** (bring to front, send to back, always on top and so on) and **Remove widget**. These work the same as on a folder widget, and they are the app's, so every plugin gets them.

Drag a plugin widget by its glass (anything that isn't a control). Where it sits, its colour and its glossy setting are remembered.

GlassLedge looks for plugins in two places:

| Folder | For |
| --- | --- |
| `plugins\` beside `GlassLedge.exe` | Plugins that ship with the app. Built from [`./plugins`](../../plugins/README.md). |
| `%APPDATA%\GlassLedge\plugins\` | Plugins you add yourself. Create the folder if it isn't there. |

A plugin is a folder holding a `plugin.json` and the `.dll` it names. If the same id is in both places, the one beside the app wins.
