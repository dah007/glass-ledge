using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GlassLedge.Core.Models;
using GlassLedge.Core.Services;

namespace GlassLedge.App;

/// <summary>
/// Picks a widget background: saturation/brightness square, rainbow hue bar, opacity bar, hex box
/// and presets. <see cref="ColorChanged"/> fires on every change so the widget can preview it live.
/// </summary>
public partial class ColorPickerWindow : Window
{
    /// <summary>Opacity a first-time colour starts at: the same 72% as the theme glass.</summary>
    public const byte DefaultAlpha = 0xB8;

    /// <summary>Brand colours only: the dusk and frost families, graphite, and the amber family.</summary>
    internal static readonly (string Name, uint Rgb)[] Presets =
    [
        ("Dusk", 0x1B1F36), ("Twilight", 0x3A3F6B), ("Haze", 0x9DBDE0), ("Mist", 0xECEFF4),
        ("Frost", 0xF7F8FA), ("Graphite", 0x1C1C1E), ("Slate", 0x3A3A3C), ("Cream", 0xFFF3D9),
        ("Amber", 0xFFB020), ("Honey", 0xFFC24D), ("Marigold", 0xE89A00), ("Umber", 0x8A5300),
    ];

    private double _hue;
    private double _saturation;
    private double _value;
    private double _alpha;

    public ColorPickerWindow(uint? currentColor)
    {
        InitializeComponent();
        BrandTheme.Apply(Resources);

        var start = currentColor ?? ColorMath.ToArgb(DefaultAlpha, 0x1B, 0x1F, 0x36); // Dusk
        SetColor(start, keepAlpha: false, raiseChanged: false);
        SelectedColor = currentColor;

        foreach (var (name, rgb) in Presets)
        {
            var swatch = new Button
            {
                Style = (Style)FindResource("SwatchButton"),
                Background = WidgetTheme.ToBrush(0xFF000000 | rgb),
                ToolTip = name,
            };
            AutomationProperties.SetName(swatch, name);
            swatch.Click += (_, _) => SetColor(0xFF000000 | rgb, keepAlpha: true, raiseChanged: true);
            PresetPanel.Children.Add(swatch);
        }

        // A screen reader can set the two sliders directly; the square is keyboard-only (arrow keys) or typed as hex.
        HueArea.ValueSetter = value => SetFromBar(HueArea, value);
        AlphaArea.ValueSetter = value => SetFromBar(AlphaArea, value);

        Loaded += (_, _) => UpdateVisuals();
    }

    /// <summary>Raised whenever the colour changes, for a live preview.</summary>
    public event EventHandler? ColorChanged;

    /// <summary>
    /// The chosen colour (#AARRGGBB), or null when the user picked Default (follow the Windows theme).
    /// Only meaningful after the dialog returns true.
    /// </summary>
    public uint? SelectedColor { get; private set; }

    private uint CurrentArgb
    {
        get
        {
            var (r, g, b) = ColorMath.HsvToRgb(_hue, _saturation, _value);
            return ColorMath.ToArgb((byte)Math.Round(_alpha * 255), r, g, b);
        }
    }

    private void SetColor(uint argb, bool keepAlpha, bool raiseChanged)
    {
        var (a, r, g, b) = ColorMath.Split(argb);
        var (hue, saturation, value) = ColorMath.RgbToHsv(r, g, b);

        // Greys have no hue; keep the bar where it was rather than snapping to red.
        if (saturation > 0)
        {
            _hue = hue;
        }

        _saturation = saturation;
        _value = value;
        if (!keepAlpha)
        {
            _alpha = a / 255.0;
        }

        UpdateVisuals();
        if (raiseChanged)
        {
            OnColorChanged();
        }
    }

    private void OnColorChanged()
    {
        SelectedColor = CurrentArgb;
        ColorChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateVisuals()
    {
        var argb = CurrentArgb;
        var (hr, hg, hb) = ColorMath.HsvToRgb(_hue, 1, 1);
        var (_, r, g, b) = ColorMath.Split(argb);

        HueFill.Background = WidgetTheme.ToBrush(ColorMath.ToArgb(0xFF, hr, hg, hb));
        PreviewSwatch.Background = WidgetTheme.ToBrush(argb);
        PreviewText.Foreground = WidgetTheme.ToBrush(WidgetPalette.ForBackground(argb).Text);

        var alphaFill = new LinearGradientBrush(
            Color.FromArgb(0, r, g, b), Color.FromArgb(0xFF, r, g, b), new Point(0, 0), new Point(1, 0));
        alphaFill.Freeze();
        AlphaFill.Background = alphaFill;

        Place(SvThumb, _saturation * SvArea.ActualWidth, (1 - _value) * SvArea.ActualHeight);
        Place(HueThumb, _hue / 360 * HueArea.ActualWidth, null);
        Place(AlphaThumb, _alpha * AlphaArea.ActualWidth, null);

        OpacityText.Text = $"{Math.Round(_alpha * 100)}%";

        SvArea.SetAutomationValue(
            _saturation,
            $"saturation {Percent(_saturation)} percent, brightness {Percent(_value)} percent");
        HueArea.SetAutomationValue(_hue, PickerArea.Spoken(_hue, "degrees"));
        AlphaArea.SetAutomationValue(_alpha * 100, PickerArea.Spoken(_alpha * 100, "percent"));
        if (!HexBox.IsKeyboardFocused)
        {
            HexBox.Text = ColorMath.ToRgbHex(argb);
        }
    }

    private static string Percent(double fraction) =>
        Math.Round(fraction * 100).ToString(CultureInfo.CurrentCulture);

    private static void Place(FrameworkElement thumb, double centerX, double? centerY)
    {
        Canvas.SetLeft(thumb, centerX - (thumb.Width / 2));
        if (centerY is { } y)
        {
            Canvas.SetTop(thumb, y - (thumb.Height / 2));
        }
    }

    // ---- Dragging on the square and the two bars ----

    private void Area_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var area = (FrameworkElement)sender;
        area.Focus(); // so the arrow keys continue from where the mouse left off
        area.CaptureMouse();
        PickAt(area, e.GetPosition(area));
        e.Handled = true;
    }

