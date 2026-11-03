// Reputation Assistant - Faction Priority and Target
// Author: Kawa | License: MIT
//
// Every default lives in Options.xml, taken from https://www.qudzoo.com/advice/reputation.
// Options.GetOption returns the player's value or that XML default.
// Factions without their own sliders (villages, modded or DLC factions) use the "Other" sliders.

using System;
using System.Globalization;
using XRL;
using XRL.UI;

namespace Kawa.ReputationAssistant
{
    internal static class FactionSettings
    {
        // qudzoo's advice for random villages; only used if the options can't be read at all.
        private const int FallbackPriority = 1;
        private const int FallbackTarget = -249;

        internal static void Get(string factionName, out int priority, out int target)
        {
            string key = OptionKey(factionName);
            priority = Read(key, "Priority") ?? Read("Other", "Priority") ?? FallbackPriority;
            target = Read(key, "Target") ?? Read("Other", "Target") ?? FallbackTarget;

            // Clamp in case PlayerOptions.json was edited by hand.
            priority = Math.Max(0, Math.Min(priority, 6));
            target = Math.Max(-600, Math.Min(target, 600));
        }

        private static string OptionKey(string factionName)
        {
            // The guide rates Putus Templar by the player's genotype.
            if (factionName is "Templar")
                return The.Player?.IsTrueKin() is true ? "Templar_TrueKin" : "Templar_Mutant";

            // Keys spell spaces as '_'. An id that already has '_', or one that would spell a
            // Templar genotype key ("Templar Mutant"), would borrow another faction's sliders.
            string key = factionName.Replace(' ', '_');
            return factionName.IndexOf('_', StringComparison.Ordinal) >= 0 || key.StartsWith("Templar_", StringComparison.Ordinal)
                ? "Other"
                : key;
        }

        private static int? Read(string key, string setting) =>
            int.TryParse(Options.GetOption("OptionRA_" + key + "_" + setting),
                    NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value
                : (int?)null;
    }
}
