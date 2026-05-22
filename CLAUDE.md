# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# MyMeetings - Modular Monolith with DDD

## Claude Code Setup

Run once after cloning:
```
/plugin install superpowers@claude-plugins-official
```

This enables shared skills (brainstorming, TDD, debugging, code review, etc.). OpenSpec skills and commands load automatically from `.claude/skills/` and `.claude/commands/`.

## Build & Test Commands

```bash
# Build entire solution
dotnet build src/CompanyName.MyMeetings.sln

# Run the API (Swagger UI at http://localhost:5000/swagger)
dotnet run --project src/API/CompanyName.MyMeetings.API

# Run unit tests for a specific module
dotnet test src/Modules/Meetings/Tests/UnitTests

# Run a single test by name
dotnet test src/Modules/Meetings/Tests/UnitTests --filter "FullyQualifiedName~CreateMeeting"

# Run module-level architecture tests
dotnet test src/Modules/Meetings/Tests/ArchTests

# Run cross-module architecture tests
dotnet test src/Tests/ArchTests

# Run integration tests (requires env var — see below)
dotnet test src/Modules/Meetings/Tests/IntegrationTests

# Run system integration tests
dotnet test src/Tests/IntegrationTests
```

**Integration test prerequisite** — set this environment variable at machine scope before running integration or system tests:
```
ASPNETCORE_MyMeetings_IntegrationTests_ConnectionString=Server=.;Database=MyMeetings;TrustServerCertificate=True;Trusted_Connection=True;
```

The API itself uses the same connection string by default (see `src/API/CompanyName.MyMeetings.API/appsettings.json`).

## Project Overview

This is a **Modular Monolith** .NET 10 application implementing **Domain-Driven Design** for a Meeting Groups domain (Meetup.com-like system). Solution: `src/CompanyName.MyMeetings.sln`.

## Architecture Rules (MUST follow)

### Module Boundaries
- **5 modules**: Meetings, Administration, Payments, Registrations, UserAccess
- Modules communicate **only via Integration Events** through the In-Memory Event Bus
- **No direct method calls** between modules - this is the most critical rule
- Each module has its own **Autofac composition root** (IoC container)
- Each module owns its data in a **separate database schema**
- Only `IntegrationEvents` assemblies can be referenced by other modules

### Module Layer Structure
Each module has 4 layers with strict dependency rules:
```
Domain       <- No dependencies on other layers (pure POCO)
Application  <- Depends on Domain only
Infrastructure <- Depends on Application and Domain
IntegrationEvents <- Standalone, shared contracts
```

### CQRS Pattern
- **Commands (Write Model)**: Use DDD Aggregates via EF Core repositories
  - Handler: `ICommandHandler<TCommand>` or `ICommandHandler<TCommand, TResult>`
  - Business logic lives in Domain Model (Aggregates), NOT in handlers
  - Use `CheckRule(IBusinessRule)` for invariant validation
  - Raise Domain Events for side effects
- **Queries (Read Model)**: Use raw SQL via Dapper against database views
  - Handler: `IQueryHandler<TQuery, TResult>`
  - Inject `ISqlConnectionFactory`, query `[schema].[v_ViewName]`
  - Return DTOs, never domain objects
- **Validation**: FluentValidation `AbstractValidator<TCommand>` in Application layer

### Domain Model Principles
1. All members `private` by default, then `internal`, `public` only at edges
2. **Persistence Ignorance** - no infrastructure dependencies, all POCOs
3. **Rich behavior** - all business logic in Domain, no leaks to Application
4. **Low Primitive Obsession** - use Value Objects to group primitives
5. **Ubiquitous Language** - name classes/methods in business terms
6. **Testable Design** - design for unit testing

### Cross-Cutting Concerns
Command handlers are decorated with 3 decorators (in order):
1. **LoggingCommandHandlerDecorator** - logs execution and arguments
2. **ValidationCommandHandlerDecorator** - validates via FluentValidation
3. **UnitOfWorkCommandHandlerDecorator** - commits transaction, dispatches domain events

### Integration Patterns
- **Outbox/Inbox Pattern** for reliable event delivery between modules
- **Internal Commands** for async processing within a module (inherit `InternalCommandBase`)
- **Quartz.NET** for background job processing
- **Event Sourcing** in the Payments module only (SQL Stream Store)

