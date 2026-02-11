// Reputation Assistant - Harmony Patch
// Author: Kawa | License: MIT
//
// Postfix-patches Description.GetLongDescription to append a reputation
// tracker showing priority, current/target rep, and WR/Kill projections.
// Also displays the creature's primary faction in the look popup.
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
    static class ReputationAssistantPatch
    {
        // Reputation deltas per relationship type (wiki source)
        static readonly Dictionary<string, (int wr, int kill)> RelationshipDeltas =
            new(StringComparer.OrdinalIgnoreCase)
        {
            { "Loved",    (+100, -100) },
            { "Admired",  ( +50,  -50) },
            { "Liked",    ( +50,  -50) },
            { "Disliked", ( -50,  +50) },
            { "Hated",    (-100, +100) },
        };

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
                string factionHeader = null;
                if (OptionEnabled("OptionRAShowFaction"))
                {
                    string fName = go.GetPrimaryFaction();
                    if (!string.IsNullOrEmpty(fName))
                    {
                        var fObj = Factions.GetIfExists(fName);
                        if (fObj != null && fObj.Visible)
                            factionHeader = !string.IsNullOrEmpty(fObj.DisplayName)
                                ? fObj.DisplayName : fName;
                    }
                }

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

        static List<FactionEntry> ParseEntries(GivesRep givesRep, Reputation playerRep)
        {
            var sb = new StringBuilder();
            givesRep.AppendReputationDescription(sb);
            string raw = sb.ToString();

            var entries = new List<FactionEntry>();
            if (string.IsNullOrEmpty(raw)) return entries;

            string clean = FactionResolver.StripMarkup(raw);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var resolveCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string ResolveCached(string value)
            {
                if (string.IsNullOrEmpty(value))
                    return null;

                if (resolveCache.TryGetValue(value, out string cached))
                    return cached;

                string resolved = FactionResolver.Resolve(value);
                resolveCache[value] = resolved;
                return resolved;
            }

            foreach (var line in ReputationTextParser.ParseRelationshipLines(clean))
            {
                string relationship = line.Relationship;
                string rawName = line.RawFactionNames;

                // Try full name first (handles factions with "and" in their name)
                string singleResolve = ResolveCached(rawName);
                if (singleResolve != null)
                {
                    if (seen.Add(singleResolve))
                        entries.Add(BuildEntry(singleResolve, rawName, relationship, playerRep));
                }
                else
                {
                    // Game combines factions: "Admired by goatfolk and pariahs"
                    foreach (string name in ReputationTextParser.SplitFactionNames(rawName, ResolveCached))
                    {
                        string internalName = ResolveCached(name);
                        if (internalName == null || !seen.Add(internalName)) continue;

                        entries.Add(BuildEntry(internalName, name, relationship, playerRep));
                    }
                }
            }

            return entries;
        }

        static FactionEntry BuildEntry(
            string internalName, string rawName, string relationship, Reputation playerRep)
        {
            // Defaults from strategy table
            int target = FactionStrategy.DefaultTarget;
            int importance = FactionStrategy.DefaultImportance;
            bool isSpecial = false;

            if (FactionStrategy.Table.TryGetValue(internalName, out var strat))
            {
                target = strat.Target;
                importance = strat.Importance;
                isSpecial = strat.IsSpecial;
            }

            // In-game option overrides
            FactionOptionOverrides.Apply(internalName, ref importance, ref target);

            // Reputation change from relationship
            int wrChange = 0, killChange = 0;
            if (RelationshipDeltas.TryGetValue(relationship, out var delta))
            {
                wrChange = delta.wr;
                killChange = delta.kill;
            }

            // Current player rep
            var faction = Factions.GetIfExists(internalName);
            int currentRep = 0;
            string displayName = rawName;
            if (faction != null)
            {
                currentRep = playerRep.Get(faction);
                if (!string.IsNullOrEmpty(faction.DisplayName))
                    displayName = faction.DisplayName;
            }

            return new FactionEntry(
                displayName, internalName,
                currentRep, target, importance, isSpecial,
                wrChange, killChange);
        }

        internal static bool OptionEnabled(string id) =>
            string.Equals(Options.GetOption(id, "Yes"), "Yes", StringComparison.OrdinalIgnoreCase);
    }
}
