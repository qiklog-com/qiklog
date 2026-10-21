#!/usr/bin/env bash
# SessionStart repo self-gate. Doctrine after a work order once landed in the wrong
# repo: the remote and the branch are line one of every session, before any work.
set -euo pipefail

remote=$(git remote get-url origin 2>/dev/null || echo "NO GIT REMOTE")
branch=$(git branch --show-current 2>/dev/null || echo "detached")

printf 'repo: %s\n' "$remote"
printf 'branch: %s\n' "$branch"

case "$remote" in
  *qiklog*) ;;
  *) printf 'STOP: this is not the QikLog repo. Confirm with Jamey before doing anything.\n' ;;
esac

if [ "$branch" = "main" ] || [ "$branch" = "master" ]; then
  printf 'STOP: you are on %s. Create a working branch before editing anything.\n' "$branch"
fi
