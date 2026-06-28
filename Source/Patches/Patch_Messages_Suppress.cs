using System;
using HarmonyLib;
using Verse;

namespace RightClickGizmoIndicator;

/// <summary>
/// Narrow non-gizmo safety guard for the one-time <c>RightClickFloatMenuOptions</c> probe in
/// <see cref="Patch_Command_GizmoOnGUIInt"/>. Some modded gizmo getters call <c>Verse.Messages.Message</c>
/// as a side effect (e.g. Quick Stockpile Creation warns when the selected item is already at max
/// priority). Probing such a getter — even once — would surface a spurious player-facing message and
/// sound. While a probe is on this thread's stack (<c>suppressDepth &gt; 0</c>), this prefix skips the
/// single funnel every <c>Messages.Message</c> overload routes through (<c>Message(Message, bool)</c> —
/// the three string overloads each build a <c>Message</c> and delegate here), suppressing the feed entry,
/// the archive entry, and the sound.
///
/// Thread-static + <see cref="Push"/>/<see cref="Pop"/> in a try/finally means we can only ever mute our
/// own probe, never an unrelated message, and never persist suppression past the probe. A depth counter
/// (not a bool) keeps a nested probe's <c>Pop</c> from clearing an outer scope's suppression.
///
/// This is the mod's only prefix and its only non-gizmo patch — a deliberate exception to the
/// "gizmo hook is postfix-only" rule, existing precisely to keep that hook's effect cosmetic. It covers
/// only <c>Messages.Message</c>; it does not suppress <c>Log</c>, direct <c>SoundDef.PlayOneShot</c>,
/// <c>WindowStack.Add</c>, or state mutation a getter might perform.
/// </summary>
[HarmonyPatch(typeof(Messages), nameof(Messages.Message), new[] { typeof(Message), typeof(bool) })]
internal static class Patch_Messages_Suppress
{
    [ThreadStatic] private static int suppressDepth;

    internal static void Push() => suppressDepth++;
    internal static void Pop() => suppressDepth--;

    // Return false (skip the original) only while a detection probe is active on this thread.
    private static bool Prefix() => suppressDepth <= 0;
}
