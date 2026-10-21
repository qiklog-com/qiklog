#!/usr/bin/env bash
# PreToolUse guard for Bash. Denies dangerous commands outright and asks Jamey
# to confirm destructive or costly cloud operations.
set -euo pipefail

input=$(cat)
if command -v jq >/dev/null 2>&1; then
  cmd=$(jq -r '.tool_input.command // ""' <<<"$input")
else
  cmd=$(python3 -c 'import json,sys; print(json.load(sys.stdin).get("tool_input",{}).get("command",""))' <<<"$input")
fi

decide() { # $1 = deny|ask, $2 = reason
  printf '{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"%s","permissionDecisionReason":"%s"}}\n' "$1" "$2"
  exit 0
}

# Hard stops
grep -Eq 'git push.*(--force|-f\b).*\b(main|master)\b|git push.*\b(main|master)\b.*(--force|-f\b)' <<<"$cmd" && decide deny "Force-pushing main is never allowed."
grep -Eq 'git push( +[^ ]+)? +(HEAD:)?(main|master)( |$)' <<<"$cmd" && decide deny "Push to a branch and open a PR; never push to main directly."
grep -Eq 'rm +-[a-zA-Z]*r[a-zA-Z]*f?[a-zA-Z]* +(/|~|\$HOME|\.\.?)( |/?$)' <<<"$cmd" && decide deny "Refusing recursive delete of a root, home, or parent directory."
grep -Eq 'gh +repo +delete' <<<"$cmd" && decide deny "Deleting repositories is not allowed."
grep -Eq '(cat|less|more|head|tail|bat) +[^|;]*(\.env(\.local)?|secrets\.json|appsettings\.(Development|Local)\.json)\b' <<<"$cmd" && decide deny "Do not read secret files; ask Jamey for the value by name."

# Ask Jamey first: destructive, costly, or public-facing
grep -Eq '\baz +.*\b(delete|purge|stop)\b' <<<"$cmd" && decide ask "Azure delete/purge/stop. Jamey must approve."
grep -Eq '\baz +network +dns|\baz +containerapp +hostname' <<<"$cmd" && decide ask "DNS or custom-domain change. Jamey must approve."
grep -Eq '\baz +(postgres|containerapp|acr|keyvault|deployment) +.*\bcreate\b' <<<"$cmd" && decide ask "Creates billable Azure resources. Confirm cost with Jamey."
grep -Eq '\baws +.*\b(delete|terminate|remove|rm)\b' <<<"$cmd" && decide ask "AWS destructive command. Jamey must approve."
grep -Eq '\brailway +(down|delete|domain)' <<<"$cmd" && decide ask "Railway takedown or domain change. Jamey must approve."
grep -Eq '\bvercel +(rm|remove|domains|dns|alias)' <<<"$cmd" && decide ask "Vercel removal or domain/DNS change. Jamey must approve."
grep -Eq '\bwrangler +.*\b(delete|dns)\b|\bflarectl\b|api\.cloudflare\.com' <<<"$cmd" && decide ask "Cloudflare change. Jamey must approve."
grep -Eq '\bdotnet +nuget +push|\bnpm +publish' <<<"$cmd" && decide ask "Publishing a package. Jamey must approve."
grep -Eq '\bgh +(pr +merge|release +create|release +delete)' <<<"$cmd" && decide ask "Merging or releasing. Jamey must approve."
grep -Eq '\bgit +tag +-d|\bgit +push +.*--delete' <<<"$cmd" && decide ask "Deleting tags or remote branches. Jamey must approve."
grep -Eq '\b(psql|pg_restore|pg_dump)\b.*(railway|azure|prod)' <<<"$cmd" && decide ask "Touches a hosted database. Jamey must approve."

exit 0
