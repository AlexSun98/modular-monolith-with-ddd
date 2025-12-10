# Creating a New Module Guide

This guide describes how to create a new module in the Modular Monolith with DDD application.

## Module Structure Overview

Each module follows Clean Architecture and consists of these assemblies:

```
Modules/{ModuleName}/
├── Application/           # Use cases, commands, queries, handlers
├── Domain/               # Domain model (Aggregates, Entities, Value Objects)
├── Infrastructure/       # Data access, external services, module startup
└── IntegrationEvents/    # Events published to other modules (contracts)
```

## Step-by-Step Module Creation

### Step 1: Create Project Files

Create four `.csproj` files for the module:

**1. Domain Project** (`CompanyName.MyMeetings.Modules.{ModuleName}.Domain.csproj`)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>CompanyName.MyMeetings.Modules.{ModuleName}.Domain</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Domain\CompanyName.MyMeetings.BuildingBlocks.Domain.csproj" />
  </ItemGroup>
</Project>
```

**2. Application Project** (`CompanyName.MyMeetings.Modules.{ModuleName}.Application.csproj`)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>CompanyName.MyMeetings.Modules.{ModuleName}.Application</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Application\CompanyName.MyMeetings.BuildingBlocks.Application.csproj" />
    <ProjectReference Include="..\Domain\CompanyName.MyMeetings.Modules.{ModuleName}.Domain.csproj" />
  </ItemGroup>
</Project>
```

**3. Infrastructure Project** (`CompanyName.MyMeetings.Modules.{ModuleName}.Infrastructure.csproj`)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>CompanyName.MyMeetings.Modules.{ModuleName}.Infrastructure</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Infrastructure\CompanyName.MyMeetings.BuildingBlocks.Infrastructure.csproj" />
    <ProjectReference Include="..\Application\CompanyName.MyMeetings.Modules.{ModuleName}.Application.csproj" />
  </ItemGroup>
</Project>
```

**4. IntegrationEvents Project** (`CompanyName.MyMeetings.Modules.{ModuleName}.IntegrationEvents.csproj`)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>CompanyName.MyMeetings.Modules.{ModuleName}.IntegrationEvents</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Infrastructure\CompanyName.MyMeetings.BuildingBlocks.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

### Step 2: Create Module Interface

```csharp
// Location: Modules/{ModuleName}/Infrastructure/I{ModuleName}Module.cs
public interface I{ModuleName}Module
{
    Task<TResult> ExecuteCommandAsync<TResult>(ICommand<TResult> command);
    Task ExecuteCommandAsync(ICommand command);
    Task<TResult> ExecuteQueryAsync<TResult>(IQuery<TResult> query);
}
```

### Step 3: Implement Module Class

```csharp
// Location: Modules/{ModuleName}/Infrastructure/{ModuleName}Module.cs
public class {ModuleName}Module : I{ModuleName}Module
{
    public async Task<TResult> ExecuteCommandAsync<TResult>(ICommand<TResult> command)
    {
        return await CommandsExecutor.Execute(command);
    }

    public async Task ExecuteCommandAsync(ICommand command)
    {
        await CommandsExecutor.Execute(command);
    }

    public async Task<TResult> ExecuteQueryAsync<TResult>(IQuery<TResult> query)
    {
        using (var scope = {ModuleName}CompositionRoot.BeginLifetimeScope())
        {
            var mediator = scope.Resolve<IMediator>();
            return await mediator.Send(query);
        }
    }
}
```

### Step 4: Create Composition Root

```csharp
// Location: Modules/{ModuleName}/Infrastructure/Configuration/{ModuleName}CompositionRoot.cs
internal static class {ModuleName}CompositionRoot
{
    private static IContainer _container;

    internal static void SetContainer(IContainer container)
    {
        _container = container;
    }

    internal static ILifetimeScope BeginLifetimeScope()
    {
        return _container.BeginLifetimeScope();
    }
}
```

### Step 5: Create Startup Class

```csharp
// Location: Modules/{ModuleName}/Infrastructure/Configuration/{ModuleName}Startup.cs
public class {ModuleName}Startup
{
    private static IContainer _container;

