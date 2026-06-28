using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RightClickGizmoIndicator;

/// <summary>
/// Draws RimWorld's native top-right extra-options marker (<c>Designator_Dropdown.PlusTex</c>) on any
/// gizmo that has a right-click float menu — the same marker vanilla puts on build buttons with
/// material choices. Patches <c>Command.GizmoOnGUIInt</c> — the single choke point both the normal
/// (<c>GizmoOnGUI</c>) and shrunk (<c>GizmoOnGUIShrunk</c>) render paths funnel through, and which
/// every right-click-bearing Command subclass (Designator, Command_Toggle, Command_Ability, …)
/// reaches via <c>base</c>. Because the patch lives on the shared base method, gizmos added by other
/// mods are covered automatically with no load-order coupling.
///
/// Detection is static: whether a gizmo opens a right-click menu is treated as constant per type, so we
/// probe each gizmo type's getter at most once and cache the result by type — never per instance.
/// (Selection gizmos are recreated every frame, so per-instance caching re-probed the getter every frame;
/// some modded getters have side effects, so that meant message spam.) There is no per-frame enumeration
/// and no map scanning after warm-up.
///
/// One vanilla family — the allowed-area designators (<c>Designator_AreaAllowed</c>: Expand/Clear) —
/// opens its float menu through <c>ProcessInput</c> rather than exposing <c>RightClickFloatMenuOptions</c>,
/// so it's matched by type. Detection never calls <c>ProcessInput</c> (that would open the menu).
/// </summary>
[HarmonyPatch(typeof(Command), "GizmoOnGUIInt")]
internal static class Patch_Command_GizmoOnGUIInt
{
    // Tier 1 — per type: does this type override RightClickFloatMenuOptions below Verse.Gizmo at all?
    // A type that doesn't override it returns Enumerable.Empty and can never have a menu.
    private static readonly Dictionary<Type, bool> TypeMayHaveMenu = new Dictionary<Type, bool>();

    // Tier 2 — per TYPE: probed at most once per type, reused for all instances of that type.
    // Selection gizmos are recreated every frame (Thing.GetGizmos yields new instances), so a per-instance
    // cache missed every frame and re-probed the getter every frame. Some modded getters have user-visible
    // side effects in their first lines (e.g. Quick Stockpile Creation shows a NegativeEvent when the
    // selected item is already at max priority), so per-frame probing = message spam. Menu presence is
    // constant per type, so one probe per type suffices. See docs/IMPLEMENTATION.md.
    private static readonly Dictionary<Type, bool> TypeHasMenu = new Dictionary<Type, bool>();

    public static void Postfix(Command __instance, Rect butRect)
    {
        // Draw-only work; skip Layout / input passes (DrawGizmoGrid also early-outs on Layout).
        if (Event.current.type != EventType.Repaint) return;

        try
        {
            // A disabled gizmo's click is swallowed (returns Mouseover) and never opens a float menu.
            if (__instance.Disabled) return;

            // Allowed-area designators (Expand/Clear) open their area-picker float menu through
            // ProcessInput, not RightClickFloatMenuOptions: on right-click GizmoGridDrawer finds their
            // RightClickFloatMenuOptions empty and routes the event to ProcessInput instead, so the
            // generic check below misses them. The menu always exists (the list always includes
            // "Manage areas"), so detect by type. Never call ProcessInput to detect — it has side
            // effects (it opens the menu).
            if (!(__instance is Designator_AreaAllowed))
            {
                if (!TypeMayHaveMenuCached(__instance.GetType())) return;   // free skip for the majority
                if (!HasMenuOnceForType(__instance)) return;
            }

            DrawExtraOptionsMarker(butRect);
        }
        catch (Exception e)
        {
            Log.ErrorOnce("[Right-Click Gizmo Indicator] Error drawing marker: " + e, 0x5C1A_0001);
        }
    }

    private static bool TypeMayHaveMenuCached(Type t)
    {
        if (!TypeMayHaveMenu.TryGetValue(t, out bool result))
        {
            var getter = t.GetProperty(nameof(Gizmo.RightClickFloatMenuOptions))?.GetGetMethod(true);
            result = getter != null && getter.DeclaringType != typeof(Gizmo);
            TypeMayHaveMenu[t] = result;
        }
        return result;
    }

    private static bool HasMenuOnceForType(Gizmo g)
    {
        // Menu presence is constant per type, so probe each type's getter exactly once and reuse the
        // result for every instance of that type. Per-instance caching was useless for selection gizmos,
        // which Thing.GetGizmos recreates every frame — see the TypeHasMenu note above. First-probed
        // instance wins for the type (an accepted imperfection; see docs/IMPLEMENTATION.md).
        Type t = g.GetType();
        if (!TypeHasMenu.TryGetValue(t, out bool result))
        {
            result = EvaluateHasMenu(g);
            TypeHasMenu[t] = result;
        }
        return result;
    }

    private static bool EvaluateHasMenu(Gizmo g)
    {
        // Some modded getters call Messages.Message as a side effect (e.g. Quick Stockpile Creation warns
        // when the selected item is already at max priority). Mute player-facing messages for the duration
        // of this one probe so detection stays invisible. See Patch_Messages_Suppress.
        Patch_Messages_Suppress.Push();
        try
        {
            IEnumerable<FloatMenuOption> opts = g.RightClickFloatMenuOptions;   // mirrors the game's own check
            return opts != null && opts.FirstOrDefault() != null;
        }
        catch
        {
            return false;   // defensive: a buggy modded override must not break drawing
        }
        finally
        {
            Patch_Messages_Suppress.Pop();
        }
    }

    /// <summary>
    /// Draws vanilla's own extra-options marker at the gizmo's top-right, identical to how the game
    /// marks build buttons with material choices. <c>butRect</c> is built as
    /// <c>new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f)</c> in <c>Command.GizmoOnGUIInt</c>,
    /// so <c>butRect.position</c>/<c>butRect.width</c> are exactly the <c>topLeft</c>/<c>width</c>
    /// vanilla passes to <c>DrawExtraOptionsIcon</c> — placement matches pixel-for-pixel. We force
    /// white and restore it so the icon renders at full tint regardless of any leftover GUI.color.
    /// </summary>
    private static void DrawExtraOptionsMarker(Rect butRect)
    {
        Color prev = GUI.color;
        GUI.color = Color.white;
        Designator_Dropdown.DrawExtraOptionsIcon(butRect.position, butRect.width);
        GUI.color = prev;
    }
}
