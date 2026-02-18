using Xunit;

namespace Kawa.ReputationAssistant.Tests
{
    public class FactionStrategyTests
    {
        [Fact]
        public void Defaults_AreStable()
        {
            Assert.Equal(-249, FactionStrategy.DefaultTarget);
            Assert.Equal(1, FactionStrategy.DefaultImportance);
        }

        [Fact]
        public void Table_IsCaseInsensitiveAndContainsKnownFaction()
        {
            Assert.True(FactionStrategy.Table.TryGetValue("joppa", out var lower));
            Assert.True(FactionStrategy.Table.TryGetValue("JOPPA", out var upper));

            Assert.Equal(50, lower.Target);
            Assert.Equal(2, lower.Importance);
            Assert.False(lower.IsSpecial);

            Assert.Equal(lower.Target, upper.Target);
            Assert.Equal(lower.Importance, upper.Importance);
        }

        [Fact]
        public void Table_MarksSpecialFactionsCorrectly()
        {
            Assert.True(FactionStrategy.Table.TryGetValue("Mechanimists", out var mechanimists));
            Assert.Equal(300, mechanimists.Target);
            Assert.Equal(3, mechanimists.Importance);
            Assert.True(mechanimists.IsSpecial);
        }

        [Fact]
        public void Table_DoesNotContainUnknownFaction()
        {
            Assert.False(FactionStrategy.Table.TryGetValue("DefinitelyNotAFaction", out _));
        }
    }
}
