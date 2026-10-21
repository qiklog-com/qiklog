---
name: ship
description: Ship the current branch — run tests, push, open or update the PR, and after Jamey merges, tag and smoke-test production.
disable-model-invocation: true
argument-hint: "[tag-name, e.g. v0.10.0-self-serve]"
allowed-tools: Bash(make test) Bash(git status*) Bash(git diff*) Bash(git log*) Bash(git push origin*) Bash(gh pr*)
---
Ship the current branch. Tag name (optional): $ARGUMENTS

1. Confirm you are not on `main` and the working tree is clean (`git status`). If dirty, list the files and stop.
2. Run `make test`. If anything fails, stop and report; do not push.
3. Push the branch and open a PR to `main` with `gh pr create`, or update the existing PR body. The body has: Summary, Conditions of Satisfaction (checked), How tested, Config Jamey must set, Risks.
4. Ask the reviewer subagent to review the PR diff and paste its verdict as a PR comment.
5. Stop and tell Jamey the PR is ready. Do not merge.
6. Only if Jamey says it is merged and a tag name was given: `git checkout main && git pull`, create an annotated tag, push the tag, then run `make smoke` and report the result.
