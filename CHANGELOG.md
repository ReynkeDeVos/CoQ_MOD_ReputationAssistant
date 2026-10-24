# Changelog

All notable changes to the Reputation Assistant mod will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.6] - 2026-10-24

Checked against Caves of Qud 2.0.211.56.

### Fixed

- Kill and Water Ritual numbers now follow the game's code. A kill costs about twice the creature's reputation value with every faction that loves or admires it (-200 for most legendaries, not -50 or -100), and gains that much with factions that hate it. Creatures with their own value, like Oboroqoru (800), now show their real numbers.
- Kills vary randomly in the game, so the kill numbers and colors now assume the worst roll. A green kill reaches your target, or at least improves your standing, however it rolls.
- The game-API path from 1.4 never actually worked. It looked for the wrong field, failed on every creature, and the mod quietly fell back to parsing the description text. The mod now reads the game data directly.
- Factions the game hides from the description (no reason given) are now listed too, because killing the creature still changes them.
- The Water Ritual projection disappears once you've done the ritual. Before that, it shows a plain ritual: Tactful or a social co-processor add a bit, and a formal ritual can raise or lower the result.
- Killing a water-bonded creature now includes the curse (about -100), and the header warns that it applies to every faction.

### Added

- Works with any faction the game shows you: villages, Sultan cults, and factions from mods or DLC. No name list to keep up to date. Hidden factions stay hidden, like in the game's own description.
- "Other factions" under Configure Faction sets the priority and target for every faction that has no entry of its own.
- Foxen and Snailfolk, the two factions coming with the Diaspora DLC, have their own entries.

### Removed

- The description text parser, the faction name table and all reflection, about 1,000 lines. Faction defaults now live only in `Options.xml`.

## [1.5] - 2026-02-13

Follow-up release focused on faction edge cases.

### Fixed

- Fixed duplicate faction relationships being dropped; repeated entries now merge into one tracker line with combined WR/Kill deltas

### Changed

- "Show Creature's Own Faction" now lists all detected creature factions (not just the primary one)
- Added exhaustive WR/Kill color regression tests, plus tests for duplicate-entry merging and faction-header parsing
- Refactored entry/header logic into dedicated helpers and removed unused runtime miss tracking

## [1.4] - 2026-02-13

Quick hotfix release.

### Fixed

- Fixed Reputation Assistant not working properly when the advanced option "Show reputation with a creature's factions when looking at them" is enabled (thanks to Reddit user **Accio-Books**!)

### Changed

- Clarified the option name for showing a creature's own faction in the Look popup

Clean-up, stabilizing, and testing with other faction mods is still ongoing; a patch will follow. If you find a bug, or want a feature, let me know!

## [1.3] - 2026-02-12

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

[1.5]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.5
[1.4]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.4
[1.3]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.3
[1.2.2]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.2.2
[1.2.1]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.2.1
[1.2.0]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.2
[1.1.0]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.1
[1.0.0]: https://github.com/ReynkeDeVos/CoQ_MOD_ReputationAssistant/releases/tag/v1.0
