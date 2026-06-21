# Right-Click Gizmo Indicator

Adds a small folded-corner marker to command buttons ("gizmos") that have a right-click menu, so hidden
right-click options are discoverable at a glance.

- Marks **exactly** the buttons whose right-click opens a menu — using the same check the game itself uses.
- Works for **vanilla and other mods'** gizmos automatically. No configuration or load-order setup.
- **No performance cost:** the check is computed once per button and cached; there is no per-frame work.
- Purely cosmetic — no gameplay change, no save data.

**Settings:** toggle the marker on/off, adjust its size and opacity.

**Requires:** [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077). RimWorld 1.6.

### Notes
A handful of special non-standard gizmos (in vanilla, only the Mechanitor control-group gizmo) draw
themselves entirely custom and won't show the marker. Everything built on the normal command button —
which is essentially all of them, vanilla and modded — is covered.
