# Build your first plugin

This walks through making a plugin the way the Search box was made. You need the .NET 10 SDK and a clone of the repository. The finished example is in [`plugins/search-box`](../../plugins/search-box); keep it open beside this page.

## 1. Copy the example

Copy `plugins/search-box` to `plugins/<your-id>`. The id is lower-case letters, digits and single hyphens (`clock`, `quick-notes`). The folder name **must equal the id**.

Rename the project file (`SearchBox.csproj` to `<YourName>.csproj`) and edit it:

- `AssemblyName` and `RootNamespace` to your own names.
- In the `StageForGlassLedge` target, change `SearchBox.dll`, `SearchBox.deps.json`, `SearchBox.xml` and the `search-box` staging folder name to yours. (Only needed if the plugin should ship inside the app; see step 6.)
- Leave the `ProjectReference` to `GlassLedge.PluginApi` with `<Private>false</Private>`. That means "compile against it, don't copy it". The app supplies the real copy so both sides agree on the types.

## 2. Write `plugin.json`

```json
{
  "id": "clock",
  "name": "Clock",
  "version": "1.0.0",
  "description": "Shows the time.",
  "author": "you",
  "apiVersion": 1,
  "entry": "Clock.dll",
  "entryType": "Clock.ClockPlugin",
  "size": { "width": 200, "height": 60 }
}
```

`entry` is your built `.dll`; `entryType` is the full name (namespace and class) of the class in step 3. Every field is explained in [manifest.md](manifest.md).

## 3. Write the plugin class

```csharp
using System.Windows;
using System.Windows.Controls;
using GlassLedge.PluginApi;

namespace Clock;

public sealed class ClockPlugin : IWidgetPlugin
{
    public FrameworkElement CreateBody(IWidgetContext context)
    {
        var text = new TextBlock { FontSize = 28, HorizontalAlignment = HorizontalAlignment.Center,
                                   VerticalAlignment = VerticalAlignment.Center };
        PluginTheme.Bind(text, TextBlock.ForegroundProperty, PluginTheme.Text);   // follows the widget's colours
        text.Text = DateTime.Now.ToString("t");
        return text;
    }
}
```

`CreateBody` is called once for every widget the person adds, on the UI thread. Return any WPF element. GlassLedge gives it the `size` from the manifest and puts it in the glass. Colours: use the names in `PluginTheme` ([theming.md](theming.md)).

If your widget needs a timer or an event subscription, clean it up in `context.Closed`:

```csharp
var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
timer.Tick += (_, _) => text.Text = DateTime.Now.ToString("t");
timer.Start();
context.Closed += (_, _) => timer.Stop();
```

## 4. Build

```powershell
dotnet build plugins/clock/Clock.csproj -c Debug
```

The output folder (`plugins/clock/bin/Debug/net10.0-windows/`) now holds `Clock.dll`, `Clock.deps.json` and `plugin.json`. That folder's contents are the plugin.

## 5. Try it

Copy those files to `%APPDATA%\GlassLedge\plugins\clock\` (the folder name is the id), switch on **Preferences > Allow plugins**, then **Add plugin widget > Clock**. If it doesn't show up or fails to start, see [troubleshooting.md](troubleshooting.md).

## 6. Ship it with the app (optional)

For a plugin that belongs in the repository and the installer:

1. Keep it in `plugins/<id>`, with the `StageForGlassLedge` target from the example (it copies the built files to `artifacts/plugins/<configuration>/<id>/`).
2. Add one line to `src/GlassLedge.App/GlassLedge.App.csproj`, beside the Search box's:
   ```xml
   <ProjectReference Include="..\..\plugins\clock\Clock.csproj" ReferenceOutputAssembly="false" />
   ```
3. Add the project to `GlassLedge.slnx`, and tests in `tests/GlassLedge.Plugins.Tests`.

The app build then copies it to `plugins\clock\` beside the exe, and the installer and Store package include it because they include the `plugins` folder.

## 7. Test it

Plugins are plain classes, so they are easy to test without the app. `tests/GlassLedge.Plugins.Tests` has a `FakeWidgetContext` (records what the plugin asks to open) and an `Sta` helper (WPF controls need an STA thread). See `SearchBoxViewTests` for the pattern.

## Rules of thumb

- Keep `CreateBody` quick. It runs on the UI thread while the widget appears.
- Don't throw from it, and catch your own errors. If it does throw, GlassLedge shows a notice and skips the widget; it never takes the app down.
- Don't draw your own window, background or border. That is the glass.
- Don't hard-code colours. Use `PluginTheme`, so light/dark, High Contrast and the person's colour choice all work.
- Give controls an accessible name (`AutomationProperties.SetName`) so screen readers can announce them.
