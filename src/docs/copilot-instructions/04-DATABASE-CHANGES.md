# Database Changes Guide

This guide describes how to manage database changes in the Modular Monolith with DDD application.

## Overview

The project uses a dual approach to database management:

1. **SSDT Database Project** - Maintains current database schema state
2. **DbUp Migrations** - Applies incremental changes to databases

## Database Project Structure

```
src/Database/
├── CompanyName.MyMeetings.Database/           # SSDT project (schema state)
│   ├── administration/                        # Administration module schema
│   │   ├── Tables/
│   │   └── Views/
│   ├── meetings/                              # Meetings module schema
│   │   ├── Tables/
│   │   └── Views/
│   ├── payments/                              # Payments module schema
│   ├── users/                                 # UserAccess module schema
│   ├── Security/                              # Schemas and roles
│   ├── Scripts/
│   │   ├── CreateDatabase_Windows.sql
│   │   ├── CreateDatabase_Linux.sql
│   │   └── SeedDatabase.sql
│   └── Migrations/                            # DbUp migration scripts
├── CompanyName.MyMeetings.Database.Build/     # For CI compilation
└── DatabaseMigrator/                          # DbUp console application
```

## Creating Database Changes

### Step 1: Create Migration Script

Create a new SQL file in `src/Database/CompanyName.MyMeetings.Database/Migrations/`:

**Naming Convention:**
```
{YYYYMMDD}_{HHMMSS}_{DescriptiveName}.sql
```

**Example:**
```
20240115_140000_AddCommentingToMeetings.sql
```

### Step 2: Write the Migration Script

```sql
-- 20240115_140000_AddCommentingToMeetings.sql

-- Create new table
CREATE TABLE [meetings].[MeetingComments]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [MeetingId] UNIQUEIDENTIFIER NOT NULL,
    [AuthorId] UNIQUEIDENTIFIER NOT NULL,
    [Comment] NVARCHAR(2000) NOT NULL,
    [InReplyToCommentId] UNIQUEIDENTIFIER NULL,
    [CreateDate] DATETIME NOT NULL,
    [EditDate] DATETIME NULL,
    [IsRemoved] BIT NOT NULL DEFAULT 0,
    [RemovedByReason] NVARCHAR(500) NULL,
    CONSTRAINT [PK_meetings_MeetingComments_Id] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_meetings_MeetingComments_MeetingId] 
        FOREIGN KEY ([MeetingId]) REFERENCES [meetings].[Meetings]([Id])
);
GO

-- Create index
CREATE INDEX [IX_meetings_MeetingComments_MeetingId] 
    ON [meetings].[MeetingComments]([MeetingId]);
GO

-- Create view for Read Model
CREATE VIEW [meetings].[v_MeetingComments]
AS
SELECT 
    [Comment].[Id],
    [Comment].[MeetingId],
    [Comment].[AuthorId],
    [Member].[FirstName] + ' ' + [Member].[LastName] AS [AuthorName],
    [Comment].[Comment],
    [Comment].[InReplyToCommentId],
    [Comment].[CreateDate],
    [Comment].[EditDate],
    [Comment].[IsRemoved]
FROM [meetings].[MeetingComments] AS [Comment]
INNER JOIN [meetings].[Members] AS [Member] 
    ON [Comment].[AuthorId] = [Member].[Id]
WHERE [Comment].[IsRemoved] = 0;
GO
```

### Step 3: Update SSDT Project (Schema State)

Add corresponding files to the SSDT database project to reflect the new schema state:

**Table Definition:**
```
src/Database/CompanyName.MyMeetings.Database/meetings/Tables/MeetingComments.sql
```

```sql
CREATE TABLE [meetings].[MeetingComments]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [MeetingId] UNIQUEIDENTIFIER NOT NULL,
    [AuthorId] UNIQUEIDENTIFIER NOT NULL,
    [Comment] NVARCHAR(2000) NOT NULL,
    [InReplyToCommentId] UNIQUEIDENTIFIER NULL,
    [CreateDate] DATETIME NOT NULL,
    [EditDate] DATETIME NULL,
    [IsRemoved] BIT NOT NULL DEFAULT 0,
    [RemovedByReason] NVARCHAR(500) NULL,
    CONSTRAINT [PK_meetings_MeetingComments_Id] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_meetings_MeetingComments_MeetingId] 
        FOREIGN KEY ([MeetingId]) REFERENCES [meetings].[Meetings]([Id])
);
GO

CREATE INDEX [IX_meetings_MeetingComments_MeetingId] 
    ON [meetings].[MeetingComments]([MeetingId]);
GO
```

**View Definition:**
```
src/Database/CompanyName.MyMeetings.Database/meetings/Views/v_MeetingComments.sql
```

```sql
CREATE VIEW [meetings].[v_MeetingComments]
AS
SELECT 
    [Comment].[Id],
    [Comment].[MeetingId],
    [Comment].[AuthorId],
    [Member].[FirstName] + ' ' + [Member].[LastName] AS [AuthorName],
    [Comment].[Comment],
    [Comment].[InReplyToCommentId],
    [Comment].[CreateDate],
    [Comment].[EditDate],
    [Comment].[IsRemoved]
FROM [meetings].[MeetingComments] AS [Comment]
INNER JOIN [meetings].[Members] AS [Member] 
    ON [Comment].[AuthorId] = [Member].[Id]
WHERE [Comment].[IsRemoved] = 0;
GO
```

## Running Migrations

