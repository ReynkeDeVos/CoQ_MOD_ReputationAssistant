// Reputation Assistant - Faction Entry Data Model
// Author: Kawa | License: MIT

namespace Kawa.ReputationAssistant
{
    /// <summary>
    /// A single faction entry combining strategy data with current game state.
    /// Built by <see cref="ReputationAssistantPatch.BuildEntry"/> during parsing.
    /// </summary>
    struct FactionEntry
    {
        public string DisplayName;
        public string InternalName;
        public int CurrentRep;
        public int TargetRep;
        public int Importance;
        public bool IsSpecial;
        public int WRChange;
        public int KillChange;

        // ── Computed properties ──────────────────────────────────────

        public readonly int WRResult => CurrentRep + WRChange;
        public readonly int KillResult => CurrentRep + KillChange;

        /// <summary>Current reputation meets or exceeds target.</summary>
        public readonly bool IsOnTarget => CurrentRep >= TargetRep;

        /// <summary>Water Ritual outcome is at/above target, or moving toward it.</summary>
        public readonly bool IsWRSafe => WRResult >= TargetRep || WRResult > CurrentRep;

        /// <summary>Kill outcome is at/above target, or moving toward it.</summary>
        public readonly bool IsKillSafe => KillResult >= TargetRep || KillResult > CurrentRep;
    }
}
