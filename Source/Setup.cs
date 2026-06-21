using HarmonyLib;
using Verse;

namespace RightClickGizmoIndicator;

/// <summary>
/// Harmony patch entry point. Fires after all Defs are loaded.
/// Kept separate from the <see cref="RightClickGizmoIndicator_Mod"/> settings class (Hard Rule #7).
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
