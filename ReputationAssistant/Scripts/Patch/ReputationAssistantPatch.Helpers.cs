using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using XRL.World;
using XRL.World.Parts;

namespace Kawa.ReputationAssistant
{
    static partial class ReputationAssistantPatch
    {
        static void AddCreatureFactionsFromValue(
            List<string> factionNames,
            HashSet<string> seenInternal,
            object value)
        {
            if (value == null)
                return;

            if (value is string text)
            {
                AddCreatureFaction(factionNames, seenInternal, text);
                return;
            }

            if (value is IDictionary dictionary)
            {
                foreach (DictionaryEntry item in dictionary)
                {
                    AddCreatureFaction(factionNames, seenInternal, ExtractFactionName(item.Key));
                    AddCreatureFaction(factionNames, seenInternal, ExtractFactionName(item.Value));
                }

                return;
            }

            if (value is IEnumerable collection)
            {
                foreach (object item in collection)
                {
                    if (item == null)
                        continue;

                    if (item is DictionaryEntry dictionaryEntry)
                    {
                        AddCreatureFaction(factionNames, seenInternal, ExtractFactionName(dictionaryEntry.Key));
                        AddCreatureFaction(factionNames, seenInternal, ExtractFactionName(dictionaryEntry.Value));
                        continue;
                    }

                    if (TryGetKeyValuePair(item, out object key, out object pairValue))
                    {
                        AddCreatureFaction(factionNames, seenInternal, ExtractFactionName(key));
                        AddCreatureFaction(factionNames, seenInternal, ExtractFactionName(pairValue));
                        continue;
                    }

                    AddCreatureFaction(factionNames, seenInternal, ExtractFactionName(item));
                }

                return;
            }

            if (TryGetKeyValuePair(value, out object singleKey, out object singleValue))
            {
                AddCreatureFaction(factionNames, seenInternal, ExtractFactionName(singleKey));
                AddCreatureFaction(factionNames, seenInternal, ExtractFactionName(singleValue));
                return;
            }

            AddCreatureFaction(factionNames, seenInternal, ExtractFactionName(value));
        }

        static void AddCreatureFaction(
            List<string> factionNames,
            HashSet<string> seenInternal,
            string rawName)
        {
            if (TryAddCreatureFaction(factionNames, seenInternal, rawName))
                return;

            foreach (string candidate in FactionHeaderUtilities.EnumerateSplitCandidates(rawName))
                TryAddCreatureFaction(factionNames, seenInternal, candidate);
        }

        static bool TryAddCreatureFaction(
            List<string> factionNames,
            HashSet<string> seenInternal,
            string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                return false;

            string trimmed = rawName.Trim();
            if (trimmed.Length == 0)
                return false;

            string internalName = FactionResolver.Resolve(trimmed);
            if (internalName == null)
                return false;
            if (!seenInternal.Add(internalName))
                return true;

            var faction = Factions.GetIfExists(internalName);
            if (faction == null || !faction.Visible)
                return true;

            string displayName = !string.IsNullOrEmpty(faction.DisplayName)
                ? faction.DisplayName
                : faction.Name;

            if (!string.IsNullOrEmpty(displayName))
                factionNames.Add(displayName);

            return true;
        }

        static List<FactionEntry> ParseEntriesFromDescription(GivesRep givesRep, Reputation playerRep)
        {
            var sb = new StringBuilder();
            givesRep.AppendReputationDescription(sb);
            string raw = sb.ToString();

            var entries = new List<FactionEntry>();
            if (string.IsNullOrEmpty(raw))
                return entries;

            string clean = FactionResolver.StripMarkup(raw);
            var entryIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var resolveCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string ResolveCached(string value)
            {
                if (string.IsNullOrEmpty(value))
                    return null;

                if (resolveCache.TryGetValue(value, out string cached))
                    return cached;

                string resolved = FactionResolver.Resolve(value);
                resolveCache[value] = resolved;
                return resolved;
            }

            foreach (var line in ReputationTextParser.ParseRelationshipLines(clean))
            {
                string relationship = line.Relationship;
                string rawName = line.RawFactionNames;

                string singleResolve = ResolveCached(rawName);
                if (singleResolve != null)
                {
                    var entry = FactionEntryFactory.Build(singleResolve, rawName, relationship, playerRep);
                    FactionEntryAggregator.AddOrMerge(entries, entryIndexes, entry);
                    continue;
                }

                foreach (string name in ReputationTextParser.SplitFactionNames(rawName, ResolveCached))
                {
                    string internalName = ResolveCached(name);
                    if (string.IsNullOrEmpty(internalName))
                        continue;

                    var entry = FactionEntryFactory.Build(internalName, name, relationship, playerRep);
                    FactionEntryAggregator.AddOrMerge(entries, entryIndexes, entry);
                }
            }

            return entries;
        }

