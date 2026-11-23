using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using GlassLedge.Core.Models;
using GlassLedge.TestSupport;

namespace GlassLedge.E2E;

/// <summary>What a screen reader sees: each tile is a button with a spoken name, and the widget has a name.</summary>
[Trait("Category", "E2E")]
public sealed class AccessibilityTests
{
    [Fact]
    public void Tiles_AreButtonsWithSpokenNames()
    {
        using var folder = new TempDirectory();
        File.WriteAllText(folder.Combine("a.txt"), "a");
        Directory.CreateDirectory(folder.Combine("sub"));

        using var env = new IsolatedAppEnvironment();
        var process = env.Launch(widgets:
        [
            new FolderWindowSettings { FolderPath = folder.Path, Size = WidgetSize.Large },
        ]);

        using var automation = new UIA3Automation();
        var window = AppAutomation.AttachMainWindow(process, automation);

        var file = window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("a.txt")));
        var directory = window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("sub, folder")));

        Assert.NotNull(file);
        Assert.NotNull(directory);
        Assert.True(file!.Properties.IsKeyboardFocusable.Value);

        AppAutomation.ExitViaContextMenu(window, automation, process);
    }
}
