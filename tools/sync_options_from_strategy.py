#!/usr/bin/env python3
"""Sync ReputationAssistant/Options.xml from FactionStrategy defaults.

Usage:
  python tools/sync_options_from_strategy.py --check
  python tools/sync_options_from_strategy.py --write
"""

from __future__ import annotations

import argparse
import re
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
STRATEGY_PATH = REPO_ROOT / "ReputationAssistant" / "Scripts" / "FactionStrategy.cs"
OPTIONS_PATH = REPO_ROOT / "ReputationAssistant" / "Options.xml"

START_MARKER = "  <!-- ===== Per-Faction Overrides ===== -->"
END_MARKER = "</options>"

SPECIAL_PRIORITY_DEFAULTS = {
    "Templar_TrueKin": 2,
}


def parse_strategy_entries(strategy_text: str) -> list[dict[str, int | str]]:
    pattern = re.compile(
        r'Add\("(?P<name>[^"]+)",\s*(?P<target>-?\d+),\s*(?P<priority>\d+)(?:,\s*special:\s*(?P<special>true|false))?\);'
    )
    entries: list[dict[str, int | str]] = []

    for match in pattern.finditer(strategy_text):
        entries.append(
            {
                "name": match.group("name"),
                "target": int(match.group("target")),
                "priority": int(match.group("priority")),
                "special": bool(match.group("special") == "true"),
            }
        )

    if not entries:
        raise ValueError("No faction strategy entries found.")

    return entries


def option_key(name: str) -> str:
    return name.replace(" ", "_")


def templar_truekin_priority(mutant_priority: int) -> int:
    return SPECIAL_PRIORITY_DEFAULTS.get("Templar_TrueKin", max(0, min(6, mutant_priority + 1)))


def combo_labels(entries: list[dict[str, int | str]]) -> list[str]:
    labels = ["(None)"]
    labels.extend(str(item["label"]) for item in build_option_items(entries))
    return labels


def build_option_items(entries: list[dict[str, int | str]]) -> list[dict[str, int | str]]:
    items: list[dict[str, int | str]] = []

    for entry in entries:
        name = str(entry["name"])
        target = int(entry["target"])
        priority = int(entry["priority"])
        special = bool(entry.get("special", False))

        if name == "Templar":
            tk_priority = templar_truekin_priority(priority)
            items.append(
                {
                    "label": "Templar (Mutant)",
                    "key": "Templar_Mutant",
                    "priority": priority,
                    "target": target,
                    "comment": f"Templar / Mutant (default: {priority}/6 Low-Threat, target {target})",
                }
            )
            items.append(
                {
                    "label": "Templar (True Kin)",
                    "key": "Templar_TrueKin",
                    "priority": tk_priority,
                    "target": target,
                    "comment": f"Templar / True Kin (default: {tk_priority}/6 Maintain, target {target})",
                }
            )
            continue

        items.append(
            {
                "label": name,
                "key": option_key(name),
                "priority": priority,
                "target": target,
                "comment": f"{name} (default: {priority}/6, target {target}{', SPECIAL' if special else ''})",
            }
        )

    items.sort(key=lambda item: str(item["label"]).casefold())
    return items


def format_slider_lines(
    key: str,
    label: str,
    priority_default: int,
    target_default: int,
) -> list[str]:
    return [
        f"  <option ID=\"OptionRA_{key}_Priority\" DisplayText=\"  Priority [default: {priority_default}]\" Category=\"Mod: Reputation Assistant\" Type=\"Slider\" Min=\"0\" Max=\"6\" Increment=\"1\" Default=\"{priority_default}\" Requires=\"OptionRAFaction=={label}\" />",
        f"  <option ID=\"OptionRA_{key}_Target\" DisplayText=\"  Target Rep [default: {target_default}]\" Category=\"Mod: Reputation Assistant\" Type=\"Slider\" Min=\"-600\" Max=\"600\" Increment=\"1\" Default=\"{target_default}\" Requires=\"OptionRAFaction=={label}\" />",
    ]


def build_overrides_block(entries: list[dict[str, int | str]]) -> str:
    option_items = build_option_items(entries)
    combo_values = ",".join(combo_labels(entries))
    lines: list[str] = [
        "  <!-- ===== Per-Faction Overrides ===== -->",
        '  <option ID="OptionRAFaction"',
        '          DisplayText="Configure Faction"',
        '          Category="Mod: Reputation Assistant"',
        '          Type="Combo"',
        f'          Values="{combo_values}"',
        '          Default="(None)"',
        '          Requires="OptionRAEnabled==Yes"',
        '          SearchKeywords="reputation assistant faction configure override">',
        "    <helptext>",
        "      Select a faction to customize its strategic priority and target reputation.",
        "      Changes take effect immediately.",
        "    </helptext>",
        "  </option>",
        "",
    ]

    for item in option_items:
        label = str(item["label"])
        key = str(item["key"])
        priority = int(item["priority"])
        target = int(item["target"])
        comment = str(item["comment"])

        lines.append(f"  <!-- {comment} -->")
        lines.extend(
            format_slider_lines(
                key=key,
                label=label,
                priority_default=priority,
                target_default=target,
            )
        )
        lines.append("")

    while lines and lines[-1] == "":
        lines.pop()

    return "\n".join(lines)


def split_options_document(options_text: str) -> tuple[str, str, str]:
    start = options_text.find(START_MARKER)
    if start < 0:
        raise ValueError(f"Start marker not found: {START_MARKER}")

    end = options_text.rfind(END_MARKER)
    if end < 0 or end < start:
        raise ValueError("Could not locate closing </options> tag")

    prefix = options_text[:start]
    current_block = options_text[start:end].rstrip()
    suffix = options_text[end:]
    return prefix, current_block, suffix


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--check", action="store_true", help="Validate sync without changing files")
    mode.add_argument("--write", action="store_true", help="Rewrite Options.xml per-faction section")
    args = parser.parse_args()

    strategy_text = STRATEGY_PATH.read_text(encoding="utf-8")
    options_text = OPTIONS_PATH.read_text(encoding="utf-8")

    entries = parse_strategy_entries(strategy_text)
    generated_block = build_overrides_block(entries)
    prefix, current_block, suffix = split_options_document(options_text)

    if current_block == generated_block:
        print("Options.xml is already in sync with FactionStrategy.cs")
        return 0

    if args.write:
        updated = f"{prefix}{generated_block}\n\n{suffix}"
        OPTIONS_PATH.write_text(updated, encoding="utf-8")
        print("Updated Options.xml from FactionStrategy.cs")
        return 0

    print("Options.xml is out of sync with FactionStrategy.cs")
    print("Run: python tools/sync_options_from_strategy.py --write")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
