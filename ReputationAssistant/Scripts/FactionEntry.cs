// Reputation Assistant - Faction Entry
// Author: Kawa | License: MIT

namespace Kawa.ReputationAssistant
{
    /// <summary>One faction's line in the tracker: settings plus current and projected reputation.</summary>
    readonly struct FactionEntry
    {
        public readonly string DisplayName;
        public readonly int CurrentRep;
        public readonly int TargetRep;
        public readonly int Importance;
        public readonly bool IsSpecial;
        public readonly int WRChange;
        public readonly int KillChange;

        public FactionEntry(
            string displayName,
            int currentRep, int targetRep, int importance, bool isSpecial,
            int wrChange, int killChange)
        {
            DisplayName = displayName;
            CurrentRep = currentRep;
            TargetRep = targetRep;
            Importance = importance;
            IsSpecial = isSpecial;
            WRChange = wrChange;
            KillChange = killChange;
        }

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
