# Domain Context — MyMeetings

This file is the canonical domain glossary for AI agent skills. Detailed per-concept definitions live in `docs/catalog-of-terms/{Term}/README.md`. Architecture decisions are in `docs/architecture-decision-log/`.

## Meeting lifecycle

| Term                  | Definition                                                                   | Aliases to avoid           |
| --------------------- | ---------------------------------------------------------------------------- | -------------------------- |
| **MeetingGroup**      | An organizer-owned group that hosts recurring meetings (Meetup.com analogue) | Group, community           |
| **Meeting**           | A scheduled gathering within a MeetingGroup                                  | Event, session             |
| **MeetingGroupProposal** | A request to create a MeetingGroup, pending Administrator approval        | Draft group                |
| **Attendee**          | A Member who has signed up to attend a specific Meeting                      | Participant, registrant    |
| **WaitlistMember**    | A Member on the waiting list when a Meeting is at capacity                   | Queued member              |

## People

| Term              | Definition                                                            | Aliases to avoid       |
| ----------------- | --------------------------------------------------------------------- | ---------------------- |
| **Member**        | A registered person who can join MeetingGroups and attend Meetings    | User, participant      |
| **Organizer**     | The Member who created and administers a MeetingGroup                 | Admin, owner           |
| **Administrator** | A privileged operator who approves MeetingGroup proposals             | SuperAdmin, moderator  |
| **User**          | An authentication identity in the UserAccess module (not a domain role) | Member, person       |

## Payments

| Term              | Definition                                                            | Aliases to avoid       |
| ----------------- | --------------------------------------------------------------------- | ---------------------- |
| **Subscription**  | A Member's active payment plan granting access to premium features   | Plan, membership       |
| **PaymentMethod** | A stored payment instrument (card, etc.) belonging to a Member        | Card, billing info     |

## Modules (bounded contexts)

| Module            | Responsibility                                                        |
| ----------------- | --------------------------------------------------------------------- |
| **Meetings**      | Core domain — MeetingGroups, Meetings, Attendees                      |
| **Administration**| Approves MeetingGroup proposals, manages platform rules               |
| **Payments**      | Subscriptions, payment events (event-sourced)                         |
| **Registrations** | Member self-registration flow                                         |
| **UserAccess**    | Authentication, authorization, RBAC                                   |

## Relationships

- A **MeetingGroup** is proposed by a **Member** and approved by an **Administrator**
- A **MeetingGroup** contains zero or more **Meetings**
- A **Member** can be an **Attendee** of zero or more **Meetings**
- Modules communicate only via **Integration Events** — never direct calls

## Flagged ambiguities

- "user" is often used loosely — prefer **Member** for domain participants and **User** strictly for authentication identity (UserAccess module only).
- "group" alone is ambiguous — always say **MeetingGroup**.
