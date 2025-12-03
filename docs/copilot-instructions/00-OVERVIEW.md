# Project Overview & Architecture

## Summary
This project is a **Modular Monolith** application built with **.NET 8.0**, implementing **Domain-Driven Design (DDD)** principles. It simulates a "Meeting Groups" system (similar to Meetup.com).

## Key Architectural Principles
*   **Modular Monolith**: The application is a single deployment unit but is logically separated into independent modules.
*   **Domain-Driven Design (DDD)**: Rich domain models, encapsulation, and business logic in the domain layer.
*   **CQRS (Command Query Responsibility Segregation)**:
    *   **Write Model**: Uses DDD Aggregates, EF Core, and is responsible for state changes.
    *   **Read Model**: Uses Dapper/Raw SQL and Database Views for fast reads.
*   **Clean Architecture**: Each module follows the Clean Architecture layers (Application, Domain, Infrastructure).
*   **Asynchronous Communication**: Modules communicate **only** via Integration Events using an In-Memory Event Bus. Direct method calls between modules are forbidden.
*   **Eventual Consistency**: Uses the Outbox/Inbox pattern to ensure reliable message delivery between modules.
*   **Event Sourcing**: Used specifically in the **Payments** module.

## Module Structure
Each module (e.g., Meetings, Administration, UserAccess) has the following structure:
*   **Domain**: Entities, Value Objects, Domain Events, Repository Interfaces. (No dependencies on Infrastructure).
*   **Application**: Commands, Queries, Command/Query Handlers, Domain Event Handlers.
*   **Infrastructure**: EF Core DbContext, Repository Implementations, Dapper Queries, External Service Implementations.
*   **IntegrationEvents**: Public contracts (events) shared with other modules.

## Technology Stack
*   **.NET 8.0**
*   **MS SQL Server**
*   **Entity Framework Core** (Write Model)
*   **Dapper** (Read Model)
*   **Autofac** (IoC)
*   **MediatR** (Mediator pattern)
*   **FluentValidation**
*   **Serilog**
*   **NUnit, NSubstitute** (Testing)
