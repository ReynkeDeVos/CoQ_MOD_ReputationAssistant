// Reputation Assistant - Harmony Patch
// Author: Kawa | License: MIT
//
// Postfix-patches Description.GetLongDescription to append a reputation
// tracker showing priority, current/target rep, and WR/Kill projections.
// Optionally displays the looked-at creature's own faction(s) in the look popup.
//
// Reputation mechanics: https://wiki.cavesofqud.com/wiki/Reputation
// Color codes: https://wiki.cavesofqud.com/wiki/Modding:Colors_%26_Object_Rendering

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using XRL.Core;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;

namespace Kawa.ReputationAssistant
{
    [HarmonyPatch(typeof(Description), nameof(Description.GetLongDescription),
        new Type[] { typeof(StringBuilder) })]
    static partial class ReputationAssistantPatch
    {
        // Sort: highest priority first, then alphabetical
        static readonly Comparison<FactionEntry> EntryComparer = (a, b) =>
        {
            int cmp = b.Importance.CompareTo(a.Importance);
            return cmp != 0 ? cmp : string.Compare(
                a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
        };

        static DateTime NextErrorLogAtUtc = DateTime.MinValue;
        static int SuppressedErrorCount;

        // ── Harmony entry point ─────────────────────────────────────────

        public static void Postfix(Description __instance, StringBuilder SB)
        {
            try
            {
                if (!OptionEnabled("OptionRAEnabled")) return;

                var go = __instance.ParentObject;
                if (go == null) return;

                // Resolve faction name for any creature
                string factionHeader = OptionEnabled("OptionRAShowFaction")
                    ? GetCreatureFactionHeader(go)
                    : null;

                // Reputation tracker — only for creatures that give rep
                var givesRep = go.GetPart<GivesRep>();
                var playerRep = XRLCore.Core?.Game?.PlayerReputation;
                List<FactionEntry> entries = null;

                if (givesRep != null && playerRep != null)
                {
                    entries = ParseEntries(givesRep, playerRep);
                    if (entries.Count > 0)
                    {
                        entries.Sort(EntryComparer);
                    }
                    else
                    {
                        entries = null;
                    }
                }

                // Nothing to show at all
                if (factionHeader == null && entries == null) return;

                bool wrDone = go.GetIntProperty("WaterRitualed") > 0;
                ReputationRenderer.RenderSectionHeader(SB, wrDone, factionHeader);

                if (entries != null)
                {
                    bool showOutcomes = OptionEnabled("OptionRAShowOutcomes");
                    bool compact = OptionEnabled("OptionRACompactLayout");
                    ReputationRenderer.RenderTracker(SB, entries, showOutcomes, compact);
                }
            }
            catch (Exception ex)
            {
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

        // ── Parsing ─────────────────────────────────────────────────────

        static string GetCreatureFactionHeader(GameObject go)
        {
            if (go == null)
                return null;

            var factionNames = new List<string>();
            var seenInternal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddCreatureFaction(factionNames, seenInternal, go.GetPrimaryFaction());

            var brain = go.GetPart<Brain>();
            if (brain != null)
            {
                AddCreatureFactionsFromValue(
                    factionNames,
                    seenInternal,
                    GetMemberValue(brain, "Factions", "FactionMembership", "FactionMemberships"));

                AddCreatureFactionsFromValue(
                    factionNames,
                    seenInternal,
                    GetMemberValue(brain, "PrimaryFaction", "Faction", "FactionName"));
            }

            return FactionHeaderUtilities.ComposeHeader(factionNames);
        }

        static List<FactionEntry> ParseEntries(GivesRep givesRep, Reputation playerRep)
        {
            try
            {
                if (TryParseEntriesFromRelatedFactions(givesRep, playerRep, out var entries) &&
                    entries.Count > 0)
                {
                    return entries;
                }
            }
            catch
            {
                // If runtime related-faction structures differ from expectations,
                // fall back to parsing the generated description text.
            }

            return ParseEntriesFromDescription(givesRep, playerRep);
        }

        internal static bool OptionEnabled(string id) =>
            string.Equals(Options.GetOption(id, "Yes"), "Yes", StringComparison.OrdinalIgnoreCase);
    }
}
