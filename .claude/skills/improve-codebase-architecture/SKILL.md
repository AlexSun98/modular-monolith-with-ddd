---
name: improve-codebase-architecture
description: Find deepening opportunities in a codebase, informed by the domain language in docs/catalog-of-terms/ and the decisions in docs/architecture-decision-log/. Use when the user wants to improve architecture, find refactoring opportunities, consolidate tightly-coupled modules, or make a codebase more testable and AI-navigable.
---

# Improve Codebase Architecture

Surface architectural friction and propose **deepening opportunities** — refactors that turn shallow modules into deep ones. The aim is testability and AI-navigability.

## Glossary

Use these terms exactly in every suggestion. Don't drift into "component," "service," "API," or "boundary."

- **Module** — anything with an interface and an implementation (function, class, package, layer, bounded context).
- **Interface** — everything a caller must know: types, invariants, error modes, ordering, config. Not just the type signature.
- **Implementation** — the code inside.
- **Depth** — leverage at the interface: a lot of behaviour behind a small interface. **Deep** = high leverage. **Shallow** = interface nearly as complex as the implementation.
- **Seam** — where an interface lives; a place behaviour can be altered without editing in place.
- **Adapter** — a concrete thing satisfying an interface at a seam.
- **Leverage** — what callers get from depth.
- **Locality** — what maintainers get from depth: change, bugs, knowledge concentrated in one place.

Key principles:

- **Deletion test**: imagine deleting the module. If complexity vanishes, it was a pass-through. If complexity reappears across N callers, it was earning its keep.
- **The interface is the test surface.**
- **One adapter = hypothetical seam. Two adapters = real seam.**

This skill is _informed_ by the project's domain model. Domain language (in `docs/catalog-of-terms/`) names good seams. ADRs (in `docs/architecture-decision-log/`) record decisions this skill should not re-litigate.

## Process

### 1. Explore

Read `docs/catalog-of-terms/` for domain vocabulary and `docs/architecture-decision-log/` for existing decisions first.

Then use the Agent tool with `subagent_type=Explore` to walk the codebase. Explore organically and note where you experience friction:

- Where does understanding one concept require bouncing between many small files?
- Where are modules **shallow** — interface nearly as complex as the implementation?
- Where have pure functions been extracted just for testability, but the real bugs hide in how they're called (no **locality**)?
- Where do tightly-coupled modules leak across bounded context boundaries (e.g. direct calls between Modules instead of Integration Events)?
- Which parts are untested, or hard to test through their current interface?

Apply the **deletion test** to anything suspect: would deleting it concentrate complexity, or just move it?

### 2. Present candidates as an HTML report

Write a self-contained HTML file to `%TEMP%\architecture-review-<timestamp>.html`. Open it with `start <path>` and tell the user the absolute path.

Use Tailwind CDN for layout and Mermaid CDN for diagrams. Each candidate gets a before/after visualisation.

For each candidate, render a card with:

- **Files** — which files/modules are involved
- **Problem** — why the current architecture causes friction
- **Solution** — plain English description of what would change
- **Benefits** — in terms of locality, leverage, and how tests would improve
- **Before / After diagram** — side-by-side, illustrating the shallowness and the deepening
- **Recommendation strength** — `Strong`, `Worth exploring`, or `Speculative`

End with a **Top recommendation** section: which candidate to tackle first and why.

Use domain vocabulary from `docs/catalog-of-terms/` and architecture vocabulary from the Glossary above.

**ADR conflicts**: if a candidate contradicts an existing ADR (e.g. ADR-0015 in-memory event bus, ADR-0018 database-per-module), only surface it when the friction is real enough to warrant reopening. Mark it clearly: _"contradicts ADR-0007 — but worth reopening because…"_

Do NOT propose interfaces yet. After the report, ask: "Which of these would you like to explore?"

### 3. Grilling loop

Once the user picks a candidate, drop into a grilling conversation. Walk the design tree — constraints, dependencies, the shape of the deepened module, what sits behind the seam, what tests survive.

Side effects happen inline as decisions crystallise:

- **Naming a deepened module after a concept not yet in `docs/catalog-of-terms/`?** Create `docs/catalog-of-terms/{Term}/README.md` right there.
- **Sharpening a fuzzy term?** Update the relevant catalog-of-terms entry.
- **User rejects a candidate with a load-bearing reason?** Offer an ADR: _"Want me to record this as an ADR so future architecture reviews don't re-suggest it?"_ Use the next available number in `docs/architecture-decision-log/`. Format:

```markdown
# {NNNN}. {Short title}

Date: YYYY-MM-DD

## Status

Accepted

## Context

{What is the issue that motivates this decision?}

## Decision

{What did we decide?}

## Consequences

{What becomes easier or harder as a result?}
```
