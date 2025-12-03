# ADR-0024: Database Change Management with DbUp and SSDT

## Status

Accepted

## Context

In a long-lived application with a database, we need a strategy for managing database schema changes:

1. **Version Control**: Database schema should be versioned like code
2. **Repeatability**: Same scripts produce same result on any environment
3. **Traceability**: Know what changes were applied and when
4. **Rollback**: Ability to revert changes if needed
5. **Multiple Environments**: Dev, Test, Staging, Production
6. **Team Collaboration**: Multiple developers working on database changes
7. **CI/CD Integration**: Automate database deployment

### Requirements

- Scripts versioned in source control
- Current state of database structure documented
- Migration scripts applied in order
- Scripts only run once (idempotent deployment)
- Support for both migrations and current state
- Simple deployment process
- Works with SQL Server

### Considered Alternatives

1. **Manual Scripts**
   - Developer runs scripts manually in SSMS
   - Pros: Simple, full control
   - Cons: Error-prone, no tracking, doesn't scale

2. **Entity Framework Migrations**
   - Code-first migrations generated from C# models
   - Pros: Integrated with EF, automatic generation
   - Cons: 
     - Couples database to ORM
     - Hard to customize for complex scenarios
     - Read model uses Dapper, not EF
     - Less control over exact SQL

3. **Flyway / Liquibase**
   - Popular migration tools
   - Pros: Mature, feature-rich
   - Cons: Java-based, extra tooling

4. **DbUp**
   - .NET library for database migrations
   - Pros: Simple, .NET-native, flexible
   - Cons: Basic features (but sufficient)

5. **SQL Server Database Project (SSDT)**
   - Visual Studio database project
   - Pros: Full database representation, dacpac deployment, schema compare
   - Cons: State-based (vs migration-based), can't customize deployment

6. **Hybrid: DbUp + SSDT**
   - DbUp for migrations, SSDT for current state
   - Pros: Best of both worlds
   - Cons: Two tools to manage

## Decision

We will use a **hybrid approach** combining:
- **DbUp** for migration-based deployment (transitions)
- **SSDT Database Project** for state-based documentation (current state)

### Architecture

```
Source Control
  |
  ├── Migrations/              ← DbUp (How to get there)
  │     ├── 20240101_CreateMeetingsSchema.sql
  │     ├── 20240102_CreateMeetingGroupsTable.sql
  │     ├── 20240103_AddMeetingHostsTable.sql
  │     └── ...
  |
  └── Database Project/        ← SSDT (What it should look like)
        ├── meetings/
        │     ├── Tables/
        │     │     ├── MeetingGroups.sql
        │     │     ├── Meetings.sql
        │     │     └── MeetingHosts.sql
        │     ├── Views/
        │     └── StoredProcedures/
        ├── administration/
        ├── payments/
        └── users/
```

### DbUp Implementation

#### 1. Migration Scripts Location

```
src/Database/DatabaseMigrations/
  ├── Scripts/
  │     ├── 0001_CreateSchemas.sql
  │     ├── 0002_CreateMeetingsModule.sql
  │     ├── 0003_CreateAdministrationModule.sql
  │     ├── 0004_CreatePaymentsModule.sql
  │     ├── 0005_CreateUsersModule.sql
  │     └── ...
```

#### 2. DatabaseMigrator Console Application

```csharp
class Program
{
    static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: DatabaseMigrator <connection_string> <scripts_path>");
            return -1;
        }

        var connectionString = args[0];
        var scriptsPath = args[1];

        var upgrader =
            DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsFromFileSystem(scriptsPath)
                .WithTransactionPerScript()
                .LogToConsole()
                .LogTo(new SerilogUpgradeLog())
                .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(result.Error);
            Console.ResetColor();
            return -1;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Success!");
        Console.ResetColor();
        return 0;
    }
}
```

#### 3. Running Migrations

Command line:
```bash
dotnet DatabaseMigrator.dll "Server=localhost;Database=MyMeetings;Trusted_Connection=True;" "C:\scripts"
```

NUKE target:
```csharp
Target MigrateDatabase => _ => _
    .Executes(() =>
    {
        var databaseMigratorPath = RootDirectory / "src" / "Database" / "DatabaseMigrator";
        var scriptsPath = RootDirectory / "src" / "Database" / "DatabaseMigrations" / "Scripts";
        
        DotNet($"{databaseMigratorPath} \"{DatabaseConnectionString}\" \"{scriptsPath}\"");
    });
```

#### 4. Migration Script Example

