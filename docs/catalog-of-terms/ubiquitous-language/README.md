# Ubiquitous Language

Per–bounded-context glossaries for MyMeetings. Each file owns the canonical definition of terms inside *its* bounded context. The **same word can legitimately mean different things across contexts** — that is the central DDD principle this layout exists to preserve.

> If you came here from a top-level pointer (`CONTEXT-MAP.md`, `CLAUDE.md`), pick the module relevant to your task and read that file first.

## Files

| Module / Bounded context | File | Core aggregates / concepts |
| --- | --- | --- |
| **Meetings** (core domain) | [`Meetings.md`](./Meetings.md) | `MeetingGroup`, `Meeting`, `MeetingGroupProposal`, `MeetingComment` |
| **Administration** | [`Administration.md`](./Administration.md) | `MeetingGroupProposal` (approval lifecycle), `Administrator` |
| **Payments** | [`Payments.md`](./Payments.md) | `Payer`, `Subscription`, `MeetingFee`, `MeetingFeePayment` |
| **Registrations** | [`Registrations.md`](./Registrations.md) | `UserRegistration` |
| **UserAccess** | [`UserAccess.md`](./UserAccess.md) | `User`, RBAC role/permission concepts |

## Cross-context relationships

- A **MeetingGroupProposal** is created in **Meetings**, approved in **Administration**, and on approval results in a new **MeetingGroup** in **Meetings**.
- A **UserRegistration** in **Registrations**, when confirmed, results in a new **User** in **UserAccess** (and that User is then surfaced as a **Member** in **Meetings**, a **Payer** in **Payments**, etc.).
- Modules communicate **only via Integration Events**. A term appearing in two modules is always a **local read-shape**, never a shared domain object — see ADR-0014 and ADR-0018.

## Global ambiguities — pick the right context first

| Word | Inside this context, it means… | Reach for the canonical owner when… |
| --- | --- | --- |
| `User` | UserAccess: an auth identity. Payments/Administration: a local read-shape of that identity. | The conversation is about authentication, roles, or RBAC → UserAccess. |
| `Member` | Meetings/Administration: a person who can join MeetingGroups, modelled as a local read-shape. | The conversation is about who someone *is* as a domain participant → Meetings owns the canonical shape. |
| `Organizer` | Meetings: the `Member` who created and administers a `MeetingGroup` (a role, not a separate entity). | Always Meetings. |
| `Administrator` | Administration: a privileged operator who approves proposals. | Always Administration. Not the same as Organizer. |
| `Payer` | Payments: the local read-shape of a person who pays. | Always Payments. |

## Adding terms

When you add or rename a domain concept, update the file for the module that **owns the aggregate**. If the term appears in another module as a local read-shape, add a row to that module's "Terms from other contexts" section — never duplicate the canonical definition.

The `/ubiquitous-language` skill writes here; the methodology/pattern catalogue (`Aggregate-DDD/`, `Command/`, etc.) is one level up in [`../`](../).
