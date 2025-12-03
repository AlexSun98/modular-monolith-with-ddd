# ADR-0023: Decorator Pattern for Cross-Cutting Concerns

## Status

Accepted

## Context

In our CQRS architecture, every business operation is handled by a Command Handler or Query Handler. We need to implement cross-cutting concerns that apply to all handlers:

1. **Logging**: Log all command/query executions with context
2. **Validation**: Validate command/query data before processing
3. **Unit of Work**: Manage transactions and commits
4. **Error Handling**: Catch and handle exceptions consistently
5. **Performance Monitoring**: Measure execution time
6. **Caching**: Cache query results (future)
7. **Authorization**: Check permissions (future, if needed at handler level)

### Requirements

- **Don't Repeat Yourself (DRY)**: Implement once, apply everywhere
- **Single Responsibility Principle (SRP)**: Each handler focuses on business logic only
- **Open/Closed Principle (OCP)**: Add new concerns without modifying handlers
- **Separation of Concerns**: Infrastructure code separate from business logic
- **Maintainability**: Easy to add, remove, or modify concerns
- **Testability**: Test handlers without infrastructure concerns

### Considered Alternatives

1. **Base Class with Template Method**
   ```csharp
   public abstract class CommandHandlerBase<T>
   {
       public async Task Handle(T command)
       {
           Log(command);
           Validate(command);
           await ExecuteCore(command);  // Abstract method
           Commit();
       }
   }
   ```
   - Pros: Simple, familiar pattern
   - Cons: Single inheritance limitation, tight coupling, hard to reorder concerns

2. **Aspect-Oriented Programming (AOP)**
   - Using frameworks like PostSharp
   - Pros: Powerful, clean business code
   - Cons: Complex, magic, debugging harder, additional dependency

3. **Decorator Pattern**
   - Wrap handlers with decorators
   - Pros: Flexible, explicit, easy to test, standard OOP
   - Cons: More classes, requires DI container support

4. **MediatR Pipeline Behaviors**
   ```csharp
   public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
   ```
   - Pros: Built into MediatR, simple
   - Cons: Tied to MediatR, less control over ordering

## Decision

We will use the **Decorator Pattern** implemented through our IoC container (Autofac) to handle cross-cutting concerns.

### Architecture

```
API Request
    |
    v
[MediatR]
    |
    v
[LoggingDecorator]
    |
    v
[ValidationDecorator]
    |
    v
[UnitOfWorkDecorator]
    |
    v
[Actual Handler]
    |
    v
Domain Logic
```

### Implementation

#### 1. Logging Decorator

Logs command execution with context and timing:

```csharp
internal class LoggingCommandHandlerDecorator<T> : ICommandHandler<T> where T : ICommand
{
    private readonly ILogger _logger;
    private readonly IExecutionContextAccessor _executionContextAccessor;
    private readonly ICommandHandler<T> _decorated;

    public LoggingCommandHandlerDecorator(
        ILogger logger,
        IExecutionContextAccessor executionContextAccessor,
        ICommandHandler<T> decorated)
    {
        _logger = logger;
        _executionContextAccessor = executionContextAccessor;
        _decorated = decorated;
    }

    public async Task Handle(T command, CancellationToken cancellationToken)
    {
        if (command is IRecurringCommand)
        {
            return await _decorated.Handle(command, cancellationToken);
        }

        using (
            LogContext.Push(
                new RequestLogEnricher(_executionContextAccessor),
                new CommandLogEnricher(command)))
        {
            try
            {
                _logger.Information("Executing command {Command}", command.GetType().Name);

                await _decorated.Handle(command, cancellationToken);

                _logger.Information("Command {Command} processed successful", command.GetType().Name);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Command {Command} processing failed", command.GetType().Name);
                throw;
            }
        }
    }

    private class CommandLogEnricher : ILogEventEnricher
    {
        private readonly ICommand _command;

        public CommandLogEnricher(ICommand command)
        {
            _command = command;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddOrUpdateProperty(
                new LogEventProperty("Context", 
                    new ScalarValue($"Command:{_command.Id}")));
        }
    }
}
```

Sample log output:
```
2024-01-15 10:23:45 [INF] Executing command CreateMeetingCommand [CorrelationId: abc-123, Context: Command:def-456]
2024-01-15 10:23:45 [INF] Command CreateMeetingCommand processed successful [CorrelationId: abc-123, Context: Command:def-456]
```

