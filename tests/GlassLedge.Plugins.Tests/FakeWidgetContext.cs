using GlassLedge.PluginApi;

namespace GlassLedge.Plugins.Tests;

/// <summary>A stand-in for GlassLedge's side of the contract, so plugins can be tested without the app.</summary>
internal sealed class FakeWidgetContext : IWidgetContext
{
    public string PluginId { get; init; } = "test-plugin";

    public Guid WidgetId { get; } = Guid.NewGuid();

    /// <summary>What the plugin asked to open, in order.</summary>
    public List<Uri> Opened { get; } = [];

    /// <summary>Set to false to pretend the browser couldn't start.</summary>
    public bool OpenSucceeds { get; set; } = true;

    public event EventHandler? Closed;

    public bool TryOpenUri(Uri uri)
    {
        Opened.Add(uri);
        return OpenSucceeds;
    }

    public void RaiseClosed() => Closed?.Invoke(this, EventArgs.Empty);
}
