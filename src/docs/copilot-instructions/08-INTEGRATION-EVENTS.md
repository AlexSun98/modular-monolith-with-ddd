# Integration Events Guide

This guide describes how modules communicate asynchronously using Integration Events and the In-Memory Events Bus.

## Overview

**Key Principles:**
1. Modules communicate **only asynchronously** via Integration Events
2. **No direct method calls** between modules
3. Events are delivered via the **Outbox/Inbox pattern** for reliability
4. Each module has its own **IntegrationEvents** assembly (contracts)

## Architecture

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Module A       │     │  Events Bus     │     │  Module B       │
│                 │     │  (In-Memory)    │     │                 │
│  ┌───────────┐  │     │                 │     │  ┌───────────┐  │
│  │ Outbox    │──┼────▶│   Publish/      │────▶│  │ Inbox     │  │
│  │ Table     │  │     │   Subscribe     │     │  │ Table     │  │
│  └───────────┘  │     │                 │     │  └───────────┘  │
│                 │     │                 │     │        │        │
│  Background     │     │                 │     │  Background     │
│  Worker ─────────────▶│                 │     │  Worker ◀───────┘
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

## Creating Integration Events

### Step 1: Define the Event Contract

```csharp
// Location: Modules/{Module}/IntegrationEvents/{EventName}IntegrationEvent.cs
// This is in the IntegrationEvents assembly - the ONLY part other modules can reference

public class MeetingGroupProposalAcceptedIntegrationEvent : IntegrationEvent
{
    public Guid MeetingGroupProposalId { get; }
    public string Name { get; }
    public string Description { get; }
    public string LocationCity { get; }
    public string LocationCountryCode { get; }
    public Guid ProposalUserId { get; }
    public DateTime ProposalDate { get; }

    public MeetingGroupProposalAcceptedIntegrationEvent(
        Guid id,
        DateTime occurredOn,
        Guid meetingGroupProposalId,
        string name,
        string description,
        string locationCity,
        string locationCountryCode,
        Guid proposalUserId,
        DateTime proposalDate)
        : base(id, occurredOn)
    {
        MeetingGroupProposalId = meetingGroupProposalId;
        Name = name;
        Description = description;
        LocationCity = locationCity;
        LocationCountryCode = locationCountryCode;
        ProposalUserId = proposalUserId;
        ProposalDate = proposalDate;
    }
}
```

### Integration Event Base Class

```csharp
// Location: BuildingBlocks/Infrastructure/IntegrationEvent.cs
public abstract class IntegrationEvent
{
    public Guid Id { get; }
    public DateTime OccurredOn { get; }

    protected IntegrationEvent(Guid id, DateTime occurredOn)
    {
        Id = id;
        OccurredOn = occurredOn;
    }
}
```

## Publishing Integration Events

### Step 1: Handle Domain Event

```csharp
// Location: Modules/{Module}/Application/{Feature}/Events/{DomainEvent}Handler.cs
internal class MeetingGroupProposalAcceptedDomainEventHandler 
    : INotificationHandler<MeetingGroupProposalAcceptedDomainEvent>
{
    private readonly IOutbox _outbox;

    public MeetingGroupProposalAcceptedDomainEventHandler(IOutbox outbox)
    {
        _outbox = outbox;
    }

    public Task Handle(
        MeetingGroupProposalAcceptedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        // Create integration event
        var integrationEvent = new MeetingGroupProposalAcceptedIntegrationEvent(
            Guid.NewGuid(),
            notification.OccurredOn,
            notification.MeetingGroupProposalId.Value,
            notification.Name,
            notification.Description,
            notification.LocationCity,
            notification.LocationCountryCode,
            notification.ProposalUserId.Value,
            notification.ProposalDate);

        // Add to outbox (will be sent by background worker)
        _outbox.Add(integrationEvent);

        return Task.CompletedTask;
    }
}
```

### Step 2: Outbox Implementation

```csharp
// Location: Modules/{Module}/Infrastructure/Outbox/Outbox.cs
internal class Outbox : IOutbox
{
    private readonly ModuleContext _context;

    public Outbox(ModuleContext context)
    {
        _context = context;
    }

    public void Add(IntegrationEvent integrationEvent)
    {
        var outboxMessage = new OutboxMessage(
            integrationEvent.Id,
            integrationEvent.OccurredOn,
            integrationEvent.GetType().FullName,
            JsonConvert.SerializeObject(integrationEvent, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            }));

        _context.OutboxMessages.Add(outboxMessage);
    }
}
```

### Step 3: Outbox Processing (Background Worker)

