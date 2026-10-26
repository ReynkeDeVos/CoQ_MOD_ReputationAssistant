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

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using XRL;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;

namespace Kawa.ReputationAssistant
{
    [HarmonyPatch(typeof(Description), nameof(Description.GetLongDescription),
        new Type[] { typeof(StringBuilder) })]
    static class ReputationAssistantPatch
    {
        static DateTime NextErrorLogAtUtc = DateTime.MinValue;
        static int SuppressedErrorCount;

        public static void Postfix(Description __instance, StringBuilder SB)
        {
            try
            {
                if (!OptionEnabled("OptionRAEnabled")) return;

                var creature = __instance.ParentObject;
                if (creature?.Brain == null) return;

                List<string> ownFactions = OwnFactionNames(creature);

                var givesRep = creature.GetPart<GivesRep>();
                var playerRep = The.Game?.PlayerReputation;
                bool wrDone = creature.HasIntProperty("WaterRitualed");
                List<FactionEntry> entries = givesRep != null && playerRep != null
                    ? FactionEntries.Collect(creature, givesRep, playerRep, wrDone)
                    : null;
                bool hasEntries = entries != null && entries.Count > 0;

                if (ownFactions == null && !hasEntries) return;

                bool bonded = givesRep?.wasParleyed == true;
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
                if (now < NextErrorLogAtUtc)
                {
                    SuppressedErrorCount++;
                    return;
                }

                string suppressed = SuppressedErrorCount > 0
                    ? $" (suppressed {SuppressedErrorCount} similar errors)"
                    : string.Empty;
                SuppressedErrorCount = 0;
                NextErrorLogAtUtc = now.AddSeconds(30);

                UnityEngine.Debug.Log(
                    $"[ReputationAssistant]{suppressed} {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>The creature's visible factions: primary first, the rest alphabetical. Null if none.</summary>
        static List<string> OwnFactionNames(GameObject creature)
        {
            string primary = null;
            var others = new List<string>();
            string primaryKey = creature.GetPrimaryFaction();

            foreach (var membership in creature.Brain.Allegiance)
            {
                if (membership.Value <= 0) continue;

                // Some blueprints list a faction by its display name instead of its id.
                var faction = Factions.GetIfExists(membership.Key) ?? Factions.GetByDisplayName(membership.Key);
                if (faction == null || !faction.Visible) continue;

                if (membership.Key == primaryKey)
                    primary = faction.DisplayName;
                else if (!others.Contains(faction.DisplayName))
                    others.Add(faction.DisplayName);
            }

            others.Sort(StringComparer.OrdinalIgnoreCase);
            if (primary != null)
            {
                others.Remove(primary);
                others.Insert(0, primary);
            }

            return others.Count > 0 ? others : null;
        }

        static bool OptionEnabled(string id) =>
            string.Equals(Options.GetOption(id, "Yes"), "Yes", StringComparison.OrdinalIgnoreCase);
    }
}
