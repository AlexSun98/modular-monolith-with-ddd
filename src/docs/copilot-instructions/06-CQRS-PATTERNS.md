# CQRS Patterns Guide

This guide describes the Command Query Responsibility Segregation (CQRS) patterns used throughout the application.

## Overview

CQRS separates read and write operations:

| Aspect | Commands (Write) | Queries (Read) |
|--------|-----------------|----------------|
| Purpose | Change state | Return data |
| Model | Domain Model (DDD) | DTOs/Read Models |
| Data Access | Entity Framework | Dapper/Raw SQL |
| Source | Tables | Views (preferred) |
| Return | ID or void | DTO objects |

## Commands

### Command Definition

```csharp
// Location: Modules/{Module}/Application/{Feature}/Commands/{Command}Command.cs

// Command with result
public class CreateMeetingCommand : CommandBase<Guid>
{
    public Guid MeetingGroupId { get; }
    public string Title { get; }
    public string Description { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    public CreateMeetingCommand(
        Guid meetingGroupId,
        string title,
        string description,
        DateTime startDate,
        DateTime endDate)
    {
        MeetingGroupId = meetingGroupId;
        Title = title;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
    }
}

// Command without result (returns Unit)
public class DeleteMeetingCommand : CommandBase
{
    public Guid MeetingId { get; }

    public DeleteMeetingCommand(Guid meetingId)
    {
        MeetingId = meetingId;
    }
}
```

### Command Base Classes

```csharp
// For commands that return a result
public abstract class CommandBase<TResult> : ICommand<TResult>
{
    public Guid Id { get; }

    protected CommandBase()
    {
        Id = Guid.NewGuid();
    }

    protected CommandBase(Guid id)
    {
        Id = id;
    }
}

// For commands that don't return a result
public abstract class CommandBase : ICommand
{
    public Guid Id { get; }

    protected CommandBase()
    {
        Id = Guid.NewGuid();
    }

    protected CommandBase(Guid id)
    {
        Id = id;
    }
}
```

### Command Handler

```csharp
// Location: Modules/{Module}/Application/{Feature}/Commands/{Command}CommandHandler.cs
internal class CreateMeetingCommandHandler : ICommandHandler<CreateMeetingCommand, Guid>
{
    private readonly IMeetingGroupRepository _meetingGroupRepository;
    private readonly IMeetingRepository _meetingRepository;
    private readonly IMemberContext _memberContext;

    internal CreateMeetingCommandHandler(
        IMeetingGroupRepository meetingGroupRepository,
        IMeetingRepository meetingRepository,
        IMemberContext memberContext)
    {
        _meetingGroupRepository = meetingGroupRepository;
        _meetingRepository = meetingRepository;
        _memberContext = memberContext;
    }

    public async Task<Guid> Handle(CreateMeetingCommand command, CancellationToken cancellationToken)
    {
        // 1. Load Aggregate(s)
        var meetingGroup = await _meetingGroupRepository.GetByIdAsync(
            new MeetingGroupId(command.MeetingGroupId));

        // 2. Execute Domain Logic
        var meeting = meetingGroup.CreateMeeting(
            command.Title,
            new MeetingTerm(command.StartDate, command.EndDate),
            command.Description,
            // ... other parameters
            _memberContext.MemberId);

        // 3. Persist
        await _meetingRepository.AddAsync(meeting);

        // 4. Return result
        return meeting.Id.Value;
    }
}
```

### Command Validation

```csharp
// Location: Modules/{Module}/Application/{Feature}/Commands/{Command}CommandValidator.cs
internal class CreateMeetingCommandValidator : AbstractValidator<CreateMeetingCommand>
{
    public CreateMeetingCommandValidator()
    {
        RuleFor(x => x.MeetingGroupId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.StartDate).GreaterThan(DateTime.UtcNow);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
    }
}
```

### Internal Commands (Background Processing)

```csharp
// For commands processed in background (from Integration Events, scheduled tasks)
internal class ProcessIntegrationEventCommand : InternalCommandBase
{
    public Guid IntegrationEventId { get; }
    public string EventType { get; }
    public string EventData { get; }

    [JsonConstructor]
    public ProcessIntegrationEventCommand(
        Guid id,
        Guid integrationEventId,
        string eventType,
        string eventData)
        : base(id)
    {
        IntegrationEventId = integrationEventId;
        EventType = eventType;
        EventData = eventData;
    }
}
```