```csharp
// Location: Modules/{Module}/Infrastructure/Configuration/Processing/Outbox/ProcessOutboxJob.cs
[DisallowConcurrentExecution]
internal class ProcessOutboxJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = CompositionRoot.BeginLifetimeScope();

        var sqlConnectionFactory = scope.Resolve<ISqlConnectionFactory>();
        var eventsBus = scope.Resolve<IEventsBus>();

        var connection = sqlConnectionFactory.GetOpenConnection();

        const string sql = """
            SELECT [Id], [Type], [Data] 
            FROM [{schema}].[OutboxMessages] 
            WHERE [ProcessedDate] IS NULL
            ORDER BY [OccurredOn]
            """;

        var messages = await connection.QueryAsync<OutboxMessageDto>(sql);

        foreach (var message in messages)
        {
            var type = Type.GetType(message.Type);
            var integrationEvent = JsonConvert.DeserializeObject(message.Data, type) as IntegrationEvent;

            // Publish to Events Bus
            await eventsBus.Publish(integrationEvent);

            // Mark as processed
            const string updateSql = """
                UPDATE [{schema}].[OutboxMessages] 
                SET [ProcessedDate] = @ProcessedDate 
                WHERE [Id] = @Id
                """;

            await connection.ExecuteAsync(updateSql, new
            {
                Id = message.Id,
                ProcessedDate = DateTime.UtcNow
            });
        }
    }
}
```

## Subscribing to Integration Events

### Step 1: Configure Event Handler Registration

```csharp
// Location: Modules/{Module}/Infrastructure/Configuration/EventsBus/EventsBusStartup.cs
internal static class EventsBusStartup
{
    internal static void Initialize(ILogger logger)
    {
        // Subscribe to events from other modules
        SubscribeToIntegrationEvent<MeetingGroupProposalAcceptedIntegrationEvent>(logger);
        SubscribeToIntegrationEvent<NewUserRegisteredIntegrationEvent>(logger);
        // Add more subscriptions...
    }

    private static void SubscribeToIntegrationEvent<T>(ILogger logger) where T : IntegrationEvent
    {
        using var scope = CompositionRoot.BeginLifetimeScope();

        var eventsBus = scope.Resolve<IEventsBus>();

        eventsBus.Subscribe(new IntegrationEventGenericHandler<T>(logger));
    }
}
```

### Step 2: Generic Integration Event Handler

```csharp
// Location: Modules/{Module}/Infrastructure/Configuration/EventsBus/IntegrationEventGenericHandler.cs
internal class IntegrationEventGenericHandler<T> : IIntegrationEventHandler<T>
    where T : IntegrationEvent
{
    private readonly ILogger _logger;

    public IntegrationEventGenericHandler(ILogger logger)
    {
        _logger = logger;
    }

    public async Task Handle(T @event)
    {
        using var scope = CompositionRoot.BeginLifetimeScope();

        using var connection = scope.Resolve<ISqlConnectionFactory>().GetOpenConnection();

        // Check if already processed (inbox)
        var existingMessage = await GetInboxMessage(@event.Id, connection);
        if (existingMessage != null)
        {
            return; // Already processed
        }

        // Store in inbox
        var inboxMessage = new InboxMessage(
            @event.Id,
            @event.OccurredOn,
            @event.GetType().FullName,
            JsonConvert.SerializeObject(@event));

        await SaveInboxMessage(inboxMessage, connection);

        // Schedule internal command for processing
        var internalCommand = CreateInternalCommand(@event);
        await ScheduleInternalCommand(internalCommand, connection);
    }
}
```

### Step 3: Create Internal Command Handler

```csharp
// Location: Modules/{Module}/Application/IntegrationEvents/{EventName}Handler.cs
internal class MeetingGroupProposalAcceptedIntegrationEventHandler 
    : ICommandHandler<ProcessMeetingGroupProposalAcceptedCommand>
{
    private readonly IMeetingGroupRepository _meetingGroupRepository;

    public MeetingGroupProposalAcceptedIntegrationEventHandler(
        IMeetingGroupRepository meetingGroupRepository)
    {
        _meetingGroupRepository = meetingGroupRepository;
    }

    public async Task Handle(
        ProcessMeetingGroupProposalAcceptedCommand command,
        CancellationToken cancellationToken)
    {
        // Create meeting group based on accepted proposal
        var meetingGroup = MeetingGroup.CreateBasedOnProposal(
            new MeetingGroupProposalId(command.MeetingGroupProposalId),
            command.Name,
            command.Description,
            new MeetingGroupLocation(command.LocationCity, command.LocationCountryCode),
            new MemberId(command.ProposalUserId));

        await _meetingGroupRepository.AddAsync(meetingGroup);
    }
}
```

## Database Tables

### Outbox Table

```sql
CREATE TABLE [{schema}].[OutboxMessages]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [OccurredOn] DATETIME2 NOT NULL,
    [Type] VARCHAR(500) NOT NULL,
    [Data] NVARCHAR(MAX) NOT NULL,
    [ProcessedDate] DATETIME2 NULL,
    CONSTRAINT [PK_{schema}_OutboxMessages_Id] PRIMARY KEY ([Id])
);

CREATE INDEX [IX_{schema}_OutboxMessages_ProcessedDate] 
    ON [{schema}].[OutboxMessages]([ProcessedDate])
    WHERE [ProcessedDate] IS NULL;
```

