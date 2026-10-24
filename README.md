# Reputation Assistant <img src="ReputationAssistant/icon.png" alt="" height="30" />

A [Caves of Qud](https://store.steampowered.com/app/333640/Caves_of_Qud/) mod. Look at a legendary, or anyone else whose death or Water Ritual changes your reputation, and it lists every faction involved: how much that faction matters, where you stand with it, and what a Water Ritual or a kill would do.

I made it because I kept opening the faction list before every Water Ritual.

Get it on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3664930126).

<img src="promo/assets/screenshot_v1.2.png" alt="The tracker under a creature's description" height="340" /> <img src="promo/assets/screenshot_v1.2_compact.png" alt="Compact layout" height="340" /> <img src="promo/assets/screenshot_menu_v1.2.png" alt="Options menu" height="340" />

**Spoilers:** the priorities come from A-F-F-I-N-E's [reputation guide](https://www.qudzoo.com/advice/reputation) on qudzoo.com. They're opinionated and give away which factions matter later, so maybe leave this off for your first few runs.

## Reading it

```txt
=== Reputation Assistant ===
  [5/6 High Priority] Naphtaali tribe
    Rep -350  target -249
    WR +100 = -250  │  Kill -200 = -550
```

The tag is the guide's priority, from 0 (irrelevant) to 6 (critical). The target is the reputation you want to stay at or above. Green means you're on target or getting closer. Red means you're below it, or about to drop further.

The game varies each kill by about 10%, so read the kill number as roughly right. The WR part goes away once you've done the ritual. If you're water-bonded, you get a reminder that killing them costs 100 reputation with every faction.

## Options

Everything is under **Mod: Reputation Assistant** in the options menu. You can hide the WR and kill line, squeeze each faction onto one line, or show the creature's own factions as the title.

**Configure Faction** changes the priority and target of any faction. Foxen and Snailfolk from the Diaspora DLC have their own entries. **Other factions** covers villages and factions added by other mods.

It works with existing saves, and you can remove it any time: it only adds text to the look popup.

It pairs well with [You Spot a Legendary!](https://steamcommunity.com/sharedfiles/filedetails/?id=3507307896) by Cattlesquat, which tells you when a legendary shows up.

## How it works

A Harmony postfix on `Description.GetLongDescription` reads `GivesRep.relatedFactions`, `GivesRep.GetLovedFactions()` and the creature's base allegiance. It then applies the same formulas the game uses for kills and the Water Ritual. Faction defaults live in [`Options.xml`](ReputationAssistant/Options.xml).

[MIT](LICENSE)
