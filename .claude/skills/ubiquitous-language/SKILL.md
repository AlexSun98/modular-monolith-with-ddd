---
name: ubiquitous-language
description: Extract a DDD-style ubiquitous language glossary from the current conversation, flagging ambiguities and proposing canonical terms. Saves per bounded context to docs/catalog-of-terms/ubiquitous-language/{Module}.md. Use when user wants to define domain terms, build a glossary, harden terminology, create a ubiquitous language, or mentions "domain model" or "DDD".
disable-model-invocation: true
---

# Ubiquitous Language

Extract and formalize domain terminology from the current conversation into the right bounded-context glossary file.

This project keeps its glossaries **per bounded context**:

- `docs/catalog-of-terms/ubiquitous-language/Meetings.md`
- `docs/catalog-of-terms/ubiquitous-language/Administration.md`
- `docs/catalog-of-terms/ubiquitous-language/Payments.md`
- `docs/catalog-of-terms/ubiquitous-language/Registrations.md`
- `docs/catalog-of-terms/ubiquitous-language/UserAccess.md`

Cross-cutting relationships and global ambiguities live in `docs/catalog-of-terms/ubiquitous-language/README.md`. The repo-root `CONTEXT-MAP.md` is the entry point that routes you to the right context. Methodology/pattern definitions (Aggregate, CQRS, Event Sourcing, ...) are one level up in `docs/catalog-of-terms/{Term}/README.md`.

## Process

1. **Scan the conversation** for domain-relevant nouns, verbs, and concepts.
2. **Pick the owning bounded context** for each new term. The owner is the module whose aggregate the term belongs to. If a term is genuinely cross-cutting, add it to the `ubiquitous-language/README.md` overview rather than to a single module file.
3. **Identify problems**:
   - Same word used for different concepts (ambiguity)
   - Different words used for the same concept (synonyms)
   - Vague or overloaded terms
4. **Propose a canonical glossary** with opinionated term choices.
5. **Update the owning module's file**. If a term *also* appears in another module as a local read-shape, add a row to that module's "Terms from other contexts" section -- never duplicate the canonical definition.
6. **Output a summary** inline in the conversation, including which file(s) you wrote to.

## File layout to follow

Each module file already uses these headings -- open one to see the exact table layout, then mirror it:

- `## Aggregates owned here` -- terms whose lifecycle this module owns. Columns: Term | Definition | Aliases to avoid.
- `## Inside-aggregate concepts` -- entities/value objects nested inside a parent aggregate.
- `## Roles` -- domain roles a foreign entity (`Member`, `User`) plays in this context.
- `## Terms from other contexts (local read-shapes)` -- terms appearing here that are *owned* elsewhere. Columns: Term | What it is here | Where the canonical shape lives.
- `## Flagged ambiguities` -- overloaded words, common confusions, words that mean different things in different contexts.

## Rules

- **Be opinionated.** Pick the best term; list others as aliases to avoid.
- **Flag conflicts explicitly** in "Flagged ambiguities".
- **Only domain terms.** Skip class names that are pure plumbing.
- **Keep definitions tight.** One sentence max. What it IS, not what it does.
- **Names mean different things in different contexts.** If you find the same word used canonically in two modules, that is a feature, not a bug -- record both, link them with `[Module](./Module.md)`, and surface the cross-context ambiguity in the overview.

## Re-running

When invoked again in the same conversation:

1. Read the relevant module file(s) and the cross-cutting `README.md`.
2. Incorporate any new terms from subsequent discussion.
3. Update definitions if understanding has evolved.
4. Re-flag any new ambiguities -- especially cross-context ones in the overview.