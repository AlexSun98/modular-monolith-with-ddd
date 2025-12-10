# Adding a New Feature Guide

This guide describes how to add a new feature to an existing module in the Modular Monolith with DDD application.

## Overview

Each feature typically involves:
1. **Command/Query** - Request object
2. **Handler** - Business logic orchestrator
3. **Domain changes** - Aggregate/Entity modifications (for Commands)
4. **Database changes** - Schema updates (if needed)
5. **API endpoint** - Controller action

## Step-by-Step Process

### 1. Define the Command or Query

**For Write Operations (Commands):**

```csharp
// Location: Modules/{ModuleName}/Application/{Feature}/Commands/{CommandName}Command.cs
public class CreateMeetingCommand : CommandBase<Guid>
{
    public Guid MeetingGroupId { get; }
    public string Title { get; }
    // ... other properties

    public CreateMeetingCommand(Guid meetingGroupId, string title, /* ... */)
    {
        MeetingGroupId = meetingGroupId;
        Title = title;
        // ...
    }
}
```

**For Read Operations (Queries):**

```csharp
// Location: Modules/{ModuleName}/Application/{Feature}/Queries/{QueryName}Query.cs
public class GetMeetingDetailsQuery : QueryBase<MeetingDetailsDto>
{
    public Guid MeetingId { get; }

    public GetMeetingDetailsQuery(Guid meetingId)
    {
        MeetingId = meetingId;
    }
}
```

### 2. Create the Handler

**Command Handler (uses Domain Model):**

```csharp
// Location: Modules/{ModuleName}/Application/{Feature}/Commands/{CommandName}CommandHandler.cs
internal class CreateMeetingCommandHandler : ICommandHandler<CreateMeetingCommand, Guid>
{
    private readonly IMeetingGroupRepository _meetingGroupRepository;
    private readonly IMeetingRepository _meetingRepository;

    internal CreateMeetingCommandHandler(
        IMeetingGroupRepository meetingGroupRepository,
        IMeetingRepository meetingRepository)
    {
        _meetingGroupRepository = meetingGroupRepository;
        _meetingRepository = meetingRepository;
    }

    public async Task<Guid> Handle(CreateMeetingCommand command, CancellationToken cancellationToken)
    {
        // 1. Load Aggregate
        var meetingGroup = await _meetingGroupRepository.GetByIdAsync(command.MeetingGroupId);

        // 2. Call Domain Method (business logic lives here!)
        var meeting = meetingGroup.CreateMeeting(
            command.Title,
            // ... other parameters
        );

        // 3. Save via Repository
        await _meetingRepository.AddAsync(meeting);

        return meeting.Id;
    }
}
```

**Query Handler (uses raw SQL/Dapper):**

```csharp
// Location: Modules/{ModuleName}/Application/{Feature}/Queries/{QueryName}QueryHandler.cs
internal class GetMeetingDetailsQueryHandler : IQueryHandler<GetMeetingDetailsQuery, MeetingDetailsDto>
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    internal GetMeetingDetailsQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    public async Task<MeetingDetailsDto> Handle(GetMeetingDetailsQuery query, CancellationToken cancellationToken)
    {
        var connection = _sqlConnectionFactory.GetOpenConnection();

        const string sql = $"""
            SELECT 
                [Meeting].[Id] as [{nameof(MeetingDetailsDto.Id)}],
                [Meeting].[Title] as [{nameof(MeetingDetailsDto.Title)}]
                -- ... other columns
            FROM [meetings].[v_MeetingDetails] AS [Meeting]
            WHERE [Meeting].[Id] = @MeetingId
            """;

        return await connection.QuerySingleOrDefaultAsync<MeetingDetailsDto>(sql, new { query.MeetingId });
    }
}
```

### 3. Add Command Validation (Optional but Recommended)

