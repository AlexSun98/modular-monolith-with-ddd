---
name: diagnose
description: Disciplined diagnosis loop for hard bugs and performance regressions. Reproduce → minimise → hypothesise → instrument → fix → regression-test. Use when user says "diagnose this" / "debug this", reports a bug, says something is broken/throwing/failing, or describes a performance regression.
---

# Diagnose

A discipline for hard bugs. Skip phases only when explicitly justified.

Before starting: use the domain glossary in `docs/catalog-of-terms/` to build a clear mental model of the relevant modules. Check `docs/architecture-decision-log/` for ADRs in the area you're touching.

## Phase 1 — Build a feedback loop

**This is the skill.** Everything else is mechanical. If you have a fast, deterministic, agent-runnable pass/fail signal for the bug, you will find the cause. If you don't, no amount of staring at code will save you.

Spend disproportionate effort here. **Be aggressive. Be creative. Refuse to give up.**

### Ways to construct one — try in roughly this order

1. **Failing test** at whatever seam reaches the bug — unit (domain), integration (module stack), or e2e.
2. **`dotnet test --filter`** with a targeted fixture against a running integration test environment.
3. **HTTP script** via `curl` or PowerShell `Invoke-RestMethod` against `http://localhost:5000`.
4. **Replay a captured trace** — save a real HTTP request payload to disk; replay through the code path.
5. **Throwaway harness** — spin up one module with a minimal `ModuleStartup`, call the handler directly.
6. **Property / fuzz loop** — if the bug is "sometimes wrong output", run many inputs and look for the failure.
7. **Bisection harness** — if the bug appeared between two known commits, `git bisect run dotnet test`.
8. **Differential loop** — run the same input through two configs/versions and diff outputs.

Build the right feedback loop, and the bug is 90% fixed.

### Iterate on the loop itself

- Can I make it faster? (Skip unrelated module init, narrow the test scope.)
- Can I make the signal sharper? (Assert on the specific symptom, not "didn't crash".)
- Can I make it more deterministic? (Pin time, seed randomness, isolate DB state with transactions.)

A 30-second flaky loop is barely better than no loop. A 2-second deterministic loop is a debugging superpower.

### Non-deterministic bugs

Goal: **higher reproduction rate**, not a clean repro. Loop 100×, add stress, narrow timing windows. A 50%-flake bug is debuggable; 1% is not.

### When you genuinely cannot build a loop

Stop and say so explicitly. List what you tried. Ask the user for: (a) an environment that reproduces it, (b) a captured artifact (log dump, SQL trace, screen recording with timestamps), or (c) permission to add temporary instrumentation.

Do not proceed to Phase 2 without a loop you believe in.

## Phase 2 — Reproduce

Run the loop. Watch the bug appear.

Confirm:

- [ ] The loop produces the failure the **user** described — not a nearby failure. Wrong bug = wrong fix.
- [ ] The failure is reproducible (or for non-deterministic bugs, at a high enough rate).
- [ ] You have captured the exact symptom (exception type + message, wrong output, timing) for Phase 5 verification.

## Phase 3 — Hypothesise

Generate **3–5 ranked hypotheses** before testing any of them.

Each must be **falsifiable**:

> "If `<X>` is the cause, then `<changing Y>` will make the bug disappear."

If you cannot state the prediction, the hypothesis is a vibe — discard or sharpen it.

**Show the ranked list to the user before testing.** They often have domain knowledge that re-ranks instantly. Don't block — proceed with your ranking if the user is AFK.

## Phase 4 — Instrument

Each probe must map to a specific prediction from Phase 3. **Change one variable at a time.**

Tool preference:

1. **Rider/VS debugger** — one breakpoint beats ten log statements.
2. **Targeted Serilog log** at the boundaries that distinguish hypotheses.
3. Never "log everything and grep".

**Tag every debug log** with a unique prefix, e.g. `[DEBUG-a4f2]`. Cleanup becomes a single grep. Untagged logs survive; tagged logs die.

**Perf branch** — for performance regressions, establish a baseline measurement first (SQL query plan via SSMS, `Stopwatch`, BenchmarkDotNet). Measure before fixing.

## Phase 5 — Fix + regression test

Write the regression test **before the fix** — but only if there is a **correct seam**.

A correct seam exercises the **real bug pattern** at the call site. If the only available seam is too shallow (can't replicate the triggering chain), a regression test there gives false confidence.

**If no correct seam exists, that itself is the finding.** Note it and flag for `/improve-codebase-architecture`.

If a correct seam exists:

1. Turn the minimised repro into a failing test at that seam.
2. Watch it fail.
3. Apply the fix.
4. Watch it pass.
5. Re-run the Phase 1 loop against the original (un-minimised) scenario.

## Phase 6 — Cleanup + post-mortem

Required before declaring done:

- [ ] Original repro no longer reproduces (re-run the Phase 1 loop)
- [ ] Regression test passes (or absence of seam is documented)
- [ ] All `[DEBUG-...]` instrumentation removed (`grep` the prefix)
- [ ] Throwaway harnesses deleted
- [ ] The winning hypothesis is stated in the commit message

**Then ask: what would have prevented this bug?** If the answer involves architectural change (no good test seam, tangled callers, hidden coupling across module boundaries) hand off to `/improve-codebase-architecture` with the specifics. Make the recommendation *after* the fix — you have more information now.
