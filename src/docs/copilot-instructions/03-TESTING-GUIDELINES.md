# Testing Guidelines

This guide describes testing standards and practices for the Modular Monolith with DDD application.

## Test Types Overview

| Test Type | Purpose | Framework | Location |
|-----------|---------|-----------|----------|
| Unit Tests | Test Domain Model | NUnit + NSubstitute | `Modules/{Module}/Tests/UnitTests/` |
| Architecture Tests | Enforce architecture rules | NetArchTest | `Modules/{Module}/Tests/ArchTests/` |
| Integration Tests | Test Commands/Queries with DB | NUnit | `Modules/{Module}/Tests/IntegrationTests/` |

## Unit Tests

### What to Test
- Domain Model (Aggregates, Entities, Value Objects)
- Business rules
- Domain services

### Test Structure (AAA Pattern)

```csharp
[Test]
public void MethodName_WhenCondition_ExpectedBehavior()
{
    // Arrange - Prepare the aggregate and dependencies
    var usersCounter = Substitute.For<IUsersCounter>();
    
    // Act - Execute exactly ONE public method
    var userRegistration = UserRegistration.RegisterNewUser(
        "login", "password", "test@email",
        "firstName", "lastName", usersCounter);
    
    // Assert - Check the outcome
    var domainEvent = AssertPublishedDomainEvent<NewUserRegisteredDomainEvent>(userRegistration);
    Assert.That(domainEvent.UserRegistrationId, Is.EqualTo(userRegistration.Id));
}
```

### Testing Business Rule Violations

```csharp
[Test]
public void MethodName_WhenRuleViolated_BreaksRule()
{
    // Arrange
    var usersCounter = Substitute.For<IUsersCounter>();
    usersCounter.CountUsersWithLogin("login").Returns(1);

    // Assert + Act
    AssertBrokenRule<UserLoginMustBeUniqueRule>(() =>
    {
        UserRegistration.RegisterNewUser(
            "login", "password", "test@email",
            "firstName", "lastName", usersCounter);
    });
}
```

### Base Test Class for Domain Tests

```csharp
public abstract class TestBase
{
    protected static T AssertPublishedDomainEvent<T>(Entity aggregate) where T : IDomainEvent
    {
        var domainEvent = DomainEventsTestHelper.GetDomainEvents(aggregate)
            .OfType<T>()
            .SingleOrDefault();

        Assert.That(domainEvent, Is.Not.Null, $"Expected {typeof(T).Name} domain event was not published.");
        return domainEvent;
    }

    protected static void AssertBrokenRule<TRule>(TestDelegate action) where TRule : class, IBusinessRule
    {
        var exception = Assert.Throws<BusinessRuleValidationException>(action);
        Assert.That(exception.BrokenRule, Is.TypeOf<TRule>());
    }
}
```

### SUT Factory Pattern

Use factory methods to create aggregates in a valid state:

```csharp
protected MeetingTestData CreateMeetingTestData(MeetingTestDataOptions options)
{
    var proposalMemberId = options.CreatorId ?? new MemberId(Guid.NewGuid());
    var meetingProposal = MeetingGroupProposal.ProposeNew(
        "name", "description",
        new MeetingGroupLocation("Warsaw", "PL"), proposalMemberId);

    meetingProposal.Accept();

    var meetingGroup = meetingProposal.CreateMeetingGroup();
    meetingGroup.UpdatePaymentInfo(DateTime.Now.AddDays(1));

    var meetingTerm = options.MeetingTerm ??
        new MeetingTerm(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2));

    var meeting = meetingGroup.CreateMeeting(
        "title", meetingTerm, "description",
        new MeetingLocation("Name", "Address", "PostalCode", "City"),
        options.AttendeesLimit, options.GuestsLimit,
        Term.NoTerm, MoneyValue.Zero,
        new List<MemberId>(), proposalMemberId);

    DomainEventsTestHelper.ClearAllDomainEvents(meetingGroup);

    return new MeetingTestData(meetingGroup, meeting);
}
```

