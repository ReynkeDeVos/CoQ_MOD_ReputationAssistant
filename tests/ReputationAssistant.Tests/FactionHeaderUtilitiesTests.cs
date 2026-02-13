using System.Collections.Generic;
using System.Linq;
using Kawa.ReputationAssistant;
using Xunit;

namespace Kawa.ReputationAssistant.Tests
{
    public class FactionHeaderUtilitiesTests
    {
        [Fact]
        public void ComposeHeader_WithNoFactions_ReturnsNull()
        {
            Assert.Null(FactionHeaderUtilities.ComposeHeader(new List<string>()));
            Assert.Null(FactionHeaderUtilities.ComposeHeader(null));
        }

        [Fact]
        public void ComposeHeader_WithOneFaction_ReturnsThatFaction()
        {
            string header = FactionHeaderUtilities.ComposeHeader(new List<string> { "Fellowship of Wardens" });

            Assert.Equal("Fellowship of Wardens", header);
        }

        [Fact]
        public void ComposeHeader_WithMultipleFactions_KeepsPrimaryAndSortsExtras()
        {
            string header = FactionHeaderUtilities.ComposeHeader(new List<string>
            {
                "Fellowship of Wardens",
                "villagers of Tash",
                "Villagers of Joppa",
            });

            Assert.Equal("Fellowship of Wardens, Villagers of Joppa, villagers of Tash", header);
        }

        [Fact]
        public void EnumerateSplitCandidates_ParsesCommonFactionAssignmentFormats()
        {
            var candidates = FactionHeaderUtilities.EnumerateSplitCandidates(
                "Fellowship of Wardens:-150, villagers of Joppa = -140 | villagers of Tash +50")
                .ToList();

            Assert.Equal(
                new[]
                {
                    "Fellowship of Wardens",
                    "villagers of Joppa",
                    "villagers of Tash",
                },
                candidates);
        }

        [Fact]
        public void EnumerateSplitCandidates_WithoutSeparators_ReturnsEmpty()
        {
            var candidates = FactionHeaderUtilities.EnumerateSplitCandidates("Fellowship of Wardens").ToList();

            Assert.Empty(candidates);
        }
    }
}