### Inbox Table

```sql
CREATE TABLE [{schema}].[InboxMessages]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [OccurredOn] DATETIME2 NOT NULL,
    [Type] VARCHAR(500) NOT NULL,
    [Data] NVARCHAR(MAX) NOT NULL,
    [ProcessedDate] DATETIME2 NULL,
    CONSTRAINT [PK_{schema}_InboxMessages_Id] PRIMARY KEY ([Id])
);

CREATE INDEX [IX_{schema}_InboxMessages_ProcessedDate] 
    ON [{schema}].[InboxMessages]([ProcessedDate])
    WHERE [ProcessedDate] IS NULL;
```

## Events Bus Interface

```csharp
// Location: BuildingBlocks/Infrastructure/EventsBus/IEventsBus.cs
public interface IEventsBus
{
    void Subscribe<T>(IIntegrationEventHandler<T> handler) where T : IntegrationEvent;

    Task Publish<T>(T @event) where T : IntegrationEvent;
}

public interface IIntegrationEventHandler<in T> where T : IntegrationEvent
{
    Task Handle(T @event);
}
```

## Common Integration Events

### User Registered (from UserAccess module)

```csharp
public class NewUserRegisteredIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; }
    public string Login { get; }
    public string Email { get; }
    public string FirstName { get; }
    public string LastName { get; }

    public NewUserRegisteredIntegrationEvent(
        Guid id,
        DateTime occurredOn,
        Guid userId,
        string login,
        string email,
        string firstName,
        string lastName)
        : base(id, occurredOn)
    {
        UserId = userId;
        Login = login;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }
}
```

### Meeting Group Proposal Accepted (from Administration module)

```csharp
public class MeetingGroupProposalAcceptedIntegrationEvent : IntegrationEvent
{
    public Guid MeetingGroupProposalId { get; }
    public string Name { get; }
    public string Description { get; }
    public string LocationCity { get; }
    public string LocationCountryCode { get; }
    public Guid ProposalUserId { get; }
    public DateTime ProposalDate { get; }

    // Constructor...
}
```

### Subscription Paid (from Payments module)

```csharp
public class SubscriptionPaymentPaidIntegrationEvent : IntegrationEvent
{
    public Guid PayerId { get; }
    public string SubscriptionPeriodCode { get; }
    public DateTime ExpirationDate { get; }

    // Constructor...
}
```

## Project Dependencies

```
ModuleA.Infrastructure
    └── references ──▶ ModuleB.IntegrationEvents (events from B that A subscribes to)

ModuleB.Infrastructure  
    └── references ──▶ ModuleA.IntegrationEvents (events from A that B subscribes to)

// NEVER reference another module's Domain, Application, or Infrastructure!
```

## Quartz Job Configuration

```csharp
// Location: Modules/{Module}/Infrastructure/Configuration/Quartz/QuartzStartup.cs
internal static class QuartzStartup
{
    internal static void Initialize(ILogger logger)
    {
        var schedulerFactory = new StdSchedulerFactory();
        var scheduler = schedulerFactory.GetScheduler().GetAwaiter().GetResult();

        // Process Outbox every 2 seconds
        var processOutboxJob = JobBuilder.Create<ProcessOutboxJob>().Build();
        var triggerOutbox = TriggerBuilder.Create()
            .StartNow()
            .WithSimpleSchedule(x => x
                .WithIntervalInSeconds(2)
                .RepeatForever())
            .Build();

        scheduler.ScheduleJob(processOutboxJob, triggerOutbox).GetAwaiter().GetResult();

        // Process Inbox every 2 seconds
        var processInboxJob = JobBuilder.Create<ProcessInboxJob>().Build();
        var triggerInbox = TriggerBuilder.Create()
            .StartNow()
            .WithSimpleSchedule(x => x
                .WithIntervalInSeconds(2)
                .RepeatForever())
            .Build();

        scheduler.ScheduleJob(processInboxJob, triggerInbox).GetAwaiter().GetResult();

        scheduler.Start().GetAwaiter().GetResult();
    }
}
```

## Best Practices

### DO:
- ✅ Use Outbox/Inbox pattern for reliability
- ✅ Keep integration events immutable
- ✅ Include all necessary data in events (no callbacks needed)
- ✅ Handle duplicate events (idempotency)
- ✅ Use meaningful event names with past tense verbs

### DON'T:
- ❌ Call other modules directly
- ❌ Share database tables between modules
- ❌ Include sensitive data in events
- ❌ Create circular dependencies via events
- ❌ Skip inbox processing (causes duplicates)