### Unit Test Best Practices

**DO:**
- ✅ Use only public API of Domain Model
- ✅ Use stubs for external dependencies (`Substitute.For<T>()`)
- ✅ Test one behavior per test
- ✅ Use meaningful test names: `MethodName_WhenCondition_ExpectedBehavior`
- ✅ Assert Domain Events were published
- ✅ Assert Business Rules were broken when expected

**DON'T:**
- ❌ Use `[InternalsVisibleTo]` to expose internals
- ❌ Create special constructors for tests
- ❌ Mock domain objects
- ❌ Test internal implementation details
- ❌ Use `protected` methods to bypass encapsulation

## Integration Tests

### Purpose
Test Command/Query handlers against a real database.

### Test Setup

```csharp
[TestFixture]
public class TestBase
{
    protected string ConnectionString;
    protected ILogger Logger;
    protected IEmailSender EmailSender;
    protected EventsBusMock EventsBus;
    protected ExecutionContextMock ExecutionContext;
    protected IMeetingsModule MeetingsModule;

    [SetUp]
    public async Task BeforeEachTest()
    {
        const string connectionStringEnvironmentVariable =
            "ASPNETCORE_MyMeetings_IntegrationTests_ConnectionString";
        ConnectionString = Environment.GetEnvironmentVariable(
            connectionStringEnvironmentVariable,
            EnvironmentVariableTarget.Machine);

        if (ConnectionString == null)
        {
            throw new ApplicationException(
                $"Define connection string using: {connectionStringEnvironmentVariable}");
        }

        using (var sqlConnection = new SqlConnection(ConnectionString))
        {
            await ClearDatabase(sqlConnection);
        }

        Logger = Substitute.For<ILogger>();
        EmailSender = Substitute.For<IEmailSender>();
        EventsBus = new EventsBusMock();
        ExecutionContext = new ExecutionContextMock(Guid.NewGuid());

        MeetingsStartup.Initialize(
            ConnectionString,
            ExecutionContext,
            Logger,
            EventsBus,
            null);

        MeetingsModule = new MeetingsModule();
    }

    private static async Task ClearDatabase(SqlConnection connection)
    {
        // Clear test data from all module tables
        var sql = "DELETE FROM [meetings].[MeetingAttendees]; " +
                  "DELETE FROM [meetings].[Meetings]; " +
                  // ... other tables
                  "";
        await connection.ExecuteAsync(sql);
    }
}
```

### Integration Test Example

```csharp
[TestFixture]
public class CreateMeetingGroupTests : TestBase
{
    [Test]
    public async Task CreateMeetingGroup_WhenDataIsValid_IsSuccessful()
    {
        // Arrange - Create prerequisite data
        var proposalId = await MeetingsModule.ExecuteCommandAsync(
            new ProposeMeetingGroupCommand(
                "Group Name",
                "Description",
                "Warsaw",
                "PL"));

        // Accept proposal via Administration module
        await AdministrationModule.ExecuteCommandAsync(
            new AcceptMeetingGroupProposalCommand(proposalId));

        // Act - Execute the command being tested
        var groups = await MeetingsModule.ExecuteQueryAsync(
            new GetAllMeetingGroupsQuery());

        // Assert
        Assert.That(groups.Any(x => x.Name == "Group Name"), Is.True);
    }
}
```

### Testing Cross-Module Communication (System Integration Tests)

Use the **Sampling** technique for asynchronous operations:

