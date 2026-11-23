using System.Text.Json;
using System.Xml.Linq;

namespace GlassLedge.Tests;

/// <summary>
/// Guards the Microsoft Store package inputs (manifest template, logos, identity file). It can't build the
/// MSIX itself (that needs the Windows SDK), but it catches the mistakes that fail Store certification.
/// </summary>
public sealed class MsixPackagingTests
{
    private static readonly XNamespace Foundation = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
    private static readonly XNamespace Uap = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
    private static readonly XNamespace Desktop = "http://schemas.microsoft.com/appx/manifest/desktop/windows10";
    private static readonly XNamespace Rescap = "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities";

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "GlassLedge.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("GlassLedge.slnx not found above the test binaries.");
    }

    private static XDocument Manifest() =>
        XDocument.Load(Path.Combine(RepoRoot(), "packaging", "msix", "AppxManifest.template.xml"));

    [Fact]
    public void Identity_IsFilledFromTokensTheBuildScriptReplaces()
    {
        var identity = Manifest().Root!.Element(Foundation + "Identity")!;

        Assert.Equal("{{IdentityName}}", (string?)identity.Attribute("Name"));
        Assert.Equal("{{Publisher}}", (string?)identity.Attribute("Publisher"));
        Assert.Equal("{{Version}}", (string?)identity.Attribute("Version"));
        Assert.Equal("x64", (string?)identity.Attribute("ProcessorArchitecture"));
        Assert.Equal("{{PublisherDisplayName}}", (string?)Manifest().Root!.Element(Foundation + "Properties")!.Element(Foundation + "PublisherDisplayName"));
    }

    [Fact]
    public void Application_RunsTheAppExecutableAsAFullTrustApp()
    {
        var application = Manifest().Root!.Descendants(Foundation + "Application").Single();
        var project = File.ReadAllText(Path.Combine(RepoRoot(), "src", "GlassLedge.App", "GlassLedge.App.csproj"));

        Assert.Equal("GlassLedge.exe", (string?)application.Attribute("Executable"));
        Assert.Equal("Windows.FullTrustApplication", (string?)application.Attribute("EntryPoint"));
        Assert.Contains("<AssemblyName>GlassLedge</AssemblyName>", project, StringComparison.Ordinal);
    }

    [Fact]
    public void Capabilities_AreOnlyRunFullTrust()
    {
        var capabilities = Manifest().Root!.Element(Foundation + "Capabilities")!.Elements().ToList();

        var only = Assert.Single(capabilities);
        Assert.Equal(Rescap + "Capability", only.Name);
        Assert.Equal("runFullTrust", (string?)only.Attribute("Name"));
    }

    [Fact]
    public void StartupTask_IsOffUntilTheUserTurnsItOn()
    {
        var task = Manifest().Root!.Descendants(Desktop + "StartupTask").Single();

        Assert.Equal("false", (string?)task.Attribute("Enabled"));
    }

    [Fact]
    public void Targets_Windows11Desktop()
    {
        var target = Manifest().Root!.Element(Foundation + "Dependencies")!.Element(Foundation + "TargetDeviceFamily")!;

        Assert.Equal("Windows.Desktop", (string?)target.Attribute("Name"));
        Assert.Equal("10.0.22000.0", (string?)target.Attribute("MinVersion"));
    }

    [Fact]
    public void DisplayName_IsTheReservedProductName()
    {
        var root = Manifest().Root!;

        Assert.Equal("GlassLedge", (string?)root.Element(Foundation + "Properties")!.Element(Foundation + "DisplayName"));
        Assert.Equal("GlassLedge", (string?)root.Descendants(Uap + "VisualElements").Single().Attribute("DisplayName"));
    }

    [Fact]
    public void EveryLogoTheManifestNamesHasAStandardScaleFile()
    {
        var root = Manifest().Root!;
        var visual = root.Descendants(Uap + "VisualElements").Single();
        var logos = new[]
        {
            (string?)root.Element(Foundation + "Properties")!.Element(Foundation + "Logo"),
            (string?)visual.Attribute("Square150x150Logo"),
            (string?)visual.Attribute("Square44x44Logo"),
            (string?)visual.Element(Uap + "DefaultTile")!.Attribute("Wide310x150Logo"),
        };

        foreach (var logo in logos)
        {
            Assert.False(string.IsNullOrEmpty(logo));
            var stem = Path.GetFileNameWithoutExtension(logo!);
            var folder = Path.Combine(RepoRoot(), "packaging", "msix", Path.GetDirectoryName(logo!)!);
            Assert.True(File.Exists(Path.Combine(folder, $"{stem}.scale-100.png")), $"{logo} has no {stem}.scale-100.png");
            Assert.True(File.Exists(Path.Combine(folder, $"{stem}.scale-200.png")), $"{logo} has no {stem}.scale-200.png");
        }
    }

    [Fact]
    public void IdentityFile_HasTheThreeValuesPartnerCenterGives()
    {
        var path = Path.Combine(RepoRoot(), "packaging", "msix", "store-identity.json");
        using var json = JsonDocument.Parse(File.ReadAllText(path));

        foreach (var name in new[] { "identityName", "publisher", "publisherDisplayName" })
        {
            Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty(name).GetString()), name);
        }
    }
}
