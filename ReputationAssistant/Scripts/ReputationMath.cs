// Reputation Assistant - Reputation Math
// Author: Kawa | License: MIT
//
// Base reputation changes the game applies per relationship status
// ("love", "friend", "dislike", "hate"), scaled by GivesRep.repValue.
// Kill awards come from GivesRep.HandleEvent(BeforeDeathRemovalEvent),
// Water Ritual awards from WaterRitual.ModifyReputation.

using System;

namespace Kawa.ReputationAssistant
{
    static class ReputationMath
    {
        /// <summary>Killing a water-bonded creature costs about this much with every faction that doesn't hate you.</summary>
        internal const int BrokenBondChange = -100;

        /// <summary>Kill award before the game's random variance (see <see cref="WorstRoll"/>).</summary>
        internal static int KillChange(string status, int repValue) => status switch
        {
            "love" or "friend" => -2 * repValue,
            "dislike" => repValue,
            "hate" => 2 * repValue,
            _ => 0,
        };

        /// <summary>
        /// Water Ritual award for a related faction, for a plain ritual. "love" gets nothing here;
        /// the creature's own factions get +repValue instead.
        /// </summary>
        internal static int WaterRitualChange(string status, int repValue) => status switch
        {
            "friend" => repValue,
            "dislike" => -repValue / 2,
            "hate" => -repValue,
            _ => 0,
        };

        /// <summary>
        /// The lowest result GivesRep.VaryRep can roll for a kill award:
        /// ±10%, or ±50% for awards under 100, rounded to a multiple of 5.
        /// </summary>
        internal static int WorstRoll(int award)
        {
            int spread = Math.Abs(award) >= 100 ? 5 : 25;
            return Math.Min(Vary(award, -spread), Vary(award, spread));
        }

        // Same float arithmetic as GivesRep.VaryRep, with the roll fixed instead of random.
        static int Vary(int rep, int roll)
        {
            float value = rep;
            value += 0.1f * value * roll / 5f;
            return 5 * (int)Math.Round(value / 5f);
        }
    }
}
