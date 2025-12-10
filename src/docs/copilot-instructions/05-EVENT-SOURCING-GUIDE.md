# Event Sourcing Guide

This guide describes Event Sourcing patterns used in the **Payments module** of the Modular Monolith application.

## Overview

Event Sourcing stores the state of an application as a sequence of events rather than current state. The Payments module uses this pattern with SQL Stream Store.

**Key Concepts:**
- **Event Stream** - Ordered sequence of domain events for an aggregate
- **Aggregate** - Rebuilt from events, not loaded from traditional tables
- **Projections** - Read models built by processing events
- **Subscriptions** - Mechanism to react to new events

## Architecture

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Command        │────▶│  Aggregate      │────▶│  Event Stream   │
│  Handler        │     │  (Domain)       │     │  (SQL Stream)   │
└─────────────────┘     └─────────────────┘     └─────────────────┘
                                                        │
                                                        ▼
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Query          │◀────│  Read Model     │◀────│  Projector      │
│  Handler        │     │  (SQL Table)    │     │  (Subscription) │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

## Implementing Event-Sourced Aggregates

### Step 1: Create Domain Events

```csharp
// Location: Modules/Payments/Domain/{Aggregate}/Events/
public class SubscriptionPaymentCreatedDomainEvent : DomainEventBase
{
    public Guid SubscriptionPaymentId { get; }
    public Guid PayerId { get; }
    public string SubscriptionPeriodCode { get; }
    public string CountryCode { get; }
    public string Status { get; }
    public decimal Value { get; }
    public string Currency { get; }

    public SubscriptionPaymentCreatedDomainEvent(
        Guid subscriptionPaymentId,
        Guid payerId,
        string subscriptionPeriodCode,
        string countryCode,
        string status,
        decimal value,
        string currency)
    {
        SubscriptionPaymentId = subscriptionPaymentId;
        PayerId = payerId;
        SubscriptionPeriodCode = subscriptionPeriodCode;
        CountryCode = countryCode;
        Status = status;
        Value = value;
        Currency = currency;
    }
}
```

### Step 2: Create the Aggregate

```csharp
// Location: Modules/Payments/Domain/SubscriptionPayments/SubscriptionPayment.cs
public class SubscriptionPayment : AggregateRoot
{
    private PayerId _payerId;
    private SubscriptionPeriod _subscriptionPeriod;
    private string _countryCode;
    private SubscriptionPaymentStatus _subscriptionPaymentStatus;
    private MoneyValue _value;

    // Required for loading from events
    private SubscriptionPayment() { }

    // Apply method - routes to specific handlers
    protected override void Apply(IDomainEvent @event)
    {
        this.When((dynamic)@event);
    }

    // Factory method - creates new aggregate
    public static SubscriptionPayment Buy(
        PayerId payerId,
        SubscriptionPeriod period,
        string countryCode,
        MoneyValue priceOffer,
        PriceList priceList)
    {
        // Check business rules
        var priceInPriceList = priceList.GetPrice(countryCode, period, PriceListItemCategory.New);
        CheckRule(new PriceOfferMustMatchPriceInPriceListRule(priceOffer, priceInPriceList));

        var subscriptionPayment = new SubscriptionPayment();

        // Create event
        var @event = new SubscriptionPaymentCreatedDomainEvent(
            Guid.NewGuid(),
            payerId.Value,
            period.Code,
            countryCode,
            SubscriptionPaymentStatus.WaitingForPayment.Code,
            priceOffer.Value,
            priceOffer.Currency);

        // Apply to self and add to domain events
        subscriptionPayment.Apply(@event);
        subscriptionPayment.AddDomainEvent(@event);

        return subscriptionPayment;
    }

    // Event handler - restores state from event
    private void When(SubscriptionPaymentCreatedDomainEvent @event)
    {
        this.Id = @event.SubscriptionPaymentId;
        _payerId = new PayerId(@event.PayerId);
        _subscriptionPeriod = SubscriptionPeriod.Of(@event.SubscriptionPeriodCode);
        _countryCode = @event.CountryCode;
        _subscriptionPaymentStatus = SubscriptionPaymentStatus.Of(@event.Status);
        _value = MoneyValue.Of(@event.Value, @event.Currency);
    }

    // Additional business method example
    public void MarkAsPaid()
    {
        CheckRule(new PaymentMustBeWaitingForPaymentRule(_subscriptionPaymentStatus));

        var @event = new SubscriptionPaymentPaidDomainEvent(
            this.Id,
            SubscriptionPaymentStatus.Paid.Code);

        Apply(@event);
        AddDomainEvent(@event);
    }

    private void When(SubscriptionPaymentPaidDomainEvent @event)
    {
        _subscriptionPaymentStatus = SubscriptionPaymentStatus.Of(@event.Status);
    }
}
```

