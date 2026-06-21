using HarmonyLib;
using Verse;

namespace RightClickGizmoIndicator;

/// <summary>
/// Harmony patch entry point. Fires after all Defs are loaded. A dedicated
/// <c>[StaticConstructorOnStartup]</c> init class, kept separate from any other mod type (Hard Rule #7).
/// </summary>
[StaticConstructorOnStartup]
public static class RightClickGizmoIndicator_Init
{
    static RightClickGizmoIndicator_Init()
    {
        new Harmony("com.cheatereater.rightclickgizmoindicator").PatchAll();

        if (Prefs.DevMode)
        {
            Log.Message("[Right-Click Gizmo Indicator] Harmony patches applied.");
        }
    }
}
