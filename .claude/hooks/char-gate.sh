#!/usr/bin/env bash
# PostToolUse gate for Write/Edit. Jamey cannot retype typographic glyphs and they
# read as an AI tell in anything outward-facing, so we keep them out of the repo.
# Emoji are fine; typographic punctuation is not. Warns Claude so it fixes the file.
set -euo pipefail

input=$(cat)

python3 - "$input" <<'PY'
import json, os, sys

# Keyed by code point so this gate file stays clean under its own rule.
BANNED = {
    0x2014: "em dash (use a comma, colon, or hyphen)",
    0x2013: "en dash (use a hyphen or the word 'to')",
    0x2018: "curly single quote (use a straight apostrophe)",
    0x2019: "curly single quote (use a straight apostrophe)",
    0x201C: "curly double quote (use a straight double quote)",
    0x201D: "curly double quote (use a straight double quote)",
    0x2026: "ellipsis character (use three periods)",
    0x00A0: "non-breaking space (use a normal space)",
    0x00B7: "middle dot (use a hyphen or comma)",
    0x2192: "arrow glyph (use the two characters - and >)",
}
BANNED = {chr(cp): why for cp, why in BANNED.items()}

try:
    payload = json.loads(sys.argv[1])
except Exception:
    sys.exit(0)

path = (payload.get("tool_input") or {}).get("file_path") or ""
if not path or not os.path.isfile(path):
    sys.exit(0)

try:
    with open(path, "r", encoding="utf-8") as fh:
        lines = fh.readlines()
except (UnicodeDecodeError, OSError):
    sys.exit(0)

hits = []
for n, line in enumerate(lines, 1):
    for ch, why in BANNED.items():
        if ch in line:
            hits.append("  line %d: %s" % (n, why))
            break

if hits:
    sys.stderr.write(
        "char-gate: %s contains untypable characters. Replace them with plain "
        "keyboard characters before moving on.\n%s\n" % (path, "\n".join(hits[:20]))
    )
    sys.exit(2)
PY
