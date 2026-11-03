// Reputation Assistant - Harmony Patch
// Author: Kawa | License: MIT
//
// Postfix-patches Description.GetLongDescription (the look popup) to append
// a reputation tracker for creatures with a GivesRep part: a one-line kill
// verdict, then priority, current/target rep, and Water Ritual / kill projections.
// The section title names the looked-at creature's own factions.
//
// Reputation mechanics: https://wiki.cavesofqud.com/wiki/Reputation
// Color codes: https://wiki.cavesofqud.com/wiki/Modding:Colors_%26_Object_Rendering

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using HarmonyLib;
using XRL;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;

namespace Kawa.ReputationAssistant
{
    [HarmonyPatch(typeof(Description), nameof(Description.GetLongDescription),
        new Type[] { typeof(StringBuilder) })]
    [SuppressMessage("Design", "MA0182", Justification = "Harmony finds the patch through its attribute.")]
    internal static class ReputationAssistantPatch
    {
        private static DateTime s_nextErrorLogAtUtc = DateTime.MinValue;
        private static int s_suppressedErrorCount;

        public static void Postfix(Description __instance, StringBuilder SB)
        {
            try
            {
                if (!OptionEnabled("OptionRAEnabled"))
                    return;

                var creature = __instance.ParentObject;
                if (creature?.Brain is null)
                    return;

                List<string> ownFactions = OwnFactionNames(creature);

                var givesRep = creature.GetPart<GivesRep>();
                var playerRep = The.Game?.PlayerReputation;
                bool wrDone = creature.HasIntProperty("WaterRitualed");
                List<FactionEntry> entries = givesRep is not null && playerRep is not null
                    ? FactionEntries.Collect(creature, givesRep, playerRep, wrDone)
                    : null;
                bool hasEntries = entries is not null && entries.Count > 0;

                if (ownFactions is null && !hasEntries)
                    return;

                bool bonded = givesRep?.wasParleyed is true;
                ReputationRenderer.RenderSectionHeader(SB, ownFactions, bonded);

                if (hasEntries)
                    ReputationRenderer.RenderTracker(SB, entries, bonded,
                        showOutcomes: OptionEnabled("OptionRAShowOutcomes"),
                        showWR: !wrDone,
                        compact: OptionEnabled("OptionRACompactLayout"),
                        showVerdict: OptionEnabled("OptionRAShowKillVerdict"));
            }
            catch (Exception ex)
            {
                // Log at most once every 30 seconds so a broken look popup can't flood the log.
                var now = DateTime.UtcNow;
                if (now < s_nextErrorLogAtUtc)
                {
                    s_suppressedErrorCount++;
                    return;
                }

                string suppressed = s_suppressedErrorCount > 0
                    ? FormattableString.Invariant($" (suppressed {s_suppressedErrorCount} similar errors)")
                    : string.Empty;
                s_suppressedErrorCount = 0;
                s_nextErrorLogAtUtc = now.AddSeconds(30);

                UnityEngine.Debug.Log(
                    $"[ReputationAssistant]{suppressed} {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>The creature's visible factions: primary first, the rest alphabetical. Null if none.</summary>
        private static List<string> OwnFactionNames(GameObject creature)
        {
            string primary = null;
            var others = new List<string>();
            string primaryKey = creature.GetPrimaryFaction();

            foreach (var membership in creature.Brain.Allegiance)
            {
                if (membership.Value <= 0)
                    continue;

                // Some blueprints list a faction by its display name instead of its id.
                var faction = Factions.GetIfExists(membership.Key) ?? Factions.GetByDisplayName(membership.Key);
                if (faction is null || !faction.Visible)
                    continue;

                if (string.Equals(membership.Key, primaryKey, StringComparison.Ordinal))
                    primary = faction.DisplayName;
                else if (!others.Contains(faction.DisplayName))
                    others.Add(faction.DisplayName);
            }

            others.Sort(StringComparer.OrdinalIgnoreCase);
            if (primary is not null)
            {
                others.Remove(primary);
                others.Insert(0, primary);
            }

            return others.Count > 0 ? others : null;
        }

        private static bool OptionEnabled(string id) =>
            string.Equals(Options.GetOption(id, "Yes"), "Yes", StringComparison.OrdinalIgnoreCase);
    }
}
