# Changelog

All notable changes to the Reputation Assistant mod will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.4.0] - 2026-02-12

- Renamed option to **Show Creature's Own Faction**
- Fixed compatibility with the advanced option: "Show reputation with a creature's factions when looking at them" by normalizing faction names and using `GO.GetPart<GivesRep>().relatedFactions` first (text parsing fallback) - thanks to Reddit user **Accio-Books** for the suggestion!

## [1.3.0] - 2026-02-12

### Added

- Added tests for faction text parsing
- Added a script to keep faction options synced automatically

### Changed

- Split code into smaller parts to make it easier to maintain
- Improved faction name splitting for names containing commas or "and"
- Improved faction lookup so modded or generated faction names are recognized more reliably
- Reduced repeated error log spam when the same issue occurs many times
- Reduced repeated faction lookup work to keep the Look popup more responsive

## [1.2.2] - 2026-02-11

### Changed

- Updated the icon using Caves of Qud assets
- WR/Kill change values now display an explicit `+` sign for positive deltas
- Improved faction name splitting for Sultan Cult names containing commas or "and"

### Fixed

- Fixed multi-faction reputation lines not being parsed (e.g. "Admired by goatfolk and pariahs" now correctly shows both factions)

## [1.2.1] - 2026-02-10

### Fixed

- Own-faction info now only appears in the Look screen, not in logs or other UI

## [1.2.0] - 2026-02-09

### Added

- Added an option to show a creature's own faction in the inspection popup (default: off)
- Added a water-bonded marker when Water Ritual has already been performed with a creature

### Changed

- Updated the icon to use Caves of Qud assets

### Fixed

- Fixed compatibility with the game's runtime

## [1.1.0] - 2026-02-08

### Added

- Added in-game options to set faction priority and target reputation
- Added a compact layout option (one line per faction)

### Changed

- Reorganized source files into the `Scripts/` folder
- Cleaned up and modernized code style

### Removed

- Removed a reset button that didn't work
- Removed old release setup files

## [1.0.0] - 2026-02-07

### Added

- Added reputation tracker to the creature inspection popup
- Reputation priority tracking for creatures (0-6 scale)
- Current reputation and target reputation display per faction
- Predicted reputation changes for Water Ritual actions
- Predicted reputation changes for combat actions
- Color hints for safe vs. risky actions
- Priority guidance based on A-F-F-I-N-E's qudzoo.com reputation guide
- Reputation rules based on the Caves of Qud wiki

### Technical

- Uses a Harmony patch for the creature description text
- Handles faction name variations (articles, special characters, cults, villages)
- No extra dependencies beyond what Caves of Qud already includes
- Safe for existing saves (no persistent data is stored)

---

[1.3.0]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.3
[1.4.0]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.4
[1.2.2]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.2.2
[1.2.1]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.2.1
[1.2.0]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.2
[1.1.0]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.1
[1.0.0]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.0
