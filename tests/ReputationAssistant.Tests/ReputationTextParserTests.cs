using System;
using System.Collections.Generic;
using Xunit;

namespace Kawa.ReputationAssistant.Tests
{
    public class ReputationTextParserTests
    {
        [Fact]
        public void TryParseRelationshipLine_WithValidLine_ReturnsParsedLine()
        {
            const string line = "Admired by the Farmers' Guild for telling bawdy jokes.";

            bool parsed = ReputationTextParser.TryParseRelationshipLine(line, out var result);

            Assert.True(parsed);
            Assert.Equal("Admired", result.Relationship);
            Assert.Equal("the Farmers' Guild", result.RawFactionNames);
        }

        [Fact]
        public void TryParseRelationshipLine_WithInvalidLine_ReturnsFalse()
        {
            const string line = "Some unrelated description text.";

            bool parsed = ReputationTextParser.TryParseRelationshipLine(line, out _);

            Assert.False(parsed);
        }

        [Fact]
        public void TryParseRelationshipLine_WithQuestionMarkSuffix_ReturnsParsedLine()
        {
            const string line = "Loved by villagers of Joppa and Inquisitive Spectre of the Oscilloscope-Worshipping Theocracy, of Kiwan People?";

            bool parsed = ReputationTextParser.TryParseRelationshipLine(line, out var result);

            Assert.True(parsed);
            Assert.Equal("Loved", result.Relationship);
            Assert.Equal(
                "villagers of Joppa and Inquisitive Spectre of the Oscilloscope-Worshipping Theocracy, of Kiwan People",
                result.RawFactionNames);
        }

        [Fact]
        public void SplitFactionNames_WithSimpleAndSplit_ReturnsBothNames()
        {
            var resolver = ResolverFor("goatfolk", "pariahs");

            var names = ReputationTextParser.SplitFactionNames("goatfolk and pariahs", resolver);

            Assert.Equal(new[] { "goatfolk", "pariahs" }, names);
        }

        [Fact]
        public void SplitFactionNames_WithOxfordComma_ReturnsAllNames()
        {
            var resolver = ResolverFor("goatfolk", "cragmensch", "pariahs");

            var names = ReputationTextParser.SplitFactionNames("goatfolk, cragmensch, and pariahs", resolver);

            Assert.Equal(new[] { "goatfolk", "cragmensch", "pariahs" }, names);
        }

        [Fact]
        public void SplitFactionNames_WithNameContainingAnd_PreservesCompositeName()
        {
            var resolver = ResolverFor("Cult of Sand and Bone", "goatfolk");

            var names = ReputationTextParser.SplitFactionNames(
                "Cult of Sand and Bone and goatfolk",
                resolver);

            Assert.Equal(new[] { "Cult of Sand and Bone", "goatfolk" }, names);
        }

        [Fact]
        public void SplitFactionNames_WithNameContainingComma_PreservesCompositeName()
        {
            var resolver = ResolverFor("Polymed II, the Charmed Heir of Mollusks", "goatfolk");

            var names = ReputationTextParser.SplitFactionNames(
                "Polymed II, the Charmed Heir of Mollusks and goatfolk",
                resolver);

            Assert.Equal(new[] { "Polymed II, the Charmed Heir of Mollusks", "goatfolk" }, names);
        }

        [Fact]
        public void SplitFactionNames_WithUnknownPrefix_KeepsKnownFactionSeparate()
        {
            var resolver = ResolverFor("goatfolk");

            var names = ReputationTextParser.SplitFactionNames("mysterious faction and goatfolk", resolver);

            Assert.Equal(new[] { "mysterious faction", "goatfolk" }, names);
        }

        [Fact]
        public void SplitFactionNames_WithVillageAndLongModdedName_KeepsBoth()
        {
            var resolver = ResolverFor(
                "villagers of Joppa",
                "Inquisitive Spectre of the Oscilloscope-Worshipping Theocracy, of Kiwan People");

            var names = ReputationTextParser.SplitFactionNames(
                "villagers of Joppa and Inquisitive Spectre of the Oscilloscope-Worshipping Theocracy, of Kiwan People",
                resolver);

            Assert.Equal(
                new[]
                {
                    "villagers of Joppa",
                    "Inquisitive Spectre of the Oscilloscope-Worshipping Theocracy, of Kiwan People",
                },
                names);
        }

        static Func<string, string> ResolverFor(params string[] names)
        {
            var set = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
            return value =>
            {
                string trimmed = value.Trim();
                return set.Contains(trimmed) ? trimmed : null;
            };
        }
    }
}
