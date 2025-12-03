# ADR-0014: Asynchronous Module Integration via Events

## Status

Accepted

## Context

In our modular monolith, modules need to communicate with each other. The way we handle inter-module communication directly impacts:

- **Coupling**: How dependent modules are on each other
- **Autonomy**: Ability of modules to evolve independently
- **Resilience**: System behavior when one module has issues
- **Testability**: Ease of testing modules in isolation
- **Future Evolution**: Ability to extract modules to separate services

### Considered Alternatives

1. **Direct Method Calls**
   - Pros: Simple, synchronous, type-safe
   - Cons: Tight coupling, creates circular dependencies, hard to test, prevents future microservices migration

2. **Shared Database**
   - Pros: Immediate consistency, simple queries
   - Cons: Highest coupling, breaks encapsulation, schema changes affect all modules

3. **Synchronous HTTP/gRPC**
   - Pros: Well understood, good for request/response
   - Cons: Unnecessary overhead in monolith, temporal coupling, cascading failures

4. **Asynchronous Events**
   - Pros: Loose coupling, autonomous modules, natural for DDD, supports future evolution
   - Cons: Eventual consistency, more complex to implement

## Decision

We will use **asynchronous integration via Integration Events** with an in-memory event bus.

### Architecture

```
Module A                    Event Bus                   Module B
   |                            |                            |
   |--[Publish Event]---------->|                            |
   |                            |----[Deliver Event]-------->|
   |                            |                            |
   |                       [Outbox]                     [Inbox]
```

### Key Components

1. **Integration Events**: Public contracts in `IntegrationEvents` assembly
   ```csharp
   public class MeetingGroupProposalAcceptedIntegrationEvent : IntegrationEventBase
   {
       public Guid MeetingGroupProposalId { get; }
       public string Name { get; }
       // ... other properties
   }
   ```

2. **In-Memory Event Bus**: Broker for event delivery within the process
   - Publish/Subscribe pattern
   - Delivers events to all subscribed modules
   - No external dependencies (RabbitMQ, Kafka, etc.)

3. **Outbox Pattern**: Ensures events are published (see ADR-0007)

4. **Inbox Pattern**: Ensures events are processed exactly once (see ADR-0007)

### Communication Rules

1. **Strictly Asynchronous**: No synchronous calls between modules
2. **Event-Driven**: All integration via domain/integration events
3. **No Shared Data**: Each module owns its data completely
4. **Minimal Coupling**: Only coupling is on IntegrationEvents structure
5. **No Direct Dependencies**: Modules cannot reference each other's implementations

### Example Flow

When a Meeting Group Proposal is accepted:

1. **Administration Module** (Producer):
   ```csharp
   // Domain event raised
   meetingGroupProposal.Accept();
   
   // Translated to integration event
   var integrationEvent = new MeetingGroupProposalAcceptedIntegrationEvent(
       meetingGroupProposal.Id,
       meetingGroupProposal.Name,
       // ...
   );
   
   // Added to outbox
   _outbox.Add(integrationEvent);
   ```

2. **Event Bus** (Broker):
   - Background job processes outbox
   - Publishes to event bus
   - Event bus delivers to all subscribers

3. **Meetings Module** (Consumer):
   ```csharp
   // Event delivered to inbox
   // Background job processes inbox
   internal class MeetingGroupProposalAcceptedIntegrationEventHandler : 
       IIntegrationEventHandler<MeetingGroupProposalAcceptedIntegrationEvent>
   {
       public async Task Handle(MeetingGroupProposalAcceptedIntegrationEvent @event)
       {
           await _meetingsModule.ExecuteCommandAsync(
               new CreateMeetingGroupCommand(@event.MeetingGroupProposalId, ...));
       }
   }
   ```

### Integration Events Location

Only the **IntegrationEvents** assembly of each module is public:
- `CompanyName.MyMeetings.Modules.Meetings.IntegrationEvents`
- `CompanyName.MyMeetings.Modules.Administration.IntegrationEvents`
- `CompanyName.MyMeetings.Modules.Payments.IntegrationEvents`
- etc.

Other modules can reference these assemblies but NOT the implementation assemblies.

## Consequences

### Positive

- **Loose Coupling**: Modules only depend on event contracts
- **Module Autonomy**: Each module can evolve independently
- **Testability**: Easy to test modules in isolation
- **Natural DDD**: Aligns with domain events concept
- **Future-Proof**: Easy migration path to message broker or microservices
- **Resilience**: Failure in one module doesn't cascade to others
- **Temporal Decoupling**: Producer doesn't wait for consumers
- **Multiple Consumers**: Same event can trigger multiple actions
- **Audit Trail**: Events provide history of what happened

### Negative

- **Eventual Consistency**: Data may be temporarily out of sync
- **Complexity**: More complex than direct calls
- **Debugging**: Harder to trace flow across modules
- **Ordering**: Event ordering not guaranteed (mitigated by idempotency)
- **Error Handling**: Failed event processing requires retry mechanism

### Mitigation Strategies

1. **Outbox/Inbox Pattern**: Ensures reliable delivery (ADR-0007)
2. **Idempotent Handlers**: Events can be processed multiple times safely
3. **Structured Logging**: Correlation IDs trace events across modules
4. **Integration Tests**: System integration tests verify event flows
5. **Event Versioning**: Events are versioned to support evolution

### Constraints

- Events must be **immutable**
- Events should be **self-contained** (all needed data included)
- Event names should be in **past tense** (MeetingGroupProposalAccepted)
- Events should **not contain behavior**, only data
- Modules must **not fail** if they receive unknown events (forward compatibility)

### When NOT to Use Events

Events are for integration between modules. Within a module:
- Use domain events for in-module communication
- Direct repository calls are fine
- Same transaction boundary applies

## References

- [Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/)
- [Event-Driven Architecture (Martin Fowler)](https://martinfowler.com/articles/201701-event-driven.html)
- Section 3.7 "Modules Integration" in project README