    public static void Initialize(
        string connectionString,
        IExecutionContextAccessor executionContextAccessor,
        ILogger logger,
        IEventsBus eventsBus)
    {
        var moduleLogger = logger.ForContext("Module", "{ModuleName}");

        ConfigureCompositionRoot(
            connectionString,
            executionContextAccessor,
            moduleLogger,
            eventsBus);

        QuartzStartup.Initialize(moduleLogger);
        EventsBusStartup.Initialize(moduleLogger);
    }

    private static void ConfigureCompositionRoot(
        string connectionString,
        IExecutionContextAccessor executionContextAccessor,
        ILogger logger,
        IEventsBus eventsBus)
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder.RegisterModule(new LoggingModule(logger));
        containerBuilder.RegisterModule(new DataAccessModule(connectionString));
        containerBuilder.RegisterModule(new ProcessingModule());
        containerBuilder.RegisterModule(new EventsBusModule(eventsBus));
        containerBuilder.RegisterModule(new MediatorModule());
        containerBuilder.RegisterModule(new OutboxModule());
        containerBuilder.RegisterModule(new QuartzModule());

        containerBuilder.RegisterInstance(executionContextAccessor);

        _container = containerBuilder.Build();

        {ModuleName}CompositionRoot.SetContainer(_container);
    }
}
```

### Step 6: Create DbContext

```csharp
// Location: Modules/{ModuleName}/Infrastructure/Configuration/DataAccess/{ModuleName}Context.cs
public class {ModuleName}Context : DbContext
{
    public DbSet<InternalCommand> InternalCommands { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    // Add your entity DbSets here

    public {ModuleName}Context(DbContextOptions<{ModuleName}Context> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DatabaseSchema.Name);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof({ModuleName}Context).Assembly);
    }
}

internal static class DatabaseSchema
{
    internal const string Name = "{modulename}"; // lowercase schema name
}
```

### Step 7: Create Autofac Modules

**DataAccessModule:**

```csharp
// Location: Modules/{ModuleName}/Infrastructure/Configuration/DataAccess/DataAccessModule.cs
internal class DataAccessModule : Autofac.Module
{
    private readonly string _connectionString;

    internal DataAccessModule(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SqlConnectionFactory>()
            .As<ISqlConnectionFactory>()
            .WithParameter("connectionString", _connectionString)
            .InstancePerLifetimeScope();

        builder.RegisterType<UnitOfWork>()
            .As<IUnitOfWork>()
            .InstancePerLifetimeScope();

        builder.Register(c =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<{ModuleName}Context>();
            optionsBuilder.UseSqlServer(_connectionString);
            return new {ModuleName}Context(optionsBuilder.Options);
        })
        .AsSelf()
        .InstancePerLifetimeScope();
    }
}
```

**MediatorModule:**

```csharp
// Location: Modules/{ModuleName}/Infrastructure/Configuration/Mediation/MediatorModule.cs
internal class MediatorModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(typeof(IMediator).GetTypeInfo().Assembly)
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        var mediatorOpenTypes = new[]
        {
            typeof(IRequestHandler<,>),
            typeof(INotificationHandler<>),
        };

        foreach (var mediatorOpenType in mediatorOpenTypes)
        {
            builder
                .RegisterAssemblyTypes(
                    ThisAssembly,
                    Assemblies.Application)
                .AsClosedTypesOf(mediatorOpenType)
                .AsImplementedInterfaces()
                .FindConstructorsWith(new AllConstructorFinder())
                .InstancePerLifetimeScope();
        }

        builder.RegisterGeneric(typeof(CommandHandlerDecorator<,>)).As(typeof(IPipelineBehavior<,>));
        builder.RegisterGeneric(typeof(CommandHandlerDecorator<>)).As(typeof(IPipelineBehavior<,>));
        builder.RegisterGeneric(typeof(ValidationBehavior<,>)).As(typeof(IPipelineBehavior<,>));
        builder.RegisterGeneric(typeof(UnitOfWorkBehavior<,>)).As(typeof(IPipelineBehavior<,>));

        builder.Register<ServiceFactory>(ctx =>
        {
            var c = ctx.Resolve<IComponentContext>();
            return t => c.Resolve(t);
        }).InstancePerLifetimeScope();
    }
}
```

### Step 8: Create Database Schema

Create SQL migration scripts in `src/Database/CompanyName.MyMeetings.Database/Migrations/`:

```sql
-- {YYYYMMDD}_{Time}_Create{ModuleName}Schema.sql
CREATE SCHEMA [{modulename}];
GO

