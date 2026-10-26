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

        // Escalating visibility: brown → grey → teal → cyan → orange → magenta → red.
        // Not gold: faction names are gold, and a gold tag would run into them.
        static readonly string[] TierColors = {"w", "y", "c", "C", "O", "M", "R"};

        // A wrapped line loses its indent, so lines stay within the narrowest look popup seen (the 1.2 capture).
        // shortcut: the popup wraps by pixels and fits 78 columns at 1920x1080; measure it if "and N more" cuts lists short.
        const int PopupWidth = 58;

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Appends the section header. Call once before RenderTracker.
        /// A water-bonded creature also gets a warning: killing it curses you.
        /// </summary>
        /// <param name="ownFactions">The creature's own factions. Null if it has none; the title then names the mod.</param>
        internal static void RenderSectionHeader(StringBuilder sb, List<string> ownFactions, bool bonded)
        {
            sb.Append("\n\n{{y|=== ");
            int width;
            if (ownFactions == null || ownFactions.Count == 0)
            {
                sb.Append("Reputation Assistant");
                width = "=== Reputation Assistant".Length;
            }
            else
            {
                sb.Append("Member of");
                width = AppendNames(sb, ownFactions, "=== Member of".Length, " ===".Length, null);
            }
            sb.Append(" ===}}");
            width += " ===".Length;

            if (bonded)
                // The tag moves to its own line when the title leaves no room for it.
                sb.Append(width + "  water-bonded".Length <= PopupWidth ? "  " : "\n  ").Append("{{G|water-bonded}}")
                  .Append("\n  {{R|Killing breaks the bond: about ")
                  .Append(ReputationMath.BrokenBondChange)
                  .Append(" with every faction}}");
        }

        /// <summary>
        /// Appends the kill verdict (with outcomes and its option on) and the reputation tracker entries.
        /// </summary>
        /// <param name="bonded">Water-bonded: the kill also costs factions that aren't listed.</param>
        /// <param name="showWR">False once the Water Ritual is done; there's nothing left to project.</param>
        /// <param name="showVerdict">The "Kill hurts:" line, an option of its own.</param>
        internal static void RenderTracker(StringBuilder sb, List<FactionEntry> entries,
            bool bonded, bool showOutcomes, bool showWR, bool compact, bool showVerdict = true)
        {
            bool verdict = showOutcomes && showVerdict;
            if (verdict)
                RenderKillVerdict(sb, entries, bonded);

            bool first = !verdict; // only the verdict line gets a blank line after it; without it, the entries follow the title

            foreach (var e in entries)
            {
                int tier = Math.Max(0, Math.Min(e.Importance, 6));

                sb.Append(first ? "\n" : "\n\n");
                first = false;

                RenderEntryHeader(sb, e, tier);

                if (compact)
                    RenderCompact(sb, e, showOutcomes, showWR);
                else
                    RenderDefault(sb, e, showOutcomes, showWR);
            }
        }

        // ── Private rendering ───────────────────────────────────────────

        /// <summary>
        /// One line naming each faction a kill hurts, in entry order, so the most important come first.
        ///   Kill hurts: Fellowship of Wardens, villagers of Joppa
        /// </summary>
        static void RenderKillVerdict(StringBuilder sb, List<FactionEntry> entries, bool bonded)
        {
            var hurt = entries.FindAll(e => e.IsKillHurt);
            if (hurt.Count == 0)
            {
                // Bonded, the kill also costs factions not listed here; the bond warning above already says so.
                if (!bonded)
                    sb.Append("\n  {{G|Kill hurts: none}}");
                return;
            }

            // Brown, like the [0/6 Irrelevant] tag, when only Irrelevant factions are hurt.
            sb.Append("\n  {{").Append(hurt.Exists(e => e.Importance > 0) ? "R" : "w").Append("|Kill hurts:}}");
            // One gold span per name: "Chavvah, the Tree of Life" has a comma of its own.
            AppendNames(sb, hurt.ConvertAll(e => e.DisplayName), "  Kill hurts:".Length, 0, "W");
        }

        /// <summary>
        /// Appends " A, B" while the line fits the popup, then " and N more" for the rest; a first name
        /// too long to fit becomes " N factions". Each name keeps room for the suffix that may follow it
        /// and for <paramref name="tail"/>, the text still to come on this line. Returns the line's width.
        /// </summary>
        static int AppendNames(StringBuilder sb, List<string> names, int width, int tail, string color)
        {
            // rest[i]: the width of names i..end with their separators.
            var rest = new int[names.Count + 1];
            for (int i = names.Count - 1; i >= 0; i--)
                rest[i] = rest[i + 1] + (i == 0 ? 1 : 2) + names[i].Length;

            for (int i = 0; i < names.Count; i++)
            {
                string sep = i == 0 ? " " : ", ";
                int after = names.Count - i - 1;
                bool allFit = width + rest[i] + tail <= PopupWidth;
                if (!allFit && width + sep.Length + names[i].Length + (after > 0 ? More(after).Length : 0) + tail > PopupWidth)
                {
                    string summary = i == 0 ? " " + names.Count + (names.Count == 1 ? " faction" : " factions") : More(names.Count - i);
                    sb.Append(summary);
                    return width + summary.Length;
                }

                sb.Append(sep);
                if (color == null)
                    sb.Append(names[i]);
                else
                    sb.Append("{{").Append(color).Append('|').Append(names[i]).Append("}}");
                width += sep.Length + names[i].Length;
            }
            return width;
        }

        static string More(int count) => " and " + count + " more";

        /// <summary>
        /// Priority tag + faction name.
        ///   [3/6 Don't Reduce] Fellowship of Wardens
        /// </summary>
        static void RenderEntryHeader(StringBuilder sb, FactionEntry e, int tier)
        {
            string label = e.IsSpecial ? "Special" : TierLabels[tier];
            sb.Append("  {{").Append(TierColors[tier]).Append("|[")
              .Append(tier).Append("/6 ")
              .Append(label)
              .Append("]}} ");
            AppendWrappedName(sb, e.DisplayName, "  [0/6 ".Length + label.Length + "] ".Length);
        }

        /// <summary>
        /// Appends a gold faction name, breaking it where the popup would, but keeping an indent.
        /// A word too long for one line is cut.
        /// </summary>
        static void AppendWrappedName(StringBuilder sb, string name, int width)
        {
            while (width + name.Length > PopupWidth)
            {
                int cut = name.LastIndexOf(' ', PopupWidth - width);
                bool atSpace = cut > 0;
                if (!atSpace)
                    cut = PopupWidth - width;
                sb.Append("{{W|").Append(name, 0, cut).Append("}}\n    ");
                name = name.Substring(atSpace ? cut + 1 : cut);
                width = 4;
            }
            sb.Append("{{W|").Append(name).Append("}}");
        }

        /// <summary>
        /// Compact layout: one data line per faction, results only. Fits the
        /// look popup's 58 columns.
        ///   Rep -150  target    0 │ Ritual  -50 │ Kill -370
        /// </summary>
        static void RenderCompact(StringBuilder sb, FactionEntry e, bool showOutcomes, bool showWR)
        {
            string repColor = e.IsOnTarget ? "G" : "R";

            sb.Append('\n');
            sb.Append("    Rep {{").Append(repColor).Append("|")
              .Append(Pad(e.CurrentRep)).Append("}}");
            sb.Append("  target ").Append(Pad(e.TargetRep));

            if (!showOutcomes) return;

            if (showWR)
                sb.Append(" {{K|\u2502}} Ritual {{").Append(e.IsWRSafe ? "G" : "R")
                  .Append("|").Append(Pad(e.WRResult)).Append("}}");
            sb.Append(" {{K|\u2502}} Kill {{").Append(e.IsKillSafe ? "G" : "R")
              .Append("|").Append(Pad(e.KillResult)).Append("}}");
        }

        /// <summary>
        /// Default layout: separate lines for rep and outcomes.
        ///   Rep -150  target    0
        ///   Ritual +100 =  -50  │  Kill -220 = -370
        /// </summary>
        static void RenderDefault(StringBuilder sb, FactionEntry e, bool showOutcomes, bool showWR)
        {
            string repColor = e.IsOnTarget ? "G" : "R";

            sb.Append('\n');
            sb.Append("    Rep {{").Append(repColor).Append("|")
              .Append(Pad(e.CurrentRep)).Append("}}");
            sb.Append("  target ").Append(Pad(e.TargetRep));

            if (!showOutcomes) return;

            sb.Append("\n    ");
            if (showWR)
                sb.Append("Ritual ").Append(SignedPad(e.WRChange))
                  .Append(" = {{").Append(e.IsWRSafe ? "G" : "R").Append("|")
                  .Append(Pad(e.WRResult)).Append("}}  {{K|\u2502}}  ");
            sb.Append("Kill ").Append(SignedPad(e.KillChange))
              .Append(" = {{").Append(e.IsKillSafe ? "G" : "R").Append("|")
              .Append(Pad(e.KillResult)).Append("}}");
        }

        // ── Formatting helpers ──────────────────────────────────────────

        /// <summary>Right-aligns a number to 4 characters (e.g. "  50", "-600").</summary>
        static string Pad(int value) => value.ToString().PadLeft(4);

        /// <summary>Right-aligns with explicit sign (e.g. " +50", "-100").</summary>
        static string SignedPad(int value) =>
            (value >= 0 ? "+" + value : value.ToString()).PadLeft(4);
    }
}
