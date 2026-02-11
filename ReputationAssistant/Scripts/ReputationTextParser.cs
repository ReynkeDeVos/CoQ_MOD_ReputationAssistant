// Reputation Assistant - Reputation Text Parsing
//
// Parses GivesRep description lines and splits combined faction names while
// preserving names that contain separators like commas or "and".

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Kawa.ReputationAssistant
{
    static class ReputationTextParser
    {
        internal readonly struct RelationshipLine
        {
            public readonly string Relationship;
            public readonly string RawFactionNames;

            public RelationshipLine(string relationship, string rawFactionNames)
            {
                Relationship = relationship;
                RawFactionNames = rawFactionNames;
            }
        }

        static readonly Regex ReputationLinePattern = new(
            @"(Loved|Admired|Liked|Disliked|Hated) by (.+?)(?:\s+for\s+.+)?(?:[.!?])?\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        static readonly Regex FactionSeparatorPattern = new(
            @"\s+and\s+|,\s+",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        static readonly Regex OxfordCommaPattern = new(
            @",\s+and\s+",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        internal static List<RelationshipLine> ParseRelationshipLines(string cleanText)
        {
            var parsed = new List<RelationshipLine>();
            if (string.IsNullOrEmpty(cleanText))
                return parsed;

            foreach (string line in cleanText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (TryParseRelationshipLine(line, out var relationshipLine))
                    parsed.Add(relationshipLine);
            }

            return parsed;
        }

        internal static bool TryParseRelationshipLine(string line, out RelationshipLine relationshipLine)
        {
            relationshipLine = default;
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var match = ReputationLinePattern.Match(line.Trim());
            if (!match.Success)
                return false;

            string relationship = match.Groups[1].Value;
            string rawFactionNames = match.Groups[2].Value.Trim();
            if (rawFactionNames.Length == 0)
                return false;

            relationshipLine = new RelationshipLine(relationship, rawFactionNames);
            return true;
        }

        internal static List<string> SplitFactionNames(string raw, Func<string, string> resolver)
        {
            var names = new List<string>();
            if (string.IsNullOrWhiteSpace(raw) || resolver == null)
                return names;

            string normalized = OxfordCommaPattern.Replace(raw, ", ");

            var tokens = new List<string>();
            var separators = new List<string>();
            Tokenize(normalized, tokens, separators);

            if (tokens.Count == 0)
                return names;

            if (tokens.Count == 1)
            {
                names.Add(tokens[0]);
                return names;
            }

            var best = BuildBestSplit(
                0,
                tokens,
                separators,
                resolver,
                new Dictionary<int, SplitPlan>(),
                new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase));

            if (best == null)
                return names;

            foreach (string segment in best.Segments)
            {
                string trimmed = segment.Trim();
                if (trimmed.Length > 0)
                    names.Add(trimmed);
            }

            return names;
        }

        sealed class SplitPlan
        {
            public readonly int ResolvedCount;
            public readonly int SegmentCount;
            public readonly List<string> Segments;

            public SplitPlan(int resolvedCount, int segmentCount, List<string> segments)
            {
                ResolvedCount = resolvedCount;
                SegmentCount = segmentCount;
                Segments = segments;
            }
        }

        static SplitPlan BuildBestSplit(
            int start,
            List<string> tokens,
            List<string> separators,
            Func<string, string> resolver,
            Dictionary<int, SplitPlan> memo,
            Dictionary<string, bool> resolvedCache)
        {
            if (memo.TryGetValue(start, out var cached))
                return cached;

            if (start >= tokens.Count)
            {
                var done = new SplitPlan(0, 0, new List<string>());
                memo[start] = done;
                return done;
            }

            SplitPlan best = null;

            for (int end = start; end < tokens.Count; end++)
            {
                string candidate = JoinTokens(tokens, separators, start, end);
                bool isResolved = IsResolved(candidate, resolver, resolvedCache);

                var next = BuildBestSplit(start: end + 1, tokens, separators, resolver, memo, resolvedCache);
                int resolvedCount = (isResolved ? 1 : 0) + next.ResolvedCount;
                int segmentCount = 1 + next.SegmentCount;

                var segments = new List<string>(segmentCount)
                {
                    candidate
                };
                segments.AddRange(next.Segments);

                var plan = new SplitPlan(resolvedCount, segmentCount, segments);
                if (IsBetter(plan, best))
                    best = plan;
            }

            memo[start] = best;
            return best;
        }

        static bool IsResolved(
            string value,
            Func<string, string> resolver,
            Dictionary<string, bool> resolvedCache)
        {
            if (resolvedCache.TryGetValue(value, out bool cached))
                return cached;

            bool resolved = resolver(value) != null;
            resolvedCache[value] = resolved;
            return resolved;
        }

        static bool IsBetter(SplitPlan candidate, SplitPlan current)
        {
            if (candidate == null)
                return false;
            if (current == null)
                return true;

            if (candidate.ResolvedCount != current.ResolvedCount)
                return candidate.ResolvedCount > current.ResolvedCount;

            if (candidate.SegmentCount != current.SegmentCount)
                return candidate.SegmentCount < current.SegmentCount;

            return false;
        }

        static string JoinTokens(List<string> tokens, List<string> separators, int start, int end)
        {
            var sb = new StringBuilder(tokens[start]);
            for (int i = start; i < end; i++)
            {
                sb.Append(i < separators.Count ? separators[i] : " and ");
                sb.Append(tokens[i + 1]);
            }

            return sb.ToString();
        }

        static void Tokenize(string raw, List<string> tokens, List<string> separators)
        {
            int index = 0;
            string pendingSeparator = null;

            foreach (Match match in FactionSeparatorPattern.Matches(raw))
            {
                string token = raw[index..match.Index].Trim();
                if (token.Length > 0)
                {
                    if (tokens.Count > 0)
                        separators.Add(pendingSeparator ?? " and ");

                    tokens.Add(token);
                    pendingSeparator = match.Value;
                }
                else pendingSeparator ??= match.Value;

                index = match.Index + match.Length;
            }

            string tail = raw[index..].Trim();
            if (tail.Length > 0)
            {
                if (tokens.Count > 0)
                    separators.Add(pendingSeparator ?? " and ");

                tokens.Add(tail);
            }
        }
    }
}
