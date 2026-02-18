// Reputation Assistant - Faction Entry Aggregation
//
// De-duplicates entries by internal faction id and merges repeated relationships.

using System;
using System.Collections.Generic;

namespace Kawa.ReputationAssistant
{
    static class FactionEntryAggregator
    {
        internal static void AddOrMerge(
            List<FactionEntry> entries,
            Dictionary<string, int> entryIndexes,
            FactionEntry entry)
        {
            if (string.IsNullOrEmpty(entry.InternalName))
                return;

            if (entryIndexes.TryGetValue(entry.InternalName, out int existingIndex))
            {
                entries[existingIndex] = Merge(entries[existingIndex], entry);
                return;
            }

            entryIndexes[entry.InternalName] = entries.Count;
            entries.Add(entry);
        }

        internal static FactionEntry Merge(FactionEntry existing, FactionEntry duplicate)
        {
            return new FactionEntry(
                !string.IsNullOrEmpty(existing.DisplayName) ? existing.DisplayName : duplicate.DisplayName,
                existing.InternalName,
                existing.CurrentRep,
                existing.TargetRep,
                Math.Max(existing.Importance, duplicate.Importance),
                existing.IsSpecial || duplicate.IsSpecial,
                existing.WRChange + duplicate.WRChange,
                existing.KillChange + duplicate.KillChange);
        }
    }
}
