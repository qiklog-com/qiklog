# QikLog brand guide

Single source of truth for visual identity. **Do not hardcode hex values anywhere else** — reference the tokens in `src/QikLog.Web/wwwroot/brand/brand.css`. If you need a color that doesn't exist there, add it as a token first, then use it.

## Identity

- **Wordmark:** `qiklog` in monospace. `qik` in primary text color, `log` in accent teal. Blinking cursor block follows the `g` to signal "live."
- **Icon:** stylized terminal `Q` in accent teal with a blinking cursor block centered in the counter, on a deep-ink background.
- **Story:** QikLog watches your logs in real time. The terminal Q nods to the command line; the cursor blinks because the system is alive and listening.

## Logo files

Located in `src/QikLog.Web/wwwroot/brand/`:

| File | Use |
|------|-----|
| `icon.svg` | App icon, social avatar, GitHub org image. 120×120 SVG with cursor animation. |
| `favicon.svg` | Browser tab favicon. Same mark, animation removed for static rendering. |
| `lockup.svg` | Icon + wordmark side by side. README, marketing site header, hero. |

When the brand matures (post-launch, post-revenue), regenerate these as hand-tuned vectors in Figma and produce a full asset pack: PNG at 16/32/64/128/256/512/1024, ICO, dark/light/mono variants.

## Colors

All defined as CSS custom properties in `brand.css`. Use the variable names, not the hex.

### Accent
- `--ql-accent-teal` `#2dd4bf` — primary accent for dark surfaces (terminal, icon, dashboard)
- `--ql-accent-teal-dark` `#0d9488` — same accent on light surfaces (marketing site, light mode)
- `--ql-accent-teal-glow` `rgba(45, 212, 191, 0.25)` — focus rings, halos

### Surfaces (dark-mode-first)
- `--ql-surface-ink` `#0d1117` — deepest background; the terminal black
- `--ql-surface-elev1` `#161b22` — cards on ink
- `--ql-surface-elev2` `#21262d` — cards on elev1
- `--ql-surface-border` `#30363d` — dividers on dark

### Text on dark
- `--ql-text-fg` `#c9d1d9` — body
- `--ql-text-fg-strong` `#f0f6fc` — headings
- `--ql-text-fg-muted` `#8b949e` — secondary
- `--ql-text-fg-dim` `#6e7681` — timestamps, tertiary

### Surfaces (light)
- `--ql-paper` `#ffffff`
- `--ql-paper-elev1` `#f6f8fa`
- `--ql-paper-border` `#d0d7de`

### Text on light
- `--ql-text-ink` `#0d1117` — body on white
- `--ql-text-ink-muted` `#57606a` — secondary on white

### Log level colors
Used by `Tail.razor` and any future log-display surface. Do not redefine.

| Token | Hex | Level |
|-------|-----|-------|
| `--ql-level-trace` | `#6e7681` | TRACE |
| `--ql-level-debug` | `#79c0ff` | DEBUG |
| `--ql-level-info` | `#56d364` | INFO |
| `--ql-level-warning` | `#d29922` | WARNING |
| `--ql-level-error` | `#f85149` | ERROR |
| `--ql-level-critical` | `#ff7b72` | CRITICAL |

## Typography

- **Sans:** Inter (with system fallbacks). Use for UI chrome, headings, body text.
- **Mono:** SF Mono / Cascadia Mono / Menlo / Consolas (system stack). Use for the wordmark, code, log content, and anything that should feel "terminal."

Token: `var(--ql-font-sans)` and `var(--ql-font-mono)`.

## Cursor blink

The blinking cursor is a recurring motif. Wherever you display a "live" state, reach for `.ql-cursor` (a span styled to look like a terminal cursor with the standard 1.2s blink animation).

```html
<span class="ql-wordmark">qik<span class="ql-wordmark-accent">log</span></span><span class="ql-cursor"></span>
```

Keep the animation rhythm consistent: 1.2s cycle, 50% duty. Defined in `brand.css` under `@keyframes ql-blink`.

## Voice (companion to the visual)

- Direct, no marketing fluff. "Tail your logs" not "Unlock real-time observability."
- Friendly to developers, not condescending. Assume the reader knows what `tail -f` does.
- Show, don't tell. A curl + screencap demonstrates the product better than a tagline.
- Lowercase by default for the brand name in body copy: "qiklog ingests up to..." not "QikLog Ingests Up To..." (TitleCase only at the start of a sentence or in formal contexts like the App Store).

## Future iteration triggers

Revisit the visual identity when one of these happens:
- $1K MRR (justifies a $300-500 designer pass)
- $5K MRR (justifies a full brand refresh + style guide)
- A bigger competitor copies the look (differentiate)
- The cursor metaphor stops landing because the product expanded beyond log tailing