    private void Area_MouseMove(object sender, MouseEventArgs e)
    {
        var area = (FrameworkElement)sender;
        if (area.IsMouseCaptured)
        {
            PickAt(area, e.GetPosition(area));
        }
    }

    private void Area_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
        ((FrameworkElement)sender).ReleaseMouseCapture();

    private void Area_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateVisuals();

    private void PickAt(FrameworkElement area, Point point)
    {
        var x = area.ActualWidth > 0 ? Math.Clamp(point.X / area.ActualWidth, 0, 1) : 0;
        var y = area.ActualHeight > 0 ? Math.Clamp(point.Y / area.ActualHeight, 0, 1) : 0;

        if (area == SvArea)
        {
            _saturation = x;
            _value = 1 - y;
        }
        else if (area == HueArea)
        {
            _hue = x * 360;
        }
        else
        {
            _alpha = x;
        }

        UpdateVisuals();
        OnColorChanged();
    }

    // ---- Keyboard: arrow keys on the square and the two bars ----

    private void Area_KeyDown(object sender, KeyEventArgs e)
    {
        var area = (PickerArea)sender;
        var bigStep = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;

        var changed = area == SvArea ? NudgeSquare(e.Key, bigStep) : NudgeBar(area, e.Key, bigStep);
        if (!changed)
        {
            return;
        }

        e.Handled = true; // the arrow keys must not also move focus to a neighbouring control
        UpdateVisuals();
        OnColorChanged();
    }

    /// <summary>Left/Right change saturation, Up/Down and Page Up/Down brightness; Home/End jump saturation to either end.</summary>
    private bool NudgeSquare(Key key, bool bigStep)
    {
        var step = bigStep ? 0.10 : 0.01;
        switch (key)
        {
            case Key.Left:
                _saturation = Math.Clamp(_saturation - step, 0, 1);
                return true;
            case Key.Right:
                _saturation = Math.Clamp(_saturation + step, 0, 1);
                return true;
            case Key.Up:
                _value = Math.Clamp(_value + step, 0, 1);
                return true;
            case Key.Down:
                _value = Math.Clamp(_value - step, 0, 1);
                return true;
            case Key.PageUp:
                _value = Math.Clamp(_value + 0.10, 0, 1);
                return true;
            case Key.PageDown:
                _value = Math.Clamp(_value - 0.10, 0, 1);
                return true;
            case Key.Home:
                _saturation = 0;
                return true;
            case Key.End:
                _saturation = 1;
                return true;
            default:
                return false;
        }
    }

    /// <summary>The hue and opacity bars: arrows step, Shift or Page Up/Down take a big step, Home/End jump to either end.</summary>
    private bool NudgeBar(PickerArea area, Key key, bool bigStep)
    {
        var current = area == HueArea ? _hue : _alpha * 100;
        var step = bigStep ? area.LargeChange : area.SmallChange;

        double? next = key switch
        {
            Key.Left or Key.Down => current - step,
            Key.Right or Key.Up => current + step,
            Key.PageDown => current - area.LargeChange,
            Key.PageUp => current + area.LargeChange,
            Key.Home => area.Minimum,
            Key.End => area.Maximum,
            _ => null,
        };

        if (next is not { } value)
        {
            return false;
        }

        SetBar(area, value);
        return true;
    }

    private void SetBar(PickerArea area, double value)
    {
        var clamped = Math.Clamp(value, area.Minimum, area.Maximum);
        if (area == HueArea)
        {
            _hue = clamped;
        }
        else
        {
            _alpha = clamped / 100;
        }
    }

    /// <summary>A screen reader set a slider's value.</summary>
    private bool SetFromBar(PickerArea area, double value)
    {
        SetBar(area, value);
        UpdateVisuals();
        OnColorChanged();
        return true;
    }

    // ---- Hex entry ----

    private void HexBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyHex();
            e.Handled = true; // Enter applies the hex; it doesn't also close the dialog
        }
    }

    private void HexBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => ApplyHex();

    private void ApplyHex()
    {
        if (ColorMath.TryParseHex(HexBox.Text, out var argb, out var hasAlpha))
        {
            SetColor(argb, keepAlpha: !hasAlpha, raiseChanged: true);
        }

        HexBox.Text = ColorMath.ToRgbHex(CurrentArgb); // normalise, or undo an unreadable entry
    }

    // ---- Buttons ----

    private void Default_Click(object sender, RoutedEventArgs e)
    {
        SelectedColor = null;
        DialogResult = true;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (HexBox.IsKeyboardFocused)
        {
            ApplyHex();
        }

        SelectedColor = CurrentArgb;
        DialogResult = true;
    }
}
