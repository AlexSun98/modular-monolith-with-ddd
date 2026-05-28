---
name: grill-with-docs
description: Grilling session that challenges your plan against the existing domain model, sharpens terminology, and updates documentation (catalog-of-terms, ADRs) inline as decisions crystallise. Use when user wants to stress-test a plan against their project's language and documented decisions.
---

<what-to-do>

Interview me relentlessly about every aspect of this plan until we reach a shared understanding. Walk down each branch of the design tree, resolving dependencies between decisions one-by-one. For each question, provide your recommended answer.

Ask the questions one at a time, waiting for feedback on each question before continuing.

If a question can be answered by exploring the codebase, explore the codebase instead.

</what-to-do>

<supporting-info>

## Domain awareness

During codebase exploration, also look for existing documentation:

### File structure in this project

```
/
├── docs/
│   ├── catalog-of-terms/       ← domain glossary (term definitions as README.md per concept)
│   │   ├── Aggregate-DDD/README.md
│   │   ├── Command/README.md
│   │   └── ...
│   └── architecture-decision-log/   ← ADRs (numbered 0001–NNNN)
│       ├── 0001-record-architecture-decisions.md
│       └── ...
└── src/
```

Terms are defined in `docs/catalog-of-terms/{Term}/README.md`. ADRs are in `docs/architecture-decision-log/` numbered sequentially.

## During the session

### Challenge against the glossary

When the user uses a term that conflicts with existing language in `docs/catalog-of-terms/`, call it out immediately. "Your catalog defines 'cancellation' as X, but you seem to mean Y — which is it?"

### Sharpen fuzzy language

When the user uses vague or overloaded terms, propose a precise canonical term. "You're saying 'account' — do you mean the Customer or the User? Those are different things in this domain."

### Discuss concrete scenarios

Stress-test domain relationships with specific scenarios. Invent edge cases that force the user to be precise about the boundaries between bounded contexts.

### Cross-reference with code

When the user states how something works, check whether the code agrees. If you find a contradiction, surface it: "Your code cancels entire Orders, but you just said partial cancellation is possible — which is right?"

### Update catalog-of-terms inline

When a new term is resolved, add it to `docs/catalog-of-terms/{Term}/README.md`. Create the folder lazily — only when you have something to write. Keep term definitions free of implementation details: domain concepts and their relationships only, no class names or infrastructure.

### Offer ADRs sparingly

Only offer to create an ADR when **all three** are true:

1. **Hard to reverse** — changing this decision later has meaningful cost
2. **Surprising without context** — a future reader will wonder "why did they do it this way?"
3. **The result of a real trade-off** — genuine alternatives existed and one was chosen for specific reasons

If any of the three is missing, skip the ADR.

**ADR format** for this project (continue the numbered sequence in `docs/architecture-decision-log/`):

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

</supporting-info>
