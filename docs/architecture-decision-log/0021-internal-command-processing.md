# ADR-0021: Internal Command Processing

## Status

Accepted

## Context

According to Domain-Driven Design and Event Storming principles, **every side effect (domain event) should be created by invoking a Command on an Aggregate**. This is illustrated in Alberto Brandolini's famous picture "The picture that explains 'almost' everything."

In our system, we have scenarios where we need to:

1. **Process Integration Events**: When Module A receives an integration event from Module B, it needs to execute business logic
2. **Eventual Consistency**: Execute commands asynchronously in a separate transaction
3. **Background Processing**: Process scheduled tasks or time-based operations
4. **Retry Logic**: Retry failed operations without blocking the original request

The principle is: **You can change system state ONLY by calling a Command**.

### The Problem

When an integration event arrives via the Inbox:

```csharp
// ❌ Anti-pattern: Processing event directly in handler
internal class MeetingGroupProposalAcceptedHandler 
{
    public async Task Handle(MeetingGroupProposalAcceptedIntegrationEvent @event)
    {
        // Directly manipulating repository - bypasses command processing pipeline!
        var meetingGroup = MeetingGroup.Create(@event.MeetingGroupId, @event.Name);
        await _repository.AddAsync(meetingGroup);
        await _unitOfWork.CommitAsync();
    }
}
```

Problems with direct processing:
- Bypasses validation decorators
- Bypasses logging decorators
- Bypasses unit of work decorator
- No retry mechanism
- Harder to test
- Inconsistent with how API commands are processed

### Considered Alternatives

1. **Direct Processing in Event Handlers**
   - Pros: Simple, immediate processing
   - Cons: Bypasses command pipeline, inconsistent, no retries

2. **Call Module Interface Directly**
   ```csharp
   await _meetingsModule.ExecuteCommandAsync(command);
   ```
   - Pros: Uses command pipeline
   - Cons: Not transactional with inbox processing, can fail without retry

3. **Internal Command Pattern**
   - Pros: Transactional, uses full pipeline, retries, consistent
   - Cons: Additional complexity, eventual consistency

## Decision

We will implement the **Internal Command Pattern** for processing that needs to happen asynchronously within a module.

### Architecture

```
Integration Event arrives
         |
         v
  [Inbox Handler]
         |
         |-- Save to Inbox table (Transaction 1)
         |-- ACK event
         |
  [Background Job - ProcessInboxJob]
         |
         |-- Read from Inbox
         |-- Create Internal Command
         |-- Save to InternalCommands table (Transaction 2)
         |
  [Background Job - ProcessInternalCommandsJob]
         |
         |-- Read from InternalCommands
         |-- Execute via Command Pipeline (Transaction 3)
         |-- Mark as processed
```

### Implementation

#### 1. Internal Command Base Class

```csharp
internal abstract class InternalCommandBase : ICommand
{
    public Guid Id { get; }

    protected InternalCommandBase(Guid id)
    {
        this.Id = id;
    }
}

// Example internal command
internal class CreateMeetingGroupCommand : InternalCommandBase
{
    public Guid MeetingGroupId { get; }
    public string Name { get; }
    public string Description { get; }
    // ... other properties

    public CreateMeetingGroupCommand(
        Guid id,
        Guid meetingGroupId, 
        string name,
        string description)
        : base(id)
    {
        MeetingGroupId = meetingGroupId;
        Name = name;
        Description = description;
    }
}
```

#### 2. Internal Commands Table

```sql
CREATE TABLE [app].[InternalCommands]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Type] VARCHAR(255) NOT NULL,
    [Data] VARCHAR(MAX) NOT NULL,
    [ProcessedDate] DATETIME2 NULL,
    [Error] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_InternalCommands] PRIMARY KEY ([Id])
);
```

#### 3. Commands Scheduler

```csharp
internal class CommandsScheduler : ICommandsScheduler
{
    private readonly ApplicationDbContext _context;

    public async Task EnqueueAsync(ICommand command)
    {
        var internalCommand = new InternalCommand
        {
            Id = command.Id,
            Type = command.GetType().FullName,
            Data = JsonConvert.SerializeObject(command, new JsonSerializerSettings
            {
                ContractResolver = new AllPropertiesContractResolver()
            })
        };

        _context.InternalCommands.Add(internalCommand);
        
        await _context.SaveChangesAsync();
    }

    public async Task EnqueueAsync<T>(ICommand<T> command)
    {
        // Same as above
    }
}
```

#### 4. Processing Inbox → Internal Commands

```csharp
internal class ProcessInboxJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var inboxMessages = await GetUnprocessedInboxMessages();

        foreach (var inboxMessage in inboxMessages)
        {
            var integrationEvent = DeserializeEvent(inboxMessage);

            // Convert integration event to internal command
            var internalCommand = MapToInternalCommand(integrationEvent);

            // Schedule for processing
            await _commandsScheduler.EnqueueAsync(internalCommand);

            // Mark inbox message as processed
            inboxMessage.ProcessedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private ICommand MapToInternalCommand(IntegrationEvent @event)
    {
        return @event switch
        {
            MeetingGroupProposalAcceptedIntegrationEvent e => 
                new CreateMeetingGroupCommand(
                    Guid.NewGuid(),
                    e.MeetingGroupId,
                    e.Name,
                    e.Description),
            
            SubscriptionExpirationDateChangedIntegrationEvent e =>
                new UpdateMeetingGroupSubscriptionCommand(
                    Guid.NewGuid(),
                    e.PayerId,
                    e.ExpirationDate),
                    
            _ => null
        };
    }
}
```