## Key File Locations

| What | Where |
|------|-------|
| API Controllers | `src/API/CompanyName.MyMeetings.API/Modules/` |
| Module code | `src/Modules/{Module}/{Layer}/` |
| Building blocks | `src/BuildingBlocks/` |
| Database scripts | `src/Database/` |
| Module tests | `src/Modules/{Module}/Tests/` |
| Module arch tests | `src/Modules/{Module}/Tests/ArchTests/` |
| Cross-module arch tests | `src/Tests/ArchTests/` |
| System integration tests | `src/Tests/IntegrationTests/` |
| Performance tests | `src/Tests/PerformanceTests/` |
| ADRs | `docs/architecture-decision-log/` |
| Copilot guides | `docs/copilot-instructions/` |

## Technology Stack

- .NET 10, C#
- MS SQL Server (separate schemas per module)
- EF Core (Write Model), Dapper (Read Model)
- Autofac (IoC per module), MediatR (mediator)
- FluentValidation, Serilog
- NUnit, NSubstitute (testing), NetArchTest (arch tests)
- Quartz.NET (background jobs)
- SQL Stream Store (event sourcing - Payments only)

## Development Workflow

### Adding a New Feature
Follow `docs/copilot-instructions/01-NEW-FEATURE-GUIDE.md`:
1. Define Command/Query in Application layer
2. Implement handler (inject repos for commands, ISqlConnectionFactory for queries)
3. Add domain logic in Aggregate (commands only)
4. Add FluentValidation validator (commands only)
5. Add API controller endpoint
6. Write unit tests (domain) and integration tests (handler)

### Adding a New Module
Follow `docs/copilot-instructions/02-NEW-MODULE-GUIDE.md`:
1. Create folder structure under `src/Modules/{Name}/`
2. Create Domain, Application, Infrastructure, IntegrationEvents projects
3. Define `I{Name}Module` interface
4. Implement Autofac composition root in Infrastructure
5. Create database schema and DbContext
6. Register in API Startup

### Database Changes
Follow `docs/copilot-instructions/04-DATABASE-CHANGES.md`:
- Scripts in `src/Database/CompanyName.MyMeetings.Database/[schema]/`
- Naming: `[Order]_[Description].sql` (e.g., `0001_CreateTable.sql`)
- Never modify deployed scripts - create new ones
- Use correct module schema

### Testing
Follow `docs/copilot-instructions/03-TESTING-GUIDELINES.md`:
- **Unit tests**: Domain model (Aggregates, Entities, Value Objects). No mocking domain objects. AAA style.
- **Integration tests**: Full module stack with real SQL Server. Mock only external deps.
- **Architecture tests**: NetArchTest to enforce dependency rules.
- **System integration tests**: End-to-end with polling for eventual consistency.

## Code Style

- `internal` access modifier for handlers, decorators, and implementation details
- `public` only for module interfaces, commands, queries, DTOs, integration events
- One handler per file, named `{Action}{Entity}CommandHandler` or `{Action}{Entity}QueryHandler`
- Commands: `{Action}{Entity}Command` (e.g., `CreateMeetingCommand`)
- Queries: `Get{Entity}Query` or `GetAll{Entities}Query`
- SQL views: `[schema].[v_{ViewName}]`
- Integration events: `{Entity}{Action}IntegrationEvent`

## Spec-Driven Development

For non-trivial features, follow `docs/spec-driven/README.md`:
1. **Requirements** (requirements.md) - User stories with EARS acceptance criteria
2. **Design** (design.md) - Architecture, components, correctness properties
3. **Tasks** (tasks.md) - Epics broken into testable tasks

## Architecture Decision Records

All major decisions are documented in `docs/architecture-decision-log/`. Key ADRs:
- ADR-0002: Modular Monolith architecture
- ADR-0007: CQRS pattern
- ADR-0010: Clean Architecture for writes
- ADR-0011: Rich domain models
- ADR-0012: DDD tactical patterns
- ADR-0014: Event-driven communication between modules
- ADR-0016: IoC container per module
- ADR-0018: Database per module (schema)
- ADR-0019: Event sourcing for Payments
- ADR-0020: Outbox/Inbox pattern
