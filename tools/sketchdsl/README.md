# sketchdsl

A small text format for explainer diagrams. One source file renders as a flat
hand-drawn sketch or as an isometric (CloudCraft-style) view.

No dependencies. Needs Node 18+.

```bash
node tools/sketchdsl/dsl.mjs docs/diagrams/qiklog-azure.dsl -o out.svg --theme ink
make diagrams   # re-renders the diagrams used on the website
```

Options: `--view flat|iso` and `--theme paper|cloud|ink`. Without them, the
file's own `view` and `theme` settings apply (iso defaults to `cloud`, flat to `paper`).

## Format

One element per line. Indentation only matters under `diagram`, `panel` and `card`.
`# ` starts a comment.

```text
diagram "QikLog on Azure"
  subtitle "From your app's ILogger to your browser, live."
  view iso                      # flat | iso
  theme ink                     # paper | cloud | ink
  title middle                  # flat only: big centered title under the panels
  accent "JWT" "OAuth 2.0"      # words drawn in the accent color

group azure "Azure" @2,0 to 5,2 pad=0.8 tint=sky slab

container api "qiklog-api\nIngest API" @2,0 tint=teal badge="scales to zero"
db        pg  "PostgreSQL"             @3.5,2

api -> pg "store" flow
```

### Nodes

`KIND id "Label" @col,row [tint=…] [badge="…"] [h=2]`

Kinds: `user server app browser lock db registry vault identity ci cloud globe terminal container laptop`.
Positions are grid cells and can be fractional (`@3.5,2`). The first label line is the name; later lines are detail.

### Edges

`from -> to "label" [options]`. Operators: `->`, `<->`, `--` (no arrowhead).
The target can be a group id; the edge then stops at the group's edge.

Edges are numbered in the order written, per panel. `#6` sets a number; `nonum` skips one.

| Option | Meaning |
| --- | --- |
| `route=hv\|vh\|z\|n\|straight` | Path shape. Flat defaults to `hv`, iso to `straight`. |
| `flow` | Iso: accent color with animated dashes (respects reduced motion). |
| `dashed`, `solid` | Line style. |
| `at=0.6` | Where the label sits along the longest segment (0–1). |
| `lpos=above\|below\|left\|right` | Flat: label side. |
| `nudge=dx,dy` | Move the label in pixels. |
| `off=px` | Shift the whole line sideways. Two-way pairs separate on their own. |

### Groups

`group id "Label" @col,row to col,row [pad=0.5] [tint=…] [slab] [solid] [label=front] [loff=1]`

In iso, `slab` draws a raised plate; nodes on it sit on top. `loff` slides the floor label along its edge.

### Panels, notes, card (flat style)

`panel` starts a side-by-side panel with its own grid. `caption "…"` goes on top and `note "…"`
under it. `card terminal` draws the dark window at the bottom:

```text
card terminal
  column "JWT (JSON Web Token)"
    - A bullet
    comment "Example JWT Structure (3 parts)"
    token xxxxx:orange "Header" | yyyyyy:green "Payload" | zzzzzz:blue "Signature"
  column "OAuth 2.0"
    1. A numbered step
```

See `examples/jwt.dsl`.

## Notes

- Fonts load from Google Fonts with `@import` inside the SVG. Browsers skip that when the SVG
  is shown with `<img>`, so text falls back to the system font. Inline the SVG to get the exact fonts.
- Layout is manual on purpose. If labels collide, move a node or use `at=`, `nudge=` or `loff=`.
