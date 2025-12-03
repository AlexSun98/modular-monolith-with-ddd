# ADR-0019: Event Sourcing for Payments Module

## Status

Accepted

## Context

The Payments module handles critical financial transactions including:
- Subscription purchases and renewals
- Meeting fee payments
- Price list management

For financial systems, we need:

1. **Complete Audit Trail**: Full history of all state changes
2. **Regulatory Compliance**: Ability to prove what happened and when
3. **Temporal Queries**: Answer questions like "What was the subscription status on date X?"
4. **Debugging**: Replay events to reproduce issues
5. **Accountability**: Know who did what and when
6. **Immutability**: Financial records should never be deleted or updated

### Considered Alternatives

1. **Traditional CRUD with Audit Log**
   - Current state in main tables + separate audit table
   - Pros: Simple, well-understood, good tooling
   - Cons: Audit is separate concern, can get out of sync, not source of truth

2. **Event Sourcing**
   - Store all changes as events, rebuild state from events
   - Pros: Complete history, temporal queries, natural audit trail
   - Cons: More complex, different mindset, harder queries

3. **Hybrid Approach**
   - Event sourcing for some aggregates, CRUD for others
   - Pros: Use event sourcing where it adds value
   - Cons: Two different patterns in same module

## Decision

We will implement **Event Sourcing** exclusively in the **Payments module** using SQL Stream Store.

### Why Only Payments Module?

Event sourcing adds complexity, so we apply it only where benefits outweigh costs:

✅ **Payments Module** (Event Sourced):
- Financial data requires complete audit trail
- Regulatory requirements for immutability
- Need to answer temporal queries
- State transitions are complex (payment lifecycle)
- Benefits justify complexity

❌ **Other Modules** (Traditional):
- Meetings, Administration, Users, Registrations use traditional approach
- Audit via domain events and outbox
- Simpler requirements don't justify ES complexity

### Architecture

#### Event Store

Using **SQL Stream Store** library:

```csharp
public class SqlStreamAggregateStore : IAggregateStore
{
    private readonly IStreamStore _streamStore;
    
    public SqlStreamAggregateStore(ISqlConnectionFactory sqlConnectionFactory)
    {
        _streamStore = new MsSqlStreamStore(
            new MsSqlStreamStoreSettings(sqlConnectionFactory.GetConnectionString())
            {
                Schema = DatabaseSchema.Name
            });
    }
    
    public async Task Save()
    {
        foreach (var aggregateToSave in _aggregatesToSave)
        {
            await _streamStore.AppendToStream(
                GetStreamId(aggregateToSave.Aggregate),
                aggregateToSave.Aggregate.Version,
                aggregateToSave.Messages.ToArray());
        }
    }
    
    public async Task<T> Load<T>(AggregateId<T> aggregateId)
    {
        // Read all events from stream
        // Reconstruct aggregate by applying events
    }
}
```

#### Aggregate Root Pattern

Aggregates are restored from events:

```csharp
public class SubscriptionPayment : AggregateRoot
{
    private PayerId _payerId;
    private SubscriptionPeriod _subscriptionPeriod;
    private SubscriptionPaymentStatus _status;
    private MoneyValue _value;
    
    public static SubscriptionPayment Buy(
        PayerId payerId,
        SubscriptionPeriod period,
        string countryCode,
        MoneyValue priceOffer,
        PriceList priceList)
    {
        // Validate
        CheckRule(new PriceOfferMustMatchPriceInPriceListRule(priceOffer, priceInPriceList));
        
        // Create event
        var subscriptionPayment = new SubscriptionPayment();
        var created = new SubscriptionPaymentCreatedDomainEvent(
            Guid.NewGuid(),
            payerId.Value,
            period.Code,
            countryCode,
            SubscriptionPaymentStatus.WaitingForPayment.Code,
            priceOffer.Value,
            priceOffer.Currency);
        
        // Apply to build state
        subscriptionPayment.Apply(created);
        subscriptionPayment.AddDomainEvent(created);
        
        return subscriptionPayment;
    }
    
    protected override void Apply(IDomainEvent @event)
    {
        this.When((dynamic)@event);  // Double dispatch
    }
    
    private void When(SubscriptionPaymentCreatedDomainEvent @event)
    {
        this.Id = @event.SubscriptionPaymentId;
        _payerId = new PayerId(@event.PayerId);
        _subscriptionPeriod = SubscriptionPeriod.Of(@event.SubscriptionPeriodCode);
        _status = SubscriptionPaymentStatus.Of(@event.Status);
        _value = MoneyValue.Of(@event.Value, @event.Currency);
    }
}
```

#### Read Models via Projections

Events are projected to read models:

```csharp
internal class SubscriptionDetailsProjector : IProjector
{
    public async Task Project(IDomainEvent @event)
    {
        await When((dynamic)@event);
    }
    
    private async Task When(SubscriptionCreatedDomainEvent subscriptionCreated)
    {
        await _connection.ExecuteScalarAsync(
            "INSERT INTO payments.SubscriptionDetails " +
            "([Id], [Period], [Status], [CountryCode], [ExpirationDate]) " +
            "VALUES (@SubscriptionId, @Period, @Status, @CountryCode, @ExpirationDate)",
            new
            {
                subscriptionCreated.SubscriptionId,
                period,
                subscriptionCreated.Status,
                subscriptionCreated.CountryCode,
                subscriptionCreated.ExpirationDate
            });
    }
    
    private async Task When(SubscriptionRenewedDomainEvent subscriptionRenewed)
    {
        await _connection.ExecuteScalarAsync(
            "UPDATE payments.SubscriptionDetails " +
            "SET [Status] = @Status, [ExpirationDate] = @ExpirationDate " +
            "WHERE [Id] = @SubscriptionId",
            new { subscriptionRenewed.SubscriptionId, subscriptionRenewed.Status, subscriptionRenewed.ExpirationDate });
    }
}
```

#### Subscription Management

```csharp
public class SubscriptionsManager
{
    public void Start()
    {
        long? actualPosition = _checkpointStore.GetCheckpoint(SubscriptionCode.All);
        _streamStore.SubscribeToAll(actualPosition, StreamMessageReceived);
    }
    
    private static async Task StreamMessageReceived(
        IAllStreamSubscription subscription, 
        StreamMessage streamMessage, 
        CancellationToken cancellationToken)
    {
        var domainEvent = DeserializeEvent(streamMessage);
        
        var projectors = scope.Resolve<IList<IProjector>>();
        
        foreach (var projector in projectors)
        {
            await projector.Project(domainEvent);
        }
        
        await _checkpointStore.StoreCheckpoint(SubscriptionCode.All, streamMessage.Position);
    }
}
```

### Command Handling Flow

1. Load aggregate from event stream
2. Execute domain logic (creates new events)
3. Append events to stream
4. Events are projected to read models asynchronously

```csharp
public async Task<Guid> Handle(BuySubscriptionCommand command, CancellationToken cancellationToken)
{
    var priceList = await PriceListProvider.GetPriceList(_sqlConnectionFactory.GetOpenConnection());
    
    // Create aggregate (generates events)
    var subscriptionPayment = SubscriptionPayment.Buy(
        _payerContext.PayerId,
        SubscriptionPeriod.Of(command.SubscriptionTypeCode),
        command.CountryCode,
        MoneyValue.Of(command.Value, command.Currency),
        priceList);
    
    // Append to event store
    _aggregateStore.AppendChanges(subscriptionPayment);
    
    return subscriptionPayment.Id;
}
```

### Key Characteristics

1. **Events as Source of Truth**: Event stream is the primary data store
2. **Immutable Events**: Events are never deleted or modified
3. **Temporal Queries**: Can rebuild state at any point in time
4. **Event Versioning**: Events are versioned via `DomainEventTypeMappings`
5. **Eventual Consistency**: Read models eventually consistent with write model
6. **Optimistic Concurrency**: Version number prevents conflicts

## Consequences

### Positive

- **Complete Audit Trail**: Every state change is recorded permanently
- **Regulatory Compliance**: Immutable record of all financial transactions
- **Temporal Queries**: Can answer "what was the state at time X?"
- **Debugging**: Replay events to reproduce bugs
- **Event Replay**: Can rebuild read models from scratch
- **Multiple Read Models**: Create different projections from same events
- **Natural Fit**: Payment lifecycle naturally event-driven
- **Accountability**: Full history of who did what when

### Negative

- **Increased Complexity**: More complex than CRUD
- **Learning Curve**: Team needs to understand event sourcing concepts
- **Eventual Consistency**: Read models lag behind (though minimal in same process)
- **Query Complexity**: Need projections for each query pattern
- **Event Versioning**: Must handle schema evolution carefully
- **Storage Growth**: Event store grows continuously (mitigated by snapshots if needed)
- **Debugging**: Harder to understand current state without tooling

### Constraints and Guidelines

1. **Events are Immutable**: Never change published events
2. **Events are Facts**: Past tense naming (SubscriptionCreated, PaymentReceived)
3. **Self-Contained**: Events contain all data needed
4. **Versioning Strategy**: Use event type mappings for versioning
5. **Idempotent Projections**: Projectors must handle duplicate events
6. **Checkpoint Management**: Track projection progress

### When NOT to Use

Event sourcing is NOT used in:
- Meetings module (simple state, no regulatory requirements)
- Administration module (approval workflow, simpler with CRUD)
- Users module (user profile, no audit trail requirements)
- Registrations module (temporary data, simple lifecycle)

### Performance Considerations

- **Read Performance**: Excellent (queries hit read models, not event stream)
- **Write Performance**: Good (append-only writes are fast)
- **Rebuild Performance**: Can be slow if many events (use snapshots if needed)
- **Current Implementation**: No snapshots (payment streams are small)

### Tools and Libraries

- **SQL Stream Store**: Event store implementation
- **Dapper**: Query read models
- **Quartz.NET**: Background projection processing

## References

- [SQL Stream Store Documentation](https://sqlstreamstore.readthedocs.io/)
- [Event Sourcing Pattern - Martin Fowler](https://martinfowler.com/eaaDev/EventSourcing.html)
- Section 3.15 "Event Sourcing" in project README