#### 5. Processing Internal Commands

```csharp
internal class ProcessInternalCommandsJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var internalCommands = await _context.InternalCommands
            .Where(x => x.ProcessedDate == null)
            .OrderBy(x => x.Id)
            .Take(50)
            .ToListAsync();

        foreach (var internalCommand in internalCommands)
        {
            try
            {
                var command = DeserializeCommand(internalCommand);
                
                // Execute through command pipeline (gets all decorators)
                await _mediator.Send(command);
                
                // Marking as processed happens in UnitOfWorkDecorator
            }
            catch (Exception ex)
            {
                // Log error but don't mark as processed (will retry)
                _logger.Error(ex, 
                    "Error processing internal command {CommandId} of type {CommandType}", 
                    internalCommand.Id, 
                    internalCommand.Type);
                    
                internalCommand.Error = ex.ToString();
                await _context.SaveChangesAsync();
            }
        }
    }
}
```

#### 6. Unit of Work Decorator Integration

```csharp
public class UnitOfWorkCommandHandlerDecorator<T> : ICommandHandler<T> where T : ICommand
{
    public async Task Handle(T command, CancellationToken cancellationToken)
    {
        await _decorated.Handle(command, cancellationToken);

        // Mark internal command as processed
        if (command is InternalCommandBase)
        {
            var internalCommand = await _context.InternalCommands
                .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

            if (internalCommand != null)
            {
                internalCommand.ProcessedDate = DateTime.UtcNow;
            }
        }

        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

### Key Characteristics

1. **Same Pipeline**: Internal commands go through same pipeline as API commands
   - Validation decorator
   - Logging decorator
   - Unit of work decorator
   - Any other decorators

2. **Transactional**: Each stage is a separate transaction
   - Inbox → InternalCommands (Transaction 1)
   - InternalCommand → Domain changes (Transaction 2)

3. **Idempotent**: Commands can be retried safely
   - Handler checks if work already done
   - Command marked as processed only on success

4. **Traceable**: Full logging via decorators
   - Each command has unique ID
   - Correlation to original integration event

## Consequences

### Positive

- **Consistency**: All state changes go through commands
- **Validation**: Internal commands validated same as API commands
- **Logging**: Automatic logging via decorators
- **Retry**: Failed commands automatically retried
- **Testability**: Easy to test command handlers in isolation
- **Auditability**: Full record of all internal commands
- **Separation of Concerns**: Event handling separated from business logic
- **Eventual Consistency**: Clear, intentional async processing
- **Error Handling**: Centralized error handling in background job

### Negative

- **Eventual Consistency**: Not immediate processing (2 second delay)
- **Complexity**: More moving parts than direct processing
- **Storage**: InternalCommands table grows (needs cleanup)
- **Latency**: Multi-step processing adds latency
- **Debugging**: Harder to trace event → command → effect

### Use Cases

Internal commands are used for:

✅ **Integration Event Processing**: 
```csharp
MeetingGroupProposalAccepted → CreateMeetingGroupCommand
```

✅ **Scheduled Operations**: 
```csharp
// Expire subscriptions daily
ExpireSubscriptionsCommand
```

✅ **Saga Coordination**: 
```csharp
// Multi-step processes
Step1Command → Step2Command → Step3Command
```

✅ **Retry Logic**: 
```csharp
// Failed operations automatically retried
SendEmailCommand (with retry)
```

### NOT Used For

❌ API Requests: Direct command execution via MediatR
❌ Domain Events within Module: Processed synchronously in same transaction
❌ Queries: Only commands, never queries

### Cleanup Strategy

```csharp
// Delete processed commands older than 30 days
DELETE FROM [app].[InternalCommands]
WHERE [ProcessedDate] IS NOT NULL 
  AND [ProcessedDate] < DATEADD(day, -30, GETUTCDATE());
```

### Error Handling

**Transient Errors**: Automatic retry (Quartz.NET)

**Poison Commands**: 
- Add retry count tracking
- Move to dead letter after N attempts
- Manual intervention required

```csharp
// Enhanced error tracking
public class InternalCommand
{
    public int RetryCount { get; set; }
    public DateTime? LastRetryDate { get; set; }
    public string Error { get; set; }
}

// In processing job
if (internalCommand.RetryCount > 10)
{
    // Move to dead letter
    _logger.Error("Command {CommandId} failed after 10 retries", internalCommand.Id);
    // Notify operations team
    continue;
}
```

### Monitoring

Track these metrics:
- Internal commands processed per minute
- Failed internal commands
- Average processing time
- Queue depth (unprocessed commands)

## References

- [Event Storming - Alberto Brandolini](https://www.eventstorming.com/)
- [The picture that explains "almost" everything](https://xebia.com/blog/eventstorming-cheat-sheet/)
- Section 3.8 "Internal Processing" in project README