## Queries

### Query Definition

```csharp
// Location: Modules/{Module}/Application/{Feature}/Queries/{Query}Query.cs
public class GetMeetingDetailsQuery : QueryBase<MeetingDetailsDto>
{
    public Guid MeetingId { get; }

    public GetMeetingDetailsQuery(Guid meetingId)
    {
        MeetingId = meetingId;
    }
}

public class GetAllMeetingGroupsQuery : QueryBase<List<MeetingGroupDto>>
{
}

public class GetMemberMeetingsQuery : QueryBase<List<MeetingDto>>
{
    public Guid MemberId { get; }
    public int PageNumber { get; }
    public int PageSize { get; }

    public GetMemberMeetingsQuery(Guid memberId, int pageNumber = 1, int pageSize = 20)
    {
        MemberId = memberId;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
```

### Query Handler (using Dapper)

```csharp
// Location: Modules/{Module}/Application/{Feature}/Queries/{Query}QueryHandler.cs
internal class GetMeetingDetailsQueryHandler : IQueryHandler<GetMeetingDetailsQuery, MeetingDetailsDto>
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    internal GetMeetingDetailsQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    public async Task<MeetingDetailsDto> Handle(
        GetMeetingDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var connection = _sqlConnectionFactory.GetOpenConnection();

        const string sql = $"""
            SELECT 
                [Meeting].[Id] AS [{nameof(MeetingDetailsDto.Id)}],
                [Meeting].[Title] AS [{nameof(MeetingDetailsDto.Title)}],
                [Meeting].[Description] AS [{nameof(MeetingDetailsDto.Description)}],
                [Meeting].[LocationName] AS [{nameof(MeetingDetailsDto.LocationName)}],
                [Meeting].[LocationAddress] AS [{nameof(MeetingDetailsDto.LocationAddress)}],
                [Meeting].[LocationCity] AS [{nameof(MeetingDetailsDto.LocationCity)}],
                [Meeting].[StartDate] AS [{nameof(MeetingDetailsDto.StartDate)}],
                [Meeting].[EndDate] AS [{nameof(MeetingDetailsDto.EndDate)}],
                [Meeting].[AttendeesCount] AS [{nameof(MeetingDetailsDto.AttendeesCount)}],
                [Meeting].[AttendeesLimit] AS [{nameof(MeetingDetailsDto.AttendeesLimit)}]
            FROM [meetings].[v_MeetingDetails] AS [Meeting]
            WHERE [Meeting].[Id] = @MeetingId
            """;

        return await connection.QuerySingleOrDefaultAsync<MeetingDetailsDto>(
            sql,
            new { query.MeetingId });
    }
}
```

### Query with Pagination

```csharp
internal class GetMemberMeetingsQueryHandler : IQueryHandler<GetMemberMeetingsQuery, List<MeetingDto>>
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    internal GetMemberMeetingsQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    public async Task<List<MeetingDto>> Handle(
        GetMemberMeetingsQuery query,
        CancellationToken cancellationToken)
    {
        var connection = _sqlConnectionFactory.GetOpenConnection();

        var offset = (query.PageNumber - 1) * query.PageSize;

        const string sql = $"""
            SELECT 
                [Meeting].[Id] AS [{nameof(MeetingDto.Id)}],
                [Meeting].[Title] AS [{nameof(MeetingDto.Title)}],
                [Meeting].[StartDate] AS [{nameof(MeetingDto.StartDate)}]
            FROM [meetings].[v_MemberMeetings] AS [Meeting]
            WHERE [Meeting].[MemberId] = @MemberId
            ORDER BY [Meeting].[StartDate] DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY
            """;

        var meetings = await connection.QueryAsync<MeetingDto>(
            sql,
            new { query.MemberId, Offset = offset, query.PageSize });

        return meetings.AsList();
    }
}
```

### Query with Multiple Result Sets