```csharp
[Test]
public async Task CreateMeetingGroupScenario_WhenProposalIsAccepted()
{
    // Create proposal in Meetings module
    var meetingGroupId = await MeetingsModule.ExecuteCommandAsync(
        new ProposeMeetingGroupCommand("Name", "Description", "Location", "PL"));

    // Wait for it to appear in Administration module
    AssertEventually(
        new GetMeetingGroupProposalFromAdministrationProbe(meetingGroupId, AdministrationModule),
        10000); // 10 second timeout

    // Accept in Administration module
    await AdministrationModule.ExecuteCommandAsync(
        new AcceptMeetingGroupProposalCommand(meetingGroupId));

    // Wait for meeting group to be created
    AssertEventually(
        new GetCreatedMeetingGroupFromMeetingsProbe(meetingGroupId, MeetingsModule),
        15000); // 15 second timeout
}
```

**Probe Implementation:**

```csharp
private class GetCreatedMeetingGroupFromMeetingsProbe : IProbe
{
    private readonly Guid _expectedMeetingGroupId;
    private readonly IMeetingsModule _meetingsModule;
    private List<MeetingGroupDto> _allMeetingGroups;

    public GetCreatedMeetingGroupFromMeetingsProbe(
        Guid expectedMeetingGroupId,
        IMeetingsModule meetingsModule)
    {
        _expectedMeetingGroupId = expectedMeetingGroupId;
        _meetingsModule = meetingsModule;
    }

    public bool IsSatisfied()
    {
        return _allMeetingGroups != null &&
               _allMeetingGroups.Any(x => x.Id == _expectedMeetingGroupId);
    }

    public async Task SampleAsync()
    {
        _allMeetingGroups = await _meetingsModule.ExecuteQueryAsync(
            new GetAllMeetingGroupsQuery());
    }

    public string DescribeFailureTo()
        => $"Meeting group with ID: {_expectedMeetingGroupId} is not created";
}
```

## Architecture Tests

### Purpose
Enforce architectural rules at compile-time using NetArchTest.

### Examples

```csharp
[Test]
public void Domain_Should_Not_Have_Dependency_On_Infrastructure()
{
    var result = Types.InAssembly(DomainAssembly)
        .Should()
        .NotHaveDependencyOn("CompanyName.MyMeetings.Modules.Meetings.Infrastructure")
        .GetResult();

    Assert.That(result.IsSuccessful, Is.True);
}

[Test]
public void Command_Handlers_Should_Be_Internal()
{
    var result = Types.InAssembly(ApplicationAssembly)
        .That()
        .ImplementInterface(typeof(ICommandHandler<>))
        .Or()
        .ImplementInterface(typeof(ICommandHandler<,>))
        .Should()
        .NotBePublic()
        .GetResult();

    Assert.That(result.IsSuccessful, Is.True);
}

[Test]
public void Domain_Should_Not_Reference_MediatR()
{
    var result = Types.InAssembly(DomainAssembly)
        .Should()
        .NotHaveDependencyOn("MediatR")
        .GetResult();

    Assert.That(result.IsSuccessful, Is.True);
}
```

## Running Tests

### Via NUKE Build System

```shell
# Run unit tests
.\build UnitTests

# Run architecture tests
.\build ArchitectureTests

# Run integration tests (requires Docker)
.\build RunAllIntegrationTests
```

### Via Visual Studio Test Explorer

Set the environment variable first:

```powershell
$env:ASPNETCORE_MyMeetings_IntegrationTests_ConnectionString="Server=127.0.0.1,1401;Database=MyMeetings;User=sa;Password=123qwe!@#QWE;Encrypt=False;"
```

## Test Project Structure

```
Modules/{ModuleName}/Tests/
├── UnitTests/
│   ├── {AggregateName}/
│   │   ├── {AggregateName}Tests.cs
│   │   └── {AggregateName}TestsBase.cs
│   └── SeedWork/
│       └── TestBase.cs
├── ArchTests/
│   ├── DomainTests.cs
│   ├── ApplicationTests.cs
│   └── SeedWork/
│       └── TestBase.cs
└── IntegrationTests/
    ├── {Feature}/
    │   └── {Feature}Tests.cs
    └── SeedWork/
        └── TestBase.cs
```
