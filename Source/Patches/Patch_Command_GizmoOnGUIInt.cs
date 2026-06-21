using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace RightClickGizmoIndicator;

/// <summary>
/// Draws a small folded-corner marker on the bottom-right of any gizmo that has a right-click
/// float menu. Patches <c>Command.GizmoOnGUIInt</c> — the single choke point both the normal
/// (<c>GizmoOnGUI</c>) and shrunk (<c>GizmoOnGUIShrunk</c>) render paths funnel through, and which
/// every right-click-bearing Command subclass (Designator, Command_Toggle, Command_Ability, …)
/// reaches via <c>base</c>. Because the patch lives on the shared base method, gizmos added by other
/// mods are covered automatically with no load-order coupling.
///
/// Detection is static: whether a gizmo opens a right-click menu is constant over its lifetime
/// (it's decided by constructor-set conditions), so we evaluate it at most once per instance and
/// cache the result. There is no per-frame enumeration and no map scanning after warm-up.
/// </summary>
[HarmonyPatch(typeof(Command), "GizmoOnGUIInt")]
internal static class Patch_Command_GizmoOnGUIInt
{
    // Tier 1 — per type: does this type override RightClickFloatMenuOptions below Verse.Gizmo at all?
    // A type that doesn't override it returns Enumerable.Empty and can never have a menu.
    private static readonly Dictionary<Type, bool> TypeMayHaveMenu = new Dictionary<Type, bool>();

    // Tier 2 — per instance: the evaluated result, computed once and cached for the gizmo's life.
    // ConditionalWeakTable keys weakly, so entries for transient gizmos are collected with them.
    private static readonly ConditionalWeakTable<Gizmo, StrongBox<bool>> InstanceHasMenu =
        new ConditionalWeakTable<Gizmo, StrongBox<bool>>();

    private static Texture2D foldTex;

    public static void Postfix(Command __instance, Rect butRect)
    {
        // Draw-only work; skip Layout / input passes (DrawGizmoGrid also early-outs on Layout).
        if (Event.current.type != EventType.Repaint) return;

        try
        {
            RightClickGizmoIndicator_Settings settings = RightClickGizmoIndicator_Mod.Settings;
            if (settings == null || !settings.enabled) return;

            // A disabled gizmo's click is swallowed (returns Mouseover) and never opens a float menu.
            if (__instance.Disabled) return;

            if (!TypeMayHaveMenuCached(__instance.GetType())) return;   // free skip for the majority
            if (!HasMenuOnce(__instance)) return;

            DrawCornerFold(butRect, settings);
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

    private static bool HasMenuOnce(Gizmo g)
    {
        // Menu existence doesn't change over an instance's life, so evaluate exactly once.
        return InstanceHasMenu.GetValue(g, EvaluateHasMenu).Value;
    }

    private static StrongBox<bool> EvaluateHasMenu(Gizmo g)
    {
        try
        {
            IEnumerable<FloatMenuOption> opts = g.RightClickFloatMenuOptions;   // mirrors the game's own check
            return new StrongBox<bool>(opts != null && opts.FirstOrDefault() != null);
        }
        catch
        {
            return new StrongBox<bool>(false);   // defensive: a buggy modded override must not break drawing
        }
    }

    private static void DrawCornerFold(Rect butRect, RightClickGizmoIndicator_Settings settings)
    {
        float size = butRect.width * settings.foldScale;
        var r = new Rect(butRect.xMax - size, butRect.yMax - size, size, size);

        Color prev = GUI.color;
        GUI.color = settings.FoldTint;
        GUI.DrawTexture(r, FoldTex);
        GUI.color = prev;
    }

    private static Texture2D FoldTex => foldTex ?? (foldTex = BuildFoldTexture(32));

    /// <summary>
    /// Builds a dog-ear: a white triangle filling the bottom-right corner of the marker square,
    /// with a brighter crease along its hypotenuse so it reads as a lifted page corner. White so it
    /// can be tinted (gray + opacity) at draw time. GUI.DrawTexture shows texture row 0 at the bottom
    /// of the rect, so low-y / high-x pixels land in the displayed bottom-right corner.
    /// </summary>
    private static Texture2D BuildFoldTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
        {
            name = "RCGI_CornerFold",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var px = new Color[size * size];
        float edgeThickness = Mathf.Max(2f, size * 0.10f);   // crease highlight width, in texture px
        const float bodyAlpha = 0.55f;

        for (int y = 0; y < size; y++)        // y = 0 is the bottom row when drawn
        {
            for (int x = 0; x < size; x++)    // x = 0 is the left column
            {
                float a;
                if (y > x)
                {
                    a = 0f;                   // above the y = x crease: outside the fold, transparent
                }
                else
                {
                    float distToCrease = (x - y) * 0.70710678f;   // perpendicular distance to line y = x
                    a = distToCrease <= edgeThickness
                        ? Mathf.Lerp(1f, bodyAlpha, distToCrease / edgeThickness)
                        : bodyAlpha;
                }
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }
}
