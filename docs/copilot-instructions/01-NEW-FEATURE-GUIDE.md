# Guide: Adding a New Feature

This guide outlines the steps to add a new feature (Command or Query) to an existing module.

## 1. Determine the Type of Operation
*   **Command**: Changes state, performs business logic. Returns `Unit` or `Id` of created resource.
*   **Query**: Reads data, no side effects. Returns a DTO.

## 2. Implementing a Command (Write Model)

### Step 2.1: Define the Command
Create a class implementing `ICommand` or `ICommand<TResult>` in the `Application` layer.
```csharp
public class CreateSomethingCommand : ICommand<Guid>
{
    public string Name { get; }
    // ... constructor and properties
}
```

### Step 2.2: Implement the Command Handler
Create a handler implementing `ICommandHandler<TCommand>` or `ICommandHandler<TCommand, TResult>`.
*   Inject Repositories and Domain Services.
*   **Do NOT** inject EF Core DbContext directly (use Repositories).
*   Perform business logic on the Domain Model (Aggregate Root).
*   Save changes via Repository.

```csharp
internal class CreateSomethingCommandHandler : ICommandHandler<CreateSomethingCommand, Guid>
{
    private readonly ISomethingRepository _repository;

    public async Task<Guid> Handle(CreateSomethingCommand request, CancellationToken cancellationToken)
    {
        // 1. Load Aggregate (if needed) or Create new
        var something = SomethingAggregate.Create(request.Name);

        // 2. Persist
        await _repository.AddAsync(something);

        // 3. Return result
        return something.Id;
    }
}
```

### Step 2.3: Domain Logic
*   Ensure business rules are enforced in the Domain Entity/Aggregate.
*   Use `CheckRule(IBusinessRule rule)` to validate invariants.
*   Raise Domain Events for side effects.

### Step 2.4: Validation
Create a `AbstractValidator<TCommand>` using FluentValidation in the `Application` layer.
```csharp
public class CreateSomethingCommandValidator : AbstractValidator<CreateSomethingCommand>
{
    public CreateSomethingCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}
```

## 3. Implementing a Query (Read Model)

### Step 3.1: Define the Query and DTO
Create a class implementing `IQuery<TResult>` and the Result DTO.
```csharp
public class GetSomethingQuery : IQuery<SomethingDto>
{
    public Guid Id { get; }
    // ...
}

public class SomethingDto 
{
    // ... properties
}
```

### Step 3.2: Implement the Query Handler
Create a handler implementing `IQueryHandler<TQuery, TResult>`.
*   Inject `ISqlConnectionFactory`.
*   Write raw SQL or use Dapper.
*   Query against Database Views (schema `[module].v_ViewName`) if possible.

```csharp
internal class GetSomethingQueryHandler : IQueryHandler<GetSomethingQuery, SomethingDto>
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public async Task<SomethingDto> Handle(GetSomethingQuery request, CancellationToken cancellationToken)
    {
        var connection = _sqlConnectionFactory.GetOpenConnection();
        const string sql = "SELECT ... FROM [module].[v_Something] WHERE Id = @Id";
        return await connection.QuerySingleOrDefaultAsync<SomethingDto>(sql, new { request.Id });
    }
}
```

## 4. Expose via API
*   Add a Controller method in the `API` project.
*   Call `_module.ExecuteCommandAsync` or `_module.ExecuteQueryAsync`.
*   Map requests/responses as needed.

## 5. Tests
*   **Unit Tests**: Test the Domain Model logic (Aggregate) using NUnit.
*   **Integration Tests**: Test the Command/Query execution using the full module stack (with in-memory DB or test DB).
