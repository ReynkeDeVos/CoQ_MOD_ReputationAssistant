// Reputation Assistant - Harmony Patch
// Author: Kawa | License: MIT
//
// Postfix-patches Description.GetLongDescription to append a reputation
// tracker showing priority, current/target rep, and WR/Kill projections.
// Also patches GameObject.GetDisplayName to display creature factions
// as a tag after the creature name.
//
// Reputation mechanics: https://wiki.cavesofqud.com/wiki/Reputation
// Color codes: https://wiki.cavesofqud.com/wiki/Modding:Colors_%26_Object_Rendering

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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

        // Parses: "Admired by the Farmers' Guild for telling bawdy jokes."
        // Group 1 = relationship, Group 2 = faction name (strips "for ..." suffix)
        static readonly Regex ReputationLine = new(
            @"(Loved|Admired|Liked|Disliked|Hated) by (.+?)(?:\s+for\s+.+)?\.?\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // ── Harmony entry point ─────────────────────────────────────────

        public static void Postfix(Description __instance, StringBuilder SB)
        {
            try
            {
                if (!OptionEnabled("OptionRAEnabled")) return;

                var go = __instance.ParentObject;
                if (go == null) return;

                var givesRep = go.GetPart<GivesRep>();
                if (givesRep == null) return;

                // Reputation tracker — append at bottom
                var playerRep = XRLCore.Core?.Game?.PlayerReputation;
                if (playerRep == null) return;

                var entries = ParseEntries(givesRep, playerRep);
                if (entries.Count == 0) return;

                // Sort: highest priority first, then alphabetical
                entries.Sort((a, b) =>
                {
                    int cmp = b.Importance.CompareTo(a.Importance);
                    return cmp != 0 ? cmp : string.Compare(
                        a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
                });

                ReputationRenderer.RenderSectionHeader(SB);
                bool showOutcomes = OptionEnabled("OptionRAShowOutcomes");
                bool compact = OptionEnabled("OptionRACompactLayout");
                ReputationRenderer.RenderTracker(SB, entries, showOutcomes, compact);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(
                    $"[ReputationAssistant] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
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

            foreach (string line in clean.Split('\n'))
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var match = ReputationLine.Match(trimmed);
                if (!match.Success) continue;

                string relationship = match.Groups[1].Value;
                string rawName = match.Groups[2].Value.Trim();

                string internalName = FactionResolver.Resolve(rawName);
                if (internalName == null || !seen.Add(internalName)) continue;

                entries.Add(BuildEntry(internalName, rawName, relationship, playerRep));
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

            if (FactionStrategy.Table != null &&
                FactionStrategy.Table.TryGetValue(internalName, out var strat))
            {
                target = strat.Target;
                importance = strat.Importance;
                isSpecial = strat.IsSpecial;
            }

            // In-game option overrides
            ApplyOptionOverrides(internalName, ref importance, ref target);

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

            return new FactionEntry
            {
                DisplayName = displayName,
                InternalName = internalName,
                CurrentRep = currentRep,
                TargetRep = target,
                Importance = importance,
                IsSpecial = isSpecial,
                WRChange = wrChange,
                KillChange = killChange,
            };
        }

        /// <summary>
        /// Reads per-faction Priority/Target overrides from in-game options.
        /// Option IDs: OptionRA_{key}_Priority, OptionRA_{key}_Target
        ///
        /// Special case: Putus Templar has separate options for Mutant and
        /// True Kin genotypes, since the qudzoo guide rates them differently.
        /// </summary>
        static void ApplyOptionOverrides(string internalName, ref int importance, ref int target)
        {
            string key = internalName.Replace(" ", "_");

            // Templar priority depends on player genotype
            if (key == "Templar")
                key = PlayerIsTrueKin() ? "Templar_TrueKin" : "Templar_Mutant";

            string pri = Options.GetOption("OptionRA_" + key + "_Priority");
            if (!string.IsNullOrEmpty(pri) && int.TryParse(pri, out int p))
                importance = Math.Max(0, Math.Min(p, 6));

            string tgt = Options.GetOption("OptionRA_" + key + "_Target");
            if (!string.IsNullOrEmpty(tgt) && int.TryParse(tgt, out int t))
                target = t;
        }

        // ── Helpers ─────────────────────────────────────────────────────

        static bool PlayerIsTrueKin()
        {
            var player = XRLCore.Core?.Game?.Player?.Body;
            return player != null && player.IsTrueKin();
        }

        internal static bool OptionEnabled(string id) =>
            Options.GetOption(id, "Yes")
                .Equals("Yes", StringComparison.OrdinalIgnoreCase);
    }

    // ── Faction display (added as tag after creature name, like Fyrefly's Visible Factions) ──

    [HarmonyPatch]
    static class FactionDisplayPatch
    {
        static MethodBase TargetMethod()
        {
            // GetDisplayName has multiple overloads; pick the one with the most parameters
            // (the full-featured version that other overloads delegate to)
            return typeof(GameObject)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == "GetDisplayName")
                .OrderByDescending(m => m.GetParameters().Length)
                .First();
        }

        public static void Postfix(GameObject __instance, ref string __result)
        {
            try
            {
                if (!ReputationAssistantPatch.OptionEnabled("OptionRAEnabled")) return;
                if (!ReputationAssistantPatch.OptionEnabled("OptionRAShowFaction")) return;
                if (__instance == null || __instance.IsPlayer()) return;

                string factionName = __instance.GetPrimaryFaction();
                if (string.IsNullOrEmpty(factionName)) return;

                var faction = Factions.GetIfExists(factionName);
                if (faction == null || !faction.Visible) return;

                string display = faction.DisplayName;
                if (string.IsNullOrEmpty(display))
                    display = factionName;

                __result += " {{K|(" + display + ")}}";
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(
                    $"[ReputationAssistant] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
