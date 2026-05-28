# UserAccess — Ubiquitous Language

Owns **authentication identity and authorization**. Every other module references a `User` here via its identifier; nobody else owns the auth shape.

## Aggregates owned here

| Term | Definition | Aliases to avoid |
| --- | --- | --- |
| **User** | The canonical authentication identity — a confirmed account with a login, password hash and active/inactive state. Created in response to a `UserConfirmed` event from [Registrations](./Registrations.md). | Account (we don't say "account"), Member (Member is a domain role — see [Meetings](./Meetings.md)), Payer (see [Payments](./Payments.md)) |

## Authorization concepts

| Term | Definition | Aliases to avoid |
| --- | --- | --- |
| **Role** | A named set of `Permission`s a `User` can hold (RBAC). | Group, profile |
| **Permission** | An atomic capability granted by a `Role`. Code-level enforcement uses permission strings. | Privilege, claim, scope |
| **Role-based Access Control (RBAC)** | The authorization model used throughout the platform; see [`../`](../) for the methodology entry. | ABAC, ACL |

## Terms used elsewhere — *not* canonical here

| Term | Where it lives | Why it's not here |
| --- | --- | --- |
| **Member** | [Meetings](./Meetings.md), [Administration](./Administration.md) | "Member" is a *domain role* a `User` plays inside a `MeetingGroup`. It's not an identity concept. |
| **Payer** | [Payments](./Payments.md) | "Payer" is the billing-domain projection of a `User`. UserAccess doesn't know about money. |
| **Organizer**, **Administrator**, **Host** | [Meetings](./Meetings.md), [Administration](./Administration.md) | These are domain-specific roles, not RBAC roles. (A platform `Role` in UserAccess can grant rights *to perform Administrator actions*, but the term "Administrator" itself belongs to Administration.) |

## Flagged ambiguities

- `User` is the most-overloaded word in the codebase. Inside this module it means *the auth aggregate*. In every other module, a property called `User…` is a **local read-shape** carrying only the identifier and whatever fields the local context needs (display name, etc.). Treat foreign `User` shapes as immutable inputs that arrive via integration events.
- An RBAC `Role` is **not** a `MeetingGroupMemberRole`. The former is a platform permission set; the latter is `Organizer` vs `Member` *inside one MeetingGroup*. Same English word, different concept — keep the qualifier.