```sql
-- 0005_CreateMeetingGroupsTable.sql

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MeetingGroups' AND schema_id = SCHEMA_ID('meetings'))
BEGIN
    CREATE TABLE [meetings].[MeetingGroups]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(1000) NULL,
        [LocationCity] NVARCHAR(100) NOT NULL,
        [LocationCountryCode] VARCHAR(2) NOT NULL,
        [CreatorId] UNIQUEIDENTIFIER NOT NULL,
        [CreateDate] DATETIME2 NOT NULL,
        [PaymentDateTo] DATETIME2 NULL,
        CONSTRAINT [PK_MeetingGroups] PRIMARY KEY ([Id])
    );
    
    CREATE INDEX [IX_MeetingGroups_CreatorId] 
        ON [meetings].[MeetingGroups] ([CreatorId]);
END
GO
```

#### 5. Tracking Table

DbUp creates a tracking table:

```sql
-- Created automatically by DbUp
CREATE TABLE [dbo].[SchemaVersions]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ScriptName] NVARCHAR(255) NOT NULL,
    [Applied] DATETIME NOT NULL,
    CONSTRAINT [PK_SchemaVersions] PRIMARY KEY ([Id])
);
```

Tracks executed scripts:
```
ScriptName                              | Applied
----------------------------------------|---------------------
0001_CreateSchemas.sql                  | 2024-01-15 10:00:00
0002_CreateMeetingsModule.sql           | 2024-01-15 10:00:01
0003_CreateAdministrationModule.sql     | 2024-01-15 10:00:02
```

### SSDT Database Project

#### 1. Project Structure

```
CompanyName.MyMeetings.Database/
  ├── administration/
  │     ├── Tables/
  │     │     └── MeetingGroupProposals.sql
  │     └── Views/
  │           └── v_MeetingGroupProposals.sql
  ├── meetings/
  │     ├── Tables/
  │     │     ├── MeetingGroups.sql
  │     │     ├── Meetings.sql
  │     │     └── MeetingAttendees.sql
  │     └── Views/
  │           ├── v_MeetingGroups.sql
  │           └── v_Meetings.sql
  ├── payments/
  ├── users/
  └── Security/
        ├── administration.sql
        ├── meetings.sql
        ├── payments.sql
        └── users.sql
```

#### 2. Table Definition Example

```sql
-- meetings/Tables/MeetingGroups.sql
CREATE TABLE [meetings].[MeetingGroups]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    [LocationCity] NVARCHAR(100) NOT NULL,
    [LocationCountryCode] VARCHAR(2) NOT NULL,
    [CreatorId] UNIQUEIDENTIFIER NOT NULL,
    [CreateDate] DATETIME2 NOT NULL,
    [PaymentDateTo] DATETIME2 NULL,
    CONSTRAINT [PK_MeetingGroups] PRIMARY KEY ([Id])
);

GO

CREATE INDEX [IX_MeetingGroups_CreatorId] 
    ON [meetings].[MeetingGroups] ([CreatorId]);
```

#### 3. View Definition Example

```sql
-- meetings/Views/v_MeetingGroups.sql
CREATE VIEW [meetings].[v_MeetingGroups]
AS
SELECT 
    [MeetingGroup].[Id],
    [MeetingGroup].[Name],
    [MeetingGroup].[Description],
    [MeetingGroup].[LocationCity],
    [MeetingGroup].[LocationCountryCode]
FROM 
    [meetings].[MeetingGroups] AS [MeetingGroup]
WHERE 
    [MeetingGroup].[PaymentDateTo] IS NULL 
    OR [MeetingGroup].[PaymentDateTo] >= GETUTCDATE();
```

#### 4. Building Database Project

Using MSBuild.Sdk.SqlProj for .NET Core:

```xml
<!-- CompanyName.MyMeetings.Database.Build.csproj -->
<Project Sdk="MSBuild.Sdk.SqlProj/1.6.0">
    <PropertyGroup>
        <TargetFramework>netstandard2.0</TargetFramework>
    </PropertyGroup>
    <ItemGroup>
        <Content Include="..\CompanyName.MyMeetings.Database\administration\**\*.sql" />
        <Content Include="..\CompanyName.MyMeetings.Database\meetings\**\*.sql" />
        <Content Include="..\CompanyName.MyMeetings.Database\payments\**\*.sql" />
        <Content Include="..\CompanyName.MyMeetings.Database\users\**\*.sql" />
        <Content Include="..\CompanyName.MyMeetings.Database\Security\**\*.sql" />
    </ItemGroup>
</Project>
```

Build in CI:
```bash
dotnet build CompanyName.MyMeetings.Database.Build.csproj
```

### Workflow

#### Development Workflow

