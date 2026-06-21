# Right-Click Gizmo Indicator

Adds RimWorld's own extra-options marker — the small top-right icon the game already puts on build
buttons with material choices — to every command button ("gizmo") that has a right-click menu, so hidden
right-click options are discoverable at a glance.

- Marks **exactly** the buttons whose right-click opens a menu — using the same check the game itself uses.
- Reuses the **native** marker (`Designator_Dropdown.PlusTex`), so it matches vanilla exactly and follows
  any UI retexture mod automatically.
- Works for **vanilla and other mods'** gizmos automatically. No settings, no configuration, no load-order
  setup — install it and it just works.
- **No performance cost:** the check is computed once per button and cached; there is no per-frame work.
- Purely cosmetic — no gameplay change, no save data.

**Requires:** [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077). RimWorld 1.6.

### Notes
A handful of special non-standard gizmos (in vanilla, only the Mechanitor control-group gizmo) draw
themselves entirely custom and won't show the marker. Everything built on the normal command button —
which is essentially all of them, vanilla and modded — is covered.
