# Testing Guidelines

## 1. Unit Tests (Domain Model)
*   **Scope**: Test business logic in Aggregates, Entities, and Value Objects.
*   **Location**: `src/Modules/[Module]/Tests/UnitTests` (or similar).
*   **Style**: Arrange-Act-Assert.
*   **Mocking**: Avoid mocking Domain objects. Use real objects or stubs. Mock external dependencies (interfaces) if absolutely necessary, but prefer state-based testing.
*   **Assertions**: Check state changes and Domain Events published.
*   **Example**:
    ```csharp
    [Test]
    public void CreateMeeting_WhenValid_ShouldSucceed()
    {
        // Arrange
        var group = MeetingGroupFactory.Create();
        
        // Act
        var meeting = group.CreateMeeting(...);
        
        // Assert
        Assert.NotNull(meeting);
        Assert.AreEqual(1, meeting.GetDomainEvents().Count);
    }
    ```

## 2. Integration Tests (Module Level)
*   **Scope**: Test Command/Query Handlers with real database and infrastructure.
*   **Location**: `src/Modules/[Module]/Tests/IntegrationTests`.
*   **Setup**:
    *   Use a real SQL Server (Docker container).
    *   Clear database before each test.
    *   Initialize the Module with a Test Composition Root.
*   **Mocking**: Mock **only** external dependencies (Email, Event Bus, other Modules).
*   **Example**:
    ```csharp
    [Test]
    public async Task CreateMeetingCommand_ShouldPersistMeeting()
    {
        // Arrange
        var command = new CreateMeetingCommand(...);
        
        // Act
        var meetingId = await Module.ExecuteCommandAsync(command);
        
        // Assert
        var meeting = await Module.ExecuteQueryAsync(new GetMeetingQuery(meetingId));
        Assert.NotNull(meeting);
    }
    ```

## 3. Architecture Tests
*   **Tool**: NetArchTest.
*   **Scope**: Enforce dependency rules (e.g., Domain cannot depend on Infrastructure).
*   **Location**: `src/Tests/ArchTests`.

## 4. System Integration Tests
*   **Scope**: Test end-to-end flows involving multiple modules.
*   **Async Handling**: Use Polling/Probes to wait for eventual consistency (Integration Events).