#### 2. Validation Decorator

Validates commands using FluentValidation:

```csharp
internal class ValidationCommandHandlerDecorator<T> : ICommandHandler<T> where T : ICommand
{
    private readonly IList<IValidator<T>> _validators;
    private readonly ICommandHandler<T> _decorated;

    public ValidationCommandHandlerDecorator(
        IList<IValidator<T>> validators,
        ICommandHandler<T> decorated)
    {
        _validators = validators;
        _decorated = decorated;
    }

    public Task Handle(T command, CancellationToken cancellationToken)
    {
        var errors = _validators
            .Select(v => v.Validate(command))
            .SelectMany(result => result.Errors)
            .Where(error => error != null)
            .ToList();

        if (errors.Any())
        {
            var errorBuilder = new StringBuilder();
            errorBuilder.AppendLine("Invalid command, reason: ");

            foreach (var error in errors)
            {
                errorBuilder.AppendLine(error.ErrorMessage);
            }

            throw new InvalidCommandException(errorBuilder.ToString(), null);
        }

        return _decorated.Handle(command, cancellationToken);
    }
}
```

Example validator:

```csharp
internal class CreateMeetingCommandValidator : AbstractValidator<CreateMeetingCommand>
{
    public CreateMeetingCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Meeting title is required");
        RuleFor(x => x.Title).MaximumLength(200).WithMessage("Meeting title is too long");
        RuleFor(x => x.TermStartDate).Must(BeInFuture).WithMessage("Meeting must be in the future");
        RuleFor(x => x.AttendeesLimit).GreaterThan(0).When(x => x.AttendeesLimit.HasValue);
    }

    private bool BeInFuture(DateTime date)
    {
        return date > DateTime.UtcNow;
    }
}
```

#### 3. Unit of Work Decorator

Manages transactions and commits:

