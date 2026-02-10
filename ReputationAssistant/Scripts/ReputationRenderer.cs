// Reputation Assistant - Rendering
// Author: Kawa | License: MIT
//
// Formats the reputation tracker section appended to creature descriptions.
// Supports two layouts: default (multi-line) and compact (single-line).
//
// Color codes: https://wiki.cavesofqud.com/wiki/Modding:Colors_%26_Object_Rendering

using System;
using System.Collections.Generic;
using System.Text;

namespace Kawa.ReputationAssistant
{
    static class ReputationRenderer
    {
        // ── Display constants ───────────────────────────────────────────

        static readonly string[] TierLabels =
        {
            "Irrelevant",    // 0
            "Low-Threat",    // 1
            "Maintain",      // 2
            "Don't Reduce",  // 3
            "Gain Rep",      // 4
            "High Priority", // 5
            "CRITICAL",      // 6
        };

        // Escalating visibility: brown → grey → teal → gold → orange → magenta → red
        static readonly string[] TierColors = ["w", "y", "c", "W", "O", "M", "R"];

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Appends the section header. Call once before RenderTracker.
        /// If wrDone is true, appends a water-bonded indicator.
        /// </summary>
        internal static void RenderSectionHeader(StringBuilder sb, bool wrDone, string factionHeader = null)
        {
            string title = string.IsNullOrEmpty(factionHeader)
                ? "Reputation Assistant"
                : factionHeader;
            sb.Append("\n\n{{K|=== " + title + " ===}}");

            if (wrDone)
                sb.Append("  {{G|water-bonded}}");
        }

        /// <summary>
        /// Appends the full reputation tracker entries.
        /// </summary>
        internal static void RenderTracker(StringBuilder sb, List<FactionEntry> entries,
            bool showOutcomes, bool compact)
        {
            bool first = true;

            foreach (var e in entries)
            {
                int tier = Math.Max(0, Math.Min(e.Importance, 6));

                sb.Append(first ? "\n" : "\n\n");
                first = false;

                RenderEntryHeader(sb, e, tier);

                if (compact)
                    RenderCompact(sb, e, showOutcomes);
                else
                    RenderDefault(sb, e, showOutcomes);
            }
        }

        // ── Private rendering ───────────────────────────────────────────

        /// <summary>
        /// Priority tag + faction name.
        ///   [3/6 Don't Reduce] Fellowship of Wardens
        /// </summary>
        static void RenderEntryHeader(StringBuilder sb, FactionEntry e, int tier)
        {
            sb.Append("  {{").Append(TierColors[tier]).Append("|[")
              .Append(tier).Append("/6 ")
              .Append(e.IsSpecial ? "Special" : TierLabels[tier])
              .Append("]}} ");
            sb.Append("{{W|").Append(e.DisplayName).Append("}}");
        }

        /// <summary>
        /// Compact layout — single data line per faction.
        ///   -150 / 0   WR→  -50  Kill→ -250
        /// </summary>
        static void RenderCompact(StringBuilder sb, FactionEntry e, bool showOutcomes)
        {
            string repColor = e.IsOnTarget ? "G" : "R";

            sb.Append('\n');
            sb.Append("    {{").Append(repColor).Append("|")
              .Append(Pad(e.CurrentRep)).Append("}}");
            sb.Append(" {{K|/}} ").Append(Pad(e.TargetRep));

            if (!showOutcomes) return;

            sb.Append("   WR{{K|\u2192}}{{").Append(e.IsWRSafe ? "G" : "R")
              .Append("|").Append(Pad(e.WRResult)).Append("}}");
            sb.Append("  Kill{{K|\u2192}}{{").Append(e.IsKillSafe ? "G" : "R")
              .Append("|").Append(Pad(e.KillResult)).Append("}}");
        }

        /// <summary>
        /// Default layout — separate lines for rep and outcomes.
        ///   Rep  -150  target    0
        ///   WR +100 =  -50  │  Kill -100 = -250
        /// </summary>
        static void RenderDefault(StringBuilder sb, FactionEntry e, bool showOutcomes)
        {
            string repColor = e.IsOnTarget ? "G" : "R";

            sb.Append('\n');
            sb.Append("    Rep {{").Append(repColor).Append("|")
              .Append(Pad(e.CurrentRep)).Append("}}");
            sb.Append("  target ").Append(Pad(e.TargetRep));

            if (!showOutcomes) return;

            sb.Append('\n');
            sb.Append("    WR ").Append(Pad(e.WRChange))
              .Append(" = {{").Append(e.IsWRSafe ? "G" : "R").Append("|")
              .Append(Pad(e.WRResult)).Append("}}");
            sb.Append("  {{K|\u2502}}  Kill ").Append(Pad(e.KillChange))
              .Append(" = {{").Append(e.IsKillSafe ? "G" : "R").Append("|")
              .Append(Pad(e.KillResult)).Append("}}");
        }

        // ── Formatting helpers ──────────────────────────────────────────

        /// <summary>Right-aligns a number to 4 characters (e.g. "  50", "-600").</summary>
        static string Pad(int value) => value.ToString().PadLeft(4);
    }
}
