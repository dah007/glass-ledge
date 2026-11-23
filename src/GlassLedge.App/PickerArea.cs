using System.Globalization;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace GlassLedge.App;

/// <summary>
/// One of the colour picker's drag areas (saturation/brightness square, hue bar, opacity bar). It is a plain
/// Grid that the mouse drags on; this class makes it reachable without a mouse by exposing a value to screen
/// readers (<see cref="AutomationValue"/>) and taking the keyboard focus. The window handles the arrow keys.
/// </summary>
internal sealed class PickerArea : Grid
{
    /// <summary>The current value, as a screen reader should speak it, e.g. "210 degrees".</summary>
    public string SpokenValue { get; private set; } = string.Empty;

    /// <summary>For a one-dimensional area (hue, opacity): the value and its range. Ignored when <see cref="IsRange"/> is false.</summary>
    public double Value { get; private set; }

    public double Minimum { get; set; }

    public double Maximum { get; set; } = 100;

    /// <summary>Size of one arrow-key step, and of one Page Up/Down step.</summary>
    public double SmallChange { get; set; } = 1;

    public double LargeChange { get; set; } = 10;

    /// <summary>True for the hue and opacity bars, which are sliders; false for the two-dimensional square.</summary>
    public bool IsRange { get; set; }

    /// <summary>Called with a new value when a screen reader sets the slider; returns whether it was accepted.</summary>
    public Func<double, bool>? ValueSetter { get; set; }

    /// <summary>Updates what screen readers see and tells them about the change.</summary>
    public void SetAutomationValue(double value, string spokenValue)
    {
        var oldValue = Value;
        var oldSpoken = SpokenValue;
        Value = value;
        SpokenValue = spokenValue;

        if (UIElementAutomationPeer.FromElement(this) is PickerAreaPeer peer)
        {
            peer.Raise(oldValue, value, oldSpoken, spokenValue);
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new PickerAreaPeer(this);

    /// <summary>
    /// A slider for the hue and opacity bars (range value, so Narrator says "slider, 210"); a read-only value
    /// for the square, whose spoken value names both of its axes.
    /// </summary>
    private sealed class PickerAreaPeer : FrameworkElementAutomationPeer, IRangeValueProvider, IValueProvider
    {
        private readonly PickerArea _area;

        public PickerAreaPeer(PickerArea area)
            : base(area)
        {
            _area = area;
        }

        public double Value => _area.Value;

        public double Minimum => _area.Minimum;

        public double Maximum => _area.Maximum;

        public double SmallChange => _area.SmallChange;

        public double LargeChange => _area.LargeChange;

        bool IRangeValueProvider.IsReadOnly => _area.ValueSetter is null;

        string IValueProvider.Value => _area.SpokenValue;

        bool IValueProvider.IsReadOnly => true;

        public void SetValue(double value)
        {
            if (_area.ValueSetter is not { } setter || !setter(Math.Clamp(value, Minimum, Maximum)))
            {
                throw new InvalidOperationException("The value can't be set.");
            }
        }

        public void SetValue(string value)
        {
            // Read-only for the square; the colour can be typed into the Hex box instead.
            throw new InvalidOperationException("The value is read-only.");
        }

        public void Raise(double oldValue, double newValue, string oldSpoken, string newSpoken)
        {
            if (_area.IsRange)
            {
                RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, oldValue, newValue);
            }
            else
            {
                RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldSpoken, newSpoken);
            }
        }

        protected override AutomationControlType GetAutomationControlTypeCore() =>
            _area.IsRange ? AutomationControlType.Slider : AutomationControlType.Custom;

        protected override string GetClassNameCore() => nameof(PickerArea);

        protected override bool IsKeyboardFocusableCore() => true;

        public override object? GetPattern(PatternInterface patternInterface) => patternInterface switch
        {
            PatternInterface.RangeValue when _area.IsRange => this,
            PatternInterface.Value when !_area.IsRange => this,
            _ => base.GetPattern(patternInterface),
        };
    }

    /// <summary>Formats a number for <see cref="SetAutomationValue"/> with a unit, e.g. "210 degrees".</summary>
    public static string Spoken(double value, string unit) =>
        Math.Round(value).ToString(CultureInfo.CurrentCulture) + " " + unit;
}