```csharp
public class UnitOfWorkCommandHandlerDecorator<T> : ICommandHandler<T> where T : ICommand
{
    private readonly ICommandHandler<T> _decorated;
    private readonly IUnitOfWork _unitOfWork;
    private readonly MeetingsContext _context;

    public UnitOfWorkCommandHandlerDecorator(
        ICommandHandler<T> decorated,
        IUnitOfWork unitOfWork,
        MeetingsContext context)
    {
        _decorated = decorated;
        _unitOfWork = unitOfWork;
        _context = context;
    }

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

        // Commit transaction
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

#### 4. Registration with Autofac

Decorators are registered in order (outermost to innermost):

```csharp
public class MeetingsAutofacModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register all command handlers
        builder.RegisterAssemblyTypes(typeof(CreateMeetingCommandHandler).Assembly)
            .AsClosedTypesOf(typeof(ICommandHandler<>))
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        // Register decorators in order
        builder.RegisterGenericDecorator(
            typeof(LoggingCommandHandlerDecorator<>),
            typeof(ICommandHandler<>));

        builder.RegisterGenericDecorator(
            typeof(ValidationCommandHandlerDecorator<>),
            typeof(ICommandHandler<>));

        builder.RegisterGenericDecorator(
            typeof(UnitOfWorkCommandHandlerDecorator<>),
            typeof(ICommandHandler<>));
    }
}
```

### Decorator Ordering

The order matters and is intentional:

1. **Logging** (outermost)
   - Logs everything, including validation failures
   - Provides full context for debugging

2. **Validation** 
   - Fails fast before business logic
   - Logged by logging decorator

3. **Unit of Work** (innermost, closest to handler)
   - Only commits if handler succeeds
   - Only commits if validation passed

### Benefits Demonstrated

**Handler Without Decorators** (what we'd need without pattern):

```csharp
public class CreateMeetingCommandHandler : ICommandHandler<CreateMeetingCommand>
{
    public async Task Handle(CreateMeetingCommand command, CancellationToken cancellationToken)
    {
        // ❌ All this infrastructure code in every handler!
        _logger.Information("Executing command CreateMeetingCommand");
        
        // Validation
        if (string.IsNullOrEmpty(command.Title))
            throw new InvalidCommandException("Title is required");
        if (command.TermStartDate < DateTime.UtcNow)
            throw new InvalidCommandException("Meeting must be in future");
        
        try
        {
            // Business logic
            var meetingGroup = await _repository.GetByIdAsync(command.MeetingGroupId);
            var meeting = meetingGroup.CreateMeeting(/* ... */);
            await _repository.AddAsync(meeting);
            
            // Transaction management
            await _unitOfWork.CommitAsync();
            
            _logger.Information("Command CreateMeetingCommand processed successful");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Command CreateMeetingCommand processing failed");
            throw;
        }
    }
}
```

**Handler With Decorators** (clean business logic):

```csharp
public class CreateMeetingCommandHandler : ICommandHandler<CreateMeetingCommand>
{
    public async Task Handle(CreateMeetingCommand command, CancellationToken cancellationToken)
    {
        // ✅ Only business logic!
        var meetingGroup = await _repository.GetByIdAsync(command.MeetingGroupId);
        
        var meeting = meetingGroup.CreateMeeting(
            command.Title,
            new MeetingTerm(command.TermStartDate, command.TermEndDate),
            command.Description,
            new MeetingLocation(command.LocationName, command.LocationAddress, /* ... */),
            command.AttendeesLimit,
            command.GuestsLimit,
            /* ... */);
        
        await _repository.AddAsync(meeting);
        
        // Commit happens in UnitOfWorkDecorator
        // Logging happens in LoggingDecorator
        // Validation happened in ValidationDecorator
    }
}
```

## Consequences

### Positive

- **Single Responsibility**: Each handler focuses only on business logic
- **DRY**: Cross-cutting concerns implemented once
- **Separation of Concerns**: Infrastructure separate from domain
- **Testability**: Test handlers without infrastructure
  ```csharp
  var handler = new CreateMeetingCommandHandler(repository, ...);
  await handler.Handle(command);  // No decorators in unit test
  ```
- **Flexibility**: Easy to add/remove/reorder decorators
- **Explicit**: Clear what happens to each command
- **Standard OOP**: No magic, no framework-specific attributes
- **Reusability**: Same decorators for all modules

### Negative

- **More Classes**: Each concern requires a decorator class
- **IoC Container Dependency**: Requires container that supports decoration
- **Complexity**: New developers need to understand decorator chain
- **Debugging**: Stack traces are deeper
- **Performance**: Slight overhead from additional method calls (negligible)

### When to Add New Decorator

Add a decorator when:
- ✅ Concern applies to all (or most) commands
- ✅ Concern is infrastructure, not business logic
- ✅ Concern should execute in specific order
- ✅ Concern is reusable across modules

Don't add decorator for:
- ❌ Business logic (belongs in handler or domain)
- ❌ One-off concerns (handle in handler)
- ❌ Module-specific concerns (use different approach)

### Example: Adding Caching Decorator

```csharp
internal class CachingQueryHandlerDecorator<TQuery, TResult> : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    private readonly IQueryHandler<TQuery, TResult> _decorated;
    private readonly ICache _cache;

    public async Task<TResult> Handle(TQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = GenerateCacheKey(query);
        
        if (_cache.TryGet(cacheKey, out TResult cachedResult))
        {
            return cachedResult;
        }

        var result = await _decorated.Handle(query, cancellationToken);
        
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        
        return result;
    }
}

// Register
builder.RegisterGenericDecorator(
    typeof(CachingQueryHandlerDecorator<,>),
    typeof(IQueryHandler<,>));
```

### Testing

**Unit Testing** (without decorators):
```csharp
[Test]
public async Task CreateMeeting_ValidCommand_CreatesMeeting()
{
    // Arrange
    var handler = new CreateMeetingCommandHandler(_repository, ...);
    var command = new CreateMeetingCommand(/* ... */);
    
    // Act
    await handler.Handle(command, CancellationToken.None);
    
    // Assert
    // Verify business logic only
}
```

**Integration Testing** (with decorators):
```csharp
[Test]
public async Task CreateMeeting_InvalidCommand_ThrowsValidationException()
{
    // Arrange
    var command = new CreateMeetingCommand { Title = "" };  // Invalid
    
    // Act & Assert
    Assert.ThrowsAsync<InvalidCommandException>(
        () => _module.ExecuteCommandAsync(command));
    
    // Validation decorator catches this
}
```

## References

- [Decorator Pattern - Gang of Four](https://en.wikipedia.org/wiki/Decorator_pattern)
- [Cross-Cutting Concerns in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/)
- [Autofac Decorators Documentation](https://autofac.readthedocs.io/en/latest/advanced/decorators.html)
- Section 3.6 "Cross-Cutting Concerns" in project README
