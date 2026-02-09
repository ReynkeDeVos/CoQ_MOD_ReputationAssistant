// Reputation Assistant - Faction Strategy Defaults
// Source: https://www.qudzoo.com/advice/reputation
//
// Built-in priority tiers and target reputations for all known factions.
// Players can override any value via the in-game options (Mod: Reputation Assistant).

using System;
using System.Collections.Generic;
using XRL;

namespace Kawa.ReputationAssistant
{
    [HasModSensitiveStaticCache]
    static class FactionStrategy
    {
        [ModSensitiveStaticCache]
        internal static Dictionary<string, Entry> Table;

        internal const int DefaultTarget = -249;
        internal const int DefaultImportance = 1;

        [ModSensitiveCacheInit]
        internal static void Init()
        {
            Table = new(StringComparer.OrdinalIgnoreCase);

            //                  Internal Name       Target  Pri  Special
            // ──────────────── 0/6 Irrelevant ─────────────────────
            Add("Antelopes",                -249,   0);
            Add("Birds",                    -249,   0);
            Add("Cannibals",                 250,   0);
            Add("Dogs",                     -249,   0);
            Add("Entropic",                  250,   0); // Highly Entropic
            Add("Equines",                  -249,   0);
            Add("Farmers",                  -249,   0); // Farmers' Guild
            Add("Issachari",                -249,   0); // Issachari Tribe
            Add("Mamon",                    -249,   0); // Children of Mamon
            Add("Newly Sentient Beings",    -249,   0);
            Add("Roots",                    -249,   0);
            Add("Snapjaws",                 -249,   0);
            Add("Succulents",               -600,   0);
            Add("Swine",                    -249,   0);
            Add("Water",                    -600,   0); // Water Barons

            // ──────────────── 1/6 Low-Threat ────────────────────
            Add("Arachnids",                -249,   1);
            Add("Baboons",                  -249,   1);
            Add("Bears",                    -249,   1);
            Add("Cats",                     -249,   1);
            Add("Cragmensch",               -249,   1);
            Add("Frogs",                    -249,   1);
            Add("Girsh",                     200,   1);
            Add("Goatfolk",                 -249,   1);
            Add("Gyre Wights",               200,   1);
            Add("Hindren",                  -249,   1); // Hindren of Bey Lah
            Add("SultanCult1",               250,   1); // dynamic name
            Add("SultanCult2",               250,   1); // dynamic name
            Add("SultanCult3",               250,   1); // dynamic name
            Add("SultanCult4",               250,   1); // dynamic name
            Add("SultanCult5",               250,   1); // dynamic name
            Add("Templar",                  -249,   1); // Putus Templar (Mutant: 1/6, True Kin: 2/6 via option)
            Add("Tortoises",                 250,   1);
            Add("Trees",                     250,   1);
            Add("Trolls",                   -249,   1);
            Add("Urchins",                  -249,   1);
            Add("Vines",                    -249,   1);
            Add("Worms",                    -249,   1);

            // ──────────────── 2/6 Maintain ──────────────────────
            Add("Apes",                      250,   2);
            Add("Baetyls",                  -249,   2);
            Add("Barathrumites",            -249,   2);
            Add("Chavvah",                  -249,   2); // Tree of Life
            Add("Dromad",                    600,   2); // Dromad Merchants
            Add("Fungi",                    -249,   2);
            Add("Hermits",                  -249,   2);
            Add("Joppa",                      50,   2); // Villagers of Joppa
            Add("Merchants",                 600,   2); // Merchants' Guild
            Add("Mopango",                  -249,   2);
            Add("Pariahs",                   100,   2);
            Add("Prey",                     -249,   2); // Grazing Hedonists
            Add("Resheph",                   100,   2); // Cult of the Coiled Lamb
            Add("Strangers",                -249,   2); // Mysterious Strangers
            Add("YdFreehold",               -249,   2); // Yd Freehold

            // ──────────────── 3/6 Don't Reduce ──────────────────
            Add("Consortium",                600,   3); // Consortium of Phyta
            Add("Daughters",                 100,   3); // Daughters of Exile
            Add("Ezra",                      150,   3); // Villagers of Ezra
            Add("Kyakukya",                  100,   3); // Villagers of Kyakukya
            Add("Mechanimists",              300,   3, special: true);
            Add("Wardens",                     0,   3); // Fellowship of Wardens

            // ──────────────── 4/6 Gain Rep ──────────────────────
            Add("Crabs",                    -249,   4);
            Add("Mollusks",                 -249,   4);
            Add("Winged Mammals",            250,   4);

            // ──────────────── 5/6 High Priority ─────────────────
            Add("Fish",                      250,   5);
            Add("Flowers",                   250,   5);
            Add("Insects",                  -249,   5);
            Add("Naphtaali",                -249,   5); // Naphtaali Tribe
            Add("Svardym",                  -249,   5);
            Add("Unshelled Reptiles",       -249,   5);

            // ──────────────── 6/6 CRITICAL ──────────────────────
            Add("Oozes",                    -249,   6);
            Add("Robots",                   -249,   6);
            Add("Seekers",                  -249,   6); // Seekers of the Sightless Way
        }

        static void Add(string name, int target, int importance, bool special = false)
        {
            Table[name] = new()
            {
                Target = target,
                Importance = importance,
                IsSpecial = special,
            };
        }

        internal struct Entry
        {
            public int Target;
            public int Importance;
            public bool IsSpecial;
        }
    }
}
