# 06 — GitHub PR Automation

How this repo talks to GitHub from Claude Code. Two layers, complementary:

| Layer | Backed by | When it runs | What it's good for |
|---|---|---|---|
| `/create-pr` slash command | `gh` CLI | You type `/create-pr` in Claude Code | The exact ritual you do every PR: branch sanity, push, open PR with the repo's template |
| `github` MCP server | `github/github-mcp-server` (Docker) | Auto-starts with Claude Code | Agentic queries — list/inspect PRs and issues, check Actions runs, search code on GitHub |

Both share the same auth (a GitHub Personal Access Token). Set up once, both work.

---

## 1. One-time setup

### 1a. Install `gh` (GitHub CLI)

In an **admin** PowerShell — but actually winget can install user-scope without admin:

```powershell
winget install --id GitHub.cli --source winget
```

If winget prompts you to accept Microsoft Store terms, accept once.

Restart your shell so `gh` is on `PATH`. Verify:

```powershell
gh --version
```

### 1b. Authenticate `gh`

In Claude Code's prompt, type the next command with the leading `!` so it runs in the live session and you can complete the interactive browser flow:

```
! gh auth login
```

Choose: **GitHub.com** → **HTTPS** → **Login with a web browser** → paste the one-time code.

After that:

```powershell
gh auth status
```

should show your user and `Token scopes: 'gist', 'read:org', 'repo', 'workflow'`.

### 1c. Make the token available to the GitHub MCP server

The MCP server reads `GITHUB_PERSONAL_ACCESS_TOKEN` from the environment. Reuse the token `gh` already obtained — no need to mint a separate PAT in the GitHub UI:

```powershell
# Get the token gh stored, set it at user scope so it persists across shells
setx GITHUB_PERSONAL_ACCESS_TOKEN (gh auth token)
```

Open a fresh PowerShell / restart Claude Code so it picks up the env var. Verify:

```powershell
$env:GITHUB_PERSONAL_ACCESS_TOKEN.Length    # should print a number, not 0
```

> **Why a separate env var?** Because the MCP server runs inside a Docker container — it can't shell out to `gh auth token` itself. The env var is the bridge.

### 1d. Ensure Docker Desktop is running

The MCP server runs as a short-lived Docker container. Docker Desktop must be running for Claude Code to start the server. (You already use Docker MCP Toolkit, so this is likely fine.)

First time Claude Code starts the server, Docker will pull `ghcr.io/github/github-mcp-server` (~one-time download). Subsequent starts are instant.

---

## 2. Daily workflow

### Opening a PR

From a feature branch with committed changes:

```
/create-pr
```

The slash command (see `.claude/commands/create-pr.md`) will:

1. Check you're not on `master`, that you have commits ahead of `master`, and that there's nothing uncommitted.
2. Detect which module(s) you touched and pre-tick those boxes in the PR template.
3. Draft a title and a body filled in from `.github/pull_request_template.md`.
4. Show the draft to you for confirmation.
5. `git push -u origin HEAD`, then `gh pr create --base master ...`.
6. Print the PR URL.

Optional title override:

```
/create-pr Meetings: enforce waitlist promotion FIFO order
```

You then open the PR URL, self-review, approve, merge — same as before.

### Querying GitHub mid-conversation

Once the MCP server is up, Claude has tools like:

| Tool | Use case |
|---|---|
| `mcp__github__list_pull_requests` | "What PRs are open?" |
| `mcp__github__get_pull_request_files` | "What files did PR #5 change?" |
| `mcp__github__list_issues` | "What's tagged `bug`?" |
| `mcp__github__list_workflow_runs` | "Did CI pass on this branch?" |
| `mcp__github__search_code` | "Find every `IBusinessRule` impl on `master`" |

These are no extra effort to use — just ask, and Claude picks the right one.

---

## 3. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `/create-pr` says "gh CLI not available" | `gh` not on PATH | Re-run step 1a, restart shell |
| `gh pr create` fails with auth error | Token expired | `! gh auth login` again, then re-run `setx GITHUB_PERSONAL_ACCESS_TOKEN (gh auth token)` |
| Claude doesn't list `mcp__github__*` tools | Env var not set, or Docker not running | Check `$env:GITHUB_PERSONAL_ACCESS_TOKEN` and Docker Desktop, then restart Claude Code |
| MCP server "exited" in `/mcp` panel | Docker pulling image on first run | Wait ~30s and retry — image is cached after first pull |
| PR opens but description is empty | Template not picked up | Confirm `.github/pull_request_template.md` exists; `gh` auto-loads it |

---

## 4. What lives where

```
.github/pull_request_template.md   # The DDD-tailored template (modules, ADRs, arch tests)
.claude/commands/create-pr.md      # The /create-pr slash command (gh-based)
.mcp.json                          # Project-scoped MCP servers — currently: github
docs/copilot-instructions/06-...   # This doc
```

Tokens and personal Claude settings stay in `.claude/settings.local.json` (gitignored) and Windows env vars — none of this is checked in.