-- Create your tables
CREATE TABLE [{modulename}].[YourEntity]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    -- ... columns
    CONSTRAINT [PK_{modulename}_YourEntity_Id] PRIMARY KEY ([Id])
);
GO

-- Create Outbox table (required for each module)
CREATE TABLE [{modulename}].[OutboxMessages]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [OccurredOn] DATETIME2 NOT NULL,
    [Type] VARCHAR(255) NOT NULL,
    [Data] NVARCHAR(MAX) NOT NULL,
    [ProcessedDate] DATETIME2 NULL,
    CONSTRAINT [PK_{modulename}_OutboxMessages_Id] PRIMARY KEY ([Id])
);
GO

-- Create InternalCommands table (required for each module)
CREATE TABLE [{modulename}].[InternalCommands]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [EnqueueDate] DATETIME2 NOT NULL,
    [Type] VARCHAR(255) NOT NULL,
    [Data] NVARCHAR(MAX) NOT NULL,
    [ProcessedDate] DATETIME2 NULL,
    CONSTRAINT [PK_{modulename}_InternalCommands_Id] PRIMARY KEY ([Id])
);
GO

-- Create Inbox table (for processing integration events)
CREATE TABLE [{modulename}].[InboxMessages]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [OccurredOn] DATETIME2 NOT NULL,
    [Type] VARCHAR(255) NOT NULL,
    [Data] NVARCHAR(MAX) NOT NULL,
    [ProcessedDate] DATETIME2 NULL,
    CONSTRAINT [PK_{modulename}_InboxMessages_Id] PRIMARY KEY ([Id])
);
GO
```

### Step 9: Register Module in API Startup

```csharp
// In API/Startup.cs or Program.cs
{ModuleName}Startup.Initialize(
    connectionString,
    executionContextAccessor,
    logger,
    eventsBus);
```

### Step 10: Create Integration Events (for cross-module communication)

```csharp
// Location: Modules/{ModuleName}/IntegrationEvents/{EventName}IntegrationEvent.cs
public class {Something}CreatedIntegrationEvent : IntegrationEvent
{
    public Guid {Something}Id { get; }
    // ... other properties

    public {Something}CreatedIntegrationEvent(Guid id, Guid {something}Id, DateTime occurredOn)
        : base(id, occurredOn)
    {
        {Something}Id = {something}Id;
    }
}
```

## Module Checklist

Before considering the module complete, verify:

- [ ] All four projects created (Domain, Application, Infrastructure, IntegrationEvents)
- [ ] Module interface defined (`I{ModuleName}Module`)
- [ ] Module class implemented (`{ModuleName}Module`)
- [ ] Composition Root created
- [ ] Startup class with `Initialize` method
- [ ] DbContext with correct schema
- [ ] All Autofac modules registered
- [ ] Database schema created with required tables (Outbox, Inbox, InternalCommands)
- [ ] Module registered in API Startup
- [ ] Quartz background jobs configured (if needed)
- [ ] Events Bus subscription configured (if listening to other modules)

## Key Architecture Rules

1. **Modules are isolated** - No direct dependencies between modules
2. **Communication via Events** - Use Integration Events for cross-module communication
3. **Separate schemas** - Each module has its own database schema
4. **Own Composition Root** - Each module manages its own IoC container
5. **IntegrationEvents are contracts** - Only IntegrationEvents assembly can be referenced by other modules
