// Reputation Assistant - Faction Entry Data Model
// Author: Kawa | License: MIT

namespace Kawa.ReputationAssistant
{
    /// <summary>
    /// A single faction entry combining strategy data with current game state.
    /// Built by <see cref="ReputationAssistantPatch.BuildEntry"/> during parsing.
    /// </summary>
    readonly struct FactionEntry
    {
        public readonly string DisplayName;
        public readonly string InternalName;
        public readonly int CurrentRep;
        public readonly int TargetRep;
        public readonly int Importance;
        public readonly bool IsSpecial;
        public readonly int WRChange;
        public readonly int KillChange;

        public FactionEntry(
            string displayName, string internalName,
            int currentRep, int targetRep, int importance, bool isSpecial,
            int wrChange, int killChange)
        {
            DisplayName = displayName;
            InternalName = internalName;
            CurrentRep = currentRep;
            TargetRep = targetRep;
            Importance = importance;
            IsSpecial = isSpecial;
            WRChange = wrChange;
            KillChange = killChange;
        }

        // -- Computed properties ------------------------------------------

        public int WRResult => CurrentRep + WRChange;
        public int KillResult => CurrentRep + KillChange;

        /// <summary>Current reputation meets or exceeds target.</summary>
        public bool IsOnTarget => CurrentRep >= TargetRep;

        /// <summary>Water Ritual outcome is at/above target, or moving toward it.</summary>
        public bool IsWRSafe => WRResult >= TargetRep || WRResult > CurrentRep;

        /// <summary>Kill outcome is at/above target, or moving toward it.</summary>
        public bool IsKillSafe => KillResult >= TargetRep || KillResult > CurrentRep;
    }
}
