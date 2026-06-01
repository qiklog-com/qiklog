# Documentation capture (screenshots + terminal demos)

Keep marketing and user docs fresh when the UI or CLI changes.

## Dashboard screenshots (Playwright)

Requires the stack running (`make up-d`).

```bash
# Install browsers once
dotnet build tests/QikLog.DocGen.Tests
pwsh tests/QikLog.DocGen.Tests/bin/Debug/net9.0/playwright.ps1 install chromium

make docs-capture
```

Writes PNGs to `www/public/docs/screenshots/`:

| File | Page |
|------|------|
| `home.png` | `/` |
| `manage.png` | `/manage` |
| `tail-demo.png` | `/tail/demo` |

Reference in www markdown:

```markdown
![Manage UI](/docs/screenshots/manage.png)
```

## Terminal GIFs (VHS)

Install [VHS](https://github.com/charmbracelet/vhs) (`brew install vhs`).

```bash
make demos-record   # records tapes/*.tape → www/public/demos/*.gif
```

Tapes live in `tapes/`. Edit the `.tape` script, re-run `make demos-record`, commit the GIF if the story changed.

## CI

- `dotnet test` excludes `Category=E2E` (fast PR gate).
- `make test-all` runs everything when you opt in.
- Doc capture is manual / release pipeline until a hosted E2E runner exists.

🐾 *Garfield was here — if the screenshot lies, fix the product or fix the tape.*