### Using NUKE Build System

```shell
.\build MigrateDatabase --DatabaseConnectionString "your_connection_string"
```

### Using DatabaseMigrator Directly

```shell
cd src/Database/DatabaseMigrator
dotnet run "your_connection_string" "..\CompanyName.MyMeetings.Database\Migrations"
```

## Schema Guidelines

### Module Isolation

Each module has its own schema:

| Module | Schema |
|--------|--------|
| Meetings | `[meetings]` |
| Administration | `[administration]` |
| Payments | `[payments]` |
| UserAccess | `[users]` |
| App (shared infrastructure) | `[app]` |

### Required Tables Per Module

Every module needs these infrastructure tables:

```sql
-- Outbox for publishing integration events
CREATE TABLE [{schema}].[OutboxMessages]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [OccurredOn] DATETIME2 NOT NULL,
    [Type] VARCHAR(255) NOT NULL,
    [Data] NVARCHAR(MAX) NOT NULL,
    [ProcessedDate] DATETIME2 NULL,
    CONSTRAINT [PK_{schema}_OutboxMessages_Id] PRIMARY KEY ([Id])
);

-- Inbox for processing integration events
CREATE TABLE [{schema}].[InboxMessages]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [OccurredOn] DATETIME2 NOT NULL,
    [Type] VARCHAR(255) NOT NULL,
    [Data] NVARCHAR(MAX) NOT NULL,
    [ProcessedDate] DATETIME2 NULL,
    CONSTRAINT [PK_{schema}_InboxMessages_Id] PRIMARY KEY ([Id])
);

-- Internal commands for background processing
CREATE TABLE [{schema}].[InternalCommands]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [EnqueueDate] DATETIME2 NOT NULL,
    [Type] VARCHAR(255) NOT NULL,
    [Data] NVARCHAR(MAX) NOT NULL,
    [ProcessedDate] DATETIME2 NULL,
    [Error] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_{schema}_InternalCommands_Id] PRIMARY KEY ([Id])
);
```

### Views for Read Model (CQRS)

Create views for Query handlers:

```sql
-- View naming convention: v_{EntityName} or v_{EntityName}{Detail}
CREATE VIEW [meetings].[v_MeetingGroups]
AS
SELECT 
    [MeetingGroup].[Id],
    [MeetingGroup].[Name],
    [MeetingGroup].[Description],
    [MeetingGroup].[LocationCity],
    [MeetingGroup].[LocationCountryCode],
    [MeetingGroup].[CreateDate]
FROM [meetings].[MeetingGroups] AS [MeetingGroup];
GO
```

### Naming Conventions

| Object Type | Convention | Example |
|-------------|------------|---------|
| Table | PascalCase, plural | `[meetings].[MeetingGroups]` |
| Column | PascalCase | `[MeetingGroupId]` |
| Primary Key | `PK_{schema}_{table}_Id` | `PK_meetings_MeetingGroups_Id` |
| Foreign Key | `FK_{schema}_{table}_{column}` | `FK_meetings_Meetings_MeetingGroupId` |
| Index | `IX_{schema}_{table}_{columns}` | `IX_meetings_Meetings_MeetingGroupId` |
| View | `v_{EntityName}` | `[meetings].[v_MeetingGroups]` |
| Unique Index | `UQ_{schema}_{table}_{column}` | `UQ_users_Users_Login` |

## EF Core Configuration

After creating database schema, add Entity Framework configuration:

```csharp
// Location: Modules/{Module}/Infrastructure/Domain/{Entity}/{Entity}EntityTypeConfiguration.cs
internal class MeetingCommentEntityTypeConfiguration : IEntityTypeConfiguration<MeetingComment>
{
    public void Configure(EntityTypeBuilder<MeetingComment> builder)
    {
        builder.ToTable("MeetingComments", "meetings");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id");
        
        builder.Property<MeetingId>("_meetingId").HasColumnName("MeetingId");
        builder.Property<MemberId>("_authorId").HasColumnName("AuthorId");
        builder.Property<string>("_comment").HasColumnName("Comment");
        builder.Property<DateTime>("_createDate").HasColumnName("CreateDate");
        builder.Property<DateTime?>("_editDate").HasColumnName("EditDate");
        builder.Property<bool>("_isRemoved").HasColumnName("IsRemoved");
        builder.Property<string>("_removedByReason").HasColumnName("RemovedByReason");
        builder.Property<MeetingCommentId>("_inReplyToCommentId").HasColumnName("InReplyToCommentId");
    }
}
```

## Best Practices

### DO:
- ✅ Create migration scripts for ALL database changes
- ✅ Keep SSDT project in sync with migrations
- ✅ Use views for Read Model queries
- ✅ Follow naming conventions consistently
- ✅ Add indexes for frequently queried columns
- ✅ Use appropriate data types

### DON'T:
- ❌ Share tables between modules
- ❌ Create cross-schema foreign keys
- ❌ Modify existing migration scripts
- ❌ Skip the SSDT project updates
- ❌ Use `SELECT *` in views

## Troubleshooting

### Migration Already Applied
If you need to fix a migration that's already been applied:
1. Create a new migration script with the fix
2. Never modify existing migration scripts

### Schema Comparison
Use Visual Studio's Schema Compare to verify SSDT project matches the database.

### CI Build Errors
The `CompanyName.MyMeetings.Database.Build` project compiles the database project using `MSBuild.Sdk.SqlProj`. Check for SQL syntax errors in your scripts.
