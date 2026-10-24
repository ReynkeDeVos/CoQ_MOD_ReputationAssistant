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

        static readonly int[] OutcomeDeltas = { -300, -200, -100, -50, 0, 50, 100, 200 };

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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RenderTracker_AfterWaterRitual_ShowsOnlyKill(bool compact)
        {
            var sb = new StringBuilder();
            ReputationRenderer.RenderTracker(sb, new List<FactionEntry> { CreateEntry(0, -249, 0, -200) },
                showOutcomes: true, showWR: false, compact: compact);

            string rendered = sb.ToString();
            Assert.DoesNotContain("WR", rendered);
            Assert.Contains("Kill", rendered);
        }

        [Fact]
        public void RenderSectionHeader_WhenBonded_WarnsAboutTheCurse()
        {
            var bonded = new StringBuilder();
            ReputationRenderer.RenderSectionHeader(bonded, null, bonded: true);
            Assert.Contains("=== Reputation Assistant ===", bonded.ToString());
            Assert.Contains("water-bonded", bonded.ToString());
            Assert.Contains("about -100 with every faction", bonded.ToString());

            var notBonded = new StringBuilder();
            ReputationRenderer.RenderSectionHeader(notBonded, "apes, Kyakukya", bonded: false);
            Assert.Contains("=== apes, Kyakukya ===", notBonded.ToString());
            Assert.DoesNotContain("water-bonded", notBonded.ToString());
        }

        static FactionEntry CreateEntry(int current, int target, int wrChange, int killChange)
        {
            return new FactionEntry(
                displayName: "Test Faction",
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
                showWR: true,
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
