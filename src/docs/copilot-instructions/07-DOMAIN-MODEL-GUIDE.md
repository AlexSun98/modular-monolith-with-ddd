# Domain Model Guide

This guide describes Domain-Driven Design (DDD) tactical patterns used in the Domain layer.

## Domain Model Principles

1. **High Encapsulation** - All members `private` by default, then `internal`, only `public` at the edge
2. **Persistence Ignorance** - No dependencies on infrastructure, databases, EF Core
3. **Rich Behavior** - All business logic in domain, not in application layer
4. **Low Primitive Obsession** - Use Value Objects instead of primitives
5. **Business Language** - Use ubiquitous language from the domain
6. **Testable Design** - Easy to test without mocking domain objects

## Aggregates

### Aggregate Definition

```csharp
// Location: Modules/{Module}/Domain/{AggregateName}/{AggregateName}.cs
public class MeetingGroup : Entity, IAggregateRoot
{
    public MeetingGroupId Id { get; private set; }

    private string _name;
    private string _description;
    private MeetingGroupLocation _location;
    private MemberId _creatorId;
    private List<MeetingGroupMember> _members;
    private DateTime _createDate;
    private DateTime? _paymentDateTo;

    // Private constructor for EF Core
    private MeetingGroup()
    {
        _members = new List<MeetingGroupMember>();
    }

    // Internal factory method - called from other aggregate
    internal static MeetingGroup CreateBasedOnProposal(
        MeetingGroupProposalId meetingGroupProposalId,
        string name,
        string description,
        MeetingGroupLocation location,
        MemberId creatorId)
    {
        return new MeetingGroup(meetingGroupProposalId, name, description, location, creatorId);
    }

    private MeetingGroup(
        MeetingGroupProposalId meetingGroupProposalId,
        string name,
        string description,
        MeetingGroupLocation location,
        MemberId creatorId)
    {
        Id = new MeetingGroupId(meetingGroupProposalId.Value);
        _name = name;
        _description = description;
        _location = location;
        _creatorId = creatorId;
        _createDate = SystemClock.Now;
        _members = new List<MeetingGroupMember>();

        AddDomainEvent(new MeetingGroupCreatedDomainEvent(Id, creatorId));

        // Add creator as organizer
        _members.Add(MeetingGroupMember.CreateOrganizer(Id, creatorId, SystemClock.Now));
    }

    // Public business method
    public Meeting CreateMeeting(
        string title,
        MeetingTerm term,
        string description,
        MeetingLocation location,
        int? attendeesLimit,
        int guestsLimit,
        Term rsvpTerm,
        MoneyValue eventFee,
        List<MemberId> hostsMembersIds,
        MemberId creatorId)
    {
        // Check business rules
        this.CheckRule(new MeetingCanBeOrganizedOnlyByPayedGroupRule(_paymentDateTo));
        this.CheckRule(new MeetingHostMustBeAMeetingGroupMemberRule(creatorId, hostsMembersIds, _members));

        // Create child entity
        return new Meeting(
            this.Id,
            title,
            term,
            description,
            location,
            attendeesLimit,
            guestsLimit,
            rsvpTerm,
            eventFee,
            hostsMembersIds,
            creatorId);
    }

    // Business method that modifies state
    public void JoinToGroupMember(MemberId memberId)
    {
        this.CheckRule(new MeetingGroupMemberCannotBeAddedTwiceRule(_members, memberId));

        _members.Add(MeetingGroupMember.CreateMember(this.Id, memberId, SystemClock.Now));

        AddDomainEvent(new NewMeetingGroupMemberJoinedDomainEvent(this.Id, memberId));
    }

    public void UpdatePaymentInfo(DateTime paymentDateTo)
    {
        _paymentDateTo = paymentDateTo;
    }
}
```

### IAggregateRoot Marker Interface

```csharp
// Location: BuildingBlocks/Domain/IAggregateRoot.cs
public interface IAggregateRoot
{
    // Marker interface - no members
    // Indicates this entity is an aggregate root
}
```

## Entities

### Entity Base Class

```csharp
// Location: BuildingBlocks/Domain/Entity.cs
public abstract class Entity
{
    private List<IDomainEvent> _domainEvents;

    protected Entity()
    {
        _domainEvents = new List<IDomainEvent>();
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents?.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents ??= new List<IDomainEvent>();
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents?.Clear();
    }

    protected void CheckRule(IBusinessRule rule)
    {
        if (rule.IsBroken())
        {
            throw new BusinessRuleValidationException(rule);
        }
    }
}
```

### Child Entity Example