```csharp
// Location: Modules/{ModuleName}/Application/{Feature}/Commands/{CommandName}CommandValidator.cs
internal class CreateMeetingCommandValidator : AbstractValidator<CreateMeetingCommand>
{
    public CreateMeetingCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MeetingGroupId).NotEmpty();
    }
}
```

### 4. Update Domain Model (For Commands)

Add methods to existing Aggregates or create new Entities:

```csharp
// Location: Modules/{ModuleName}/Domain/{AggregateName}/{AggregateName}.cs
public class MeetingGroup : Entity, IAggregateRoot
{
    // ... existing code

    public Meeting CreateMeeting(
        string title,
        MeetingTerm term,
        // ... other parameters
    )
    {
        // Check business rules
        this.CheckRule(new MeetingCanBeOrganizedOnlyByPayedGroupRule(_paymentDateTo));
        this.CheckRule(new MeetingHostMustBeAMeetingGroupMemberRule(creatorId, hostsMembersIds, _members));

        // Create and return new entity
        return new Meeting(
            this.Id,
            title,
            term,
            // ...
        );
    }
}
```

### 5. Create DTO (For Queries)

```csharp
// Location: Modules/{ModuleName}/Application/{Feature}/{DtoName}Dto.cs
public class MeetingDetailsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    // ... other properties (flat structure, no domain objects!)
}
```

### 6. Add API Endpoint

```csharp
// Location: API/Modules/{ModuleName}/{Feature}Controller.cs
[Route("api/meetings")]
[ApiController]
public class MeetingsController : ControllerBase
{
    private readonly IMeetingsModule _meetingsModule;

    public MeetingsController(IMeetingsModule meetingsModule)
    {
        _meetingsModule = meetingsModule;
    }

    [HttpPost]
    [HasPermission(MeetingsPermissions.CreateMeeting)]
    public async Task<IActionResult> CreateMeeting([FromBody] CreateMeetingRequest request)
    {
        var meetingId = await _meetingsModule.ExecuteCommandAsync(
            new CreateMeetingCommand(
                request.MeetingGroupId,
                request.Title,
                // ...
            ));

        return Ok(meetingId);
    }

    [HttpGet("{meetingId}")]
    [HasPermission(MeetingsPermissions.GetMeetingDetails)]
    public async Task<IActionResult> GetMeetingDetails([FromRoute] Guid meetingId)
    {
        var meeting = await _meetingsModule.ExecuteQueryAsync(new GetMeetingDetailsQuery(meetingId));
        return Ok(meeting);
    }
}
```

## Key Rules to Follow

### DO:
- ✅ Keep business logic in Domain layer (Aggregates/Entities)
- ✅ Use raw SQL with Dapper for Queries (read from Views when possible)
- ✅ Use Entity Framework for Commands (write model)
- ✅ Create flat DTOs for query results
- ✅ Add validation using FluentValidation
- ✅ Check permissions at Controller level using `[HasPermission]`

### DON'T:
- ❌ Put business logic in Command Handlers
- ❌ Return Domain objects from Queries
- ❌ Call other modules directly (use Integration Events)
- ❌ Share data between modules
- ❌ Skip validation

## File Locations Summary

| Component | Location |
|-----------|----------|
| Command | `Modules/{Module}/Application/{Feature}/Commands/` |
| Query | `Modules/{Module}/Application/{Feature}/Queries/` |
| Handler | Same folder as Command/Query |
| Validator | Same folder as Command |
| DTO | `Modules/{Module}/Application/{Feature}/` |
| Domain Entity | `Modules/{Module}/Domain/{AggregateName}/` |
| Business Rule | `Modules/{Module}/Domain/{AggregateName}/Rules/` |
| Repository Interface | `Modules/{Module}/Domain/{AggregateName}/` |
| Repository Implementation | `Modules/{Module}/Infrastructure/Domain/{AggregateName}/` |
| EF Configuration | `Modules/{Module}/Infrastructure/Domain/{AggregateName}/` |
| Controller | `API/Modules/{Module}/` |
