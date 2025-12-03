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
For specific tasks, refer to the detailed documentation created in `docs/copilot-instructions/`:
*   **Adding Features**: `docs/copilot-instructions/01-NEW-FEATURE-GUIDE.md`
*   **New Modules**: `docs/copilot-instructions/02-NEW-MODULE-GUIDE.md`
*   **Testing**: `docs/copilot-instructions/03-TESTING-GUIDELINES.md`
*   **Database**: `docs/copilot-instructions/04-DATABASE-CHANGES.md`
*   **Event Sourcing**: `docs/copilot-instructions/05-EVENT-SOURCING-GUIDE.md` (Payments module only)

## 4. General Behavior
*   Always prefer **Clean Architecture** layering (Domain <- Application <- Infrastructure).
*   When modifying the database, remind the user to create a migration script in `src/Database`.
*   If unsure about a pattern, check the `README.md` or the guides listed above.
