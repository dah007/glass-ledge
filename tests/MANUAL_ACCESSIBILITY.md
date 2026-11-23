# Manual accessibility checks

The automated tests cover the pieces that can be checked in code (spoken names, palettes, the picker's
slider and value patterns, tiles exposed as buttons). These need a person at the keyboard, ideally with
Narrator (Win+Ctrl+Enter) running. Run them before each release that touches the widget or the picker.

## Reaching a widget without the mouse
- [ ] Win+B, arrow to the GlassLedge icon: Narrator says its name. Enter hides or shows the widgets; the Menu key opens the menu.
- [ ] Tray menu > **Go to widget** > a widget: the widget comes forward and the first item has a thick amber ring. (If the focus snaps back to the previous window, tell me: the tray menu closing may be taking the foreground.)
- [ ] With **Hide all** on, **Go to widget** is greyed out.

## Inside a widget
- [ ] Arrow keys move between items, including up and down across rows; Tab leaves the group.
- [ ] Narrator reads "name, folder" for folders, the file name for files, and "N more items, opens the folder" for the +N tile.
- [ ] Enter or Space opens the item; double-clicking with the mouse still opens it, and dragging still moves the widget.
- [ ] Menu key or Shift+F10 opens the right-click menu; every entry is reachable with the arrow keys and Enter.
- [ ] F2 renames (typing works, Enter saves, Esc cancels and keeps keyboard focus in the widget); F5 refreshes.
- [ ] Esc returns focus to the window you came from, and the widget goes back behind other windows.
- [ ] Alt+Tab away: the widget stops taking focus and does not pull it back.
- [ ] An empty folder and a collapsed widget: focus goes to the card (a ring around the whole widget) and the menu still opens.
- [ ] Narrator reads the item count as "12 items".

## Color picker
- [ ] Tab order: square, hue, opacity, hex, presets, Default, OK, Cancel. Each of the three areas shows a focus ring.
- [ ] Narrator reads "Saturation and brightness" with its value, "Hue, slider, 210", "Opacity, slider, 72".
- [ ] Arrow keys change the color live in the widget; Shift and Page Up/Down take bigger steps; Home/End jump to the ends.
- [ ] Preset swatches are announced by their color names.

## Display settings
- [ ] Settings > Accessibility > Contrast themes: **Night sky** (dark) and **Desert** (light) switch the widget and the dialogs to system colors with no glass, no gloss and a visible focus ring, live, without restarting.
- [ ] Settings > Personalization > Colors > Transparency effects off: widgets turn solid and the text stays readable, including a custom color.
- [ ] Text size (Settings > Accessibility > Text size) is not a feature of the widget titles yet; note what you see.
- [ ] Windows scaling at 150% and 200%: nothing is clipped.

## Tools
- [ ] Accessibility Insights for Windows, **FastPass** on the widget, the About window and the color picker: no failures for names, roles and keyboard focus.
