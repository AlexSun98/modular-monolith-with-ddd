# Event Sourcing Guide

## 1. Overview
Event Sourcing is used in the **Payments** module. It uses **SQL Stream Store** to persist events.

## 2. Key Components
*   **AggregateRoot**: Base class for event-sourced aggregates.
    *   `Apply(IDomainEvent @event)`: Updates state based on event.
    *   `AddDomainEvent(IDomainEvent @event)`: Adds event to the list of uncommitted changes.
    *   `Load(IEnumerable<IDomainEvent> history)`: Rehydrates aggregate from history.
*   **IAggregateStore**: Interface for saving and loading aggregates.
    *   `Save()`: Appends events to the stream.
    *   `Load<T>(AggregateId<T> id)`: Loads events and reconstructs the aggregate.
*   **Projections**: Convert events into Read Models (SQL tables).

## 3. Implementing an Event-Sourced Aggregate
1.  Inherit from `AggregateRoot`.
2.  Define `Apply` method to handle state changes.
3.  Define public methods (Commands) that:
    *   Validate rules.
    *   Create a Domain Event.
    *   Call `Apply(@event)`.
    *   Call `AddDomainEvent(@event)`.

```csharp
public class SubscriptionPayment : AggregateRoot
{
    // State
    private SubscriptionStatus _status;

    // Command
    public void Pay()
    {
        if (_status == SubscriptionStatus.Paid) throw new Exception("Already paid");
        
        var @event = new SubscriptionPaidEvent(Id, DateTime.UtcNow);
        Apply(@event);
        AddDomainEvent(@event);
    }

    // Event Handler (State Mutation)
    protected override void Apply(IDomainEvent @event)
    {
        if (@event is SubscriptionPaidEvent e)
        {
            _status = SubscriptionStatus.Paid;
        }
    }
}
```

## 4. Projections
1.  Create a Projector class implementing `IProjector`.
2.  Subscribe to the Event Stream (handled by `SubscriptionsManager`).
3.  In the `Project` method, update the Read Model (SQL table) based on the event.

```csharp
public class PaymentProjector : IProjector
{
    public async Task Project(IDomainEvent @event)
    {
        if (@event is SubscriptionPaidEvent e)
        {
            // Update SQL table
            await _connection.ExecuteAsync("UPDATE ... SET Status = 'Paid' ...");
        }
    }
}
```