1. **Make Schema Change**:
   ```sql
   -- Create migration: 0042_AddMeetingComments.sql
   CREATE TABLE [meetings].[MeetingComments]
   (
       [Id] UNIQUEIDENTIFIER NOT NULL,
       [MeetingId] UNIQUEIDENTIFIER NOT NULL,
       [AuthorId] UNIQUEIDENTIFIER NOT NULL,
       [Comment] NVARCHAR(MAX) NOT NULL,
       [CreateDate] DATETIME2 NOT NULL,
       CONSTRAINT [PK_MeetingComments] PRIMARY KEY ([Id])
   );
   ```

2. **Update SSDT Project**:
   ```sql
   -- meetings/Tables/MeetingComments.sql
   CREATE TABLE [meetings].[MeetingComments]
   (
       [Id] UNIQUEIDENTIFIER NOT NULL,
       ...
   );
   ```

3. **Run Migration Locally**:
   ```bash
   .\build MigrateDatabase --DatabaseConnectionString "connection_string"
   ```

4. **Verify**:
   - Check table created in database
   - Build SSDT project (validates SQL syntax)
   - Run integration tests

5. **Commit**: Both migration and SSDT changes together

#### Deployment Workflow

**Development Environment**:
```bash
dotnet DatabaseMigrator.dll "Server=dev-sql;Database=MyMeetings;..." "scripts_path"
```

**CI Pipeline** (GitHub Actions / NUKE):
```bash
.\build MigrateDatabase --DatabaseConnectionString ${{ secrets.DB_CONNECTION_STRING }}
```

**Production**:
```bash
# Same command, different connection string
dotnet DatabaseMigrator.dll "Server=prod-sql;Database=MyMeetings;..." "scripts_path"
```

## Consequences

### Positive DbUp Benefits

- **Migration-Based**: Explicit, ordered changes
- **Full Control**: Write exact SQL you want
- **Transactions**: Each script in transaction (rollback on error)
- **Idempotent Deployment**: Scripts run once, tracked in SchemaVersions
- **Simple**: Just .NET console app, no complex tooling
- **Flexible**: Can run data migrations, not just schema
- **Cross-Platform**: Works on Windows, Linux, Docker
- **CI/CD Friendly**: Easy to integrate

### Positive SSDT Benefits

- **Current State**: Single source of truth for database structure
- **Schema Compare**: Visual Studio tooling to compare
- **IntelliSense**: Code completion for SQL
- **Refactoring**: Rename table/column with refactoring
- **Build Validation**: Catches SQL errors at compile time
- **Documentation**: Self-documenting database structure
- **Team Alignment**: Everyone sees current state

### Negative

- **Two Tools**: Must maintain both migrations and SSDT
- **Duplication**: Same table defined twice (migration + SSDT)
- **Sync Required**: SSDT must be kept in sync with migrations
- **Learning Curve**: Team must understand both approaches
- **SSDT Complexity**: Database project can be complex
- **.NET Core SSDT**: Need MSBuild.Sdk.SqlProj workaround

### Division of Responsibilities

**DbUp (How to get there)**:
- ✅ Schema changes (CREATE, ALTER, DROP)
- ✅ Data migrations
- ✅ Production deployments
- ✅ Version tracking
- ✅ Deployment automation

**SSDT (What it should be)**:
- ✅ Current state documentation
- ✅ Schema validation (compile-time)
- ✅ Developer reference
- ✅ Schema comparison
- ❌ NOT used for deployment (only for documentation)

### Best Practices

1. **Migration Naming**: Use sequential numbers
   ```
   0001_CreateSchemas.sql
   0002_CreateMeetingsModule.sql
   0042_AddMeetingComments.sql
   ```

2. **Idempotent Migrations**: Check existence before creating
   ```sql
   IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MeetingGroups')
   BEGIN
       CREATE TABLE [meetings].[MeetingGroups] (...)
   END
   ```

3. **One Logical Change**: Each migration should be atomic
   - ✅ Add one table
   - ✅ Add related tables and FKs
   - ❌ Mix unrelated changes

4. **Test Migrations**: Run against copy of production before deploying

5. **Keep SSDT Updated**: Update SSDT when writing migration

6. **Version Control Both**: Always commit migration + SSDT together

### Rollback Strategy

**Forward-Only**: We don't support automated rollback

- ❌ No "down" migrations
- ✅ Create new migration to undo changes

Example:
```sql
-- 0043_RollbackMeetingComments.sql
DROP TABLE [meetings].[MeetingComments];
```

Reason: Rollback is risky in production with data

### Monitoring

Track in CI/CD:
- ✅ All migrations applied successfully
- ✅ SSDT project builds without errors
- ✅ Integration tests pass after migration

## References

- [DbUp Documentation](https://dbup.readthedocs.io/)
- [SQL Server Data Tools](https://docs.microsoft.com/en-us/sql/ssdt/)
- [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj/)
- [Database change management by Kamil Grzybek](https://www.kamilgrzybek.com/database/database-change-management/)
- Section 3.16 "Database change management" in project README