```csharp
internal class GetMeetingWithAttendeesQueryHandler 
    : IQueryHandler<GetMeetingWithAttendeesQuery, MeetingWithAttendeesDto>
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    internal GetMeetingWithAttendeesQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    public async Task<MeetingWithAttendeesDto> Handle(
        GetMeetingWithAttendeesQuery query,
        CancellationToken cancellationToken)
    {
        var connection = _sqlConnectionFactory.GetOpenConnection();

        const string sql = """
            -- Meeting details
            SELECT [Id], [Title], [Description], [StartDate], [EndDate]
            FROM [meetings].[v_Meetings]
            WHERE [Id] = @MeetingId;

            -- Attendees
            SELECT [MemberId], [FirstName], [LastName], [Role]
            FROM [meetings].[v_MeetingAttendees]
            WHERE [MeetingId] = @MeetingId;
            """;

        using var multi = await connection.QueryMultipleAsync(sql, new { query.MeetingId });

        var meeting = await multi.ReadSingleOrDefaultAsync<MeetingWithAttendeesDto>();
        if (meeting != null)
        {
            meeting.Attendees = (await multi.ReadAsync<AttendeeDto>()).AsList();
        }

        return meeting;
    }
}
```

## DTOs (Data Transfer Objects)

### DTO Design Rules

```csharp
// Location: Modules/{Module}/Application/{Feature}/{Dto}Dto.cs

// ✅ GOOD: Flat structure, simple types
public class MeetingDetailsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string LocationName { get; set; }
    public string LocationAddress { get; set; }
    public string LocationCity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int AttendeesCount { get; set; }
    public int? AttendeesLimit { get; set; }
}

// ❌ BAD: Don't return domain objects
public class BadMeetingDto
{
    public Meeting Meeting { get; set; }  // Don't do this!
    public MeetingGroup MeetingGroup { get; set; }  // Don't do this!
}

// ✅ GOOD: Nested DTOs are OK for composed data
public class MeetingWithAttendeesDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public DateTime StartDate { get; set; }
    public List<AttendeeDto> Attendees { get; set; }
}

public class AttendeeDto
{
    public Guid MemberId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Role { get; set; }
}
```

## Command Handler Decorators

The pipeline applies decorators in this order:

```
Request → Validation → Logging → UnitOfWork → Handler → Response
```

### Validation Decorator

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

### Unit of Work Decorator

```csharp
public class UnitOfWorkCommandHandlerDecorator<T> : ICommandHandler<T> where T : ICommand
{
    private readonly ICommandHandler<T> _decorated;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ModuleContext _moduleContext;

    public UnitOfWorkCommandHandlerDecorator(
        ICommandHandler<T> decorated,
        IUnitOfWork unitOfWork,
        ModuleContext moduleContext)
    {
        _decorated = decorated;
        _unitOfWork = unitOfWork;
        _moduleContext = moduleContext;
    }

    public async Task Handle(T command, CancellationToken cancellationToken)
    {
        await _decorated.Handle(command, cancellationToken);

        // Mark internal command as processed
        if (command is InternalCommandBase)
        {
            var internalCommand = await _moduleContext.InternalCommands
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

## Database Views for Queries

### Create Views for Read Model

```sql
-- Location: Database/Migrations/{timestamp}_CreateMeetingViews.sql

CREATE VIEW [meetings].[v_MeetingDetails]
AS
SELECT 
    [M].[Id],
    [M].[Title],
    [M].[Description],
    [M].[LocationName],
    [M].[LocationAddress],
    [M].[LocationPostalCode],
    [M].[LocationCity],
    [M].[TermStartDate] AS [StartDate],
    [M].[TermEndDate] AS [EndDate],
    [M].[AttendeesLimit],
    (SELECT COUNT(*) FROM [meetings].[MeetingAttendees] WHERE [MeetingId] = [M].[Id]) AS [AttendeesCount],
    [MG].[Name] AS [MeetingGroupName]
FROM [meetings].[Meetings] AS [M]
INNER JOIN [meetings].[MeetingGroups] AS [MG] ON [M].[MeetingGroupId] = [MG].[Id];
GO
```

## Best Practices Summary

### Commands
- ✅ Use domain model for business logic
- ✅ Load aggregates via repositories
- ✅ Return only ID (or void) from commands
- ✅ Add validation using FluentValidation
- ❌ Don't return complex objects from commands
- ❌ Don't put business logic in handlers

### Queries
- ✅ Use raw SQL with Dapper
- ✅ Query from Views when possible
- ✅ Return flat DTOs
- ✅ Support pagination for list queries
- ❌ Don't return domain objects
- ❌ Don't use Entity Framework for queries
