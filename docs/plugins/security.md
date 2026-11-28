# Security and trust

**A plugin is a program.** It runs inside GlassLedge with the same rights as GlassLedge, and can do anything you can do: read files, use the network, start programs. GlassLedge doesn't sandbox it.

That is why plugins are **off by default** and why the switch lives in Preferences with a warning.

## What GlassLedge does to keep you in control

- **Allow plugins is off until you turn it on.** While it's off, no plugin code is loaded or run. GlassLedge still reads each plugin's small `plugin.json` to list what's installed, but nothing in it is executed.
- **Turning it off takes effect at once.** Plugin widgets close; their saved positions and colours are kept.
- **Discovery is limited to two folders:** `plugins\` beside the app, and `%APPDATA%\GlassLedge\plugins`. Nothing is fetched from the internet. There is no install-from-URL.
- **The app's own plugins can't be silently replaced.** If a plugin in your own folder has the same id as one shipped with the app, the shipped one is used and yours is skipped.
- **A failing plugin can't take GlassLedge down.** Errors when a plugin starts are caught and shown as a notice.
- **`TryOpenUri` is limited to `http` and `https`**, so a well-behaved plugin has a safe way to open a link without starting a program.

## What you should do

- Only add plugins from sources you trust, and only if you've read or trust the code. Treat a plugin's `.dll` like any downloaded program.
- Keep **Allow plugins** off if you don't use plugins.
- Anything in `%APPDATA%\GlassLedge\plugins` is yours to review. Delete a plugin by deleting its folder.

## Microsoft Store builds

Loading code that isn't part of the app package may not be allowed in the Store edition. Check the current Store policy before shipping a Store build with plugin loading enabled; the Search box ships inside the package itself.
