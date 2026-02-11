// Reputation Assistant - Faction Name Resolution
//
// Maps the display names used in the game's reputation text to internal
// faction identifiers used by the Factions API. Handles articles ("the ..."),
// Unicode apostrophes, Sultan Cults (dynamic names), and procedural villages.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using XRL.World;

namespace Kawa.ReputationAssistant
{
    static class FactionResolver
    {
        /// <summary>
        /// Display name (as shown in game text) → internal faction name.
        /// Only needed where the two differ or the game uses articles/variations.
        /// </summary>
        static readonly Dictionary<string, string> DisplayToInternal =
            new(StringComparer.OrdinalIgnoreCase)
        {
            // Factions whose display name differs from internal name
            {"chavvah, the tree of life",      "Chavvah"},
            {"chavvah",                        "Chavvah"},
            {"children of mamon",              "Mamon"},
            {"consortium of phyta",            "Consortium"},
            {"cult of the coiled lamb",        "Resheph"},
            {"daughters of exile",             "Daughters"},
            {"denizens of the yd freehold",    "YdFreehold"},
            {"dromad merchants",               "Dromad"},
            {"farmers' guild",                 "Farmers"},
            {"fellowship of wardens",          "Wardens"},
            {"grazing hedonists",              "Prey"},
            {"highly entropic beings",         "Entropic"},
            {"hindren of bey lah",             "Hindren"},
            {"issachari tribe",                "Issachari"},
            {"merchants' guild",               "Merchants"},
            {"mysterious strangers",           "Strangers"},
            {"naphtaali tribe",                "Naphtaali"},
            {"putus templar",                  "Templar"},
            {"seekers of the sightless way",   "Seekers"},
            {"villagers of ezra",              "Ezra"},
            {"villagers of joppa",             "Joppa"},
            {"villagers of kyakukya",          "Kyakukya"},
            {"water barons",                   "Water"},

            // Factions whose display name matches internal name — listed because
            // Factions.GetIfExists() is case-sensitive but game text is lowercase.
            {"antelopes",          "Antelopes"},
            {"apes",               "Apes"},
            {"arachnids",          "Arachnids"},
            {"baboons",            "Baboons"},
            {"barathrumites",      "Barathrumites"},
            {"baetyls",            "Baetyls"},
            {"bears",              "Bears"},
            {"birds",              "Birds"},
            {"cannibals",          "Cannibals"},
            {"cats",               "Cats"},
            {"crabs",              "Crabs"},
            {"cragmensch",         "Cragmensch"},
            {"dogs",               "Dogs"},
            {"equines",            "Equines"},
            {"fish",               "Fish"},
            {"flowers",            "Flowers"},
            {"frogs",              "Frogs"},
            {"fungi",              "Fungi"},
            {"girsh",              "Girsh"},
            {"goatfolk",           "Goatfolk"},
            {"gyre wights",        "Gyre Wights"},
            {"hermits",            "Hermits"},
            {"insects",            "Insects"},
            {"mechanimists",       "Mechanimists"},
            {"mollusks",           "Mollusks"},
            {"mopango",            "Mopango"},
            {"newly sentient beings", "Newly Sentient Beings"},
            {"oozes",              "Oozes"},
            {"urchins",            "Urchins"},
            {"pariahs",            "Pariahs"},
            {"roots",              "Roots"},
            {"snapjaws",           "Snapjaws"},
            {"succulents",         "Succulents"},
            {"svardym",            "Svardym"},
            {"swine",              "Swine"},
            {"tortoises",          "Tortoises"},
            {"trees",              "Trees"},
            {"trolls",             "Trolls"},
            {"unshelled reptiles", "Unshelled Reptiles"},
            {"vines",              "Vines"},
            {"winged mammals",     "Winged Mammals"},
            {"worms",              "Worms"},
            {"robots",             "Robots"},
        };

        static readonly Regex MarkupPattern = new(
            @"\{\{[^|]*\|([^}]*)\}\}", RegexOptions.Compiled);

        static readonly object RuntimeMapSync = new();
        static Dictionary<string, string> RuntimeNameToInternal;
        static readonly HashSet<string> RuntimeMisses = new(StringComparer.OrdinalIgnoreCase);
        static readonly TimeSpan RuntimeMapRefreshInterval = TimeSpan.FromSeconds(60);
        static DateTime RuntimeMapNextRefreshUtc = DateTime.MinValue;

        /// <summary>
        /// Strips CoQ color markup: {{color|text}} → text.
        /// Handles nested markup by repeating until stable.
        /// </summary>
        internal static string StripMarkup(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            string result = text;
            string prev;
            do
            {
                prev = result;
                result = MarkupPattern.Replace(result, "$1");
            } while (result != prev);

            return result;
        }