        static bool TryParseEntriesFromRelatedFactions(
            GivesRep givesRep,
            Reputation playerRep,
            out List<FactionEntry> entries)
        {
            entries = new List<FactionEntry>();
            object related = GetMemberValue(givesRep, "relatedFactions", "RelatedFactions");
            if (related is not IEnumerable collection)
                return false;

            var entryIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (object item in collection)
            {
                if (!TryReadRelatedFaction(item, out string rawFactionName, out string relationship))
                    continue;

                string internalName = FactionResolver.Resolve(rawFactionName);
                if (string.IsNullOrEmpty(internalName))
                    continue;

                var entry = FactionEntryFactory.Build(internalName, rawFactionName, relationship, playerRep);
                FactionEntryAggregator.AddOrMerge(entries, entryIndexes, entry);
            }

            return true;
        }

        static bool TryReadRelatedFaction(object item, out string factionName, out string relationship)
        {
            factionName = null;
            relationship = null;
            if (item == null)
                return false;

            object rawFaction;
            object rawRelationship;

            if (item is DictionaryEntry dictionaryEntry)
            {
                rawFaction = dictionaryEntry.Key;
                rawRelationship = dictionaryEntry.Value;
            }
            else if (TryGetKeyValuePair(item, out object key, out object value))
            {
                rawFaction = key;
                rawRelationship = value;
            }
            else
            {
                rawFaction = GetMemberValue(item, "Faction", "FactionName", "Name");
                rawRelationship = GetMemberValue(item, "Feeling", "Relationship", "Attitude", "Opinion");
            }

            factionName = ExtractFactionName(rawFaction);
            relationship = NormalizeRelationship(rawRelationship);

            return !string.IsNullOrEmpty(factionName);
        }

        static bool TryGetKeyValuePair(object item, out object key, out object value)
        {
            key = null;
            value = null;
            if (item == null)
                return false;

            var type = item.GetType();
            if (!type.IsValueType || !type.IsGenericType ||
                !string.Equals(type.Name, "KeyValuePair`2", StringComparison.Ordinal))
            {
                return false;
            }

            var keyProperty = type.GetProperty("Key");
            var valueProperty = type.GetProperty("Value");
            if (keyProperty == null || valueProperty == null)
                return false;

            key = keyProperty.GetValue(item, null);
            value = valueProperty.GetValue(item, null);
            return true;
        }

        static string ExtractFactionName(object value)
        {
            if (value == null)
                return null;

            if (value is string s)
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();

            if (value is Faction faction)
                return string.IsNullOrWhiteSpace(faction.Name) ? null : faction.Name;

            object nestedFaction = GetMemberValue(value, "Faction");
            if (nestedFaction != null && !ReferenceEquals(nestedFaction, value))
            {
                string nestedName = ExtractFactionName(nestedFaction);
                if (!string.IsNullOrEmpty(nestedName))
                    return nestedName;
            }

            object name = GetMemberValue(value, "Name", "DisplayName");
            if (name is string nameText && !string.IsNullOrWhiteSpace(nameText))
                return nameText.Trim();

            return null;
        }

        static string NormalizeRelationship(object value)
        {
            if (value == null)
                return null;

            if (value is string s)
                return NormalizeRelationshipName(s);

            if (value is IConvertible convertible)
            {
                try
                {
                    int number = convertible.ToInt32(System.Globalization.CultureInfo.InvariantCulture);
                    return NormalizeRelationshipFromScore(number);
                }
                catch
                {
                    // Fall through to string conversion.
                }
            }

            return NormalizeRelationshipName(value.ToString());
        }

        static string NormalizeRelationshipName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string normalized = value.Trim();
            if (normalized.IndexOf("loved", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Loved";
            if (normalized.IndexOf("admired", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Admired";
            if (normalized.IndexOf("disliked", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Disliked";
            if (normalized.IndexOf("liked", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Liked";
            if (normalized.IndexOf("hated", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Hated";

            if (int.TryParse(normalized, out int numericValue))
                return NormalizeRelationshipFromScore(numericValue);

            return null;
        }

        static string NormalizeRelationshipFromScore(int score)
        {
            if (score >= 100)
                return "Loved";
            if (score >= 50)
                return "Admired";
            if (score > 0)
                return "Liked";
            if (score <= -100)
                return "Hated";
            if (score <= -50 || score < 0)
                return "Disliked";
            return null;
        }

        static object GetMemberValue(object source, params string[] memberNames)
        {
            if (source == null || memberNames == null)
                return null;

            const BindingFlags Flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.IgnoreCase;

            Type type = source.GetType();
            foreach (string memberName in memberNames)
            {
                if (string.IsNullOrEmpty(memberName))
                    continue;

                var property = type.GetProperty(memberName, Flags);
                if (property != null)
                {
                    try
                    {
                        return property.GetValue(source, null);
                    }
                    catch
                    {
                    }
                }

                var field = type.GetField(memberName, Flags);
                if (field != null)
                {
                    try
                    {
                        return field.GetValue(source);
                    }
                    catch
                    {
                    }
                }
            }

            return null;
        }
    }
}
