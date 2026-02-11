# Changelog

All notable changes to the Reputation Assistant mod will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.2] - 2026-02-11

### Fixed

- Fixed multi-faction reputation lines not being parsed (e.g. "Admired by goatfolk and pariahs" now correctly shows both factions)

### Changed

- WR/Kill change values now display an explicit `+` sign for positive deltas
- Faction name splitting uses greedy resolution to safely handle Sultan Cult names containing commas or "and"

## [1.2.1] - 2026-02-10

### Fixed

- Restricted own faction info display to the inspection (Look) screen, preventing it from appearing in message logs and other UI elements
  
## [1.2.0] - 2026-02-09

### Added

- Option to display the character's own faction next to its name in the inspection popup (default: off)
- Water-bonded indicator on the section header when a Water Ritual has already been performed with the creature

### Changed

- Changed the icon, with one that uses CoQ own assets

### Fixed

- Fixed C# 12 collection expression syntax that was incompatible with CoQ's runtime

## [1.1.0] - 2026-02-08

### Added

- Configurable per-faction priorities and target reputation via in-game options
- Compact layout option (single data line per faction instead of two)

### Changed

- Reorganized source code into `Scripts/` folder with single-responsibility files
- Modernized C# syntax (target-typed new, readonly struct members, range operators)

### Removed

- Non-functional reset button from options UI
- Stale release setup files

## [1.0.0] - 2026-02-07

### Added

- Initial release of Reputation Assistant mod
- Strategic importance tracking for reputation-affecting creatures (0-6 scale)
- Display of current reputation vs. strategic target for each related faction
- Projected reputation outcomes for Water Ritual actions
- Projected reputation outcomes for combat actions
- Color-coded safety indicators (green for safe, red for dangerous)
- Integration with creature inspection popup
- Priority tier data from A-F-F-I-N-E's qudzoo.com reputation guide
- Reputation mechanics from Caves of Qud wiki

### Technical

- HarmonyLib postfix patch for Description.GetLongDescription
- Faction name resolution system (handles articles, Unicode, Sultan Cults, villages)
- No external dependencies beyond Caves of Qud's included libraries
- Safe for existing saves (no persistent data storage)

---

[1.2.2]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.2.2
[1.2.1]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.2.1
[1.2.0]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.2
[1.1.0]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.1
[1.0.0]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.0
