// Reputation Assistant - Faction Entries
// Author: Kawa | License: MIT
//
// Reads every faction a GivesRep creature affects straight from the game,
// the same way the game awards reputation:
//   - Kill: each "Loved by" faction (GetLovedFactions) and each entry of relatedFactions
//   - Water Ritual: each related faction, plus the creature's primary and other own factions
// A faction can show up more than once (say, loved and admired); its changes add up,
// just like in the game. Kill changes use the worst roll of the game's random variance.
// Water Ritual changes are for a plain ritual: no Tactful or social co-processor bonus,
// and no formal-ritual performance, which can raise or lower every award.
// Works for any faction in the game's registry, so villages, Sultan cults, modded and
// DLC factions need no special handling. Hidden factions are left out, as in the
// game's own description.

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
            int Kill(string status) => ReputationMath.WorstRoll(ReputationMath.KillChange(status, rep));
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
                Add(loved.Name, 0, Kill("love"));

            foreach (var related in givesRep.relatedFactions)
                Add(related.faction,
                    wrDone ? 0 : ReputationMath.WaterRitualChange(related.status, rep),
                    Kill(related.status));

            // The Water Ritual gives +repValue with the creature's primary faction, then with each other
            // faction it belongs to. The game resolves the primary by id or display name, once, into the record.
            if (!wrDone)
            {
                string primary = creature.GetPart<WaterRitualRecord>()?.faction;
                if (primary == null)
                {
                    string key = creature.GetPrimaryFaction(Base: true);
                    primary = (Factions.GetIfExists(key) ?? Factions.GetByDisplayName(key))?.Name;
                }

                Add(primary, rep, 0);
                foreach (var membership in creature.Brain.GetBaseAllegiance())
                    if (membership.Key != primary)
                        Add(membership.Key, rep, 0);
            }

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
                    brokenBond ? kill + ReputationMath.WorstRoll(ReputationMath.BrokenBondChange) : kill));
            }

            entries.Sort(ByPriority);
            return entries;
        }
    }
}
