using UnityEngine;
using Verse;

namespace RightClickGizmoIndicator;

/// <summary>
/// Mod settings — persisted in the config folder. All cosmetic: whether to show the marker,
/// how large it is, and how opaque.
/// </summary>
public sealed class RightClickGizmoIndicator_Settings : ModSettings
{
    public const float MinScale = 0.12f;
    public const float MaxScale = 0.35f;

    private const bool DefaultEnabled = true;
    private const float DefaultScale = 0.22f;     // fraction of the gizmo's width
    private const float DefaultOpacity = 0.8f;

    public bool enabled = DefaultEnabled;
    public float foldScale = DefaultScale;
    public float foldOpacity = DefaultOpacity;

    /// <summary>Light-gray tint applied to the (white) fold texture; opacity is player-adjustable.</summary>
    public Color FoldTint => new Color(0.88f, 0.88f, 0.88f, foldOpacity);

    public void Reset()
    {
        enabled = DefaultEnabled;
        foldScale = DefaultScale;
        foldOpacity = DefaultOpacity;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref enabled, "enabled", DefaultEnabled);
        Scribe_Values.Look(ref foldScale, "foldScale", DefaultScale);
        Scribe_Values.Look(ref foldOpacity, "foldOpacity", DefaultOpacity);
    }
}

/// <summary>
/// Mod entry point for settings UI. Loads very early — do NOT reference Defs here.
/// </summary>
public sealed class RightClickGizmoIndicator_Mod : Mod
{
    public static RightClickGizmoIndicator_Settings Settings { get; private set; }

    public RightClickGizmoIndicator_Mod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<RightClickGizmoIndicator_Settings>();
    }

    public override string SettingsCategory() => "Right-Click Gizmo Indicator";

    public override void DoSettingsWindowContents(Rect inRect)
    {
        RightClickGizmoIndicator_Settings s = Settings;
        var listing = new Listing_Standard();
        listing.Begin(inRect);

        listing.CheckboxLabeled(
            "Show right-click marker",
            ref s.enabled,
            "Draw a small folded-corner marker on gizmos that have a right-click menu.");

        if (s.enabled)
        {
            listing.Gap(6f);
            listing.Label($"Marker size: {Mathf.RoundToInt(s.foldScale * 100f)}%");
            s.foldScale = Mathf.Clamp(
                listing.Slider(s.foldScale, RightClickGizmoIndicator_Settings.MinScale, RightClickGizmoIndicator_Settings.MaxScale),
                RightClickGizmoIndicator_Settings.MinScale, RightClickGizmoIndicator_Settings.MaxScale);

            listing.Gap(6f);
            listing.Label($"Marker opacity: {Mathf.RoundToInt(s.foldOpacity * 100f)}%");
            s.foldOpacity = Mathf.Clamp(listing.Slider(s.foldOpacity, 0.25f, 1f), 0.25f, 1f);
        }

        listing.Gap(12f);
        if (listing.ButtonText("Reset to defaults"))
        {
            s.Reset();
        }

        listing.End();
    }
}
