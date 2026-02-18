// Reputation Assistant - Per-Faction Option Overrides

using System;
using XRL.Core;
using XRL.UI;

namespace Kawa.ReputationAssistant
{
    static class FactionOptionOverrides
    {
        internal static void Apply(string internalName, ref int importance, ref int target)
        {
            string key = internalName.Replace(" ", "_");

            // Templar priority depends on player genotype
            if (key == "Templar")
                key = PlayerIsTrueKin() ? "Templar_TrueKin" : "Templar_Mutant";

            string prefix = $"OptionRA_{key}_";

            string pri = Options.GetOption(prefix + "Priority");
            if (!string.IsNullOrEmpty(pri) && int.TryParse(pri, out int p))
                importance = Math.Max(0, Math.Min(p, 6));

            string tgt = Options.GetOption(prefix + "Target");
            if (!string.IsNullOrEmpty(tgt) && int.TryParse(tgt, out int t))
                target = Math.Max(-600, Math.Min(t, 600));
        }

        static bool PlayerIsTrueKin()
        {
            var player = XRLCore.Core?.Game?.Player?.Body;
            return player != null && player.IsTrueKin();
        }
    }
}
