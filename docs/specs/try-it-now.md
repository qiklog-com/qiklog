# Try it now playground

Branch: `feat/try-it-now-playground`, from `main`.

Page: `src/QikLog.Web` home (`/`), the "Try it now" screen on app.qiklog.com.
No changes under `www/`.

## Problem

The home page prints `QikLog:HubApiKey` into the curl sample. That key is the
bootstrap tenant's key (the published one starts with `l6qrq16m`). Anyone who
loads the page can read every source that key can read. The page is also a
static curl for the shared `demo` source, with a warning that everything sent
there is public.

## Part 1 — Security

1. No configured API key is rendered. Not in HTML, not in a `data-*` attribute,
   not in a URL, not in a log line from this feature.
2. New setting `QikLog:Demo:ApiKey` (`QikLog__Demo__ApiKey`). Demo sends and the
   home-page tail subscription use it. Server side only.
3. When `QikLog:Demo:ApiKey` is unset or blank, use `QikLog:HubApiKey`. Log one
   startup warning: `QikLog:Demo:ApiKey is unset; the try-it-now page is using QikLog:HubApiKey.`
   The warning text does not contain the key.
4. Samples show `Authorization: Bearer $QIKLOG_API_KEY` (curl) and
   `YOUR_API_KEY` (C# and JavaScript), plus a link "Sign in to get your own
   key" to `/login`.

## Part 2 — Playground

The home page is a private tail for this circuit.

- Message input, max 120 characters. Sanitize with `QikLog.Core.DemoMessage`
  (the same rules as `www/api/demo-send.js`: trim, drop code points `<= 0x1F`
  and `0x7F`, then cap at 120). Send runs on the server through
  `POST {ApiBaseUrl}/v1/logs` with the demo credential. The browser does not
  receive that credential.
- Level chips: Info, Warn, Error, Critical. Default Info. The wire level is
  `info`, `warning`, `error`, `critical`. The tail row uses the existing
  `--ql-level-*` color for that level.
- Stopwatch: from the Send (or simulated send) call to the matching line
  arriving on the SignalR handler. The row shows `arrived in N ms` (`N` is
  whole milliseconds, minimum 0). `your best: N ms` is the smallest of those
  for this circuit, hidden until the first one.
- Simulate traffic: one static list of 8 lines, sent 375 ms apart (first to
  last is under 3 seconds). The list includes `200 GET /api/orders 24ms`,
  `WARN slow query orders_by_customer 812ms`, and
  `ERROR 500 POST /checkout NullReferenceException`.
- Source: `demo-` plus 4 to 6 lowercase letters or digits, chosen once per
  circuit. The tail subscribes to that source only, with the demo credential,
  not the visitor's OIDC token. Copy: "This tail is just yours. It disappears
  when you leave." The old public-demo sentence is gone.
- Code tabs curl | C# | JavaScript. The template strings in
  `DemoCodeSamples` match `www/src/lib/code-deck.ts` exactly. Rendering
  replaces `__MSG__` (escaped), the template's `demo` source, and its `info`
  level with the current message, the visitor source, and the selected wire
  level. Placeholders for the key stay as in the template.
- After the third successful send (manual and simulated lines both count),
  show a quiet card, not a modal: "That's QikLog. Get your own key: free up
  to 10,000 lines a month." "Get your own key" links to `/login`.

### Limits

Server side. A denied click sets a friendly message and disables the button.
No exception.

| Limit | Scope | Message |
|-------|--------|---------|
| 1 manual send per second | circuit | Give it a second. |
| 1 simulate burst per 30 seconds | circuit | Simulate is cooling down. |
| 60 lines total | circuit | That's the limit for this visit. |
| 300 lines per rolling hour | client IP, across circuits | This network is at its hourly limit. |

A burst is 8 lines. It is refused as a whole when fewer than 8 lines remain
in the circuit cap or the IP window. Manual sends check the 1-second gap.
Burst lines do not. Both consume the 60 and the 300. Caps are checked before
cooldowns. A missing client IP does not join a shared "unknown" bucket; that
circuit still has the per-circuit caps.

Failed ingest (not HTTP 202, or no key configured) does not throw. Message:
`Couldn't send that line. Try again.` No key configured, before any send:
`Demo ingest is not configured.` Empty message after sanitize does not send.

### Visual

Brand tokens only. No new fonts. `prefers-reduced-motion: reduce` disables
animation inside the playground. The layout stacks by 800px and stays usable
at 390px (gutter, wrapping chips, full-width actions, horizontal scroll on
the sample).

## Out of scope

Share links, OpenTelemetry or gRPC samples, Postman, any file under `www/`,
deleting stored demo lines when the circuit ends, rotating the leaked key
(that is a production step, not code).

## Conditions of Satisfaction

1. Rendering the home page with `HubApiKey` and `Demo:ApiKey` set does not
   include either value in the markup, on any code tab.
2. Fallback to `HubApiKey` is logged once at startup with the fixed warning
   and without the key. A set demo key does not log that warning.
3. Samples contain `$QIKLOG_API_KEY` and `YOUR_API_KEY` and the sign-in link.
4. Sanitize matches the existing demo-message rules. Send posts source, wire
   level, and sanitized message with the server credential.
5. The tail row for a sent line uses that level's color token and
   `arrived in N ms`. Best updates to the minimum.
6. The scripted burst is 8 lines, contains the three examples, and spaces
   sends 375 ms apart.
7. The source matches `demo-[a-z0-9]{4,6}` and is stable for the session.
   The public-demo sentence is absent. The private-tail sentence is present.
8. Code templates match `code-deck.ts`. A typed message, level, and source
   appear in the active sample. The key placeholders stay.
9. The third successful line reveals the nudge card and the `/login` link.
10. Each limit disables the matching control, shows its message, and does not
    throw. Two circuits on one IP share the hourly cap.
11. Playground CSS uses `var(--ql-*)` only, includes a 390px layout, and
    turns animation off under `prefers-reduced-motion`.

## Scenarios

xUnit + Shouldly. Render tests use bunit, same stack as the web component
tests. Bodies start with `// Given:`, `// When:`, `// Then:`.

### 1. `Given_configured_keys_When_home_rendered_Then_markup_has_no_key`

`tests/QikLog.Web.Tests`. Covers COS 1 and 3.

- **Given** demo key `ql_demo_secret_value` and hub key `ql_hub_secret_value`
- **When** the home page renders and each code tab is selected
- **Then** the markup contains neither secret
- **And** it contains `$QIKLOG_API_KEY`, `YOUR_API_KEY`, and
  `Sign in to get your own key` pointing at `/login`

### 2. `Given_demo_key_missing_When_resolving_Then_hub_key_is_used_and_warned`

Covers COS 2.

- **Given** a blank demo key and a hub key
- **When** startup fallback runs
- **Then** the resolved credential is the hub key
- **And** one warning equals the fixed sentence and does not contain the hub key
- **And** a set demo key resolves to itself and logs no warning

### 3. `Given_control_chars_When_sanitized_Then_capped_at_120`

Covers COS 4. Existing `DemoMessageTests` stay green after the helper moves
to `QikLog.Core`.

### 4. `Given_message_and_level_When_send_clicked_Then_server_posts_them`

Covers COS 4.

- **Given** the input `hello\nthere` and chip Error
- **When** Send is clicked
- **Then** the server ingest receives source `demo-…`, level Error, message
  `hellothere`
- **And** the rendered page still does not contain the demo key

### 5. `Given_matching_line_arrives_When_noted_Then_latency_and_best_show`

Covers COS 5.

- **Given** a send marked at t0 and the same line observed 15 ms later, then
  another observed at 40 ms
- **When** the tail row renders
- **Then** the first row reads `arrived in 15 ms` and uses
  `--ql-level-error` when the level is Error
- **And** `your best: 15 ms` remains after the slower line

### 6. `Given_simulate_When_burst_runs_Then_eight_scripted_lines_are_spaced`

Covers COS 6.

- **Given** the static script
- **When** the burst runs with a fake delay
- **Then** 8 posts go out in list order, including the three example messages
- **And** the recorded pauses are 375 ms

### 7. `Given_new_session_When_source_created_Then_private_tail_copy`

Covers COS 7.

- **Given** a new home page
- **When** it renders
- **Then** the sample source matches `demo-[a-z0-9]{4,6}`
- **And** the markup contains `This tail is just yours. It disappears when you leave.`
- **And** it does not contain `Treat anything you send as public`

### 8. `Given_code_deck_When_templates_compared_Then_they_match`

Covers COS 8.

- **Given** `www/src/lib/code-deck.ts`
- **When** its three `template` strings are read
- **Then** they equal the curl, C#, and JavaScript constants
- **And** rendering with message `say "hi"`, level warning, source `demo-ab12`
  bakes those in and still shows `$QIKLOG_API_KEY` or `YOUR_API_KEY`

### 9. `Given_two_lines_sent_When_third_succeeds_Then_nudge_appears`

Covers COS 9.

- **Given** two successful sends
- **When** the third succeeds
- **Then** the card text is present and its link is `/login`
- **And** it was absent before the third

### 10. `Given_limits_When_exceeded_Then_friendly_denial`

Covers COS 10.

- **Given** a circuit clock
- **When** a second manual send happens before 1 second, a second burst
  before 30 seconds, the 61st line, or the 301st line on one IP across two
  circuits
- **Then** each denial returns the message in the table above and consumes
  nothing further
- **And** a burst that needs 8 lines with 7 remaining is refused entirely
- **And** the home page shows that message and disables the control

COS 11 is the stylesheet. Review it; `make test` does not compute layout.
