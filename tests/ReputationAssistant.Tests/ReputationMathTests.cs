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

        // GivesRep.VaryRep: ±10% at 100 or more, ±50% below, rounded to 5 with banker's rounding.
        [Theory]
        [InlineData(-200, -220)]
        [InlineData(200, 180)]
        [InlineData(-100, -110)]
        [InlineData(100, 90)]
        [InlineData(-50, -75)]
        [InlineData(50, 25)]
        [InlineData(25, 10)] // 12.5 / 5 = 2.5 rounds to 2
        [InlineData(-1600, -1760)]
        [InlineData(0, 0)]
        public void WorstRoll_MatchesVaryRepExtremes(int award, int expected)
        {
            Assert.Equal(expected, ReputationMath.WorstRoll(award));
        }
    }
}
