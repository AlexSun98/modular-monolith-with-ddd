# Payments — Ubiquitous Language

Owns all money movement. This is the only **event-sourced** module (see [ADR-0019](../../architecture-decision-log/) and [`Event-Sourcing/README.md`](../Event-Sourcing/README.md)).

## Aggregates owned here

| Term | Definition | Aliases to avoid |
| --- | --- | --- |
| **Payer** | A person from the Payments perspective — the local projection of a `User` who can make payments. The aggregate that owns subscription state. | Customer, billing account |
| **Subscription** | A `Payer`'s active payment plan granting access to premium platform features for a period. | Plan, membership |
| **SubscriptionPayment** | A single charge initiated to start or extend a `Subscription`. | Payment, invoice (no invoicing here) |
| **SubscriptionRenewalPayment** | A charge that extends an existing `Subscription` after its current period ends. | Renewal, recurring payment |
| **MeetingFee** | An amount a `Meeting`'s `Host` requires from each attendee; modelled here as the Payments-side projection of a meeting's fee. | Cover charge, ticket |
| **MeetingFeePayment** | A `Payer`'s payment of a `MeetingFee` for a specific meeting. | Ticket purchase |
| **PriceListItem** | A configurable price for a subscription tier or product, used to compute payment amounts. | SKU, product, plan price |

## Value objects of note

| Term | Definition |
| --- | --- |
| **MoneyValue** | A currency-aware amount; the canonical money type across the codebase (also lives in `SharedKernel`). |
| **PriceListItemCategory** | Categorisation of a `PriceListItem` (e.g. *Subscription*, *MeetingFee*). |

## Terms from other contexts (local read-shapes)

| Term | What it is here | Where the canonical shape lives |
| --- | --- | --- |
| **User** | A local read-shape carrying the identifier of the auth identity that became a `Payer`. | [UserAccess](./UserAccess.md) |

## Flagged ambiguities

- A `Payer` and a `User` are **not** the same lifecycle. `User` is created on registration; `Payer` is created lazily when first payment occurs. Don't assume one always exists when the other does.
- A `MeetingFee` here is a **read-shape** — the authoritative fee on a `Meeting` lives in the [Meetings](./Meetings.md) module's `Meeting` aggregate (`eventFee`).
- "Payment" alone is ambiguous — always say `SubscriptionPayment`, `SubscriptionRenewalPayment` or `MeetingFeePayment`.
