# ADR-0020: Outbox/Inbox Pattern for Reliable Messaging

## Status

Accepted

## Context

When modules communicate asynchronously via events (see ADR-0004), we face reliability challenges:

1. **Atomicity Problem**: 
   - We need to save aggregate state AND publish event atomically
   - If we save to DB but fail to publish event → data inconsistency
   - If we publish event but fail to save to DB → event without corresponding state

2. **Delivery Guarantees**:
   - Network failures can cause message loss
   - Process crashes can lose in-flight messages
   - No guarantee messages are delivered exactly once

3. **Module Independence**:
   - Modules can't share transactions (different schemas)
   - Can't use distributed transactions (2PC) - adds complexity we want to avoid
   - Need reliability without tight coupling

### Considered Alternatives

1. **Direct Event Bus Publication**
   ```csharp
   await _repository.Save(aggregate);
   await _eventBus.Publish(@event);  // ❌ Not atomic!
   ```
   - Pros: Simple, fast
   - Cons: Not atomic, can lose events, no retry

2. **Distributed Transaction (2PC)**
   ```csharp
   using (var scope = new TransactionScope())
   {
       await _repository.Save(aggregate);
       await _eventBus.Publish(@event);
       scope.Complete();
   }
   ```
   - Pros: Atomic
   - Cons: Complex, requires DTC, performance issues, not suitable for async messaging

3. **Outbox Pattern**
   - Save events to database in same transaction as aggregate
   - Background process publishes from outbox
   - Pros: Atomic writes, guaranteed delivery, simple
   - Cons: Eventual consistency, requires background processing

## Decision

We will implement the **Outbox Pattern** for publishing events and **Inbox Pattern** for consuming events.

### Outbox Pattern (Publishing)

#### Architecture

```
Module A (Publisher)
  |
  |-- [Command Handler]
  |       |
  |       |-- Save Aggregate (Transaction Start)
  |       |-- Add Events to Outbox ←---- Same Transaction
  |       |-- Commit Transaction
  |
  |-- [Background Worker - Quartz.NET]
          |
          |-- Read Outbox
          |-- Publish to Event Bus
          |-- Mark as Processed
```

#### Implementation

**1. Outbox Table**
```sql
CREATE TABLE [app].[OutboxMessages]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [OccurredOn] DATETIME2 NOT NULL,
    [Type] VARCHAR(255) NOT NULL,
    [Data] VARCHAR(MAX) NOT NULL,
    [ProcessedDate] DATETIME2 NULL
);
```

**2. Adding to Outbox**
```csharp
public class UnitOfWorkCommandHandlerDecorator<T> : ICommandHandler<T>
{
    public async Task Handle(T command, CancellationToken cancellationToken)
    {
        await _decorated.Handle(command, cancellationToken);
        
        // Mark internal command as processed if applicable
        if (command is InternalCommandBase internalCommand)
        {
            internalCommand.ProcessedDate = DateTime.UtcNow;
        }
        
        // Commit saves both aggregate AND outbox messages in same transaction
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}

internal class OutboxAccessor : IOutbox
{
    public void Add(IDomainEvent domainEvent)
    {
        var outboxMessage = new OutboxMessage(
            domainEvent,
            domainEvent.GetType().FullName);
            
        _context.OutboxMessages.Add(outboxMessage);
    }
}
```

**3. Processing Outbox**
```csharp
internal class ProcessOutboxJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var outboxMessages = await _context.OutboxMessages
            .Where(x => x.ProcessedDate == null)
            .OrderBy(x => x.OccurredOn)
            .Take(50)
            .ToListAsync();
            
        foreach (var outboxMessage in outboxMessages)
        {
            var domainEvent = Deserialize(outboxMessage);
            
            // Publish to event bus
            await _eventsBus.Publish(domainEvent);
            
            // Mark as processed
            outboxMessage.ProcessedDate = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync();
    }
}
```

### Inbox Pattern (Consuming)

#### Architecture

```
Event Bus
  |
  |-- [Deliver Event]
  |
Module B (Consumer)
  |
  |-- [Event Handler]
  |       |
  |       |-- Check if already processed (idempotency)
  |       |-- If not: Add to Inbox
  |       |-- Return (ACK event)
  |
  |-- [Background Worker - Quartz.NET]
          |
          |-- Read Inbox
          |-- Process via Internal Command
          |-- Mark as Processed
```

#### Implementation

**1. Inbox Table**
```sql
CREATE TABLE [app].[InboxMessages]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [OccurredOn] DATETIME2 NOT NULL,
    [Type] VARCHAR(255) NOT NULL,
    [Data] VARCHAR(MAX) NOT NULL,
    [ProcessedDate] DATETIME2 NULL
);
```

**2. Adding to Inbox**
```csharp
internal class IntegrationEventGenericHandler<T> : IIntegrationEventHandler<T>
    where T : IntegrationEvent
{
    public async Task Handle(T @event)
    {
        // Check if already processed (idempotency)
        var alreadyProcessed = await _context.InboxMessages
            .AnyAsync(x => x.Id == @event.Id);
            
        if (alreadyProcessed)
        {
            return;  // Already handled, skip
        }
        
        // Add to inbox
        var inboxMessage = new InboxMessage(@event, @event.GetType().FullName);
        _context.InboxMessages.Add(inboxMessage);
        
        await _context.SaveChangesAsync();
    }
}
```