```csharp
// Location: Modules/{Module}/Domain/{AggregateName}/{ChildEntity}.cs
public class MeetingAttendee : Entity
{
    public MeetingAttendeeId Id { get; private set; }

    private MeetingId _meetingId;
    private MemberId _memberId;
    private DateTime _decisionDate;
    private MeetingAttendeeRole _role;
    private int _guestsNumber;
    private bool _decisionChanged;

    private MeetingAttendee()
    {
        // EF Core
    }

    internal MeetingAttendee(
        MeetingId meetingId,
        MemberId memberId,
        DateTime decisionDate,
        MeetingAttendeeRole role,
        int guestsNumber)
    {
        Id = new MeetingAttendeeId(Guid.NewGuid());
        _meetingId = meetingId;
        _memberId = memberId;
        _decisionDate = decisionDate;
        _role = role;
        _guestsNumber = guestsNumber;
        _decisionChanged = false;
    }

    internal void ChangeDecision()
    {
        _decisionChanged = true;
    }

    internal bool IsActiveAttendee(MemberId memberId)
    {
        return _memberId == memberId && !_decisionChanged;
    }
}
```

## Value Objects

### Simple Value Object

```csharp
// Location: Modules/{Module}/Domain/{AggregateName}/{ValueObject}.cs
public class MeetingGroupId : TypedIdValueBase
{
    public MeetingGroupId(Guid value) : base(value)
    {
    }
}

// Base class for ID value objects
public abstract class TypedIdValueBase : IEquatable<TypedIdValueBase>
{
    public Guid Value { get; }

    protected TypedIdValueBase(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new InvalidOperationException("Id value cannot be empty!");
        }

        Value = value;
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        return obj is TypedIdValueBase other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public bool Equals(TypedIdValueBase other)
    {
        return Value == other?.Value;
    }

    public static bool operator ==(TypedIdValueBase left, TypedIdValueBase right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TypedIdValueBase left, TypedIdValueBase right)
    {
        return !Equals(left, right);
    }
}
```

### Complex Value Object

```csharp
// Location: Modules/{Module}/Domain/{AggregateName}/{ValueObject}.cs
public class MeetingGroupLocation : ValueObject
{
    public string City { get; }
    public string CountryCode { get; }

    public MeetingGroupLocation(string city, string countryCode)
    {
        City = city;
        CountryCode = countryCode;
    }

    // Required for EF Core
    private MeetingGroupLocation() { }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return City;
        yield return CountryCode;
    }
}

// Value Object base class
public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object obj)
    {
        if (obj == null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject left, ValueObject right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ValueObject left, ValueObject right)
    {
        return !Equals(left, right);
    }
}
```

### Value Object with Behavior

```csharp
// Location: Modules/{Module}/Domain/SharedKernel/MoneyValue.cs
public class MoneyValue : ValueObject
{
    public decimal Value { get; }
    public string Currency { get; }

    private MoneyValue(decimal value, string currency)
    {
        Value = value;
        Currency = currency;
    }

    public static MoneyValue Of(decimal value, string currency)
    {
        return new MoneyValue(value, currency);
    }

    public static MoneyValue Zero => new MoneyValue(0, "USD");

    public MoneyValue Add(MoneyValue other)
    {
        if (Currency != other.Currency)
        {
            throw new DomainException("Cannot add money with different currencies");
        }

        return new MoneyValue(Value + other.Value, Currency);
    }

    public MoneyValue Multiply(int multiplier)
    {
        return new MoneyValue(Value * multiplier, Currency);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return Currency;
    }
}
```

## Business Rules

### Business Rule Interface

```csharp
// Location: BuildingBlocks/Domain/IBusinessRule.cs
public interface IBusinessRule
{
    bool IsBroken();
    string Message { get; }
}
```

### Business Rule Implementation

```csharp
// Location: Modules/{Module}/Domain/{AggregateName}/Rules/{RuleName}Rule.cs
public class MeetingCanBeOrganizedOnlyByPayedGroupRule : IBusinessRule
{
    private readonly DateTime? _paymentDateTo;

    public MeetingCanBeOrganizedOnlyByPayedGroupRule(DateTime? paymentDateTo)
    {
        _paymentDateTo = paymentDateTo;
    }

    public bool IsBroken()
    {
        return !_paymentDateTo.HasValue || _paymentDateTo.Value < SystemClock.Now;
    }

    public string Message => "Meeting can be organized only by paid group.";
}

public class MeetingHostMustBeAMeetingGroupMemberRule : IBusinessRule
{
    private readonly MemberId _creatorId;
    private readonly List<MemberId> _hostsMembersIds;
    private readonly List<MeetingGroupMember> _members;

    public MeetingHostMustBeAMeetingGroupMemberRule(
        MemberId creatorId,
        List<MemberId> hostsMembersIds,
        List<MeetingGroupMember> members)
    {
        _creatorId = creatorId;
        _hostsMembersIds = hostsMembersIds;
        _members = members;
    }

    public bool IsBroken()
    {
        var allHostsIds = _hostsMembersIds.Union(new[] { _creatorId }).ToList();
        var memberIds = _members.Select(x => x.MemberId).ToList();

        return allHostsIds.Any(hostId => !memberIds.Contains(hostId));
    }

    public string Message => "Meeting host must be a member of the meeting group.";
}

public class MemberCannotBeAnAttendeeOfMeetingMoreThanOnceRule : IBusinessRule
{
    private readonly MemberId _memberId;
    private readonly List<MeetingAttendee> _attendees;

    public MemberCannotBeAnAttendeeOfMeetingMoreThanOnceRule(
        MemberId memberId,
        List<MeetingAttendee> attendees)
    {
        _memberId = memberId;
        _attendees = attendees;
    }

    public bool IsBroken()
    {
        return _attendees.Any(x => x.IsActiveAttendee(_memberId));
    }

    public string Message => "Member is already an attendee of this meeting.";
}
```

