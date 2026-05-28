# Context Map — MyMeetings

This repository is a **modular monolith** with five bounded contexts. Each context owns its ubiquitous language file. Skills (`/grill-with-docs`, `/diagnose`, `/tdd`, `/ubiquitous-language`) read this map to find the right context for a given topic.

> Looking for **methodology and DDD pattern definitions** (Aggregate, CQRS, Event Sourcing, etc.)? See [`docs/catalog-of-terms/`](./docs/catalog-of-terms/) — same root folder, different sibling files.

## Contexts

- [Meetings](./docs/catalog-of-terms/ubiquitous-language/Meetings.md) — core domain: `MeetingGroup`, `Meeting`, `MeetingComment`. Owns the social/scheduling lifecycle.
- [Administration](./docs/catalog-of-terms/ubiquitous-language/Administration.md) — approval lifecycle of `MeetingGroupProposal`; the `Administrator` role.
- [Payments](./docs/catalog-of-terms/ubiquitous-language/Payments.md) — `Payer`, `Subscription`, `MeetingFee`. Event-sourced.
- [Registrations](./docs/catalog-of-terms/ubiquitous-language/Registrations.md) — `UserRegistration`: the self-signup funnel.
- [UserAccess](./docs/catalog-of-terms/ubiquitous-language/UserAccess.md) — `User` (auth identity), RBAC `Role` / `Permission`.

The cross-cutting overview (relationships, global ambiguities) lives at [`docs/catalog-of-terms/ubiquitous-language/README.md`](./docs/catalog-of-terms/ubiquitous-language/README.md).

## Relationships

```
Registrations  ─[ UserConfirmedIntegrationEvent ]─►  UserAccess
                                                          │
                                                          ├─►  Meetings        (Member read-shape)
                                                          ├─►  Administration  (User / Member read-shapes)
                                                          └─►  Payments        (Payer aggregate)

Meetings  ─[ MeetingGroupProposalCreatedIntegrationEvent ]─►  Administration
Administration  ─[ MeetingGroupProposalAcceptedIntegrationEvent ]─►  Meetings   (creates MeetingGroup)
```

Modules communicate **only via Integration Events** — never direct calls. See [ADR-0014](./docs/architecture-decision-log/) and [ADR-0018](./docs/architecture-decision-log/).

## Which context for a given topic?

| Topic / question | Read first |
| --- | --- |
| Sign-up / email confirmation / account creation | [Registrations](./docs/catalog-of-terms/ubiquitous-language/Registrations.md), then [UserAccess](./docs/catalog-of-terms/ubiquitous-language/UserAccess.md) |
| Login, password, roles, permissions | [UserAccess](./docs/catalog-of-terms/ubiquitous-language/UserAccess.md) |
| Creating / joining groups, RSVP, hosts, attendees | [Meetings](./docs/catalog-of-terms/ubiquitous-language/Meetings.md) |
| Approving a new group | [Administration](./docs/catalog-of-terms/ubiquitous-language/Administration.md) (then [Meetings](./docs/catalog-of-terms/ubiquitous-language/Meetings.md) for the resulting group) |
| Subscriptions, fees, charges, money | [Payments](./docs/catalog-of-terms/ubiquitous-language/Payments.md) |
| A property/column called `User…` in any module *other than* UserAccess | The owning module — it's a local read-shape, not the auth identity |
