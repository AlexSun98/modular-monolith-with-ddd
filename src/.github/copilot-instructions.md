# GitHub Copilot Instructions for Modular Monolith with DDD

You are an expert .NET developer working on a **Modular Monolith** application.
When generating code or answering questions, you **MUST** follow these architectural rules and project standards.

## 1. Architecture Overview
*   **Style**: Modular Monolith with Domain-Driven Design (DDD).
*   **Communication**: Modules (e.g., Meetings, Administration) are logically isolated. They communicate **asynchronously** via Integration Events (Event Bus). **NEVER** suggest direct method calls between modules.
*   **CQRS**: Strictly separate Write (Command) and Read (Query) models.

## 2. Coding Standards

### Write Model (Commands)
*   **Pattern**: Use DDD Aggregates and Entities.
*   **Persistence**: Entity Framework Core.
*   **Logic**: Business logic belongs in the **Domain** layer (Entities/Aggregates), not in Services or Handlers.
*   **Handlers**: Command Handlers should orchestrate: Load Aggregate -> Call Method -> Save Repository.

### Read Model (Queries)
*   **Pattern**: Raw SQL or Dapper.
*   **Source**: Query against Database Views (e.g., `[schema].v_ViewName`) whenever possible.
*   **No Domain Objects**: Do NOT return Domain Entities from Queries. Return flat DTOs.

### Testing
*   **Unit Tests**: Test Domain logic using NUnit. Do not mock Domain objects.
*   **Integration Tests**: Test Command/Query execution against a real (dockerized) database.

## 3. Detailed Guides
For specific tasks, refer to the detailed documentation in `docs/copilot-instructions/`:

| Guide | Description |
|-------|-------------|
| `01-NEW-FEATURE-GUIDE.md` | Adding Commands, Queries, Handlers, DTOs, and API endpoints |
| `02-NEW-MODULE-GUIDE.md` | Creating new modules with Clean Architecture structure |
| `03-TESTING-GUIDELINES.md` | Unit Tests, Integration Tests, Architecture Tests |
| `04-DATABASE-CHANGES.md` | Migration scripts, SSDT project, schema conventions |
| `05-EVENT-SOURCING-GUIDE.md` | Event Sourcing patterns (Payments module only) |
| `06-CQRS-PATTERNS.md` | Command/Query patterns, DTOs, decorators |
| `07-DOMAIN-MODEL-GUIDE.md` | DDD tactical patterns: Aggregates, Entities, Value Objects, Business Rules |
| `08-INTEGRATION-EVENTS.md` | Cross-module communication via Outbox/Inbox pattern |

## 4. Quick Reference

### Module Structure
```
Modules/{ModuleName}/
├── Application/        # Commands, Queries, Handlers, DTOs
├── Domain/            # Aggregates, Entities, Value Objects, Rules
├── Infrastructure/    # Data Access, Module Startup, Background Jobs
└── IntegrationEvents/ # Events published to other modules (contracts)
```

### Key Patterns
- **Command Handler**: Load Aggregate → Call Domain Method → Save via Repository
- **Query Handler**: Execute raw SQL with Dapper → Return DTO
- **Business Rules**: Encapsulate in `IBusinessRule` implementations
- **Domain Events**: Published internally, converted to Integration Events for cross-module

### Database Conventions
- Each module has its own schema: `[meetings]`, `[administration]`, `[payments]`, `[users]`
- Views for Read Model: `[schema].[v_EntityName]`
- Required per module: `OutboxMessages`, `InboxMessages`, `InternalCommands` tables

## 5. General Behavior
*   Always prefer **Clean Architecture** layering (Domain ← Application ← Infrastructure).
*   When modifying the database, remind the user to create a migration script in `src/Database/CompanyName.MyMeetings.Database/Migrations/`.
*   If unsure about a pattern, check the guides in `docs/copilot-instructions/` or the main `README.md`.
