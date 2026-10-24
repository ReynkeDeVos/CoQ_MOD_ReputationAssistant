// Reputation Assistant - Reputation Math
// Author: Kawa | License: MIT
//
// Base reputation changes the game applies per relationship status
// ("love", "friend", "dislike", "hate"), scaled by GivesRep.repValue.
// Kill awards come from GivesRep.HandleEvent(BeforeDeathRemovalEvent),
// Water Ritual awards from WaterRitual.ModifyReputation.

namespace Kawa.ReputationAssistant
{
    static class ReputationMath
    {
        /// <summary>Killing a water-bonded creature costs this much with every faction that doesn't hate you.</summary>
        internal const int BrokenBondChange = -100;

        /// <summary>Kill award. The game varies each award by about ±10%.</summary>
        internal static int KillChange(string status, int repValue) => status switch
        {
            "love" or "friend" => -2 * repValue,
            "dislike" => repValue,
            "hate" => 2 * repValue,
            _ => 0,
        };

        /// <summary>
        /// Water Ritual award for a related faction. "love" gets nothing here;
        /// the creature's own factions get +repValue instead.
        /// </summary>
        internal static int WaterRitualChange(string status, int repValue) => status switch
        {
            "friend" => repValue,
            "dislike" => -repValue / 2,
            "hate" => -repValue,
            _ => 0,
        };
    }
}