**3. Processing Inbox**
```csharp
internal class ProcessInboxJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var inboxMessages = await _context.InboxMessages
            .Where(x => x.ProcessedDate == null)
            .OrderBy(x => x.OccurredOn)
            .Take(50)
            .ToListAsync();
            
        foreach (var inboxMessage in inboxMessages)
        {
            var integrationEvent = Deserialize(inboxMessage);
            
            // Convert to internal command and execute
            var command = CreateCommandFromEvent(integrationEvent);
            await _commandsScheduler.EnqueueAsync(command);
            
            // Mark as processed
            inboxMessage.ProcessedDate = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync();
    }
}
```

### Background Processing

Using **Quartz.NET** for reliable background job execution:

```csharp
public static class QuartzStartup
{
    public static void Initialize(ILogger logger)
    {
        var scheduler = CreateScheduler();
        
        scheduler.ScheduleJob(
            JobBuilder.Create<ProcessOutboxJob>().Build(),
            TriggerBuilder.Create()
                .WithCronSchedule("0/2 * * ? * *")  // Every 2 seconds
                .Build());
                
        scheduler.ScheduleJob(
            JobBuilder.Create<ProcessInboxJob>().Build(),
            TriggerBuilder.Create()
                .WithCronSchedule("0/2 * * ? * *")  // Every 2 seconds
                .Build());
                
        scheduler.Start();
    }
}
```

## Consequences

### Positive

- **Guaranteed Delivery (At-Least-Once)**: Events will be delivered eventually
- **Atomicity**: State changes and event publishing are atomic
- **Reliability**: Survives process crashes and network failures
- **Idempotency**: Inbox pattern prevents duplicate processing
- **Auditability**: Full record of all events in outbox
- **Ordering**: Events processed in order within an aggregate
- **No Message Broker Needed**: Works with in-memory event bus
- **Transactional**: Uses local transactions, no 2PC needed
- **Debuggability**: Can inspect outbox/inbox tables to see event flow

### Negative

- **Eventual Consistency**: Events are not published immediately
- **Latency**: Small delay (2 seconds by default) before event processing
- **Storage**: Outbox/Inbox tables grow over time (need cleanup strategy)
- **Complexity**: More moving parts than direct publishing
- **Background Jobs**: Requires job scheduler (Quartz.NET)
- **Duplicate Processing**: Must handle at-least-once delivery

### Guarantees Provided

✅ **At-Least-Once Delivery**: Every event will be delivered at least once
✅ **At-Least-Once Processing**: Every event will be processed at least once  
✅ **No Lost Events**: Events persisted before commit
✅ **No Phantom Events**: Events only published if transaction commits
❌ **Exactly-Once**: Not guaranteed (handled by idempotent handlers)
❌ **Ordering Across Aggregates**: Not guaranteed (only within aggregate)

### Idempotency Strategy

Since we have at-least-once delivery, handlers must be idempotent:

```csharp
internal class CreateMeetingGroupCommandHandler : ICommandHandler<CreateMeetingGroupCommand>
{
    public async Task Handle(CreateMeetingGroupCommand command, CancellationToken cancellationToken)
    {
        // Check if already exists
        var exists = await _context.MeetingGroups
            .AnyAsync(x => x.Id == command.MeetingGroupId);
            
        if (exists)
        {
            return;  // Already created, skip
        }
        
        // Create meeting group
        var meetingGroup = MeetingGroup.Create(/* ... */);
        await _repository.AddAsync(meetingGroup);
    }
}
```

### Cleanup Strategy

Processed messages should be cleaned up periodically:

```csharp
// Delete processed messages older than 30 days
DELETE FROM [app].[OutboxMessages]
WHERE [ProcessedDate] IS NOT NULL 
  AND [ProcessedDate] < DATEADD(day, -30, GETUTCDATE());

DELETE FROM [app].[InboxMessages]
WHERE [ProcessedDate] IS NOT NULL 
  AND [ProcessedDate] < DATEADD(day, -30, GETUTCDATE());
```

### Performance Considerations

- **Batch Size**: Process 50 messages at a time (configurable)
- **Poll Interval**: Every 2 seconds (configurable)
- **Indexing**: Index on `ProcessedDate` for efficient queries
- **Partitioning**: Consider table partitioning for high volume

### Failure Handling

**Transient Failures**: 
- Background job retries automatically (Quartz.NET built-in retry)

**Poison Messages**: 
- If message consistently fails, it blocks the queue
- Solution: Add retry count, move to dead letter after N attempts

```csharp
// Enhanced with retry tracking
public class InboxMessage
{
    public int RetryCount { get; set; }
    public DateTime? LastRetryDate { get; set; }
    public string ErrorMessage { get; set; }
}
```

## References

- [The Outbox Pattern by Kamil Grzybek](http://www.kamilgrzybek.com/design/the-outbox-pattern/)
- [Reliable Event Processing by Udi Dahan](https://particular.net/blog/reliable-event-processing)
- Section 3.7 "Modules Integration" in project README
