# Registrations — Ubiquitous Language

Owns the **self-registration funnel** — the process of turning an anonymous visitor into a confirmed `User` in [UserAccess](./UserAccess.md).

## Aggregates owned here

| Term | Definition | Aliases to avoid |
| --- | --- | --- |
| **UserRegistration** | A registration attempt with its own lifecycle: `Waiting for Confirmation` → `Confirmed` or `Expired`. On `Confirm`, raises an integration event that causes a `User` to be created in UserAccess. | Signup, registration request, account request, User (a `User` only exists *after* confirmation) |

## States of UserRegistration

| State | Meaning |
| --- | --- |
| `Waiting for Confirmation` | The user submitted the form; a confirmation email has been sent and is awaiting their click. |
| `Confirmed` | The user followed the confirmation link in time. The integration event that creates the `User` is dispatched here. |
| `Expired` | The confirmation window passed without action. Terminal. |

## Cross-context flow

```
[Registration form] --> UserRegistration(WaitingForConfirmation)
                              |
                              | (user clicks confirm link in time)
                              v
                        UserRegistration(Confirmed)
                              |
                              | UserConfirmedIntegrationEvent
                              v
                        [UserAccess]  --> User created
                              |
                              | UserCreatedIntegrationEvent (or projection)
                              v
                        [Meetings/Payments]  --> local Member / Payer read-shapes
```

See [ADR-0020](../../architecture-decision-log/) for the Outbox/Inbox guarantees that make this chain reliable.

## Flagged ambiguities

- A `UserRegistration` is **not** a `User`. The `User` aggregate only exists in [UserAccess](./UserAccess.md), and only after a successful `Confirm`.
- "Sign up" colloquially means both submitting the form *and* completing confirmation. Inside this context, "sign up" = create `UserRegistration`; "confirm" = the terminal step. Don't mix them.
