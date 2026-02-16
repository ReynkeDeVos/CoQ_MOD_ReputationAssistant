using System.Collections.Generic;
using Kawa.ReputationAssistant;
using Xunit;

namespace Kawa.ReputationAssistant.Tests
{
    public class FactionEntryAggregatorTests
    {
        [Fact]
        public void AddOrMerge_WithNewFaction_AddsEntry()
        {
            var entries = new List<FactionEntry>();
            var indexes = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

            FactionEntryAggregator.AddOrMerge(entries, indexes, Entry("Joppa", wr: 50, kill: -50));

            Assert.Single(entries);
            Assert.Equal(0, indexes["Joppa"]);
            Assert.Equal(50, entries[0].WRChange);
            Assert.Equal(-50, entries[0].KillChange);
        }

        [Fact]
        public void AddOrMerge_WithDuplicateFaction_MergesDeltasAndPriorityFlags()
        {
            var entries = new List<FactionEntry>();
            var indexes = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

            FactionEntryAggregator.AddOrMerge(entries, indexes, Entry("Joppa", wr: 50, kill: -50, importance: 2));
            FactionEntryAggregator.AddOrMerge(entries, indexes, Entry("joppa", wr: 50, kill: -50, importance: 3, isSpecial: true));

            Assert.Single(entries);
            Assert.Equal(100, entries[0].WRChange);
            Assert.Equal(-100, entries[0].KillChange);
            Assert.Equal(3, entries[0].Importance);
            Assert.True(entries[0].IsSpecial);
        }

        [Fact]
        public void Merge_PreservesExistingDisplayNameAndTargets()
        {
            var existing = new FactionEntry(
                displayName: "Villagers of Joppa",
                internalName: "Joppa",
                currentRep: -140,
                targetRep: 50,
                importance: 2,
                isSpecial: false,
                wrChange: 50,
                killChange: -50);

            var duplicate = new FactionEntry(
                displayName: "joppa",
                internalName: "Joppa",
                currentRep: -140,
                targetRep: 999,
                importance: 0,
                isSpecial: false,
                wrChange: 50,
                killChange: -50);

            var merged = FactionEntryAggregator.Merge(existing, duplicate);

            Assert.Equal("Villagers of Joppa", merged.DisplayName);
            Assert.Equal(50, merged.TargetRep);
            Assert.Equal(100, merged.WRChange);
            Assert.Equal(-100, merged.KillChange);
        }

        static FactionEntry Entry(string internalName, int wr, int kill, int importance = 1, bool isSpecial = false)
        {
            return new FactionEntry(
                displayName: internalName,
                internalName: internalName,
                currentRep: 0,
                targetRep: 0,
                importance: importance,
                isSpecial: isSpecial,
                wrChange: wr,
                killChange: kill);
        }
    }
}
