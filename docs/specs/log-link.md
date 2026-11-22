# Spec: log links

Written 2026-11-21. Source: docs/MARKETING_PLAN.md ("The product decision everything depends on").

## Problem

QikLog's promise is "console.log for your server", as easy as making a short link.
Today the first real snippet needs an API key, and a key needs an account. A stranger
cannot put a line from their own code on a tail without signing up first.

## Scope

A log link is one URL that you send to and open.

- `POST /l` creates a link. No account, no key.
- `POST /l/{id}` appends a line. The request body is the line.
- `GET /l/{id}` in a browser opens the live tail. From curl it prints the lines.
- `GET /l/{id}/raw` always returns plain text, for terminals and coding agents.
- `GET /l/{id}/lines` returns the same lines as JSON for the web tail.
- The web app serves the tail at `/l/{id}` and a compact embed at `/embed/l/{id}`.
- qiklog.com leads with "Get a log link" and proxies `/l` to the API.

## Out of scope (next PR)

- **Keep this link.** Claiming a link into an account (private reads, no expiry, lines
  counted against the plan). It touches tenant isolation and gets its own spec and
  review. Until then "Keep this link" sends the visitor to sign-up.
- Named links, custom slugs, share permissions.
- Any change to `POST /v1/logs`, API keys, plans, or billing.

## Rules

1. The id is 12 characters from a 31-character alphabet with no look-alikes
   (about 59 bits). The link is the secret. It is never written to application logs.
2. A link lasts 24 hours and holds 500 lines. A line is at most 2,048 characters.
   One request carries at most 50 lines. All four numbers are settings (`QikLog:Links`).
3. Link creation is limited per client address (20 an hour by default).
4. Link lines belong to no tenant. They never count against a customer's monthly cap
   and never appear in any tenant's sources or history.
5. Reading always works while the link exists, including when it is full.
6. Expired links and their lines are deleted when new links are created.
7. Level comes from `?level=`, else a JSON `level`, else a leading token such as
   `ERROR` or `warn:`, else `info`.

## Conditions of Satisfaction

1. A stranger can create a link and send a line with no credential, with auth
   enforcement on.
2. The line is stored, is broadcast to live viewers of that link, and comes back from
   `/raw` and `/lines`.
3. `curl <url> -d "it broke"` works as written (curl sends a form content type).
4. A JSON body with `message` (and optional `level`) is accepted.
5. Piped multi-line bodies become one line each.
6. Unknown and malformed ids return 404. Expired links return 410. A full link returns
   402 on send and still reads. Oversized lines return 413. An empty body returns 400.
7. Link lines are invisible to tenants: not in history, not in usage.
8. A browser `GET /l/{id}` redirects to the web tail when `WebBaseUrl` is set.
9. Creation is rate limited per address and returns 429 past the limit.
10. Browsers on other origins can create links and send lines (CORS, no credentials).
11. The web tail page shows the link, the one-line snippet, the "anyone with this link
    can read it" warning, and never renders a server credential.
12. `POST /v1/logs` behaviour is unchanged.

## Scenarios

```text
Given auth enforcement is on
When a visitor POSTs /l with no credential
Then they get 201 with an id, a url, and an expiry about 24 hours away

Given a link
When "it broke" is POSTed to it with curl's default content type
Then the response is 202 and GET /raw contains "INFO  it broke"

Given a link
When "ERROR 500 POST /checkout" is POSTed
Then the stored level is error

Given a link
When {"message":"slow query","level":"warn"} is POSTed as JSON
Then the stored level is warning and the message is "slow query"

Given a link that expired a minute ago
When a line is POSTed
Then the response is 410 and nothing is stored

Given a link holding 500 lines
When a line is POSTed
Then the response is 402 and GET /raw still returns 200

Given a link with one line and a tenant API key
When the tenant reads /v1/sources/link-<id>/logs
Then the list is empty

Given a hub subscriber on a link
When a line is POSTed to that link
Then the subscriber receives LogReceived with that message
```
