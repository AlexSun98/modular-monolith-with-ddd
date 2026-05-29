# Catalog of terms

This catalog has two halves: the **business glossary** (per–bounded-context ubiquitous language) and the **methodology & pattern reference** (DDD building blocks, architectural patterns, testing idioms).

> Need the entry point? Start at the root [`CONTEXT-MAP.md`](../../CONTEXT-MAP.md), which routes you to the right bounded context.

## Ubiquitous Language (business — per bounded context)

Each MyMeetings module owns its own glossary. The same word (e.g. `User`, `Member`) can mean different things in different contexts — that is the point. See the [overview](./ubiquitous-language/README.md) for cross-context relationships and global ambiguities.

- [Meetings](./ubiquitous-language/Meetings.md) — `MeetingGroup`, `Meeting`, `MeetingComment`, `Organizer`, `Host`, `Attendee` (core domain)
- [Administration](./ubiquitous-language/Administration.md) — `MeetingGroupProposal` lifecycle, `Administrator`
- [Payments](./ubiquitous-language/Payments.md) — `Payer`, `Subscription`, `MeetingFee`
- [Registrations](./ubiquitous-language/Registrations.md) — `UserRegistration`
- [UserAccess](./ubiquitous-language/UserAccess.md) — `User`, `Role`, `Permission`

## Methodology & Patterns

DDD building blocks, architectural patterns, and engineering vocabulary used across the codebase. Each entry has a definition, code excerpt, and (where useful) a PlantUML diagram.

- Act/Arrange/Assert
- Actor (Event Storming)
- API
- Application Layer
- [Aggregate (DDD)](Aggregate-DDD/)
- Architecture Decision Record (ADR)
- Architecture Test
- Asynchronous Communication
- Audit Log/Trail
- Authentication
- Authorization
- Bounded Context (DDD)
- C4 Model
- Chain Of Command Pattern
- [Command](Command/)
- Composition Root
- Continuous Integration
- Contract
- CQRS
- Database Change Management
- [Decorator Pattern](Decorator-Pattern/)
- [Dependency Injection](Dependency-Injection/)
- Dependency Inversion
- Diagram as text
- Domain Centric Architecture
- [Domain Event](Domain-Event/)
- Domain Layer
- Domain Model
- Domain Primitive
- Domain Services (DDD)
- Don't Repeat Yourself Principle
- Encapsulation
- [Entity (DDD)](Entity-DDD/)
- [Event](Event/)
- Eventual Consistency
- [Event Driven Architecture](Event-Driven-Architecture/)
- [Event Sourcing](Event-Sourcing/)
- [Event Storming](Event-Storming/)
- Events Stream
- External System (Event Storming)
- Facade Pattern
- Factory Pattern
- Given When Then
- Layered Architecture
- Mediator Pattern
- Message (Messaging)
- Mock
- Modularity
- Module
- Monolith
- Idempotency
- Immediate Consistency
- Immutability
- Infrastructure Layer
- [Integration Event](Integration-Event/)
- Interface
- Interface Segregation Principle
- Inversion Of Control
- Integration Test
- Outbox Pattern (aka Store And Forward)
- Parameter Object Pattern
- Persistence Ignorance
- POCO
- Policy (EventStorming)
- Projection (EventSourcing)
- Pure Function
- Rich Domain Model
- Role-based Access Control
- Query
- Read Model
- Repositories (DDD)
- Single Responsibility Principle
- [Strategy Pattern](Strategy-Pattern/)
- Stub
- Synchronous Communication
- Transaction (Database)
- Ubiquitous Language (DDD)
- Unit Of Work Pattern
- Unit Test
- Write Model
- [ValueObject (DDD)](ValueObject-DDD/)
