---
description: Create a GitHub PR from the current feature branch into master, using the repo's DDD-tailored PR template.
argument-hint: [optional PR title override]
allowed-tools: Bash, PowerShell, Read, Edit, Write, Glob, Grep
---

# /create-pr — feature-branch → master PR automation

You are creating a pull request from the **current local feature branch** into **master** for the modular-monolith DDD repo at `C:\Github\POCs\my-dotnet-poc\ddd`. The user's workflow is: branch from master locally → commit → run this command → review/approve/merge on github.com. Your job is the middle step: surface state, push the branch, and open the PR with a thoughtful title and body.

## Hard rules

1. **Never push to or PR against anything other than `master`.** This repo has no `develop`/`main`/`staging`.
2. **Never use `--no-verify`, `--force`, `--force-with-lease`, or amend.** If a hook fails, fix it; if push is rejected, stop and tell the user.
3. **Never commit on the user's behalf in this command.** If there are uncommitted changes, surface them and stop — let the user decide.
4. **Never invent issue numbers or ADR links.** Use `N/A` if you cannot find a real one.
5. If `gh` is not installed or not authenticated, stop immediately and tell the user to run `! winget install --id GitHub.cli` and `! gh auth login` (the `!` prefix runs the command in their session).

## Step 1 — Sanity checks (run all in parallel)

Run these and reason about the output before proceeding:

- `git branch --show-current` — confirm we are NOT on `master`
- `git status --short` — any uncommitted or untracked files?
- `git log --oneline master..HEAD` — what commits will this PR contain?
- `git diff master...HEAD --stat` — what files change, how much churn?
- `git diff master...HEAD --name-only` — for module-detection later
- `gh auth status` — is `gh` ready?

**Stop and ask the user if any of these are true:**
- Current branch is `master` → "You're on master. Switch to your feature branch first."
- There are uncommitted changes → list them and ask "Commit these before opening the PR? Or stash?"
- `master..HEAD` is empty → "No commits ahead of master — nothing to PR."
- `gh auth status` errors → walk through the install/auth steps above.

## Step 2 — Detect affected modules

From the changed-files list, infer which boxes to tick in the PR template's **Affected Module(s)** section. Map file paths to modules:

| Path pattern | Tick |
|---|---|
| `src/Modules/Meetings/**` | Meetings |
| `src/Modules/Administration/**` | Administration |
| `src/Modules/Payments/**` | Payments |
| `src/Modules/Registrations/**` | Registrations |
| `src/Modules/UserAccess/**` | UserAccess |
| `src/BuildingBlocks/**` | BuildingBlocks (cross-cutting) |
| `src/API/**` | API host / composition |
| `src/Database/**` | Database scripts |

Also infer **Type of Change** boxes by looking at file paths and diff content (e.g., a new `*.sql` in `src/Database/` → "Database schema"; a new file under `*/IntegrationEvents/` → "Integration event"; a new aggregate or `*Command.cs` → "Command / Write model").

## Step 3 — Draft the title and body

**Title** (≤ 70 chars):
- If the user passed an argument (`$ARGUMENTS`), use it verbatim.
- Otherwise derive from the commit log. If there's a single commit, use its subject. If there are several, write a one-line summary in imperative mood. Prefix with the affected module if it's single-module (e.g., `Meetings: add waitlist promotion rule`).

**Body** — read `.github/pull_request_template.md` and fill it in:
- Pre-tick the affected-module and type-of-change checkboxes you inferred in Step 2.
- Write a 2-4 sentence **Summary** that explains *why* the change exists (business outcome), then *what* changed at the architectural level. Mine the commit messages, not the diff.
- Leave **Architecture Compliance** checkboxes UNCHECKED — the user must tick them after self-review. Do not assume any of them.
- Leave **Tests** checkboxes UNCHECKED for the same reason.
- Fill **Database Changes** only if `src/Database/**` files changed; otherwise delete the section.
- **Related**: search the commits and branch name for issue numbers (e.g., `POC-10011` from branch names like `feat/POC-10011`). If none found, write `N/A`.

Show the drafted title + body to the user and ask "Open this PR? (y/n/edit)". Do not skip this confirmation.

## Step 4 — Push the branch and create the PR

After user confirms:

1. Check if upstream exists: `git rev-parse --abbrev-ref --symbolic-full-name '@{u}' 2>&1`
2. If no upstream: `git push -u origin HEAD`
3. If upstream exists: `git push`
4. If push is rejected, **stop** and report — never force.

Then create the PR. Use a HEREDOC for the body to preserve formatting:

```
gh pr create --base master --title "<title>" --body "$(cat <<'EOF'
<filled-in template body>
EOF
)"
```

## Step 5 — Return the URL

Print the PR URL `gh pr create` returns, and end with:

```
result: opened PR #<num> at <url> against master from <branch> (<N> commits, <M> files changed)
```

That's the only success signal — the work isn't "done" until that line appears.

## Failure modes — say `failed:` explicitly

- `gh` not installed/authed → `failed: gh CLI not available; run \`! winget install --id GitHub.cli && gh auth login\``
- On master → `failed: refusing to PR from master into master`
- No commits ahead → `failed: no commits to PR (master..HEAD is empty)`
- Push rejected → `failed: push to origin rejected — pull/rebase first, do not force`
- Hook failure on push → `failed: pre-push hook blocked — fix the underlying issue, do not bypass`
