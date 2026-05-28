---
name: ubiquitous-language
description: Extract a DDD-style ubiquitous language glossary from the current conversation, flagging ambiguities and proposing canonical terms. Saves to CONTEXT.md. Use when user wants to define domain terms, build a glossary, harden terminology, create a ubiquitous language, or mentions "domain model" or "DDD".
disable-model-invocation: true
---

# Ubiquitous Language

Extract and formalize domain terminology from the current conversation into a consistent glossary, saved to `CONTEXT.md` in the project root.

This project already has per-concept definitions in `docs/catalog-of-terms/{Term}/README.md`. When adding new terms, also create the corresponding folder there.

## Process

1. **Scan the conversation** for domain-relevant nouns, verbs, and concepts
2. **Identify problems**:
   - Same word used for different concepts (ambiguity)
   - Different words used for the same concept (synonyms)
   - Vague or overloaded terms
3. **Propose a canonical glossary** with opinionated term choices
4. **Update `CONTEXT.md`** in the working directory using the format below
5. **Output a summary** inline in the conversation

## Output Format

Write or update `CONTEXT.md` with this structure:

```md
# Domain Context — MyMeetings

## Meeting lifecycle

| Term              | Definition                                                  | Aliases to avoid         |
| ----------------- | ----------------------------------------------------------- | ------------------------ |
| **MeetingGroup**  | An organizer-owned group that hosts recurring meetings      | Group, community         |
| **Meeting**       | A scheduled gathering within a MeetingGroup                 | Event, session           |

## People

| Term         | Definition                                              | Aliases to avoid       |
| ------------ | ------------------------------------------------------- | ---------------------- |
| **Member**   | A person who has joined a MeetingGroup                  | User, participant      |
| **Organizer**| The Member who created and administers a MeetingGroup   | Admin, owner           |

## Relationships

- A **MeetingGroup** contains zero or more **Meetings**
- A **Member** can attend zero or more **Meetings**

## Flagged ambiguities

- "user" is used inconsistently — prefer **Member** for domain participants and reserve **User** for authentication identity (UserAccess module).
```

## Rules

- **Be opinionated.** Pick the best term; list others as aliases to avoid.
- **Flag conflicts explicitly.** Call out ambiguous usage in the "Flagged ambiguities" section.
- **Only domain terms.** Skip module/class names and infrastructure concepts.
- **Keep definitions tight.** One sentence max. What it IS, not what it does.
- **Show relationships.** Use bold term names with cardinality where obvious.
- **Group into cohesive clusters** — by subdomain, lifecycle, or actor. One table is fine if all terms belong to a single domain.

## Re-running

When invoked again in the same conversation:

1. Read the existing `CONTEXT.md`
2. Incorporate any new terms from subsequent discussion
3. Update definitions if understanding has evolved
4. Re-flag any new ambiguities
