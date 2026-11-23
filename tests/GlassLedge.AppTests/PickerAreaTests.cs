using System.Windows.Automation.Peers;
using GlassLedge.App;

namespace GlassLedge.AppTests;

/// <summary>What a screen reader sees on the colour picker's drag areas.</summary>
public sealed class PickerAreaTests
{
    /// <summary>WPF elements and their automation peers belong on an STA thread.</summary>
    private static void OnStaThread(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw new InvalidOperationException("The test failed on its STA thread.", failure);
        }
    }

    [Fact]
    public void Bar_IsASliderWithARangeAndValue()
    {
        OnStaThread(() =>
        {
            var area = new PickerArea { IsRange = true, Minimum = 0, Maximum = 360, SmallChange = 1, LargeChange = 15 };
            area.SetAutomationValue(210, "210 degrees");

            var peer = UIElementAutomationPeer.CreatePeerForElement(area);
            var range = (System.Windows.Automation.Provider.IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue)!;

            Assert.Equal(AutomationControlType.Slider, peer.GetAutomationControlType());
            Assert.Equal(210, range.Value);
            Assert.Equal(0, range.Minimum);
            Assert.Equal(360, range.Maximum);
            Assert.Equal(15, range.LargeChange);
            Assert.Null(peer.GetPattern(PatternInterface.Value));
        });
    }

    [Fact]
    public void Bar_SetValue_GoesThroughTheWindowsSetter_AndIsClamped()
    {
        OnStaThread(() =>
        {
            double? received = null;
            var area = new PickerArea { IsRange = true, Minimum = 0, Maximum = 100, ValueSetter = v => { received = v; return true; } };

            var peer = UIElementAutomationPeer.CreatePeerForElement(area);
            var range = (System.Windows.Automation.Provider.IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue)!;
            range.SetValue(250);

            Assert.Equal(100, received);
        });
    }

    [Fact]
    public void Square_ExposesItsTwoAxesAsAReadOnlyValue()
    {
        OnStaThread(() =>
        {
            var area = new PickerArea();
            area.SetAutomationValue(0.4, "saturation 40 percent, brightness 80 percent");

            var peer = UIElementAutomationPeer.CreatePeerForElement(area);
            var value = (System.Windows.Automation.Provider.IValueProvider)peer.GetPattern(PatternInterface.Value)!;

            Assert.Equal("saturation 40 percent, brightness 80 percent", value.Value);
            Assert.True(value.IsReadOnly);
            Assert.Null(peer.GetPattern(PatternInterface.RangeValue));
            Assert.Throws<InvalidOperationException>(() => value.SetValue("anything"));
        });
    }

    [Fact]
    public void Spoken_RoundsAndAddsTheUnit()
    {
        Assert.Equal("210 degrees", PickerArea.Spoken(209.6, "degrees"));
    }
}
