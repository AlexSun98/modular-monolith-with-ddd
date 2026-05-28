---
name: handoff
description: Compact the current conversation into a handoff document for another agent to pick up.
argument-hint: "What will the next session be used for?"
---

Write a handoff document summarising the current conversation so a fresh agent can continue the work. Save to `%TEMP%\handoff-<timestamp>.md` — not the current workspace.

Include a "suggested skills" section recommending skills the next agent should invoke. Prefer skills from this project: `/diagnose`, `/tdd`, `/improve-codebase-architecture`, `/grill-with-docs`.

Do not duplicate content already captured in other artifacts (ADRs in `docs/architecture-decision-log/`, specs in `docs/spec-driven/`, plans, commits, diffs). Reference them by path instead.

Redact any sensitive information (connection strings, API keys, passwords, PII).

If the user passed arguments, treat them as a description of what the next session will focus on and tailor the doc accordingly.
