using System.Collections.Generic;
using System.Text;
using Kawa.ReputationAssistant;
using Xunit;

namespace Kawa.ReputationAssistant.Tests
{
    public class ReputationRendererColorTests
    {
        static readonly int[] BoundaryValues =
        {
            -600,
            -250,
            -249,
            -101,
            -100,
            -51,
            -50,
            -1,
            0,
            1,
            49,
            50,
            99,
            100,
            249,
            250,
            600,
        };

        static readonly int[] OutcomeDeltas = { -100, -50, 0, 50, 100 };

        [Fact]
        public void FactionEntry_SafetyFlags_MatchRuleAcrossBoundaryCases()
        {
            foreach (int current in BoundaryValues)
            foreach (int target in BoundaryValues)
            foreach (int delta in OutcomeDeltas)
            {
                var wrEntry = CreateEntry(current, target, delta, 0);
                Assert.Equal(ExpectedSafe(current, target, delta), wrEntry.IsWRSafe);

                var killEntry = CreateEntry(current, target, 0, delta);
                Assert.Equal(ExpectedSafe(current, target, delta), killEntry.IsKillSafe);
            }
        }

        [Fact]
        public void RenderDefault_UsesExpectedGreenRedForRepWrAndKill()
        {
            foreach (int current in BoundaryValues)
            foreach (int target in BoundaryValues)
            foreach (int wrDelta in OutcomeDeltas)
            foreach (int killDelta in OutcomeDeltas)
            {
                var entry = CreateEntry(current, target, wrDelta, killDelta);
                string rendered = Render(entry, compact: false);

                string repColor = entry.IsOnTarget ? "G" : "R";
                string wrColor = entry.IsWRSafe ? "G" : "R";
                string killColor = entry.IsKillSafe ? "G" : "R";

                Assert.Contains("Rep {{" + repColor + "|", rendered);
                Assert.Contains("WR " + SignedPad(wrDelta) + " = {{" + wrColor + "|", rendered);
                Assert.Contains("Kill " + SignedPad(killDelta) + " = {{" + killColor + "|", rendered);
            }
        }

        [Fact]
        public void RenderCompact_UsesExpectedGreenRedForRepWrAndKill()
        {
            foreach (int current in BoundaryValues)
            foreach (int target in BoundaryValues)
            foreach (int wrDelta in OutcomeDeltas)
            foreach (int killDelta in OutcomeDeltas)
            {
                var entry = CreateEntry(current, target, wrDelta, killDelta);
                string rendered = Render(entry, compact: true);

                string repColor = entry.IsOnTarget ? "G" : "R";
                string wrColor = entry.IsWRSafe ? "G" : "R";
                string killColor = entry.IsKillSafe ? "G" : "R";

                Assert.Contains("    {{" + repColor + "|", rendered);
                Assert.Contains("WR{{K|\u2192}}{{" + wrColor + "|", rendered);
                Assert.Contains("Kill{{K|\u2192}}{{" + killColor + "|", rendered);
            }
        }

        static FactionEntry CreateEntry(int current, int target, int wrChange, int killChange)
        {
            return new FactionEntry(
                displayName: "Test Faction",
                internalName: "TestFaction",
                currentRep: current,
                targetRep: target,
                importance: 2,
                isSpecial: false,
                wrChange: wrChange,
                killChange: killChange);
        }

        static string Render(FactionEntry entry, bool compact)
        {
            var sb = new StringBuilder();
            ReputationRenderer.RenderTracker(
                sb,
                new List<FactionEntry> { entry },
                showOutcomes: true,
                compact: compact);

            return sb.ToString();
        }

        static bool ExpectedSafe(int current, int target, int delta)
        {
            int result = current + delta;
            return result >= target || result > current;
        }

        static string SignedPad(int value)
        {
            string signed = value >= 0 ? "+" + value : value.ToString();
            return signed.PadLeft(4);
        }
    }
}
