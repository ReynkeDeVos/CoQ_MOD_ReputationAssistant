using Xunit;

namespace Kawa.ReputationAssistant.Tests
{
    public class FactionEntryTests
    {
        [Fact]
        public void ComputedResults_AddDeltaToCurrentRep()
        {
            var entry = Create(currentRep: -120, targetRep: 50, wrChange: 50, killChange: -50);

            Assert.Equal(-70, entry.WRResult);
            Assert.Equal(-170, entry.KillResult);
        }

        [Theory]
        [InlineData(-50, -50, true)]
        [InlineData(-49, -50, true)]
        [InlineData(-51, -50, false)]
        public void IsOnTarget_UsesCurrentRepAgainstTarget(int currentRep, int targetRep, bool expected)
        {
            var entry = Create(currentRep, targetRep, wrChange: 0, killChange: 0);

            Assert.Equal(expected, entry.IsOnTarget);
        }

        [Fact]
        public void SafetyFlags_AreTrueWhenMovingTowardTargetEvenIfBelowTarget()
        {
            var entry = Create(currentRep: -140, targetRep: 50, wrChange: 50, killChange: 50);

            Assert.True(entry.IsWRSafe);
            Assert.True(entry.IsKillSafe);
        }

        [Fact]
        public void SafetyFlags_AreFalseWhenOutcomeDropsFurtherFromTarget()
        {
            var entry = Create(currentRep: -140, targetRep: 50, wrChange: -50, killChange: -50);

            Assert.False(entry.IsWRSafe);
            Assert.False(entry.IsKillSafe);
        }

        static FactionEntry Create(int currentRep, int targetRep, int wrChange, int killChange)
        {
            return new FactionEntry(
                displayName: "Test",
                internalName: "TestInternal",
                currentRep: currentRep,
                targetRep: targetRep,
                importance: 1,
                isSpecial: false,
                wrChange: wrChange,
                killChange: killChange);
        }
    }
}
