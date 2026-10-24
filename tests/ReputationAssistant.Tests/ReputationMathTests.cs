using Kawa.ReputationAssistant;
using Xunit;

namespace Kawa.ReputationAssistant.Tests
{
    // Expected values follow GivesRep.HandleEvent(BeforeDeathRemovalEvent) and
    // WaterRitual.ModifyReputation in game version 2.0.211.56.
    public class ReputationMathTests
    {
        [Theory]
        [InlineData("love", 100, -200)]
        [InlineData("friend", 100, -200)]
        [InlineData("dislike", 100, 100)]
        [InlineData("hate", 100, 200)]
        [InlineData("love", 25, -50)]
        [InlineData("hate", 800, 1600)]
        [InlineData("unknown", 100, 0)]
        [InlineData(null, 100, 0)]
        public void KillChange_MatchesGame(string status, int repValue, int expected)
        {
            Assert.Equal(expected, ReputationMath.KillChange(status, repValue));
        }

        [Theory]
        [InlineData("friend", 100, 100)]
        [InlineData("dislike", 100, -50)]
        [InlineData("hate", 100, -100)]
        [InlineData("love", 100, 0)] // the game has no Water Ritual case for related "love"
        [InlineData("dislike", 25, -12)] // integer division, like the game
        [InlineData("friend", 800, 800)]
        [InlineData(null, 100, 0)]
        public void WaterRitualChange_MatchesGame(string status, int repValue, int expected)
        {
            Assert.Equal(expected, ReputationMath.WaterRitualChange(status, repValue));
        }
    }
}
