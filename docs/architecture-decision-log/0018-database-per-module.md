# ADR-0018: Database Per Module Pattern

## Status

Accepted

## Context

In a modular monolith, we need to decide how modules interact with data storage. The database strategy directly impacts:

- **Module Autonomy**: Can modules evolve their schema independently?
- **Coupling**: How much do modules depend on each other's data?
- **Data Integrity**: How do we maintain consistency?
- **Performance**: What are the query and transaction characteristics?
- **Future Evolution**: Can we split modules into separate services?

### Considered Alternatives

1. **Shared Database**
   - Single database, all modules access all tables
   - Pros: Easy joins, ACID transactions across modules, simple deployment
   - Cons: High coupling, schema changes affect all modules, breaks encapsulation, prevents future microservices

2. **Database Per Module (Logical Separation)**
   - Single physical database, separate schema per module
   - Pros: Logical isolation, can enforce access control, easier to migrate later
   - Cons: No cross-module joins, need to coordinate transactions

3. **Database Per Module (Physical Separation)**
   - Completely separate databases
   - Pros: Full isolation, independent scaling, clear boundaries
   - Cons: Complex deployment, distributed transactions, higher cost

## Decision

We will use **Database Per Module with Logical Separation** via database schemas.

### Schema Structure

Each module owns its own database schema:

```sql
-- Administration module
CREATE SCHEMA administration;

-- Meetings module  
CREATE SCHEMA meetings;

-- Payments module
CREATE SCHEMA payments;

-- User Access module
CREATE SCHEMA users;

-- Application infrastructure
CREATE SCHEMA app;
```

### Data Ownership Rules

1. **Exclusive Ownership**: Each module owns its tables completely
   ```
   meetings.MeetingGroups           -- Owned by Meetings module
   meetings.Meetings                -- Owned by Meetings module
   administration.MeetingGroupProposals  -- Owned by Administration module
   payments.Subscriptions           -- Owned by Payments module
   ```

2. **No Direct Access**: Modules CANNOT query other modules' tables
   - ❌ Meetings module cannot SELECT from `administration.MeetingGroupProposals`
   - ✅ Meetings module receives data via integration events
   - ✅ Each module has its own `ISqlConnectionFactory` configured for its schema

3. **No Shared Tables**: No tables shared between modules
   - If data is needed by multiple modules, it's duplicated via events
   - Each module maintains its own projection of needed data

4. **No Foreign Keys Across Schemas**: Cannot create FKs between module schemas
   - Referential integrity maintained at application level
   - IDs from other modules stored as simple GUIDs

### Data Consistency Strategy

Since we can't use database constraints across modules:

1. **Eventual Consistency via Events**: 
   ```csharp
   // When MeetingGroupProposal is accepted in Administration
   // Meetings module receives event and creates its own MeetingGroup
   ```

2. **Data Duplication**: Each module stores what it needs
   ```csharp
   // Meetings module stores:
   public class MeetingGroup
   {
       public MeetingGroupId Id { get; private set; }  // Same ID as in Administration
       public string Name { get; private set; }        // Duplicated data
       // ... other properties needed by Meetings module
   }
   ```

3. **Saga Pattern**: For operations spanning modules (if needed)
   - Coordinated via commands and events
   - Each module maintains its own state

### Transaction Boundaries

- **Module = Transaction Boundary**: Each module has ACID transactions
- **Cross-Module = Eventual Consistency**: Use Outbox/Inbox pattern
- **No Distributed Transactions**: Simplified architecture

### Database Connection Management

Each module has its own connection factory:

```csharp
// Module initialization
PaymentsStartup.Initialize(
    connectionString,  // Same connection string but different schema access
    executionContext,
    logger,
    eventsBus
);
```

Connection configuration ensures module isolation:
```csharp
internal class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;
    
    public IDbConnection GetOpenConnection()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        // Connection opens with proper schema context
        return connection;
    }
}
```

## Consequences

### Positive

- **Module Autonomy**: Each module can change its schema independently
- **Clear Boundaries**: Schema = module boundary, easy to understand
- **Encapsulation**: Modules cannot access each other's data directly
- **Future-Proof**: Can easily move to separate databases
  - Change connection string
  - Deploy to different database server
  - No code changes needed
- **Security**: Database-level access control per schema
- **Easier Testing**: Each module can have its own test database
- **Independent Deployment**: Schema migrations per module
- **Clear Ownership**: Schema ownership maps to team ownership

### Negative

- **Data Duplication**: Same data may exist in multiple schemas
- **No Joins**: Cannot join tables across modules
  - Must retrieve and combine data at application level
  - May need multiple queries
- **Eventual Consistency**: Data may be temporarily out of sync between modules
- **Complexity**: More complex than shared database
- **Storage Overhead**: Duplicated data uses more space
- **Synchronization**: Must keep duplicated data in sync via events

### Trade-offs

**vs. Shared Database:**
- 🔺 More complex implementation
- ✅ Much better modularity
- ✅ Enables future evolution

**vs. Physical Separation:**
- ✅ Simpler deployment (single database)
- ✅ Easier development (single connection string)
- ✅ Can share infrastructure (backup, monitoring)
- ⚠️ Could still create unauthorized cross-schema queries (mitigated by code review + architecture tests)

### Migration Path

Current state → Future microservices:

1. **Phase 1** (Current): Logical schemas in one database
2. **Phase 2**: Separate database files (AlwaysOn availability groups)
3. **Phase 3**: Separate database servers
4. **Phase 4**: Separate services with own databases

Each step requires only infrastructure changes, no code changes.

### Enforcement

1. **Architecture Tests**: Verify no cross-schema dependencies
   ```csharp
   [Test]
   public void Meetings_Module_Should_Not_Reference_Other_Module_Tables()
   {
       // Architecture test to enforce rule
   }
   ```

2. **Code Review**: Check for violations

3. **Database Permissions**: (Optional) Restrict schema access at DB level

4. **Connection Factory**: Each module's connection factory could be configured to only access its schema

## References

- [Database per Service Pattern](https://microservices.io/patterns/data/database-per-service.html)
- Section 3.1 "High Level View" in project README (Key Assumption #5)