        /// <summary>
        /// Resolves a faction display name (from game reputation text) to its
        /// internal name (used by the Factions API).
        /// Returns null if the faction cannot be identified.
        /// </summary>
        internal static string Resolve(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return null;

            // Normalize Unicode smart quotes to ASCII
            string name = displayName.Trim()
                .Replace('\u2019', '\'')
                .Replace('\u2018', '\'');

            // Known aliases, direct internal names, and runtime case-insensitive map
            if (TryResolveKnownOrRuntime(name, out string result))
                return result;

            // Strip "the " article prefix
            if (name.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
            {
                string bare = name[4..];
                if (TryResolveKnownOrRuntime(bare, out result))
                    return result;
            }

            // Sultan Cults have per-game dynamic display names
            for (int i = 1; i <= 5; i++)
            {
                string cultId = "SultanCult" + i;
                var cult = Factions.GetIfExists(cultId);
                if (cult?.DisplayName == null) continue;

                string cultDisplay = cult.DisplayName;
                if (cultDisplay.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return cultId;
                if (name.StartsWith("the ", StringComparison.OrdinalIgnoreCase) &&
                    cultDisplay.Equals(name[4..], StringComparison.OrdinalIgnoreCase))
                    return cultId;
            }

            // Procedural villages: "villagers of X" / "denizens of X"
            string suffix = null;
            if (name.StartsWith("villagers of ", StringComparison.OrdinalIgnoreCase))
                suffix = name[13..];
            else if (name.StartsWith("denizens of ", StringComparison.OrdinalIgnoreCase))
                suffix = name[12..];

            if (suffix != null)
            {
                if (TryResolveKnownOrRuntime(suffix, out string villageName))
                    return villageName;

                if (suffix.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryResolveKnownOrRuntime(suffix[4..], out villageName))
                        return villageName;
                }
            }

            return null;
        }

        static bool TryResolveKnownOrRuntime(string value, out string internalName)
        {
            internalName = null;
            if (string.IsNullOrEmpty(value))
                return false;

            if (DisplayToInternal.TryGetValue(value, out internalName))
                return true;

            var faction = Factions.GetIfExists(value);
            if (faction != null)
            {
                internalName = faction.Name;
                return true;
            }

            return TryResolveRuntime(value, out internalName);
        }

        static bool TryResolveRuntime(string value, out string internalName)
        {
            internalName = null;
            if (string.IsNullOrEmpty(value))
                return false;

            lock (RuntimeMapSync)
            {
                var now = DateTime.UtcNow;
                bool shouldRefresh = RuntimeNameToInternal == null || now >= RuntimeMapNextRefreshUtc;
                if (shouldRefresh)
                {
                    RuntimeNameToInternal = BuildRuntimeNameMap();
                    RuntimeMisses.Clear();
                    RuntimeMapNextRefreshUtc = now.Add(RuntimeMapRefreshInterval);
                }

                if (RuntimeNameToInternal.TryGetValue(value, out internalName))
                    return true;

                if (!RuntimeMisses.Contains(value))
                    RuntimeMisses.Add(value);

                return false;
            }
        }

        static Dictionary<string, string> BuildRuntimeNameMap()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (var dict in EnumerateFactionDictionaries())
                {
                    foreach (DictionaryEntry item in dict)
                    {
                        if (item.Value is Faction faction)
                        {
                            AddRuntimeAlias(map, faction.Name, faction.Name);
                            AddRuntimeAlias(map, faction.DisplayName, faction.Name);
                        }
                    }
                }
            }
            catch
            {
                // Reflection fallback is best effort only.
            }

            return map;
        }

        static IEnumerable<IDictionary> EnumerateFactionDictionaries()
        {
            var factionType = typeof(Factions);
            const BindingFlags Flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var field in factionType.GetFields(Flags))
            {
                if (!typeof(IDictionary).IsAssignableFrom(field.FieldType))
                    continue;

                if (field.GetValue(null) is IDictionary dictionary)
                    yield return dictionary;
            }

            foreach (var property in factionType.GetProperties(Flags))
            {
                if (!property.CanRead || property.GetIndexParameters().Length > 0)
                    continue;
                if (!typeof(IDictionary).IsAssignableFrom(property.PropertyType))
                    continue;

                IDictionary dictionary = null;
                try
                {
                    dictionary = property.GetValue(null, null) as IDictionary;
                }
                catch
                {
                    // Ignore inaccessible properties.
                }

                if (dictionary != null)
                    yield return dictionary;
            }
        }

        static void AddRuntimeAlias(Dictionary<string, string> map, string alias, string internalName)
        {
            if (string.IsNullOrEmpty(alias) || string.IsNullOrEmpty(internalName))
                return;

            map[alias] = internalName;

            if (alias.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
                map[alias[4..]] = internalName;
        }
    }
}
