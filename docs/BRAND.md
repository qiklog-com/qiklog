# QikLog Brand

Canonical identity for QikLog. This file is the source of truth. If a color,
face, or asset appears anywhere in the product, it comes from here.

Status: adopted 2026-08-13. Supersedes the teal gradient logo drafts
(`logo-1.jpg`, `logo-2.jpg`), which should not ship.

## The mark

A prompt frame with the bottom right corner broken open, a chevron inside, and
a detached tail crossing the gap. It reads two ways on purpose: a terminal
prompt box, and a Q. That double read is the whole idea, so do not close the
corner and do not attach the tail.

Files: `assets/qiklog-mark.svg` (rust), `assets/qiklog-mark-reverse.svg` (paper).

Three paths, one stroke color, no fills. Repalette by changing the single
`stroke` value on the group.

### Construction rules

- 64 unit grid. Stroke width 8. Corner radius 12.
- Round caps and round joins throughout. Square caps break the drawing.
- Stroke weight is not decorative. At 8 units the mark survives 16px; thinner
  strokes dissolve at favicon scale. That was the flaw in the original draft.
- Keep the clearance between the tail and the chevron. They blur into one shape
  when moved closer.

### Color variants

| Surface | Stroke |
| --- | --- |
| Paper or any light background | `#B94700` rust |
| Ink or any dark background | `#FCFAF5` paper |
| Rust field (app icon, badges) | `#FCFAF5` paper |

Rust is primary. Rust on ink loses too much contrast, so dark surfaces get the
paper mark rather than the rust one.

### Clear space and minimum size

- Clear space on all sides: 8 grid units, one stroke width.
- Minimum size: 16px. Below that use a solid rust square with no interior detail.
- Never rotate, skew, add gradients, add shadows, or recolor outside the table above.

## Palette

| Token | Hex | Use |
| --- | --- | --- |
| paper | `#FCFAF5` | Backgrounds, reversed marks and type |
| ink | `#2E2A26` | Body text, dark surfaces |
| rust | `#B94700` | The mark, accents, primary actions, eyebrows |
| hairline | `#D9D3CA` | Rules, borders, dividers |

Muted body text sits at `#5C554D`. Captions and metadata at `#8A8177`. Both are
derived from ink, not new brand colors.

Rust is an accent, not a field color. It carries the mark, primary buttons, and
small structural marks. It does not become a section background.

## Type

| Role | Face | Weights |
| --- | --- | --- |
| Display and headings | Bricolage Grotesque | 700, 800 |
| Body and UI | Public Sans | 400, 500, 600 |
| Code, logs, metadata | IBM Plex Mono | 400, 500 |

Headings run tight: tracking `-0.02em` at display sizes, `-0.01em` at section
sizes. Mono is not decoration; it marks things that are literally machine
output, such as log lines, keys, timestamps, and hex values.

The wordmark is Bricolage Grotesque 800 at `-0.03em`, set as `Qik` in ink and
`Log` in rust. In the lockup, mark height matches the wordmark cap height.

## Voice

- No em dashes in anything outward facing. They read as an AI tell.
- Sentence case for headings, labels, and buttons.
- Active voice. A button says what happens: "Send logs", not "Submit".
- An action keeps its name through the whole flow. "Publish" produces "Published".
- Errors explain what happened and what to do. They do not apologize and they
  are never vague.
- Empty states are invitations to act, not decoration.

## Asset kit

To generate from `assets/qiklog-mark.svg`:

- `favicon.ico` — 16, 32, 48
- `favicon.svg` — modern browsers
- `apple-touch-icon.png` — 180, paper mark on rust field
- `icon-192.png`, `icon-512.png` — PWA manifest, paper mark on rust field
- `og-image.png` — 1200x630, lockup on paper, rust hairline border

Do not generate raster derivatives from the old JPGs. Their transparency is a
checkerboard baked into the pixels, not an alpha channel.

## Reference

`assets/brand-preview.html` renders the mark at every size and variant.