### Step 3: Create AggregateRoot Base Class

```csharp
// Location: Modules/Payments/Domain/SeedWork/AggregateRoot.cs
public abstract class AggregateRoot
{
    public Guid Id { get; protected set; }
    public int Version { get; private set; }

    private readonly List<IDomainEvent> _domainEvents;

    protected AggregateRoot()
    {
        _domainEvents = new List<IDomainEvent>();
        Version = -1;
    }

    protected void AddDomainEvent(IDomainEvent @event)
    {
        _domainEvents.Add(@event);
    }

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    // Load aggregate from event history
    public void Load(IEnumerable<IDomainEvent> history)
    {
        foreach (var e in history)
        {
            Apply(e);
            Version++;
        }
    }

    protected abstract void Apply(IDomainEvent @event);

    protected static void CheckRule(IBusinessRule rule)
    {
        if (rule.IsBroken())
        {
            throw new BusinessRuleValidationException(rule);
        }
    }
}
```

## Aggregate Store (SQL Stream Store)

### Implementation

```csharp
// Location: Modules/Payments/Infrastructure/AggregateStore/SqlStreamAggregateStore.cs
public class SqlStreamAggregateStore : IAggregateStore
{
    private readonly IStreamStore _streamStore;
    private readonly List<AggregateToSave> _aggregatesToSave;

    public SqlStreamAggregateStore(ISqlConnectionFactory sqlConnectionFactory)
    {
        _streamStore = new MsSqlStreamStore(
            new MsSqlStreamStoreSettings(sqlConnectionFactory.GetConnectionString())
            {
                Schema = DatabaseSchema.Name
            });

        _aggregatesToSave = new List<AggregateToSave>();
    }

    // Mark aggregate for saving (called from Command Handler)
    public void AppendChanges(AggregateRoot aggregate)
    {
        var messages = aggregate.GetDomainEvents()
            .Select(e => new NewStreamMessage(
                Guid.NewGuid(),
                e.GetType().Name,
                JsonConvert.SerializeObject(e)))
            .ToArray();

        _aggregatesToSave.Add(new AggregateToSave(aggregate, messages));
    }

    // Save all pending changes (called from Unit of Work)
    public async Task Save()
    {
        foreach (var aggregateToSave in _aggregatesToSave)
        {
            await _streamStore.AppendToStream(
                GetStreamId(aggregateToSave.Aggregate),
                aggregateToSave.Aggregate.Version,
                aggregateToSave.Messages);
        }

        _aggregatesToSave.Clear();
    }

    // Load aggregate from event stream
    public async Task<T> Load<T>(AggregateId<T> aggregateId) where T : AggregateRoot
    {
        var streamId = GetStreamId(aggregateId);

        IList<IDomainEvent> domainEvents = new List<IDomainEvent>();
        ReadStreamPage readStreamPage;

        do
        {
            readStreamPage = await _streamStore.ReadStreamForwards(
                streamId,
                StreamVersion.Start,
                maxCount: 100);

            foreach (var streamMessage in readStreamPage.Messages)
            {
                Type type = DomainEventTypeMappings.Dictionary[streamMessage.Type];
                var jsonData = await streamMessage.GetJsonData();
                var domainEvent = JsonConvert.DeserializeObject(jsonData, type) as IDomainEvent;

                domainEvents.Add(domainEvent);
            }
        } while (!readStreamPage.IsEnd);

        var aggregate = (T)Activator.CreateInstance(typeof(T), true);
        aggregate.Load(domainEvents);

        return aggregate;
    }

    private static string GetStreamId<T>(AggregateId<T> aggregateId) where T : AggregateRoot
        => $"{typeof(T).Name}-{aggregateId.Value}";

    private static string GetStreamId(AggregateRoot aggregate)
        => $"{aggregate.GetType().Name}-{aggregate.Id}";
}
```

