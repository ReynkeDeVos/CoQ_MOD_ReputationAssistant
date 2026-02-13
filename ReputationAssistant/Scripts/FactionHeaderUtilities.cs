// Reputation Assistant - Faction Header Utilities
//
// Shared helpers for formatting creature faction headers and splitting
// runtime faction-assignment text into candidate faction names.

using System;
using System.Collections.Generic;

namespace Kawa.ReputationAssistant
{
    static class FactionHeaderUtilities
    {
        static readonly char[] FactionListSeparators = { ',', ';', '|' };

        internal static string ComposeHeader(List<string> factionNames)
        {
            if (factionNames == null || factionNames.Count == 0)
                return null;
            if (factionNames.Count == 1)
                return factionNames[0];

            string primary = factionNames[0];
            var extras = factionNames.GetRange(1, factionNames.Count - 1);
            extras.Sort(StringComparer.OrdinalIgnoreCase);

            factionNames.Clear();
            factionNames.Add(primary);
            factionNames.AddRange(extras);

            return string.Join(", ", factionNames);
        }

        internal static IEnumerable<string> EnumerateSplitCandidates(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                yield break;

            string trimmed = rawName.Trim();
            if (trimmed.Length == 0)
                yield break;

            if (trimmed.IndexOfAny(FactionListSeparators) < 0)
                yield break;

            foreach (string segment in trimmed.Split(FactionListSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = TrimFactionAssignment(segment);
                if (!string.IsNullOrWhiteSpace(candidate))
                    yield return candidate;
            }
        }

        static string TrimFactionAssignment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string candidate = value.Trim();

            int colon = candidate.IndexOf(':');
            if (colon > 0)
                candidate = candidate[..colon].Trim();

            int equals = candidate.IndexOf('=');
            if (equals > 0)
                candidate = candidate[..equals].Trim();

            int minus = candidate.LastIndexOf('-');
            if (minus > 0 && int.TryParse(candidate[(minus + 1)..].Trim(), out _))
                candidate = candidate[..minus].Trim();

            int plus = candidate.LastIndexOf('+');
            if (plus > 0 && int.TryParse(candidate[(plus + 1)..].Trim(), out _))
                candidate = candidate[..plus].Trim();

            return candidate;
        }
    }
}
