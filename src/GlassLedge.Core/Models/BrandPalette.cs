namespace GlassLedge.Core.Models;

/// <summary>
/// The GlassLedge brand colours for dialogs and menus, as 0xAARRGGBB values. Widgets keep the Windows
/// light/dark glass (<see cref="WidgetPalette"/>); this palette covers everything around them: About,
/// the colour picker, the right-click and tray menus.
/// <para>
/// Light mode is frost surfaces with graphite text; dark mode is dusk surfaces with white text. Amber is
/// the accent in both: a primary button fill, and the keyboard focus ring (deep amber on light surfaces,
/// where plain amber would be too faint).
/// </para>
/// </summary>
public sealed record BrandPalette(
    uint Page,
    uint Text,
    uint SecondaryText,
    uint Row,
    uint Hairline,
    uint Footer,
    uint Focus,
    uint Menu,
    uint OnAmberFocus)
{
    public const uint Amber = 0xFFFFB020;
    public const uint AmberHover = 0xFFFFC24D;
    public const uint AmberPressed = 0xFFE89A00;
    public const uint AmberDeep = 0xFF8A5300;
    public const uint Graphite = 0xFF1C1C1E;
    public const uint Dusk = 0xFF1B1F36;
    public const uint Frost = 0xFFF7F8FA;
    public const uint Haze = 0xFF9DBDE0;

    public static readonly BrandPalette Light = new(
        Page: Frost,
        Text: Graphite,
        SecondaryText: 0xFF52555C,
        Row: 0xB3FFFFFF,
        Hairline: 0x241B1F36,
        Footer: 0x0A1B1F36,
        Focus: AmberDeep,
        Menu: 0xFFFFFFFF,
        OnAmberFocus: Dusk);

    public static readonly BrandPalette Dark = new(
        Page: Dusk,
        Text: 0xFFFFFFFF,
        SecondaryText: 0xFFB4BACB,
        Row: 0x1FFFFFFF,
        Hairline: 0x339DBDE0,
        Footer: 0x33000000,
        Focus: Amber,
        Menu: 0xFF23283F,
        OnAmberFocus: Dusk);

    /// <summary>The palette for the Windows light or dark app setting.</summary>
    public static BrandPalette For(bool light) => light ? Light : Dark;
}
