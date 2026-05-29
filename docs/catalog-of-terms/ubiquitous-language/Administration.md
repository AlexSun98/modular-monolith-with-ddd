# Administration — Ubiquitous Language

Owns the **approval lifecycle** for proposed MeetingGroups and the platform-level operator role.

## Aggregates owned here

| Term | Definition | Aliases to avoid |
| --- | --- | --- |
| **MeetingGroupProposal** | The administrative view of a proposed `MeetingGroup`, with an explicit `Accept` / `Reject` lifecycle performed by an `Administrator`. | Application, request |

## Roles

| Term | Definition | Aliases to avoid |
| --- | --- | --- |
| **Administrator** | A privileged operator who reviews and approves/rejects `MeetingGroupProposal`s and manages platform-level rules. | Admin, moderator, Organizer (Organizer is group-level — see [Meetings](./Meetings.md)) |

## Terms from other contexts (local read-shapes)

| Term | What it is here | Where the canonical shape lives |
| --- | --- | --- |
| **Member** | A read-shape projecting a registered person, used to display *who* proposed a group. | [UserAccess](./UserAccess.md) (identity) and [Meetings](./Meetings.md) (domain role) |
| **User** | A read-shape projecting the auth identity of the Administrator acting on a proposal. | [UserAccess](./UserAccess.md) |

## Cross-context flow

`MeetingGroupProposal` is *created* in [Meetings](./Meetings.md) when a `Member` proposes a group. An integration event surfaces it here. The `Administrator`'s `Accept` decision raises a domain event consumed by Meetings, which then **creates the `MeetingGroup`** (see `Aggregate-DDD/README.md` line 37: `CreateBasedOnProposal`).

## Flagged ambiguities

- `Administrator` is **not** an `Organizer`. Organizer is a `MeetingGroupMember` role inside a single group; Administrator is a platform-wide role.
- Approving a proposal is *not* the same as creating the MeetingGroup — Administration emits an event, Meetings does the creation.