### Event Type Mappings

```csharp
// Location: Modules/Payments/Infrastructure/AggregateStore/DomainEventTypeMappings.cs
internal static class DomainEventTypeMappings
{
    internal static readonly Dictionary<string, Type> Dictionary = new()
    {
        { nameof(SubscriptionPaymentCreatedDomainEvent), typeof(SubscriptionPaymentCreatedDomainEvent) },
        { nameof(SubscriptionPaymentPaidDomainEvent), typeof(SubscriptionPaymentPaidDomainEvent) },
        { nameof(SubscriptionCreatedDomainEvent), typeof(SubscriptionCreatedDomainEvent) },
        { nameof(SubscriptionRenewedDomainEvent), typeof(SubscriptionRenewedDomainEvent) },
        { nameof(SubscriptionExpiredDomainEvent), typeof(SubscriptionExpiredDomainEvent) },
        // Add all domain events here
    };
}
```

## Command Handler Pattern

```csharp
// Location: Modules/Payments/Application/Subscriptions/BuySubscription/BuySubscriptionCommandHandler.cs
public class BuySubscriptionCommandHandler : ICommandHandler<BuySubscriptionCommand, Guid>
{
    private readonly IAggregateStore _aggregateStore;
    private readonly IPayerContext _payerContext;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public BuySubscriptionCommandHandler(
        IAggregateStore aggregateStore,
        IPayerContext payerContext,
        ISqlConnectionFactory sqlConnectionFactory)
    {
        _aggregateStore = aggregateStore;
        _payerContext = payerContext;
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    public async Task<Guid> Handle(BuySubscriptionCommand command, CancellationToken cancellationToken)
    {
        // Load any required data
        var priceList = await PriceListProvider.GetPriceList(_sqlConnectionFactory.GetOpenConnection());

        // Create aggregate (events are created internally)
        var subscriptionPayment = SubscriptionPayment.Buy(
            _payerContext.PayerId,
            SubscriptionPeriod.Of(command.SubscriptionTypeCode),
            command.CountryCode,
            MoneyValue.Of(command.Value, command.Currency),
            priceList);

        // Mark for saving (events will be saved by Unit of Work)
        _aggregateStore.AppendChanges(subscriptionPayment);

        return subscriptionPayment.Id;
    }
}
```

## Projections (Read Models)

### Subscriptions Manager

```csharp
// Location: Modules/Payments/Infrastructure/Configuration/Processing/SubscriptionsManager.cs
public class SubscriptionsManager
{
    private readonly IStreamStore _streamStore;

    public SubscriptionsManager(IStreamStore streamStore)
    {
        _streamStore = streamStore;
    }

    public void Start()
    {
        long? actualPosition;

        using (var scope = PaymentsCompositionRoot.BeginLifetimeScope())
        {
            var checkpointStore = scope.Resolve<ICheckpointStore>();
            actualPosition = checkpointStore.GetCheckpoint(SubscriptionCode.All);
        }

        _streamStore.SubscribeToAll(actualPosition, StreamMessageReceived);
    }

    public void Stop()
    {
        _streamStore.Dispose();
    }

    private static async Task StreamMessageReceived(
        IAllStreamSubscription subscription,
        StreamMessage streamMessage,
        CancellationToken cancellationToken)
    {
        var type = DomainEventTypeMappings.Dictionary[streamMessage.Type];
        var jsonData = await streamMessage.GetJsonData(cancellationToken);
        var domainEvent = JsonConvert.DeserializeObject(jsonData, type) as IDomainEvent;

        using var scope = PaymentsCompositionRoot.BeginLifetimeScope();

        // Get all projectors and invoke them
        var projectors = scope.Resolve<IList<IProjector>>();
        var tasks = projectors.Select(projector => projector.Project(domainEvent));
        await Task.WhenAll(tasks);

        // Update checkpoint
        var checkpointStore = scope.Resolve<ICheckpointStore>();
        await checkpointStore.StoreCheckpoint(SubscriptionCode.All, streamMessage.Position);
    }
}
```

