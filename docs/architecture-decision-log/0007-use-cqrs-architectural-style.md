# ADR-0007: Implement CQRS Pattern

## Status

Accepted

## Context

In traditional CRUD applications, the same model is used for both reading and writing data. As our system grows, we face several challenges:

1. **Different Optimization Needs**: Reads and writes have different performance characteristics
   - Writes need validation, business rules, transactions
   - Reads need speed, denormalization, specific projections

2. **Complexity**: Using rich domain models for queries adds unnecessary complexity
   - Domain model focuses on behavior and invariants
   - Queries often just need simple data transfer

3. **Scalability**: Read and write operations have different scaling requirements
   - Reads are typically 90%+ of operations
   - Writes are less frequent but more complex

4. **Model Mismatch**: UI needs don't always match domain model structure
   - Reports and dashboards need specific data shapes
   - Domain model is optimized for behavior, not reporting

## Decision

We will implement the **Command Query Responsibility Segregation (CQRS)** pattern to separate read and write operations.

### Write Model (Commands)

Commands will use the **Domain Model** implemented with DDD tactical patterns:

- **Aggregates**: Enforce business rules and invariants
- **Entities and Value Objects**: Rich domain model
- **Domain Events**: Capture state changes
- **Repositories**: Load and persist aggregates
- **Entity Framework Core**: ORM for write operations

Example Command Handler:
```csharp
internal class CreateNewMeetingGroupCommandHandler : ICommandHandler<CreateNewMeetingGroupCommand>
{
    private readonly IMeetingGroupRepository _meetingGroupRepository;
    private readonly IMeetingGroupProposalRepository _meetingGroupProposalRepository;

    public async Task Handle(CreateNewMeetingGroupCommand request, CancellationToken cancellationToken)
    {
        var meetingGroupProposal = await _meetingGroupProposalRepository.GetByIdAsync(request.MeetingGroupProposalId);
        var meetingGroup = meetingGroupProposal.CreateMeetingGroup();
        await _meetingGroupRepository.AddAsync(meetingGroup);
    }
}
```

### Read Model (Queries)

Queries will use **raw SQL** against **database views**:

- **Direct SQL**: No ORM overhead for reads
- **Database Views**: Optimized projections for specific use cases
- **DTOs**: Simple data transfer objects
- **Dapper**: Micro-ORM for query execution

Example Query Handler:
```csharp
internal class GetAllMeetingGroupsQueryHandler : IQueryHandler<GetAllMeetingGroupsQuery, List<MeetingGroupDto>>
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public async Task<List<MeetingGroupDto>> Handle(GetAllMeetingGroupsQuery request, CancellationToken cancellationToken)
    {
        var connection = _sqlConnectionFactory.GetOpenConnection();
        
        const string sql = @"
            SELECT 
                [MeetingGroup].[Id], 
                [MeetingGroup].[Name], 
                [MeetingGroup].[Description],
                [MeetingGroup].[LocationCountryCode],
                [MeetingGroup].[LocationCity]
            FROM [meetings].[v_MeetingGroups] AS [MeetingGroup]";
            
        var meetingGroups = await connection.QueryAsync<MeetingGroupDto>(sql);
        return meetingGroups.AsList();
    }
}
```

### Mediator Pattern

All commands and queries go through **MediatR**:

```csharp
public interface IMeetingsModule
{
    Task<TResult> ExecuteCommandAsync<TResult>(ICommand<TResult> command);
    Task ExecuteCommandAsync(ICommand command);
    Task<TResult> ExecuteQueryAsync<TResult>(IQuery<TResult> query);
}
```

### Handler Pattern

- **One handler per command/query**: Single Responsibility Principle
- **Interface Segregation**: Each handler implements one method
- **Parameter Object**: Commands/Queries are objects, easy to serialize
- **Decorator Pattern**: Cross-cutting concerns applied via decorators

## Consequences

### Positive

- **Optimized Performance**: Reads and writes can be optimized independently
- **Simplified Queries**: No need to navigate complex object graphs
- **Clear Intent**: Commands express business operations, queries express data needs
- **Scalability**: Can scale read and write sides independently
- **Testability**: Easy to test handlers in isolation
- **Single Responsibility**: Each handler does one thing
- **Flexibility**: Can add caching, pagination, filtering to queries without affecting writes
- **Performance Monitoring**: Easy to identify slow operations by handler

### Negative

- **Increased Complexity**: More code than simple CRUD
- **Learning Curve**: Team needs to understand CQRS concepts
- **Eventual Consistency**: Read model may lag behind write model (mitigated by synchronous updates in same process)
- **Duplication**: Some logic may appear in both views and domain model
- **Mediator Indirection**: Harder to trace which handler processes a request

### Trade-offs

- **Not Full CQRS**: We don't use separate databases for read/write
  - Simpler to implement and maintain
  - Still get most benefits of CQRS
  - Could evolve to separate databases if needed

- **Synchronous Updates**: Database views updated synchronously
  - No eventual consistency for reads within same module
  - Simpler reasoning about data state
  - Could add async projections if needed

### Applicability

CQRS is used in all modules for:
- ✅ All business operations (commands)
- ✅ All data retrieval (queries)
- ✅ API endpoints
- ✅ Background processing

## References

- [CQRS Pattern - Microsoft Architecture Center](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [Simple CQRS implementation with raw SQL and DDD by Kamil Grzybek](https://www.kamilgrzybek.com/design/simple-cqrs-implementation-with-raw-sql-and-ddd/)
- [MediatR library](https://github.com/jbogard/MediatR)
