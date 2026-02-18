// Reputation Assistant - Faction Entry Factory
//
// Builds a render-ready faction entry from raw relationship data.

using System;
using System.Collections.Generic;
using XRL.World;

namespace Kawa.ReputationAssistant
{
    static class FactionEntryFactory
    {
        static readonly Dictionary<string, (int wr, int kill)> RelationshipDeltas =
            new(StringComparer.OrdinalIgnoreCase)
        {
            { "Loved",    (+100, -100) },
            { "Admired",  ( +50,  -50) },
            { "Liked",    ( +50,  -50) },
            { "Disliked", ( -50,  +50) },
            { "Hated",    (-100, +100) },
        };

        internal static FactionEntry Build(
            string internalName,
            string rawName,
            string relationship,
            Reputation playerRep)
        {
            int target = FactionStrategy.DefaultTarget;
            int importance = FactionStrategy.DefaultImportance;
            bool isSpecial = false;

            if (FactionStrategy.Table.TryGetValue(internalName, out var strat))
            {
                target = strat.Target;
                importance = strat.Importance;
                isSpecial = strat.IsSpecial;
            }

            FactionOptionOverrides.Apply(internalName, ref importance, ref target);

            int wrChange = 0;
            int killChange = 0;
            if (RelationshipDeltas.TryGetValue(relationship, out var delta))
            {
                wrChange = delta.wr;
                killChange = delta.kill;
            }

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
                displayName,
                internalName,
                currentRep,
                target,
                importance,
                isSpecial,
                wrChange,
                killChange);
        }
    }
}