### Projector Implementation

```csharp
// Location: Modules/Payments/Infrastructure/Configuration/Processing/Projections/SubscriptionDetailsProjector.cs
internal class SubscriptionDetailsProjector : ProjectorBase, IProjector
{
    private readonly IDbConnection _connection;

    public SubscriptionDetailsProjector(ISqlConnectionFactory sqlConnectionFactory)
    {
        _connection = sqlConnectionFactory.GetOpenConnection();
    }

    public async Task Project(IDomainEvent @event)
    {
        await When((dynamic)@event);
    }

    private async Task When(SubscriptionCreatedDomainEvent @event)
    {
        var period = SubscriptionPeriod.GetName(@event.SubscriptionPeriodCode);

        await _connection.ExecuteScalarAsync(
            """
            INSERT INTO payments.SubscriptionDetails 
            ([Id], [Period], [Status], [CountryCode], [ExpirationDate]) 
            VALUES (@SubscriptionId, @Period, @Status, @CountryCode, @ExpirationDate)
            """,
            new
            {
                @event.SubscriptionId,
                period,
                @event.Status,
                @event.CountryCode,
                @event.ExpirationDate
            });
    }

    private async Task When(SubscriptionRenewedDomainEvent @event)
    {
        var period = SubscriptionPeriod.GetName(@event.SubscriptionPeriodCode);

        await _connection.ExecuteScalarAsync(
            """
            UPDATE payments.SubscriptionDetails 
            SET [Status] = @Status, 
                [ExpirationDate] = @ExpirationDate, 
                [Period] = @Period 
            WHERE [Id] = @SubscriptionId
            """,
            new
            {
                @event.SubscriptionId,
                @event.Status,
                @event.ExpirationDate,
                period
            });
    }

    private async Task When(SubscriptionExpiredDomainEvent @event)
    {
        await _connection.ExecuteScalarAsync(
            """
            UPDATE payments.SubscriptionDetails 
            SET [Status] = @Status 
            WHERE [Id] = @SubscriptionId
            """,
            new
            {
                @event.SubscriptionId,
                @event.Status
            });
    }

    // Fallback for unhandled events
    private Task When(IDomainEvent @event) => Task.CompletedTask;
}
```

## Database Schema for Event Sourcing

```sql
-- SQL Stream Store creates its own tables in the schema
-- You need to create read model tables for projections

CREATE TABLE [payments].[SubscriptionDetails]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Period] VARCHAR(50) NOT NULL,
    [Status] VARCHAR(50) NOT NULL,
    [CountryCode] VARCHAR(10) NOT NULL,
    [ExpirationDate] DATETIME NOT NULL,
    CONSTRAINT [PK_payments_SubscriptionDetails_Id] PRIMARY KEY ([Id])
);

-- Checkpoint store for tracking projection progress
CREATE TABLE [payments].[Checkpoints]
(
    [Code] VARCHAR(100) NOT NULL,
    [Position] BIGINT NOT NULL,
    CONSTRAINT [PK_payments_Checkpoints_Code] PRIMARY KEY ([Code])
);
```

## Key Considerations

### Eventual Consistency
- Projections are **eventually consistent** with the event stream
- There's always a delay between saving an event and updating projections
- Design queries to handle this (or use read-your-writes pattern)

### Transaction Boundaries
- Use `TransactionScope` when you need to save events AND other data atomically
- Otherwise, accept eventual consistency

### When to Use Event Sourcing
- ✅ Full audit trail required
- ✅ Complex domain with many state transitions
- ✅ Need to replay events for debugging/analysis
- ✅ Time-travel queries (what was state at time X?)

### When NOT to Use
- ❌ Simple CRUD operations
- ❌ Strong consistency requirements across modules
- ❌ Teams unfamiliar with the pattern
