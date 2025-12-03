# ADR-0002: Use Modular Monolith Architecture

## Status

Accepted

## Context

We need to choose an architectural style for the MyMeetings application that balances:

- **Complexity**: The system needs to handle multiple bounded contexts (Meetings, Administration, Payments, User Access, Registrations)
- **Team structure**: Small to medium-sized team working on related features
- **Deployment**: Need for simple deployment and operations
- **Scalability**: Ability to scale in the future if needed
- **Development speed**: Need to deliver features quickly
- **Maintainability**: Code should be organized and maintainable long-term

### Considered Alternatives

1. **Layered Monolith**: Single application with horizontal layers (UI, Business Logic, Data Access)
   - Pros: Simple, well-understood
   - Cons: Tight coupling, difficult to maintain as it grows, no clear module boundaries

2. **Microservices**: Separate deployable services for each bounded context
   - Pros: Independent deployment, technology diversity, clear boundaries
   - Cons: Operational complexity, distributed system challenges, network latency, eventual consistency everywhere, higher infrastructure costs

3. **Modular Monolith**: Single deployable unit with well-defined internal module boundaries
   - Pros: Module isolation, simpler operations, easier development, can evolve to microservices
   - Cons: Requires discipline to maintain boundaries, shared database can be a bottleneck

## Decision

We will use a **Modular Monolith** architecture with the following characteristics:

### Module Structure

Each module represents a bounded context and consists of:
- **Application Layer**: Use cases, handlers, DTOs
- **Domain Layer**: Domain model with DDD tactical patterns
- **Infrastructure Layer**: Database access, external integrations
- **IntegrationEvents**: Public contracts for inter-module communication

### Key Principles

1. **High Encapsulation**: Each module exposes minimal public API
   - Only IntegrationEvents assembly can be referenced by other modules
   - Internal implementation is not accessible to other modules

2. **Independent Composition Roots**: Each module has its own IoC container
   - Modules are initialized independently
   - No shared dependencies between module implementations

3. **Asynchronous Communication Only**: Modules communicate via events
   - No direct method calls between modules
   - Events published to in-memory event bus
   - Ensures loose coupling

4. **Database Per Module**: Each module owns its data
   - Separate database schemas (administration, meetings, payments, users)
   - No shared tables between modules
   - Could be moved to separate databases if needed

5. **Clean Architecture**: Each module follows clean architecture principles
   - Domain at the center, infrastructure at the edges
   - Dependency inversion throughout

### Module Interfaces

API communicates with modules through simple interfaces:
```csharp
public interface IModule
{
    Task<TResult> ExecuteCommandAsync<TResult>(ICommand<TResult> command);
    Task ExecuteCommandAsync(ICommand command);
    Task<TResult> ExecuteQueryAsync<TResult>(IQuery<TResult> query);
}
```

## Consequences

### Positive

- **Simplified Operations**: Single deployment unit, easier monitoring and debugging
- **Lower Operational Cost**: No need for orchestration, service mesh, distributed tracing
- **Development Speed**: Easier refactoring across module boundaries, shared testing infrastructure
- **Clear Boundaries**: Enforced through architecture, not just conventions
- **Evolution Path**: Can extract modules to microservices if needed without rewriting
- **Single Transaction**: ACID transactions available within a module
- **Easier Testing**: Integration tests are simpler without network calls
- **Team Alignment**: Modules can be owned by different teams while sharing codebase

### Negative

- **Requires Discipline**: Module boundaries must be respected through code review
- **Deployment Coupling**: All modules deploy together
- **Resource Sharing**: Cannot scale modules independently
- **Technology Stack**: All modules must use .NET
- **Single Point of Failure**: If the monolith fails, entire system is down

### Mitigation Strategies

- Architecture unit tests to enforce module boundaries
- Asynchronous communication prevents temporal coupling
- Database per schema allows future database separation
- Each module has independent CI/CD testing
- Monitoring and logging at module level for observability

### Future Considerations

If we need to evolve to microservices:
- IntegrationEvents already define contracts
- Each module has independent data store
- Asynchronous communication already in place
- Can extract one module at a time (strangler pattern)

## References

- [Modular Monolith: A Primer by Kamil Grzybek](https://www.kamilgrzybek.com/design/modular-monolith-primer/)
- [MonolithFirst by Martin Fowler](https://martinfowler.com/bliki/MonolithFirst.html)
- [Majestic Modular Monoliths by Axel Fontaine](https://www.youtube.com/watch?v=BOvxJaklcr0)
