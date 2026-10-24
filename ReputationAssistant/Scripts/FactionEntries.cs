// Reputation Assistant - Faction Entries
// Author: Kawa | License: MIT
//
// Reads every faction a GivesRep creature affects straight from the game,
// the same way the game awards reputation:
//   - Kill: each "Loved by" faction (GetLovedFactions) and each entry of relatedFactions
//   - Water Ritual: each related faction, plus each of the creature's own factions
// A faction can show up more than once (say, loved and admired); its changes add up,
// just like in the game. Works for any faction in the game's registry, so villages,
// Sultan cults, modded and DLC factions need no special handling.

using System;
using System.Collections.Generic;
using XRL.World;
using XRL.World.Parts;

namespace Kawa.ReputationAssistant
{
    static class FactionEntries
    {
        // Highest priority first, then alphabetical.
        static readonly Comparison<FactionEntry> ByPriority = (a, b) =>
        {
            int cmp = b.Importance.CompareTo(a.Importance);
            return cmp != 0 ? cmp : string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
        };

        /// <param name="wrDone">The Water Ritual already happened, so it has nothing left to award.</param>
        internal static List<FactionEntry> Collect(GameObject creature, GivesRep givesRep, Reputation playerRep, bool wrDone)
        {
            int rep = givesRep.repValue;
            var changes = new Dictionary<string, (Faction faction, int wr, int kill)>();

            void Add(string factionName, int wr, int kill)
            {
                var faction = Factions.GetIfExists(factionName);
                if (faction == null || !faction.Visible)
                    return;

                changes.TryGetValue(faction.Name, out var sum);
                changes[faction.Name] = (faction, sum.wr + wr, sum.kill + kill);
            }

            // GetLovedFactions refills one shared list, so read it right away.
            foreach (var loved in givesRep.GetLovedFactions(25, VisibleOnly: true, WithoutReason: true))
                Add(loved.Name, 0, ReputationMath.KillChange("love", rep));

            foreach (var related in givesRep.relatedFactions)
                Add(related.faction,
                    wrDone ? 0 : ReputationMath.WaterRitualChange(related.status, rep),
                    ReputationMath.KillChange(related.status, rep));

            // The Water Ritual gives +repValue with each of the creature's own factions.
            if (!wrDone)
                foreach (var membership in creature.Brain.GetBaseAllegiance())
                    Add(membership.Key, rep, 0);

            var entries = new List<FactionEntry>(changes.Count);
            foreach (var (faction, wr, kill) in changes.Values)
            {
                FactionSettings.Get(faction.Name, out int priority, out int target);
                bool brokenBond = givesRep.wasParleyed && !faction.HatesPlayer;

                entries.Add(new FactionEntry(
                    faction.DisplayName,
                    playerRep.Get(faction),
                    target,
                    priority,
                    isSpecial: faction.Name == "Mechanimists", // the guide rates them outside the tiers
                    wr,
                    brokenBond ? kill + ReputationMath.BrokenBondChange : kill));
            }

            entries.Sort(ByPriority);
            return entries;
        }
    }
}