## Domain Events

### Domain Event Definition

```csharp
// Location: Modules/{Module}/Domain/{AggregateName}/Events/{EventName}DomainEvent.cs
public class MeetingGroupCreatedDomainEvent : DomainEventBase
{
    public MeetingGroupId MeetingGroupId { get; }
    public MemberId CreatorId { get; }

    public MeetingGroupCreatedDomainEvent(MeetingGroupId meetingGroupId, MemberId creatorId)
    {
        MeetingGroupId = meetingGroupId;
        CreatorId = creatorId;
    }
}

public class NewMeetingGroupMemberJoinedDomainEvent : DomainEventBase
{
    public MeetingGroupId MeetingGroupId { get; }
    public MemberId MemberId { get; }

    public NewMeetingGroupMemberJoinedDomainEvent(
        MeetingGroupId meetingGroupId,
        MemberId memberId)
    {
        MeetingGroupId = meetingGroupId;
        MemberId = memberId;
    }
}
```

### Domain Event Base

```csharp
// Location: BuildingBlocks/Domain/DomainEventBase.cs
public abstract class DomainEventBase : IDomainEvent
{
    public Guid Id { get; }
    public DateTime OccurredOn { get; }

    protected DomainEventBase()
    {
        Id = Guid.NewGuid();
        OccurredOn = SystemClock.Now;
    }
}
```

## Domain Services

Use when logic doesn't fit in a single aggregate:

```csharp
// Location: Modules/{Module}/Domain/Services/{ServiceName}.cs
public class MeetingGroupProposalAcceptanceService
{
    public MeetingGroup AcceptProposal(
        MeetingGroupProposal proposal,
        IUsersCounter usersCounter)
    {
        // Logic that spans multiple aggregates
        if (!proposal.CanBeAccepted())
        {
            throw new DomainException("Proposal cannot be accepted");
        }

        // Create meeting group from proposal
        return proposal.CreateMeetingGroup();
    }
}
```

## Repository Interfaces

```csharp
// Location: Modules/{Module}/Domain/{AggregateName}/I{AggregateName}Repository.cs
public interface IMeetingGroupRepository
{
    Task<MeetingGroup> GetByIdAsync(MeetingGroupId id);
    Task AddAsync(MeetingGroup meetingGroup);
}

public interface IMeetingRepository
{
    Task<Meeting> GetByIdAsync(MeetingId id);
    Task AddAsync(Meeting meeting);
}
```

## Domain Model File Structure

```
Modules/{Module}/Domain/
├── {AggregateName}/
│   ├── {AggregateName}.cs           # Aggregate root
│   ├── {AggregateName}Id.cs         # Typed ID value object
│   ├── {ChildEntity}.cs             # Child entities
│   ├── I{AggregateName}Repository.cs # Repository interface
│   ├── Events/
│   │   ├── {Event1}DomainEvent.cs
│   │   └── {Event2}DomainEvent.cs
│   └── Rules/
│       ├── {Rule1}Rule.cs
│       └── {Rule2}Rule.cs
├── SharedKernel/                     # Shared value objects
│   ├── MoneyValue.cs
│   └── Address.cs
└── Services/                         # Domain services
    └── {ServiceName}.cs
```

## Best Practices

### DO:
- ✅ Keep aggregates small and focused
- ✅ Use Value Objects for grouped properties
- ✅ Validate invariants in constructors and methods
- ✅ Publish Domain Events for state changes
- ✅ Use private/internal constructors
- ✅ Name rules with business language

### DON'T:
- ❌ Reference other aggregates directly (use IDs)
- ❌ Add public setters to properties
- ❌ Leak domain logic to application layer
- ❌ Create anemic domain models (data bags)
- ❌ Use inheritance for sharing behavior (prefer composition)
