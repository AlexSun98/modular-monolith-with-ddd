# Meetings — Ubiquitous Language

The **core domain**. Owns the MeetingGroup and Meeting aggregates and everything that happens inside the social/scheduling lifecycle.

## Aggregates owned here

| Term | Definition | Aliases to avoid |
| --- | --- | --- |
| **MeetingGroup** | An organizer-owned group that hosts recurring meetings; the Meetup.com analogue of a "community". | Group, community |
| **Meeting** | A scheduled gathering inside a `MeetingGroup` with hosts, attendees, RSVP rules and an optional fee. | Event, session |
| **MeetingGroupProposal** | A request to create a `MeetingGroup`, awaiting Administrator approval; the proposal *originates* here but its acceptance lifecycle lives in [Administration](./Administration.md). | Draft group, pending group |
| **MeetingComment** | A comment posted on a `Meeting` (or reply to another comment) by a `Member`. | Note, post |
| **MeetingCommentingConfiguration** | Per-`MeetingGroup` policy controlling whether commenting is enabled and who may post. | Comment settings |

## Inside-aggregate concepts

| Term | Definition | Aliases to avoid |
| --- | --- | --- |
| **Attendee** | A `Member` who has signed up to attend a specific `Meeting`. Part of the `Meeting` aggregate, not its own root. | Participant, registrant |
| **MeetingWaitlistMember** | A `Member` placed on a waitlist when a `Meeting` is at capacity. | Queued member |
| **MeetingNotAttendee** | A `Member` who explicitly declined the `Meeting`. | RSVP-no |
| **Host** | A `Member` designated to lead/host a `Meeting`; must be a member of the parent `MeetingGroup`. | Organizer (Organizer is a group-level role, not a meeting-level role) |
| **MeetingLimits** | Value object on `Meeting`: attendee cap and guest cap. | Capacity |
| **MeetingTerm**, **Term** | Value objects: a date range or an RSVP window. | Date range |

## Roles

| Term | Definition | Aliases to avoid |
| --- | --- | --- |
| **Organizer** | The `Member` who created and administers a `MeetingGroup`. A *role on `MeetingGroupMember`*, not a separate aggregate. | Admin, owner, Administrator (Administrator is a platform role — see [Administration](./Administration.md)) |
| **MeetingGroupMember** | The link between a `Member` and a `MeetingGroup`, carrying the member's role inside that group (`Organizer` or `Member`). | Membership |

## Terms from other contexts (local read-shapes)

| Term | What it is here | Where the canonical shape lives |
| --- | --- | --- |
| **Member** | A read-shape projecting a registered person into the Meetings context, identified by `MemberId`. Created/updated via integration events from UserAccess. | [UserAccess](./UserAccess.md) (auth identity) and [Registrations](./Registrations.md) (lifecycle) |

## Flagged ambiguities

- `Organizer` (group-level) is **not** the same as `Administrator` (platform-level — Administration module). An Organizer can create meetings but cannot approve `MeetingGroupProposal`s.
- `Host` is a role at the `Meeting` level; `Organizer` is a role at the `MeetingGroup` level. Don't conflate.
- `MeetingGroupProposal` lives in this module's database schema but its **business lifecycle** (approve/reject) is owned by [Administration](./Administration.md). When in doubt about its state machine, read Administration's file.
